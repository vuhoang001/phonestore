<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import Select from 'primevue/select'
import DatePicker from 'primevue/datepicker'
import Paginator from 'primevue/paginator'
import ProgressSpinner from 'primevue/progressspinner'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import { orderApi, cartApi } from '@/services'
import type { Order } from '@/types'
import { formatCurrency, formatDate, orderStatusLabel, orderStatusSeverity } from '@/composables/format'
import { useCartStore } from '@/stores/cart'
import { extractError } from '@/services/api'

const router = useRouter()
const toast = useToast()
const confirm = useConfirm()
const cart = useCartStore()

const orders = ref<Order[]>([])
const loading = ref(true)
const statusFilter = ref<string | undefined>(undefined)
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const dateRange = ref<any>(null)   // [từ, đến] — lọc theo ngày đặt

const statusOptions = [
  { label: 'Tất cả trạng thái', value: undefined },
  { label: 'Chờ xác nhận', value: 'Pending' },
  { label: 'Đã xác nhận', value: 'Confirmed' },
  { label: 'Đang giao', value: 'Shipping' },
  { label: 'Hoàn tất', value: 'Completed' },
  { label: 'Đã hủy', value: 'Cancelled' }
]

const startOfDay = (d: Date) => { const x = new Date(d); x.setHours(0, 0, 0, 0); return x.getTime() }
const endOfDay = (d: Date) => { const x = new Date(d); x.setHours(23, 59, 59, 999); return x.getTime() }

const hasFilter = computed(() => !!(statusFilter.value || dateRange.value?.length))

// Danh sách đã sắp theo thời gian đặt (mới nhất trước) + lọc theo trạng thái/khoảng ngày.
const filteredOrders = computed(() => {
  let list = orders.value
  if (statusFilter.value) list = list.filter((o) => o.status === statusFilter.value)
  if (Array.isArray(dateRange.value) && dateRange.value[0]) {
    const from = startOfDay(dateRange.value[0])
    const to = dateRange.value[1] ? endOfDay(dateRange.value[1]) : endOfDay(dateRange.value[0])
    list = list.filter((o) => { const t = new Date(o.createdAt).getTime(); return t >= from && t <= to })
  }
  return [...list].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
})

// Phân trang phía client (đã tải sẵn danh sách) — 6 đơn/trang cho gọn.
const first = ref(0)
const rows = ref(6)
const pagedOrders = computed(() => filteredOrders.value.slice(first.value, first.value + rows.value))
watch(filteredOrders, () => { first.value = 0 })   // đổi bộ lọc → về trang đầu

function resetFilters() { statusFilter.value = undefined; dateRange.value = null }

async function load() {
  loading.value = true
  try {
    const res = await orderApi.my(1, 100)
    orders.value = res.items
  } finally {
    loading.value = false
  }
}

function canCancel(o: Order) { return o.status === 'Pending' || o.status === 'Confirmed' }

function cancelOrder(o: Order) {
  confirm.require({
    message: `Hủy đơn ${o.orderCode}?`, header: 'Xác nhận hủy', icon: 'pi pi-exclamation-triangle',
    acceptLabel: 'Hủy đơn', rejectLabel: 'Không',
    accept: async () => {
      try {
        await orderApi.cancel(o.id)
        toast.add({ severity: 'success', summary: 'Đã hủy đơn', life: 2000 })
        load()
      } catch (e) {
        toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
      }
    }
  })
}

async function buyAgain(o: Order) {
  try {
    for (const it of o.items) {
      await cartApi.add(it.variantId, it.quantity)
    }
    await cart.fetch()
    toast.add({ severity: 'success', summary: 'Đã thêm vào giỏ', life: 2000 })
    router.push('/cart')
  } catch (e) {
    toast.add({ severity: 'warn', summary: 'Một số sản phẩm không thể thêm', detail: extractError(e), life: 3500 })
    router.push('/cart')
  }
}

onMounted(load)
</script>

