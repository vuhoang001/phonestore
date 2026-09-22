# PhoneStore — Backlog công việc

Sổ backlog dài hạn của dự án (bền qua nhiều phiên). Quy ước ghi chép ở cuối file.

## 🔥 Ưu tiên cao
_(trống)_

## 🔨 Đang làm
_(trống)_

## 🔜 Cần làm
_(trống)_

## ✅ Đã xong
- [x] (2026-09-22) `[FE]` **So sánh máy (Product Compare)** — store `compare` (localStorage, tối đa 4 máy), nút "So sánh" trên `ProductCard`, thanh nổi `CompareBar` (MainLayout), trang `/compare` (`CompareView`): bảng đối chiếu giá + thông tin chung + thông số gom nhóm, **tô sáng dòng khác biệt** + toggle "chỉ hiện điểm khác biệt". Link ở subbar. _Verify: `npm run build` (vue-tsc+vite) xanh._
- [x] (2026-09-22) `[FE]` **Quản lý Thu cũ đổi mới (admin)** — trang `/admin/trade-in` (`TradeInManageView`): xem toàn bộ yêu cầu, thẻ tổng theo trạng thái, lọc, **báo giá** (`quote`) qua dialog, duyệt **đồng ý/từ chối** (`updateStatus`). Menu admin + route. BE đã sẵn (`GET /trade-in`, `PUT /{id}/quote`, `PUT /{id}/status`). _Verify: build FE xanh._
- [x] (2026-08-23) `[FE]` Khởi tạo Vue 3 + Vite + PrimeVue + Pinia + Router; layout khách & admin (tông xanh công nghệ).
- [x] (2026-08-23) `[FE]` Trang khách: Home (Flash Sale), Danh sách (lọc hãng/giá/dung lượng), Chi tiết (màu×dung lượng, thông số, trả góp), Giỏ, Checkout, Đơn hàng, Tra cứu bảo hành, Wishlist, Tài khoản, Auth. _(So sánh máy bổ sung 2026-09-22.)_
- [x] (2026-08-23) `[FE]` Trang admin: Dashboard, Sản phẩm (sinh biến thể ma trận + thông số), Thương hiệu, Danh mục, Đơn (nhập IMEI khi giao), Coupon, Flash Sale, Vận chuyển, Báo cáo, Audit log. _(Quản lý Thu cũ bổ sung 2026-09-22.)_
- [x] (2026-09-13) `[BE]` `[FE]` **Trả góp hàng tháng thực tế** — lịch trả `InstallmentPayment` (kỳ/hạn/số tiền/trạng thái), sinh khi đặt đơn trả góp; `GetMyPlans` + `Pay` (trả TUẦN TỰ, trả hết → Payment=Paid, báo realtime); `GET/POST /api/installments`; trang `InstallmentsView` (tiến độ + nút thanh toán kỳ tới) + menu "Trả góp của tôi". Seeder sinh sẵn lịch Paid/Pending/Overdue. _Verify: mine 2 lịch, pay cập nhật tiến độ, trả sai thứ tự → 400._
- [x] (2026-09-13) `[UI]` `[FE]` **Redesign toàn app sang Minimal Premium (Apple-like)** — token/theme (main.css + theme.ts), chrome (header kính mờ + sidebar sáng), ProductCard, relayout HomeView (spotlight + two-up), ProductList (sidebar hairline). _Cổng :5174; build+deploy xanh._
- [x] (2026-09-13) `[BE]` `[FE]` **Báo cáo nâng cao (Advanced Reports)** — port từ ShopViet, thích ứng nghiệp vụ điện thoại. BE: `AdvancedReportService`/`AdvancedReportsController` (endpoint `/reports/{promotion,reconciliation,profit,churn,rfm,market-basket,demand-forecast,processing-time,cancel-reasons,view-to-sale,search,funnel,cohort,peak-time,reviews,flash-sale-perf,...}`), biến thể Màu×Dung lượng (Storage), đối soát COD/VNPay/Trả góp. FE: `ReportsView` 5 tab + drill-down, 4 view con `views/admin/reports`, `advReportApi`. Bổ sung `TradeInView` còn thiếu để build FE xanh. _Verify: `docker compose build api` xanh · `npm run build` (vue-tsc+vite) xanh._
- [x] (2026-08-23) `[BE]` **Backend build + chạy được qua Docker** (verify: `docker compose build api` xanh, container lên, Swagger 200). Đủ tầng Application (45 file: DTOs record, Services, DI) + Infrastructure (JWT/PBKDF2, MinIO, Email Log/SMTP, DbSeeder điện thoại) + API (28 file: Program, 21 controller, middleware lỗi, SignalR hub, OrderExpiry). Seed 6 hãng · 4 danh mục · 14 điện thoại (biến thể Màu×Dung lượng, 6-8 thông số, trả góp 6/9/12 tháng, bảo hành 12 tháng, Flash Sale). Test OK: brands/products/detail/categories/login/warranty/flash-sale.
- [x] (2026-08-23) `[BE]` Khởi tạo solution 4 layer (Domain/Application/Infrastructure/API) + toàn bộ Domain entities (29 entity đặc thù điện thoại), IAppDbContext, AppDbContext (index/precision/soft-delete), docker-compose, docs dự án.

## 💡 Ý tưởng
- [ ] Gợi ý phụ kiện "mua kèm" theo máy.
- [ ] `[FE]` Quản lý phiếu bảo hành ở admin (hiện chỉ tra cứu phía khách + gán IMEI khi giao) — cần thêm endpoint list-all bảo hành ở BE.

---
## Quy ước ghi chép
- Mẫu dòng: `- [ ] (YYYY-MM-DD) \`[Khu vực]\` Mô tả — _lý do._`
- Khu vực: `[BE]` backend, `[FE]` frontend, `[UI]` giao diện, `[DevOps]`...
- Bắt đầu làm → chuyển xuống **🔨 Đang làm**. Xong (đã verify/build) → `[x]` + chuyển **✅ Đã xong**.
