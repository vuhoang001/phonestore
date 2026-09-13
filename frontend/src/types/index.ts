// Kiểu dữ liệu dùng chung, khớp với DTO của backend PhoneStore.

export interface User {
  id: number
  email: string
  fullName: string
  phone?: string
  avatarUrl?: string
  role: 'Customer' | 'Seller' | 'Admin'
  emailConfirmed?: boolean
}

export interface AuthResponse {
  token: string
  refreshToken: string
  expiresAt: string
  user: User
}

export interface Category {
  id: number
  parentId?: number
  name: string
  slug: string
  imageUrl?: string
  children: Category[]
}

// ---------- Thương hiệu (điện thoại) ----------
export interface Brand {
  id: number
  name: string
  slug: string
  logoUrl?: string
  description?: string
}

export interface ProductListItem {
  id: number
  name: string
  slug: string
  basePrice: number
  primaryImage?: string
  // Tên hãng (Apple, Samsung...) để hiển thị & lọc.
  brandName: string
  averageRating: number
  reviewCount: number
  totalStock: number
  soldCount: number
  flashPrice?: number // Giá Flash Sale đang chạy (nếu có)
}

export interface FlashSaleItem {
  id: number
  productId: number
  productName: string
  productSlug: string
  productImage?: string
  originalPrice: number
  flashPrice: number
  discountPercent: number
  quantityLimit: number
  soldCount: number
}

export interface FlashSale {
  id: number
  name: string
  startAt: string
  endAt: string
  isActive: boolean
  isRunning: boolean
  items: FlashSaleItem[]
}

export interface ProductVariant {
  id: number
  sku: string
  color?: string
  colorHex?: string   // Mã màu hiển thị swatch (VD: #1e6fff)
  storage?: string    // Dung lượng lưu trữ (VD: 128GB, 256GB)
  price: number
  cost?: number       // Giá vốn (admin nhập, không lộ cho khách)
  stockQuantity: number
}

export interface ProductImage {
  id: number
  variantId?: number
  url: string
  isPrimary: boolean
  sortOrder: number
}

// Một dòng thông số kỹ thuật của máy (gom nhóm theo group ở UI).
export interface ProductSpec {
  group: string
  name: string
  value: string
}

// Ước tính trả góp theo kỳ hạn.
export interface InstallmentOption {
  months: number
  monthly: number
}

export interface ProductDetail {
  id: number
  categoryId: number
  categoryName: string
  brandId: number
  brandName: string
  name: string
  slug: string
  description?: string
  basePrice: number
  status: string
  warrantyMonths: number          // Số tháng bảo hành mặc định
  installmentAvailable: boolean    // Máy có hỗ trợ trả góp không
  variants: ProductVariant[]
  images: ProductImage[]
  specifications: ProductSpec[]    // Bảng thông số kỹ thuật
  installmentOptions: InstallmentOption[] // Các kỳ hạn trả góp ước tính
  averageRating: number
  reviewCount: number
  viewCount: number
  soldCount: number
  ratingBreakdown: number[]
  flashPrice?: number  // Giá Flash Sale đang chạy (nếu có)
  flashEndAt?: string  // Thời điểm kết thúc sale (đếm ngược)
}

export interface CartItem {
  id: number
  variantId: number
  productId: number
  productName: string
  variantInfo?: string
  imageUrl?: string
  price: number
  quantity: number
  stockQuantity: number
  lineTotal: number
}

export interface Cart {
  id: number
  items: CartItem[]
  subTotal: number
  totalQuantity: number
}

export interface Address {
  id: number
  recipientName: string
  phone: string
  province: string
  district?: string // Không dùng ở hệ hành chính 2 cấp (từ 01/7/2025); giữ để tương thích dữ liệu cũ
  ward: string
  detail: string // Số nhà, tên đường
  note?: string // Ghi chú giao hàng (toà nhà, tầng, mốc gần đó...) — tùy chọn
  isDefault: boolean
}

