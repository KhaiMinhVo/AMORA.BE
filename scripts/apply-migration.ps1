param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

Set-Location $ProjectRoot

docker compose up -d postgres

# Wait for PostgreSQL to be healthy
$maxAttempts = 30
$attempt = 0
Write-Host "Waiting for PostgreSQL to be ready..."
do {
    $attempt++
    $health = docker inspect --format='{{.State.Health.Status}}' amora-postgres 2>$null
    if ($health -eq "healthy") {
        Write-Host "PostgreSQL is ready!"
        break
    }
    Write-Host "Attempt $attempt/$maxAttempts - PostgreSQL status: $health"
    Start-Sleep -Seconds 2
} while ($attempt -lt $maxAttempts)

if ($health -ne "healthy") {
    Write-Error "PostgreSQL failed to become healthy after $maxAttempts attempts"
    exit 1
}

# Wait additional time for authentication to be fully ready
Write-Host "Waiting for authentication to be ready..."
Start-Sleep -Seconds 10

$env:NUGET_PACKAGES = 'D:\nuget-packages'

dotnet ef database update `
    --project .\src\Amora.Infrastructure\Amora.Infrastructure.csproj `
    --startup-project .\src\Amora.Api\Amora.Api.csproj