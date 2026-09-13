<script setup lang="ts">
import { ref, onMounted } from 'vue'
import Chart from 'primevue/chart'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Dialog from 'primevue/dialog'
import Tag from 'primevue/tag'
import ReportRangeBar from '@/components/ReportRangeBar.vue'
import OrderCodeLink from '@/components/OrderCodeLink.vue'
import { advReportApi } from '@/services'
import type { PromotionReport, Reconciliation, ProfitReport, ReconOrder, ProfitOrder, FlashSaleReport } from '@/types'
import { formatCurrency, formatDate, paymentMethodShort, paymentStatusLabel, orderStatusLabel, orderStatusSeverity } from '@/composables/format'

const promo = ref<PromotionReport | null>(null)
const recon = ref<Reconciliation | null>(null)
const profit = ref<ProfitReport | null>(null)
const flash = ref<FlashSaleReport | null>(null)
const range = ref<{ from: string; to: string }>({ from: '', to: '' })

// Flash sale tính trên toàn bộ chương trình (không theo khoảng lọc) → nạp 1 lần.
onMounted(async () => { flash.value = await advReportApi.flashSalePerf() })

async function load(p: { from: string; to: string }) {
  range.value = p
  const [pr, rc, pf] = await Promise.all([
    advReportApi.promotion(p), advReportApi.reconciliation(p), advReportApi.profit(p)
  ])
  promo.value = pr; recon.value = rc; profit.value = pf
}

// ----- Drill-down: đơn nào tạo nên con số -----
const drillOpen = ref(false)
const drillTitle = ref('')
const drillLoading = ref(false)
const drillOrders = ref<ReconOrder[]>([])
const drillTotal = () => drillOrders.value.reduce((s, o) => s + o.amount, 0)

async function drill(params: { bucket?: string; method?: string; payStatus?: string }, title: string) {
  drillTitle.value = title
  drillOpen.value = true
  drillLoading.value = true
  drillOrders.value = []
  try {
    drillOrders.value = await advReportApi.reconciliationOrders({ ...range.value, ...params })
  } finally {
    drillLoading.value = false
  }
}

const profitChart = () => ({
  labels: profit.value?.byCategory.map((c) => c.category) ?? [],
  datasets: [
    { label: 'Doanh thu', data: profit.value?.byCategory.map((c) => c.revenue) ?? [], backgroundColor: '#94a3b8' },
    { label: 'Lợi nhuận gộp', data: profit.value?.byCategory.map((c) => c.profit) ?? [], backgroundColor: '#1e6fff' }
  ]
})
// Bấm 1 cột trên biểu đồ → drill xuống các đơn của danh mục đó.
const barOpts = {
  maintainAspectRatio: false,
  plugins: { legend: { position: 'top' } },
  onClick: (_e: unknown, els: { index: number }[]) => {
    const cat = els?.length ? profit.value?.byCategory[els[0].index]?.category : undefined
    if (cat) drillProfit(cat)
  }
}

// ----- Drill-down lợi nhuận (Doanh thu / Giá vốn / LN gộp ...) -----
const pOpen = ref(false)
const pTitle = ref('')
const pLoading = ref(false)
const pOrders = ref<ProfitOrder[]>([])
type PKey = 'revenue' | 'cogs' | 'profit' | 'discount' | 'shipping'
const pTotal = (k: PKey) => pOrders.value.reduce((s, o) => s + o[k], 0)

async function drillProfit(category?: string) {
  pTitle.value = category ? `Đơn theo danh mục · ${category}` : 'Doanh thu & lợi nhuận theo đơn'
  pOpen.value = true; pLoading.value = true; pOrders.value = []
  try {
    pOrders.value = await advReportApi.profitOrders({ ...range.value, category })
  } finally {
    pLoading.value = false
  }
}
</script>

