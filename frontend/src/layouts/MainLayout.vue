<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import Badge from 'primevue/badge'
import Menu from 'primevue/menu'
import Button from 'primevue/button'
import NotificationBell from '@/components/NotificationBell.vue'
import CompareBar from '@/components/CompareBar.vue'
import { categoryApi } from '@/services'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { useNotificationStore } from '@/stores/notification'

const router = useRouter()
const auth = useAuthStore()
const cart = useCartStore()
const notif = useNotificationStore()
const keyword = ref('')
const userMenu = ref()

const menuItems = ref([
  { label: 'Tài khoản', icon: 'pi pi-user', command: () => router.push('/account') },
  { label: 'Đơn hàng của tôi', icon: 'pi pi-box', command: () => router.push('/orders') },
  { label: 'Trả góp của tôi', icon: 'pi pi-calendar', command: () => router.push('/installments') },
  { label: 'Yêu thích', icon: 'pi pi-heart', command: () => router.push('/wishlist') },
  { separator: true },
  { label: 'Đăng xuất', icon: 'pi pi-sign-out', command: logout }
])

// Danh mục nổi bật dưới ô tìm kiếm — lấy THẬT từ API (không hardcode).
const trending = ref<{ label: string; id: number }[]>([])

function search() {
  router.push({ name: 'products', query: keyword.value ? { keyword: keyword.value } : {} })
}
function quickCat(id: number) { router.push({ name: 'products', query: { categoryId: id } }) }

function logout() {
  auth.logout()
  cart.cart = null
  notif.disconnect() // đóng kết nối realtime khi đăng xuất
  router.push('/login')
}

onMounted(async () => {
  if (auth.isAuthenticated) {
    cart.fetch()
    notif.fetch()
    notif.connect() // mở kênh thông báo realtime
  }
  // Lấy danh mục con thật để làm gợi ý nhanh (6 mục đầu).
  try {
    const tree = await categoryApi.tree()
    const children = tree.flatMap((p) => p.children || [])
    trending.value = (children.length ? children : tree).slice(0, 6).map((c) => ({ label: c.name, id: c.id }))
  } catch { /* bỏ qua nếu lỗi */ }
})
</script>

<template>
  <header class="header">
    <div class="container header-inner">
      <router-link to="/" class="logo">
        <i class="pi pi-mobile" />
        <span>Phone<b>Store</b></span>
      </router-link>

      <div class="search-box">
        <input v-model="keyword" placeholder="Tìm điện thoại, phụ kiện..." @keyup.enter="search" />
        <button class="search-btn" @click="search" aria-label="Tìm kiếm">
          <i class="pi pi-search" />
        </button>
      </div>

      <nav class="actions">
        <NotificationBell v-if="auth.isAuthenticated" light />

        <router-link to="/cart" class="icon-btn" aria-label="Giỏ hàng">
          <i class="pi pi-shopping-cart" />
          <Badge v-if="cart.itemCount" :value="cart.itemCount" class="cart-badge" />
        </router-link>

        <span class="divider" />

        <template v-if="auth.isAuthenticated">
          <button v-if="auth.isAdmin" class="nav-link admin" @click="router.push('/admin')">
            <i class="pi pi-cog" /> Quản trị
          </button>
          <button class="user-btn" @click="userMenu.toggle($event)">
            <span class="ava">
              <img v-if="auth.user?.avatarUrl" :src="auth.user.avatarUrl" alt="" />
              <template v-else>{{ auth.user?.fullName?.[0] || 'U' }}</template>
            </span>
            <span class="uname">{{ auth.user?.fullName }}</span>
            <i class="pi pi-angle-down" />
          </button>
          <Menu ref="userMenu" :model="menuItems" :popup="true" />
        </template>
        <template v-else>
          <router-link to="/login" class="nav-link">Đăng nhập</router-link>
          <router-link to="/register"><button class="ghost-btn">Đăng ký</button></router-link>
        </template>
      </nav>
    </div>

    <!-- Thanh danh mục (lớp riêng bên dưới header) -->
    <div class="subbar">
      <div class="container subbar-inner">
        <router-link to="/products" class="sub-all"><i class="pi pi-th-large" /> Tất cả sản phẩm</router-link>
        <a v-for="c in trending" :key="c.id" @click="quickCat(c.id)">{{ c.label }}</a>
        <!-- Tính năng đặc thù điện thoại -->
        <router-link to="/compare" class="sub-feat"><i class="pi pi-sliders-h" /> So sánh máy</router-link>
        <router-link to="/warranty" class="sub-feat"><i class="pi pi-verified" /> Tra cứu bảo hành</router-link>
        <router-link to="/trade-in" class="sub-feat"><i class="pi pi-refresh" /> Thu cũ đổi mới</router-link>
      </div>
    </div>
  </header>

  <main class="container page">
    <router-view />
  </main>

  <!-- Thanh So sánh nổi — hiện khi khách đã chọn máy để so sánh -->
  <CompareBar />

  <footer class="footer">
    <div class="container footer-grid">
      <div class="fcol">
        <div class="logo footer-logo"><i class="pi pi-mobile" /> Phone<b>Store</b></div>
        <p>Điện thoại chính hãng, bảo hành theo IMEI, giá tốt mỗi ngày.</p>
      </div>
      <div class="fcol">
        <h4>Chăm sóc khách hàng</h4>
        <ul>
          <li><router-link to="/ho-tro">Trung tâm trợ giúp</router-link></li>
          <li><router-link to="/huong-dan">Hướng dẫn mua hàng</router-link></li>
          <li><router-link to="/doi-tra">Trả hàng &amp; hoàn tiền</router-link></li>
          <li><router-link to="/warranty">Tra cứu bảo hành</router-link></li>
        </ul>
      </div>
      <div class="fcol">
        <h4>Về PhoneStore</h4>
        <ul>
          <li><router-link to="/gioi-thieu">Giới thiệu</router-link></li>
          <li><router-link to="/dieu-khoan">Điều khoản</router-link></li>
          <li><router-link to="/bao-mat">Chính sách bảo mật</router-link></li>
          <li><router-link to="/lien-he">Liên hệ hợp tác</router-link></li>
        </ul>
      </div>
      <div class="fcol">
        <h4>Thanh toán & Vận chuyển</h4>
        <div class="pays">
          <span class="pay vnpay">VNPAY</span>
          <span class="pay cod">COD</span>
          <span class="pay inst">Trả góp</span>
        </div>
        <div class="ships">
          <span class="ship"><i class="pi pi-truck" /> Giao nhanh</span>
          <span class="ship"><i class="pi pi-verified" /> Chính hãng</span>
        </div>
      </div>
    </div>
    <div class="footer-bottom">© 2026 PhoneStore · Điện thoại chính hãng</div>
  </footer>
