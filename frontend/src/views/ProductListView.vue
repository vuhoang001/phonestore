<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import Select from 'primevue/select'
import InputNumber from 'primevue/inputnumber'
import Button from 'primevue/button'
import ProductCard from '@/components/ProductCard.vue'
import ProductSkeleton from '@/components/ProductSkeleton.vue'
import { productApi, categoryApi, brandsApi, type ProductFilter } from '@/services'
import type { ProductListItem, Category, Brand } from '@/types'
import { formatCurrency } from '@/composables/format'

const route = useRoute()

const products = ref<ProductListItem[]>([])
const tree = ref<Category[]>([])
const brands = ref<Brand[]>([])
const loading = ref(false)      // tải trang đầu / đổi bộ lọc → hiện skeleton
const loadingMore = ref(false)  // đang nối trang tiếp theo (infinite scroll)
const showTop = ref(false)      // hiện nút "về đầu trang"
const total = ref(0)
const showFilters = ref(false) // mobile toggle
const expanded = ref<Set<number>>(new Set()) // danh mục cha đang mở (tree)

// Bộ lọc dung lượng ở phía client (ProductListItem không mang biến thể) —
// khớp theo tên máy (thường có "128GB", "256GB"...).
const storageOptions = ['64GB', '128GB', '256GB', '512GB', '1TB']
const storageSel = ref<string | undefined>(undefined)

const filter = ref<ProductFilter>({
  keyword: (route.query.keyword as string) || undefined,
  categoryId: route.query.categoryId ? Number(route.query.categoryId) : undefined,
  brandId: route.query.brandId ? Number(route.query.brandId) : undefined,
  minPrice: undefined,
  maxPrice: undefined,
  minRating: undefined,
  sortBy: 'newest',
  page: 1,
  pageSize: 12
})

const sortOptions = [
  { label: 'Mới nhất', value: 'newest' },
  { label: 'Bán chạy', value: 'rating' },
  { label: 'Giá thấp → cao', value: 'price_asc' },
  { label: 'Giá cao → thấp', value: 'price_desc' }
]

const pricePresets = [
  { label: 'Dưới 5 triệu', min: undefined, max: 5000000 },
  { label: '5 - 15 triệu', min: 5000000, max: 15000000 },
  { label: '15 - 25 triệu', min: 15000000, max: 25000000 },
  { label: 'Trên 25 triệu', min: 25000000, max: undefined }
]
const ratingPresets = [
  { label: 'Từ 4 sao', value: 4 },
  { label: 'Từ 3 sao', value: 3 }
]

// Map id -> tên danh mục (để hiện chip)
const catName = computed(() => {
  const m: Record<number, string> = {}
  for (const p of tree.value) { m[p.id] = p.name; for (const c of p.children) m[c.id] = c.name }
  return m
})
const brandName = computed(() => {
  const m: Record<number, string> = {}
  for (const b of brands.value) m[b.id] = b.name
  return m
})

const activePreset = computed(() =>
  pricePresets.findIndex((p) => p.min === filter.value.minPrice && p.max === filter.value.maxPrice))

const hasActiveFilters = computed(() =>
  !!(filter.value.categoryId || filter.value.brandId || filter.value.minPrice || filter.value.maxPrice ||
     filter.value.minRating || filter.value.keyword || storageSel.value))

// Còn trang để tải tiếp không (theo dữ liệu server, chưa tính lọc dung lượng client)
const hasMore = computed(() => products.value.length < total.value)

// Danh sách hiển thị = kết quả server, lọc thêm theo dung lượng (khớp tên máy).
const shownProducts = computed(() => {
  if (!storageSel.value) return products.value
  const s = storageSel.value.toLowerCase()
  return products.value.filter((p) => p.name.toLowerCase().includes(s))
})

// reset=true: tải mới (đổi lọc/sắp xếp) → thay danh sách. reset=false: nối trang kế tiếp.
async function load(reset = false) {
  if (reset) { filter.value.page = 1; products.value = []; total.value = 0; loading.value = true }
  else loadingMore.value = true
  try {
    const res = await productApi.search(filter.value)
    if (reset) products.value = res.items
    else products.value.push(...res.items)
    total.value = res.totalItems
  } finally { loading.value = false; loadingMore.value = false }
}

