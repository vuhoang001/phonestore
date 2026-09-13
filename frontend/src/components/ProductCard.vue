<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import type { ProductListItem } from '@/types'
import { formatCurrency } from '@/composables/format'
import { useAuthStore } from '@/stores/auth'
import { wishlistApi } from '@/services'

// ProductListItem (từ /products) mang giá rẻ nhất trong basePrice (backend đã tính từ biến thể).
// installmentAvailable KHÔNG có trong list DTO → nhận qua prop rời (mặc định false) để card
// vẫn hiện badge "Trả góp" khi nơi gọi biết máy hỗ trợ (vd Home flash / trang chi tiết liên quan).
const props = defineProps<{ product: ProductListItem; installmentAvailable?: boolean }>()
const router = useRouter()
const toast = useToast()
const auth = useAuthStore()
const placeholder = 'https://placehold.co/300x300/f4f6fb/c8d0e0?text=No+Image'

const liked = ref(false)
// Nhãn suy ra từ dữ liệu thật (không bịa % giảm giá).
const isHot = computed(() => props.product.soldCount >= 500)
const isLoved = computed(() => !isHot.value && props.product.averageRating >= 4.5 && props.product.reviewCount >= 3)
const lowStock = computed(() => props.product.totalStock > 0 && props.product.totalStock <= 5)
// Flash Sale: chỉ tính là flash khi có giá flash & rẻ hơn giá gốc.
const onFlash = computed(() => props.product.flashPrice != null && props.product.flashPrice < props.product.basePrice)
// Giá hiển thị = giá flash nếu có, ngược lại basePrice (đã là giá biến thể rẻ nhất).
const showPrice = computed(() => onFlash.value ? props.product.flashPrice! : props.product.basePrice)
const flashPct = computed(() => onFlash.value
  ? Math.round((1 - props.product.flashPrice! / props.product.basePrice) * 100) : 0)
const soldText = computed(() => {
  const n = props.product.soldCount
  return n >= 1000 ? (n / 1000).toFixed(1).replace(/\.0$/, '') + 'k' : String(n)
})

function go() { router.push(`/products/${props.product.slug}`) }
async function toggleWish(e: Event) {
  e.stopPropagation()
  if (!auth.isAuthenticated) { router.push('/login'); return }
  liked.value = !liked.value
  try {
    await wishlistApi.toggle(props.product.id)
  } catch {
    liked.value = !liked.value
    toast.add({ severity: 'error', summary: 'Không cập nhật được yêu thích', life: 2000 })
  }
}
</script>

<template>
  <div class="card" role="link" tabindex="0" :aria-label="product.name" @click="go" @keydown.enter="go">
    <div class="thumb">
      <img :src="product.primaryImage || placeholder" :alt="product.name" loading="lazy" />

      <!-- Nhãn góc -->
      <span v-if="isHot" class="badge hot"><i class="pi pi-bolt" /> HOT</span>
      <span v-else-if="isLoved" class="badge loved"><i class="pi pi-star-fill" /> Yêu thích</span>

      <!-- Nút yêu thích (hiện khi hover) -->
      <button class="wish" :class="{ on: liked }" @click.stop="toggleWish" aria-label="Yêu thích">
        <i class="pi" :class="liked ? 'pi-heart-fill' : 'pi-heart'" />
      </button>

      <div v-if="product.totalStock === 0" class="soldout"><span>Hết hàng</span></div>
    </div>

    <div class="body">
      <span v-if="product.brandName" class="brand">{{ product.brandName }}</span>
      <h3 class="name">{{ product.name }}</h3>

      <div class="price-row">
        <span class="cur">₫</span><span class="price">{{ formatCurrency(showPrice).replace(/\s*₫/, '') }}</span>
        <template v-if="onFlash">
          <span class="old-price">{{ formatCurrency(product.basePrice).replace(/\s*₫/, '') }}</span>
          <span class="flash-pct"><i class="pi pi-bolt" /> -{{ flashPct }}%</span>
        </template>
      </div>

      <div class="tags" v-if="installmentAvailable || lowStock">
        <span v-if="installmentAvailable" class="chip inst"><i class="pi pi-calendar" /> Trả góp 0%</span>
        <span v-if="lowStock" class="chip low"><i class="pi pi-exclamation-circle" /> Sắp hết</span>
      </div>

      <div class="foot">
        <span class="rate"><i class="pi pi-star-fill" /> {{ product.averageRating || '—' }}
          <em v-if="product.reviewCount">({{ product.reviewCount }})</em></span>
        <span class="sold">Đã bán {{ soldText }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius);
  overflow: hidden;
  cursor: pointer;
  transition: box-shadow var(--ease), transform var(--ease), border-color var(--ease);
  display: flex; flex-direction: column;
}
/* Minimal: hover chỉ nhấc nhẹ + bóng khuếch tán, KHÔNG đổi viền sang xanh */
.card:hover { box-shadow: var(--shadow-hover); border-color: var(--border-strong); transform: translateY(-4px); }
.card:focus-visible { outline: 2px solid var(--brand); outline-offset: 2px; }

