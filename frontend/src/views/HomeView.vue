<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import Carousel from 'primevue/carousel'
import { useToast } from 'primevue/usetoast'
import ProductCard from '@/components/ProductCard.vue'
import ProductSkeleton from '@/components/ProductSkeleton.vue'
import { productApi, brandsApi, couponApi, flashSaleApi } from '@/services'
import type { ProductListItem, Brand, Coupon, FlashSale, FlashSaleItem } from '@/types'
import { formatCurrency } from '@/composables/format'

const router = useRouter()
const toast = useToast()

const flashSale = ref<FlashSale | null>(null)
const feed = ref<ProductListItem[]>([])
const brands = ref<Brand[]>([])
const coupons = ref<Coupon[]>([])
const loading = ref(true)
const loadingMore = ref(false)
const feedPage = ref(1)
const feedDone = ref(false)
const savedCodes = ref<string[]>(JSON.parse(localStorage.getItem('savedVouchers') || '[]'))

// Banner carousel — ảnh thật (picsum) phủ tint xanh công nghệ để chữ nổi rõ.
const banners = [
  { id: 1, title: 'Siêu Sale Điện Thoại', sub: 'Giảm đến 40% các dòng máy hot',
    img: 'https://picsum.photos/seed/phonestore-sale/1280/440',
    tint: 'linear-gradient(90deg, rgba(15,45,110,.94) 0%, rgba(30,111,255,.58) 55%, rgba(77,139,255,.28) 100%)' },
  { id: 2, title: 'Trả Góp 0% Lãi Suất', sub: 'Sở hữu máy mới chỉ từ vài trăm nghìn/tháng',
    img: 'https://picsum.photos/seed/phonestore-inst/1280/440',
    tint: 'linear-gradient(90deg, rgba(22,87,204,.94) 0%, rgba(77,139,255,.5) 100%)' },
  { id: 3, title: 'Điện Thoại Chính Hãng', sub: 'Bảo hành theo IMEI, giao nhanh toàn quốc',
    img: 'https://picsum.photos/seed/phonestore-tech/1280/440',
    tint: 'linear-gradient(90deg, rgba(15,45,110,.94) 0%, rgba(30,111,255,.5) 100%)' }
]
// 2 banner phụ bên phải (ảnh thật + phủ tối nhẹ để chữ trắng nổi)
const promos = [
  { title: 'Thu Cũ Đổi Mới', sub: 'Trợ giá tới vài triệu đồng', img: 'https://picsum.photos/seed/phonestore-p1/640/320', to: '/trade-in' },
  { title: 'Tra Cứu Bảo Hành', sub: 'Kiểm tra nhanh theo IMEI', img: 'https://picsum.photos/seed/phonestore-p2/640/320', to: '/warranty' }
]

// Dải cam kết dịch vụ — icon trung tính (Minimal Premium: 1 tông, không cầu vồng).
const benefits = [
  { icon: 'pi-truck', fg: 'var(--text)', bg: 'var(--surface-2)', title: 'Miễn phí vận chuyển', sub: 'Đơn từ 500.000đ' },
  { icon: 'pi-verified', fg: 'var(--text)', bg: 'var(--surface-2)', title: 'Chính hãng 100%', sub: 'Bảo hành theo IMEI' },
  { icon: 'pi-calendar', fg: 'var(--text)', bg: 'var(--surface-2)', title: 'Trả góp 0%', sub: 'Duyệt nhanh 15 phút' },
  { icon: 'pi-shield', fg: 'var(--text)', bg: 'var(--surface-2)', title: 'Thanh toán an toàn', sub: 'Bảo mật VNPAY' }
]

// ----- Đồng hồ đếm ngược Flash Sale (đến thời điểm endAt của phiên) -----
const now = ref(Date.now())
let timer: number
const flashEnd = computed(() => flashSale.value ? new Date(flashSale.value.endAt).getTime() : 0)
const countdown = computed(() => {
  const diff = Math.max(0, flashEnd.value - now.value)
  const h = Math.floor(diff / 3.6e6)
  const m = Math.floor((diff % 3.6e6) / 6e4)
  const s = Math.floor((diff % 6e4) / 1000)
  const pad = (n: number) => String(n).padStart(2, '0')
  return { h: pad(h), m: pad(m), s: pad(s) }
})

