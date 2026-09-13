<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import Chart from 'primevue/chart'
import ProgressSpinner from 'primevue/progressspinner'
import { dashboardApi, reportApi } from '@/services'
import type { DashboardStats, InventoryReport, PaymentMethodRevenue } from '@/types'
import { formatCurrency, orderStatusLabel, paymentMethodLabel, pmStyle } from '@/composables/format'

const stats = ref<DashboardStats | null>(null)
const loading = ref(true)

// Dữ liệu drilldown (nạp cùng lúc để mở ra là có ngay)
const byPayment = ref<PaymentMethodRevenue[]>([])
const inventory = ref<InventoryReport | null>(null)
const openDrill = ref<'revenue' | 'orders' | 'products' | 'customers' | null>(null)

const kpis = computed(() => [
  { key: 'revenue' as const, icon: 'pi-dollar', label: 'Doanh thu', value: formatCurrency(stats.value?.totalRevenue || 0) },
  { key: 'orders' as const, icon: 'pi-shopping-bag', label: 'Đơn hàng', value: String(stats.value?.totalOrders || 0) },
  { key: 'products' as const, icon: 'pi-mobile', label: 'Máy đang bán', value: String(stats.value?.totalProducts || 0) },
  { key: 'customers' as const, icon: 'pi-users', label: 'Khách hàng', value: String(stats.value?.totalCustomers || 0) }
])

const completedOrders = computed(() => stats.value?.ordersByStatus?.['Completed'] || 0)
const aov = computed(() => completedOrders.value ? Math.round((stats.value?.totalRevenue || 0) / completedOrders.value) : 0)

function toggleDrill(key: typeof openDrill.value) { openDrill.value = openDrill.value === key ? null : key }
function pct(part: number, whole: number) { return whole ? Math.round((part / whole) * 100) : 0 }

// Bảng màu xanh công nghệ dùng chung cho biểu đồ.
const revenueChart = computed(() => stats.value ? {
  labels: stats.value.revenueByDay.map((d) => d.date),
  datasets: [{ label: 'Doanh thu', data: stats.value.revenueByDay.map((d) => d.revenue), fill: true, borderColor: '#1e6fff', backgroundColor: 'rgba(30,111,255,.12)', tension: 0.4 }]
} : {})
const statusChart = computed(() => stats.value ? {
  labels: Object.keys(stats.value.ordersByStatus).map((s) => orderStatusLabel[s] || s),
  datasets: [{ data: Object.values(stats.value.ordersByStatus), backgroundColor: ['#f59e0b', '#1e6fff', '#7c5cfc', '#16a34a', '#e8453c'] }]
} : {})
const chartOptions = { plugins: { legend: { display: false } }, maintainAspectRatio: false }
const pieOptions = { maintainAspectRatio: false }

const statusSeverityColor: Record<string, string> = {
  Pending: '#f59e0b', Confirmed: '#1e6fff', Shipping: '#7c5cfc', Completed: '#16a34a', Cancelled: '#e8453c'
}

onMounted(async () => {
  try {
    const wide = { from: '2020-01-01', to: '2030-12-31' }
    const [s, pay, inv] = await Promise.all([
      dashboardApi.stats(),
      reportApi.byPayment(wide).catch(() => []),
      reportApi.inventory().catch(() => null)
    ])
    stats.value = s; byPayment.value = pay; inventory.value = inv
  } finally { loading.value = false }
})
</script>

