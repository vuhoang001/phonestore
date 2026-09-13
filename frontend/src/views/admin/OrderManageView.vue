<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'
import Button from 'primevue/button'
import Select from 'primevue/select'
import Dialog from 'primevue/dialog'
import IconField from 'primevue/iconfield'
import InputIcon from 'primevue/inputicon'
import InputText from 'primevue/inputtext'
import DatePicker from 'primevue/datepicker'
import Tabs from 'primevue/tabs'
import TabList from 'primevue/tablist'
import Tab from 'primevue/tab'
import TabPanels from 'primevue/tabpanels'
import TabPanel from 'primevue/tabpanel'
import { orderApi } from '@/services'
import type { Order } from '@/types'
import { formatCurrency, formatDate, orderStatusLabel, orderStatusSeverity, paymentMethodLabel, paymentMethodShort, paymentStatusLabel } from '@/composables/format'
import { extractError } from '@/services/api'

const toast = useToast()
const orders = ref<Order[]>([])
const loading = ref(true)
const filterStatus = ref<string | undefined>(undefined)
const search = ref('')
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const dateRange = ref<any>(null)   // [từ, đến] — any vì DatePicker range khó gõ chặt với vue-tsc

const selected = ref<Order | null>(null)
const showDetail = ref(false)
const activeTab = ref('items')
const newStatus = ref('')
const statusNote = ref('')
const savingStatus = ref(false)

// Bản đồ IMEI theo orderItemId khi chuyển đơn sang "Đang giao" (Shipping).
const imeiMap = ref<Record<number, string>>({})
// Có đang chuyển sang trạng thái Shipping không → hiện ô nhập IMEI.
const isShippingNext = computed(() => newStatus.value === 'Shipping')

// Tách chuỗi địa chỉ snapshot "Tên | SĐT | địa chỉ | Ghi chú: ..." thành thông tin khách hàng
const customer = computed(() => {
  const raw = selected.value?.shippingAddress || ''
  const parts = raw.split(' | ')
  let deliveryNote = ''
  const noteIdx = parts.findIndex((p) => p.startsWith('Ghi chú:'))
  if (noteIdx >= 0) { deliveryNote = parts[noteIdx].replace('Ghi chú:', '').trim(); parts.splice(noteIdx, 1) }
  return {
    name: parts[0]?.trim() || '—',
    phone: parts[1]?.trim() || '—',
    address: parts.slice(2).join(', ').trim() || '—',
    deliveryNote
  }
})

// Có mua trả góp không (hiển thị khối thông tin trả góp).
const hasInstallment = computed(() => !!selected.value?.installmentMonths)

const statusFilterOptions = [
  { label: 'Tất cả trạng thái', value: undefined },
  { label: 'Chờ xác nhận', value: 'Pending' },
  { label: 'Đã xác nhận', value: 'Confirmed' },
  { label: 'Đang giao', value: 'Shipping' },
  { label: 'Hoàn tất', value: 'Completed' },
  { label: 'Đã hủy', value: 'Cancelled' }
]

// Chỉ cho phép chọn các trạng thái kế tiếp hợp lệ (state machine từ backend).
const nextStatusOptions = computed(() =>
  (selected.value?.allowedNextStatuses ?? []).map((s) => ({ label: orderStatusLabel[s] || s, value: s })))

// Có đang áp bộ lọc nào không (để hiện nút xoá lọc).
const hasFilter = computed(() => !!(search.value.trim() || filterStatus.value || dateRange.value?.length))

function isoDate(d?: Date | null) {
  return d ? `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}` : undefined
}

// Lọc phía server: trạng thái + khoảng ngày + mã đơn.
async function load() {
  loading.value = true
  try {
    const from = isoDate(dateRange.value?.[0])
    const to = isoDate(dateRange.value?.[1])
    const res = await orderApi.all(1, 200, filterStatus.value, {
      from, to, keyword: search.value.trim() || undefined
    })
    orders.value = res.items
  } finally { loading.value = false }
}

// Tải lại khi xoá ngày hoặc đã chọn đủ cả 2 mốc.
function onDateChange(v: unknown) {
  const arr = Array.isArray(v) ? v.filter(Boolean) : []
  if (!v || arr.length === 2) load()
}

// Preset nhanh theo khoảng ngày gần đây.
function setPreset(days: number) {
  const to = new Date()
  const from = new Date(); from.setDate(from.getDate() - (days - 1))
  dateRange.value = [from, to]
  load()
}