</template>

<style scoped>
/* Minimal Premium: header kính mờ trắng trong suốt (Apple nav), viền hairline dưới */
.header {
  background: rgba(255, 255, 255, 0.72);
  backdrop-filter: saturate(180%) blur(20px);
  -webkit-backdrop-filter: saturate(180%) blur(20px);
  position: sticky; top: 0; z-index: 100;
  border-bottom: 1px solid var(--border);
}
/* Hàng header chính — 1 hàng gọn */
.header-inner { display: flex; align-items: center; gap: var(--sp-5); height: 60px; }

.logo {
  display: flex; align-items: center; gap: 8px;
  font-size: 1.35rem; font-weight: 700; letter-spacing: -0.02em; color: var(--text); white-space: nowrap;
}
.logo .pi { font-size: 1.5rem; color: var(--brand); }
.logo b { font-weight: 700; }

/* Ô tìm kiếm — pill xám nhạt trên nền trắng, viền hairline, focus vòng xanh nhạt */
.search-box {
  flex: 1; max-width: 520px; display: flex; align-items: center;
  background: var(--surface-2); border: 1px solid var(--border); border-radius: var(--radius-pill);
  padding: 0 4px 0 6px; transition: border-color var(--ease), box-shadow var(--ease);
}
.search-box:focus-within { border-color: var(--brand); box-shadow: 0 0 0 3px var(--brand-50); }
.search-box input {
  flex: 1; border: none; outline: none; height: 38px; padding: 0 10px;
  font-family: inherit; font-size: 14px; background: transparent; color: var(--text);
}
.search-box input::placeholder { color: var(--text-muted); }
.search-box input:focus-visible { outline: none; }
.search-btn {
  border: none; background: transparent; color: var(--text-muted); cursor: pointer;
  width: 34px; height: 34px; flex-shrink: 0; border-radius: var(--radius-pill);
  display: grid; place-items: center; transition: background var(--ease), color var(--ease);
}
.search-btn .pi { font-size: 16px; }
.search-btn:hover { background: var(--brand-50); color: var(--brand); }

/* Thanh danh mục — hairline, chữ xám dịu, hover xanh */
.subbar { border-top: 1px solid var(--border); }
.subbar-inner { display: flex; align-items: center; gap: 18px; height: 42px; overflow-x: auto; scrollbar-width: none; }
.subbar-inner::-webkit-scrollbar { display: none; }
.subbar a, .sub-all { color: var(--text-2); font-size: 13px; cursor: pointer; white-space: nowrap; transition: color var(--ease); display: inline-flex; align-items: center; gap: 5px; }
.subbar a:hover, .sub-all:hover { color: var(--brand); }
.sub-all { font-weight: 600; color: var(--text); padding-right: 16px; border-right: 1px solid var(--border); }
.sub-all .pi { font-size: 13px; }
/* Tính năng đặc thù đẩy về cuối hàng, có gạch ngăn trước */
.sub-feat { font-weight: 500; }
.sub-feat:first-of-type { margin-left: auto; padding-left: 16px; border-left: 1px solid var(--border); }
.sub-feat .pi { font-size: 12px; color: var(--brand); }