<template>
  <h1>Tổng quan</h1>
  <div v-if="loading" class="center"><ProgressSpinner /></div>
  <template v-else-if="stats">
    <!-- KPI có mũi tên drilldown -->
    <div class="kpis">
      <button v-for="k in kpis" :key="k.key" class="kpi" :class="{ active: openDrill === k.key }" @click="toggleDrill(k.key)">
        <i class="pi kpi-ic" :class="k.icon" />
        <div class="kpi-txt"><span>{{ k.label }}</span><strong>{{ k.value }}</strong></div>
        <i class="pi pi-chevron-down chev" :class="{ open: openDrill === k.key }" />
      </button>
    </div>

    <!-- Panel drilldown -->
    <transition name="drill">
      <div v-if="openDrill" class="drill">
        <!-- Doanh thu -->
        <template v-if="openDrill === 'revenue'">
          <h4><i class="pi pi-dollar" /> Chi tiết doanh thu</h4>
          <div class="drill-stats">
            <div><span>Tổng doanh thu</span><b class="price">{{ formatCurrency(stats.totalRevenue) }}</b></div>
            <div><span>Đơn hoàn tất</span><b>{{ completedOrders }}</b></div>
            <div><span>Giá trị TB/đơn</span><b>{{ formatCurrency(aov) }}</b></div>
          </div>
          <div class="drill-title">Theo phương thức thanh toán</div>
          <div v-for="p in byPayment" :key="p.method" class="drow">
            <span class="dlabel">
              <span class="pm-ic" :style="{ background: pmStyle(p.method).color + '1f', color: pmStyle(p.method).color }">
                <i class="pi" :class="pmStyle(p.method).icon" />
              </span>
              <span class="pm-name">{{ paymentMethodLabel[p.method] || p.method }} <em>({{ p.orders }} đơn)</em></span>
            </span>
            <span class="dbar"><span class="dfill" :style="{ width: pct(p.revenue, stats.totalRevenue) + '%', background: pmStyle(p.method).color }" /></span>
            <span class="dval">{{ formatCurrency(p.revenue) }}</span>
          </div>
          <p v-if="!byPayment.length" class="text-muted">Chưa có giao dịch hoàn tất.</p>
        </template>

        <!-- Đơn hàng -->
        <template v-else-if="openDrill === 'orders'">
          <h4><i class="pi pi-shopping-bag" /> Đơn hàng theo trạng thái (tổng {{ stats.totalOrders }})</h4>
          <div v-for="(count, st) in stats.ordersByStatus" :key="st" class="drow">
            <span class="dlabel"><i class="dot" :style="{ background: statusSeverityColor[st] }" /> {{ orderStatusLabel[st] || st }}</span>
            <span class="dbar"><span class="dfill" :style="{ width: pct(count, stats.totalOrders) + '%', background: statusSeverityColor[st] }" /></span>
            <span class="dval">{{ count }} <em>({{ pct(count, stats.totalOrders) }}%)</em></span>
          </div>
        </template>

        <!-- Máy / kho -->
        <template v-else-if="openDrill === 'products'">
          <h4><i class="pi pi-mobile" /> Tình trạng máy &amp; kho</h4>
          <div class="drill-stats">
            <div><span>Tổng máy đang bán</span><b>{{ stats.totalProducts }}</b></div>
            <div><span>Tổng phiên bản (màu/dung lượng)</span><b>{{ inventory?.totalVariants || 0 }}</b></div>
            <div><span>Đơn vị tồn kho</span><b>{{ inventory?.totalStockUnits || 0 }}</b></div>
            <div><span>Giá trị tồn kho</span><b class="price">{{ formatCurrency(inventory?.stockValue || 0) }}</b></div>
            <div class="warn"><span>Sắp hết (&lt;10)</span><b>{{ inventory?.lowStockCount || 0 }}</b></div>
            <div class="danger"><span>Hết hàng</span><b>{{ inventory?.outOfStockCount || 0 }}</b></div>
          </div>
        </template>

        <!-- Khách hàng -->
        <template v-else-if="openDrill === 'customers'">
          <h4><i class="pi pi-users" /> Khách hàng</h4>
          <div class="drill-stats">
            <div><span>Tổng khách hàng</span><b>{{ stats.totalCustomers }}</b></div>
            <div><span>Đơn hoàn tất</span><b>{{ completedOrders }}</b></div>
            <div><span>Giá trị TB/đơn</span><b class="price">{{ formatCurrency(aov) }}</b></div>
          </div>
          <p class="text-muted small">Xem chi tiết khách mới theo kỳ ở trang Báo cáo.</p>
        </template>
      </div>
    </transition>

    <div class="charts">
      <div class="box">
        <h3>Doanh thu gần đây</h3>
        <div class="chart-wrap"><Chart type="line" :data="revenueChart" :options="chartOptions" /></div>
      </div>
      <div class="box">
        <h3>Đơn theo trạng thái</h3>
        <div class="chart-wrap"><Chart type="doughnut" :data="statusChart" :options="pieOptions" /></div>
      </div>
    </div>

    <div class="box">
      <h3>Top máy bán chạy</h3>
      <table class="top-table">
        <thead><tr><th>Máy</th><th>Đã bán</th><th>Doanh thu</th></tr></thead>
        <tbody>
          <tr v-for="p in stats.topProducts" :key="p.productId">
            <td>{{ p.productName }}</td>
            <td>{{ p.quantitySold }}</td>
            <td>{{ formatCurrency(p.revenue) }}</td>
          </tr>
          <tr v-if="!stats.topProducts.length"><td colspan="3" class="text-muted">Chưa có dữ liệu bán hàng.</td></tr>
        </tbody>
      </table>
    </div>
  </template>
</template>

