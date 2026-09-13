<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import Tag from 'primevue/tag'
import Button from 'primevue/button'
import ProgressSpinner from 'primevue/progressspinner'
import { useToast } from 'primevue/usetoast'
import { orderApi, paymentApi } from '@/services'
import { extractError } from '@/services/api'
import type { Order } from '@/types'
import { formatCurrency, formatDate, orderStatusLabel, orderStatusSeverity, paymentMethodLabel, paymentStatusLabel } from '@/composables/format'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const order = ref<Order | null>(null)
const loading = ref(true)
const paying = ref(false)

// Cho phép thanh toán lại khi là đơn VNPay chưa thanh toán và chưa bị hủy.
const canPayVnPay = computed(() =>
  order.value?.paymentMethod === 'VnPay' &&
  order.value?.paymentStatus === 'Pending' &&
  order.value?.status !== 'Cancelled')

async function payVnPay() {
  if (!order.value) return
  paying.value = true
  try {
    const { paymentUrl } = await paymentApi.vnpay(order.value.id)
    window.location.href = paymentUrl
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Không tạo được thanh toán', detail: extractError(e), life: 4000 })
    paying.value = false
  }
}

// Luôn lấy trạng thái mới nhất từ server. silent=true → làm mới ngầm (không hiện spinner).
async function load(silent = false) {
  if (!silent) loading.value = true
  try {
    order.value = await orderApi.byId(Number(route.params.id))
  } catch (e) {
    if (!silent) toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    if (!silent) loading.value = false
  }
}

// Quay lại tab (đơn có thể vừa được admin cập nhật) → tự làm mới trạng thái.
function onVisible() { if (document.visibilityState === 'visible' && order.value) load(true) }

onMounted(() => { load(); document.addEventListener('visibilitychange', onVisible) })
onUnmounted(() => document.removeEventListener('visibilitychange', onVisible))
// Đổi sang xem đơn khác (không remount component) → tải lại đúng đơn.
watch(() => route.params.id, () => load())
</script>

<template>
  <div v-if="loading" class="center"><ProgressSpinner /></div>
  <div v-else-if="order">
    <Button label="Quay lại" icon="pi pi-arrow-left" text size="small" @click="router.push('/orders')" class="back" />

    <div class="head">
      <h1>{{ order.orderCode }}</h1>
      <Tag :value="orderStatusLabel[order.status]" :severity="orderStatusSeverity[order.status]" />
      <span class="head-date"><i class="pi pi-calendar" /> {{ formatDate(order.createdAt) }}</span>
    </div>

    <div class="grid">
      <!-- Cột trái: sản phẩm + tổng tiền -->
      <div class="surface-card">
        <h3 class="card-title">Sản phẩm đã đặt</h3>
        <div v-for="i in order.items" :key="i.id" class="item-row">
          <div class="item-info">
            <span class="item-name">{{ i.productName }}</span>
            <span v-if="i.variantInfo" class="item-variant">{{ i.variantInfo }}</span>
            <!-- IMEI/Serial gán khi giao máy (đặc thù điện thoại) -->
            <span v-if="i.imei" class="item-imei"><i class="pi pi-hashtag" /> IMEI: {{ i.imei }}</span>
            <span class="item-unit">{{ formatCurrency(i.price) }} × {{ i.quantity }}</span>
          </div>
          <span class="price">{{ formatCurrency(i.lineTotal) }}</span>
        </div>

        <div class="totals">
          <div class="row"><span>Tạm tính</span><span>{{ formatCurrency(order.subTotal) }}</span></div>
          <div class="row" v-if="order.discountAmount"><span>Giảm giá</span><span class="minus">-{{ formatCurrency(order.discountAmount) }}</span></div>
          <div class="row"><span>Phí vận chuyển</span><span>{{ formatCurrency(order.shippingFee) }}</span></div>
          <div class="row total"><span>Tổng cộng</span><span class="price">{{ formatCurrency(order.totalAmount) }}</span></div>
        </div>
      </div>

      <!-- Cột phải: giao hàng + timeline -->
      <div class="side">
        <div class="surface-card">
          <h3 class="card-title">Giao hàng</h3>
          <p class="ship-addr">{{ order.shippingAddress }}</p>
          <p v-if="order.note" class="ship-note"><i class="pi pi-comment" /> {{ order.note }}</p>
          <div class="pay-line">
            <span class="pay-label">Thanh toán</span>
            <span>{{ paymentMethodLabel[order.paymentMethod || ''] || order.paymentMethod }}
              <span class="dot-sep">·</span>
              <span class="pay-status">{{ paymentStatusLabel[order.paymentStatus || ''] || order.paymentStatus }}</span>
            </span>
          </div>
          <!-- Thông tin trả góp (nếu đơn mua trả góp) -->
          <div v-if="order.installmentMonths" class="inst-line">
            <span class="pay-label"><i class="pi pi-calendar" /> Trả góp</span>
            <span>{{ order.installmentMonths }} tháng
              <template v-if="order.installmentMonthly">
                <span class="dot-sep">·</span>
                <strong class="price">{{ formatCurrency(order.installmentMonthly) }}</strong>/tháng
              </template>
            </span>
          </div>
          <Button v-if="canPayVnPay" label="Thanh toán chuyển khoản" icon="pi pi-qrcode"
                  class="w-full mt-2" :loading="paying" @click="payVnPay" />
        </div>

        <div class="surface-card">
          <h3 class="card-title">Lịch sử trạng thái</h3>
          <ol class="timeline">
            <li v-for="(h, idx) in order.statusHistory" :key="idx" class="tl-item"
                :class="{ last: idx === order.statusHistory.length - 1, current: idx === order.statusHistory.length - 1 }">
              <span class="tl-dot" />
              <div class="tl-body">
                <span class="tl-status">{{ orderStatusLabel[h.status] }}</span>
                <span class="tl-time">{{ formatDate(h.changedAt) }}</span>
                <span v-if="h.note" class="tl-note">{{ h.note }}</span>
              </div>
            </li>
          </ol>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.center { display: flex; justify-content: center; padding: var(--sp-8); }