.actions { display: flex; align-items: center; gap: var(--sp-2); margin-left: auto; }
.nav-link {
  color: var(--text); font-weight: 500; font-size: 14px; background: none; border: none;
  cursor: pointer; display: inline-flex; align-items: center; gap: 5px;
  padding: 7px 12px; border-radius: var(--radius-pill); transition: background var(--ease), color var(--ease);
}
.nav-link:hover { background: var(--surface-2); color: var(--brand); }
.nav-link.admin { background: var(--surface-2); border: 1px solid var(--border); padding: 6px 14px; border-radius: var(--radius-pill); }
.nav-link.admin:hover { background: var(--brand-50); color: var(--brand); }

/* Nút icon (giỏ hàng, chuông) — nền tròn xám khi hover */
.icon-btn { position: relative; color: var(--text-2); display: inline-grid; place-items: center; width: 40px; height: 40px; border-radius: 50%; background: none; border: none; cursor: pointer; transition: background var(--ease), color var(--ease); }
.icon-btn:hover { background: var(--surface-2); color: var(--brand); }
.icon-btn .pi { font-size: 1.3rem; }

/* Vạch ngăn giữa nhóm điều hướng và tài khoản */
.divider { width: 1px; height: 22px; background: var(--border); margin: 0 4px; }
/* Avatar tròn trong nút tài khoản */
.ava { width: 26px; height: 26px; border-radius: 50%; background: var(--brand-50); color: var(--brand); display: grid; place-items: center; overflow: hidden; font-size: 12px; font-weight: 700; text-transform: uppercase; flex-shrink: 0; }
.ava img { width: 100%; height: 100%; object-fit: cover; }
.ava .pi { font-size: 13px; }

.cart-badge { position: absolute; top: 4px; right: 4px; min-width: 17px; height: 17px; line-height: 17px; padding: 0 4px;
  font-size: 10px; font-weight: 700; border-radius: var(--radius-pill);
  background: var(--brand) !important; color: #fff !important; }

.user-btn {
  display: inline-flex; align-items: center; gap: 6px; color: var(--text);
  background: var(--surface-2); border: 1px solid var(--border); cursor: pointer;
  padding: 6px 12px; border-radius: var(--radius-pill); font-family: inherit; font-size: 14px;
  transition: background var(--ease);
}
.user-btn:hover { background: var(--brand-50); }
.uname { max-width: 120px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

.ghost-btn {
  background: var(--brand); color: #fff; border: none; cursor: pointer;
  padding: 8px 18px; border-radius: var(--radius-pill); font-family: inherit; font-weight: 600; font-size: 14px;
  transition: transform var(--ease), background var(--ease);
}
.ghost-btn:hover { transform: translateY(-1px); background: var(--brand-dark); }

/* Footer */
.footer { background: var(--surface); border-top: 1px solid var(--border); margin-top: var(--sp-8); }
.footer-grid { display: grid; grid-template-columns: 1.6fr 1fr 1fr 1.2fr; gap: var(--sp-6); padding: var(--sp-8) var(--sp-4); }
.footer h4 { font-size: 14px; margin-bottom: 12px; color: var(--text); }
.footer p { color: var(--text-muted); margin: 4px 0; font-size: 13px; }
.footer-logo { color: var(--brand); font-size: 1.2rem; margin-bottom: 8px; }
.fcol ul { list-style: none; padding: 0; margin: 0; }
.fcol ul li { font-size: 13px; padding: 4px 0; width: fit-content; }
.fcol ul li a { color: var(--text-muted); text-decoration: none; transition: color var(--ease); }
.fcol ul li a:hover { color: var(--brand); }
.fcol ul li a.router-link-active { color: var(--brand); font-weight: 600; }

/* Badge thanh toán / vận chuyển */
.pays { display: flex; flex-wrap: wrap; gap: 6px; }
.pay { font-size: 11px; font-weight: 800; padding: 5px 9px; border-radius: var(--radius-sm); color: #fff; letter-spacing: 0.3px; }
.pay.vnpay { background: #005baa; }
.pay.cod { background: var(--c-green); }
.pay.inst { background: var(--c-purple); }
.ships { display: flex; flex-wrap: wrap; gap: 8px; margin-top: 10px; }
.ship { display: inline-flex; align-items: center; gap: 4px; font-size: 12px; color: var(--text-2); background: var(--surface-2); border: 1px solid var(--border); padding: 4px 10px; border-radius: var(--radius-pill); }
.ship .pi { color: var(--c-green); font-size: 12px; }

.footer-bottom { text-align: center; padding: var(--sp-4); border-top: 1px solid var(--border); color: var(--text-muted); font-size: 13px; }

@media (max-width: 900px) { .footer-grid { grid-template-columns: 1fr 1fr; } }
@media (max-width: 768px) {
  .header-inner { gap: var(--sp-3); }
  .nav-link span, .uname { display: none; }   /* giữ icon, ẩn chữ cho gọn */
  .sub-feat:first-of-type { margin-left: 16px; }  /* trên mobile để cuộn ngang tự nhiên */
}
@media (max-width: 480px) { .footer-grid { grid-template-columns: 1fr; } }
@media (max-width: 480px) {
  .header-inner { min-height: 54px; padding: 8px 0; gap: var(--sp-2); }
  .logo span { display: none; }          /* chỉ giữ icon để dành chỗ cho ô tìm kiếm */
  .search-btn { width: 46px; }
  .divider { display: none; }
}
</style>