export interface OrderItem {
  id: number
  variantId: number
  productName: string
  variantInfo?: string
  price: number
  quantity: number
  lineTotal: number
  imei?: string   // IMEI/Serial gán khi giao máy (null trước khi giao)
}

export interface OrderStatusHistory {
  status: string
  note?: string
  changedAt: string
}

export interface Order {
  id: number
  orderCode: string
  subTotal: number
  discountAmount: number
  shippingFee: number
  totalAmount: number
  status: string
  shippingAddress: string
  note?: string
  createdAt: string
  paymentMethod?: string
  paymentStatus?: string
  installmentMonths?: number    // Số kỳ trả góp (null nếu trả thẳng)
  installmentMonthly?: number   // Số tiền ước tính phải trả mỗi tháng
  items: OrderItem[]
  statusHistory: OrderStatusHistory[]
  allowedNextStatuses: string[]
  canCancelByCustomer: boolean
}

export interface Review {
  id: number
  userId: number
  userName: string
  rating: number
  comment?: string
  images: string[]
  createdAt: string
}

export interface Coupon {
  id: number
  code: string
  discountType: string
  discountValue: number
  minOrderAmount: number
  startDate: string
  endDate: string
  usageLimit: number
  usedCount: number
  isActive: boolean
}

export interface WishlistItem {
  id: number
  productId: number
  productName: string
  slug: string
  basePrice: number
  primaryImage?: string
}