// Tự nối trang kế khi cuộn tới đáy.
function loadMore() {
  if (loading.value || loadingMore.value || !hasMore.value) return
  filter.value.page = (filter.value.page ?? 1) + 1
  load(false)
}

function apply() { load(true); showFilters.value = false }

function setCategory(id?: number) { filter.value.categoryId = id; apply() }
function setBrand(id?: number) { filter.value.brandId = filter.value.brandId === id ? undefined : id; apply() }
function setStorage(s?: string) { storageSel.value = storageSel.value === s ? undefined : s }
function toggleCat(id: number) {
  if (expanded.value.has(id)) expanded.value.delete(id)
  else expanded.value.add(id)
}
// Mở cha chứa danh mục đang chọn để nó luôn hiện trong tree
function expandSelected() {
  const id = filter.value.categoryId
  if (!id) return
  for (const p of tree.value) {
    if (p.id === id || p.children.some((c) => c.id === id)) expanded.value.add(p.id)
  }
}
function setPricePreset(i: number) {
  const p = pricePresets[i]
  if (activePreset.value === i) { filter.value.minPrice = undefined; filter.value.maxPrice = undefined }
  else { filter.value.minPrice = p.min; filter.value.maxPrice = p.max }
  apply()
}
function setRating(v?: number) { filter.value.minRating = filter.value.minRating === v ? undefined : v; apply() }
function clearAll() {
  filter.value.categoryId = undefined; filter.value.brandId = undefined
  filter.value.minPrice = undefined; filter.value.maxPrice = undefined
  filter.value.minRating = undefined; filter.value.keyword = undefined
  storageSel.value = undefined; apply()
}
function scrollToTop() { window.scrollTo({ top: 0, behavior: 'smooth' }) }

// Xử lý cuộn: hiện nút về đầu + tự tải thêm khi gần chạm đáy (throttle bằng rAF).
let ticking = false
function onScroll() {
  if (ticking) return
  ticking = true
  requestAnimationFrame(() => {
    showTop.value = window.scrollY > 600
    if (!loading.value && !loadingMore.value && hasMore.value &&
        window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 700) {
      loadMore()
    }
    ticking = false
  })
}

onMounted(async () => {
  tree.value = await categoryApi.tree()
  brands.value = await brandsApi.list().catch(() => [])
  expandSelected(); load(true)
  window.addEventListener('scroll', onScroll, { passive: true })
})
onUnmounted(() => window.removeEventListener('scroll', onScroll))

watch(() => route.query, (q) => {
  filter.value.keyword = (q.keyword as string) || undefined
  filter.value.categoryId = q.categoryId ? Number(q.categoryId) : undefined
  filter.value.brandId = q.brandId ? Number(q.brandId) : undefined
  expandSelected()
  load(true)
  window.scrollTo({ top: 0 })
})
</script>