<template>
  <ReportRangeBar @change="load" />

  <!-- A3 Lợi nhuận gộp — bấm 1 ô để xem các đơn tạo nên con số -->
  <div class="rp-kpis">
    <div class="rp-kpi drillable" @click="drillProfit()"><span>Doanh thu <i class="pi pi-search-plus" /></span><strong>{{ formatCurrency(profit?.revenue || 0) }}</strong></div>
    <div class="rp-kpi drillable" @click="drillProfit()"><span>Giá vốn (COGS) <i class="pi pi-search-plus" /></span><strong>{{ formatCurrency(profit?.cogs || 0) }}</strong></div>
    <div class="rp-kpi good drillable" @click="drillProfit()"><span>Lợi nhuận gộp <i class="pi pi-search-plus" /></span><strong>{{ formatCurrency(profit?.grossProfit || 0) }}</strong></div>
    <div class="rp-kpi drillable" @click="drillProfit()"><span>Biên LN gộp <i class="pi pi-search-plus" /></span><strong>{{ profit?.grossMarginPct || 0 }}%</strong></div>
    <div class="rp-kpi drillable" @click="drillProfit()"><span>Đã giảm giá <i class="pi pi-search-plus" /></span><strong>{{ formatCurrency(profit?.discounts || 0) }}</strong></div>
    <div class="rp-kpi drillable" @click="drillProfit()"><span>Phí ship thu <i class="pi pi-search-plus" /></span><strong>{{ formatCurrency(profit?.shippingCharged || 0) }}</strong></div>
  </div>

  <!-- Hiệu quả Flash Sale -->
  <div class="rp-box">
    <h3><i class="pi pi-bolt" />Hiệu quả Flash Sale</h3>
    <div class="fs-kpis">
      <div class="fs-kpi"><span>Chương trình</span><strong>{{ flash?.programs || 0 }}</strong></div>
      <div class="fs-kpi"><span>Đã bán trong sale</span><strong>{{ flash?.unitsSold || 0 }}</strong></div>
      <div class="fs-kpi"><span>Doanh thu sale</span><strong>{{ formatCurrency(flash?.revenue || 0) }}</strong></div>
      <div class="fs-kpi"><span>Đã giảm cho khách</span><strong class="minus">{{ formatCurrency(flash?.discountGiven || 0) }}</strong></div>
    </div>
    <DataTable :value="flash?.items" size="small" stripedRows paginator :rows="10">
      <Column header="Máy">
        <template #body="{ data }">{{ data.productName }} <Tag v-if="data.running" value="Đang chạy" severity="success" style="margin-left:6px" /></template>
      </Column>
      <Column header="Giá gốc"><template #body="{ data }"><span class="old">{{ formatCurrency(data.originalPrice) }}</span></template></Column>
      <Column header="Giá flash"><template #body="{ data }"><span class="price">{{ formatCurrency(data.flashPrice) }}</span></template></Column>
      <Column header="Đã bán"><template #body="{ data }">{{ data.unitsSold }}</template></Column>
      <Column header="Doanh thu"><template #body="{ data }">{{ formatCurrency(data.revenue) }}</template></Column>
      <Column header="Đã giảm"><template #body="{ data }"><span class="minus">-{{ formatCurrency(data.discount) }}</span></template></Column>
      <template #empty><div class="rp-empty">Chưa có đơn nào phát sinh trong khung giờ Flash Sale.</div></template>
    </DataTable>
  </div>

  <div class="rp-box">
    <h3><i class="pi pi-chart-bar" />Lợi nhuận gộp theo danh mục</h3>
    <div class="rp-chart"><Chart type="bar" :data="profitChart()" :options="barOpts" /></div>
    <p class="drill-hint"><i class="pi pi-info-circle" /> Bấm 1 ô tổng ở trên hoặc 1 cột trên biểu đồ để xem các đơn tạo nên con số.</p>
  </div>

  <!-- Drill-down lợi nhuận theo đơn -->
  <Dialog v-model:visible="pOpen" modal :header="pTitle" :style="{ width: '920px' }" :dismissableMask="true">
    <div v-if="pLoading" class="drill-load"><i class="pi pi-spin pi-spinner" /> Đang tải...</div>
    <template v-else>
      <DataTable :value="pOrders" size="small" stripedRows paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
        <Column header="Mã đơn"><template #body="{ data }"><OrderCodeLink :id="data.id" :code="data.orderCode" /></template></Column>
        <Column header="Ngày"><template #body="{ data }">{{ formatDate(data.createdAt) }}</template></Column>
        <Column header="Doanh thu"><template #body="{ data }">{{ formatCurrency(data.revenue) }}</template></Column>
        <Column header="Giá vốn"><template #body="{ data }">{{ formatCurrency(data.cogs) }}</template></Column>
        <Column header="LN gộp"><template #body="{ data }"><b class="good-t">{{ formatCurrency(data.profit) }}</b></template></Column>
        <Column header="Giảm giá"><template #body="{ data }">{{ formatCurrency(data.discount) }}</template></Column>
        <Column header="Phí ship"><template #body="{ data }">{{ formatCurrency(data.shipping) }}</template></Column>
        <template #empty><div class="rp-empty">Không có đơn hoàn tất trong kỳ.</div></template>
      </DataTable>
      <div class="drill-sum">
        <span>{{ pOrders.length }} đơn</span>
        <span class="pt">DT: <b>{{ formatCurrency(pTotal('revenue')) }}</b></span>
        <span class="pt">Vốn: <b>{{ formatCurrency(pTotal('cogs')) }}</b></span>
        <span class="pt">LN: <b class="good-t">{{ formatCurrency(pTotal('profit')) }}</b></span>
      </div>
    </template>
  </Dialog>

  <div class="rp-grid2">
    <!-- A1 Khuyến mãi -->
    <div class="rp-box">
      <h3><i class="pi pi-ticket" />Hiệu quả khuyến mãi</h3>
      <div class="mini-kpis">
        <div><span>Tổng giảm giá</span><b>{{ formatCurrency(promo?.totalDiscount || 0) }}</b></div>
        <div><span>Đơn dùng mã</span><b>{{ promo?.ordersWithCoupon || 0 }}</b></div>
        <div><span>AOV có mã</span><b>{{ formatCurrency(promo?.aovWithCoupon || 0) }}</b></div>
        <div><span>AOV không mã</span><b>{{ formatCurrency(promo?.aovWithoutCoupon || 0) }}</b></div>
      </div>
      <DataTable :value="promo?.coupons" size="small" stripedRows class="mt">
        <Column field="code" header="Mã" />
        <Column header="Lượt"><template #body="{ data }">{{ data.timesUsed }}</template></Column>
        <Column header="Đã giảm"><template #body="{ data }">{{ formatCurrency(data.totalDiscount) }}</template></Column>
        <Column header="DT tạo ra"><template #body="{ data }">{{ formatCurrency(data.revenueGenerated) }}</template></Column>
        <template #empty><div class="rp-empty">Chưa có đơn dùng mã trong kỳ.</div></template>
      </DataTable>
    </div>

    <!-- A2 Đối soát COD -->
    <div class="rp-box">
      <h3><i class="pi pi-wallet" />Đối soát COD &amp; Thanh toán</h3>
      <div class="mini-kpis">
        <div class="drillable" @click="drill({ bucket: 'codPending' }, 'COD đang trên đường')">
          <span>COD đang trên đường <i class="pi pi-search-plus" /></span>
          <b class="warn">{{ formatCurrency(recon?.codPending || 0) }}</b><small>{{ recon?.codPendingOrders || 0 }} đơn</small>
        </div>
        <div class="drillable" @click="drill({ bucket: 'codCollected' }, 'COD đã thu')">
          <span>COD đã thu <i class="pi pi-search-plus" /></span>
          <b class="ok">{{ formatCurrency(recon?.codCollected || 0) }}</b>
        </div>
        <div class="drillable" @click="drill({ bucket: 'onlinePaid' }, 'Chuyển khoản đã nhận')">
          <span>Chuyển khoản đã nhận <i class="pi pi-search-plus" /></span>
          <b class="ok">{{ formatCurrency(recon?.onlinePaid || 0) }}</b>
        </div>
        <div class="drillable" @click="drill({ bucket: 'onlinePending' }, 'Chuyển khoản chờ')">
          <span>Chuyển khoản chờ <i class="pi pi-search-plus" /></span>
          <b>{{ formatCurrency(recon?.onlinePending || 0) }}</b>
        </div>
      </div>
      <DataTable :value="recon?.rows" size="small" stripedRows rowHover class="mt drill-table"
        @row-click="(e) => drill({ method: e.data.method, payStatus: e.data.status }, `${paymentMethodShort[e.data.method] || e.data.method} · ${paymentStatusLabel[e.data.status] || e.data.status}`)">
        <Column header="Phương thức"><template #body="{ data }">{{ paymentMethodShort[data.method] || data.method }}</template></Column>
        <Column header="Trạng thái"><template #body="{ data }">{{ paymentStatusLabel[data.status] || data.status }}</template></Column>
        <Column header="Đơn"><template #body="{ data }">{{ data.orders }}</template></Column>
        <Column header="Số tiền"><template #body="{ data }">{{ formatCurrency(data.amount) }}</template></Column>
        <template #empty><div class="rp-empty">Không có giao dịch trong kỳ.</div></template>
      </DataTable>
      <p class="drill-hint"><i class="pi pi-info-circle" /> Bấm vào ô tổng hoặc 1 dòng để xem các đơn tạo nên con số.</p>
    </div>
  </div>

  <!-- Drill-down: danh sách đơn -->
  <Dialog v-model:visible="drillOpen" modal :header="drillTitle" :style="{ width: '780px' }" :dismissableMask="true">
    <div v-if="drillLoading" class="drill-load"><i class="pi pi-spin pi-spinner" /> Đang tải...</div>
    <template v-else>
      <DataTable :value="drillOrders" size="small" stripedRows paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
        <Column header="Mã đơn"><template #body="{ data }"><OrderCodeLink :id="data.id" :code="data.orderCode" /></template></Column>
        <Column header="Ngày đặt"><template #body="{ data }">{{ formatDate(data.createdAt) }}</template></Column>
        <Column header="Trạng thái đơn">
          <template #body="{ data }"><Tag :value="orderStatusLabel[data.orderStatus] || data.orderStatus" :severity="orderStatusSeverity[data.orderStatus]" /></template>
        </Column>
        <Column header="Thanh toán">
          <template #body="{ data }">{{ paymentMethodShort[data.method] || data.method }} · {{ paymentStatusLabel[data.payStatus] || data.payStatus }}</template>
        </Column>
        <Column header="Số tiền"><template #body="{ data }">{{ formatCurrency(data.amount) }}</template></Column>
        <template #empty><div class="rp-empty">Không có đơn nào trong nhóm này.</div></template>
      </DataTable>
      <div class="drill-sum"><span>{{ drillOrders.length }} đơn</span><strong>Tổng: {{ formatCurrency(drillTotal()) }}</strong></div>
    </template>
  </Dialog>
