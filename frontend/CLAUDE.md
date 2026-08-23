# PhoneStore Frontend — Design System (bắt buộc đọc trước khi sửa UI)

Tông thương hiệu **xanh công nghệ** (tech-blue) cho cửa hàng điện thoại — hiện đại, sạch, cao cấp theo tinh thần Vercel/v0. Mọi `.vue`/CSS phải tuân theo file này.

## Nguyên tắc vàng
1. **Luôn dùng design token** (CSS variables) — KHÔNG hardcode màu/padding/radius/shadow.
2. **Một màu nhấn duy nhất** (xanh `--brand`), không phối 3–4 màu sặc sỡ.
3. **Card bo góc ~12px + đổ bóng nhẹ**, hover nhấc nhẹ (`translateY(-2px)`).
4. **Phân cấp bằng typography + khoảng trắng**, không bằng nhiều màu.
5. Ưu tiên **component PrimeVue 4** có sẵn (Button, DataTable, Dialog, Tag, Rating, Skeleton...).
6. Trạng thái đầy đủ: loading (Skeleton đúng hình), empty state, hover, focus, disabled, error.
7. Font **Be Vietnam Pro** hoặc **Inter** (load ở index.html).

## Color tokens (đặt trong `:root`)
```css
:root {
  --brand: #1e6fff;         /* xanh công nghệ chủ đạo */
  --brand-dark: #1657cc;
  --brand-light: #4d8bff;
  --brand-50: #eff5ff;      /* nền nhạt */
  --brand-100: #dbe8ff;
  --price: #e8453c;         /* giá/sale màu đỏ nổi bật */
  --sale: #e8453c;
  --star: #ffb400;          /* sao đánh giá */
  --ok: #16a34a;            /* còn hàng / hoàn tất */
  --bg: #f4f6fb;            /* nền trang */
  --surface: #ffffff;
  --surface-2: #fafbfe;
  --border: #e8ebf1;
  --border-strong: #d5dae3;
  --text: #1a2233;
  --text-2: #55607a;
  --text-muted: #929bb0;
  --radius-sm: 8px; --radius: 12px; --radius-lg: 16px;
  --shadow-sm: 0 1px 2px rgba(20,30,60,.06);
  --shadow: 0 2px 10px rgba(20,30,60,.08);
  --shadow-hover: 0 8px 24px rgba(20,30,60,.12);
  --sp-1:4px; --sp-2:8px; --sp-3:12px; --sp-4:16px; --sp-5:20px; --sp-6:24px; --sp-8:32px;
}
```

## Quy ước component
- Nút chính: `<Button>` mặc định (xanh brand). Nút phụ: `outlined`/`text`.
- **Severity** Tag/Button: success=hoàn tất, warn=chờ, danger=hủy, info=đang xử lý, secondary=phụ.
- Card sản phẩm: luôn dùng `ProductCard.vue`. Grid: `.grid-products` (auto-fill, min 200px).
- Ảnh sản phẩm: `aspect-ratio: 1` + `object-fit: contain` (điện thoại nền trắng), có fallback.

## Điểm nhấn ngành điện thoại (nên có)
- Badge **"Giảm %"**, giá gạch ngang, **"Trả góp 0%"**, **"Chính hãng"**, chip **"5G"**, **"Đã bán N"**.
- Swatch **màu máy** (dùng `ColorHex`) + nút chọn **dung lượng** ở trang chi tiết.
- Bảng **Thông số kỹ thuật** gom nhóm; nút **So sánh** máy.
- Flash Sale: đồng hồ **đếm ngược** + thanh "đã bán".

## Responsive breakpoint
| Mốc | max-width | Ghi chú |
|---|---|---|
| md | 900px | 2 cột → 1 cột |
| sm | 640px | grid 2 cột; dialog 95vw |
| xs | 420px | grid 1 cột; ẩn text logo |

## Chống "AI-look"
- KHÔNG theme tím Aura mặc định (phải xanh brand). KHÔNG gradient tím-hồng.
- KHÔNG card phẳng đều tăm tắp (dùng shadow + hover nhấc).
- KHÔNG spacing tùy tiện (dùng token `--sp-*`). KHÔNG chữ đen tuyền (dùng `--text`).
- CÓ micro-interaction, CÓ điểm nhấn TMĐT, ảnh căn chỉnh quang học.
