<script setup lang="ts">
// Trả góp hàng tháng: khách xem lịch trả của từng đơn & thanh toán tuần tự từng kỳ (mock).
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import ProgressSpinner from 'primevue/progressspinner'
import OrderCodeLink from '@/components/OrderCodeLink.vue'
import { installmentApi } from '@/services'
import type { InstallmentPlan } from '@/types'
import { formatCurrency, formatDate } from '@/composables/format'
import { extractError } from '@/services/api'

const toast = useToast()
const confirm = useConfirm()
const plans = ref<InstallmentPlan[]>([])
const loading = ref(true)
const paying = ref<number | null>(null)

const statusLabel: Record<string, string> = { Paid: 'Đã trả', Overdue: 'Quá hạn', Pending: 'Chờ trả' }
const statusSeverity: Record<string, string> = { Paid: 'success', Overdue: 'danger', Pending: 'info' }

async function load() {
  loading.value = true
  try { plans.value = await installmentApi.mine() } finally { loading.value = false }
}

function pct(p: InstallmentPlan) { return p.months ? Math.round((p.paidCount / p.months) * 100) : 0 }

function confirmPay(planIdx: number, instId: number, no: number, amount: number) {
  confirm.require({
    header: `Thanh toán kỳ ${no}`,
    message: `Xác nhận thanh toán ${formatCurrency(amount)} cho kỳ ${no}? (thanh toán mô phỏng)`,
    icon: 'pi pi-wallet',
    acceptLabel: 'Thanh toán',
    rejectLabel: 'Hủy',
    accept: () => pay(planIdx, instId)
  })
}

async function pay(planIdx: number, instId: number) {
  paying.value = instId
  try {
    const updated = await installmentApi.pay(instId)
    plans.value[planIdx] = updated   // thay lịch của đơn bằng bản mới nhất
    toast.add({ severity: 'success', summary: 'Đã thanh toán kỳ trả góp', life: 2500 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 4000 })
  } finally {
    paying.value = null
  }
}

onMounted(load)
</script>

<template>
  <div class="page">
    <h1 class="head">Trả góp của tôi</h1>
    <p class="sub">Theo dõi lịch trả góp hàng tháng và thanh toán từng kỳ. Trả tuần tự từ kỳ sớm nhất.</p>

    <div v-if="loading" class="center"><ProgressSpinner style="width:44px;height:44px" /></div>

    <div v-else-if="!plans.length" class="empty">
      <i class="pi pi-calendar" />
      <p>Bạn chưa có đơn trả góp nào.</p>
      <Button label="Mua trả góp 0%" icon="pi pi-mobile" outlined @click="$router.push('/products')" />
    </div>

    <div v-else class="plans">
      <article v-for="(p, idx) in plans" :key="p.orderId" class="plan">
        <header class="plan-head">
          <div>
            <div class="plan-order">Đơn <OrderCodeLink :id="p.orderId" :code="p.orderCode" /></div>
            <div class="plan-date">Đặt ngày {{ formatDate(p.orderDate) }} · {{ p.months }} kỳ × {{ formatCurrency(p.monthly) }}</div>
          </div>
          <Tag v-if="p.completed" value="Hoàn tất" severity="success" />
          <Tag v-else :value="`Còn ${formatCurrency(p.remainingAmount)}`" severity="info" />
        </header>

        <!-- Thanh tiến độ -->
        <div class="bar"><span class="fill" :style="{ width: pct(p) + '%' }" /></div>
        <div class="bar-row">
          <span>Đã trả {{ p.paidCount }}/{{ p.months }} kỳ ({{ formatCurrency(p.paidAmount) }})</span>
          <span v-if="p.nextDueDate && !p.completed" class="next">Kỳ tới: {{ formatDate(p.nextDueDate) }}</span>
        </div>

        <!-- Danh sách kỳ -->
        <div class="rows">
          <div v-for="it in p.payments" :key="it.id" class="row" :class="{ paid: it.status === 'Paid', overdue: it.status === 'Overdue' }">
            <span class="no">Kỳ {{ it.installmentNo }}</span>
            <span class="due">{{ formatDate(it.dueDate) }}</span>
            <span class="amt">{{ formatCurrency(it.amount) }}</span>
            <Tag :value="statusLabel[it.status] || it.status" :severity="statusSeverity[it.status]" />
            <span class="paid-at">{{ it.paidAt ? 'Đã trả ' + formatDate(it.paidAt) : '' }}</span>
            <Button v-if="it.payable" label="Thanh toán" size="small" icon="pi pi-wallet"
              :loading="paying === it.id" @click="confirmPay(idx, it.id, it.installmentNo, it.amount)" />
          </div>
        </div>
      </article>
    </div>
  </div>
</template>

<style scoped>
.head { margin: 0 0 4px; }
.sub { color: var(--text-2); margin: 0 0 var(--sp-5); }
.center { display: flex; justify-content: center; padding: var(--sp-8); }
.empty { text-align: center; padding: var(--sp-8) var(--sp-4); color: var(--text-muted); display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); }
.empty .pi { font-size: 2.2rem; opacity: .5; }

.plans { display: flex; flex-direction: column; gap: var(--sp-4); }
.plan { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); padding: var(--sp-5); }
.plan-head { display: flex; align-items: flex-start; justify-content: space-between; gap: var(--sp-3); margin-bottom: var(--sp-4); }
.plan-order { font-weight: 600; font-size: 1.05rem; }
.plan-date { color: var(--text-muted); font-size: 13px; margin-top: 2px; }

.bar { height: 8px; background: var(--surface-2); border: 1px solid var(--border); border-radius: var(--radius-pill); overflow: hidden; }
.fill { display: block; height: 100%; background: var(--ok); border-radius: var(--radius-pill); transition: width var(--ease); }
.bar-row { display: flex; justify-content: space-between; gap: var(--sp-3); font-size: 13px; color: var(--text-2); margin: 6px 0 var(--sp-4); flex-wrap: wrap; }
.next { color: var(--brand); font-weight: 500; }

.rows { display: flex; flex-direction: column; gap: 2px; }
.row { display: grid; grid-template-columns: 64px 110px 1fr auto auto auto; align-items: center; gap: var(--sp-3); padding: 10px var(--sp-3); border-radius: var(--radius); }
.row + .row { border-top: 1px solid var(--border); border-radius: 0; }
.row.paid { opacity: .72; }
.row.overdue { background: #fef2f2; }
.no { font-weight: 600; }
.due { color: var(--text-2); font-size: 13px; }
.amt { font-weight: 600; }
.paid-at { color: var(--text-muted); font-size: 12px; text-align: right; }

@media (max-width: 700px) {
  .row { grid-template-columns: 1fr auto; row-gap: 4px; }
  .due, .paid-at { display: none; }
}
</style>