<template>
  <h1 class="section-title">Đơn hàng của tôi</h1>

  <!-- Filter: trạng thái + khoảng thời gian -->
  <div class="filter-bar">
    <Select v-model="statusFilter" :options="statusOptions" optionLabel="label" optionValue="value"
      placeholder="Trạng thái" size="small" class="f-status" />
    <DatePicker v-model="dateRange" selectionMode="range" :manualInput="false" dateFormat="dd/mm/yy"
      placeholder="Từ ngày – Đến ngày" size="small" showIcon iconDisplay="input" class="f-date" />
    <Button v-if="hasFilter" icon="pi pi-filter-slash" label="Xóa lọc" size="small" outlined severity="secondary" @click="resetFilters" />
    <span class="count" v-if="!loading">{{ filteredOrders.length }} đơn</span>
  </div>

  <div v-if="loading" class="center"><ProgressSpinner /></div>
  <template v-else-if="filteredOrders.length">
    <div class="order-list">
      <div v-for="o in pagedOrders" :key="o.id" class="order-card" @click="router.push(`/orders/${o.id}`)">
        <div class="oc-info">
          <div class="oc-line">
            <strong class="oc-code">{{ o.orderCode }}</strong>
            <Tag :value="orderStatusLabel[o.status]" :severity="orderStatusSeverity[o.status]" />
          </div>
          <span class="oc-meta">
            <i class="pi pi-box" /> {{ o.items.length }} SP
            <span class="dot-sep">·</span>
            <i class="pi pi-clock" /> {{ formatDate(o.createdAt) }}
          </span>
        </div>
        <div class="oc-side">
          <span class="oc-total">{{ formatCurrency(o.totalAmount) }}</span>
          <div class="actions" @click.stop>
            <Button v-if="canCancel(o)" label="Hủy" size="small" outlined severity="danger" @click="cancelOrder(o)" />
            <Button v-if="o.status === 'Completed' || o.status === 'Cancelled'" label="Mua lại" icon="pi pi-refresh" size="small" outlined @click="buyAgain(o)" />
            <Button icon="pi pi-angle-right" text rounded size="small" aria-label="Chi tiết" @click="router.push(`/orders/${o.id}`)" />
          </div>
        </div>
      </div>
    </div>
    <Paginator v-if="filteredOrders.length > rows" :first="first" :rows="rows"
      :totalRecords="filteredOrders.length" class="pager" @page="first = $event.first" />
  </template>
  <div v-else class="empty">
    <i class="pi pi-inbox empty-icon" />
    <p>{{ orders.length ? 'Không có đơn hàng phù hợp bộ lọc.' : 'Chưa có đơn hàng nào.' }}</p>
    <Button v-if="orders.length" label="Xóa lọc" size="small" outlined @click="resetFilters" />
    <Button v-else label="Mua sắm ngay" @click="router.push('/products')" />
  </div>
</template>

<style scoped>
.filter-bar { display: flex; align-items: center; gap: var(--sp-2); margin-bottom: var(--sp-4); flex-wrap: wrap; }
.filter-bar .f-status { width: 190px; }
.filter-bar .f-date { width: 230px; }
.filter-bar :deep(.p-datepicker-input) { width: 100%; }
.count { margin-left: auto; color: var(--text-muted); font-size: 13px; }

.center, .empty { text-align: center; padding: var(--sp-8) var(--sp-4); display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); }
.empty p { margin: 0; color: var(--text-muted); }
.empty-icon { font-size: 2.75rem; color: #d1d5db; }
/* Khung cố định: luôn chừa đủ chỗ 6 hàng để trang cuối (ít đơn) không bị co/thụt lại. */
.order-list { display: flex; flex-direction: column; gap: var(--sp-2); min-height: 402px; }
.order-card {
  display: flex; align-items: center; justify-content: space-between; gap: var(--sp-3);
  min-height: 59px;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius);
  padding: var(--sp-2) var(--sp-3);
  cursor: pointer;
  transition: border-color var(--ease), box-shadow var(--ease);
}
.order-card:hover { border-color: var(--brand-100); box-shadow: var(--shadow-sm); }

.oc-info { display: flex; flex-direction: column; gap: 3px; min-width: 0; }
.oc-line { display: flex; align-items: center; gap: var(--sp-2); }
.oc-code { font-size: 0.9rem; letter-spacing: 0.01em; }
.oc-meta { display: inline-flex; align-items: center; gap: 5px; color: var(--text-muted); font-size: 12.5px; }
.oc-meta .pi { font-size: 11px; }
.dot-sep { opacity: 0.5; }

.oc-side { display: flex; align-items: center; gap: var(--sp-2); flex-shrink: 0; }
.oc-total { color: var(--price); font-weight: 700; font-size: 0.95rem; white-space: nowrap; }
.actions { display: flex; align-items: center; gap: var(--sp-1); }

.pager { margin-top: var(--sp-3); background: transparent; }
.pager :deep(.p-paginator) { background: transparent; }

@media (max-width: 640px) {
  .filter-bar .f-status, .filter-bar .f-date { width: 100%; }
  .count { margin-left: 0; }
  .order-card { flex-direction: column; align-items: stretch; gap: var(--sp-2); }
  .oc-side { justify-content: space-between; }
}
</style>
