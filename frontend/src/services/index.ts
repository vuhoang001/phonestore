import api from './api'
import type {
  AuthResponse, User, Category, Brand, ProductDetail, ProductListItem, PagedResult,
  Cart, Address, Order, Review, Coupon, WishlistItem, ShippingMethod, DashboardStats,
  NotificationList, FlashSale, Warranty, WarrantyLookupResult, TradeIn
} from '@/types'
import type {
  ReportSummary, RevenueReport, TopProductReport, CategoryRevenue,
  PaymentMethodRevenue, OrderStats, CountPoint, InventoryReport
} from '@/types'
import type {
  PromotionReport, Reconciliation, ReconOrder, ProfitReport, ProfitOrder,
  SummaryOrder, NewCustomer, InventoryItem, ChurnReport, ChurnCustomer, ProductPair, RfmReport,
  DemandItem, ProcessingTime, ProcessingOrder, CancelReason, ViewToSale, SearchReport, FunnelReport,
  CohortReport, PeakTimeReport, ReviewReport, FlashSaleReport
} from '@/types'

export interface ReportRange { from?: string; to?: string }

// ---------- Auth ----------
export const authApi = {
  register: (data: { email: string; password: string; fullName: string; phone?: string }) =>
    api.post<AuthResponse>('/auth/register', data).then((r) => r.data),
  login: (data: { email: string; password: string }) =>
    api.post<AuthResponse>('/auth/login', data).then((r) => r.data),
  me: () => api.get<User>('/auth/me').then((r) => r.data),
  updateProfile: (data: { fullName: string; phone?: string; avatarUrl?: string }) =>
    api.put<User>('/auth/profile', data).then((r) => r.data),
  changePassword: (data: { currentPassword: string; newPassword: string }) =>
    api.post('/auth/change-password', data),
  refresh: (refreshToken: string) =>
    api.post<AuthResponse>('/auth/refresh', { refreshToken }).then((r) => r.data),
  logout: (refreshToken: string) => api.post('/auth/logout', { refreshToken }),
  forgotPassword: (email: string) => api.post('/auth/forgot-password', { email }),
  resetPassword: (token: string, newPassword: string) =>
    api.post('/auth/reset-password', { token, newPassword }),
  confirmEmail: (token: string) => api.get('/auth/confirm-email', { params: { token } })
}

// ---------- Catalog ----------
export const categoryApi = {
  tree: () => api.get<Category[]>('/categories/tree').then((r) => r.data),
  all: () => api.get<Category[]>('/categories').then((r) => r.data),
  create: (data: { name: string; parentId?: number; imageUrl?: string }) =>
    api.post<Category>('/categories', data).then((r) => r.data),
  update: (id: number, data: { name: string; parentId?: number; imageUrl?: string }) =>
    api.put<Category>(`/categories/${id}`, data).then((r) => r.data),
  remove: (id: number) => api.delete(`/categories/${id}`)
}

// ---------- Thương hiệu (điện thoại) ----------
export const brandsApi = {
  // Danh sách hãng để hiển thị/lọc phía client (công khai).
  list: () => api.get<Brand[]>('/brands').then((r) => r.data),
  // Admin CRUD — backend nhận BrandDto (id, name, slug, logoUrl?, description?).
  create: (data: Partial<Brand>) => api.post<Brand>('/brands', data).then((r) => r.data),
  update: (id: number, data: Partial<Brand>) => api.put<Brand>(`/brands/${id}`, data).then((r) => r.data),
  remove: (id: number) => api.delete(`/brands/${id}`)
}

export interface ProductFilter {
  keyword?: string
  categoryId?: number
  brandId?: number       // Lọc theo hãng điện thoại
  minPrice?: number
  maxPrice?: number
  minRating?: number
  sortBy?: string
  page?: number
  pageSize?: number
}

export const productApi = {
  search: (filter: ProductFilter) =>
    api.get<PagedResult<ProductListItem>>('/products', { params: filter }).then((r) => r.data),
  // Product detail theo slug: GET /api/products/slug/{slug}
  bySlug: (slug: string) => api.get<ProductDetail>(`/products/slug/${slug}`).then((r) => r.data),
  byId: (id: number) => api.get<ProductDetail>(`/products/${id}`).then((r) => r.data),
  create: (data: any) => api.post<ProductDetail>('/products', data).then((r) => r.data),
  update: (id: number, data: any) => api.put<ProductDetail>(`/products/${id}`, data).then((r) => r.data),
  remove: (id: number) => api.delete(`/products/${id}`),
  reviews: (productId: number) => api.get<Review[]>(`/products/${productId}/reviews`).then((r) => r.data),
  related: (productId: number) => api.get<ProductListItem[]>(`/products/${productId}/related`).then((r) => r.data)
}

