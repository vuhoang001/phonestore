import { definePreset } from '@primevue/themes'
import Aura from '@primevue/themes/aura'

// Phong cách MINIMAL PREMIUM (Apple-like): màu nhấn xanh Apple (#0071e3) dùng dè,
// bo góc lớn, nút bo tròn dạng viên (pill), bóng khuếch tán rất nhẹ.
// "single source of truth" cho màu primary — đồng bộ với --brand (#0071e3) trong main.css.
const PhonePreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '#f0f7ff',
      100: '#dbeafe',
      200: '#bcd8fb',
      300: '#7cbcf7',
      400: '#3898ec',
      500: '#0071e3', // màu nhấn chính (xanh Apple)
      600: '#0056b3',
      700: '#00458f',
      800: '#013a75',
      900: '#0a325f',
      950: '#07213f'
    },
    // Bo góc LỚN hơn cho cảm giác mềm, cao cấp.
    borderRadius: { none: '0', xs: '6px', sm: '10px', md: '12px', lg: '16px', xl: '20px' },
    // Ô nhập airy hơn (nhiều khoảng thở) — rule toàn cục cho mọi input/select/number.
    formField: { paddingX: '0.8rem', paddingY: '0.5rem', fontSize: '0.9rem', sm: { fontSize: '0.85rem', paddingY: '0.4rem' } },
    // Overlay (Dialog/ConfirmDialog/Popover): bo góc lớn + bóng khuếch tán nhẹ + padding rộng.
    overlay: {
      modal: { borderRadius: '20px', padding: '1.5rem 1.75rem', shadow: '0 30px 70px rgba(0, 0, 0, 0.16)' },
      popover: { borderRadius: '16px', shadow: '0 14px 36px rgba(0, 0, 0, 0.12)' }
    },
    colorScheme: {
      light: {
        primary: {
          color: '#0071e3',
          contrastColor: '#ffffff',
          hoverColor: '#0056b3',
          activeColor: '#00458f'
        },
        formField: { borderRadius: '12px' },
        content: { borderRadius: '16px' },
        // Nền mờ sau dialog: trung tính, tối vừa đủ (kết hợp blur ở main.css).
        mask: { background: 'rgba(0, 0, 0, 0.4)' }
      }
    }
  },
  components: {
    button: {
      // Nút viên (pill) kiểu Apple — dấu hiệu premium; padding rộng, chữ đậm.
      root: { paddingX: '1.15rem', paddingY: '0.5rem', gap: '0.45rem', borderRadius: '980px', label: { fontWeight: '600' } },
      root_sm: { paddingX: '0.85rem', paddingY: '0.35rem', fontSize: '0.82rem' }
    },
    dialog: {
      // Tiêu đề đậm & to cho phân cấp rõ; nội dung padding rộng.
      title: { fontSize: '1.35rem', fontWeight: '700' },
      content: { padding: '1.5rem 1.75rem' }
    }
  }
})

export default PhonePreset
