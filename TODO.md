# PhoneStore — Backlog công việc

Sổ backlog dài hạn của dự án (bền qua nhiều phiên). Quy ước ghi chép ở cuối file.

## 🔥 Ưu tiên cao
_(trống)_

## 🔨 Đang làm
_(trống)_

## 🔜 Cần làm
- [ ] (2026-08-23) `[FE]` Khởi tạo Vue 3 + Vite + PrimeVue + Pinia + Router; layout khách & admin (tông xanh công nghệ).
- [ ] (2026-08-23) `[FE]` Trang khách: Home (Flash Sale), Danh sách (lọc hãng/giá/dung lượng), Chi tiết (chọn màu×dung lượng, thông số, trả góp), So sánh, Giỏ, Checkout, Đơn hàng, Tra cứu bảo hành, Wishlist, Tài khoản, Auth.
- [ ] (2026-08-23) `[FE]` Trang admin: Dashboard, Sản phẩm (sinh biến thể ma trận + thông số), Thương hiệu, Danh mục, Đơn (nhập IMEI khi giao), Coupon, Flash Sale, Vận chuyển, Bảo hành, Thu cũ, Báo cáo, Audit log.

## ✅ Đã xong
- [x] (2026-09-13) `[BE]` `[FE]` **Báo cáo nâng cao (Advanced Reports)** — port từ ShopViet, thích ứng nghiệp vụ điện thoại. BE: `AdvancedReportService`/`AdvancedReportsController` (endpoint `/reports/{promotion,reconciliation,profit,churn,rfm,market-basket,demand-forecast,processing-time,cancel-reasons,view-to-sale,search,funnel,cohort,peak-time,reviews,flash-sale-perf,...}`), biến thể Màu×Dung lượng (Storage), đối soát COD/VNPay/Trả góp. FE: `ReportsView` 5 tab + drill-down, 4 view con `views/admin/reports`, `advReportApi`. Bổ sung `TradeInView` còn thiếu để build FE xanh. _Verify: `docker compose build api` xanh · `npm run build` (vue-tsc+vite) xanh._
- [x] (2026-08-23) `[BE]` **Backend build + chạy được qua Docker** (verify: `docker compose build api` xanh, container lên, Swagger 200). Đủ tầng Application (45 file: DTOs record, Services, DI) + Infrastructure (JWT/PBKDF2, MinIO, Email Log/SMTP, DbSeeder điện thoại) + API (28 file: Program, 21 controller, middleware lỗi, SignalR hub, OrderExpiry). Seed 6 hãng · 4 danh mục · 14 điện thoại (biến thể Màu×Dung lượng, 6-8 thông số, trả góp 6/9/12 tháng, bảo hành 12 tháng, Flash Sale). Test OK: brands/products/detail/categories/login/warranty/flash-sale.
- [x] (2026-08-23) `[BE]` Khởi tạo solution 4 layer (Domain/Application/Infrastructure/API) + toàn bộ Domain entities (29 entity đặc thù điện thoại), IAppDbContext, AppDbContext (index/precision/soft-delete), docker-compose, docs dự án.

## 💡 Ý tưởng
- [ ] So sánh máy nâng cao (highlight khác biệt thông số).
- [ ] Gợi ý phụ kiện "mua kèm" theo máy.

---
## Quy ước ghi chép
- Mẫu dòng: `- [ ] (YYYY-MM-DD) \`[Khu vực]\` Mô tả — _lý do._`
- Khu vực: `[BE]` backend, `[FE]` frontend, `[UI]` giao diện, `[DevOps]`...
- Bắt đầu làm → chuyển xuống **🔨 Đang làm**. Xong (đã verify/build) → `[x]` + chuyển **✅ Đã xong**.
