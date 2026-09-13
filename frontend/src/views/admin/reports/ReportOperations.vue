<script setup lang="ts">
import { ref, onMounted } from 'vue'
import Chart from 'primevue/chart'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'
import Dialog from 'primevue/dialog'
import ReportRangeBar from '@/components/ReportRangeBar.vue'
import OrderCodeLink from '@/components/OrderCodeLink.vue'
import ProductNameLink from '@/components/ProductNameLink.vue'
import { advReportApi } from '@/services'
import type { DemandItem, ProcessingTime, ProcessingOrder, CancelReason } from '@/types'
import { formatCurrency, formatDate } from '@/composables/format'

const demand = ref<DemandItem[]>([])
const proc = ref<ProcessingTime | null>(null)
const cancels = ref<CancelReason[]>([])
const range = ref<{ from: string; to: string }>({ from: '', to: '' })

onMounted(async () => { demand.value = await advReportApi.demand() })
async function loadRange(p: { from: string; to: string }) {
  range.value = p
  const [pt, cr] = await Promise.all([advReportApi.processingTime(p), advReportApi.cancelReasons(p)])
  proc.value = pt; cancels.value = cr
}

// ----- Drill-down: đơn mẫu kèm giờ từng chặng -----
const dOpen = ref(false)
const dLoading = ref(false)
const dRows = ref<ProcessingOrder[]>([])
type Stage = 'confirmHours' | 'shipHours' | 'completeHours' | 'totalHours'
const dAvg = (k: Stage) => dRows.value.length ? Math.round(dRows.value.reduce((s, o) => s + o[k], 0) / dRows.value.length * 10) / 10 : 0

async function drillProc() {
  dOpen.value = true; dLoading.value = true; dRows.value = []
  try {
    dRows.value = await advReportApi.processingOrders(range.value)
  } finally {
    dLoading.value = false
  }
}

function daysSeverity(d: number) { return d < 7 ? 'danger' : d < 15 ? 'warn' : 'success' }
const cancelChart = () => ({
  labels: cancels.value.map((c) => c.reason),
  datasets: [{ data: cancels.value.map((c) => c.count), backgroundColor: ['#1e6fff', '#eab308', '#3b82f6', '#8b5cf6', '#22aa99', '#94a3b8'] }]
})
const pieOpts = { maintainAspectRatio: false, plugins: { legend: { position: 'bottom' } } }
</script>

