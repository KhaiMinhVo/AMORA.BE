# Amora Backend

Voice dating API (.NET 8) + Pet System + Python audio workers.

## Quick start

```powershell
# 1. Infrastructure
docker compose up -d postgres mongodb rabbitmq

# 2. API (local)
cd src\Amora.Api
dotnet ef database update --project ..\Amora.Infrastructure
dotnet run --launch-profile http
```

- Swagger: http://localhost:5002/swagger  
- Health: http://localhost:5002/health  

## Auth

```http
POST /api/auth/register  { "email", "password", "displayName" }
POST /api/auth/login     { "email", "password" }
POST /api/auth/dev-token { "userId", "displayName" }  # Development
```

JWT có hạn (`Jwt:ExpiryHours`, mặc định 12h).

## Docker full stack

```powershell
docker compose up -d --build
```

API container: http://localhost:5002

## Pet Coin

- Đăng ký: **100 PC**
- Đăng nhập mỗi ngày: **+15 PC**
- Online cùng partner (heartbeat): **+5 PC/ngày**

## SignalR

- Chat: `/hubs/chat`
- Pet: `/hubs/pet` — gọi `Heartbeat(matchId)` mỗi ~30s
