<script setup lang="ts">
// Trang So sánh máy (đặc thù điện thoại): đặt 2–4 máy cạnh nhau, đối chiếu giá,
// thông tin cơ bản và bảng thông số kỹ thuật gom nhóm. Tô sáng dòng có KHÁC BIỆT
// để khách nhìn ra chỗ hơn/kém giữa các máy. Danh sách máy lấy từ store compare.
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import ToggleSwitch from 'primevue/toggleswitch'
import ProgressSpinner from 'primevue/progressspinner'
import { productApi } from '@/services'
import type { ProductDetail } from '@/types'
import { useCompareStore } from '@/stores/compare'
import { formatCurrency } from '@/composables/format'

const compare = useCompareStore()
const router = useRouter()
const placeholder = 'https://placehold.co/300x300/f4f6fb/c8d0e0?text=No+Image'

const details = ref<ProductDetail[]>([])
const loading = ref(false)
const diffOnly = ref(false) // chỉ hiện các dòng thông số khác nhau

// Giá đại diện của 1 máy = giá flash nếu đang chạy, ngược lại giá gốc (rẻ nhất).
function shownPrice(p: ProductDetail) {
  return p.flashPrice != null && p.flashPrice < p.basePrice ? p.flashPrice : p.basePrice
}
function isFlash(p: ProductDetail) {
  return p.flashPrice != null && p.flashPrice < p.basePrice
}

// Tải chi tiết tất cả máy đang chọn (song song). Giữ đúng thứ tự theo store.
async function loadDetails() {
  const ids = compare.ids
  if (!ids.length) { details.value = []; return }
  loading.value = true
  try {
    const loaded = await Promise.all(
      ids.map((id) => productApi.byId(id).catch(() => null))
    )
    details.value = loaded.filter((p): p is ProductDetail => p != null)
  } finally {
    loading.value = false
  }
}

// Ma trận thông số: gom theo group giữ nguyên thứ tự xuất hiện; mỗi dòng là 1 tên thông số
// với giá trị của từng máy + cờ "differ" nếu không phải máy nào cũng cùng giá trị.
interface SpecRow { name: string; values: string[]; differ: boolean }
interface SpecGroup { group: string; rows: SpecRow[] }

const specGroups = computed<SpecGroup[]>(() => {
  const list = details.value
  if (list.length === 0) return []
  const groupOrder: string[] = []
  // group -> (specName -> map(productIndex -> value))
  const map = new Map<string, Map<string, string[]>>()

  list.forEach((p, idx) => {
    for (const s of p.specifications || []) {
      if (!map.has(s.group)) { map.set(s.group, new Map()); groupOrder.push(s.group) }
      const g = map.get(s.group)!
      if (!g.has(s.name)) g.set(s.name, Array(list.length).fill('—'))
      g.get(s.name)![idx] = s.value
    }
  })

  return groupOrder.map((group) => {
    const rows: SpecRow[] = []
    for (const [name, values] of map.get(group)!) {
      const present = values.filter((v) => v !== '—')
      const differ = new Set(present).size > 1 || present.length !== values.length
      rows.push({ name, values, differ })
    }
    return { group, rows }
  })
})

// Dòng thông số hiển thị sau khi lọc "chỉ khác biệt".
const visibleGroups = computed<SpecGroup[]>(() => {
  if (!diffOnly.value) return specGroups.value
  return specGroups.value
    .map((g) => ({ group: g.group, rows: g.rows.filter((r) => r.differ) }))
    .filter((g) => g.rows.length > 0)
})

// Các dòng thông tin cơ bản (không phải spec) — cũng đánh dấu khác biệt.
const basicRows = computed(() => {
  const list = details.value
  if (!list.length) return []
  const rows = [
    { label: 'Thương hiệu', values: list.map((p) => p.brandName) },
    { label: 'Danh mục', values: list.map((p) => p.categoryName) },
    { label: 'Đánh giá', values: list.map((p) => (p.averageRating ? `${p.averageRating}★ (${p.reviewCount})` : 'Chưa có')) },
    { label: 'Đã bán', values: list.map((p) => String(p.soldCount)) },
    { label: 'Bảo hành', values: list.map((p) => `${p.warrantyMonths} tháng`) },
    { label: 'Trả góp', values: list.map((p) => (p.installmentAvailable ? 'Có (0%)' : 'Không')) }
  ]
  return rows.map((r) => ({ ...r, differ: new Set(r.values).size > 1 }))
})