<template>
  <ReportRangeBar @change="loadRange" />

  <!-- C2 Thời gian xử lý — bấm 1 thẻ để xem các đơn mẫu -->
  <div class="rp-kpis">
    <div class="rp-kpi drillable" @click="drillProc"><span>TB Chờ → Xác nhận <i class="pi pi-search-plus" /></span><strong>{{ proc?.avgConfirmHours || 0 }} giờ</strong></div>
    <div class="rp-kpi drillable" @click="drillProc"><span>TB Xác nhận → Giao <i class="pi pi-search-plus" /></span><strong>{{ proc?.avgShipHours || 0 }} giờ</strong></div>
    <div class="rp-kpi drillable" @click="drillProc"><span>TB Giao → Hoàn tất <i class="pi pi-search-plus" /></span><strong>{{ proc?.avgCompleteHours || 0 }} giờ</strong></div>
    <div class="rp-kpi good drillable" @click="drillProc"><span>TB Tổng vòng đời <i class="pi pi-search-plus" /></span><strong>{{ proc?.avgTotalHours || 0 }} giờ</strong></div>
    <div class="rp-kpi drillable" @click="drillProc"><span>Số đơn mẫu <i class="pi pi-search-plus" /></span><strong>{{ proc?.sampleSize || 0 }}</strong></div>
  </div>

  <!-- Drill-down: đơn mẫu kèm giờ từng chặng -->
  <Dialog v-model:visible="dOpen" modal header="Các đơn mẫu · thời gian xử lý từng chặng" :style="{ width: '880px' }" :dismissableMask="true">
    <div v-if="dLoading" class="drill-load"><i class="pi pi-spin pi-spinner" /> Đang tải...</div>
    <template v-else>
      <DataTable :value="dRows" size="small" stripedRows paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
        <Column header="Mã đơn"><template #body="{ data }"><OrderCodeLink :id="data.id" :code="data.orderCode" /></template></Column>
        <Column header="Ngày đặt"><template #body="{ data }">{{ formatDate(data.createdAt) }}</template></Column>
        <Column header="Chờ→XN"><template #body="{ data }">{{ data.confirmHours }} giờ</template></Column>
        <Column header="XN→Giao"><template #body="{ data }">{{ data.shipHours }} giờ</template></Column>
        <Column header="Giao→HT"><template #body="{ data }">{{ data.completeHours }} giờ</template></Column>
        <Column header="Tổng"><template #body="{ data }"><b class="good-t">{{ data.totalHours }} giờ</b></template></Column>
        <template #empty><div class="rp-empty">Không có đơn hoàn tất trong kỳ.</div></template>
      </DataTable>
      <div class="drill-sum">
        <span>{{ dRows.length }} đơn mẫu</span>
        <span class="pt">TB Chờ→XN: <b>{{ dAvg('confirmHours') }}h</b></span>
        <span class="pt">TB XN→Giao: <b>{{ dAvg('shipHours') }}h</b></span>
        <span class="pt">TB Giao→HT: <b>{{ dAvg('completeHours') }}h</b></span>
        <span class="pt">TB Tổng: <b class="good-t">{{ dAvg('totalHours') }}h</b></span>
      </div>
    </template>
  </Dialog>

  <div class="rp-grid2">
    <!-- C3 Lý do hủy -->
    <div class="rp-box"><h3><i class="pi pi-times-circle" />Lý do hủy đơn</h3>
      <div class="rp-chart-sm"><Chart type="doughnut" :data="cancelChart()" :options="pieOpts" /></div>
    </div>
    <div class="rp-box"><h3><i class="pi pi-list" />Chi tiết hủy đơn</h3>
      <DataTable :value="cancels" size="small" stripedRows>
        <Column field="reason" header="Lý do" />
        <Column header="Số đơn"><template #body="{ data }">{{ data.count }}</template></Column>
        <Column header="DT mất"><template #body="{ data }">{{ formatCurrency(data.lostRevenue) }}</template></Column>
        <template #empty><div class="rp-empty">Không có đơn hủy trong kỳ — tốt!</div></template>
      </DataTable>
    </div>
  </div>

  <!-- C1 Dự báo nhập kho -->
  <div class="rp-box">
    <h3><i class="pi pi-box" />Dự báo nhập kho (theo tốc độ bán 30 ngày)</h3>
    <DataTable :value="demand" size="small" stripedRows paginator :rows="10">
      <Column header="Máy"><template #body="{ data }"><ProductNameLink :id="data.productId" :name="data.productName" /></template></Column>
      <Column field="sku" header="SKU" />
      <Column header="Tồn"><template #body="{ data }">{{ data.stock }}</template></Column>
      <Column header="Bán/ngày"><template #body="{ data }">{{ data.avgDailySold }}</template></Column>
      <Column header="Còn đủ bán">
        <template #body="{ data }"><Tag :value="data.daysLeft > 900 ? '∞' : data.daysLeft + ' ngày'" :severity="daysSeverity(data.daysLeft)" /></template>
      </Column>
      <Column header="Gợi ý nhập">
        <template #body="{ data }"><strong v-if="data.suggestedReorder > 0" style="color:var(--brand)">+{{ data.suggestedReorder }}</strong><span v-else class="text-muted">—</span></template>
      </Column>
      <template #empty><div class="rp-empty">Chưa có dữ liệu bán để dự báo.</div></template>
    </DataTable>
  </div>
</template>