<template>
  <div class="toolbar-top">
    <Button class="filter-toggle" icon="pi pi-filter" label="Bộ lọc" size="small" outlined @click="showFilters = !showFilters" />
    <span class="total-txt">{{ total }} sản phẩm</span>
    <Select v-model="filter.sortBy" :options="sortOptions" optionLabel="label" optionValue="value"
      size="small" style="width: 180px" @change="apply" />
  </div>

  <!-- Chip filter đang áp dụng -->
  <div v-if="hasActiveFilters" class="active-chips">
    <span v-if="filter.keyword" class="chip">Từ khóa: "{{ filter.keyword }}" <i class="pi pi-times" @click="filter.keyword = undefined; apply()" /></span>
    <span v-if="filter.brandId" class="chip">{{ brandName[filter.brandId] }} <i class="pi pi-times" @click="setBrand(undefined)" /></span>
    <span v-if="filter.categoryId" class="chip">{{ catName[filter.categoryId] }} <i class="pi pi-times" @click="setCategory(undefined)" /></span>
    <span v-if="storageSel" class="chip">{{ storageSel }} <i class="pi pi-times" @click="setStorage(undefined)" /></span>
    <span v-if="filter.minPrice || filter.maxPrice" class="chip">
      {{ filter.minPrice ? formatCurrency(filter.minPrice) : '0' }} - {{ filter.maxPrice ? formatCurrency(filter.maxPrice) : '∞' }}
      <i class="pi pi-times" @click="filter.minPrice = undefined; filter.maxPrice = undefined; apply()" />
    </span>
    <span v-if="filter.minRating" class="chip">Từ {{ filter.minRating }} sao <i class="pi pi-times" @click="setRating(undefined)" /></span>
    <button class="clear-all" @click="clearAll">Xóa tất cả</button>
  </div>

  <div class="layout">
    <aside class="filters" :class="{ open: showFilters }">
      <!-- Thương hiệu điện thoại -->
      <div class="f-group">
        <h4>Thương hiệu</h4>
        <div class="brand-list">
          <button class="brand-item" :class="{ active: !filter.brandId }" @click="setBrand(undefined)">Tất cả hãng</button>
          <button v-for="b in brands" :key="b.id" class="brand-item" :class="{ active: filter.brandId === b.id }" @click="setBrand(b.id)">
            <img v-if="b.logoUrl" :src="b.logoUrl" :alt="b.name" class="brand-logo" />
            <span>{{ b.name }}</span>
          </button>
        </div>
      </div>

      <!-- Dung lượng -->
      <div class="f-group">
        <h4>Dung lượng</h4>
        <div class="storage-list">
          <button v-for="s in storageOptions" :key="s" class="storage-chip" :class="{ active: storageSel === s }" @click="setStorage(s)">{{ s }}</button>
        </div>
      </div>

      <div class="f-group">
        <h4>Danh mục</h4>
        <div class="cat-list">
          <button class="cat-item" :class="{ active: !filter.categoryId }" @click="setCategory(undefined)">Tất cả sản phẩm</button>
          <div v-for="p in tree" :key="p.id" class="cat-node">
            <div class="cat-row">
              <button class="cat-item parent" :class="{ active: filter.categoryId === p.id }" @click="setCategory(p.id)">{{ p.name }}</button>
              <button v-if="p.children.length" class="cat-toggle" :class="{ open: expanded.has(p.id) }"
                :aria-label="expanded.has(p.id) ? 'Thu gọn' : 'Mở rộng'" @click="toggleCat(p.id)">
                <i class="pi pi-chevron-down" />
              </button>
            </div>
            <div v-if="p.children.length && expanded.has(p.id)" class="cat-children">
              <button v-for="c in p.children" :key="c.id" class="cat-item child" :class="{ active: filter.categoryId === c.id }" @click="setCategory(c.id)">{{ c.name }}</button>
            </div>
          </div>
        </div>
      </div>

      <div class="f-group">
        <h4>Khoảng giá</h4>
        <div class="preset-list">
          <button v-for="(p, i) in pricePresets" :key="i" class="preset" :class="{ active: activePreset === i }" @click="setPricePreset(i)">{{ p.label }}</button>
        </div>
        <div class="custom-price">
          <InputNumber v-model="filter.minPrice" placeholder="Giá tối thiểu" :min="0" size="small" fluid inputClass="w-full" />
          <span class="cp-sep">đến</span>
          <InputNumber v-model="filter.maxPrice" placeholder="Giá tối đa" :min="0" size="small" fluid inputClass="w-full" />
        </div>
        <Button label="Áp dụng giá" size="small" outlined class="w-full mt-2" @click="apply" />
      </div>

      <div class="f-group">
        <h4>Đánh giá</h4>
        <button v-for="r in ratingPresets" :key="r.value" class="rating-item" :class="{ active: filter.minRating === r.value }" @click="setRating(r.value)">
          <span class="stars"><i v-for="n in 5" :key="n" class="pi" :class="n <= r.value ? 'pi-star-fill' : 'pi-star'" /></span>
          {{ r.label }}
        </button>
      </div>
    </aside>

    <div class="results">
      <div v-if="loading" class="grid-products"><ProductSkeleton :count="12" /></div>
      <template v-else>
        <div v-if="shownProducts.length" class="grid-products">
          <ProductCard v-for="p in shownProducts" :key="p.id" :product="p" />
          <!-- Skeleton nối khi đang tải trang tiếp theo -->
          <ProductSkeleton v-if="loadingMore" :count="4" />
        </div>
        <div v-else class="empty">
          <i class="pi pi-search" style="font-size: 2.5rem; color: #d1d5db" />
          <p>Không tìm thấy sản phẩm phù hợp.</p>
          <Button v-if="hasActiveFilters" label="Xóa bộ lọc" size="small" outlined @click="clearAll" />
        </div>
        <!-- Báo đã tải hết -->
        <p v-if="shownProducts.length && !hasMore && !storageSel" class="feed-end">— Đã xem hết {{ total }} sản phẩm —</p>
      </template>
    </div>
  </div>

  <!-- Nút nổi: về đầu trang -->
  <Transition name="fade">
    <button v-show="showTop" class="to-top" aria-label="Về đầu trang" @click="scrollToTop">
      <i class="pi pi-arrow-up" />
    </button>
  </Transition>
