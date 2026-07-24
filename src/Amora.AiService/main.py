import ipaddress
import hmac
import logging
import os
import socket
import tempfile
import threading
from pathlib import Path
from urllib.parse import urlparse

import requests
from fastapi import Depends, FastAPI, Header, HTTPException
from faster_whisper import WhisperModel
from pydantic import BaseModel, Field
from transformers import pipeline

WHISPER_MODEL_NAME = os.getenv("WHISPER_MODEL_NAME", "base")
TOXIC_MODEL_NAME = os.getenv(
    "TOXIC_MODEL_NAME",
    "unitary/multilingual-toxic-xlm-roberta",
)
TOXIC_THRESHOLD = float(os.getenv("TOXIC_THRESHOLD", "0.85"))
MAX_AUDIO_BYTES = int(os.getenv("MAX_AUDIO_BYTES", str(20 * 1024 * 1024)))
DOWNLOAD_TIMEOUT_SECONDS = float(os.getenv("DOWNLOAD_TIMEOUT_SECONDS", "20"))
AI_SERVICE_API_KEY = os.getenv("AI_SERVICE_API_KEY", "").strip()
ALLOWED_AUDIO_HOSTS = {
    host.strip().lower().rstrip(".")
    for host in os.getenv(
        "AI_ALLOWED_AUDIO_HOSTS",
        "amora-voice-bucket.s3.amazonaws.com,cdn.amora.pro.vn",
    ).split(",")
    if host.strip()
}

if not AI_SERVICE_API_KEY:
    raise RuntimeError("AI_SERVICE_API_KEY must be configured.")
if not ALLOWED_AUDIO_HOSTS:
    raise RuntimeError("AI_ALLOWED_AUDIO_HOSTS must contain at least one trusted host.")

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("amora-ai")

logger.info("Loading Whisper model %s", WHISPER_MODEL_NAME)
whisper_model = WhisperModel(WHISPER_MODEL_NAME, device="cpu", compute_type="int8")

logger.info("Loading toxicity model %s", TOXIC_MODEL_NAME)
toxic_pipeline = pipeline(
    "text-classification",
    model=TOXIC_MODEL_NAME,
    tokenizer=TOXIC_MODEL_NAME,
    top_k=None,
)

whisper_lock = threading.Lock()
toxic_lock = threading.Lock()
app = FastAPI(title="Amora AI Internal Service", docs_url=None, redoc_url=None)


class TranscribeRequest(BaseModel):
    audioUrl: str = Field(min_length=1, max_length=2048)


class TranscribeResponse(BaseModel):
    text: str


class EvaluateRequest(BaseModel):
    text: str = Field(max_length=10_000)


class EvaluateResponse(BaseModel):
    isToxic: bool
    score: float
    labels: dict[str, float]


def require_internal_api_key(
    x_internal_api_key: str | None = Header(default=None),
) -> None:
    if not x_internal_api_key or not hmac.compare_digest(
        x_internal_api_key,
        AI_SERVICE_API_KEY,
    ):
        raise HTTPException(status_code=401, detail="Unauthorized")


def _is_public_ip(address: str) -> bool:
    ip = ipaddress.ip_address(address)
    return ip.is_global


def _validate_audio_url(audio_url: str) -> None:
    parsed = urlparse(audio_url)
    host = (parsed.hostname or "").lower().rstrip(".")

    if parsed.scheme != "https" or not host or parsed.username or parsed.password:
        raise HTTPException(status_code=400, detail="Invalid audio URL")
    if host not in ALLOWED_AUDIO_HOSTS:
        raise HTTPException(status_code=400, detail="Audio host is not allowed")

    try:
        addresses = {
            item[4][0]
            for item in socket.getaddrinfo(host, parsed.port or 443, type=socket.SOCK_STREAM)
        }
    except socket.gaierror as exc:
        raise HTTPException(status_code=400, detail="Audio host cannot be resolved") from exc

    if not addresses or any(not _is_public_ip(address) for address in addresses):
        raise HTTPException(status_code=400, detail="Audio host resolved to a blocked address")


def _download_audio(audio_url: str, destination: Path) -> None:
    _validate_audio_url(audio_url)

    with requests.get(
        audio_url,
        stream=True,
        allow_redirects=False,
        timeout=(5, DOWNLOAD_TIMEOUT_SECONDS),
    ) as response:
        response.raise_for_status()

        content_type = response.headers.get("Content-Type", "").split(";", 1)[0].lower()
        if content_type and not (
            content_type.startswith("audio/")
            or content_type == "application/octet-stream"
        ):
            raise HTTPException(status_code=400, detail="URL does not contain audio")

        content_length = response.headers.get("Content-Length")
        if content_length and int(content_length) > MAX_AUDIO_BYTES:
            raise HTTPException(status_code=413, detail="Audio file is too large")

        downloaded = 0
        with destination.open("wb") as output:
            for chunk in response.iter_content(chunk_size=64 * 1024):
                if not chunk:
                    continue
                downloaded += len(chunk)
                if downloaded > MAX_AUDIO_BYTES:
                    raise HTTPException(status_code=413, detail="Audio file is too large")
                output.write(chunk)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "healthy"}


@app.post(
    "/transcribe",
    response_model=TranscribeResponse,
    dependencies=[Depends(require_internal_api_key)],
)
def transcribe_audio(req: TranscribeRequest) -> TranscribeResponse:
    temp_path: Path | None = None
    try:
        suffix = Path(urlparse(req.audioUrl).path).suffix or ".audio"
        with tempfile.NamedTemporaryFile(suffix=suffix, delete=False) as temp_file:
            temp_path = Path(temp_file.name)

        _download_audio(req.audioUrl, temp_path)
        with whisper_lock:
            segments, _ = whisper_model.transcribe(str(temp_path), beam_size=5)
            text = "".join(segment.text for segment in segments)

        return TranscribeResponse(text=text.strip())
    except HTTPException:
        raise
    except requests.RequestException as exc:
        logger.warning("Audio download failed: %s", exc)
        raise HTTPException(status_code=422, detail="Unable to download audio") from exc
    except Exception:
        logger.exception("Audio transcription failed")
        raise HTTPException(status_code=500, detail="Audio transcription failed")
    finally:
        if temp_path is not None:
            temp_path.unlink(missing_ok=True)


@app.post(
    "/evaluate",
    response_model=EvaluateResponse,
    dependencies=[Depends(require_internal_api_key)],
)
def evaluate_text(req: EvaluateRequest) -> EvaluateResponse:
    text = req.text.strip()
    if not text:
        return EvaluateResponse(isToxic=False, score=0.0, labels={})

    try:
        with toxic_lock:
            raw_results = toxic_pipeline(text)
        results = raw_results[0] if raw_results and isinstance(raw_results[0], list) else raw_results

        toxic_score = 0.0
        labels: dict[str, float] = {}
        for result in results:
            label = str(result["label"]).lower()
            score = float(result["score"])
            labels[label] = score
            if label in {"toxic", "severe_toxic"}:
                toxic_score = max(toxic_score, score)

        return EvaluateResponse(
            isToxic=toxic_score >= TOXIC_THRESHOLD,
            score=toxic_score,
            labels=labels,
        )
    except Exception:
        logger.exception("Toxicity evaluation failed")
        raise HTTPException(status_code=500, detail="Toxicity evaluation failed")


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(
        app,
        host=os.getenv("AI_BIND_HOST", "127.0.0.1"),
        port=int(os.getenv("AI_PORT", "8000")),
    )