export interface ShippingMethod {
  id: number
  name: string
  baseFee: number
  estimatedDays: number
  isActive?: boolean
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export interface AppNotification {
  id: number
  title: string
  message: string
  type: string
  link?: string
  isRead: boolean
  createdAt: string
}

export interface NotificationList {
  items: AppNotification[]
  unreadCount: number
}

export interface DashboardStats {
  totalRevenue: number
  totalOrders: number
  totalProducts: number
  totalCustomers: number
  revenueByDay: { date: string; revenue: number }[]
  topProducts: { productId: number; productName: string; quantitySold: number; revenue: number }[]
  ordersByStatus: Record<string, number>
}

// ---------- Bảo hành theo IMEI (đặc thù điện thoại) ----------
export interface Warranty {
  id: number
  imei: string
  productName: string
  orderCode: string
  startDate: string
  endDate: string
  status: string   // "Còn hạn" / "Hết hạn" (do backend quyết định)
}

export interface WarrantyLookupResult {
  found: boolean
  items: Warranty[]
}

// ---------- Thu cũ đổi mới ----------
export interface TradeIn {
  id: number
  oldDeviceModel: string
  condition: string
  note?: string
  quotedPrice: number
  status: string
  targetProductId?: number
  createdAt: string
}

// ---------- Reports (chỉ báo cáo cơ bản — bỏ advanced) ----------
export interface TimePoint { label: string; revenue: number; orders: number }
export interface CountPoint { label: string; count: number }
export interface ReportSummary {
  revenue: number; orders: number; avgOrderValue: number
  itemsSold: number; discounts: number; newCustomers: number
}
export interface RevenueReport {
  totalRevenue: number; totalOrders: number; avgOrderValue: number; series: TimePoint[]
}
export interface TopProductReport { productId: number; productName: string; quantitySold: number; revenue: number }
export interface CategoryRevenue { category: string; quantitySold: number; revenue: number }
export interface PaymentMethodRevenue { method: string; orders: number; revenue: number }
export interface OrderStats {
  total: number; byStatus: Record<string, number>; completed: number; cancelled: number
  completionRate: number; cancelRate: number; avgOrderValue: number
}
export interface LowStockItem { productId: number; productName: string; sku: string; variant?: string; stock: number }
export interface InventoryReport {
  totalProducts: number; totalVariants: number; totalStockUnits: number; stockValue: number
  outOfStockCount: number; lowStockCount: number; lowStockItems: LowStockItem[]
}

// ---------- Báo cáo nâng cao (Advanced Reports) ----------
export interface CouponPerf { code: string; timesUsed: number; totalDiscount: number; revenueGenerated: number }
export interface PromotionReport {
  totalDiscount: number; ordersWithCoupon: number; revenueWithCoupon: number
  aovWithCoupon: number; aovWithoutCoupon: number; coupons: CouponPerf[]
}
export interface PaymentRow { method: string; status: string; orders: number; amount: number }
export interface Reconciliation {
  codPending: number; codPendingOrders: number; codCollected: number
  onlinePaid: number; onlinePending: number; rows: PaymentRow[]
}
export interface ReconOrder {
  id: number; orderCode: string; createdAt: string
  orderStatus: string; method: string; payStatus: string; amount: number
}
export interface ProfitOrder {
  id: number; orderCode: string; createdAt: string
  revenue: number; cogs: number; profit: number; discount: number; shipping: number
}
export interface SummaryOrder { id: number; orderCode: string; createdAt: string; status: string; itemCount: number; discount: number; total: number }
export interface NewCustomer { userId: number; name: string; email: string; createdAt: string }
export interface InventoryItem { productId: number; productName: string; sku: string; variant: string; stock: number; price: number; value: number }
export interface ProcessingOrder { id: number; orderCode: string; createdAt: string; confirmHours: number; shipHours: number; completeHours: number; totalHours: number }
export interface ProfitCategory { category: string; revenue: number; cogs: number; profit: number; marginPct: number }
export interface ProfitReport {
  revenue: number; cogs: number; grossProfit: number; grossMarginPct: number
  discounts: number; shippingCharged: number; byCategory: ProfitCategory[]
}
export interface ChurnCustomer { userId: number; name: string; email: string; lastOrder?: string; daysSince: number; totalSpent: number }
export interface ChurnReport {
  totalCustomers: number; active: number; atRisk: number; churned: number; neverOrdered: number
  topChurned: ChurnCustomer[]
}
export interface ProductPair { productA: string; productB: string; count: number }
export interface RfmSegment { segment: string; customers: number; revenue: number }
export interface RfmReport { segments: RfmSegment[] }
export interface DemandItem { productId: number; productName: string; sku: string; stock: number; avgDailySold: number; daysLeft: number; suggestedReorder: number }
export interface ProcessingTime { avgConfirmHours: number; avgShipHours: number; avgCompleteHours: number; avgTotalHours: number; sampleSize: number }
export interface CancelReason { reason: string; count: number; lostRevenue: number }
export interface ViewToSale { productId: number; productName: string; views: number; sold: number; conversionRate: number }
export interface KeywordStat { keyword: string; count: number; avgResults: number }
export interface SearchReport { totalSearches: number; topKeywords: KeywordStat[]; noResultKeywords: KeywordStat[] }
export interface FunnelReport { productViews: number; cartItems: number; orders: number; completedOrders: number; viewToOrderRate: number; orderCompletionRate: number }

// ---------- Báo cáo mở rộng ----------
export interface CohortRow { cohort: string; size: number; retained: number[]; retainedPct: number[] }
export interface CohortReport { offsetLabels: string[]; rows: CohortRow[] }
export interface PeakCell { weekday: number; hour: number; orders: number; revenue: number }
export interface PeakTimeReport {
  cells: PeakCell[]; maxOrders: number; peakHour: number; peakWeekday: number
  byHour: number[]; byWeekday: number[]; totalOrders: number
}
export interface ReviewTrend { label: string; count: number; avg: number }
export interface WorstProduct { productId: number; productName: string; avgRating: number; reviewCount: number }
export interface ReviewReport {
  totalReviews: number; avgRating: number; distribution: number[]
  trend: ReviewTrend[]; worstProducts: WorstProduct[]
}
export interface FlashSaleRow {
  saleName: string; productName: string; originalPrice: number; flashPrice: number
  unitsSold: number; revenue: number; discount: number; running: boolean
}
export interface FlashSaleReport { programs: number; unitsSold: number; revenue: number; discountGiven: number; items: FlashSaleRow[] }
