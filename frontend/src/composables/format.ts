/** Định dạng tiền tệ VND. */
export function formatCurrency(value: number): string {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value ?? 0)
}

/** Định dạng ngày giờ theo locale VN. */
export function formatDate(value: string | Date): string {
  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit'
  }).format(new Date(value))
}

/** Gộp địa chỉ thành một dòng, bỏ qua phần rỗng (vd: quận/huyện ở hệ 2 cấp). */
export function formatAddressLine(a: {
  detail: string; ward: string; district?: string; province: string; note?: string
}): string {
  const line = [a.detail, a.ward, a.district, a.province].filter(Boolean).join(', ')
  // Ghi chú giao hàng (nếu có) hiển thị cuối dòng
  return a.note?.trim() ? `${line} · Ghi chú: ${a.note.trim()}` : line
}

/** Nhãn tiếng Việt cho trạng thái đơn hàng. */
export const orderStatusLabel: Record<string, string> = {
  Pending: 'Chờ xác nhận',
  Confirmed: 'Đã xác nhận',
  Shipping: 'Đang giao',
  Completed: 'Hoàn tất',
  Cancelled: 'Đã hủy'
}

/** Màu severity của PrimeVue Tag theo trạng thái. */
export const orderStatusSeverity: Record<string, string> = {
  Pending: 'warn',
  Confirmed: 'info',
  Shipping: 'info',
  Completed: 'success',
  Cancelled: 'danger'
}

/** Nhãn tiếng Việt cho phương thức thanh toán (chỉ 3 phương thức: COD / VNPAY / Trả góp). */
export const paymentMethodLabel: Record<string, string> = {
  Cod: 'Thanh toán khi nhận hàng (COD)',
  VnPay: 'Thanh toán chuyển khoản (VNPAY)',
  Installment: 'Mua trả góp'
}

/** Icon + màu cho từng phương thức (dùng cho biểu đồ/khối doanh thu cho sinh động). */
export const paymentMethodStyle: Record<string, { icon: string; color: string }> = {
  Cod: { icon: 'pi-money-bill', color: '#16a34a' },
  VnPay: { icon: 'pi-qrcode', color: '#1e6fff' },
  Installment: { icon: 'pi-calendar', color: '#7c5cfc' }
}
export const pmStyle = (m: string) => paymentMethodStyle[m] || { icon: 'pi-credit-card', color: '#1e6fff' }

/** Nhãn tiếng Việt cho trạng thái thanh toán. */
export const paymentStatusLabel: Record<string, string> = {
  Pending: 'Chờ thanh toán',
  Paid: 'Đã thanh toán',
  Failed: 'Thanh toán thất bại',
  Refunded: 'Đã hoàn tiền'
}

/** Màu severity cho trạng thái thanh toán. */
export const paymentStatusSeverity: Record<string, string> = {
  Pending: 'warn',
  Paid: 'success',
  Failed: 'danger',
  Refunded: 'info'
}

/** Nhãn ngắn cho phương thức thanh toán (dùng ở chỗ hẹp). */
export const paymentMethodShort: Record<string, string> = {
  Cod: 'COD', VnPay: 'VNPAY', Installment: 'Trả góp'
}
