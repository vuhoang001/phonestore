<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { extractError } from '@/services/api'

const router = useRouter()
const route = useRoute()
const toast = useToast()
const auth = useAuthStore()
const cart = useCartStore()

const email = ref('customer@phone.com')
const password = ref('Customer@123')
const loading = ref(false)

async function submit() {
  loading.value = true
  try {
    await auth.login(email.value, password.value)
    await cart.fetch()
    toast.add({ severity: 'success', summary: 'Đăng nhập thành công', life: 2000 })
    const redirect = (route.query.redirect as string) || '/'
    router.push(redirect)
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
      <h1>Đăng nhập</h1>
      <p class="text-muted">Demo: admin@phone.com / Admin@123 · customer@phone.com / Customer@123</p>
      <form @submit.prevent="submit" class="form">
        <label>Email</label>
        <InputText v-model="email" type="email" required class="w-full" />
        <label>Mật khẩu</label>
        <Password v-model="password" :feedback="false" toggleMask fluid inputClass="w-full" />
        <Button type="submit" label="Đăng nhập" :loading="loading" class="w-full mt-2" />
      </form>
      <p class="text-center mt-2">
        <router-link to="/forgot-password" class="link">Quên mật khẩu?</router-link>
      </p>
      <p class="text-center mt-1">
        Chưa có tài khoản? <router-link to="/register" class="link">Đăng ký</router-link>
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
h1 { margin: 0 0 var(--sp-1); color: var(--brand); }
.form { display: flex; flex-direction: column; gap: var(--sp-1); margin-top: var(--sp-4); }
.form label { font-weight: 600; font-size: 0.9rem; margin-top: var(--sp-2); color: var(--text-2); }
.link { color: var(--brand); font-weight: 600; }
.link:hover { color: var(--brand-dark); }
.text-center { text-align: center; color: var(--text-2); }
</style>
