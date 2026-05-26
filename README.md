# Hướng dẫn chạy dự án API Rate Limiter (.NET 8)

Dưới đây là các bước để tải dự án từ Git về và khởi chạy trên máy cá nhân của bạn.

---

## 🛠️ Yêu Cầu Trước Khi Chạy

Hãy đảm bảo máy của bạn đã cài đặt các công cụ sau:
1. **.NET 8.0 SDK** ([Tải về tại đây](https://dotnet.microsoft.com/download/dotnet/8.0))
2. **Docker / Docker Desktop** (Dùng để chạy nhanh Redis Server)

---

## 🏃 Các Bước Khởi Chạy Dự Án

### Bước 1: Clone dự án về máy
Mở Terminal / Command Prompt và chạy lệnh sau để tải mã nguồn:
```bash
git clone <URL_KHO_CHUA_GIT_CUA_BAN>
cd "Api Rate Limiter"
```

### Bước 2: Khởi động Redis
Thuật toán Rate Limiting phân tán của dự án cần Redis. Bạn có thể khởi động nhanh một container Redis bằng Docker:
```bash
docker run -d --name redis-rate-limiter -p 6379:6379 redis:alpine
```

### Bước 3: Khởi chạy Web API
Chạy lệnh sau tại thư mục gốc của dự án để tải thư viện và khởi động Server:
```bash
# Khôi phục các gói NuGet và chạy dự án
dotnet run --project RateLimiter.Api
```

Khi Terminal thông báo API đã chạy thành công (mặc định tại cổng `http://localhost:8080`), bạn có thể mở trình duyệt và truy cập vào đường dẫn sau để thử nghiệm:
🔗 **Swagger UI:** [http://localhost:8080/swagger](http://localhost:8080/swagger)

---

## 🧪 Chạy Unit Test (Tùy chọn)
Nếu muốn chạy các bộ kiểm thử tự động của dự án, sử dụng lệnh:
```bash
dotnet test
```
