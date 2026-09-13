<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import Password from 'primevue/password'
import Button from 'primevue/button'
import { authApi } from '@/services'
import { extractError } from '@/services/api'

const route = useRoute()
const router = useRouter()
const toast = useToast()

const token = computed(() => (route.query.token as string) || '')
const password = ref('')
const confirm = ref('')
const loading = ref(false)

async function submit() {
  if (password.value.length < 6) {
    toast.add({ severity: 'warn', summary: 'Mật khẩu tối thiểu 6 ký tự', life: 2500 })
    return
  }
  if (password.value !== confirm.value) {
    toast.add({ severity: 'warn', summary: 'Mật khẩu nhập lại không khớp', life: 2500 })
    return
  }
  loading.value = true
  try {
    await authApi.resetPassword(token.value, password.value)
    toast.add({ severity: 'success', summary: 'Đặt lại mật khẩu thành công', life: 2500 })
    router.push('/login')
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 4000 })
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="auth-wrap">
    <div class="auth-card">
      <h1>Đặt lại mật khẩu</h1>
      <p v-if="!token" class="err">Link không hợp lệ (thiếu token).</p>
      <form v-else @submit.prevent="submit" class="form">
        <label>Mật khẩu mới</label>
        <Password v-model="password" :feedback="false" toggleMask fluid inputClass="w-full" />
        <label>Nhập lại mật khẩu</label>
        <Password v-model="confirm" :feedback="false" toggleMask fluid inputClass="w-full" />
        <Button type="submit" label="Đặt lại mật khẩu" :loading="loading" class="w-full mt-2" />
      </form>
      <p class="text-center mt-3"><router-link to="/login" class="link">← Về đăng nhập</router-link></p>
    </div>
  </div>
</template>

<style scoped>
.auth-wrap { min-height: 100vh; display: flex; align-items: center; justify-content: center; background: var(--bg); padding: var(--sp-4); }
.auth-card { background: var(--surface); padding: var(--sp-8); border-top: 3px solid var(--brand); border-radius: var(--radius-lg); box-shadow: var(--shadow-md); width: 100%; max-width: 420px; }
h1 { margin: 0 0 var(--sp-2); color: var(--brand); }
.form { display: flex; flex-direction: column; gap: var(--sp-1); margin-top: var(--sp-4); }
.form label { font-weight: 600; font-size: 0.9rem; margin-top: var(--sp-2); color: var(--text-2); }
.link { color: var(--brand); font-weight: 600; }
.text-center { text-align: center; }
.err { color: var(--brand); }
</style>