/* Ảnh điện thoại nền trắng → contain, nhiều khoảng thở kiểu premium */
.thumb { position: relative; aspect-ratio: 1; background: #fff; overflow: hidden; }
.thumb img { width: 100%; height: 100%; object-fit: contain; padding: 16px; transition: transform 0.5s cubic-bezier(0.4, 0, 0.2, 1); }
.card:hover .thumb img { transform: scale(1.05); }

/* Nhãn góc trái */
.badge {
  position: absolute; top: 8px; left: 8px; z-index: 2;
  display: inline-flex; align-items: center; gap: 3px;
  font-size: 10px; font-weight: 800; letter-spacing: 0.3px; color: #fff;
  padding: 3px 8px; border-radius: var(--radius-pill); box-shadow: var(--shadow-sm);
}
/* Minimal: nhãn góc solid, tiết chế (không gradient sặc sỡ) */
.badge.hot { background: var(--text); color: #fff; }
.badge.loved { background: #fff; color: var(--text); border: 1px solid var(--border); }
.badge.loved .pi { color: var(--star); }
.badge .pi { font-size: 10px; }

/* Nút yêu thích */
.wish {
  position: absolute; top: 6px; right: 6px; z-index: 2;
  width: 30px; height: 30px; border-radius: 50%; border: none; cursor: pointer;
  background: rgba(255, 255, 255, 0.92); color: var(--text-muted);
  display: grid; place-items: center; box-shadow: var(--shadow-sm);
  opacity: 0; transform: scale(0.8); transition: all var(--ease);
}
.card:hover .wish, .wish.on { opacity: 1; transform: scale(1); }
.wish:hover { color: var(--brand); }
.wish.on { color: var(--brand); }
.wish .pi { font-size: 14px; }

.soldout { position: absolute; inset: 0; display: grid; place-items: center; background: rgba(255, 255, 255, 0.65); z-index: 1; }
.soldout span { background: rgba(0, 0, 0, 0.6); color: #fff; font-size: 12px; padding: 4px 12px; border-radius: var(--radius-pill); }

.body { padding: var(--sp-4); display: flex; flex-direction: column; gap: var(--sp-2); flex: 1; }
.brand { font-size: 11px; font-weight: 600; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.04em; }
.name {
  font-size: 14px; font-weight: 500; margin: 0; line-height: 1.4; height: 2.8em;
  display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; color: var(--text);
}
.price-row { display: flex; align-items: baseline; gap: 1px; margin-top: auto; color: var(--price); flex-wrap: wrap; }
.cur { font-size: 12px; font-weight: 600; }
.price { font-size: 19px; font-weight: 700; letter-spacing: -0.02em; }
.old-price { font-size: 12px; color: var(--text-muted); text-decoration: line-through; font-weight: 500; margin-left: 5px; }
.flash-pct { display: inline-flex; align-items: center; gap: 2px; margin-left: auto; background: var(--sale); color: #fff; font-size: 10px; font-weight: 700; padding: 1px 5px; border-radius: var(--radius-sm); }
.flash-pct .pi { font-size: 9px; }

.tags { display: flex; flex-wrap: wrap; gap: 4px; }
.chip { display: inline-flex; align-items: center; gap: 3px; font-size: 10px; font-weight: 600; padding: 2px 7px; border-radius: var(--radius-sm); }
.chip .pi { font-size: 9px; }
.chip.inst { background: var(--brand-50); color: var(--brand); }
.chip.low { background: var(--c-red-bg); color: var(--c-red); }

.foot { display: flex; align-items: center; justify-content: space-between; font-size: 12px; color: var(--text-muted); }
.rate { display: inline-flex; align-items: center; gap: 3px; color: var(--text-2); font-weight: 600; }
.rate .pi-star-fill { color: var(--star); font-size: 11px; }
.rate em { font-style: normal; color: var(--text-muted); font-weight: 400; font-size: 11px; }
.sold { font-size: 11px; }
</style>