.back { margin-bottom: var(--sp-2); }

.head { display: flex; align-items: center; gap: var(--sp-3); flex-wrap: wrap; margin-bottom: var(--sp-5); }
.head h1 { margin: 0; font-size: 1.5rem; letter-spacing: 0.01em; }
.head-date { display: inline-flex; align-items: center; gap: 6px; color: var(--text-muted); font-size: 13px; margin-left: auto; }
.head-date .pi { font-size: 12px; }

.grid { display: grid; grid-template-columns: 1fr 360px; gap: var(--sp-5); align-items: start; }
.side { display: flex; flex-direction: column; gap: var(--sp-5); }
.card-title { margin: 0 0 var(--sp-4); font-size: 1rem; }

/* Sản phẩm */
.item-row { display: flex; justify-content: space-between; gap: var(--sp-4); padding: var(--sp-3) 0; border-bottom: 1px solid var(--border); }
.item-info { display: flex; flex-direction: column; gap: 3px; min-width: 0; }
.item-name { font-weight: 600; }
.item-variant { font-size: 12px; color: var(--text-2); }
.item-imei { font-size: 12px; color: var(--brand); font-weight: 600; display: inline-flex; align-items: center; gap: 4px; }
.item-imei .pi { font-size: 10px; }
.item-unit { font-size: 12px; color: var(--text-muted); }
.item-row .price { white-space: nowrap; }

/* Tổng tiền — tách khối bằng khoảng trắng, chỉ 1 kẻ trên dòng tổng */
.totals { margin-top: var(--sp-4); display: flex; flex-direction: column; gap: var(--sp-2); }
.row { display: flex; justify-content: space-between; color: var(--text-2); font-size: 0.9rem; }
.row .minus { color: var(--brand); }
.row.total { margin-top: var(--sp-2); padding-top: var(--sp-3); border-top: 1px solid var(--border); color: var(--text); font-weight: 700; font-size: 1.15rem; }

/* Giao hàng */
.ship-addr { margin: 0; line-height: 1.6; }
.ship-note { display: flex; align-items: flex-start; gap: 6px; margin: var(--sp-2) 0 0; color: var(--text-2); font-size: 0.9rem; }
.ship-note .pi { color: var(--brand); font-size: 12px; margin-top: 3px; }
.pay-line { display: flex; justify-content: space-between; align-items: center; gap: var(--sp-3); margin-top: var(--sp-4); padding-top: var(--sp-3); border-top: 1px solid var(--border); font-size: 0.9rem; }
.pay-label { color: var(--text-muted); display: inline-flex; align-items: center; gap: 5px; }
.pay-label .pi { font-size: 12px; }
.pay-status { font-weight: 600; }
.inst-line { display: flex; justify-content: space-between; align-items: center; gap: var(--sp-3); margin-top: var(--sp-3); font-size: 0.9rem; }
.dot-sep { opacity: 0.6; margin: 0 4px; }

/* Timeline tự dựng — rail dọc bên trái, nội dung full-width (không xuống dòng lộn xộn) */
.timeline { list-style: none; margin: 0; padding: 0; }
.tl-item { position: relative; padding: 0 0 var(--sp-4) var(--sp-5); }
.tl-item::before { content: ''; position: absolute; left: 5px; top: 4px; bottom: 0; width: 2px; background: var(--border); }
.tl-item.last { padding-bottom: 0; }
.tl-item.last::before { display: none; }
.tl-dot { position: absolute; left: 0; top: 3px; width: 12px; height: 12px; border-radius: 50%; background: #cfd3da; box-shadow: 0 0 0 3px var(--surface); }
.tl-item.current .tl-dot { background: var(--brand); box-shadow: 0 0 0 3px var(--brand-50); }
.tl-body { display: flex; flex-direction: column; gap: 2px; }
.tl-status { font-weight: 600; font-size: 0.9rem; }
.tl-item.current .tl-status { color: var(--brand); }
.tl-time { font-size: 12px; color: var(--text-muted); }
.tl-note { font-size: 12px; color: var(--text-2); margin-top: 2px; }

@media (max-width: 900px) { .grid { grid-template-columns: 1fr; } }
</style>