// % tiến độ bán của 1 suất flash sale
function soldPercent(item: FlashSaleItem): number {
  return item.quantityLimit ? Math.min(100, Math.round((item.soldCount / item.quantityLimit) * 100)) : 0
}

function isSaved(code: string) { return savedCodes.value.includes(code) }
function saveVoucher(c: Coupon) {
  if (isSaved(c.code)) return
  savedCodes.value.push(c.code)
  localStorage.setItem('savedVouchers', JSON.stringify(savedCodes.value))
  toast.add({ severity: 'success', summary: 'Đã lưu voucher', detail: c.code, life: 2000 })
}

async function loadMore() {
  if (feedDone.value || loadingMore.value) return
  loadingMore.value = true
  try {
    const res = await productApi.search({ page: feedPage.value, pageSize: 12, sortBy: 'newest' })
    feed.value.push(...res.items)
    if (feedPage.value >= res.totalPages || res.items.length === 0) feedDone.value = true
    else feedPage.value++
  } finally {
    loadingMore.value = false
  }
}

// ----- Infinite scroll + nút về đầu trang -----
const showTop = ref(false)
function scrollToTop() { window.scrollTo({ top: 0, behavior: 'smooth' }) }
let ticking = false
function onScroll() {
  if (ticking) return
  ticking = true
  requestAnimationFrame(() => {
    showTop.value = window.scrollY > 600
    if (!loading.value && !loadingMore.value && !feedDone.value &&
        window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 700) {
      loadMore()
    }
    ticking = false
  })
}

onMounted(async () => {
  timer = window.setInterval(() => (now.value = Date.now()), 1000)
  window.addEventListener('scroll', onScroll, { passive: true })
  try {
    const [flash, brs, cps] = await Promise.all([
      flashSaleApi.active().catch(() => null),
      brandsApi.list().catch(() => []),
      couponApi.available().catch(() => [])
    ])
    flashSale.value = flash
    brands.value = brs
    coupons.value = cps
    await loadMore()
  } finally {
    loading.value = false
  }
})
onUnmounted(() => { clearInterval(timer); window.removeEventListener('scroll', onScroll) })
</script>

