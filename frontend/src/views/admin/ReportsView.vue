<script setup lang="ts">
// Trang Báo cáo: tab "Tổng quan" (cơ bản, có drill-down) + 4 tab nâng cao
// (Tài chính / Khách hàng / Vận hành & Kho / Hành vi & Tìm kiếm) tách thành component riêng.
import { ref, computed, onMounted } from 'vue'
import Chart from 'primevue/chart'
import DatePicker from 'primevue/datepicker'
import Button from 'primevue/button'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'
import Dialog from 'primevue/dialog'
import ProgressSpinner from 'primevue/progressspinner'
import { reportApi, advReportApi } from '@/services'
import type {
  ReportSummary, RevenueReport, TopProductReport, CategoryRevenue,
  PaymentMethodRevenue, OrderStats, CountPoint, InventoryReport,
  SummaryOrder, NewCustomer, InventoryItem
} from '@/types'
import { formatCurrency, formatDate, orderStatusLabel, orderStatusSeverity, paymentMethodLabel } from '@/composables/format'
import OrderCodeLink from '@/components/OrderCodeLink.vue'
import ProductNameLink from '@/components/ProductNameLink.vue'
import ReportFinance from '@/views/admin/reports/ReportFinance.vue'
import ReportCustomers from '@/views/admin/reports/ReportCustomers.vue'
import ReportOperations from '@/views/admin/reports/ReportOperations.vue'
import ReportBehavior from '@/views/admin/reports/ReportBehavior.vue'

const tabs = [
  { key: 'overview', label: 'Tổng quan', icon: 'pi-chart-line' },
  { key: 'finance', label: 'Tài chính', icon: 'pi-dollar' },
  { key: 'customers', label: 'Khách hàng', icon: 'pi-users' },
  { key: 'operations', label: 'Vận hành & Kho', icon: 'pi-box' },
  { key: 'behavior', label: 'Hành vi & Tìm kiếm', icon: 'pi-compass' }
]
const tab = ref<'overview' | 'finance' | 'customers' | 'operations' | 'behavior'>('overview')

const loading = ref(true)
const range = ref<[Date, Date]>([daysAgo(29), new Date()])
const groupBy = ref<'day' | 'month'>('day')

const summary = ref<ReportSummary | null>(null)
const revenue = ref<RevenueReport | null>(null)
const topProducts = ref<TopProductReport[]>([])
const byCategory = ref<CategoryRevenue[]>([])
const byPayment = ref<PaymentMethodRevenue[]>([])
const orderStats = ref<OrderStats | null>(null)
const newCustomers = ref<CountPoint[]>([])
const inventory = ref<InventoryReport | null>(null)

function daysAgo(n: number) { const d = new Date(); d.setDate(d.getDate() - n); return d }
function iso(d: Date) { return d.toISOString().slice(0, 10) }

const presets = [
  { label: '7 ngày', days: 6 },
  { label: '30 ngày', days: 29 },
  { label: '90 ngày', days: 89 },
  { label: 'Năm nay', days: -1 }
]
function applyPreset(days: number) {
  if (days === -1) { const y = new Date(); range.value = [new Date(y.getFullYear(), 0, 1), new Date()] }
  else range.value = [daysAgo(days), new Date()]
  // Tự chuyển sang gộp theo tháng khi khoảng dài.
  const span = (range.value[1].getTime() - range.value[0].getTime()) / 86400000
  groupBy.value = span > 92 ? 'month' : 'day'
  load()
}

const params = computed(() => ({ from: iso(range.value[0]), to: iso(range.value[1]) }))