function resetFilters() {
  search.value = ''
  filterStatus.value = undefined
  dateRange.value = null
  load()
}

function openDetail(o: Order) {
  selected.value = o
  activeTab.value = 'items'
  newStatus.value = o.allowedNextStatuses[0] ?? ''
  statusNote.value = ''
  // Khởi tạo ô IMEI: giữ IMEI đã có (nếu đơn đã từng gán).
  imeiMap.value = Object.fromEntries(o.items.map((it) => [it.id, it.imei ?? '']))
  showDetail.value = true
}

async function saveStatus() {
  if (!selected.value || !newStatus.value) return
  // Khi chuyển sang "Đang giao": bắt buộc nhập IMEI cho từng dòng hàng.
  let imeiAssignments: { orderItemId: number; imei: string }[] | undefined
  if (isShippingNext.value) {
    const missing = selected.value.items.filter((it) => !(imeiMap.value[it.id] || '').trim())
    if (missing.length) {
      toast.add({ severity: 'warn', summary: 'Thiếu IMEI', detail: 'Nhập IMEI cho tất cả dòng hàng trước khi giao.', life: 3000 })
      return
    }
    imeiAssignments = selected.value.items.map((it) => ({ orderItemId: it.id, imei: (imeiMap.value[it.id] || '').trim() }))
  }
  savingStatus.value = true
  try {
    const updated = await orderApi.updateStatus(selected.value.id, newStatus.value, statusNote.value || undefined, imeiAssignments)
    selected.value = updated
    newStatus.value = updated.allowedNextStatuses[0] ?? ''
    statusNote.value = ''
    imeiMap.value = Object.fromEntries(updated.items.map((it) => [it.id, it.imei ?? '']))
    const idx = orders.value.findIndex((o) => o.id === updated.id)
    if (idx >= 0) orders.value[idx] = updated
    toast.add({ severity: 'success', summary: 'Đã cập nhật trạng thái', life: 2000 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Không hợp lệ', detail: extractError(e), life: 3500 })
  } finally { savingStatus.value = false }
}

const route = useRoute()
const router = useRouter()

// Mở thẳng popup chi tiết khi vào từ thông báo: /admin/orders?open=<orderId>
async function openFromQuery() {
  const open = route.query.open
  if (!open) return
  try { openDetail(await orderApi.byId(Number(open))) }
  catch { toast.add({ severity: 'warn', summary: 'Không mở được đơn', detail: 'Đơn không tồn tại hoặc đã bị xoá.', life: 3000 }) }
  router.replace({ query: {} })
}

onMounted(async () => { await load(); await openFromQuery() })
watch(() => route.query.open, () => openFromQuery())
</script>

<template>
  <h1 class="page-title">Quản lý đơn hàng</h1>

  <!-- Thanh filter -->
  <div class="filter-bar">
    <IconField iconPosition="left" class="f-search">
      <InputIcon class="pi pi-search" />
      <InputText v-model="search" placeholder="Mã đơn..." size="small" class="w-full" @keyup.enter="load" />
    </IconField>

    <DatePicker v-model="dateRange" selectionMode="range" :manualInput="false" dateFormat="dd/mm/yy"
      placeholder="Từ ngày – Đến ngày" size="small" showIcon iconDisplay="input" class="f-date"
      @update:modelValue="onDateChange" />

    <Select v-model="filterStatus" :options="statusFilterOptions" optionLabel="label" optionValue="value"
      placeholder="Trạng thái" size="small" class="f-status" @change="load" />

    <Button label="Lọc" icon="pi pi-filter" size="small" @click="load" />
    <Button v-if="hasFilter" icon="pi pi-filter-slash" size="small" outlined severity="secondary"
      @click="resetFilters" v-tooltip.top="'Xoá lọc'" />

    <!-- Preset nhanh theo thời gian -->
    <div class="presets">
      <button class="preset" @click="setPreset(1)">Hôm nay</button>
      <button class="preset" @click="setPreset(7)">7 ngày</button>
      <button class="preset" @click="setPreset(30)">30 ngày</button>
    </div>
  </div>

  <DataTable :value="orders" :loading="loading" paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]"
    stripedRows size="small" class="box" rowHover @row-click="openDetail($event.data)">
    <Column field="orderCode" header="Mã đơn" />
    <Column header="Ngày"><template #body="{ data }">{{ formatDate(data.createdAt) }}</template></Column>
    <Column header="SL"><template #body="{ data }">{{ data.items.length }}</template></Column>
    <Column header="Tổng tiền"><template #body="{ data }">{{ formatCurrency(data.totalAmount) }}</template></Column>
    <Column header="Thanh toán"><template #body="{ data }">{{ paymentMethodLabel[data.paymentMethod] || data.paymentMethod }}</template></Column>
    <Column header="Trạng thái">
      <template #body="{ data }"><Tag :value="orderStatusLabel[data.status]" :severity="orderStatusSeverity[data.status]" /></template>
    </Column>
    <Column header="" style="width: 100px">
      <template #body="{ data }"><Button label="Xem" size="small" outlined icon="pi pi-eye" @click="openDetail(data)" /></template>
    </Column>
    <template #empty><div class="empty-row">Không có đơn hàng phù hợp.</div></template>
  </DataTable>

  <!-- Chi tiết đơn -->
  <Dialog v-model:visible="showDetail" modal class="order-dialog" :style="{ width: '960px' }">
    <template #header>
      <div class="dlg-title">
        <i class="pi pi-receipt" />
        <span>Đơn hàng <b>{{ selected?.orderCode }}</b></span>
        <Tag v-if="selected" :value="orderStatusLabel[selected.status]" :severity="orderStatusSeverity[selected.status]" />
      </div>
    </template>

    <div v-if="selected" class="detail">
      <!-- Dải tóm tắt nhanh -->
      <div class="sum-strip">
        <div class="cell"><span class="k">Ngày đặt</span><b>{{ formatDate(selected.createdAt) }}</b></div>
        <div class="cell"><span class="k">Khách hàng</span><b>{{ customer.name }}</b></div>
        <div class="cell"><span class="k">Thanh toán</span>
          <b class="pay-val">{{ paymentMethodShort[selected.paymentMethod || ''] || selected.paymentMethod }}
            <span class="pay-status" :class="'st-' + (selected.paymentStatus || '').toLowerCase()">
              <i class="pi" :class="selected.paymentStatus === 'Paid' ? 'pi-check-circle' : selected.paymentStatus === 'Failed' ? 'pi-times-circle' : 'pi-clock'" />
              {{ paymentStatusLabel[selected.paymentStatus || ''] || selected.paymentStatus }}
            </span>
          </b>
        </div>
        <div class="cell"><span class="k">Số sản phẩm</span><b>{{ selected.items.length }}</b></div>
        <div class="cell"><span class="k">Tổng tiền</span><b class="price">{{ formatCurrency(selected.totalAmount) }}</b></div>
      </div>

      <!-- Khối trả góp (nếu đơn mua trả góp) -->
      <div v-if="hasInstallment" class="install-strip">
        <i class="pi pi-calendar" />
        <span>Mua <b>trả góp {{ selected.installmentMonths }} tháng</b></span>
        <span v-if="selected.installmentMonthly" class="im-monthly">≈ <b>{{ formatCurrency(selected.installmentMonthly) }}</b>/tháng</span>
      </div>

      <!-- Tabs theo khu thông tin -->
      <Tabs v-model:value="activeTab" class="order-tabs">
        <TabList>
          <Tab value="items"><i class="pi pi-box" /> Sản phẩm<span class="tab-count">{{ selected.items.length }}</span></Tab>
          <Tab value="customer"><i class="pi pi-user" /> Khách hàng &amp; Giao hàng</Tab>
          <Tab value="history"><i class="pi pi-history" /> Lịch sử<span class="tab-count">{{ selected.statusHistory.length }}</span></Tab>
        </TabList>

        <TabPanels>
          <!-- TAB · Sản phẩm (kèm cột IMEI) -->
          <TabPanel value="items">
            <div class="items-wrap">
              <table class="items">
                <thead><tr><th>Sản phẩm</th><th>Phân loại</th><th>IMEI</th><th class="r">Đơn giá</th><th class="c">SL</th><th class="r">Thành tiền</th></tr></thead>
                <tbody>
                  <tr v-for="i in selected.items" :key="i.id">
                    <td class="pname">{{ i.productName }}</td>
                    <td class="text-muted">{{ i.variantInfo || '—' }}</td>
                    <td>
                      <span v-if="i.imei" class="imei"><i class="pi pi-shield" /> {{ i.imei }}</span>
                      <span v-else class="text-muted">—</span>
                    </td>
                    <td class="r">{{ formatCurrency(i.price) }}</td>
                    <td class="c">{{ i.quantity }}</td>
                    <td class="r price">{{ formatCurrency(i.lineTotal) }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
            <div class="totals">
              <div class="row"><span>Tạm tính</span><span>{{ formatCurrency(selected.subTotal) }}</span></div>
              <div class="row" v-if="selected.discountAmount"><span>Giảm giá</span><span class="minus">-{{ formatCurrency(selected.discountAmount) }}</span></div>
              <div class="row"><span>Phí vận chuyển</span><span>{{ formatCurrency(selected.shippingFee) }}</span></div>
              <div class="row grand"><span>Tổng cộng</span><span class="price">{{ formatCurrency(selected.totalAmount) }}</span></div>
            </div>
          </TabPanel>

          <!-- TAB · Khách hàng & Giao hàng -->
          <TabPanel value="customer">
            <div class="info-grid">
              <div class="info-item"><span class="k"><i class="pi pi-user" /> Người nhận</span><b>{{ customer.name }}</b></div>
              <div class="info-item"><span class="k"><i class="pi pi-phone" /> Số điện thoại</span><b>{{ customer.phone }}</b></div>
              <div class="info-item full"><span class="k"><i class="pi pi-map-marker" /> Địa chỉ giao hàng</span><b>{{ customer.address }}</b></div>
              <div class="info-item"><span class="k"><i class="pi pi-credit-card" /> Phương thức thanh toán</span><b>{{ paymentMethodLabel[selected.paymentMethod || ''] || selected.paymentMethod || '—' }}</b></div>
              <div class="info-item"><span class="k"><i class="pi pi-wallet" /> Trạng thái thanh toán</span><b>{{ paymentStatusLabel[selected.paymentStatus || ''] || selected.paymentStatus || '—' }}</b></div>
              <div class="info-item" v-if="hasInstallment"><span class="k"><i class="pi pi-calendar" /> Trả góp</span><b>{{ selected.installmentMonths }} tháng<template v-if="selected.installmentMonthly"> · {{ formatCurrency(selected.installmentMonthly) }}/tháng</template></b></div>
              <div class="info-item full" v-if="customer.deliveryNote"><span class="k"><i class="pi pi-truck" /> Ghi chú giao hàng</span><b>{{ customer.deliveryNote }}</b></div>
              <div class="info-item full" v-if="selected.note"><span class="k"><i class="pi pi-comment" /> Ghi chú đơn hàng</span><b>{{ selected.note }}</b></div>
            </div>
          </TabPanel>

          <!-- TAB · Lịch sử (stepper ngang) -->
          <TabPanel value="history">
            <div class="stepper">
              <div v-for="(h, idx) in selected.statusHistory" :key="idx" class="step">
                <div class="step-line">
                  <span class="ln" :class="{ on: idx > 0 }" />
                  <span class="dot"><i class="pi pi-check" /></span>
                  <span class="ln" :class="{ on: idx < selected.statusHistory.length - 1 }" />
                </div>
                <div class="step-body">
                  <strong>{{ orderStatusLabel[h.status] }}</strong>
                  <div class="s-date">{{ formatDate(h.changedAt) }}</div>
                  <div v-if="h.note" class="s-note">{{ h.note }}</div>
                </div>
              </div>
            </div>
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>

    <!-- Thanh hành động cố định: chuyển trạng thái -->
    <template #footer>
      <div v-if="selected" class="action-wrap">
        <!-- Ô nhập IMEI cho từng dòng khi chuyển sang Đang giao -->
        <div v-if="nextStatusOptions.length && isShippingNext" class="imei-box">
          <div class="imei-title"><i class="pi pi-shield" /> Nhập IMEI/Serial cho từng máy trước khi giao</div>
          <div v-for="it in selected.items" :key="it.id" class="imei-row">
            <span class="imei-name">{{ it.productName }} <em v-if="it.variantInfo">· {{ it.variantInfo }}</em></span>
            <InputText v-model="imeiMap[it.id]" placeholder="Nhập IMEI (15 số)" class="imei-input" />
          </div>
        </div>

        <div class="action-bar">
          <template v-if="nextStatusOptions.length">
            <div class="ab-flow">
              <span class="ab-cur">{{ orderStatusLabel[selected.status] }}</span>
              <i class="pi pi-arrow-right" />
            </div>
            <Select v-model="newStatus" :options="nextStatusOptions" optionLabel="label" optionValue="value" class="ab-select" />
            <InputText v-model="statusNote" placeholder="Ghi chú (tùy chọn)" class="ab-note" />
            <Button label="Cập nhật" icon="pi pi-check" :loading="savingStatus" class="ab-btn" @click="saveStatus" />
          </template>
          <div v-else class="terminal">
            <i class="pi pi-check-circle" />
            <span>Đơn đã kết thúc — không thể đổi trạng thái.</span>
          </div>
        </div>
      </div>
    </template>
  </Dialog>
</template>

<style scoped>
.page-title { margin: 0 0 var(--sp-4); }
.filter-bar { display: flex; gap: var(--sp-2); align-items: center; margin-bottom: var(--sp-3); flex-wrap: wrap; }
.filter-bar .f-search { width: 240px; }
.filter-bar .f-date { width: 240px; }
.filter-bar .f-status { width: 170px; }
.filter-bar :deep(.p-datepicker-input) { width: 100%; }
.presets { display: flex; gap: 6px; margin-left: auto; flex-wrap: wrap; }
.preset { background: var(--surface); border: 1px solid var(--border); color: var(--text-2); font-family: inherit;
  font-size: 12px; padding: 5px 12px; border-radius: var(--radius-pill); cursor: pointer; transition: all var(--ease); }
.preset:hover { border-color: var(--brand); color: var(--brand); background: var(--brand-50); }
@media (max-width: 640px) {
  .filter-bar .f-search, .filter-bar .f-date, .filter-bar .f-status { width: 100%; }
  .presets { margin-left: 0; }
}
.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); }
:deep(.p-datatable-tbody > tr) { cursor: pointer; }
.empty-row { padding: var(--sp-6); text-align: center; color: var(--text-muted); }

