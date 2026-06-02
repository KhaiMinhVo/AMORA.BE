import os
import io
import torch
from faster_whisper import WhisperModel
import requests
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from transformers import pipeline

# Cấu hình
WHISPER_MODEL_NAME = "base"
TOXIC_MODEL_NAME = "unitary/multilingual-toxic-xlm-roberta"
THRESHOLD = 0.85 # Ngưỡng xác định là Toxic

print("Loading Whisper model...")
# Tải Whisper model (dùng faster-whisper cho tốc độ nhanh hơn trên CPU)
whisper_model = WhisperModel(WHISPER_MODEL_NAME, device="cpu", compute_type="int8")

print("Loading Toxic Classification model...")
# Tải mô hình kiểm duyệt (có thể tốn vài phút lần đầu)
# Mô hình này được train trên nhiều ngôn ngữ, hỗ trợ Tiếng Việt
toxic_pipeline = pipeline("text-classification", model=TOXIC_MODEL_NAME, tokenizer=TOXIC_MODEL_NAME, return_all_scores=True)

app = FastAPI(title="Amora AI Local Service")

class TranscribeRequest(BaseModel):
    audioUrl: str

class TranscribeResponse(BaseModel):
    text: str

class EvaluateRequest(BaseModel):
    text: str

class EvaluateResponse(BaseModel):
    isToxic: bool
    score: float
    labels: dict

@app.post("/transcribe", response_model=TranscribeResponse)
async def transcribe_audio(req: TranscribeRequest):
    if not req.audioUrl:
        raise HTTPException(status_code=400, detail="Audio URL is required")
    
    try:
        # Tải file âm thanh về bộ nhớ đệm
        response = requests.get(req.audioUrl)
        response.raise_for_status()
        
        # Whisper yêu cầu file trên đĩa cứng đối với API mặc định
        temp_file_name = "temp_audio.m4a"
        with open(temp_file_name, "wb") as f:
            f.write(response.content)
            
        # Dịch âm thanh thành văn bản
        segments, info = whisper_model.transcribe(temp_file_name, beam_size=5)
        text = "".join([segment.text for segment in segments])
        
        # Xóa file tạm
        if os.path.exists(temp_file_name):
            os.remove(temp_file_name)
            
        return TranscribeResponse(text=text.strip())
        
    except Exception as e:
        print(f"Error in transcribe: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/evaluate", response_model=EvaluateResponse)
async def evaluate_text(req: EvaluateRequest):
    if not req.text or len(req.text.strip()) == 0:
        return EvaluateResponse(isToxic=False, score=0.0, labels={})
    
    try:
        # Chạy pipeline phân loại
        results = toxic_pipeline(req.text)[0]
        
        # Trích xuất nhãn 'toxic' hoặc 'severe_toxic'
        toxic_score = 0.0
        labels_dict = {}
        for res in results:
            labels_dict[res['label']] = res['score']
            if res['label'] == 'toxic' or res['label'] == 'severe_toxic':
                if res['score'] > toxic_score:
                    toxic_score = res['score']
                    
        is_toxic = toxic_score >= THRESHOLD
        
        return EvaluateResponse(
            isToxic=is_toxic,
            score=toxic_score,
            labels=labels_dict
        )
    except Exception as e:
        print(f"Error in evaluate: {e}")
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