// ---------- Cart ----------
export const cartApi = {
  get: () => api.get<Cart>('/cart').then((r) => r.data),
  add: (variantId: number, quantity: number) =>
    api.post<Cart>('/cart/items', { variantId, quantity }).then((r) => r.data),
  update: (itemId: number, quantity: number) =>
    api.put<Cart>(`/cart/items/${itemId}`, { quantity }).then((r) => r.data),
  remove: (itemId: number) => api.delete<Cart>(`/cart/items/${itemId}`).then((r) => r.data),
  clear: () => api.delete('/cart'),
  // Gộp giỏ khách vào giỏ user sau khi đăng nhập
  merge: (guestToken: string) =>
    api.post<Cart>('/cart/merge', null, { params: { guestToken } }).then((r) => r.data)
}

// ---------- Address ----------
export const addressApi = {
  mine: () => api.get<Address[]>('/addresses').then((r) => r.data),
  create: (data: Partial<Address>) => api.post<Address>('/addresses', data).then((r) => r.data),
  update: (id: number, data: Partial<Address>) => api.put<Address>(`/addresses/${id}`, data).then((r) => r.data),
  remove: (id: number) => api.delete(`/addresses/${id}`)
}

// ---------- Order ----------
export const orderApi = {
  // CreateOrder có thêm installmentMonths (6/9/12) khi mua trả góp.
  create: (data: {
    addressId: number; shippingMethodId?: number; paymentMethod: string
    couponCode?: string; note?: string; cartItemIds?: number[]; installmentMonths?: number
  }) => api.post<Order>('/orders', data).then((r) => r.data),
  my: (page = 1, pageSize = 10, status?: string) =>
    api.get<PagedResult<Order>>('/orders/my', { params: { page, pageSize, status } }).then((r) => r.data),
  byId: (id: number) => api.get<Order>(`/orders/${id}`).then((r) => r.data),
  cancel: (id: number, reason?: string) =>
    api.put<Order>(`/orders/${id}/cancel`, null, { params: { reason } }).then((r) => r.data),
  all: (page = 1, pageSize = 20, status?: string, extra?: { from?: string; to?: string; keyword?: string }) =>
    api.get<PagedResult<Order>>('/orders', { params: { page, pageSize, status, ...extra } }).then((r) => r.data),
  // Đổi trạng thái đơn (admin). Khi chuyển sang Shipping, gửi imeiAssignments để tạo phiếu bảo hành.
  updateStatus: (
    id: number,
    status: string,
    note?: string,
    imeiAssignments?: { orderItemId: number; imei: string }[]
  ) => api.put<Order>(`/orders/${id}/status`, { status, note, imeiAssignments: imeiAssignments ?? [] }).then((r) => r.data)
}

// ---------- Payment (chỉ Cod / VnPay / Installment) ----------
export interface PaymentResult {
  success: boolean
  orderId?: number
  orderCode: string
  responseCode: string
  message: string
  redirectUrl: string
}

export const paymentApi = {
  // Tạo URL thanh toán VNPAY cho đơn hàng, client tự redirect sang cổng (hoặc trang giả lập).
  vnpay: (orderId: number) =>
    api.post<{ paymentUrl: string }>(`/payments/vnpay/${orderId}`).then((r) => r.data),
  // Chốt kết quả ở chế độ giả lập (khi chưa cấu hình credential VNPAY thật).
  mockComplete: (orderId: number, success: boolean) =>
    api.post<PaymentResult>(`/payments/vnpay/mock/${orderId}`, null, { params: { success } }).then((r) => r.data)
}

// ---------- Warranty (bảo hành theo IMEI — đặc thù điện thoại) ----------
export const warrantyApi = {
  // Tra cứu công khai theo IMEI hoặc mã đơn.
  lookup: (imei?: string, orderCode?: string) =>
    api.get<WarrantyLookupResult>('/warranty/lookup', { params: { imei, orderCode } }).then((r) => r.data),
  // Admin: gán IMEI cho một dòng hàng của đơn (tạo phiếu bảo hành).
  assignImei: (orderId: number, data: { orderItemId: number; imei: string }) =>
    api.post(`/warranty/orders/${orderId}/assign-imei`, data),
  // Admin: danh sách phiếu bảo hành của một đơn.
  byOrder: (orderId: number) =>
    api.get<Warranty[]>(`/warranty/orders/${orderId}`).then((r) => r.data)
}

