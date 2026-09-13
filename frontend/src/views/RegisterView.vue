<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'
import { useAuthStore } from '@/stores/auth'
import { extractError } from '@/services/api'

const router = useRouter()
const toast = useToast()
const auth = useAuthStore()

const form = ref({ fullName: '', email: '', phone: '', password: '' })
const loading = ref(false)

async function submit() {
  loading.value = true
  try {
    await auth.register(form.value)
    toast.add({ severity: 'success', summary: 'Đăng ký thành công', life: 2000 })
    router.push('/')
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="auth-wrap">
    <div class="auth-card">
      <h1>Đăng ký tài khoản</h1>
      <form @submit.prevent="submit" class="form">
        <label>Họ tên</label>
        <InputText v-model="form.fullName" required class="w-full" />
        <label>Email</label>
        <InputText v-model="form.email" type="email" required class="w-full" />
        <label>Số điện thoại</label>
        <InputText v-model="form.phone" class="w-full" />
        <label>Mật khẩu (tối thiểu 6 ký tự)</label>
        <Password v-model="form.password" toggleMask fluid inputClass="w-full" />
        <Button type="submit" label="Đăng ký" :loading="loading" class="w-full mt-2" />
      </form>
      <p class="text-center mt-3">
        Đã có tài khoản? <router-link to="/login" class="link">Đăng nhập</router-link>
      </p>
    </div>
  </div>
</template>

<style scoped>
.auth-wrap { min-height: 100vh; display: flex; align-items: center; justify-content: center; background: var(--bg); padding: var(--sp-4); }
.auth-card {
  background: var(--surface);
  padding: var(--sp-8);
  border-top: 3px solid var(--brand);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-md);
  width: 100%; max-width: 420px;
}
h1 { margin: 0 0 var(--sp-2); color: var(--brand); }
.form { display: flex; flex-direction: column; gap: var(--sp-1); }
.form label { font-weight: 600; font-size: 0.9rem; margin-top: var(--sp-2); color: var(--text-2); }
.link { color: var(--brand); font-weight: 600; }
.link:hover { color: var(--brand-dark); }
.text-center { text-align: center; color: var(--text-2); }
</style>