<template>
  <!-- Hero spotlight full-bleed (Apple-store style): 1 tuyên ngôn lớn, chữ căn giữa, chồng tầng -->
  <section class="spot">
    <Carousel :value="banners" :numVisible="1" :numScroll="1" circular :autoplayInterval="5000" :showNavigators="false" class="spot-carousel">
      <template #item="{ data }">
        <div class="spot-slide" :style="{ backgroundImage: 'url(' + data.img + ')' }">
          <div class="spot-text">
            <span class="eyebrow">PhoneStore</span>
            <h1>{{ data.title }}</h1>
            <p>{{ data.sub }}</p>
            <div class="spot-links">
              <button class="spot-link primary" @click="router.push('/products')">Mua ngay <i class="pi pi-arrow-right" /></button>
              <button class="spot-link" @click="router.push('/trade-in')">Thu cũ đổi mới <i class="pi pi-angle-right" /></button>
            </div>
          </div>
        </div>
      </template>
    </Carousel>
  </section>

  <!-- Two-up band (Apple two-tile): 2 ô lớn, chữ căn giữa phía trên, ảnh phía dưới -->
  <section class="two-up">
    <article v-for="p in promos" :key="p.title" class="tile" @click="router.push(p.to)">
      <div class="tile-text">
        <h3>{{ p.title }}</h3>
        <p>{{ p.sub }}</p>
        <span class="tile-link">Tìm hiểu thêm <i class="pi pi-angle-right" /></span>
      </div>
      <div class="tile-img" :style="{ backgroundImage: 'url(' + p.img + ')' }" />
    </article>
  </section>

  <!-- Cam kết dịch vụ (icon nhiều màu) -->
  <section class="benefits">
    <div v-for="b in benefits" :key="b.title" class="benefit">
      <span class="bi" :style="{ background: b.bg, color: b.fg }"><i class="pi" :class="b.icon" /></span>
      <div class="bt"><strong>{{ b.title }}</strong><small>{{ b.sub }}</small></div>
    </div>
  </section>

  <!-- Kho voucher -->
  <section v-if="coupons.length" class="vouchers surface-card">
    <div class="v-head"><h2><i class="pi pi-ticket" /> Kho Voucher</h2>
      <router-link to="/account" class="see-all">Voucher của tôi <i class="pi pi-angle-right" /></router-link>
    </div>
    <div class="v-strip">
      <div v-for="c in coupons" :key="c.id" class="voucher">
        <div class="v-left">
          <div class="v-val">{{ c.discountType === 'Percentage' ? c.discountValue + '%' : formatCurrency(c.discountValue) }}</div>
          <div class="v-min">Đơn từ {{ formatCurrency(c.minOrderAmount) }}</div>
        </div>
        <div class="v-right">
          <div class="v-code">{{ c.code }}</div>
          <button class="v-save" :class="{ saved: isSaved(c.code) }" @click="saveVoucher(c)">
            {{ isSaved(c.code) ? 'Đã lưu' : 'Lưu' }}
          </button>
        </div>
      </div>
    </div>
  </section>

  <!-- Dải thương hiệu (logo) -->
  <section v-if="brands.length" class="brands surface-card">
    <h2 class="brands-title">Thương hiệu nổi bật</h2>
    <div class="brand-strip">
      <button v-for="b in brands" :key="b.id" class="brand-chip"
        @click="router.push({ name: 'products', query: { brandId: b.id } })">
        <img v-if="b.logoUrl" :src="b.logoUrl" :alt="b.name" class="brand-logo" />
        <span class="brand-name">{{ b.name }}</span>
      </button>
    </div>
  </section>

  <!-- Flash sale (dữ liệu thật; chỉ hiện khi có phiên đang chạy và có sản phẩm) -->
  <section v-if="flashSale && flashSale.isRunning && flashSale.items.length" class="flash surface-card">
    <div class="flash-head">
      <div class="flash-title"><i class="pi pi-bolt" /> FLASH SALE</div>
      <div class="countdown">
        <span class="cd-label">Kết thúc trong</span>
        <span class="cd-box">{{ countdown.h }}</span>:<span class="cd-box">{{ countdown.m }}</span>:<span class="cd-box">{{ countdown.s }}</span>
      </div>
      <router-link to="/products" class="see-all">Xem tất cả <i class="pi pi-angle-right" /></router-link>
    </div>
    <div class="flash-grid">
      <div v-for="item in flashSale.items" :key="item.id" class="flash-card" @click="router.push(`/products/${item.productSlug}`)">
        <div class="fc-img">
          <span class="fc-badge">-{{ item.discountPercent }}%</span>
          <img :src="item.productImage || 'https://placehold.co/200'" :alt="item.productName" />
        </div>
        <div class="fc-name">{{ item.productName }}</div>
        <div class="fc-prices">
          <span class="price">{{ formatCurrency(item.flashPrice) }}</span>
          <span class="line-through">{{ formatCurrency(item.originalPrice) }}</span>
        </div>
        <div class="fc-bar">
          <span class="fc-fill" :style="{ width: soldPercent(item) + '%' }" />
          <span class="fc-text">Đã bán {{ item.soldCount }}</span>
        </div>
      </div>
    </div>
  </section>

  <!-- Gợi ý hôm nay -->
  <h2 class="suggest-head section-title">Máy nổi bật</h2>
  <div class="grid-products">
    <ProductCard v-for="p in feed" :key="p.id" :product="p" />
    <!-- Skeleton khi tải trang đầu hoặc nối trang tiếp theo (infinite scroll) -->
    <ProductSkeleton v-if="loadingMore" :count="feed.length ? 4 : 12" />
  </div>
  <div class="more">
    <span v-if="feedDone && feed.length" class="text-muted">— Bạn đã xem hết —</span>
  </div>

  <!-- Nút nổi: về đầu trang -->
  <Transition name="fade">
    <button v-show="showTop" class="to-top" aria-label="Về đầu trang" @click="scrollToTop">
      <i class="pi pi-arrow-up" />
    </button>
  </Transition>