// ---------- Trade-in (thu cũ đổi mới) ----------
export const tradeInApi = {
  // Khách tạo yêu cầu.
  create: (data: { oldDeviceModel: string; condition: string; note?: string; targetProductId?: number }) =>
    api.post<TradeIn>('/trade-in', data).then((r) => r.data),
  // Khách xem yêu cầu của mình.
  mine: () => api.get<TradeIn[]>('/trade-in/mine').then((r) => r.data),
  // Admin xem tất cả yêu cầu.
  all: () => api.get<TradeIn[]>('/trade-in').then((r) => r.data),
  // Admin báo giá thu.
  quote: (id: number, data: { quotedPrice: number }) =>
    api.put<TradeIn>(`/trade-in/${id}/quote`, data).then((r) => r.data),
  // Admin đổi trạng thái (status truyền qua query).
  updateStatus: (id: number, status: string) =>
    api.put<TradeIn>(`/trade-in/${id}/status`, null, { params: { status } }).then((r) => r.data)
}

// ---------- Review ----------
export const reviewApi = {
  create: (data: { productId: number; rating: number; comment?: string; images?: string[] }) =>
    api.post<Review>('/reviews', data).then((r) => r.data),
  remove: (id: number) => api.delete(`/reviews/${id}`),
  // Kiểm tra người dùng có đủ điều kiện đánh giá (đã mua & chưa đánh giá) không
  canReview: (productId: number) =>
    api.get<{ canReview: boolean; reason: string }>(`/reviews/can-review/${productId}`).then((r) => r.data)
}

// ---------- Wishlist ----------
export const wishlistApi = {
  mine: () => api.get<WishlistItem[]>('/wishlist').then((r) => r.data),
  toggle: (productId: number) => api.post(`/wishlist/toggle/${productId}`)
}

// ---------- Coupon ----------
export const couponApi = {
  all: () => api.get<Coupon[]>('/coupons').then((r) => r.data),
  available: () => api.get<Coupon[]>('/coupons/available').then((r) => r.data),
  create: (data: any) => api.post<Coupon>('/coupons', data).then((r) => r.data),
  remove: (id: number) => api.delete(`/coupons/${id}`),
  validate: (code: string, orderAmount: number) =>
    api.get<Coupon>('/coupons/validate', { params: { code, orderAmount } }).then((r) => r.data)
}

// ---------- Flash Sale ----------
export const flashSaleApi = {
  active: () => api.get<FlashSale | null>('/flash-sale/active').then((r) => r.data),
  all: () => api.get<FlashSale[]>('/flash-sale').then((r) => r.data),
  create: (data: any) => api.post<FlashSale>('/flash-sale', data).then((r) => r.data),
  update: (id: number, data: any) => api.put<FlashSale>(`/flash-sale/${id}`, data).then((r) => r.data),
  remove: (id: number) => api.delete(`/flash-sale/${id}`)
}

// ---------- Shipping ----------
export const shippingApi = {
  methods: () => api.get<ShippingMethod[]>('/shipping/methods').then((r) => r.data),
  // Admin
  all: () => api.get<ShippingMethod[]>('/shipping').then((r) => r.data),
  create: (data: Partial<ShippingMethod>) => api.post<ShippingMethod>('/shipping', data).then((r) => r.data),
  update: (id: number, data: Partial<ShippingMethod>) => api.put<ShippingMethod>(`/shipping/${id}`, data).then((r) => r.data),
  remove: (id: number) => api.delete(`/shipping/${id}`)
}

// ---------- Uploads (ảnh) ----------
export const uploadApi = {
  image: (file: File) => api.postForm<{ url: string }>('/uploads/image', { file }).then((r) => r.data),
  reviewImage: (file: File) => api.postForm<{ url: string }>('/uploads/review-image', { file }).then((r) => r.data),
  avatar: (file: File) => api.postForm<{ url: string }>('/uploads/avatar', { file }).then((r) => r.data)
}

// ---------- Reports (admin analytics — chỉ báo cáo cơ bản) ----------
export const reportApi = {
  summary: (p: ReportRange) => api.get<ReportSummary>('/reports/summary', { params: p }).then((r) => r.data),
  revenue: (p: ReportRange & { groupBy?: string }) => api.get<RevenueReport>('/reports/revenue', { params: p }).then((r) => r.data),
  topProducts: (p: ReportRange & { limit?: number }) => api.get<TopProductReport[]>('/reports/top-products', { params: p }).then((r) => r.data),
  byCategory: (p: ReportRange) => api.get<CategoryRevenue[]>('/reports/revenue-by-category', { params: p }).then((r) => r.data),
  byPayment: (p: ReportRange) => api.get<PaymentMethodRevenue[]>('/reports/revenue-by-payment', { params: p }).then((r) => r.data),
  orderStats: (p: ReportRange) => api.get<OrderStats>('/reports/order-stats', { params: p }).then((r) => r.data),
  newCustomers: (p: ReportRange & { groupBy?: string }) => api.get<CountPoint[]>('/reports/new-customers', { params: p }).then((r) => r.data),
  inventory: () => api.get<InventoryReport>('/reports/inventory').then((r) => r.data)
}

