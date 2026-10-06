# PhoneStore — Tài liệu tổng quan dự án

> Tài liệu này giúp người đọc hiểu **dự án là gì, dùng công nghệ gì, có tính năng gì, và hoạt động ra sao**.
> Đối tượng: lập trình viên mới tiếp cận dự án (chưa cần biết trước gì về source).

---

## 1. Dự án này là gì?

**PhoneStore** là một website **thương mại điện tử (TMĐT) chuyên bán điện thoại & phụ kiện** — kiểu Thế Giới Di Động / CellphoneS thu nhỏ. Dự án là một đồ án hoàn chỉnh gồm:

- **Trang khách hàng (storefront):** xem sản phẩm, so sánh máy, giỏ hàng, đặt hàng, thanh toán, trả góp, thu cũ đổi mới, tra cứu bảo hành, đánh giá…
- **Trang quản trị (admin):** quản lý sản phẩm/thương hiệu/danh mục, đơn hàng, flash sale, mã giảm giá, vận chuyển, thu cũ, báo cáo, nhật ký hệ thống…

Điểm khác biệt so với shop tổng hợp: dự án mô hình hoá **đúng nghiệp vụ ngành điện thoại** (biến thể màu × dung lượng, IMEI/bảo hành, trả góp, thu cũ đổi mới, thông số kỹ thuật để so sánh).

---

## 2. Công nghệ sử dụng (Tech Stack)

### 2.1. Backend — API (.NET 8)

| Thành phần | Công nghệ | Vai trò |
|---|---|---|
| Nền tảng | **.NET 8** (ASP.NET Core Web API) | Khung chạy API |
| ORM / CSDL | **Entity Framework Core 8** + **Npgsql** | Truy cập **PostgreSQL** bằng C# |
| CSDL | **PostgreSQL** | Lưu dữ liệu |
| Xác thực | **JWT Bearer** (`Microsoft.AspNetCore.Authentication.JwtBearer`) | Đăng nhập bằng token |
| Validation | **FluentValidation** | Kiểm tra dữ liệu đầu vào |
| Logging | **Serilog** | Ghi log có cấu trúc |
| API docs | **Swashbuckle / Swagger** | Trang thử API tự động |
| Lưu file | **MinIO** (S3-compatible) | Lưu ảnh sản phẩm/ảnh đánh giá |
| Realtime | **SignalR** | Thông báo tức thời (đơn mới, thanh toán…) |

### 2.2. Frontend — Web (Vue 3)

| Thành phần | Công nghệ | Vai trò |
|---|---|---|
| Framework | **Vue 3.5** (`<script setup lang="ts">`) | Giao diện SPA |
| Ngôn ngữ | **TypeScript** | An toàn kiểu dữ liệu |
| Build tool | **Vite 5** | Dev server + đóng gói |
| UI library | **PrimeVue 4** + **PrimeFlex** + **PrimeIcons** | Bộ component + layout + icon |
| State management | **Pinia** | Quản lý trạng thái (giỏ hàng, auth…) |
| Routing | **Vue Router 4** | Điều hướng trang |
| Gọi API | **Axios** | HTTP client (có tự refresh token) |
| Biểu đồ | **Chart.js** | Vẽ biểu đồ báo cáo |
| Realtime | **@microsoft/signalr** | Nhận thông báo realtime |

### 2.3. Hạ tầng (Infrastructure)

- **Docker Compose** chạy 4 service: `web` (Nginx phục vụ bản build Vue), `api` (.NET), `db` (PostgreSQL), `minio` (lưu file).
- Cổng (đã tách riêng để chạy song song các dự án khác trên cùng máy):

| Service | URL / Cổng |
|---|---|
| Web (frontend) | http://localhost:5174 |
| API + Swagger | http://localhost:8081/swagger |
| PostgreSQL | `localhost:5433` |
| MinIO (API / Console) | `localhost:9002` / `localhost:9003` |

---

## 3. Kiến trúc tổng thể

### 3.1. Backend theo Clean Architecture

Code backend chia **4 tầng**, phụ thuộc một chiều từ ngoài vào trong:

```
API  ──▶  Infrastructure  ──▶  Application  ──▶  Domain
(HTTP)      (EF, MinIO,         (nghiệp vụ,        (lõi: entity,
            SignalR, SMTP)       service, DTO)       enum — không
                                                     phụ thuộc ai)
```

- **Domain** — lõi: các *entity* (Product, Order…) và *enum* (OrderStatus…). Không tham chiếu ra ngoài.
- **Application** — **nghiệp vụ** nằm ở đây: các *Service* (OrderService, PaymentService…), *DTO* (dữ liệu trả về client dưới dạng `record`), *interface*. Truy cập DB qua `IAppDbContext`.
- **Infrastructure** — hiện thực kỹ thuật: `AppDbContext` (EF Core), gửi email (SMTP), lưu file (MinIO), đẩy realtime (SignalR).
- **API** — controllers nhận HTTP request, gọi service, trả JSON. Chứa cả job nền (`OrderExpiryService`).

> **Quy tắc quan trọng:** không bao giờ trả thẳng *entity* ra client — luôn map sang **DTO** (`record`). Lỗi nghiệp vụ ném `AppException` → middleware đổi thành mã HTTP phù hợp.

### 3.2. Toàn cảnh hệ thống

```
┌─────────────┐   HTTP/JSON    ┌──────────────┐   EF Core    ┌────────────┐
│  Trình duyệt │ ◀────────────▶ │   API (.NET)  │ ◀──────────▶ │ PostgreSQL │
│  (Vue SPA)   │                │               │              └────────────┘
│              │   SignalR      │               │   S3 API     ┌────────────┐
│              │ ◀────realtime─▶ │               │ ◀──────────▶ │   MinIO    │
└─────────────┘                │               │   SMTP       └────────────┘
                               │               │ ──────────▶  Gmail (gửi email)
                               └──────────────┘
```

### 3.3. Database được tạo thế nào?

Dự án dùng **EF Core `EnsureCreated`** lúc khởi động: lần chạy đầu tiên, EF tự sinh schema từ model C# và **seed sẵn** tài khoản admin/khách + dữ liệu mẫu. (Chưa dùng Migrations.)

Tài khoản demo:
- Admin: `admin@phone.com` / `Admin@123`
- Khách: `customer@phone.com` / `Customer@123`

---

## 4. Cách chạy dự án

```bash
# Chạy toàn bộ (web + api + db + minio)
docker compose up -d --build

# Mở web
http://localhost:5174

# Thử API
http://localhost:8081/swagger
```

Sau khi sửa frontend:

```bash
cd frontend && npm run build          # verify build xanh
docker compose up -d --build web      # rebuild lại container web
```

> Máy dev hiện **chưa cài .NET** → backend chỉ build/chạy qua Docker (`docker compose build api`).

**Cấu hình bí mật** (SMTP, VNPay…) nạp từ file `.env` (đã bị `.gitignore` bỏ qua, **không commit**). Mẫu các biến xem ở `.env.example`.

---

## 5. Các tính năng chính

### 5.1. Phía khách hàng

| Nhóm | Tính năng |
|---|---|
| Tài khoản | Đăng ký, đăng nhập (JWT), quên/đặt lại mật khẩu qua email, xác nhận email, quản lý địa chỉ |
| Mua sắm | Danh sách sản phẩm (lọc theo hãng/giá/thông số), chi tiết sản phẩm, **so sánh máy**, wishlist, giỏ hàng |
| Đặt hàng | Checkout (chọn địa chỉ, vận chuyển, mã giảm giá), theo dõi đơn, hủy đơn, lịch sử đơn |
| Thanh toán | **COD**, **VNPay** (có mock khi chưa có credential), **trả góp** hàng tháng |
| Dịch vụ | **Thu cũ đổi mới** (khai máy cũ chờ admin định giá), **tra cứu bảo hành** theo IMEI/mã đơn, đánh giá sản phẩm |
| Khác | Thông báo realtime, trang tin/thông tin tĩnh |

### 5.2. Phía quản trị (Admin)

