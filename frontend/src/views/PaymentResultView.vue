<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import Button from 'primevue/button'

const route = useRoute()
const router = useRouter()

// Backend verify chữ ký VNPAY rồi redirect về đây kèm tham số kết quả.
const success = computed(() => route.query.status === 'success')
const orderId = computed(() => route.query.orderId as string | undefined)
const orderCode = computed(() => (route.query.orderCode as string) || '')
const code = computed(() => (route.query.code as string) || '')

// Diễn giải một số mã phản hồi VNPAY thường gặp.
const reason = computed(() => {
  const map: Record<string, string> = {
    '00': 'Giao dịch thành công.',
    '24': 'Bạn đã hủy giao dịch.',
    '51': 'Tài khoản không đủ số dư.',
    '11': 'Đã hết hạn chờ thanh toán.',
    '97': 'Chữ ký không hợp lệ.'
  }
  return map[code.value] || 'Giao dịch không thành công.'
})

function goToOrder() {
  if (orderId.value) router.push(`/orders/${orderId.value}`)
  else router.push('/orders')
}
</script>

<template>
  <div class="page">
    <div class="container result">
      <div class="surface-card card">
        <i class="pi" :class="success ? 'pi-check-circle ok' : 'pi-times-circle err'"></i>
        <h1>{{ success ? 'Thanh toán thành công' : 'Thanh toán thất bại' }}</h1>
        <p class="text-muted">{{ reason }}</p>
        <p v-if="orderCode" class="code">Mã đơn hàng: <strong>{{ orderCode }}</strong></p>

        <div class="actions">
          <Button label="Xem đơn hàng" icon="pi pi-receipt" @click="goToOrder" />
          <Button label="Tiếp tục mua sắm" icon="pi pi-shopping-bag" outlined @click="router.push('/products')" />
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.result { display: flex; justify-content: center; padding: var(--sp-8) var(--sp-4); }
.card { max-width: 480px; width: 100%; text-align: center; padding: var(--sp-8) var(--sp-6); }
.card i { font-size: 4rem; margin-bottom: var(--sp-4); }
.ok { color: var(--success); }
.err { color: var(--brand); }
.card h1 { font-size: 1.5rem; margin: 0 0 var(--sp-2); color: var(--text); }
.code { margin-top: var(--sp-3); color: var(--text-2); }
.actions { display: flex; gap: var(--sp-3); justify-content: center; margin-top: var(--sp-6); flex-wrap: wrap; }
@media (max-width: 768px) { .actions { flex-direction: column; } }
</style>
