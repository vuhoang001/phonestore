<script setup lang="ts">
import { ref, onMounted } from 'vue'
import Chart from 'primevue/chart'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Dialog from 'primevue/dialog'
import ReportRangeBar from '@/components/ReportRangeBar.vue'
import { advReportApi } from '@/services'
import type { ChurnReport, ChurnCustomer, ProductPair, RfmReport, CohortReport } from '@/types'
import { formatCurrency, formatDate } from '@/composables/format'

const churn = ref<ChurnReport | null>(null)
const rfm = ref<RfmReport | null>(null)
const cohort = ref<CohortReport | null>(null)
// Ô cohort: đậm dần theo % giữ chân; ô không có khách quay lại thì để nhạt.
function cellStyle(pct: number, count: number) {
  if (!count) return { background: 'var(--surface-2)', color: 'var(--text-muted)' }
  return { background: `color-mix(in srgb, var(--brand) ${Math.round(pct)}%, var(--surface))`, color: pct > 55 ? '#fff' : 'var(--text)' }
}
const basket = ref<ProductPair[]>([])
const range = ref<{ from: string; to: string }>({ from: '', to: '' })

async function loadBasket(p: { from: string; to: string }) { basket.value = await advReportApi.marketBasket(p) }
// Bộ lọc thời gian: phân tích vòng đời khách "tính đến" cuối kỳ đã chọn.
async function load(p: { from: string; to: string }) {
  range.value = p
  churn.value = await advReportApi.churn(p)
}
onMounted(async () => {
  rfm.value = await advReportApi.rfm()
  cohort.value = await advReportApi.cohort()
})

// ----- Drill-down: khách trong từng nhóm vòng đời -----
const dOpen = ref(false)
const dTitle = ref('')
const dLoading = ref(false)
const dRows = ref<ChurnCustomer[]>([])
const dSpent = () => dRows.value.reduce((s, c) => s + c.totalSpent, 0)

async function drill(bucket: string, title: string) {
  dTitle.value = title
  dOpen.value = true; dLoading.value = true; dRows.value = []
  try {
    dRows.value = await advReportApi.churnCustomers({ ...range.value, bucket })
  } finally {
    dLoading.value = false
  }
}

const churnChart = () => ({
  labels: ['Đang hoạt động', 'Nguy cơ rời', 'Đã rời bỏ', 'Chưa mua'],
  datasets: [{ data: [churn.value?.active || 0, churn.value?.atRisk || 0, churn.value?.churned || 0, churn.value?.neverOrdered || 0],
    backgroundColor: ['#22aa99', '#eab308', '#ef4444', '#94a3b8'] }]
})
const rfmChart = () => ({
  labels: rfm.value?.segments.map((s) => s.segment) ?? [],
  datasets: [{ data: rfm.value?.segments.map((s) => s.customers) ?? [], backgroundColor: ['#1e6fff', '#22aa99', '#3b82f6', '#eab308', '#8b5cf6', '#94a3b8'] }]
})
const pieOpts = { maintainAspectRatio: false, plugins: { legend: { position: 'bottom' } } }
</script>

