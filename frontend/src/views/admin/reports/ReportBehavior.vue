<script setup lang="ts">
import { ref, onMounted } from 'vue'
import Chart from 'primevue/chart'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'
import ReportRangeBar from '@/components/ReportRangeBar.vue'
import ProductNameLink from '@/components/ProductNameLink.vue'
import { advReportApi } from '@/services'
import type { ViewToSale, SearchReport, FunnelReport, PeakTimeReport, ReviewReport } from '@/types'

const v2s = ref<ViewToSale[]>([])
const search = ref<SearchReport | null>(null)
const funnel = ref<FunnelReport | null>(null)
const peak = ref<PeakTimeReport | null>(null)
const reviews = ref<ReviewReport | null>(null)
const weekdays = ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'CN']

onMounted(async () => { v2s.value = await advReportApi.viewToSale() })
async function loadRange(p: { from: string; to: string }) {
  const [s, f, pk, rv] = await Promise.all([
    advReportApi.search(p), advReportApi.funnel(p), advReportApi.peakTime(p), advReportApi.reviews(p)
  ])
  search.value = s; funnel.value = f; peak.value = pk; reviews.value = rv
}

// Biểu đồ khung giờ vàng
const hourChart = () => ({ labels: Array.from({ length: 24 }, (_, h) => h + 'h'), datasets: [{ label: 'Đơn', data: peak.value?.byHour ?? [], backgroundColor: '#1e6fff', borderRadius: 3 }] })
const weekdayChart = () => ({ labels: weekdays, datasets: [{ label: 'Đơn', data: peak.value?.byWeekday ?? [], backgroundColor: '#3b82f6', borderRadius: 3 }] })
const barOpts = { maintainAspectRatio: false, plugins: { legend: { display: false } } }
// Biểu đồ xu hướng sao
const trendChart = () => ({ labels: reviews.value?.trend.map((t) => t.label) ?? [], datasets: [{ label: 'Sao TB', data: reviews.value?.trend.map((t) => t.avg) ?? [], borderColor: '#f5a623', backgroundColor: 'rgba(245,166,35,.15)', fill: true, tension: 0.35 }] })
const trendOpts = { maintainAspectRatio: false, plugins: { legend: { display: false } }, scales: { y: { min: 0, max: 5 } } }
function distMax() { return Math.max(1, ...(reviews.value?.distribution ?? [1])) }
function starSeverity(r: number) { return r < 3 ? 'danger' : r < 4 ? 'warn' : 'success' }

function convSeverity(r: number) { return r < 1 ? 'danger' : r < 5 ? 'warn' : 'success' }
// Chiều rộng thanh phễu tương đối
function funnelWidth(v: number, max: number) { return max ? Math.max(6, Math.round(v / max * 100)) : 6 }
</script>

