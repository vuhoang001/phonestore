<script setup lang="ts">
import { ref } from 'vue'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import { authApi } from '@/services'
import { extractError } from '@/services/api'

const email = ref('')
const loading = ref(false)
const sent = ref(false)
const error = ref('')

async function submit() {
  loading.value = true
  error.value = ''
  try {
    await authApi.forgotPassword(email.value)
    sent.value = true
  } catch (e) {
    error.value = extractError(e)
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="auth-wrap">
    <div class="auth-card">
      <h1>Quên mật khẩu</h1>
      <template v-if="!sent">
        <p class="text-muted">Nhập email, chúng tôi sẽ gửi link đặt lại mật khẩu.</p>
        <form @submit.prevent="submit" class="form">
          <label>Email</label>
          <InputText v-model="email" type="email" required class="w-full" />
          <small v-if="error" class="err">{{ error }}</small>
          <Button type="submit" label="Gửi link đặt lại" :loading="loading" class="w-full mt-2" />
        </form>
      </template>
      <div v-else class="done">
        <i class="pi pi-envelope"></i>
        <p>Nếu email tồn tại, link đặt lại mật khẩu đã được gửi. Vui lòng kiểm tra hòm thư.</p>
      </div>
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
.done { text-align: center; padding: var(--sp-4) 0; }
.done i { font-size: 3rem; color: #22c55e; margin-bottom: var(--sp-3); }
</style>
