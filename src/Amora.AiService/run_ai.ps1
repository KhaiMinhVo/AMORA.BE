$ErrorActionPreference = "Stop"

Set-Location "D:\FPTU\EXE\AMORA.BE\src\Amora.AiService"

if (Test-Path ".env") {
    Get-Content ".env" |
        Where-Object { $_ -match '^[A-Za-z_][A-Za-z0-9_]*=' } |
        ForEach-Object {
            $name, $value = $_ -split '=', 2
            Set-Item -Path "Env:$name" -Value $value
        }
}

if ([string]::IsNullOrWhiteSpace($env:AI_SERVICE_API_KEY)) {
    throw "AI_SERVICE_API_KEY is required. Copy .env.example to .env and configure it."
}

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
