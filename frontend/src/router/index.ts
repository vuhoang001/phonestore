import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const routes = [
  {
    path: '/',
    component: () => import('@/layouts/MainLayout.vue'),
    children: [
      { path: '', name: 'home', component: () => import('@/views/HomeView.vue') },
      { path: 'products', name: 'products', component: () => import('@/views/ProductListView.vue') },
      { path: 'products/:slug', name: 'product-detail', component: () => import('@/views/ProductDetailView.vue') },
      { path: 'cart', name: 'cart', component: () => import('@/views/CartView.vue') },
      { path: 'checkout', name: 'checkout', component: () => import('@/views/CheckoutView.vue'), meta: { requiresAuth: true } },
      { path: 'orders', name: 'orders', component: () => import('@/views/OrderHistoryView.vue'), meta: { requiresAuth: true } },
      { path: 'orders/:id', name: 'order-detail', component: () => import('@/views/OrderDetailView.vue'), meta: { requiresAuth: true } },
      { path: 'payment/mock', name: 'payment-mock', component: () => import('@/views/PaymentMockView.vue'), meta: { requiresAuth: true } },
      { path: 'payment/result', name: 'payment-result', component: () => import('@/views/PaymentResultView.vue') },
      { path: 'wishlist', name: 'wishlist', component: () => import('@/views/WishlistView.vue'), meta: { requiresAuth: true } },
      { path: 'notifications', name: 'notifications', component: () => import('@/views/NotificationsView.vue'), meta: { requiresAuth: true } },
      { path: 'account', name: 'account', component: () => import('@/views/AccountView.vue'), meta: { requiresAuth: true } },
      // Tra cứu bảo hành theo IMEI/mã đơn (công khai — đặc thù điện thoại)
      { path: 'warranty', name: 'warranty-lookup', component: () => import('@/views/WarrantyLookupView.vue') },
      // Thu cũ đổi mới
      { path: 'trade-in', name: 'trade-in', component: () => import('@/views/TradeInView.vue') },
      // Trang thông tin tĩnh (footer) — dùng chung InfoPageView, phân biệt bằng meta.key
      { path: 'ho-tro', name: 'help', component: () => import('@/views/InfoPageView.vue'), meta: { key: 'help' } },
      { path: 'huong-dan', name: 'guide', component: () => import('@/views/InfoPageView.vue'), meta: { key: 'guide' } },
      { path: 'doi-tra', name: 'returns', component: () => import('@/views/InfoPageView.vue'), meta: { key: 'returns' } },
      { path: 'bao-hanh', name: 'warranty-policy', component: () => import('@/views/InfoPageView.vue'), meta: { key: 'warranty' } },
      { path: 'gioi-thieu', name: 'about', component: () => import('@/views/InfoPageView.vue'), meta: { key: 'about' } },
      { path: 'dieu-khoan', name: 'terms', component: () => import('@/views/InfoPageView.vue'), meta: { key: 'terms' } },
      { path: 'bao-mat', name: 'privacy', component: () => import('@/views/InfoPageView.vue'), meta: { key: 'privacy' } },
      { path: 'lien-he', name: 'contact', component: () => import('@/views/InfoPageView.vue'), meta: { key: 'contact' } }
    ]
  },
  { path: '/login', name: 'login', component: () => import('@/views/LoginView.vue') },
  { path: '/register', name: 'register', component: () => import('@/views/RegisterView.vue') },
  { path: '/forgot-password', name: 'forgot-password', component: () => import('@/views/ForgotPasswordView.vue') },
  { path: '/reset-password', name: 'reset-password', component: () => import('@/views/ResetPasswordView.vue') },
  { path: '/confirm-email', name: 'confirm-email', component: () => import('@/views/ConfirmEmailView.vue') },
  {
    path: '/admin',
    component: () => import('@/layouts/AdminLayout.vue'),
    meta: { requiresAuth: true, requiresAdmin: true },
    children: [
      { path: '', name: 'admin-dashboard', component: () => import('@/views/admin/DashboardView.vue') },
      { path: 'reports', name: 'admin-reports', component: () => import('@/views/admin/ReportsView.vue') },
      { path: 'products', name: 'admin-products', component: () => import('@/views/admin/ProductManageView.vue') },
      { path: 'brands', name: 'admin-brands', component: () => import('@/views/admin/BrandManageView.vue') },
      { path: 'categories', name: 'admin-categories', component: () => import('@/views/admin/CategoryManageView.vue') },
      { path: 'orders', name: 'admin-orders', component: () => import('@/views/admin/OrderManageView.vue') },
      { path: 'shipping', name: 'admin-shipping', component: () => import('@/views/admin/ShippingManageView.vue') },
      { path: 'coupons', name: 'admin-coupons', component: () => import('@/views/admin/CouponManageView.vue') },
      { path: 'flash-sale', name: 'admin-flash-sale', component: () => import('@/views/admin/FlashSaleManageView.vue') },
      { path: 'audit-logs', name: 'admin-audit', component: () => import('@/views/admin/AuditLogView.vue') }
    ]
  },
  { path: '/:pathMatch(.*)*', name: 'not-found', component: () => import('@/views/NotFoundView.vue') }
]

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior: () => ({ top: 0 })
})

// Route guard: kiểm tra đăng nhập & quyền admin
router.beforeEach((to) => {
  const auth = useAuthStore()
  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }
  if (to.meta.requiresAdmin && !auth.isAdmin) {
    return { name: 'home' }
  }
  return true
})

// Sau mỗi lần deploy, Vite đổi hash các chunk → tab cũ tải chunk lazy đã biến mất (404).
// Khi lỗi "failed to fetch dynamically imported module", tự nạp lại 1 lần để lấy index.html + chunk mới.
const RELOAD_KEY = 'chunk-reload-at'
router.onError((err, to) => {
  const msg = String((err as Error)?.message || '')
  const isChunkError = /dynamically imported module|Importing a module script failed|Failed to fetch/i.test(msg)
  if (!isChunkError) return
  // Chống lặp vô hạn: chỉ reload lại nếu lần trước cách đây > 10s.
  const last = Number(sessionStorage.getItem(RELOAD_KEY) || 0)
  if (Date.now() - last > 10_000) {
    sessionStorage.setItem(RELOAD_KEY, String(Date.now()))
    window.location.assign(to?.fullPath || window.location.pathname)
  }
})

export default router