</template>

<style scoped>
.toolbar-top { display: flex; align-items: center; gap: var(--sp-3); margin-bottom: var(--sp-3); background: var(--surface); padding: var(--sp-3) var(--sp-4); border-radius: var(--radius); }
.total-txt { color: var(--text-muted); font-size: 14px; }
.toolbar-top .filter-toggle { display: none; }
.total-txt + :deep(.p-select) { margin-left: auto; }

.active-chips { display: flex; flex-wrap: wrap; gap: var(--sp-2); align-items: center; margin-bottom: var(--sp-3); }
.chip { display: inline-flex; align-items: center; gap: 6px; background: var(--brand-50); color: var(--brand); border: 1px solid var(--brand-100); padding: 4px 10px; border-radius: var(--radius-pill); font-size: 13px; }
.chip .pi { cursor: pointer; font-size: 11px; }
.clear-all { background: none; border: none; color: var(--text-muted); cursor: pointer; font-size: 13px; text-decoration: underline; }

.layout { display: grid; grid-template-columns: 230px 1fr; gap: var(--sp-4); align-items: start; }
.filters { background: var(--surface); border-radius: var(--radius-lg); padding: var(--sp-4); position: sticky; top: 84px; }
.f-group { padding-bottom: var(--sp-4); margin-bottom: var(--sp-4); border-bottom: 1px solid var(--border); }
.f-group:last-child { border-bottom: none; margin-bottom: 0; padding-bottom: 0; }
.f-group h4 { margin: 0 0 var(--sp-3); font-size: 14px; }

/* Danh sách thương hiệu — có logo nhỏ */
.brand-list { display: flex; flex-direction: column; gap: 2px; max-height: min(34vh, 260px); overflow-y: auto; }
.brand-list::-webkit-scrollbar { width: 6px; }
.brand-list::-webkit-scrollbar-thumb { background: var(--border-strong); border-radius: var(--radius-pill); }
.brand-item { display: flex; align-items: center; gap: 8px; width: 100%; text-align: left; background: none; border: none; padding: 7px 10px; border-radius: var(--radius-sm); cursor: pointer; font-family: inherit; font-size: 13px; color: var(--text-2); }
.brand-item:hover { background: var(--surface-2); color: var(--brand); }
.brand-item.active { background: var(--brand-50); color: var(--brand); font-weight: 600; }
.brand-logo { width: 20px; height: 20px; object-fit: contain; border-radius: 4px; }

