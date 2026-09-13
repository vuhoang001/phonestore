# PhoneStore Frontend — Design System (bắt buộc đọc trước khi sửa UI)

Phong cách **MINIMAL PREMIUM (Apple-like)**: nhiều khoảng trắng, xám trung tính, viền hairline, bóng khuếch tán rất nhẹ, bo góc lớn, typography to & chặt chữ, **một** màu nhấn xanh Apple dùng **rất dè** (chỉ CTA/link/tiêu điểm). Header là thanh **kính mờ trắng** (frosted). Giá hiển thị màu **neutral** (near-black), đỏ CHỈ dành cho badge giảm giá. Mọi `.vue`/CSS phải tuân theo file này.

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
  --brand: #0071e3;         /* xanh Apple — màu nhấn duy nhất, dùng dè */
  --brand-dark: #0056b3;
  --brand-light: #3898ec;
  --brand-50: #f0f7ff;      /* nền nhạt */
  --brand-100: #dbeafe;
  --price: #1d1d1f;         /* giá: neutral tự tin (KHÔNG đỏ) */
  --sale: #e0402f;          /* đỏ CHỈ cho badge giảm giá */
  --star: #f5a623;          /* sao đánh giá */
  --ok: #1d8a4e;            /* còn hàng / hoàn tất */
  --bg: #f5f5f7;            /* nền trang (xám Apple) */
  --surface: #ffffff;
  --surface-2: #fbfbfd;
  --border: #e5e5ea;        /* hairline trung tính */
  --border-strong: #d2d2d7;
  --text: #1d1d1f;          /* near-black trung tính */
  --text-2: #4b4f58;
  --text-muted: #86868b;    /* xám Apple */
  --radius-sm: 10px; --radius: 14px; --radius-lg: 20px;
  --shadow-sm: 0 1px 2px rgba(0,0,0,.04);
  --shadow: 0 4px 16px rgba(0,0,0,.06);
  --shadow-hover: 0 14px 36px rgba(0,0,0,.10);
  --sp-1:4px; --sp-2:8px; --sp-3:12px; --sp-4:18px; --sp-5:24px; --sp-6:32px; --sp-8:48px;
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