<template>
  <ReportRangeBar @change="loadRange" />

  <!-- Khung giờ vàng -->
  <div class="rp-box">
    <h3><i class="pi pi-clock" />Khung giờ vàng — bán chạy lúc nào?</h3>
    <p v-if="peak && peak.totalOrders" class="peak-hi">
      Cao điểm: <b>{{ peak.peakHour }}h</b> · <b>{{ weekdays[peak.peakWeekday] }}</b>
      <span class="text-muted"> — trên {{ peak.totalOrders }} đơn hoàn tất trong kỳ</span>
    </p>
    <div class="rp-grid2">
      <div><h4 class="sub">Đơn theo giờ trong ngày (giờ VN)</h4><div class="chart-sm"><Chart type="bar" :data="hourChart()" :options="barOpts" /></div></div>
      <div><h4 class="sub">Đơn theo thứ trong tuần</h4><div class="chart-sm"><Chart type="bar" :data="weekdayChart()" :options="barOpts" /></div></div>
    </div>
    <div v-if="!peak?.totalOrders" class="rp-empty">Chưa có đơn hoàn tất trong kỳ để phân tích khung giờ.</div>
  </div>

  <!-- Phân tích đánh giá -->
  <div class="rp-box">
    <h3><i class="pi pi-star-fill" />Phân tích đánh giá — {{ reviews?.totalReviews || 0 }} review · TB {{ reviews?.avgRating || 0 }}★</h3>
    <div class="dist" v-if="reviews?.totalReviews">
      <div v-for="(cnt, i) in reviews.distribution" :key="i" class="dist-row">
        <span class="dist-lbl">{{ 5 - i }}★</span>
        <div class="dist-bg"><div class="dist-bar" :style="{ width: (cnt / distMax() * 100) + '%' }" /></div>
        <span class="dist-cnt">{{ cnt }}</span>
      </div>
    </div>
    <div v-else class="rp-empty">Chưa có đánh giá nào trong kỳ.</div>
  </div>

  <div class="rp-grid2">
    <div class="rp-box"><h3><i class="pi pi-chart-line" />Xu hướng sao theo tháng</h3>
      <div class="chart-md"><Chart type="line" :data="trendChart()" :options="trendOpts" /></div>
    </div>
    <div class="rp-box"><h3><i class="pi pi-thumbs-down" />Sản phẩm bị chê nhiều nhất</h3>
      <DataTable :value="reviews?.worstProducts" size="small" stripedRows>
        <Column header="Máy"><template #body="{ data }"><ProductNameLink :id="data.productId" :name="data.productName" /></template></Column>
        <Column header="Sao TB"><template #body="{ data }"><Tag :value="data.avgRating + '★'" :severity="starSeverity(data.avgRating)" /></template></Column>
        <Column header="Số review"><template #body="{ data }">{{ data.reviewCount }}</template></Column>
        <template #empty><div class="rp-empty">Chưa có đánh giá nào.</div></template>
      </DataTable>
    </div>
  </div>

  <!-- D3 Funnel -->
  <div class="rp-box">
    <h3><i class="pi pi-filter" />Phễu chuyển đổi</h3>
    <div v-if="funnel" class="funnel">
      <div class="fn-row"><span class="fn-lbl">Lượt xem SP</span>
        <div class="fn-bar" :style="{ width: funnelWidth(funnel.productViews, funnel.productViews) + '%' }">{{ funnel.productViews }}</div></div>
      <div class="fn-row"><span class="fn-lbl">SP trong giỏ</span>
        <div class="fn-bar" :style="{ width: funnelWidth(funnel.cartItems, funnel.productViews) + '%' }">{{ funnel.cartItems }}</div></div>
      <div class="fn-row"><span class="fn-lbl">Đơn tạo</span>
        <div class="fn-bar" :style="{ width: funnelWidth(funnel.orders, funnel.productViews) + '%' }">{{ funnel.orders }}</div></div>
      <div class="fn-row"><span class="fn-lbl">Đơn hoàn tất</span>
        <div class="fn-bar done" :style="{ width: funnelWidth(funnel.completedOrders, funnel.productViews) + '%' }">{{ funnel.completedOrders }}</div></div>
    </div>
    <div class="fn-rates">
      <span>Xem → Đặt: <b>{{ funnel?.viewToOrderRate || 0 }}%</b></span>
      <span>Đặt → Hoàn tất: <b>{{ funnel?.orderCompletionRate || 0 }}%</b></span>
    </div>
  </div>

  <div class="rp-grid2">
    <!-- D2 Search hot -->
    <div class="rp-box"><h3><i class="pi pi-search" />Từ khóa tìm nhiều ({{ search?.totalSearches || 0 }} lượt)</h3>
      <DataTable :value="search?.topKeywords" size="small" stripedRows>
        <Column field="keyword" header="Từ khóa" />
        <Column header="Lượt"><template #body="{ data }">{{ data.count }}</template></Column>
        <Column header="KQ TB"><template #body="{ data }">{{ data.avgResults }}</template></Column>
        <template #empty><div class="rp-empty">Chưa có lượt tìm kiếm nào được ghi nhận.</div></template>
      </DataTable>
    </div>
    <!-- D2 No-result -->
    <div class="rp-box"><h3><i class="pi pi-ban" />Từ khóa KHÔNG ra kết quả (cơ hội nhập hàng)</h3>
      <DataTable :value="search?.noResultKeywords" size="small" stripedRows>
        <Column field="keyword" header="Từ khóa" />
        <Column header="Lượt"><template #body="{ data }">{{ data.count }}</template></Column>
        <template #empty><div class="rp-empty">Không có từ khóa nào bị rỗng kết quả.</div></template>
      </DataTable>
    </div>
  </div>

  <!-- D1 View-to-sale -->
  <div class="rp-box">
    <h3><i class="pi pi-eye" />View-to-Sale — SP hút view nhưng ít chuyển đổi</h3>
    <DataTable :value="v2s" size="small" stripedRows paginator :rows="10">
      <Column header="Máy"><template #body="{ data }"><ProductNameLink :id="data.productId" :name="data.productName" /></template></Column>
      <Column header="Lượt xem"><template #body="{ data }">{{ data.views }}</template></Column>
      <Column header="Đã bán"><template #body="{ data }">{{ data.sold }}</template></Column>
      <Column header="Tỉ lệ chuyển đổi">
        <template #body="{ data }"><Tag :value="data.conversionRate + '%'" :severity="convSeverity(data.conversionRate)" /></template>
      </Column>
      <template #empty><div class="rp-empty">Chưa có dữ liệu lượt xem.</div></template>
    </DataTable>
  </div>
</template>

<style scoped>
/* Khung giờ vàng */
.peak-hi { margin: 0 0 var(--sp-3); font-size: 14px; }
.peak-hi b { color: var(--brand); }
.sub { font-size: 12px; font-weight: 600; color: var(--text-2); margin: 0 0 var(--sp-2); }
.chart-sm { height: 200px; } .chart-md { height: 240px; }

/* Phân bố sao */
.dist { display: flex; flex-direction: column; gap: 6px; }
.dist-row { display: flex; align-items: center; gap: var(--sp-3); }
.dist-lbl { width: 32px; font-size: 13px; color: var(--text-2); font-weight: 600; }
.dist-bg { flex: 1; height: 12px; background: var(--surface-2); border-radius: var(--radius-pill); overflow: hidden; }
.dist-bar { height: 100%; background: linear-gradient(90deg, var(--star), #f5a623); border-radius: var(--radius-pill); transition: width var(--ease); }
.dist-cnt { width: 42px; text-align: right; font-size: 13px; color: var(--text-2); }

.funnel { display: flex; flex-direction: column; gap: 8px; margin-bottom: var(--sp-3); }
.fn-row { display: flex; align-items: center; gap: var(--sp-3); }
.fn-lbl { width: 110px; font-size: 13px; color: var(--text-2); flex-shrink: 0; }
.fn-bar { background: linear-gradient(90deg, var(--brand-light), var(--brand)); color: #fff; padding: 6px 12px; border-radius: var(--radius-sm); font-weight: 600; font-size: 13px; min-width: 40px; transition: width var(--ease); }
.fn-bar.done { background: linear-gradient(90deg, #34d399, #22aa99); }
.fn-rates { display: flex; gap: var(--sp-5); font-size: 13px; color: var(--text-2); }
.fn-rates b { color: var(--brand); }
</style>
