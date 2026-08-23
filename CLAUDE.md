# PhoneStore — Hướng dẫn cho Claude Code

Đồ án TMĐT **chuyên bán điện thoại & phụ kiện**. **Backend** .NET 8 Clean Architecture + PostgreSQL (`backend/`). **Frontend** Vue 3 + PrimeVue + Pinia (`frontend/`). Kiến trúc & tech mô phỏng dự án ShopViet nhưng nghiệp vụ đặc thù ngành điện thoại.

## ⚠️ Quy tắc giao diện (bắt buộc)
Mọi thay đổi UI/CSS/`.vue` **PHẢI** tuân theo **[`frontend/CLAUDE.md`](frontend/CLAUDE.md)** — design system (tông **xanh công nghệ**, dùng design token, chống "AI-look"). Đọc file đó trước khi sửa giao diện.

## Chạy dự án
- Toàn bộ: `docker compose up -d --build` → web http://localhost:5173 · api http://localhost:8080/swagger
- Sau khi sửa frontend, verify: `cd frontend && npm run build`; rebuild container: `docker compose up -d --build web`
- Máy này **chưa cài dotnet** → backend chỉ build/verify qua Docker (`docker compose build api`).
- Tài khoản demo: `admin@phone.com`/`Admin@123` · `customer@phone.com`/`Customer@123`

## Kiến trúc backend (luồng phụ thuộc)
`API → Infrastructure → Application → Domain`. Domain là lõi, không tham chiếu ra ngoài.
- Business logic đặt ở `Application/Services`, truy cập DB qua `IAppDbContext`.
- DTO trả về client dùng `record` trong `Application/DTOs`. Không lộ entity trực tiếp.
- Lỗi nghiệp vụ ném `AppException` (middleware map sang HTTP status).
- Schema tạo bằng EF `EnsureCreated` khi khởi động (chưa dùng Migrations vì môi trường chưa có dotnet-ef).

## Nghiệp vụ đặc thù điện thoại (khác shop tổng hợp)
- **Thương hiệu (Brand)**: Apple, Samsung, Xiaomi, OPPO... lọc & báo cáo theo hãng.
- **Biến thể = Màu × Dung lượng** (`ProductVariant.Color` + `Storage`), mỗi tổ hợp có SKU/giá/giá vốn/tồn kho riêng. Admin sinh biến thể theo ma trận.
- **Thông số kỹ thuật** (`ProductSpecification`) gom nhóm (Màn hình/Chip/Camera/Pin...) để render bảng & so sánh máy.
- **IMEI/Serial** gán lúc giao (`OrderItem.Imei`) → sinh **phiếu bảo hành** (`WarrantyRecord`) tra cứu theo IMEI/mã đơn.
- **Trả góp** (`Order.InstallmentMonths/Monthly`, `PaymentMethod.Installment`) — mock, tính số tiền/tháng.
- **Thu cũ đổi mới** (`TradeInRequest`) — khách khai máy cũ, admin định giá thu.
- **Flash Sale, Coupon** tính giá server-side (không tin client).

## Quy ước chung
- Backend: PascalCase, C# convention. Frontend: camelCase, `<script setup lang="ts">`.
- Comment tiếng Việt ngắn gọn, giải thích "tại sao".

## 📋 Quản lý công việc — [`TODO.md`](TODO.md)
Backlog dài hạn nằm ở **[`TODO.md`](TODO.md)**. Khi người dùng nói "ghi todo / để sau / nhắc tôi" → thêm 1 dòng vào `TODO.md` đúng mẫu, KHÔNG làm luôn (trừ khi bảo làm ngay). Mẫu: `- [ ] (YYYY-MM-DD) \`[Khu vực]\` Mô tả — _lý do._`

## 🚀 Tự đẩy Git sau mỗi task
Sau khi hoàn thành & verify một task (build xanh) → tự `git add -A` → `git commit` (message tiếng Việt, mẫu `<khu vực>: <việc>`) → `git push origin main`. Kèm trailer đồng tác giả. Không push khi build đỏ. File `.env`/secret đã trong `.gitignore` — tuyệt đối không commit.
