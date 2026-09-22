<script setup lang="ts">
// Quản lý Thu cũ đổi mới (admin): xem toàn bộ yêu cầu khách gửi, định giá thu (báo giá),
// và đổi trạng thái (đồng ý/từ chối). Khớp enum TradeInStatus backend: Pending/Quoted/Accepted/Rejected.
import { ref, computed, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputNumber from 'primevue/inputnumber'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import { tradeInApi } from '@/services'
import type { TradeIn } from '@/types'
import { formatCurrency, formatDate } from '@/composables/format'
import { extractError } from '@/services/api'

const toast = useToast()
const confirm = useConfirm()
const items = ref<TradeIn[]>([])
const loading = ref(true)

// Nhãn + màu Tag cho trạng thái (đồng bộ với trang khách TradeInView).
const statusLabel: Record<string, string> = {
  Pending: 'Chờ định giá', Quoted: 'Đã báo giá', Accepted: 'Đã đồng ý', Rejected: 'Từ chối'
}
const statusSeverity: Record<string, string> = {
  Pending: 'warn', Quoted: 'info', Accepted: 'success', Rejected: 'danger'
}

// Bộ lọc theo trạng thái (client-side).
const statusFilter = ref<string | null>(null)
const filterOptions = [
  { label: 'Tất cả trạng thái', value: null },
  { label: 'Chờ định giá', value: 'Pending' },
  { label: 'Đã báo giá', value: 'Quoted' },
  { label: 'Đã đồng ý', value: 'Accepted' },
  { label: 'Từ chối', value: 'Rejected' }
]
const filtered = computed(() =>
  statusFilter.value ? items.value.filter((t) => t.status === statusFilter.value) : items.value
)

// Thẻ tổng nhanh theo trạng thái.
const stats = computed(() => ({
  pending: items.value.filter((t) => t.status === 'Pending').length,
  quoted: items.value.filter((t) => t.status === 'Quoted').length,
  accepted: items.value.filter((t) => t.status === 'Accepted').length
}))

async function load() {
  loading.value = true
  try { items.value = await tradeInApi.all() } finally { loading.value = false }
}

// ----- Dialog báo giá -----
const showQuote = ref(false)
const quoting = ref<TradeIn | null>(null)
const quotePrice = ref<number>(0)
const saving = ref(false)

function openQuote(t: TradeIn) {
  quoting.value = t
  quotePrice.value = t.quotedPrice > 0 ? t.quotedPrice : 0
  showQuote.value = true
}

async function saveQuote() {
  if (!quoting.value) return
  if (!quotePrice.value || quotePrice.value <= 0) {
    toast.add({ severity: 'warn', summary: 'Thiếu giá', detail: 'Nhập mức giá thu lớn hơn 0.', life: 2500 })
    return
  }
  saving.value = true
  try {
    await tradeInApi.quote(quoting.value.id, { quotedPrice: quotePrice.value })
    showQuote.value = false
    await load()
    toast.add({ severity: 'success', summary: 'Đã báo giá', detail: 'Khách sẽ thấy mức định giá này.', life: 2500 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3500 })
  } finally {
    saving.value = false
  }
}

// ----- Đổi trạng thái (đồng ý/từ chối) -----
function changeStatus(t: TradeIn, status: string, label: string) {
  confirm.require({
    message: `${label} yêu cầu thu "${t.oldDeviceModel}"?`,
    header: 'Xác nhận', icon: 'pi pi-exclamation-triangle',
    accept: async () => {
      try {
        await tradeInApi.updateStatus(t.id, status)
        await load()
        toast.add({ severity: 'success', summary: 'Đã cập nhật trạng thái', life: 2000 })
      } catch (e) {
        toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3500 })
      }
    }
  })
}

onMounted(load)
</script>