</template>

<style scoped>
/* ===== Hero spotlight full-bleed (Apple-store style) — chữ căn giữa, chồng tầng ===== */
.spot { position: relative; margin: 0 0 var(--sp-6); }
.spot-carousel :deep(.p-carousel-indicator-list) { position: absolute; left: 0; right: 0; bottom: 16px; margin: 0; z-index: 2; }
.spot-slide {
  min-height: 520px; display: flex; align-items: flex-start; justify-content: center;
  text-align: center; padding: var(--sp-8) var(--sp-4);
  background-size: cover; background-position: center; background-color: var(--surface-2);
  border-radius: var(--radius-lg); overflow: hidden; position: relative;
}
/* Lớp phủ trắng gradient để chữ tối nổi rõ trên ảnh (kiểu Apple sáng) */
.spot-slide::before { content: ''; position: absolute; inset: 0;
  background: linear-gradient(180deg, rgba(255,255,255,.9), rgba(255,255,255,.55) 55%, rgba(255,255,255,.15)); }
.spot-text { position: relative; z-index: 1; max-width: 760px; padding-top: var(--sp-5); }
.eyebrow { display: block; color: var(--brand); font-size: 15px; font-weight: 600; margin-bottom: var(--sp-2); }
.spot-text h1 { font-size: clamp(2rem, 4.2vw, 3.4rem); font-weight: 700; letter-spacing: -0.03em; color: var(--text); margin-bottom: var(--sp-3); line-height: 1.08; }
.spot-text p { font-size: clamp(1rem, 1.6vw, 1.35rem); color: var(--text-2); margin-bottom: var(--sp-4); }
.spot-links { display: inline-flex; gap: var(--sp-4); flex-wrap: wrap; justify-content: center; align-items: center; }
.spot-link { display: inline-flex; align-items: center; gap: 6px; background: none; border: none; cursor: pointer; font-family: inherit; font-size: 1.05rem; color: var(--brand); font-weight: 500; transition: gap var(--ease); }
.spot-link:hover { gap: 10px; text-decoration: underline; }
.spot-link .pi { font-size: 12px; }
.spot-link.primary { background: var(--brand); color: #fff; padding: 10px 24px; border-radius: var(--radius-pill); }
.spot-link.primary:hover { background: var(--brand-dark); text-decoration: none; }

/* ===== Two-up band: 2 ô lớn chữ căn giữa trên, ảnh dưới (Apple two-tile) ===== */
.two-up { display: grid; grid-template-columns: 1fr 1fr; gap: var(--sp-4); margin-bottom: var(--sp-6); }
.tile {
  position: relative; border-radius: var(--radius-lg); overflow: hidden; cursor: pointer;
  background: var(--surface-2); border: 1px solid var(--border);
  min-height: 420px; display: flex; flex-direction: column; align-items: center; text-align: center;
  padding: var(--sp-6) var(--sp-4) 0; transition: box-shadow var(--ease), transform var(--ease);
}
.tile:hover { box-shadow: var(--shadow-hover); transform: translateY(-3px); }
.tile-text { position: relative; z-index: 1; }
.tile-text h3 { font-size: 1.9rem; font-weight: 700; letter-spacing: -0.02em; color: var(--text); margin-bottom: 6px; }
.tile-text p { color: var(--text-2); margin-bottom: var(--sp-3); }
.tile-link { display: inline-flex; align-items: center; gap: 5px; color: var(--brand); font-weight: 500; font-size: 1.05rem; }
.tile-link .pi { font-size: 12px; }
.tile-img { margin-top: auto; width: 100%; height: 210px; background-size: cover; background-position: center; }

/* Dải cam kết dịch vụ — icon nhiều màu */
.benefits { display: grid; grid-template-columns: repeat(4, 1fr); gap: var(--sp-3); margin-bottom: var(--sp-4); }
.benefit { display: flex; align-items: center; gap: var(--sp-3); background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius); padding: var(--sp-3) var(--sp-4); transition: box-shadow var(--ease), transform var(--ease); }
.benefit:hover { box-shadow: var(--shadow); transform: translateY(-2px); }
.bi { width: 44px; height: 44px; border-radius: 14px; display: grid; place-items: center; flex-shrink: 0; }
.bi .pi { font-size: 1.25rem; }
.bt { display: flex; flex-direction: column; line-height: 1.35; }
.bt strong { font-size: 13px; color: var(--text); }
.bt small { color: var(--text-muted); font-size: 11px; }