<template>
  <ReportRangeBar @change="load" />

  <!-- B1 Churn — bấm 1 thẻ để xem danh sách khách trong nhóm -->
  <div class="rp-kpis">
    <div class="rp-kpi drillable" @click="drill('all', 'Tất cả khách hàng')"><span>Tổng khách <i class="pi pi-search-plus" /></span><strong>{{ churn?.totalCustomers || 0 }}</strong></div>
    <div class="rp-kpi good drillable" @click="drill('active', 'Khách đang hoạt động (≤30 ngày)')"><span>Đang hoạt động (≤30d) <i class="pi pi-search-plus" /></span><strong>{{ churn?.active || 0 }}</strong></div>
    <div class="rp-kpi drillable" @click="drill('atRisk', 'Khách có nguy cơ rời (31–90 ngày)')"><span>Nguy cơ rời (31–90d) <i class="pi pi-search-plus" /></span><strong>{{ churn?.atRisk || 0 }}</strong></div>
    <div class="rp-kpi bad drillable" @click="drill('churned', 'Khách đã rời bỏ (>90 ngày)')"><span>Đã rời bỏ (>90d) <i class="pi pi-search-plus" /></span><strong>{{ churn?.churned || 0 }}</strong></div>
    <div class="rp-kpi drillable" @click="drill('never', 'Khách chưa từng mua')"><span>Chưa từng mua <i class="pi pi-search-plus" /></span><strong>{{ churn?.neverOrdered || 0 }}</strong></div>
  </div>

  <!-- Drill-down: danh sách khách trong nhóm -->
  <Dialog v-model:visible="dOpen" modal :header="dTitle" :style="{ width: '820px' }" :dismissableMask="true">
    <div v-if="dLoading" class="drill-load"><i class="pi pi-spin pi-spinner" /> Đang tải...</div>
    <template v-else>
      <DataTable :value="dRows" size="small" stripedRows paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
        <Column field="name" header="Khách" />
        <Column field="email" header="Email" />
        <Column header="Đơn cuối"><template #body="{ data }">{{ data.lastOrder ? formatDate(data.lastOrder) : '— chưa mua' }}</template></Column>
        <Column header="Số ngày"><template #body="{ data }">{{ data.lastOrder ? data.daysSince + ' ngày' : '—' }}</template></Column>
        <Column header="Đã chi"><template #body="{ data }">{{ formatCurrency(data.totalSpent) }}</template></Column>
        <template #empty><div class="rp-empty">Không có khách nào trong nhóm này.</div></template>
      </DataTable>
      <div class="drill-sum"><span>{{ dRows.length }} khách</span><strong>Tổng đã chi: {{ formatCurrency(dSpent()) }}</strong></div>
    </template>
  </Dialog>

  <div class="rp-grid2">
    <div class="rp-box"><h3><i class="pi pi-users" />Phân bố vòng đời khách</h3>
      <div class="rp-chart"><Chart type="doughnut" :data="churnChart()" :options="pieOpts" /></div>
    </div>
    <div class="rp-box"><h3><i class="pi pi-star" />Phân khúc RFM</h3>
      <div class="rp-chart"><Chart type="doughnut" :data="rfmChart()" :options="pieOpts" /></div>
    </div>
  </div>

  <div class="rp-grid2">
    <!-- B3 RFM table -->
    <div class="rp-box"><h3><i class="pi pi-chart-pie" />Chi tiết phân khúc</h3>
      <DataTable :value="rfm?.segments" size="small" stripedRows>
        <Column field="segment" header="Phân khúc" />
        <Column header="Số khách"><template #body="{ data }">{{ data.customers }}</template></Column>
        <Column header="Doanh thu"><template #body="{ data }">{{ formatCurrency(data.revenue) }}</template></Column>
      </DataTable>
    </div>
    <!-- B2 Market basket -->
    <div class="rp-box">
      <h3><i class="pi pi-sitemap" />Sản phẩm mua kèm (cross-sell)</h3>
      <ReportRangeBar @change="loadBasket" />
      <DataTable :value="basket" size="small" stripedRows>
        <Column header="Cặp sản phẩm">
          <template #body="{ data }"><span class="pair">{{ data.productA }}</span> <i class="pi pi-plus tiny" /> <span class="pair">{{ data.productB }}</span></template>
        </Column>
        <Column header="Số lần"><template #body="{ data }">{{ data.count }}</template></Column>
        <template #empty><div class="rp-empty">Chưa đủ dữ liệu để phân tích mua kèm.</div></template>
      </DataTable>
    </div>
  </div>

  <!-- Cohort — giữ chân khách theo tháng mua đầu -->
  <div class="rp-box">
    <h3><i class="pi pi-history" />Giữ chân khách theo tháng (Cohort)</h3>
    <div v-if="cohort?.rows.length" class="cohort-wrap">
      <table class="cohort">
        <thead>
          <tr>
            <th class="c-head">Nhóm khách</th><th>Số khách</th>
            <th v-for="(lb, i) in cohort.offsetLabels" :key="i">{{ lb }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in cohort.rows" :key="row.cohort">
            <td class="c-head">{{ row.cohort }}</td>
            <td class="c-size">{{ row.size }}</td>
            <td v-for="(pct, i) in row.retainedPct" :key="i" class="c-cell" :style="cellStyle(pct, row.retained[i])">
              <template v-if="i === 0 || row.retained[i] > 0">{{ pct }}%</template>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
    <div v-else class="rp-empty">Chưa đủ dữ liệu để tính cohort.</div>
    <p class="cohort-note">% khách của mỗi nhóm còn quay lại mua ở các tháng kế tiếp. Tháng 0 = tháng mua đầu (100%). Đậm = giữ chân tốt.</p>
  </div>

  <!-- B1 churned list -->
  <div class="rp-box"><h3><i class="pi pi-exclamation-triangle" />Khách đã rời bỏ (cần remarketing)</h3>
    <DataTable :value="churn?.topChurned" size="small" stripedRows paginator :rows="10">
      <Column field="name" header="Khách" />
      <Column field="email" header="Email" />
      <Column header="Đơn cuối"><template #body="{ data }">{{ data.lastOrder ? formatDate(data.lastOrder) : '—' }}</template></Column>
      <Column header="Số ngày"><template #body="{ data }">{{ data.daysSince }} ngày</template></Column>
      <Column header="Đã chi"><template #body="{ data }">{{ formatCurrency(data.totalSpent) }}</template></Column>
      <template #empty><div class="rp-empty">Không có khách rời bỏ — tốt!</div></template>
    </DataTable>
  </div>
</template>

<style scoped>
.pair { font-size: 13px; } .tiny { font-size: 10px; color: var(--brand); }

/* Cohort heatmap */
.cohort-wrap { overflow-x: auto; }
.cohort { border-collapse: separate; border-spacing: 3px; width: 100%; font-size: 12.5px; }
.cohort th { font-weight: 600; color: var(--text-muted); font-size: 11px; padding: 4px 6px; text-align: center; white-space: nowrap; }
.cohort th.c-head { text-align: left; }
.cohort td { text-align: center; padding: 7px 6px; border-radius: var(--radius-sm); min-width: 52px; }
.cohort td.c-head { text-align: left; font-weight: 600; color: var(--text); background: var(--surface-2); white-space: nowrap; }
.cohort td.c-size { font-weight: 700; color: var(--text-2); background: var(--surface-2); }
.cohort td.c-cell { font-weight: 600; }
.cohort-note { margin: var(--sp-3) 0 0; font-size: 12px; color: var(--text-muted); }
</style>
