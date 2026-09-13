# FRONTEND PORTING SPEC — ShopViet(hai) → PhoneStore

Port frontend Vue 3 từ `hai` sang `phonestore`, đổi nghiệp vụ sang **điện thoại**, tông màu **xanh công nghệ**.

## Đường dẫn
- Nguồn: `/home/hoanggggf/Documents/do-an/hai/frontend/`
- Đích: `/home/hoanggggf/Documents/do-an/phonestore/frontend/`
- Design system BẮT BUỘC theo: `/home/hoanggggf/Documents/do-an/phonestore/frontend/CLAUDE.md` (tông xanh `--brand:#1e6fff`).

## Quy tắc chung
- Stack giữ nguyên: Vue 3.5 + TS + Vite + PrimeVue 4 + Pinia + Vue Router + Axios + Chart.js + SignalR. `package.json` copy y hệt hai (chỉ đổi field `name` → `phonestore-frontend`).
- Copy nguyên `vite.config.ts`, `tsconfig.json`, `env.d.ts`, `nginx.conf`, `Dockerfile`, `index.html` (đổi `<title>` → "PhoneStore — Điện thoại chính hãng"; giữ link font).
- `<script setup lang="ts">`, camelCase. Comment tiếng Việt.
- API base giữ `/api` (Axios proxy như hai). Backend chạy cổng 8080.

## ĐỔI THEME sang xanh công nghệ (QUAN TRỌNG)
- `src/theme.ts`: đổi preset màu PrimeVue từ cam sang **xanh `#1e6fff`** (primary palette 50→900 theo sắc xanh). Giữ cấu trúc file như hai.
- `src/assets/main.css`: đổi các design token `:root` sang bảng token trong `frontend/CLAUDE.md` (--brand #1e6fff, --brand-dark, --brand-50 #eff5ff, --price #e8453c, --star #ffb400, radius 12px, ...). Giữ mọi class tiện ích/layout khác của hai (grid-products, surface-card, empty, responsive breakpoint) nhưng theo token mới.
- KHÔNG còn màu cam `#ee4d2d` ở bất cứ đâu.

## ĐỔI API CONTRACT (services + types) — điểm mấu chốt để không vỡ
Đọc `src/services/index.ts`, `src/services/api.ts`, `src/types/index.ts` của hai rồi CHỈNH:
1. **Product detail theo slug**: endpoint là `GET /api/products/slug/{slug}` (KHÁC hai dùng `/products/{slug}`). Sửa hàm gọi tương ứng.
2. **Brands (MỚI)**: thêm service `brandsApi`: `list()` → `GET /api/brands`; admin `create/update/remove` → `POST/PUT/DELETE /api/brands`. Type `Brand { id, name, slug, logoUrl?, description? }`.
3. **ProductVariant**: field `size` → `storage`; thêm `colorHex`, `cost`. Type & mọi nơi dùng.
4. **Product**: thêm `brandId`, `brandName`, `warrantyMonths`, `installmentAvailable`, `specifications: {group,name,value}[]`, `installmentOptions: {months,monthly}[]`. BỎ `attributes`.
5. **ProductFilter**: thêm `brandId?` (lọc theo hãng) — list gọi `GET /api/products?brandId=&...`.
6. **Order**: type thêm `installmentMonths?`, `installmentMonthly?`; `OrderItem` thêm `imei?`. `CreateOrder` thêm `installmentMonths?`.
7. **Payment**: chỉ còn `Cod`, `VnPay`, `Installment` (BỎ Momo/Stripe) ở mọi chỗ chọn phương thức.
8. **Warranty (MỚI)**: service `warrantyApi.lookup(imei?, orderCode?)` → `GET /api/warranty/lookup?imei=&orderCode=`. Admin `assignImei(orderId, {orderItemId, imei})`.
9. **TradeIn (MỚI)**: service `tradeInApi`: khách `create(dto)`, `mine()`; admin `all()`, `quote(id,{quotedPrice})`, `updateStatus(id,status)`.
10. **Order update status (admin)**: body `UpdateOrderStatusDto` có thêm `imeiAssignments: {orderItemId,imei}[]` — khi chuyển sang "Đang giao" (Shipping) cho nhập IMEI từng dòng.
11. BỎ mọi tham chiếu tới AdvancedReports ở services/types (báo cáo nâng cao lược bỏ ở BE lần này). Giữ reports cơ bản.

Nếu tên field/DTO thực tế khác, KIỂM CHỨNG bằng cách đọc file backend đã build: `/home/hoanggggf/Documents/do-an/phonestore/backend/src/PhoneStore.Application/DTOs/*.cs`.

## VIEW cần đổi nội dung điện thoại
- **ProductListView**: bộ lọc thêm **Thương hiệu** (brandId) + **Dung lượng**; sort giữ nguyên. Card giữ badge giảm %, "Trả góp" nếu installmentAvailable.
- **ProductDetailView**: chọn **Màu** (swatch dùng colorHex) × **Dung lượng** (chip) để ra variant; **bảng Thông số kỹ thuật** gom theo group; khối **Trả góp** (liệt kê installmentOptions); nút thêm giỏ theo variant đã chọn.
- **HomeView**: Flash Sale đếm ngược + lưới máy nổi bật + dải thương hiệu (logo brands).
- **CheckoutView**: thêm chọn "Trả góp" (6/9/12 tháng) khi giỏ có máy hỗ trợ; hiển thị số tiền/tháng.
- **MỚI `WarrantyLookupView`**: ô nhập IMEI hoặc mã đơn → gọi warrantyApi.lookup → hiển thị danh sách phiếu bảo hành (còn hạn/hết hạn). Route `/warranty` (public, có trong menu).
- **MỚI `TradeInView`** (tùy chọn, nếu kịp): form thu cũ đổi mới. Nếu không kịp, tạo trang placeholder gọn.
- Admin **ProductManageView**: form thêm **BrandId**, **WarrantyMonths**, **InstallmentAvailable**, bảng **Thông số** (group/name/value), khu **sinh biến thể Màu×Dung lượng** (nhập list màu + list dung lượng → sinh dòng biến thể có giá/tồn/SKU).
- Admin **MỚI `BrandManageView`**: CRUD thương hiệu. Thêm route + menu admin.
- Admin **OrderManageView**: khi đổi trạng thái sang "Đang giao", cho nhập **IMEI** từng dòng hàng (gửi imeiAssignments).
- Admin ReportsView: giữ các tab báo cáo CƠ BẢN (bỏ tab/ά phần advanced không có API).

## Router & menu
- Thêm route: `/warranty` (WarrantyLookupView), `/trade-in` (nếu làm), admin `/admin/brands` (BrandManageView).
- Bỏ route nào trỏ tới tính năng đã cắt (advanced reports nếu là route riêng).
- Menu khách: thêm "Tra cứu bảo hành". Menu admin: thêm "Thương hiệu".

## Verify
- Máy có Node. Sau khi port: `cd /home/hoanggggf/Documents/do-an/phonestore/frontend && npm install && npm run build` phải XANH (vue-tsc + vite build). Sửa hết lỗi type. KHÔNG chạy docker.
