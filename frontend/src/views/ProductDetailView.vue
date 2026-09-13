<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'
import Rating from 'primevue/rating'
import InputNumber from 'primevue/inputnumber'
import Textarea from 'primevue/textarea'
import Skeleton from 'primevue/skeleton'
import Image from 'primevue/image'
import ProductCard from '@/components/ProductCard.vue'
import { productApi, reviewApi, wishlistApi, uploadApi } from '@/services'
import type { ProductDetail, ProductVariant, Review, ProductListItem, ProductSpec } from '@/types'
import { formatCurrency, formatDate } from '@/composables/format'
import { useCartStore } from '@/stores/cart'
import { useAuthStore } from '@/stores/auth'
import { extractError } from '@/services/api'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const cart = useCartStore()
const auth = useAuthStore()

const product = ref<ProductDetail | null>(null)
const reviews = ref<Review[]>([])
const related = ref<ProductListItem[]>([])
const loading = ref(true)
const activeImage = ref<string>('')
const quantity = ref(1)
const reviewFilter = ref<number | null>(null) // lọc review theo số sao

// ----- Chọn biến thể theo Màu × Dung lượng -----
const selectedColor = ref<string | null>(null)
const selectedStorage = ref<string | null>(null)

const newReview = ref({ rating: 5, comment: '' })
const submittingReview = ref(false)
const reviewImages = ref<string[]>([])       // URL ảnh đã upload lên MinIO
const uploadingImg = ref(false)
const canReviewState = ref<{ canReview: boolean; reason: string } | null>(null)
const ratingLabel = ['', 'Rất tệ', 'Không hài lòng', 'Bình thường', 'Hài lòng', 'Tuyệt vời']

const placeholder = 'https://placehold.co/600x600/f4f6fb/c8d0e0?text=No+Image'

// Danh sách màu duy nhất (giữ colorHex đầu tiên gặp) và dung lượng duy nhất.
const colors = computed(() => {
  const map = new Map<string, string | undefined>()
  for (const v of product.value?.variants ?? []) {
    if (v.color && !map.has(v.color)) map.set(v.color, v.colorHex)
  }
  return Array.from(map, ([color, hex]) => ({ color, hex }))
})
const storages = computed(() => {
  const set = new Set<string>()
  for (const v of product.value?.variants ?? []) if (v.storage) set.add(v.storage)
  return Array.from(set)
})

// Biến thể khớp cả màu & dung lượng đang chọn (bỏ qua tiêu chí khi máy không có).
const selectedVariant = computed<ProductVariant | null>(() => {
  const vs = product.value?.variants ?? []
  if (!vs.length) return null
  return vs.find((v) =>
    (!colors.value.length || v.color === selectedColor.value) &&
    (!storages.value.length || v.storage === selectedStorage.value)
  ) ?? null
})

// Kiểm tra 1 màu có còn ít nhất 1 biến thể còn hàng (để làm mờ swatch hết hàng).
function colorAvailable(color: string) {
  return (product.value?.variants ?? []).some((v) => v.color === color && v.stockQuantity > 0)
}
// Với màu đang chọn, dung lượng có còn hàng không.
function storageAvailable(storage: string) {
  return (product.value?.variants ?? []).some((v) =>
    v.storage === storage && (!colors.value.length || v.color === selectedColor.value) && v.stockQuantity > 0)
}

const canBuy = computed(() => !!selectedVariant.value && selectedVariant.value.stockQuantity > 0)

// ----- Giá Flash Sale -----
const originalPrice = computed(() => selectedVariant.value?.price ?? product.value?.basePrice ?? 0)
const flashActive = computed(() =>
  product.value?.flashPrice != null && product.value.flashPrice < originalPrice.value)
const displayPrice = computed(() => flashActive.value ? product.value!.flashPrice! : originalPrice.value)
const flashDiscount = computed(() =>
  flashActive.value ? Math.round((1 - product.value!.flashPrice! / originalPrice.value) * 100) : 0)

// ----- Thông số kỹ thuật gom theo group -----
const specGroups = computed(() => {
  const groups: { group: string; rows: ProductSpec[] }[] = []
  for (const s of product.value?.specifications ?? []) {
    let g = groups.find((x) => x.group === (s.group || 'Khác'))
    if (!g) { g = { group: s.group || 'Khác', rows: [] }; groups.push(g) }
    g.rows.push(s)
  }
  return groups
})

const filteredReviews = computed(() =>
  reviewFilter.value ? reviews.value.filter((r) => r.rating === reviewFilter.value) : reviews.value)
const totalReviews = computed(() => reviews.value.length)

