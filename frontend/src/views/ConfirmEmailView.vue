<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import Button from 'primevue/button'
import ProgressSpinner from 'primevue/progressspinner'
import { authApi } from '@/services'
import { extractError } from '@/services/api'

const route = useRoute()
const state = ref<'loading' | 'ok' | 'error'>('loading')
const message = ref('')

onMounted(async () => {
  const token = (route.query.token as string) || ''
  if (!token) { state.value = 'error'; message.value = 'Thiếu token xác nhận.'; return }
  try {
    await authApi.confirmEmail(token)
    state.value = 'ok'
  } catch (e) {
    state.value = 'error'
    message.value = extractError(e)
  }
})
</script>

<template>
  <div class="auth-wrap">
    <div class="auth-card center">
      <template v-if="state === 'loading'"><ProgressSpinner /></template>
      <template v-else-if="state === 'ok'">
        <i class="pi pi-check-circle ok"></i>
        <h1>Xác nhận email thành công</h1>
        <p class="text-muted">Tài khoản của bạn đã được kích hoạt.</p>
        <Button label="Đăng nhập" icon="pi pi-sign-in" class="mt-3" @click="$router.push('/login')" />
      </template>
      <template v-else>
        <i class="pi pi-times-circle err"></i>
        <h1>Xác nhận thất bại</h1>
        <p class="text-muted">{{ message }}</p>
        <Button label="Về trang chủ" outlined class="mt-3" @click="$router.push('/')" />
      </template>
    </div>
  </div>
</template>

<style scoped>
.auth-wrap { min-height: 100vh; display: flex; align-items: center; justify-content: center; background: var(--bg); padding: var(--sp-4); }
.auth-card { background: var(--surface); padding: var(--sp-8); border-top: 3px solid var(--brand); border-radius: var(--radius-lg); box-shadow: var(--shadow-md); width: 100%; max-width: 420px; }
.center { text-align: center; }
.auth-card i { font-size: 3.5rem; margin-bottom: var(--sp-3); }
.ok { color: #22c55e; }
.err { color: var(--brand); }
h1 { margin: 0 0 var(--sp-2); font-size: 1.4rem; color: var(--text); }
</style>