// ---------- Drill-down Tổng quan ----------
// Đơn hoàn tất: nguồn của Doanh thu / Đơn / Giá trị TB / Máy đã bán / Đã giảm giá
const soOpen = ref(false)
const soLoading = ref(false)
const soRows = ref<SummaryOrder[]>([])
const soSum = (k: 'total' | 'discount' | 'itemCount') => soRows.value.reduce((s, o) => s + o[k], 0)
async function drillSummary() {
  soOpen.value = true; soLoading.value = true; soRows.value = []
  try { soRows.value = await advReportApi.summaryOrders(params.value) } finally { soLoading.value = false }
}
// Khách mới trong kỳ
const ncOpen = ref(false)
const ncLoading = ref(false)
const ncRows = ref<NewCustomer[]>([])
async function drillNewCustomers() {
  ncOpen.value = true; ncLoading.value = true; ncRows.value = []
  try { ncRows.value = await advReportApi.newCustomerList(params.value) } finally { ncLoading.value = false }
}
// ---------- Drill-down Tồn kho ----------
const ivOpen = ref(false)
const ivTitle = ref('')
const ivLoading = ref(false)
const ivRows = ref<InventoryItem[]>([])
const ivStock = () => ivRows.value.reduce((s, i) => s + i.stock, 0)
const ivValue = () => ivRows.value.reduce((s, i) => s + i.value, 0)
async function drillInventory(bucket: string, title: string) {
  ivTitle.value = title; ivOpen.value = true; ivLoading.value = true; ivRows.value = []
  try { ivRows.value = await advReportApi.inventoryItems({ bucket }) } finally { ivLoading.value = false }
}

async function load() {
  loading.value = true
  try {
    const p = params.value
    const [s, rev, tp, cat, pay, os, nc, inv] = await Promise.all([
      reportApi.summary(p),
      reportApi.revenue({ ...p, groupBy: groupBy.value }),
      reportApi.topProducts({ ...p, limit: 10 }),
      reportApi.byCategory(p),
      reportApi.byPayment(p),
      reportApi.orderStats(p),
      reportApi.newCustomers({ ...p, groupBy: groupBy.value }),
      reportApi.inventory()
    ])
    summary.value = s; revenue.value = rev; topProducts.value = tp; byCategory.value = cat
    byPayment.value = pay; orderStats.value = os; newCustomers.value = nc; inventory.value = inv
  } finally { loading.value = false }
}

// ---------- Charts (tông xanh công nghệ) ----------
const brand = '#1e6fff'
const palette = ['#1e6fff', '#7c5cfc', '#16a34a', '#f59e0b', '#e8453c', '#0ea5e9', '#ec4899']

const revenueChart = computed(() => ({
  labels: revenue.value?.series.map((p) => p.label) ?? [],
  datasets: [{
    label: 'Doanh thu', data: revenue.value?.series.map((p) => p.revenue) ?? [],
    borderColor: brand, backgroundColor: 'rgba(30,111,255,.12)', fill: true, tension: 0.35, yAxisID: 'y'
  }, {
    label: 'Số đơn', data: revenue.value?.series.map((p) => p.orders) ?? [],
    borderColor: '#7c5cfc', backgroundColor: 'transparent', tension: 0.35, yAxisID: 'y1', type: 'line'
  }]
}))
const revenueOpts = {
  maintainAspectRatio: false,
  plugins: { legend: { position: 'top' } },
  scales: { y: { position: 'left', ticks: { callback: (v: number) => (v / 1e6) + 'tr' } }, y1: { position: 'right', grid: { drawOnChartArea: false } } }
}

const categoryChart = computed(() => ({
  labels: byCategory.value.map((c) => c.category),
  datasets: [{ label: 'Doanh thu', data: byCategory.value.map((c) => c.revenue), backgroundColor: palette }]
}))
const barOpts = { maintainAspectRatio: false, indexAxis: 'y' as const, plugins: { legend: { display: false } } }

const paymentChart = computed(() => ({
  labels: byPayment.value.map((p) => paymentMethodLabel[p.method] || p.method),
  datasets: [{ data: byPayment.value.map((p) => p.revenue), backgroundColor: palette }]
}))
const statusChart = computed(() => ({
  labels: Object.keys(orderStats.value?.byStatus ?? {}).map((s) => orderStatusLabel[s] || s),
  datasets: [{ data: Object.values(orderStats.value?.byStatus ?? {}), backgroundColor: ['#f59e0b', '#1e6fff', '#7c5cfc', '#16a34a', '#e8453c'] }]
}))
const custChart = computed(() => ({
  labels: newCustomers.value.map((c) => c.label),
  datasets: [{ label: 'Khách mới', data: newCustomers.value.map((c) => c.count), backgroundColor: brand }]
}))