async function load() {
  loading.value = true
  try {
    const slug = route.params.slug as string
    product.value = await productApi.bySlug(slug)
    // Chọn mặc định theo biến thể đầu tiên.
    const first = product.value.variants[0]
    selectedColor.value = first?.color ?? null
    selectedStorage.value = first?.storage ?? null
    activeImage.value = product.value.images.find((i) => i.isPrimary)?.url
      || product.value.images[0]?.url || placeholder
    reviewFilter.value = null
    const [rev, rel] = await Promise.all([
      productApi.reviews(product.value.id),
      productApi.related(product.value.id)
    ])
    reviews.value = rev
    related.value = rel
    canReviewState.value = null
    if (auth.isAuthenticated) {
      try { canReviewState.value = await reviewApi.canReview(product.value.id) } catch { /* bỏ qua */ }
    }
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    loading.value = false
  }
}

// Chọn màu: nếu dung lượng đang chọn không có với màu mới → nhảy sang dung lượng còn hàng đầu tiên.
function pickColor(color: string) {
  selectedColor.value = color
  if (selectedStorage.value && !storageAvailable(selectedStorage.value)) {
    const alt = storages.value.find((s) => storageAvailable(s))
    if (alt) selectedStorage.value = alt
  }
}
function pickStorage(storage: string) { selectedStorage.value = storage }

// Upload ảnh đánh giá lên MinIO, lưu URL trả về.
async function onPickImages(e: Event) {
  const input = e.target as HTMLInputElement
  const files = Array.from(input.files || [])
  input.value = ''
  if (!files.length) return
  if (reviewImages.value.length + files.length > 5) {
    toast.add({ severity: 'warn', summary: 'Tối đa 5 ảnh mỗi đánh giá', life: 2500 }); return
  }
  uploadingImg.value = true
  try {
    for (const f of files) {
      const { url } = await uploadApi.reviewImage(f)
      reviewImages.value.push(url)
    }
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Tải ảnh thất bại', detail: extractError(err), life: 3000 })
  } finally {
    uploadingImg.value = false
  }
}
function removeReviewImage(i: number) { reviewImages.value.splice(i, 1) }

