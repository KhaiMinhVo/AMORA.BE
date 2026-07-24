FROM python:3.11-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY main.py .

ENV AI_BIND_HOST=0.0.0.0
ENV AI_PORT=8000
EXPOSE 8000

CMD ["python", "main.py"]
