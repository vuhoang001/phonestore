<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import Button from 'primevue/button'
import NotificationBell from '@/components/NotificationBell.vue'
import { useAuthStore } from '@/stores/auth'
import { useNotificationStore } from '@/stores/notification'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()
const notif = useNotificationStore()

// Admin có thể vào thẳng /admin (không qua MainLayout) → tự nạp + mở kênh realtime ở đây.
onMounted(() => {
  if (auth.isAuthenticated) {
    notif.fetch()
    notif.connect()
  }
})

// Sidebar mở/đóng — trên desktop luôn hiện; trên mobile là drawer trượt.
const sidebarOpen = ref(false)

const nav = [
  { label: 'Tổng quan', icon: 'pi pi-chart-line', name: 'admin-dashboard' },
  { label: 'Báo cáo', icon: 'pi pi-chart-bar', name: 'admin-reports' },
  { label: 'Sản phẩm', icon: 'pi pi-mobile', name: 'admin-products' },
  { label: 'Thương hiệu', icon: 'pi pi-tags', name: 'admin-brands' },
  { label: 'Danh mục', icon: 'pi pi-sitemap', name: 'admin-categories' },
  { label: 'Đơn hàng', icon: 'pi pi-shopping-bag', name: 'admin-orders' },
  { label: 'Vận chuyển', icon: 'pi pi-truck', name: 'admin-shipping' },
  { label: 'Mã giảm giá', icon: 'pi pi-ticket', name: 'admin-coupons' },
  { label: 'Flash Sale', icon: 'pi pi-bolt', name: 'admin-flash-sale' },
  { label: 'Nhật ký', icon: 'pi pi-history', name: 'admin-audit' }
]

function go(name: string) {
  router.push({ name })
  sidebarOpen.value = false // đóng drawer sau khi chọn (mobile)
}

// Đóng drawer khi đổi route
watch(() => route.name, () => { sidebarOpen.value = false })
</script>

<template>
  <div class="admin-shell">
    <!-- Overlay mờ khi mở drawer trên mobile -->
    <div v-if="sidebarOpen" class="overlay" @click="sidebarOpen = false" />

    <aside class="sidebar" :class="{ open: sidebarOpen }">
      <div class="brand"><i class="pi pi-mobile" /> Phone<b>Store</b></div>
      <nav>
        <button
          v-for="item in nav"
          :key="item.name"
          class="nav-item"
          :class="{ active: route.name === item.name }"
          @click="go(item.name)"
        >
          <i :class="item.icon" /> <span>{{ item.label }}</span>
        </button>
      </nav>
      <div class="sidebar-footer">
        <Button label="Về trang bán hàng" icon="pi pi-home" text @click="router.push('/')" />
      </div>
    </aside>

    <div class="admin-main">
      <header class="admin-header">
        <button class="burger" @click="sidebarOpen = !sidebarOpen" aria-label="Menu">
          <i class="pi pi-bars" />
        </button>
        <span class="hello">Xin chào, <strong>{{ auth.user?.fullName }}</strong></span>
        <div class="header-right">
          <NotificationBell />
        </div>
      </header>
      <div class="admin-content">
        <router-view />
      </div>
    </div>
  </div>
</template>

<style scoped>
.admin-shell { display: flex; min-height: 100vh; }

.sidebar {
  width: 240px; background: #131a2b; color: #e5e7eb;
  display: flex; flex-direction: column; padding: var(--sp-4) 0;
  flex-shrink: 0;
  /* Ghim sidebar theo viewport khi cuộn nội dung (giống filter trang sản phẩm). */
  position: sticky; top: 0;
  align-self: flex-start;
  height: 100vh; overflow-y: auto;
}
.brand { display: flex; align-items: center; gap: 8px; font-size: 1.2rem; font-weight: 800; padding: var(--sp-2) var(--sp-5) var(--sp-5); color: #fff; }
.brand .pi { color: var(--brand-light); }
.nav-item {
  display: flex; align-items: center; gap: 12px; width: 100%;
  background: none; border: none; color: #c9ccd3; padding: 12px var(--sp-5);
  cursor: pointer; font-size: 0.95rem; font-family: inherit; text-align: left;
  border-left: 3px solid transparent; transition: all var(--ease);
}
.nav-item:hover { background: #1c2740; color: #fff; }
.nav-item.active { background: rgba(30, 111, 255, 0.16); color: #fff; border-left-color: var(--brand-light); }
.sidebar-footer { margin-top: auto; padding: var(--sp-4) var(--sp-3); }

.admin-main { flex: 1; display: flex; flex-direction: column; background: var(--bg); min-width: 0; }
.admin-header {
  background: var(--surface); padding: var(--sp-3) var(--sp-5);
  border-bottom: 1px solid var(--border); display: flex; align-items: center; gap: var(--sp-3);
}
.header-right { margin-left: auto; display: flex; align-items: center; gap: var(--sp-3); }
.burger {
  display: none; background: none; border: none; cursor: pointer;
  font-size: 1.3rem; color: var(--text); padding: 4px;
}
.admin-content { padding: var(--sp-5); }

.overlay { display: none; }

/* ---------- Mobile: sidebar thành drawer trượt ---------- */
@media (max-width: 900px) {
  .burger { display: inline-flex; }
  .sidebar {
    position: fixed; top: 0; left: 0; bottom: 0; z-index: 200;
    transform: translateX(-100%); transition: transform var(--ease);
    box-shadow: var(--shadow-md);
  }
  .sidebar.open { transform: translateX(0); }
  .overlay {
    display: block; position: fixed; inset: 0; z-index: 150;
    background: rgba(0, 0, 0, 0.45);
  }
  .admin-content { padding: var(--sp-4); }
}
</style>