| Nhóm | Tính năng |
|---|---|
| Danh mục hàng | Quản lý **sản phẩm** (+ biến thể, thông số, ảnh), **thương hiệu**, **danh mục** |
| Bán hàng | Quản lý **đơn hàng** (đổi trạng thái, gán IMEI khi giao), **flash sale**, **mã giảm giá (coupon)**, **vận chuyển** |
| Dịch vụ | Duyệt & định giá **thu cũ đổi mới** |
| Phân tích | **Dashboard**, **báo cáo nâng cao** (tài chính / khách hàng / vận hành / hành vi), **nhật ký hệ thống (audit log)** |

---

## 6. Nghiệp vụ đặc thù ngành điện thoại

Đây là phần làm PhoneStore khác một shop tổng hợp:

- **Thương hiệu (Brand):** Apple, Samsung, Xiaomi, OPPO… dùng để lọc & báo cáo theo hãng.
- **Biến thể = Màu × Dung lượng** (`ProductVariant`): mỗi tổ hợp (ví dụ *iPhone 15 — Đen — 256GB*) có **SKU, giá, giá vốn, tồn kho** riêng.
- **Thông số kỹ thuật** (`ProductSpecification`): gom nhóm (Màn hình / Chip / Camera / Pin…) để render bảng thông số & **so sánh máy**.
- **IMEI / Serial:** khi admin chuyển đơn sang *Đang giao*, nhập IMEI cho từng máy (`OrderItem.Imei`) → tự sinh **phiếu bảo hành** (`WarrantyRecord`) tra cứu được theo IMEI/mã đơn.
- **Trả góp** (`InstallmentPayment`): chọn kỳ 6/9/12 tháng ngay ở checkout → sinh **lịch trả theo tháng**, khách trả từng kỳ (mock).
- **Thu cũ đổi mới** (`TradeInRequest`): khách khai máy cũ → admin định giá thu.
- **Flash Sale & Coupon:** giá khuyến mãi được tính **ở server** (không tin giá client gửi lên).

---

## 7. Các luồng hoạt động quan trọng

### 7.1. Luồng đặt hàng & thanh toán

```
1. Khách thêm sản phẩm vào giỏ (Cart)
2. Vào Checkout → chọn địa chỉ, vận chuyển, mã giảm giá, phương thức thanh toán
3. POST /api/orders  (OrderService.CreateAsync)
   ├─ Kiểm tra tồn kho (chống bán quá số lượng bằng concurrency token)
   ├─ Tính giá flash sale + áp coupon  (SERVER tính, không tin client)
   ├─ Trừ kho, tăng SoldCount
   ├─ Nếu trả góp: tạo lịch InstallmentPayment theo tháng
   └─ Tạo Order (Pending) + Payment (Pending) trong 1 transaction
4. Rẽ nhánh theo phương thức:
   • COD        → về trang chi tiết đơn, chờ admin xác nhận
   • VNPay      → POST /api/payments/vnpay/{id} → chuyển sang cổng VNPay (hoặc trang mock)
                   → cổng callback /return + /ipn → verify chữ ký HMAC-SHA512
                   → đúng tiền + đúng chữ ký → Payment = Paid
   • Trả góp    → về chi tiết đơn, trả từng kỳ ở trang Installments
5. Job nền OrderExpiryService: tự hủy đơn online quá hạn chưa thanh toán,
   hoàn kho + hoàn lượt dùng coupon. (COD không bị hủy tự động.)
```

**Vòng đời trạng thái đơn (`OrderStatus`):**

```
Pending ──▶ Confirmed ──▶ Shipping ──▶ Completed
   │            │
   └────────────┴──▶ Cancelled   (kèm hoàn kho)
```

### 7.2. Luồng xác thực (Auth)

- Đăng nhập → API trả **access token (JWT)** + **refresh token**.
- Frontend (Axios) tự đính kèm JWT vào mỗi request; khi token hết hạn thì tự gọi **refresh** để lấy token mới.
- Quên mật khẩu → gửi email chứa link reset (token hết hạn sau 1 giờ).
- Phân quyền theo vai trò (`UserRole`): **Admin** vs **Customer** — các API admin yêu cầu role Admin.