.vouchers { margin-bottom: var(--sp-4); }
.v-head, .flash-head { display: flex; align-items: center; gap: var(--sp-4); margin-bottom: var(--sp-4); }
.v-head h2, .flash-title { display: flex; align-items: center; gap: 8px; font-size: 1.35rem; font-weight: 700; letter-spacing: -0.02em; margin: 0; }
.v-head h2 .pi { color: var(--brand); }
.see-all { margin-left: auto; color: var(--brand); font-size: 13px; font-weight: 500; display: inline-flex; align-items: center; gap: 3px; }
.v-strip { display: flex; gap: var(--sp-3); overflow-x: auto; padding-bottom: 4px; }
.voucher { flex-shrink: 0; display: flex; width: 300px; border: 1px dashed var(--brand-100); border-radius: var(--radius); overflow: hidden; }
.v-left { background: var(--brand-50); color: var(--brand); padding: var(--sp-3); display: flex; flex-direction: column; justify-content: center; min-width: 120px; }
.v-val { font-size: 1.25rem; font-weight: 800; }
.v-min { font-size: 11px; color: var(--text-2); }
.v-right { flex: 1; padding: var(--sp-3); display: flex; flex-direction: column; justify-content: center; gap: 6px; }
.v-code { font-weight: 700; font-size: 13px; }
.v-save { background: var(--brand); color: #fff; border: none; border-radius: var(--radius-sm); padding: 5px 12px; cursor: pointer; font-family: inherit; font-size: 12px; align-self: flex-start; transition: background var(--ease); }
.v-save:not(.saved):hover { background: var(--brand-dark); }
.v-save.saved { background: var(--surface-2); color: var(--text-muted); cursor: default; }

/* Dải thương hiệu */
.brands { margin-bottom: var(--sp-6); }
.brands-title { font-size: 1.35rem; letter-spacing: -0.02em; margin-bottom: var(--sp-4); }
.brand-strip { display: flex; flex-wrap: wrap; gap: var(--sp-3); }
.brand-chip { display: flex; align-items: center; gap: 10px; background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius); padding: 10px 18px; cursor: pointer; transition: all var(--ease); font-family: inherit; }
.brand-chip:hover { border-color: var(--brand); box-shadow: var(--shadow); transform: translateY(-2px); }
.brand-logo { height: 28px; width: auto; max-width: 60px; object-fit: contain; }
.brand-name { font-size: 14px; font-weight: 600; color: var(--text); }

.flash { margin-bottom: var(--sp-6); overflow: hidden; }
/* Minimal: header flash-sale sạch, viền hairline dưới (bỏ dải gradient xanh) */
.flash-head { border-bottom: 1px solid var(--border);
  margin: calc(-1 * var(--sp-5)) calc(-1 * var(--sp-5)) var(--sp-5); padding: var(--sp-4) var(--sp-5); }