// Luồng vòng đời đơn: Đặt hàng → Xác nhận → Đang giao → Hoàn tất.
const flow = computed(() => {
  const bs = orderStats.value?.byStatus ?? {}
  return [
    { key: 'Pending', label: 'Đặt hàng', icon: 'pi-shopping-cart', count: bs['Pending'] || 0, color: '#f59e0b' },
    { key: 'Confirmed', label: 'Xác nhận', icon: 'pi-check-circle', count: bs['Confirmed'] || 0, color: '#1e6fff' },
    { key: 'Shipping', label: 'Đang giao', icon: 'pi-truck', count: bs['Shipping'] || 0, color: '#7c5cfc' },
    { key: 'Completed', label: 'Hoàn tất', icon: 'pi-flag-fill', count: bs['Completed'] || 0, color: '#16a34a' }
  ]
})
const cancelledCount = computed(() => orderStats.value?.byStatus?.['Cancelled'] || 0)
const pieOpts = { maintainAspectRatio: false, plugins: { legend: { position: 'bottom' } } }
const barVOpts = { maintainAspectRatio: false, plugins: { legend: { display: false } } }

// ---------- CSV export ----------
function downloadCsv(filename: string, rows: (string | number)[][]) {
  const csv = rows.map((r) => r.map((c) => `"${String(c).replace(/"/g, '""')}"`).join(',')).join('\n')
  const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8;' })
  const a = document.createElement('a')
  a.href = URL.createObjectURL(blob); a.download = filename; a.click(); URL.revokeObjectURL(a.href)
}
function exportRevenue() {
  const rows: (string | number)[][] = [['Thời gian', 'Doanh thu', 'Số đơn']]
  revenue.value?.series.forEach((p) => rows.push([p.label, p.revenue, p.orders]))
  downloadCsv(`doanh-thu_${params.value.from}_${params.value.to}.csv`, rows)
}
function exportTopProducts() {
  const rows: (string | number)[][] = [['Máy', 'Đã bán', 'Doanh thu']]
  topProducts.value.forEach((p) => rows.push([p.productName, p.quantitySold, p.revenue]))
  downloadCsv(`top-may_${params.value.from}_${params.value.to}.csv`, rows)
}

onMounted(load)
</script>