</template>

<style scoped>
/* Flash sale KPIs */
.fs-kpis { display: grid; grid-template-columns: repeat(4, 1fr); gap: var(--sp-2); margin-bottom: var(--sp-3); }
.fs-kpi { background: var(--surface-2); border: 1px solid var(--border); border-radius: var(--radius); padding: var(--sp-2) var(--sp-3); }
.fs-kpi span { font-size: 11px; color: var(--text-muted); display: block; }
.fs-kpi strong { font-size: 1.05rem; }
.minus { color: var(--brand); }
.old { color: var(--text-muted); text-decoration: line-through; font-size: 12px; }
@media (max-width: 640px) { .fs-kpis { grid-template-columns: 1fr 1fr; } }

.mini-kpis { display: grid; grid-template-columns: 1fr 1fr; gap: var(--sp-2); margin-bottom: var(--sp-3); }
.mini-kpis > div { background: var(--surface-2); border-radius: var(--radius-sm); padding: var(--sp-2) var(--sp-3); }
.mini-kpis span { font-size: 11px; color: var(--text-muted); display: block; }
.mini-kpis b { font-size: 1rem; } .mini-kpis small { font-size: 11px; color: var(--text-muted); }
.mini-kpis .ok { color: var(--success); } .mini-kpis .warn { color: #d97706; }
.mt { margin-top: var(--sp-2); }

/* Ô tổng bấm được để drill-down */
.drillable { cursor: pointer; border: 1px solid transparent; transition: border-color var(--ease), background var(--ease); }
.drillable:hover { border-color: var(--brand); background: var(--brand-50); }
.drillable span .pi { font-size: 10px; opacity: 0; transition: opacity var(--ease); }
.drillable:hover span .pi { opacity: 0.7; }
.drill-table :deep(.p-datatable-tbody > tr) { cursor: pointer; }
.drill-hint { display: flex; align-items: center; gap: 6px; margin: var(--sp-2) 0 0; font-size: 11.5px; color: var(--text-muted); }
.drill-hint .pi { font-size: 11px; }
.drill-load { display: flex; align-items: center; gap: 8px; padding: var(--sp-5); color: var(--text-muted); justify-content: center; }
.drill-sum { display: flex; justify-content: space-between; align-items: center; gap: var(--sp-4); flex-wrap: wrap; margin-top: var(--sp-3); padding-top: var(--sp-3); border-top: 1px solid var(--border); font-size: 14px; }
.drill-sum strong { color: var(--price); }
.drill-sum .pt { color: var(--text-2); font-size: 13px; } .drill-sum .pt b { color: var(--text); }
.rp-kpi.drillable { cursor: pointer; }
.rp-kpi.drillable span .pi { font-size: 10px; opacity: 0; transition: opacity var(--ease); }
.rp-kpi.drillable:hover span .pi { opacity: 0.7; }
.good-t { color: var(--success); }
</style>
