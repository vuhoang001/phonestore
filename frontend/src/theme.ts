import { definePreset } from '@primevue/themes'
import Aura from '@primevue/themes/aura'

// Bảng màu thương hiệu: XANH công nghệ (tech-blue) cho cửa hàng điện thoại.
// Đây là "single source of truth" cho màu primary — đồng bộ với --brand (#1e6fff) trong main.css.
const PhonePreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '#eff5ff',
      100: '#dbe8ff',
      200: '#bcd4ff',
      300: '#8fb6ff',
      400: '#4d8bff',
      500: '#1e6fff', // màu thương hiệu chính (xanh công nghệ)
      600: '#1657cc',
      700: '#1247a6',
      800: '#123c85',
      900: '#13366b',
      950: '#0d2044'
    },
    // Bo góc mềm mại (medium radius) theo lựa chọn thiết kế.
    borderRadius: { none: '0', xs: '4px', sm: '8px', md: '10px', lg: '12px', xl: '16px' },
    // Ô nhập GỌN, mật độ cao (dày dặn vừa phải) — rule toàn cục cho mọi input/select/number.
    formField: { paddingX: '0.625rem', paddingY: '0.375rem', fontSize: '0.875rem', sm: { fontSize: '0.8rem', paddingY: '0.3rem' } },
    // Overlay (Dialog/ConfirmDialog/Popover): bo góc mềm + bóng sâu tinh tế + padding rộng rãi.
    overlay: {
      modal: { borderRadius: '16px', padding: '1.25rem 1.5rem', shadow: '0 24px 64px rgba(20, 30, 60, 0.18)' },
      popover: { borderRadius: '12px', shadow: '0 12px 32px rgba(20, 30, 60, 0.12)' }
    },
    colorScheme: {
      light: {
        primary: {
          color: '#1e6fff',
          contrastColor: '#ffffff',
          hoverColor: '#1657cc',
          activeColor: '#1247a6'
        },
        formField: { borderRadius: '8px' },
        content: { borderRadius: '12px' },
        // Nền mờ sau dialog: tối vừa đủ để nội dung nổi bật (kết hợp blur ở main.css).
        mask: { background: 'rgba(20, 30, 60, 0.45)' }
      }
    }
  },
  components: {
    button: {
      // Nút nhỏ gọn hơn, chữ 14px, bo góc vừa.
      root: { paddingX: '0.85rem', paddingY: '0.45rem', gap: '0.4rem', borderRadius: '10px', label: { fontWeight: '600' } },
      root_sm: { paddingX: '0.65rem', paddingY: '0.3rem', fontSize: '0.8rem' }
    },
    dialog: {
      // Tiêu đề đậm & to hơn cho phân cấp rõ; nội dung có padding trên để không dính divider header.
      title: { fontSize: '1.2rem', fontWeight: '700' },
      content: { padding: '1.25rem 1.5rem' }
    }
  }
})

export default PhonePreset