<template>
  <div class="head">
    <h1>Báo cáo &amp; Thống kê</h1>
    <div class="range-tools" v-if="tab === 'overview'">
      <button v-for="p in presets" :key="p.label" class="preset" @click="applyPreset(p.days)">{{ p.label }}</button>
      <DatePicker v-model="range" selectionMode="range" dateFormat="dd/mm/yy" :manualInput="false" size="small" showIcon @update:modelValue="range[1] && load()" />
      <div class="grp">
        <button :class="{ active: groupBy === 'day' }" @click="groupBy = 'day'; load()">Ngày</button>
        <button :class="{ active: groupBy === 'month' }" @click="groupBy = 'month'; load()">Tháng</button>
      </div>
    </div>
  </div>

  <!-- Tab loại báo cáo -->
  <div class="report-tabs">
    <button v-for="t in tabs" :key="t.key" class="rtab" :class="{ active: tab === t.key }" @click="tab = t.key as any">
      <i class="pi" :class="t.icon" /> {{ t.label }}
    </button>
  </div>

  <ReportFinance v-if="tab === 'finance'" />
  <ReportCustomers v-else-if="tab === 'customers'" />
  <ReportOperations v-else-if="tab === 'operations'" />
  <ReportBehavior v-else-if="tab === 'behavior'" />

  <template v-else>
  <div v-if="loading" class="center"><ProgressSpinner /></div>
  <template v-else>
    <!-- KPI tổng quan — bấm 1 thẻ để xem các đơn / khách tạo nên con số -->
    <div class="kpis">
      <div class="kpi drillable" @click="drillSummary"><i class="pi pi-dollar" /><div><span>Doanh thu <i class="pi pi-search-plus" /></span><strong>{{ formatCurrency(summary?.revenue || 0) }}</strong></div></div>
      <div class="kpi drillable" @click="drillSummary"><i class="pi pi-shopping-bag" /><div><span>Đơn hoàn tất <i class="pi pi-search-plus" /></span><strong>{{ summary?.orders || 0 }}</strong></div></div>
      <div class="kpi drillable" @click="drillSummary"><i class="pi pi-chart-line" /><div><span>Giá trị TB/đơn <i class="pi pi-search-plus" /></span><strong>{{ formatCurrency(summary?.avgOrderValue || 0) }}</strong></div></div>
      <div class="kpi drillable" @click="drillSummary"><i class="pi pi-mobile" /><div><span>Máy đã bán <i class="pi pi-search-plus" /></span><strong>{{ summary?.itemsSold || 0 }}</strong></div></div>
      <div class="kpi drillable" @click="drillNewCustomers"><i class="pi pi-users" /><div><span>Khách mới <i class="pi pi-search-plus" /></span><strong>{{ summary?.newCustomers || 0 }}</strong></div></div>
      <div class="kpi drillable" @click="drillSummary"><i class="pi pi-tag" /><div><span>Đã giảm giá <i class="pi pi-search-plus" /></span><strong>{{ formatCurrency(summary?.discounts || 0) }}</strong></div></div>
    </div>

    <!-- Drill: đơn hoàn tất tạo nên Doanh thu/Đơn/Máy đã bán/Giảm giá -->
    <Dialog v-model:visible="soOpen" modal header="Đơn hoàn tất trong kỳ" :style="{ width: '860px' }" :dismissableMask="true">
      <div v-if="soLoading" class="drill-load"><i class="pi pi-spin pi-spinner" /> Đang tải...</div>
      <template v-else>
        <DataTable :value="soRows" size="small" stripedRows paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
          <Column header="Mã đơn"><template #body="{ data }"><OrderCodeLink :id="data.id" :code="data.orderCode" /></template></Column>
          <Column header="Ngày"><template #body="{ data }">{{ formatDate(data.createdAt) }}</template></Column>
          <Column header="Trạng thái"><template #body="{ data }"><Tag :value="orderStatusLabel[data.status] || data.status" :severity="orderStatusSeverity[data.status]" /></template></Column>
          <Column header="SL máy"><template #body="{ data }">{{ data.itemCount }}</template></Column>
          <Column header="Giảm giá"><template #body="{ data }">{{ formatCurrency(data.discount) }}</template></Column>
          <Column header="Doanh thu"><template #body="{ data }"><b class="good-t">{{ formatCurrency(data.total) }}</b></template></Column>
          <template #empty><div class="empty-row">Không có đơn hoàn tất trong kỳ.</div></template>
        </DataTable>
        <div class="drill-sum">
          <span>{{ soRows.length }} đơn</span>
          <span class="pt">Máy đã bán: <b>{{ soSum('itemCount') }}</b></span>
          <span class="pt">Giảm giá: <b>{{ formatCurrency(soSum('discount')) }}</b></span>
          <span class="pt">Doanh thu: <b class="good-t">{{ formatCurrency(soSum('total')) }}</b></span>
        </div>
      </template>
    </Dialog>

    <!-- Drill: khách mới trong kỳ -->
    <Dialog v-model:visible="ncOpen" modal header="Khách hàng mới trong kỳ" :style="{ width: '680px' }" :dismissableMask="true">
      <div v-if="ncLoading" class="drill-load"><i class="pi pi-spin pi-spinner" /> Đang tải...</div>
      <template v-else>
        <DataTable :value="ncRows" size="small" stripedRows paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
          <Column field="name" header="Khách" />
          <Column field="email" header="Email" />
          <Column header="Ngày đăng ký"><template #body="{ data }">{{ formatDate(data.createdAt) }}</template></Column>
          <template #empty><div class="empty-row">Không có khách mới trong kỳ.</div></template>
        </DataTable>
        <div class="drill-sum"><span>{{ ncRows.length }} khách mới</span></div>
      </template>
    </Dialog>

    <!-- Tồn kho -->
    <div class="box">
      <h3><i class="pi pi-warehouse ttl-ic" /> Báo cáo tồn kho</h3>
      <div class="inv-kpis">
        <div class="inv val drillable" @click="drillInventory('all', 'Tất cả phân loại · giá trị tồn kho')"><i class="pi pi-wallet" /><div><span>Giá trị tồn kho <i class="pi pi-search-plus" /></span><strong>{{ formatCurrency(inventory?.stockValue || 0) }}</strong></div></div>
        <div class="inv total drillable" @click="drillInventory('all', 'Tất cả phân loại tồn kho')"><i class="pi pi-mobile" /><div><span>Tổng máy <i class="pi pi-search-plus" /></span><strong>{{ inventory?.totalProducts || 0 }}</strong></div></div>
        <div class="inv units drillable" @click="drillInventory('all', 'Tất cả phân loại · đơn vị tồn')"><i class="pi pi-database" /><div><span>Đơn vị tồn <i class="pi pi-search-plus" /></span><strong>{{ inventory?.totalStockUnits || 0 }}</strong></div></div>
        <div class="inv warn drillable" @click="drillInventory('low', 'Phân loại sắp hết (tồn 1–9)')"><i class="pi pi-exclamation-triangle" /><div><span>Sắp hết ({{ '<' }}10) <i class="pi pi-search-plus" /></span><strong>{{ inventory?.lowStockCount || 0 }}</strong></div></div>
        <div class="inv danger drillable" @click="drillInventory('out', 'Phân loại đã hết hàng')"><i class="pi pi-times-circle" /><div><span>Hết hàng <i class="pi pi-search-plus" /></span><strong>{{ inventory?.outOfStockCount || 0 }}</strong></div></div>
      </div>

      <!-- Drill: phân loại tồn kho theo nhóm -->
      <Dialog v-model:visible="ivOpen" modal :header="ivTitle" :style="{ width: '840px' }" :dismissableMask="true">
        <div v-if="ivLoading" class="drill-load"><i class="pi pi-spin pi-spinner" /> Đang tải...</div>
        <template v-else>
          <DataTable :value="ivRows" size="small" stripedRows paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
            <Column header="Máy"><template #body="{ data }"><ProductNameLink :id="data.productId" :name="data.productName" /></template></Column>
            <Column field="sku" header="SKU" />
            <Column header="Phân loại"><template #body="{ data }">{{ data.variant?.trim() || '—' }}</template></Column>
            <Column header="Tồn"><template #body="{ data }"><Tag :value="String(data.stock)" :severity="data.stock === 0 ? 'danger' : data.stock < 10 ? 'warn' : 'success'" /></template></Column>
            <Column header="Giá"><template #body="{ data }">{{ formatCurrency(data.price) }}</template></Column>
            <Column header="Giá trị tồn"><template #body="{ data }"><b>{{ formatCurrency(data.value) }}</b></template></Column>
            <template #empty><div class="empty-row">Không có phân loại nào trong nhóm này.</div></template>
          </DataTable>
          <div class="drill-sum">
            <span>{{ ivRows.length }} phân loại</span>
            <span class="pt">Đơn vị tồn: <b>{{ ivStock() }}</b></span>
            <span class="pt">Giá trị tồn: <b class="good-t">{{ formatCurrency(ivValue()) }}</b></span>
          </div>
        </template>
      </Dialog>
      <h4 class="sub">Máy sắp hết / hết hàng</h4>
      <DataTable :value="inventory?.lowStockItems" size="small" stripedRows
        paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
        <Column header="Máy"><template #body="{ data }"><ProductNameLink :id="data.productId" :name="data.productName" /></template></Column>
        <Column field="sku" header="SKU" />
        <Column header="Phân loại"><template #body="{ data }">{{ data.variant?.trim() || '—' }}</template></Column>
        <Column header="Tồn"><template #body="{ data }">
          <Tag :value="String(data.stock)" :severity="data.stock === 0 ? 'danger' : 'warn'" /></template>
        </Column>
        <template #empty><div class="empty-row">Kho ổn định — không có máy sắp hết.</div></template>
      </DataTable>
    </div>

    <!-- Revenue chart -->
    <div class="box">
      <div class="box-head"><h3>Doanh thu theo {{ groupBy === 'month' ? 'tháng' : 'ngày' }}</h3>
        <Button label="Xuất CSV" icon="pi pi-download" size="small" outlined @click="exportRevenue" /></div>
      <div class="chart-lg"><Chart type="line" :data="revenueChart" :options="revenueOpts" /></div>
    </div>

    <div class="grid-2">
      <div class="box"><h3>Doanh thu theo danh mục</h3>
        <div class="chart-md"><Chart type="bar" :data="categoryChart" :options="barOpts" /></div>
      </div>
      <div class="box"><h3>Doanh thu theo thanh toán</h3>
        <div class="chart-md"><Chart type="doughnut" :data="paymentChart" :options="pieOpts" /></div>
      </div>
    </div>

    <div class="grid-2">
      <div class="box">
        <h3>Tỉ lệ xử lý đơn</h3>
        <div class="rates">
          <div class="rate"><span class="big" style="color:var(--success)">{{ orderStats?.completionRate || 0 }}%</span><span>Hoàn tất</span></div>
          <div class="rate"><span class="big" style="color:var(--danger)">{{ orderStats?.cancelRate || 0 }}%</span><span>Bị hủy</span></div>
          <div class="rate"><span class="big">{{ orderStats?.total || 0 }}</span><span>Tổng đơn</span></div>
        </div>

        <!-- Luồng mũi tên vòng đời đơn -->
        <div class="flow">
          <template v-for="(st, i) in flow" :key="st.key">
            <div class="flow-node" :style="{ '--c': st.color }">
              <div class="fn-ic"><i class="pi" :class="st.icon" /></div>
              <strong>{{ st.count }}</strong>
              <span>{{ st.label }}</span>
            </div>
            <div v-if="i < flow.length - 1" class="flow-arrow"><span class="line" /><i class="pi pi-angle-right" /></div>
          </template>
        </div>
        <div class="flow-cancel" v-if="cancelledCount > 0">
          <i class="pi pi-times-circle" /> {{ cancelledCount }} đơn bị hủy giữa chừng (không đi hết luồng)
        </div>

        <div class="chart-sm"><Chart type="doughnut" :data="statusChart" :options="pieOpts" /></div>
      </div>
      <div class="box"><h3>Khách hàng mới</h3>
        <div class="chart-md"><Chart type="bar" :data="custChart" :options="barVOpts" /></div>
      </div>
    </div>

    <!-- Top products -->
    <div class="box">
      <div class="box-head"><h3>Top máy bán chạy</h3>
        <Button label="Xuất CSV" icon="pi pi-download" size="small" outlined @click="exportTopProducts" /></div>
      <DataTable :value="topProducts" size="small" stripedRows>
        <Column header="#"><template #body="{ index }">{{ index + 1 }}</template></Column>
        <Column header="Máy"><template #body="{ data }"><ProductNameLink :id="data.productId" :name="data.productName" /></template></Column>
        <Column header="Đã bán"><template #body="{ data }">{{ data.quantitySold }}</template></Column>
        <Column header="Doanh thu"><template #body="{ data }">{{ formatCurrency(data.revenue) }}</template></Column>
        <template #empty><div class="empty-row">Chưa có dữ liệu bán hàng trong kỳ.</div></template>
      </DataTable>
    </div>
  </template>
  </template>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: flex-start; gap: var(--sp-3); margin-bottom: var(--sp-4); flex-wrap: wrap; }