.dlg-title { display: flex; align-items: center; gap: var(--sp-2); font-size: 1.05rem; font-weight: 700; }
.dlg-title .pi { color: var(--brand); }
.dlg-title b { font-weight: 800; }

.detail { display: flex; flex-direction: column; gap: var(--sp-5); }

.sum-strip { display: grid; grid-template-columns: repeat(5, 1fr); background: var(--surface-2); border: 1px solid var(--border); border-radius: var(--radius); padding: var(--sp-4) 0; }
.sum-strip .cell { display: flex; flex-direction: column; align-items: flex-start; gap: 5px; padding: 2px var(--sp-4); border-left: 1px solid var(--border); min-width: 0; }
.sum-strip .cell:first-child { border-left: none; }
.sum-strip .k { font-size: 11px; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.02em; }
.sum-strip .cell b { font-size: 15px; line-height: 1.4; }
.pay-val { display: inline-flex; align-items: center; flex-wrap: wrap; gap: 4px 8px; }
.pay-status { display: inline-flex; align-items: center; gap: 3px; font-size: 12px; font-weight: 600; }
.pay-status .pi { font-size: 12px; }
.pay-status.st-paid { color: var(--success); }
.pay-status.st-pending { color: #d97706; }
.pay-status.st-failed { color: var(--danger); }
@media (max-width: 720px) { .sum-strip { grid-template-columns: repeat(2, 1fr); gap: var(--sp-3) 0; } .sum-strip .cell:nth-child(odd) { border-left: none; } }

/* Khối trả góp */
.install-strip { display: flex; align-items: center; gap: var(--sp-3); flex-wrap: wrap; background: var(--brand-50); border: 1px solid var(--brand-100); border-radius: var(--radius); padding: var(--sp-3) var(--sp-4); font-size: 14px; color: var(--text); }
.install-strip > .pi { color: var(--brand); font-size: 1.1rem; }
.install-strip .im-monthly { color: var(--text-2); }

.order-tabs :deep(.p-tabpanels) { padding: var(--sp-5) 0 0; min-height: 260px; }
.order-tabs :deep(.p-tab) { display: inline-flex; align-items: center; gap: 6px; }
.tab-count { background: var(--brand-50); color: var(--brand); font-size: 11px; font-weight: 700; min-width: 18px; height: 18px; padding: 0 5px; border-radius: var(--radius-pill); display: inline-grid; place-items: center; margin-left: 6px; }

.items-wrap { border: 1px solid var(--border); border-radius: var(--radius); overflow-x: auto; }
.items { width: 100%; border-collapse: collapse; }
.items th, .items td { padding: 12px 16px; border-bottom: 1px solid var(--border); font-size: 14px; text-align: left; }
.items tbody tr:last-child td { border-bottom: none; }
.items th { background: var(--surface-2); font-size: 11px; font-weight: 600; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.02em; }
.items .pname { font-weight: 500; }
.items .r { text-align: right; } .items .c { text-align: center; }
.imei { display: inline-flex; align-items: center; gap: 5px; font-family: ui-monospace, monospace; font-size: 12.5px; color: var(--brand-dark); background: var(--brand-50); border-radius: var(--radius-sm); padding: 2px 8px; }
.imei .pi { font-size: 11px; }

.totals { margin: var(--sp-4) 0 0 auto; width: 320px; max-width: 100%; background: var(--surface-2); border: 1px solid var(--border); border-radius: var(--radius); padding: var(--sp-4) var(--sp-5); }
.totals .row { display: flex; justify-content: space-between; margin: 6px 0; font-size: 14px; }
.totals .minus { color: var(--success); }
.totals .grand { font-weight: 800; font-size: 17px; border-top: 1px dashed var(--border-strong); padding-top: 10px; margin-top: 10px; }

.info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: var(--sp-4); }
.info-item { display: flex; flex-direction: column; gap: 5px; background: var(--surface-2); border: 1px solid var(--border); border-radius: var(--radius); padding: var(--sp-3) var(--sp-4); }
.info-item.full { grid-column: 1 / -1; }
.info-item .k { font-size: 12px; color: var(--text-muted); display: flex; align-items: center; gap: 6px; }
.info-item .k .pi { color: var(--brand); font-size: 12px; }
.info-item b { font-size: 14.5px; font-weight: 600; color: var(--text); }