.flash-head .flash-title { color: var(--text); font-size: 1.35rem; letter-spacing: -0.02em; }
.flash-title .pi { color: var(--sale); }
.countdown { display: flex; align-items: center; gap: 4px; font-size: 13px; color: var(--text-2); }
.cd-label { color: var(--text-muted); margin-right: 4px; }
.cd-box { background: var(--text); color: #fff; padding: 2px 7px; border-radius: var(--radius-sm); font-weight: 700; font-variant-numeric: tabular-nums; }
.flash-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: var(--sp-3); }
.flash-card { cursor: pointer; border-radius: var(--radius); overflow: hidden; border: 1px solid var(--border); transition: box-shadow var(--ease), transform var(--ease), border-color var(--ease); }
.flash-card:hover { box-shadow: var(--shadow-hover); transform: translateY(-2px); border-color: var(--brand-100); }
.flash-card img { transition: transform 0.35s cubic-bezier(0.4, 0, 0.2, 1); }
.flash-card:hover img { transform: scale(1.05); }
/* Điện thoại nền trắng → contain */
.flash-card img { width: 100%; aspect-ratio: 1; object-fit: contain; background: #fff; padding: 6px; display: block; }
.fc-img { position: relative; }
.fc-badge {
  position: absolute; top: 6px; right: 6px; z-index: 1;
  background: var(--price); color: #fff; font-size: 11px; font-weight: 700;
  padding: 2px 6px; border-radius: var(--radius-sm); box-shadow: var(--shadow-sm);
}
.fc-name {
  font-size: 12px; color: var(--text); padding: 6px 8px 0;
  display: -webkit-box; -webkit-line-clamp: 1; line-clamp: 1; -webkit-box-orient: vertical;
  overflow: hidden;
}
.fc-prices { display: flex; align-items: baseline; gap: 6px; flex-wrap: wrap; padding: 4px 8px; }
.fc-prices .price { color: var(--price); font-weight: 700; font-size: 0.95rem; }
.fc-prices .line-through { color: var(--text-muted); font-size: 11px; }
.fc-bar { position: relative; height: 16px; background: var(--brand-100); border-radius: var(--radius-pill); margin: 0 8px 8px; overflow: hidden; }
.fc-fill { position: absolute; inset: 0; background: linear-gradient(90deg, #ffab00, var(--brand)); border-radius: var(--radius-pill); }
.fc-text { position: absolute; inset: 0; display: grid; place-items: center; font-size: 10px; color: #fff; font-weight: 600; text-shadow: 0 0 2px rgba(0,0,0,.3); }

.suggest-head { margin: var(--sp-8) 0 var(--sp-5); }
.center { display: flex; justify-content: center; padding: var(--sp-6); }
.more { text-align: center; margin: var(--sp-4) 0 var(--sp-6); }

/* Nút nổi: về đầu trang */
.to-top {
  position: fixed; right: 24px; bottom: 24px; z-index: 90;
  width: 46px; height: 46px; border-radius: 50%; border: none; cursor: pointer;
  background: var(--brand); color: #fff; box-shadow: var(--shadow-md);
  display: grid; place-items: center; transition: transform var(--ease), background var(--ease);
}
.to-top:hover { background: var(--brand-dark); transform: translateY(-3px); }
.to-top .pi { font-size: 1.1rem; }
.fade-enter-active, .fade-leave-active { transition: opacity var(--ease), transform var(--ease); }
.fade-enter-from, .fade-leave-to { opacity: 0; transform: translateY(10px); }

@media (max-width: 900px) {
  .benefits { grid-template-columns: 1fr 1fr; }
  .two-up { grid-template-columns: 1fr; }
  .spot-slide { min-height: 440px; }
}
@media (max-width: 640px) {
  .spot-slide { min-height: 380px; padding: var(--sp-6) var(--sp-3); }
  .tile { min-height: 340px; }
  .tile-img { height: 170px; }
  .see-all { display: none; }
  .bt small { display: none; }
}
</style>