.head h1 { margin: 0; }
.range-tools { display: flex; align-items: center; gap: var(--sp-2); flex-wrap: wrap; }
.preset { background: var(--surface); border: 1px solid var(--border); color: var(--text-2); padding: 5px 12px; border-radius: var(--radius-pill); cursor: pointer; font-family: inherit; font-size: 13px; transition: all var(--ease); }
.preset:hover { border-color: var(--brand); color: var(--brand); }
.grp { display: inline-flex; border: 1px solid var(--border); border-radius: var(--radius-sm); overflow: hidden; }
.grp button { background: var(--surface); border: none; padding: 6px 12px; cursor: pointer; font-family: inherit; font-size: 13px; color: var(--text-2); }
.grp button.active { background: var(--brand); color: #fff; }
.center { display: flex; justify-content: center; padding: 3rem; }

/* Tab loại báo cáo */
.report-tabs { display: flex; gap: var(--sp-1); border-bottom: 2px solid var(--border); margin-bottom: var(--sp-4); overflow-x: auto; }
.rtab { display: inline-flex; align-items: center; gap: 6px; background: none; border: none; border-bottom: 2px solid transparent; margin-bottom: -2px; padding: 10px 16px; cursor: pointer; font-family: inherit; font-size: 14px; color: var(--text-2); white-space: nowrap; transition: all var(--ease); }
.rtab:hover { color: var(--brand); }
.rtab.active { color: var(--brand); border-bottom-color: var(--brand); font-weight: 600; }

/* Drill-down (Dialog xem đơn/khách/phân loại tạo nên con số) */
.drill-load { display: flex; align-items: center; gap: 8px; padding: var(--sp-5); color: var(--text-muted); justify-content: center; }
.drill-sum { display: flex; justify-content: space-between; align-items: center; gap: var(--sp-4); flex-wrap: wrap; margin-top: var(--sp-3); padding-top: var(--sp-3); border-top: 1px solid var(--border); font-size: 14px; }
.drill-sum .pt { color: var(--text-2); font-size: 13px; } .drill-sum .pt b { color: var(--text); }
.good-t { color: var(--success); }

.kpis { display: grid; grid-template-columns: repeat(6, minmax(0, 1fr)); gap: var(--sp-3); margin-bottom: var(--sp-3); }
.kpi { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); padding: var(--sp-4); display: flex; align-items: center; gap: var(--sp-3); min-width: 0; transition: border-color var(--ease), box-shadow var(--ease), transform var(--ease); }
.kpi:hover { border-color: var(--brand-100); box-shadow: var(--shadow); transform: translateY(-1px); }
.kpi > i { font-size: 1.2rem; color: var(--brand); background: var(--brand-50); padding: 9px; border-radius: var(--radius-sm); flex-shrink: 0; }
.kpi > div { min-width: 0; }
.kpi span { color: var(--text-muted); font-size: 12px; display: inline-flex; align-items: center; gap: 4px; }
.kpi strong { display: block; font-size: 1.1rem; margin-top: 2px; overflow-wrap: anywhere; }
/* Thẻ bấm được để drill-down: icon kính lúp mờ, hover nhấc nhẹ */
.kpi.drillable, .inv.drillable { cursor: pointer; }
.kpi span i, .inv span i { font-size: 10px; color: var(--text-muted); opacity: .55; }
.inv.drillable:hover { box-shadow: var(--shadow); transform: translateY(-1px); }