// ---------- Reports (báo cáo nâng cao: tài chính, khách hàng, vận hành, hành vi) ----------
export const advReportApi = {
  promotion: (p: ReportRange) => api.get<PromotionReport>('/reports/promotion', { params: p }).then((r) => r.data),
  reconciliation: (p: ReportRange) => api.get<Reconciliation>('/reports/reconciliation', { params: p }).then((r) => r.data),
  // Drill-down: đơn tạo nên 1 con số (bucket cho thẻ tổng, hoặc method+payStatus cho dòng bảng)
  reconciliationOrders: (p: ReportRange & { bucket?: string; method?: string; payStatus?: string }) =>
    api.get<ReconOrder[]>('/reports/reconciliation-orders', { params: p }).then((r) => r.data),
  profit: (p: ReportRange) => api.get<ProfitReport>('/reports/profit', { params: p }).then((r) => r.data),
  // Drill-down lợi nhuận theo đơn (lọc theo danh mục nếu truyền category)
  profitOrders: (p: ReportRange & { category?: string }) =>
    api.get<ProfitOrder[]>('/reports/profit-orders', { params: p }).then((r) => r.data),
  // Drill-down Tổng quan: đơn hoàn tất (Doanh thu/Đơn/Máy đã bán/Giảm giá) & khách mới
  summaryOrders: (p: ReportRange) => api.get<SummaryOrder[]>('/reports/summary-orders', { params: p }).then((r) => r.data),
  newCustomerList: (p: ReportRange) => api.get<NewCustomer[]>('/reports/new-customer-list', { params: p }).then((r) => r.data),
  // Drill-down Tồn kho: phân loại theo nhóm (low/out/lowout/all)
  inventoryItems: (p: { bucket?: string }) => api.get<InventoryItem[]>('/reports/inventory-items', { params: p }).then((r) => r.data),
  churn: (p: ReportRange) => api.get<ChurnReport>('/reports/churn', { params: p }).then((r) => r.data),
  // Drill-down vòng đời khách theo nhóm (active/atRisk/churned/never/all)
  churnCustomers: (p: ReportRange & { bucket?: string }) =>
    api.get<ChurnCustomer[]>('/reports/churn-customers', { params: p }).then((r) => r.data),
  marketBasket: (p: ReportRange) => api.get<ProductPair[]>('/reports/market-basket', { params: p }).then((r) => r.data),
  rfm: () => api.get<RfmReport>('/reports/rfm').then((r) => r.data),
  demand: () => api.get<DemandItem[]>('/reports/demand-forecast').then((r) => r.data),
  processingTime: (p: ReportRange) => api.get<ProcessingTime>('/reports/processing-time', { params: p }).then((r) => r.data),
  processingOrders: (p: ReportRange) => api.get<ProcessingOrder[]>('/reports/processing-orders', { params: p }).then((r) => r.data),
  cancelReasons: (p: ReportRange) => api.get<CancelReason[]>('/reports/cancel-reasons', { params: p }).then((r) => r.data),
  viewToSale: () => api.get<ViewToSale[]>('/reports/view-to-sale').then((r) => r.data),
  search: (p: ReportRange) => api.get<SearchReport>('/reports/search', { params: p }).then((r) => r.data),
  funnel: (p: ReportRange) => api.get<FunnelReport>('/reports/funnel', { params: p }).then((r) => r.data),
  // Báo cáo mở rộng
  cohort: () => api.get<CohortReport>('/reports/cohort').then((r) => r.data),
  peakTime: (p: ReportRange) => api.get<PeakTimeReport>('/reports/peak-time', { params: p }).then((r) => r.data),
  reviews: (p: ReportRange) => api.get<ReviewReport>('/reports/reviews', { params: p }).then((r) => r.data),
  flashSalePerf: () => api.get<FlashSaleReport>('/reports/flash-sale-perf').then((r) => r.data)
}

// ---------- Notifications ----------
export const notificationApi = {
  mine: () => api.get<NotificationList>('/notifications').then((r) => r.data),
  read: (id: number) => api.put(`/notifications/${id}/read`),
  readAll: () => api.put('/notifications/read-all')
}

// ---------- Dashboard ----------
export const dashboardApi = {
  stats: () => api.get<DashboardStats>('/dashboard/stats').then((r) => r.data)
}

// ---------- Audit log (admin) ----------
export interface AuditLog {
  id: number
  userId?: number
  action: string
  entityType: string
  entityId?: number
  detail?: string
  createdAt: string
}
export const auditApi = {
  list: (page = 1, pageSize = 20) =>
    api.get<PagedResult<AuditLog>>('/auditlogs', { params: { page, pageSize } }).then((r) => r.data)
}