### 7.3. Thông báo realtime (SignalR)

Khi có sự kiện (đơn mới, thanh toán thành công, đơn bị hủy…), backend đẩy thông báo qua **SignalR** → frontend hiện ngay chuông thông báo mà không cần tải lại trang.

---

## 8. Cấu trúc thư mục

### 8.1. Backend (`backend/src/`)

```
PhoneStore.Domain/          # Lõi: Entities/, Enums/
PhoneStore.Application/      # Nghiệp vụ: Services/, DTOs/, Interfaces/, Common/
PhoneStore.Infrastructure/   # EF (Persistence/AppDbContext), Email (SMTP), MinIO, SignalR
PhoneStore.API/             # Controllers/, Services/ (job nền), Program.cs, appsettings.json
```

### 8.2. Frontend (`frontend/src/`)

```
views/          # Trang khách: Home, ProductList, ProductDetail, Compare, Cart,
                #              Checkout, Payment*, Order*, Installments, TradeIn,
                #              WarrantyLookup, Wishlist, Account, Login/Register...
views/admin/    # Trang admin: Dashboard, ProductManage, OrderManage, FlashSaleManage,
                #              CouponManage, TradeInManage, ReportsView, AuditLogView...
stores/         # Pinia: auth.ts, cart.ts, compare.ts, notification.ts
services/       # Axios client + khai báo gọi API (orderApi, paymentApi...)
router/         # Khai báo route + bảo vệ route cần đăng nhập
```

---

## 9. Danh sách API (Controllers)

Mỗi controller là một nhóm endpoint (ví dụ `OrdersController` → `/api/orders/...`):

`Auth`, `Products`, `Brands`, `Categories`, `Cart`, `Orders`, `Payments`,
`Installments`, `Coupons`, `FlashSale`, `Shipping`, `Addresses`, `Reviews`,
`Wishlist`, `Warranty`, `TradeIn`, `Notifications`, `Uploads`, `Dashboard`,
`Reports`, `AdvancedReports`, `AuditLogs`, `Maintenance`.

> Muốn xem chi tiết từng endpoint (tham số, response) → mở **Swagger** tại http://localhost:8081/swagger.

---

## 10. Bảo mật & nguyên tắc quan trọng

- **Không tin client về giá/tồn kho:** giá flash sale, coupon, tổng tiền đều tính lại ở server.
- **JWT + refresh token:** token ngắn hạn + cơ chế làm mới; phân quyền theo vai trò.
- **Chống bán quá kho:** dùng *concurrency token* trên biến thể khi trừ kho.
- **Thanh toán VNPay an toàn:** xác minh **chữ ký HMAC-SHA512**, đối chiếu số tiền, chống xử lý trùng (idempotent).
- **Rate limit** trên endpoint thanh toán (chống spam).
- **Secret tách khỏi code:** SMTP/VNPay nạp qua `.env` (không commit).

---

## 11. Thuật ngữ nhanh (glossary)

| Từ | Nghĩa |
|---|---|
| **Entity** | Lớp C# ánh xạ 1 bảng trong DB (Product, Order…) |
| **DTO** | Dữ liệu định dạng riêng để trả cho client (không lộ entity) |
| **Service** | Nơi chứa logic nghiệp vụ (ở tầng Application) |
| **Variant** | Biến thể sản phẩm = Màu × Dung lượng |
| **SKU** | Mã định danh riêng của từng biến thể |
| **IMEI** | Số serial riêng của mỗi chiếc điện thoại (gắn khi giao hàng) |
| **IPN** | Callback server-to-server của VNPay báo kết quả thanh toán |
| **SignalR** | Công nghệ realtime của .NET (giống WebSocket) |
| **Seed** | Dữ liệu mẫu nạp sẵn khi tạo DB lần đầu |

---

*Tài liệu mô tả kiến trúc & luồng tổng quan. Chi tiết endpoint luôn tra ở Swagger; chi tiết code tra trong các thư mục tương ứng ở mục 8.*
