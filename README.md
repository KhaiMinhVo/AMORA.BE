# Amora Backend

Voice dating API (.NET 8) + Pet System + Python audio workers.

## 🚀 Hướng dẫn Setup cho Developer mới (Teammate)

Làm theo đúng các bước dưới đây để khởi chạy dự án trên máy của bạn (Local Environment):

### Bước 1: Yêu cầu cài đặt (Prerequisites)
Bạn cần đảm bảo máy tính đã cài đặt sẵn các phần mềm sau:
- **[Docker Desktop](https://www.docker.com/products/docker-desktop/)** (Bắt buộc phải mở Docker Desktop lên cho nó chạy ngầm).
- **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)**
- Khuyên dùng IDE: **Visual Studio 2022** hoặc **Rider** hoặc **VS Code**.

### Bước 2: Khởi động các Database & Cấu hình
Dự án sử dụng PostgreSQL, MongoDB, RabbitMQ, Redis và MinIO (S3 ảo). Tất cả đã được đóng gói trong Docker. Mở Terminal tại thư mục gốc của dự án và chạy:
```powershell
docker compose up -d postgres mongo rabbitmq minio redis
```
*(Lệnh này chỉ chạy các dịch vụ nền tảng, không chạy API, để bạn có thể tự Debug API trên Visual Studio).*

### Bước 3: Tạo Bucket MinIO (Lưu trữ ảnh/voice)
Hệ thống dùng MinIO làm server lưu file (giả lập AWS S3). Khi mới chạy Docker lần đầu, bạn cần tạo kho lưu trữ:
1. Mở trình duyệt vào: **http://localhost:9001**
2. Đăng nhập: Username: `admin` / Password: `admin123`
3. Tìm mục **Buckets** (bên trái) -> Bấm **Create Bucket**.
4. Nhập tên bucket bắt buộc là: `amora-voice-bucket` -> Bấm Create.
5. *(Quan trọng)* Ở bucket vừa tạo, bấm vào icon bánh răng (Settings) -> Mục **Access Policy** -> Chuyển từ `Private` sang `Public` để App có thể load ảnh.

### Bước 4: Chạy Migration (Tạo Database)
Cập nhật cấu trúc bảng cho PostgreSQL:
```powershell
cd src\Amora.Api
dotnet ef database update --project ..\Amora.Infrastructure
```

### Bước 5: Chạy API
Mở file Solution (`Amora.sln`) bằng Visual Studio và bấm **F5** (chọn profile là `http`).
Hoặc chạy bằng Terminal:
```powershell
dotnet run --launch-profile http
```

### Bước 6: Test kết quả
Mở trình duyệt, nếu thấy giao diện này là bạn đã thành công:
- **Swagger API:** http://localhost:5002/swagger
- **Health Check:** http://localhost:5002/health

## Local dependencies

Default docker compose services:

- Postgres, MongoDB, RabbitMQ
- Redis (presence)
- MinIO (S3 compatible object storage)

Start all services:

```powershell
docker compose up -d
```

## Storage (MinIO / S3)

Configuration:

- `AWS:ServiceURL` + `AWS:ForcePathStyle=true` for MinIO.
- `Storage:PublicBaseUrl` controls the URL returned to clients.

For local MinIO, create the bucket once in the console:

- MinIO console: http://localhost:9001
- Bucket: `amora-voice-bucket`

## IAP webhooks

Endpoints:

- `POST /api/iap/webhooks/apple`
- `POST /api/iap/webhooks/google`

Required config for production:

- `Iap:AppleSharedSecret`, `Iap:AppleBundleId`
- `Iap:GooglePackageName`, `Iap:GoogleServiceAccountJsonPath`
- Optional Google Pub/Sub OIDC validation: `Iap:GoogleWebhookAudience`, `Iap:GoogleWebhookServiceAccountEmail`

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