<template>
  <div class="head">
    <h1>Thu cũ đổi mới</h1>
    <Button icon="pi pi-refresh" label="Tải lại" outlined size="small" :loading="loading" @click="load" />
  </div>

  <!-- Thẻ tổng nhanh -->
  <div class="stat-row">
    <div class="stat"><span class="s-num warn">{{ stats.pending }}</span><span class="s-lb">Chờ định giá</span></div>
    <div class="stat"><span class="s-num info">{{ stats.quoted }}</span><span class="s-lb">Đã báo giá</span></div>
    <div class="stat"><span class="s-num ok">{{ stats.accepted }}</span><span class="s-lb">Đã đồng ý</span></div>
  </div>

  <div class="toolbar">
    <Select v-model="statusFilter" :options="filterOptions" optionLabel="label" optionValue="value"
      class="filter" />
  </div>

  <DataTable :value="filtered" :loading="loading" stripedRows size="small" class="box"
    paginator :rows="12" :rowsPerPageOptions="[12, 24, 50]">
    <Column field="id" header="#" style="width: 60px" />
    <Column field="oldDeviceModel" header="Máy cũ" />
    <Column field="condition" header="Tình trạng" />
    <Column header="Ghi chú">
      <template #body="{ data }"><span class="note">{{ data.note || '—' }}</span></template>
    </Column>
    <Column header="Định giá thu">
      <template #body="{ data }">
        <b v-if="data.quotedPrice > 0" class="price">{{ formatCurrency(data.quotedPrice) }}</b>
        <span v-else class="muted">Chưa định giá</span>
      </template>
    </Column>
    <Column header="Trạng thái">
      <template #body="{ data }"><Tag :value="statusLabel[data.status] || data.status" :severity="statusSeverity[data.status]" /></template>
    </Column>
    <Column header="Ngày gửi">
      <template #body="{ data }">{{ formatDate(data.createdAt) }}</template>
    </Column>
    <Column header="Thao tác" style="width: 240px">
      <template #body="{ data }">
        <div class="ops">
          <Button :label="data.quotedPrice > 0 ? 'Sửa giá' : 'Báo giá'" icon="pi pi-tag" size="small" outlined
            @click="openQuote(data)" />
          <Button icon="pi pi-check" size="small" severity="success" text v-tooltip.top="'Đánh dấu đã đồng ý'"
            :disabled="data.status === 'Accepted'" @click="changeStatus(data, 'Accepted', 'Đồng ý')" />
          <Button icon="pi pi-times" size="small" severity="danger" text v-tooltip.top="'Từ chối yêu cầu'"
            :disabled="data.status === 'Rejected'" @click="changeStatus(data, 'Rejected', 'Từ chối')" />
        </div>
      </template>
    </Column>
    <template #empty>
      <div class="empty">
        <i class="pi pi-sync" />
        <p>Chưa có yêu cầu thu cũ nào.</p>
      </div>
    </template>
  </DataTable>

  <!-- Dialog báo giá -->
  <Dialog v-model:visible="showQuote" header="Định giá thu máy cũ" modal style="width: 420px">
    <div v-if="quoting" class="qform">
      <div class="q-info">
        <div><span class="k">Máy cũ</span><b>{{ quoting.oldDeviceModel }}</b></div>
        <div><span class="k">Tình trạng</span><span>{{ quoting.condition }}</span></div>
        <div v-if="quoting.note"><span class="k">Ghi chú</span><span>{{ quoting.note }}</span></div>
      </div>
      <label>Giá thu đề nghị (₫)</label>
      <InputNumber v-model="quotePrice" :min="0" :step="100000" mode="currency" currency="VND" locale="vi-VN"
        class="w-full" inputClass="w-full" />
      <p class="hint"><i class="pi pi-info-circle" /> Sau khi báo giá, trạng thái tự chuyển sang “Đã báo giá” và khách sẽ thấy mức này.</p>
    </div>
    <template #footer>
      <Button label="Hủy" text @click="showQuote = false" />
      <Button label="Lưu báo giá" icon="pi pi-check" :loading="saving" @click="saveQuote" />
    </template>
  </Dialog>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; gap: var(--sp-3); margin-bottom: var(--sp-4); flex-wrap: wrap; }

.stat-row { display: grid; grid-template-columns: repeat(3, 1fr); gap: var(--sp-3); margin-bottom: var(--sp-4); }
.stat { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius); box-shadow: var(--shadow-sm); padding: var(--sp-4); display: flex; flex-direction: column; gap: 2px; }
.s-num { font-size: 1.7rem; font-weight: 700; letter-spacing: -0.02em; }
.s-num.warn { color: #b9770e; }
.s-num.info { color: var(--brand); }
.s-num.ok { color: var(--ok); }
.s-lb { font-size: 13px; color: var(--text-muted); }

.toolbar { margin-bottom: var(--sp-3); }
.filter { min-width: 200px; }

.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); }
.price { color: var(--price); }
.muted { color: var(--text-muted); }
.note { color: var(--text-2); font-size: 13px; }
.ops { display: flex; align-items: center; gap: 4px; }

.empty { display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); padding: var(--sp-8) var(--sp-4); color: var(--text-muted); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.empty p { margin: 0; }

.qform { display: flex; flex-direction: column; gap: var(--sp-2); }
.q-info { background: var(--surface-2); border: 1px solid var(--border); border-radius: var(--radius); padding: var(--sp-3); display: flex; flex-direction: column; gap: 6px; margin-bottom: var(--sp-2); }
.q-info > div { display: flex; justify-content: space-between; gap: var(--sp-3); font-size: 13.5px; }
.q-info .k { color: var(--text-muted); }
.qform label { font-weight: 600; font-size: 0.85rem; }
.hint { display: flex; align-items: flex-start; gap: 6px; margin: 6px 0 0; font-size: 12.5px; color: var(--text-muted); }

@media (max-width: 640px) { .stat-row { grid-template-columns: 1fr; } }
</style>
