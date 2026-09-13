<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'
import { paymentApi } from '@/services'
import { extractError } from '@/services/api'
import { formatCurrency } from '@/composables/format'

const route = useRoute()
const router = useRouter()
const toast = useToast()

// Trang GIẢ LẬP cổng VNPAY — chỉ dùng khi chưa cấu hình credential thật.
const orderId = computed(() => Number(route.query.orderId))
const orderCode = computed(() => (route.query.orderCode as string) || '')
const amount = computed(() => Number(route.query.amount) || 0)
const processing = ref(false)

async function pay(success: boolean) {
  if (!orderId.value) return
  processing.value = true
  try {
    const r = await paymentApi.mockComplete(orderId.value, success)
    router.push({
      name: 'payment-result',
      query: {
        status: r.success ? 'success' : 'failed',
        orderId: r.orderId?.toString(),
        orderCode: r.orderCode,
        code: r.responseCode
      }
    })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi giả lập', detail: extractError(e), life: 4000 })
    processing.value = false
  }
}
</script>

<template>
  <div class="gate">
    <div class="surface-card panel">
      <div class="brand">
        <span class="logo">VN<span>PAY</span></span>
        <span class="sim">Giả lập · Sandbox</span>
      </div>

      <div class="amount">
        <span class="text-muted">Số tiền thanh toán</span>
        <strong class="price">{{ formatCurrency(amount) }}</strong>
      </div>
      <p class="order text-muted">Đơn hàng: <strong>{{ orderCode }}</strong></p>

      <div class="note">
        <i class="pi pi-info-circle"></i>
        Đây là cổng thanh toán <strong>giả lập</strong> để demo. Điền TmnCode/HashSecret thật
        (đăng ký tại sandbox.vnpayment.vn) là tự động chuyển sang VNPAY thật.
      </div>

      <div class="actions">
        <Button label="Thanh toán thành công" icon="pi pi-check" :loading="processing" @click="pay(true)" />
        <Button label="Hủy giao dịch" icon="pi pi-times" severity="secondary" outlined
                :disabled="processing" @click="pay(false)" />
      </div>
    </div>
  </div>
</template>

<style scoped>
.gate { min-height: 70vh; display: flex; align-items: center; justify-content: center; padding: var(--sp-4); }
.panel { width: 100%; max-width: 420px; padding: var(--sp-6); }
.brand { display: flex; align-items: center; justify-content: space-between; margin-bottom: var(--sp-5); }
.logo { font-weight: 800; font-size: 1.4rem; color: #005baa; letter-spacing: -0.5px; }
.logo span { color: var(--brand); }
.sim { font-size: 0.75rem; color: var(--text-muted); background: var(--brand-50); padding: 2px var(--sp-2); border-radius: var(--radius-pill); }
.amount { display: flex; flex-direction: column; gap: var(--sp-1); padding: var(--sp-4); background: var(--surface-2); border-radius: var(--radius); text-align: center; }
.amount .price { font-size: 1.8rem; }
.order { margin: var(--sp-3) 0 0; text-align: center; }
.note { display: flex; gap: var(--sp-2); font-size: 0.82rem; color: var(--text-2); background: var(--brand-50); border-radius: var(--radius); padding: var(--sp-3); margin: var(--sp-4) 0; }
.note i { color: var(--brand); margin-top: 2px; }
.actions { display: flex; flex-direction: column; gap: var(--sp-3); }
</style>