<style scoped>
.center { display: flex; justify-content: center; padding: var(--sp-8); }
.kpis { display: grid; grid-template-columns: repeat(4, 1fr); gap: var(--sp-4); margin: var(--sp-5) 0 var(--sp-3); }
.kpi {
  background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg);
  box-shadow: var(--shadow-sm); padding: var(--sp-4) var(--sp-5); display: flex; align-items: center; gap: var(--sp-4);
  cursor: pointer; font-family: inherit; text-align: left; width: 100%;
  transition: box-shadow var(--ease), transform var(--ease), border-color var(--ease);
}
.kpi:hover { box-shadow: var(--shadow); transform: translateY(-2px); }
.kpi.active { border-color: var(--brand); box-shadow: var(--shadow-hover); }
.kpi-ic { font-size: 1.6rem; color: var(--brand); background: var(--brand-50); padding: var(--sp-3); border-radius: var(--radius); }
.kpi-txt { flex: 1; min-width: 0; }
.kpi-txt span { color: var(--text-muted); font-size: 0.85rem; display: block; }
.kpi-txt strong { font-size: 1.25rem; }
.chev { color: var(--text-muted); font-size: 0.85rem; transition: transform var(--ease); }
.chev.open { transform: rotate(180deg); color: var(--brand); }

/* Panel drilldown */
.drill { background: var(--surface); border: 1px solid var(--brand-100); border-radius: var(--radius-lg); padding: var(--sp-5); margin-bottom: var(--sp-4); box-shadow: var(--shadow-sm); }
.drill h4 { display: flex; align-items: center; gap: 8px; margin: 0 0 var(--sp-4); font-size: 1rem; }
.drill h4 .pi { color: var(--brand); }
.drill-stats { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: var(--sp-3); margin-bottom: var(--sp-3); }
.drill-stats > div { background: var(--surface-2); border-radius: var(--radius); padding: var(--sp-3); }
.drill-stats span { font-size: 12px; color: var(--text-muted); display: block; }
.drill-stats b { font-size: 1.05rem; }
.drill-stats .warn { background: #fff7ed; } .drill-stats .warn b { color: #d97706; }
.drill-stats .danger { background: #fef2f2; } .drill-stats .danger b { color: var(--danger); }
.drill-title { font-size: 13px; font-weight: 600; margin: var(--sp-3) 0 var(--sp-2); color: var(--text-2); }
.drow { display: flex; align-items: center; gap: var(--sp-3); margin: 6px 0; font-size: 13px; }
.dlabel { width: 210px; flex-shrink: 0; display: inline-flex; align-items: center; gap: 8px; }
.dlabel em { color: var(--text-muted); font-style: normal; font-size: 11px; }
.pm-ic { width: 26px; height: 26px; border-radius: 8px; display: grid; place-items: center; flex-shrink: 0; }
.pm-ic .pi { font-size: 13px; }
.pm-name { min-width: 0; line-height: 1.3; }
.dot { width: 9px; height: 9px; border-radius: 50%; flex-shrink: 0; }
.dbar { flex: 1; height: 9px; background: var(--border); border-radius: var(--radius-pill); overflow: hidden; }
.dfill { display: block; height: 100%; background: var(--brand); border-radius: var(--radius-pill); }
.dval { width: 130px; text-align: right; flex-shrink: 0; color: var(--text-2); }
.dval em { color: var(--text-muted); font-style: normal; font-size: 11px; }
.small { font-size: 12px; }

.drill-enter-active, .drill-leave-active { transition: all 0.22s ease; overflow: hidden; }
.drill-enter-from, .drill-leave-to { opacity: 0; transform: translateY(-8px); }

.charts { display: grid; grid-template-columns: 2fr 1fr; gap: var(--sp-4); margin-bottom: var(--sp-4); }
.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); padding: var(--sp-5); }
.box h3 { margin-bottom: var(--sp-4); }
.chart-wrap { height: 280px; }
.top-table { width: 100%; border-collapse: collapse; }
.top-table th, .top-table td { text-align: left; padding: var(--sp-3) var(--sp-2); border-bottom: 1px solid var(--border); }
.top-table th { font-size: 12px; color: var(--text-2); font-weight: 600; }
.top-table tbody tr { transition: background var(--ease); }
.top-table tbody tr:hover { background: var(--surface-2); }
@media (max-width: 900px) { .kpis, .charts { grid-template-columns: 1fr 1fr; } .dlabel { width: 130px; } .dval { width: 90px; } }
@media (max-width: 640px) { .kpis { grid-template-columns: 1fr; } }
</style>
