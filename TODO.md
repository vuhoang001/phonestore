# PhoneStore — Backlog công việc

Sổ backlog dài hạn của dự án (bền qua nhiều phiên). Quy ước ghi chép ở cuối file.

## 🔥 Ưu tiên cao
_(trống)_

## 🔨 Đang làm
- [ ] (2026-08-23) `[BE]` Dựng tầng Application/Infrastructure/API để backend build được qua Docker.

## 🔜 Cần làm
- [ ] (2026-08-23) `[BE]` DTOs (record) + Services nghiệp vụ: Auth, Product/Variant, Cart, Order, Payment, Coupon, Review, Wishlist, Warranty, TradeIn, FlashSale, Report, Notification.
- [ ] (2026-08-23) `[BE]` Controllers + Middleware lỗi + JWT + Seeder dữ liệu điện thoại (Brand/Category/Product mẫu).
- [ ] (2026-08-23) `[BE]` MinIO client + Upload ảnh; SignalR hub thông báo realtime.
- [ ] (2026-08-23) `[FE]` Khởi tạo Vue 3 + Vite + PrimeVue + Pinia + Router; layout khách & admin.
- [ ] (2026-08-23) `[FE]` Trang khách: Home (Flash Sale), Danh sách (lọc hãng/giá/dung lượng), Chi tiết (chọn màu×dung lượng, thông số, trả góp), So sánh, Giỏ, Checkout, Đơn hàng, Tra cứu bảo hành, Wishlist, Tài khoản, Auth.
- [ ] (2026-08-23) `[FE]` Trang admin: Dashboard, Sản phẩm (sinh biến thể ma trận + thông số), Thương hiệu, Danh mục, Đơn (nhập IMEI khi giao), Coupon, Flash Sale, Vận chuyển, Bảo hành, Thu cũ, Báo cáo, Audit log.

## ✅ Đã xong
- [x] (2026-08-23) `[BE]` Khởi tạo solution 4 layer (Domain/Application/Infrastructure/API) + toàn bộ Domain entities (28 entity đặc thù điện thoại), IAppDbContext, AppDbContext (index/precision/soft-delete), docker-compose, docs dự án.

## 💡 Ý tưởng
- [ ] So sánh máy nâng cao (highlight khác biệt thông số).
- [ ] Gợi ý phụ kiện "mua kèm" theo máy.

---
## Quy ước ghi chép
- Mẫu dòng: `- [ ] (YYYY-MM-DD) \`[Khu vực]\` Mô tả — _lý do._`
- Khu vực: `[BE]` backend, `[FE]` frontend, `[UI]` giao diện, `[DevOps]`...
- Bắt đầu làm → chuyển xuống **🔨 Đang làm**. Xong (đã verify/build) → `[x]` + chuyển **✅ Đã xong**.