async function addToCart() {
  if (!selectedVariant.value) return
  try {
    await cart.add(selectedVariant.value.id, quantity.value)
    toast.add({ severity: 'success', summary: 'Đã thêm vào giỏ', life: 2000 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  }
}

async function buyNow() {
  if (!auth.isAuthenticated) {
    router.push({ name: 'login', query: { redirect: route.fullPath } })
    return
  }
  if (!selectedVariant.value) return
  try {
    await cart.add(selectedVariant.value.id, quantity.value)
    const item = cart.cart?.items.find((i) => i.variantId === selectedVariant.value!.id)
    cart.selectedIds = item ? [item.id] : []
    router.push('/checkout')
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  }
}

async function toggleWishlist() {
  if (!auth.isAuthenticated) { router.push('/login'); return }
  try {
    await wishlistApi.toggle(product.value!.id)
    toast.add({ severity: 'success', summary: 'Đã cập nhật yêu thích', life: 2000 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  }
}

async function submitReview() {
  submittingReview.value = true
  try {
    await reviewApi.create({
      productId: product.value!.id,
      rating: newReview.value.rating,
      comment: newReview.value.comment,
      images: reviewImages.value
    })
    toast.add({ severity: 'success', summary: 'Cảm ơn đánh giá của bạn', life: 2000 })
    newReview.value = { rating: 5, comment: '' }
    reviewImages.value = []
    reviews.value = await productApi.reviews(product.value!.id)
    canReviewState.value = { canReview: false, reason: 'Bạn đã đánh giá sản phẩm này rồi. Cảm ơn bạn!' }
  } catch (e) {
    toast.add({ severity: 'warn', summary: 'Không thể đánh giá', detail: extractError(e), life: 4000 })
  } finally {
    submittingReview.value = false
  }
}

onMounted(load)
watch(() => route.params.slug, load)
</script>

<template>
  <div v-if="loading" class="detail">
    <div class="gallery">
      <Skeleton height="0" class="sk-main" />
      <div class="sk-thumbs"><Skeleton v-for="n in 4" :key="n" width="64px" height="64px" /></div>
    </div>
    <div class="info sk-info">
      <Skeleton width="40%" height="0.8rem" />
      <Skeleton width="80%" height="1.6rem" class="mt-2" />
      <Skeleton width="55%" height="1rem" class="mt-2" />
      <Skeleton width="35%" height="2.4rem" class="mt-3" />
      <Skeleton width="100%" height="2.6rem" class="mt-3" />
      <Skeleton width="90%" height="1rem" class="mt-3" />
      <Skeleton width="85%" height="1rem" />
    </div>
  </div>
  <div v-else-if="product" class="detail">
    <div class="gallery">
      <img :src="activeImage" :alt="product.name" class="main-img" />
      <div class="thumbs">
        <img v-for="img in product.images" :key="img.id" :src="img.url"
          :class="{ active: activeImage === img.url }" @click="activeImage = img.url" />
      </div>
    </div>

    <div class="info">
      <nav class="breadcrumb">
        <router-link to="/">Trang chủ</router-link>
        <i class="pi pi-angle-right" />
        <router-link :to="{ name: 'products', query: { brandId: product.brandId } }">{{ product.brandName }}</router-link>
        <i class="pi pi-angle-right" />
        <span class="current">{{ product.name }}</span>
      </nav>

      <div class="brand-line">
        <span class="brand-tag">{{ product.brandName }}</span>
        <span class="genuine"><i class="pi pi-verified" /> Chính hãng</span>
        <span v-if="product.installmentAvailable" class="inst-tag"><i class="pi pi-calendar" /> Trả góp 0%</span>
      </div>
      <h1>{{ product.name }}</h1>

      <div class="rating-row">
        <span class="rate-num">{{ product.averageRating || '—' }}</span>
        <Rating :modelValue="product.averageRating" readonly />
        <span class="sep" />
        <span class="stat"><b>{{ product.reviewCount }}</b> đánh giá</span>
        <span class="sep" />
        <span class="stat"><b>{{ product.soldCount }}</b> đã bán</span>
        <span class="sep" />
        <span class="stat muted"><i class="pi pi-eye" /> {{ product.viewCount }} lượt xem</span>
        <span class="sep" />
        <span class="stat muted"><i class="pi pi-verified" /> BH {{ product.warrantyMonths }} tháng</span>
      </div>

      <!-- Giá -->
      <div class="price-band" :class="{ flash: flashActive }">
        <div class="price-line">
          <span class="price-now">{{ formatCurrency(displayPrice) }}</span>
          <template v-if="flashActive">
            <span class="price-old">{{ formatCurrency(originalPrice) }}</span>
            <span class="flash-badge"><i class="pi pi-bolt" /> -{{ flashDiscount }}%</span>
          </template>
        </div>
        <div v-if="flashActive" class="flash-note">
          <i class="pi pi-bolt" /> Đang trong Flash Sale
          <span v-if="product.flashEndAt"> · kết thúc {{ formatDate(product.flashEndAt) }}</span>
        </div>
      </div>

      <!-- Chọn màu (swatch theo colorHex) -->
      <div v-if="colors.length" class="opt-row">
        <span class="opt-label">Màu sắc</span>
        <div class="opt-choices">
          <button v-for="c in colors" :key="c.color" class="swatch"
            :class="{ active: selectedColor === c.color, disabled: !colorAvailable(c.color) }"
            :disabled="!colorAvailable(c.color)" :title="c.color" @click="pickColor(c.color)">
            <span class="dot" :style="{ background: c.hex || '#ccc' }" />
            <span class="sw-name">{{ c.color }}</span>
            <i v-if="selectedColor === c.color" class="pi pi-check" />
          </button>
        </div>
      </div>

      <!-- Chọn dung lượng (chip) -->
      <div v-if="storages.length" class="opt-row">
        <span class="opt-label">Dung lượng</span>
        <div class="opt-choices">
          <button v-for="s in storages" :key="s" class="storage-btn"
            :class="{ active: selectedStorage === s, disabled: !storageAvailable(s) }"
            :disabled="!storageAvailable(s)" @click="pickStorage(s)">
            {{ s }}
          </button>
        </div>
      </div>

      <!-- Số lượng + tồn kho -->
      <div class="opt-row">
        <span class="opt-label">Số lượng</span>
        <InputNumber v-model="quantity" :min="1" :max="selectedVariant?.stockQuantity || 1" showButtons buttonLayout="horizontal" class="qty-input" />
        <span class="stock-inline" :class="{ out: !canBuy }">
          <i class="pi" :class="canBuy ? 'pi-check-circle' : 'pi-times-circle'" />
          {{ canBuy ? `Còn ${selectedVariant?.stockQuantity} sản phẩm` : 'Hết hàng' }}
        </span>
      </div>

      <!-- Nút mua -->
      <div class="buy-row">
        <Button label="Thêm vào giỏ" icon="pi pi-shopping-cart" outlined class="btn-cart" :disabled="!canBuy" @click="addToCart" />
        <Button label="Mua ngay" class="btn-buy" :disabled="!canBuy" @click="buyNow" />
        <button class="wish-btn" @click="toggleWishlist" v-tooltip.top="'Yêu thích'"><i class="pi pi-heart" /></button>
      </div>

      <!-- Trả góp -->
      <div v-if="product.installmentAvailable && product.installmentOptions.length" class="installment">
        <div class="inst-head"><i class="pi pi-calendar" /> Mua trả góp 0% lãi suất</div>
        <div class="inst-grid">
          <div v-for="o in product.installmentOptions" :key="o.months" class="inst-card">
            <span class="inst-mo">{{ o.months }} tháng</span>
            <span class="inst-price">{{ formatCurrency(o.monthly) }}<small>/tháng</small></span>
          </div>
        </div>
        <p class="inst-hint">Số tiền/tháng là ước tính. Chọn "Trả góp" ở bước thanh toán để hoàn tất.</p>
      </div>

      <div class="info-block">
        <h3 class="blk-title">Mô tả sản phẩm</h3>
        <p class="desc">{{ product.description || 'Chưa có mô tả.' }}</p>
      </div>

      <!-- Thông số kỹ thuật gom theo group -->
      <div v-if="specGroups.length" class="info-block">
        <h3 class="blk-title">Thông số kỹ thuật</h3>
        <div v-for="g in specGroups" :key="g.group" class="spec-group">
          <div class="spec-group-name">{{ g.group }}</div>
          <table class="attrs">
            <tr v-for="(r, i) in g.rows" :key="i">
              <td class="attr-name">{{ r.name }}</td>
              <td>{{ r.value }}</td>
            </tr>
          </table>
        </div>
      </div>
    </div>
  </div>

  <!-- Reviews -->
  <section v-if="product" class="reviews">
    <h2 class="section-title">Đánh giá ({{ reviews.length }})</h2>

    <!-- Chưa đăng nhập -->
    <div v-if="!auth.isAuthenticated" class="rv-note">
      <span class="rv-ic"><i class="pi pi-user-edit" /></span>
      <div class="rv-txt"><strong>Đăng nhập để đánh giá</strong><p>Chia sẻ trải nghiệm của bạn về sản phẩm này.</p></div>
      <Button label="Đăng nhập" icon="pi pi-sign-in" size="small"
        @click="router.push({ name: 'login', query: { redirect: route.fullPath } })" />
    </div>

    <!-- Đã đăng nhập nhưng chưa đủ điều kiện -->
    <div v-else-if="canReviewState && !canReviewState.canReview" class="rv-note locked">
      <span class="rv-ic"><i class="pi pi-lock" /></span>
      <div class="rv-txt"><strong>Chưa thể đánh giá</strong><p>{{ canReviewState.reason }}</p></div>
    </div>

    <!-- Form đầy đủ -->
    <div v-else-if="canReviewState?.canReview" class="review-form">
      <div class="rf-head"><i class="pi pi-pencil" /> Viết đánh giá của bạn</div>
      <div class="rf-rate">
        <span class="rf-label">Chất lượng:</span>
        <Rating v-model="newReview.rating" />
        <span class="rf-rate-txt">{{ ratingLabel[newReview.rating] }}</span>
      </div>
      <Textarea v-model="newReview.comment" rows="4" autoResize class="w-full"
        placeholder="Máy có đúng mô tả không? Pin, camera, hiệu năng thế nào? Chia sẻ để mọi người tham khảo nhé..." />

      <!-- Ảnh đánh giá -->
      <div class="rf-imgs">
        <div v-for="(img, i) in reviewImages" :key="img" class="rf-thumb">
          <img :src="img" alt="ảnh đánh giá" />
          <button class="rf-del" @click="removeReviewImage(i)" aria-label="Xoá ảnh"><i class="pi pi-times" /></button>
        </div>
        <label v-if="reviewImages.length < 5" class="rf-add" :class="{ busy: uploadingImg }">
          <input type="file" accept="image/*" multiple hidden :disabled="uploadingImg" @change="onPickImages" />
          <i class="pi" :class="uploadingImg ? 'pi-spin pi-spinner' : 'pi-camera'" />
          <span>{{ uploadingImg ? 'Đang tải...' : 'Thêm ảnh' }}</span>
        </label>
      </div>

      <div class="rf-actions">
        <Button label="Gửi đánh giá" icon="pi pi-send" :loading="submittingReview" :disabled="uploadingImg" @click="submitReview" />
        <small class="text-muted">Ảnh thật giúp người mua sau tin tưởng hơn (tối đa 5 ảnh).</small>
      </div>
    </div>

    <!-- Đang kiểm tra điều kiện -->
    <div v-else class="rv-note"><span class="rv-ic"><i class="pi pi-spin pi-spinner" /></span>
      <div class="rv-txt"><p>Đang kiểm tra điều kiện đánh giá…</p></div>
    </div>

    <!-- Tổng quan đánh giá + lọc theo sao -->
    <div v-if="product && totalReviews" class="rating-summary">
      <div class="avg-box">
        <div class="avg-num">{{ product.averageRating }}<small>/5</small></div>
        <Rating :modelValue="product.averageRating" readonly />
        <div class="text-muted">{{ totalReviews }} đánh giá</div>
      </div>
      <div class="breakdown">
        <button v-for="(count, i) in product.ratingBreakdown" :key="i" class="bd-row"
          @click="reviewFilter = reviewFilter === 5 - i ? null : 5 - i" :class="{ active: reviewFilter === 5 - i }">
          <span class="bd-star">{{ 5 - i }} <i class="pi pi-star-fill" /></span>
          <span class="bar"><span class="fill" :style="{ width: (totalReviews ? count / totalReviews * 100 : 0) + '%' }" /></span>
          <span class="bd-count">{{ count }}</span>
        </button>
      </div>
    </div>

    <div class="filter-chips" v-if="totalReviews">
      <button class="chip" :class="{ active: reviewFilter === null }" @click="reviewFilter = null">Tất cả</button>
      <button v-for="s in [5,4,3,2,1]" :key="s" class="chip" :class="{ active: reviewFilter === s }"
        @click="reviewFilter = reviewFilter === s ? null : s">{{ s }} sao</button>
    </div>

    <div v-if="filteredReviews.length" class="review-list">
      <div v-for="r in filteredReviews" :key="r.id" class="review-item">
        <div class="review-head">
          <span class="rv-avatar">{{ (r.userName || '?').charAt(0).toUpperCase() }}</span>
          <div class="rv-meta">
            <strong>{{ r.userName }}</strong>
            <div class="rv-sub"><Rating :modelValue="r.rating" readonly /><span class="text-muted">{{ formatDate(r.createdAt) }}</span></div>
          </div>
        </div>
        <p v-if="r.comment" class="rv-comment">{{ r.comment }}</p>
        <div v-if="r.images.length" class="review-imgs">
          <Image v-for="(img, i) in r.images" :key="i" :src="img" imageClass="rv-photo" preview />
        </div>
      </div>
    </div>
    <p v-else class="text-muted">{{ reviewFilter ? 'Không có đánh giá ' + reviewFilter + ' sao.' : 'Chưa có đánh giá nào.' }}</p>
  </section>

  <!-- Sản phẩm liên quan -->
  <section v-if="related.length" class="related">
    <h2 class="section-title">Sản phẩm liên quan</h2>
    <div class="grid-products">
      <ProductCard v-for="p in related" :key="p.id" :product="p" />
    </div>
  </section>
</template>

<style scoped>
.center { display: flex; justify-content: center; padding: 4rem; }
.detail { display: grid; grid-template-columns: 460px 1fr; gap: 2rem; background: var(--surface); border: 1px solid var(--border); box-shadow: var(--shadow-sm); padding: var(--sp-5); border-radius: var(--radius-lg); }
.sk-main { aspect-ratio: 1; width: 100%; border-radius: var(--radius-lg); }
.sk-thumbs { display: flex; gap: 8px; margin-top: 12px; }
.sk-info { display: flex; flex-direction: column; gap: 8px; }
.mt-2 { margin-top: 8px; } .mt-3 { margin-top: 12px; }
/* Điện thoại nền trắng → contain để không cắt máy */
.main-img { width: 100%; aspect-ratio: 1; object-fit: contain; border-radius: var(--radius-lg); background: #fff; border: 1px solid var(--border); padding: var(--sp-4); }
.thumbs { display: flex; gap: 0.5rem; margin-top: 0.75rem; flex-wrap: wrap; }
.thumbs img { width: 64px; height: 64px; object-fit: contain; background: #fff; padding: 4px; border-radius: var(--radius-sm); cursor: pointer; border: 2px solid var(--border); transition: border-color var(--ease); }
.thumbs img:hover { border-color: var(--brand-100); }
.thumbs img.active { border-color: var(--brand); }
.breadcrumb { display: flex; align-items: center; gap: 6px; font-size: 13px; color: var(--text-muted); flex-wrap: wrap; }
.breadcrumb a { color: var(--text-2); }
.breadcrumb a:hover { color: var(--brand); }
.breadcrumb .pi { font-size: 11px; }
.breadcrumb .current { color: var(--text); overflow: hidden; text-overflow: ellipsis; max-width: 260px; white-space: nowrap; }

.brand-line { display: flex; align-items: center; gap: var(--sp-2); flex-wrap: wrap; margin-top: 0.6rem; }
.brand-tag { font-size: 12px; font-weight: 700; color: var(--brand); background: var(--brand-50); padding: 3px 10px; border-radius: var(--radius-pill); text-transform: uppercase; letter-spacing: 0.02em; }
.genuine { display: inline-flex; align-items: center; gap: 4px; font-size: 12px; font-weight: 600; color: var(--success); }
.genuine .pi { font-size: 12px; }
.inst-tag { display: inline-flex; align-items: center; gap: 4px; font-size: 12px; font-weight: 600; color: var(--brand); }
.inst-tag .pi { font-size: 11px; }

h1 { margin: 0.35rem 0 0.7rem; font-size: 1.45rem; line-height: 1.35; }
.rating-row { display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; font-size: 14px; }
.rate-num { color: var(--brand); font-weight: 700; border-bottom: 1.5px solid var(--brand); line-height: 1.1; }
.rating-row .sep { width: 1px; height: 14px; background: var(--border-strong); }
.rating-row .stat { color: var(--text-2); } .rating-row .stat b { color: var(--text); }
.rating-row .stat.muted { color: var(--text-muted); }
.rating-row :deep(.p-rating) { gap: 2px; }

.price-band { margin: 0.85rem 0 1.15rem; padding-bottom: 1.1rem; border-bottom: 1px solid var(--border); }
.price-line { display: flex; align-items: baseline; gap: var(--sp-3); flex-wrap: wrap; }
.price-now { font-size: 2.1rem; font-weight: 800; color: var(--price); letter-spacing: -0.5px; line-height: 1; }
.price-old { font-size: 1.1rem; color: var(--text-muted); text-decoration: line-through; font-weight: 500; }
.flash-badge { display: inline-flex; align-items: center; gap: 3px; background: var(--price); color: #fff; font-weight: 700; font-size: 0.85rem; padding: 3px 8px; border-radius: var(--radius-pill); }
.flash-note { margin-top: var(--sp-2); display: inline-flex; align-items: center; gap: 5px; color: var(--price); font-weight: 600; font-size: 0.85rem; }
.flash-note .pi { font-size: 12px; }

.opt-row { display: flex; align-items: center; gap: 14px; margin: 0.85rem 0; flex-wrap: wrap; }
.opt-label { width: 88px; flex-shrink: 0; color: var(--text-muted); font-size: 14px; }
.opt-choices { display: flex; flex-wrap: wrap; gap: 8px; }
.qty-input :deep(.p-inputnumber-input) { width: 3.2rem; text-align: center; }
.stock-inline { display: inline-flex; align-items: center; gap: 5px; font-size: 13px; color: var(--success); font-weight: 500; }
.stock-inline .pi { font-size: 14px; }
.stock-inline.out { color: var(--danger); }

/* Swatch màu */
.swatch { display: inline-flex; align-items: center; gap: 7px; border: 1px solid var(--border-strong); background: var(--surface); border-radius: var(--radius-sm); padding: 6px 12px 6px 8px; cursor: pointer; font-family: inherit; font-size: 13px; color: var(--text-2); transition: all var(--ease); }
.swatch:not(.disabled):hover { border-color: var(--brand); color: var(--brand); }
.swatch.active { border-color: var(--brand); color: var(--brand); font-weight: 600; background: var(--brand-50); }
.swatch .dot { width: 18px; height: 18px; border-radius: 50%; border: 1px solid rgba(0,0,0,.15); flex-shrink: 0; box-shadow: inset 0 0 0 2px #fff; }
.swatch .pi { font-size: 11px; }
.swatch.disabled { opacity: 0.4; cursor: not-allowed; text-decoration: line-through; }

/* Chip dung lượng */
.storage-btn { border: 1px solid var(--border-strong); background: var(--surface); border-radius: var(--radius-sm); padding: 7px 16px; cursor: pointer; font-family: inherit; font-size: 13px; font-weight: 600; color: var(--text-2); transition: all var(--ease); }
.storage-btn:not(.disabled):hover { border-color: var(--brand); color: var(--brand); }
.storage-btn.active { border-color: var(--brand); color: var(--brand); background: var(--brand-50); }
.storage-btn.disabled { opacity: 0.4; cursor: not-allowed; text-decoration: line-through; }

.buy-row { display: flex; gap: 10px; align-items: stretch; margin: 1.3rem 0; flex-wrap: wrap; }
.buy-row .btn-cart, .buy-row .btn-buy { flex: 1; min-width: 150px; justify-content: center; padding-top: 0.7rem; padding-bottom: 0.7rem; }
.buy-row .btn-buy { font-size: 15px; box-shadow: 0 4px 12px rgba(30,111,255,.28); }
.wish-btn { width: 46px; flex-shrink: 0; border: 1px solid var(--border-strong); background: var(--surface); border-radius: var(--radius); cursor: pointer; color: var(--text-2); font-size: 1.1rem; transition: all var(--ease); }
.wish-btn:hover { border-color: var(--brand); color: var(--brand); background: var(--brand-50); }

/* Khối trả góp */
.installment { border: 1px solid var(--brand-100); background: var(--brand-50); border-radius: var(--radius-lg); padding: var(--sp-4); margin: var(--sp-4) 0; }
.inst-head { display: flex; align-items: center; gap: 7px; font-weight: 700; color: var(--brand-dark); margin-bottom: var(--sp-3); }
.inst-head .pi { color: var(--brand); }
.inst-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(140px, 1fr)); gap: var(--sp-2); }
.inst-card { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius); padding: var(--sp-3); display: flex; flex-direction: column; gap: 3px; }
.inst-mo { font-size: 12px; color: var(--text-muted); font-weight: 600; }
.inst-price { font-size: 1.05rem; font-weight: 800; color: var(--price); }
.inst-price small { font-size: 11px; font-weight: 500; color: var(--text-muted); }
.inst-hint { margin: var(--sp-3) 0 0; font-size: 12px; color: var(--text-2); }

.info-block { border-top: 1px solid var(--border); padding-top: var(--sp-4); margin-top: var(--sp-4); }
.blk-title { font-size: 1rem; font-weight: 700; margin: 0 0 var(--sp-3); padding-left: 10px; border-left: 3px solid var(--brand); line-height: 1.2; }
.desc { white-space: pre-line; line-height: 1.7; color: var(--text-2); }

/* Bảng thông số gom nhóm */
.spec-group + .spec-group { margin-top: var(--sp-4); }
.spec-group-name { font-size: 13px; font-weight: 700; color: var(--brand-dark); background: var(--brand-50); padding: 7px 14px; border-radius: var(--radius-sm) var(--radius-sm) 0 0; }
.attrs { border-collapse: collapse; width: 100%; }
.attrs tr:nth-child(odd) { background: var(--surface-2); }
.attrs td { padding: 10px 14px; font-size: 14px; border-bottom: 1px solid var(--border); }
.attr-name { font-weight: 500; width: 180px; color: var(--text-muted); }

.reviews { background: var(--surface); border: 1px solid var(--border); box-shadow: var(--shadow-sm); padding: var(--sp-5); border-radius: var(--radius-lg); margin-top: 1.5rem; }
.rv-note { display: flex; align-items: center; gap: var(--sp-3); background: var(--brand-50);
  border: 1px solid var(--brand-100); border-radius: var(--radius); padding: var(--sp-4); margin-bottom: 1.5rem; }
.rv-note.locked { background: var(--surface-2); border-color: var(--border); }
.rv-ic { width: 44px; height: 44px; border-radius: 12px; display: grid; place-items: center;
  background: #fff; color: var(--brand); flex-shrink: 0; box-shadow: var(--shadow-sm); }
.rv-note.locked .rv-ic { color: var(--text-muted); }
.rv-ic .pi { font-size: 1.25rem; }
.rv-txt { flex: 1; min-width: 0; }
.rv-txt strong { display: block; color: var(--text); }
.rv-txt p { margin: 2px 0 0; color: var(--text-muted); font-size: 13px; }

.review-form { border: 1px solid var(--border); border-radius: var(--radius-lg); padding: var(--sp-5);
  margin-bottom: 1.5rem; background: var(--surface); box-shadow: var(--shadow-sm);
  border-top: 3px solid var(--brand); }
.rf-head { display: flex; align-items: center; gap: 8px; font-weight: 700; font-size: 1.05rem; margin-bottom: var(--sp-4); }
.rf-head .pi { color: var(--brand); }
.rf-rate { display: flex; align-items: center; gap: var(--sp-3); margin-bottom: var(--sp-3); flex-wrap: wrap; }
.rf-label { font-weight: 600; color: var(--text-2); }
.rf-rate-txt { color: var(--brand); font-weight: 700; font-size: 14px; }
.rf-rate :deep(.p-rating) { gap: 4px; }
.rf-rate :deep(.p-rating .p-icon) { width: 1.5rem; height: 1.5rem; }

.rf-imgs { display: flex; flex-wrap: wrap; gap: var(--sp-3); margin-top: var(--sp-3); }
.rf-thumb { position: relative; width: 80px; height: 80px; border-radius: var(--radius); overflow: hidden; border: 1px solid var(--border); }
.rf-thumb img { width: 100%; height: 100%; object-fit: cover; }
.rf-del { position: absolute; top: 2px; right: 2px; width: 20px; height: 20px; border: none; border-radius: 50%;
  background: rgba(0,0,0,.6); color: #fff; cursor: pointer; display: grid; place-items: center; font-size: 10px; }
.rf-del:hover { background: var(--brand); }
.rf-add { width: 80px; height: 80px; border: 1.5px dashed var(--border-strong); border-radius: var(--radius);
  display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 4px;
  cursor: pointer; color: var(--text-muted); font-size: 11px; transition: all var(--ease); }
.rf-add:hover { border-color: var(--brand); color: var(--brand); background: var(--brand-50); }
.rf-add.busy { pointer-events: none; opacity: 0.7; }
.rf-add .pi { font-size: 1.2rem; }
.rf-actions { display: flex; align-items: center; gap: var(--sp-4); margin-top: var(--sp-4); flex-wrap: wrap; }

.rating-summary { display: flex; align-items: center; gap: var(--sp-6); border: 1px solid var(--border); border-radius: var(--radius-lg); padding: var(--sp-5); margin-bottom: var(--sp-4); flex-wrap: wrap; }
.avg-box { text-align: center; min-width: 128px; padding-right: var(--sp-6); border-right: 1px solid var(--border); }
.avg-num { font-size: 2.6rem; font-weight: 800; color: var(--brand); line-height: 1; }
.avg-num small { font-size: 0.95rem; color: var(--text-muted); font-weight: 600; }
.avg-box :deep(.p-rating) { justify-content: center; gap: 2px; margin: 8px 0 6px; }
.avg-box .text-muted { font-size: 13px; }
.breakdown { flex: 1; display: flex; flex-direction: column; gap: 7px; min-width: 240px; }
.bd-row { display: flex; align-items: center; gap: 12px; background: none; border: none; cursor: pointer; padding: 2px 4px; border-radius: var(--radius-sm); font-family: inherit; transition: background var(--ease); }
.bd-row:hover { background: var(--surface-2); }
.bd-row.active { background: var(--brand-50); }
.bd-star { display: inline-flex; align-items: center; gap: 3px; font-size: 13px; width: 38px; color: var(--text-2); flex-shrink: 0; }
.bd-star .pi { color: var(--star); font-size: 11px; }
.bar { flex: 1; height: 9px; background: var(--border); border-radius: var(--radius-pill); overflow: hidden; }
.fill { display: block; height: 100%; background: linear-gradient(90deg, #ffd54f, #ffb300); border-radius: var(--radius-pill); }
.bd-count { font-size: 13px; color: var(--text-muted); width: 24px; text-align: right; flex-shrink: 0; }
.filter-chips { display: flex; gap: var(--sp-2); flex-wrap: wrap; margin-bottom: var(--sp-4); }
.chip { background: var(--surface); border: 1px solid var(--border); color: var(--text-2); padding: 5px 14px; border-radius: var(--radius-pill); cursor: pointer; font-family: inherit; font-size: 13px; transition: all var(--ease); }
.chip:hover { border-color: var(--brand); color: var(--brand); }
.chip.active { background: var(--brand); border-color: var(--brand); color: #fff; }
.related { margin-top: var(--sp-4); }

.review-item { padding: var(--sp-4) 0; border-bottom: 1px solid var(--border); }
.review-item:last-child { border-bottom: none; }
.review-head { display: flex; align-items: center; gap: var(--sp-3); margin-bottom: var(--sp-2); }
.rv-avatar { width: 40px; height: 40px; border-radius: 50%; flex-shrink: 0; display: grid; place-items: center;
  background: var(--brand-100); color: var(--brand-dark); font-weight: 700; }
.rv-meta strong { display: block; font-size: 14px; }
.rv-sub { display: flex; align-items: center; gap: var(--sp-3); }
.rv-sub .text-muted { font-size: 12px; }
.rv-comment { margin: 0 0 var(--sp-2); color: var(--text-2); line-height: 1.6; }
.review-imgs { display: flex; flex-wrap: wrap; gap: var(--sp-2); margin-top: var(--sp-2); }
.review-imgs :deep(.rv-photo) { width: 84px; height: 84px; object-fit: cover; border-radius: var(--radius);
  border: 1px solid var(--border); cursor: zoom-in; transition: transform var(--ease); }
.review-imgs :deep(.rv-photo):hover { transform: scale(1.04); }
@media (max-width: 900px) { .detail { grid-template-columns: 1fr; } }
</style>
