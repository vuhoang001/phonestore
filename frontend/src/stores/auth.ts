import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { User } from '@/types'
import { authApi, cartApi } from '@/services'
import { useCartStore } from './cart'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(JSON.parse(localStorage.getItem('user') || 'null'))
  const token = ref<string | null>(localStorage.getItem('token'))
  const refreshToken = ref<string | null>(localStorage.getItem('refreshToken'))

  const isAuthenticated = computed(() => !!token.value)
  const isAdmin = computed(() => user.value?.role === 'Admin' || user.value?.role === 'Seller')

  function persist() {
    token.value ? localStorage.setItem('token', token.value) : localStorage.removeItem('token')
    refreshToken.value ? localStorage.setItem('refreshToken', refreshToken.value) : localStorage.removeItem('refreshToken')
    user.value ? localStorage.setItem('user', JSON.stringify(user.value)) : localStorage.removeItem('user')
  }

  // Gộp giỏ khách (nếu có) vào giỏ user, rồi bỏ token giỏ khách và nạp lại giỏ.
  async function mergeGuestCart() {
    const guest = localStorage.getItem('cartToken')
    if (guest) {
      try { await cartApi.merge(guest) } catch { /* giỏ khách rỗng/không có -> bỏ qua */ }
      localStorage.removeItem('cartToken')
    }
    await useCartStore().fetch()
  }

  async function login(email: string, password: string) {
    const res = await authApi.login({ email, password })
    token.value = res.token
    refreshToken.value = res.refreshToken
    user.value = res.user
    persist()
    await mergeGuestCart()
  }

  async function register(data: { email: string; password: string; fullName: string; phone?: string }) {
    const res = await authApi.register(data)
    token.value = res.token
    refreshToken.value = res.refreshToken
    user.value = res.user
    persist()
    await mergeGuestCart()
  }

  function updateUser(u: User) {
    user.value = u
    persist()
  }

  async function logout() {
    const rt = refreshToken.value
    token.value = null
    user.value = null
    refreshToken.value = null
    persist()
    if (rt) { try { await authApi.logout(rt) } catch { /* ignore */ } }
    useCartStore().reset()
  }

  return { user, token, refreshToken, isAuthenticated, isAdmin, login, register, logout, updateUser }
})