.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); padding: var(--sp-4); margin-bottom: var(--sp-3); }
.box-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--sp-3); }
.box h3 { margin: 0 0 var(--sp-3); font-size: 1rem; }
.box-head h3 { margin: 0; }
.chart-lg { height: 320px; } .chart-md { height: 260px; } .chart-sm { height: 200px; }
.grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: var(--sp-3); }
.grid-2 .box { margin-bottom: 0; }
.grid-2 + .grid-2 { margin-top: var(--sp-3); }

.rates { display: flex; gap: var(--sp-4); margin-bottom: var(--sp-2); }
.rate { text-align: center; } .rate .big { font-size: 1.3rem; font-weight: 800; display: block; line-height: 1.15; } .rate span:last-child { font-size: 11px; color: var(--text-muted); }

/* Luồng mũi tên vòng đời đơn */
.flow { display: flex; align-items: stretch; justify-content: space-between; gap: 2px; margin-bottom: var(--sp-2); }
.flow-node { flex: 1; display: flex; flex-direction: column; align-items: center; gap: 1px; padding: var(--sp-2) 4px; border-radius: var(--radius-sm); background: color-mix(in srgb, var(--c) 8%, var(--surface)); border: 1px solid color-mix(in srgb, var(--c) 30%, transparent); }
.flow-node .fn-ic { width: 24px; height: 24px; border-radius: 50%; display: grid; place-items: center; background: var(--c); color: #fff; margin-bottom: 1px; }
.flow-node .fn-ic .pi { font-size: 12px; }
.flow-node strong { font-size: 1.05rem; line-height: 1; color: var(--c); }
.flow-node span { font-size: 11px; color: var(--text-2); white-space: nowrap; }
.flow-arrow { display: flex; align-items: center; align-self: center; color: var(--text-muted); }
.flow-arrow .line { width: 8px; height: 2px; background: currentColor; opacity: .4; }
.flow-arrow .pi { font-size: 12px; margin-left: -3px; }
.flow-cancel { display: flex; align-items: center; gap: 6px; font-size: 11.5px; color: var(--danger); background: #fef2f2; border-radius: var(--radius-sm); padding: 5px 9px; }
@media (max-width: 520px) { .flow-node span { font-size: 10px; } .flow-arrow .line { width: 4px; } }

.inv-kpis { display: grid; grid-template-columns: repeat(5, 1fr); gap: var(--sp-3); margin-bottom: var(--sp-4); }
.inv { display: flex; align-items: center; gap: 10px; background: var(--surface-2); border: 1px solid var(--border); border-left: 3px solid var(--border-strong); border-radius: var(--radius); padding: var(--sp-3); transition: box-shadow var(--ease), transform var(--ease); }
.inv:hover { box-shadow: var(--shadow-sm); transform: translateY(-1px); }
.inv i { font-size: 1.2rem; padding: 9px; border-radius: var(--radius-sm); flex-shrink: 0; }
.inv span { font-size: 12px; color: var(--text-muted); display: block; }
.inv strong { font-size: 1.15rem; }
.inv.val { border-left-color: var(--brand); } .inv.val i { color: var(--brand); background: var(--brand-50); }
.inv.total { border-left-color: #7c5cfc; } .inv.total i { color: #6d47f0; background: #f1edff; }
.inv.units { border-left-color: var(--success); } .inv.units i { color: #0e8a7a; background: #e6f7f4; }
.inv.warn { border-left-color: #f59e0b; background: #fff7ed; } .inv.warn i { color: #b45309; background: #ffedd5; }
.inv.danger { border-left-color: #ef4444; background: #fef2f2; } .inv.danger i { color: #dc2626; background: #fee2e2; }
.ttl-ic { color: var(--brand); margin-right: 4px; }
.sub { font-size: 0.9rem; margin: var(--sp-2) 0 var(--sp-3); }
.empty-row { padding: var(--sp-5); text-align: center; color: var(--text-muted); }

@media (max-width: 900px) { .kpis { grid-template-columns: repeat(3, 1fr); } .grid-2 { grid-template-columns: 1fr; } .grid-2 .box { margin-bottom: var(--sp-3); } .inv-kpis { grid-template-columns: repeat(2, 1fr); } }
@media (max-width: 640px) { .kpis { grid-template-columns: repeat(2, 1fr); } .kpi strong { font-size: 1rem; } .kpi { padding: var(--sp-3); } }
@media (max-width: 480px) { .kpis, .inv-kpis { grid-template-columns: 1fr; } }
</style>
