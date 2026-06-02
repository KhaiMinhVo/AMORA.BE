$ErrorActionPreference = "Stop"

Set-Location "D:\FPTU\EXE\AMORA.BE\src\Amora.AiService"

if (-not (Test-Path "venv")) {
    Write-Host "Creating Python virtual environment..."
    python -m venv venv
}

Write-Host "Activating virtual environment..."
& .\venv\Scripts\Activate.ps1

Write-Host "Installing requirements (This might take a few minutes)..."
pip install -r requirements.txt

Write-Host "Starting AI Server..."
python main.py