const visibleBasicRows = computed(() => diffOnly.value ? basicRows.value.filter((r) => r.differ) : basicRows.value)

function removeAt(id: number) { compare.remove(id) }
function goProducts() { router.push({ name: 'products' }) }

// Nạp lại khi danh sách id đổi (khách bỏ máy ngay trong trang / từ thanh nổi).
watch(() => compare.ids.join(','), loadDetails)
onMounted(loadDetails)
</script>

<template>
  <div class="cmp-wrap">
    <header class="cmp-head">
      <div>
        <h1>So sánh điện thoại</h1>
        <p class="sub">Đặt các máy cạnh nhau để chọn đúng chiếc phù hợp nhất.</p>
      </div>
      <div v-if="details.length > 1" class="diff-toggle">
        <ToggleSwitch v-model="diffOnly" inputId="diffOnly" />
        <label for="diffOnly">Chỉ hiện điểm khác biệt</label>
      </div>
    </header>

    <!-- Loading -->
    <div v-if="loading" class="center"><ProgressSpinner style="width:48px;height:48px" /></div>

    <!-- Rỗng / thiếu máy -->
    <div v-else-if="details.length === 0" class="empty">
      <i class="pi pi-sliders-h" />
      <h2>Chưa có máy nào để so sánh</h2>
      <p>Bấm nút <b>So sánh</b> trên thẻ sản phẩm để thêm máy vào đây (tối đa {{ compare.MAX }} máy).</p>
      <Button label="Khám phá sản phẩm" icon="pi pi-arrow-right" iconPos="right" @click="goProducts" />
    </div>

    <div v-else class="cmp-scroll">
      <table class="cmp-table" :style="{ '--cols': details.length }">
        <!-- Hàng đầu: ảnh + tên + giá + thao tác -->
        <thead>
          <tr>
            <th class="rowhead corner"></th>
            <th v-for="p in details" :key="p.id" class="prod-cell">
              <button class="remove" :aria-label="`Bỏ ${p.name}`" @click="removeAt(p.id)"><i class="pi pi-times" /></button>
              <router-link :to="`/products/${p.slug}`" class="prod-link">
                <img :src="p.images?.find(i => i.isPrimary)?.url || p.images?.[0]?.url || placeholder" :alt="p.name" />
                <span class="prod-name">{{ p.name }}</span>
              </router-link>
              <div class="price-row">
                <b class="price">{{ formatCurrency(shownPrice(p)) }}</b>
                <span v-if="isFlash(p)" class="old">{{ formatCurrency(p.basePrice) }}</span>
              </div>
              <Button label="Xem chi tiết" size="small" outlined class="w-full"
                @click="router.push(`/products/${p.slug}`)" />
            </th>
          </tr>
        </thead>

        <tbody>
          <!-- Thông tin cơ bản -->
          <tr class="group-row"><td :colspan="details.length + 1"><i class="pi pi-info-circle" /> Thông tin chung</td></tr>
          <tr v-for="r in visibleBasicRows" :key="r.label" :class="{ differ: r.differ }">
            <td class="rowhead">{{ r.label }}</td>
            <td v-for="(v, i) in r.values" :key="i" class="val">{{ v }}</td>
          </tr>

          <!-- Thông số kỹ thuật theo nhóm -->
          <template v-for="g in visibleGroups" :key="g.group">
            <tr class="group-row"><td :colspan="details.length + 1"><i class="pi pi-cog" /> {{ g.group }}</td></tr>
            <tr v-for="r in g.rows" :key="g.group + r.name" :class="{ differ: r.differ }">
              <td class="rowhead">{{ r.name }}</td>
              <td v-for="(v, i) in r.values" :key="i" class="val" :class="{ dim: v === '—' }">{{ v }}</td>
            </tr>
          </template>

          <tr v-if="diffOnly && visibleGroups.length === 0 && visibleBasicRows.length === 0">
            <td :colspan="details.length + 1" class="all-same">
              <i class="pi pi-check-circle" /> Các máy này có thông số giống nhau ở mọi mục.
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="details.length && details.length < compare.MAX" class="add-more">
      <Button label="Thêm máy để so sánh" icon="pi pi-plus" text @click="goProducts" />
    </div>
  </div>
</template>