/* Dung lượng — chip vuông gọn */
.storage-list { display: flex; flex-wrap: wrap; gap: 6px; }
.storage-chip { background: none; border: 1px solid var(--border); border-radius: var(--radius-sm); padding: 5px 12px; cursor: pointer; font-family: inherit; font-size: 12.5px; font-weight: 600; color: var(--text-2); transition: all var(--ease); }
.storage-chip:hover { border-color: var(--brand); color: var(--brand); }
.storage-chip.active { background: var(--brand-50); border-color: var(--brand); color: var(--brand); }

/* Tree danh mục — cuộn nội bộ khi dài, không kéo cả trang */
.cat-list { max-height: min(40vh, 300px); overflow-y: auto; margin: 0 calc(-1 * var(--sp-2)); padding: 0 var(--sp-2); }
.cat-list::-webkit-scrollbar { width: 6px; }
.cat-list::-webkit-scrollbar-thumb { background: var(--border-strong); border-radius: var(--radius-pill); }
.cat-list::-webkit-scrollbar-thumb:hover { background: var(--text-muted); }
.cat-row { display: flex; align-items: center; gap: 2px; }
.cat-row .cat-item.parent { flex: 1; margin-top: 0; }
.cat-node + .cat-node .cat-row, .cat-item + .cat-node .cat-row { margin-top: 2px; }
.cat-toggle { flex-shrink: 0; background: none; border: none; cursor: pointer; color: var(--text-muted); padding: 6px; display: flex; align-items: center; border-radius: var(--radius-sm); }
.cat-toggle:hover { color: var(--brand); background: var(--surface-2); }
.cat-toggle .pi { font-size: 11px; transition: transform var(--ease); }
.cat-toggle:not(.open) .pi { transform: rotate(-90deg); }
.cat-children { display: flex; flex-direction: column; }

.cat-item { display: block; width: 100%; text-align: left; background: none; border: none; padding: 7px 10px; border-radius: var(--radius-sm); cursor: pointer; font-family: inherit; font-size: 13px; color: var(--text-2); }
.cat-item:hover { background: var(--surface-2); color: var(--brand); }
.cat-item.active { background: var(--brand-50); color: var(--brand); font-weight: 600; }
.cat-item.parent { font-weight: 600; color: var(--text); }
.cat-item.child { padding-left: 22px; font-size: 12.5px; }

.preset-list { display: flex; flex-direction: column; gap: 6px; }
.preset { text-align: left; background: none; border: 1px solid var(--border); border-radius: var(--radius-sm); padding: 6px 10px; cursor: pointer; font-family: inherit; font-size: 13px; color: var(--text-2); transition: all var(--ease); }
.preset:hover { border-color: var(--brand); color: var(--brand); }
.preset.active { background: var(--brand-50); border-color: var(--brand); color: var(--brand); font-weight: 600; }
/* Nhập giá tuỳ chỉnh: xếp dọc, mỗi ô full-width — đọc số tiền lớn không bị bóp */
.custom-price { display: flex; flex-direction: column; gap: 6px; margin-top: var(--sp-3); }
.custom-price :deep(.p-inputnumber) { width: 100%; }
.custom-price :deep(.p-inputnumber-input) { width: 100%; }
.cp-sep { font-size: 12px; color: var(--text-muted); text-align: center; line-height: 1; }

.rating-item { display: flex; align-items: center; gap: 8px; width: 100%; text-align: left; background: none; border: none; padding: 6px 10px; border-radius: var(--radius-sm); cursor: pointer; font-family: inherit; font-size: 13px; color: var(--text-2); }
.rating-item:hover { background: var(--surface-2); }
.rating-item.active { background: var(--brand-50); color: var(--brand); font-weight: 600; }
.stars .pi { font-size: 12px; color: var(--star); }
.stars .pi-star { color: #d8d8d8; }

.center, .empty { padding: 3rem; text-align: center; display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); color: var(--text-muted); }
.feed-end { text-align: center; color: var(--text-muted); font-size: 13px; padding: var(--sp-5) 0 var(--sp-2); }

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

@media (max-width: 768px) {
  .layout { grid-template-columns: 1fr; }
  .toolbar-top .filter-toggle { display: inline-flex; }
  .filters { display: none; position: static; }
  .filters.open { display: block; }
}
</style>