.stepper { display: flex; padding: var(--sp-5) var(--sp-2) var(--sp-4); overflow-x: auto; }
.step { flex: 1 1 0; min-width: 120px; text-align: center; }
.step-line { position: relative; display: flex; align-items: center; justify-content: center; height: 26px; }
.step-line .ln { flex: 1; height: 3px; background: transparent; }
.step-line .ln.on { background: var(--brand); }
.dot { position: relative; z-index: 1; width: 26px; height: 26px; flex-shrink: 0; border-radius: 50%; background: var(--brand); color: #fff; display: grid; place-items: center; box-shadow: 0 0 0 4px var(--brand-50); }
.dot .pi { font-size: 12px; }
.step-body { margin-top: 10px; padding: 0 6px; }
.step-body strong { font-size: 13.5px; display: block; }
.s-date { font-size: 11.5px; color: var(--text-muted); margin-top: 3px; }
.s-note { font-size: 11.5px; color: var(--text-2); margin-top: 3px; line-height: 1.4; }

/* Ô nhập IMEI trước khi giao */
.action-wrap { width: 100%; display: flex; flex-direction: column; gap: var(--sp-3); }
.imei-box { background: var(--surface-2); border: 1px solid var(--brand-100); border-radius: var(--radius); padding: var(--sp-3) var(--sp-4); }
.imei-title { font-size: 13px; font-weight: 700; color: var(--text); display: flex; align-items: center; gap: 6px; margin-bottom: var(--sp-2); }
.imei-title .pi { color: var(--brand); }
.imei-row { display: flex; align-items: center; gap: var(--sp-3); margin-top: 6px; }
.imei-name { flex: 1; min-width: 0; font-size: 13px; color: var(--text-2); }
.imei-name em { color: var(--text-muted); font-style: normal; }
.imei-input { width: 220px; flex-shrink: 0; }

.action-bar { display: flex; align-items: center; gap: 10px; width: 100%; }
.ab-flow { display: inline-flex; align-items: center; gap: 8px; flex-shrink: 0; background: var(--surface-2); border: 1px solid var(--border); border-radius: var(--radius); padding: 8px 12px; }
.ab-cur { font-size: 13px; font-weight: 700; color: var(--brand); white-space: nowrap; }
.ab-flow > .pi { font-size: 12px; color: var(--text-muted); }
.ab-select { width: 160px; flex-shrink: 0; }
.ab-note { flex: 1; min-width: 140px; }
.ab-btn { flex-shrink: 0; }
.terminal { display: flex; align-items: center; gap: 8px; color: var(--success); font-size: 13px; }
.terminal .pi { font-size: 1.2rem; }

@media (max-width: 700px) {
  .sum-strip .cell { border-left: none; padding: var(--sp-1) var(--sp-3); }
  .totals { width: 100%; }
  .info-grid { grid-template-columns: 1fr; }
  .imei-row { flex-wrap: wrap; }
  .imei-input { width: 100%; }
  .action-bar { flex-wrap: wrap; }
  .ab-note { flex: 1 1 100%; order: 3; }
  .ab-btn { margin-left: auto; }
}
</style>
