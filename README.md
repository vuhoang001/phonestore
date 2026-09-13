# PhoneStore — Website bán điện thoại (đồ án TMĐT full-stack)

Hệ thống thương mại điện tử chuyên **điện thoại & phụ kiện**, xây theo **Clean Architecture (.NET 8)** + **Vue 3**. Mô phỏng kiến trúc dự án ShopViet nhưng nghiệp vụ đặc thù ngành điện thoại.

## Tech stack
- **Backend**: ASP.NET Core 8 (Clean Architecture: Domain / Application / Infrastructure / API), EF Core 8 + PostgreSQL 16, JWT auth, FluentValidation, Serilog, MinIO (lưu ảnh), SignalR (thông báo realtime), Swagger.
- **Frontend**: Vue 3 + TypeScript + Vite, PrimeVue 4, Pinia, Vue Router, Axios, Chart.js.
- **Hạ tầng**: Docker Compose (db + minio + api + web).

## Nghiệp vụ đặc thù điện thoại
- Thương hiệu (Apple/Samsung/Xiaomi...), biến thể **Màu × Dung lượng**, **thông số kỹ thuật** gom nhóm, **so sánh máy**.
- **IMEI/Serial + phiếu bảo hành** (tra cứu theo IMEI/mã đơn), **trả góp** (mock), **thu cũ đổi mới**.
- Giỏ hàng (khách vãng lai + đồng bộ khi đăng nhập), đặt hàng, thanh toán VNPay/COD/Trả góp, coupon, Flash Sale, đánh giá, wishlist, thông báo realtime, báo cáo/thống kê, phân quyền Admin/Customer.

## Chạy nhanh
```bash
cp .env.example .env   # điền secret nếu cần
docker compose up -d --build
# web: http://localhost:5174 · api: http://localhost:8081/swagger
# (DB 5433 · MinIO 9002/9003 — đổi cổng để chạy song song dự án 'hai')
```
Tài khoản demo: `admin@phone.com` / `Admin@123`.

## Cấu trúc
```
backend/               # .NET 8 solution (PhoneStore.sln)
  src/PhoneStore.Domain          # Entities + Enums (lõi)
  src/PhoneStore.Application      # DTOs, Interfaces, Services (business logic)
  src/PhoneStore.Infrastructure   # EF Core DbContext, JWT, MinIO, Seeder
  src/PhoneStore.API              # Controllers, Middleware, Program.cs
frontend/              # Vue 3 + PrimeVue + Pinia
docker-compose.yml
```

> Trạng thái: đang phát triển theo module — xem [`TODO.md`](TODO.md).