<style scoped>
.cmp-wrap { max-width: 1180px; margin: 0 auto; padding: var(--sp-4) 0 var(--sp-8); }

.cmp-head { display: flex; align-items: flex-end; justify-content: space-between; gap: var(--sp-4); margin-bottom: var(--sp-5); flex-wrap: wrap; }
.cmp-head h1 { margin: 0; font-size: 1.7rem; letter-spacing: -0.02em; }
.cmp-head .sub { margin: 6px 0 0; color: var(--text-2); }
.diff-toggle { display: flex; align-items: center; gap: 8px; font-size: 14px; color: var(--text-2); }
.diff-toggle label { cursor: pointer; user-select: none; }

.center { display: flex; justify-content: center; padding: var(--sp-8); }

.empty { text-align: center; padding: var(--sp-8) var(--sp-4); color: var(--text-muted); display: flex; flex-direction: column; align-items: center; gap: var(--sp-2); }
.empty .pi { font-size: 2.6rem; opacity: .5; }
.empty h2 { margin: var(--sp-2) 0 0; color: var(--text); font-size: 1.2rem; }
.empty p { margin: 0 0 var(--sp-3); max-width: 460px; }

/* Bảng cuộn ngang trên màn nhỏ */
.cmp-scroll { overflow-x: auto; border: 1px solid var(--border); border-radius: var(--radius-lg); background: var(--surface); box-shadow: var(--shadow-sm); }
.cmp-table { width: 100%; border-collapse: collapse; min-width: calc(180px + var(--cols) * 200px); }

/* Cột đầu (nhãn hàng) dính trái khi cuộn */
.rowhead {
  position: sticky; left: 0; z-index: 1; width: 180px; min-width: 180px;
  background: var(--surface-2); border-right: 1px solid var(--border);
  padding: 12px var(--sp-4); font-size: 13px; color: var(--text-2); font-weight: 500; text-align: left;
}
.corner { background: var(--surface); border-bottom: 1px solid var(--border); }

.prod-cell { padding: var(--sp-4); text-align: center; border-bottom: 1px solid var(--border); border-left: 1px solid var(--border); vertical-align: top; position: relative; min-width: 200px; }
.remove { position: absolute; top: 8px; right: 8px; width: 24px; height: 24px; border: none; cursor: pointer; border-radius: 50%; background: var(--surface-2); color: var(--text-muted); display: grid; place-items: center; transition: all var(--ease); }
.remove:hover { background: var(--sale); color: #fff; }
.remove .pi { font-size: 11px; }
.prod-link { display: flex; flex-direction: column; align-items: center; gap: 8px; text-decoration: none; }
.prod-link img { width: 96px; height: 96px; object-fit: contain; background: #fff; }
.prod-name { font-size: 14px; font-weight: 600; color: var(--text); line-height: 1.35; min-height: 2.7em; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
.price-row { display: flex; flex-direction: column; align-items: center; gap: 2px; margin: 10px 0; }
.price { color: var(--price); font-size: 1.05rem; }
.old { font-size: 12px; color: var(--text-muted); text-decoration: line-through; }

/* Hàng tiêu đề nhóm */
.group-row td { background: var(--brand-50); color: var(--brand); font-weight: 700; font-size: 13px; padding: 9px var(--sp-4); position: sticky; left: 0; }
.group-row .pi { font-size: 12px; margin-right: 6px; }

.val { padding: 11px var(--sp-4); text-align: center; font-size: 13.5px; color: var(--text); border-bottom: 1px solid var(--border); border-left: 1px solid var(--border); }
.val.dim { color: var(--text-muted); }
tbody tr:not(.group-row):hover .rowhead,
tbody tr:not(.group-row):hover .val { background: var(--surface-2); }

/* Tô sáng dòng có khác biệt giữa các máy */
tr.differ .rowhead { color: var(--text); font-weight: 600; }
tr.differ .val { background: color-mix(in srgb, var(--brand-50) 60%, transparent); font-weight: 600; }

.all-same { text-align: center; padding: var(--sp-6); color: var(--ok); }
.all-same .pi { margin-right: 6px; }

.add-more { text-align: center; margin-top: var(--sp-4); }

@media (max-width: 640px) {
  .rowhead { width: 120px; min-width: 120px; }
  .cmp-table { min-width: calc(120px + var(--cols) * 160px); }
}
</style>
