import { defineStore } from 'pinia'
import { ref } from 'vue'
import * as signalR from '@microsoft/signalr'
import type { AppNotification } from '@/types'
import { notificationApi } from '@/services'
import { useAuthStore } from './auth'

// URL hub suy ra từ baseURL API: '/api' → '/hubs/notifications' (nginx/vite proxy WS);
// 'http://host:8080/api' → 'http://host:8080/hubs/notifications'.
function hubUrl() {
  const base = import.meta.env.VITE_API_BASE_URL || '/api'
  return base.replace(/\/api\/?$/, '') + '/hubs/notifications'
}

export const useNotificationStore = defineStore('notification', () => {
  const items = ref<AppNotification[]>([])
  const unreadCount = ref(0)
  const connected = ref(false)
  let connection: signalR.HubConnection | null = null

  async function fetch() {
    const auth = useAuthStore()
    if (!auth.isAuthenticated) { items.value = []; unreadCount.value = 0; return }
    const res = await notificationApi.mine()
    items.value = res.items
    unreadCount.value = res.unreadCount
  }

  // Mở kết nối realtime; nhận sự kiện "notification" do server đẩy xuống.
  async function connect() {
    const auth = useAuthStore()
    if (!auth.isAuthenticated || connection) return
    connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl(), { accessTokenFactory: () => localStorage.getItem('token') || '' })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connection.on('notification', (n: AppNotification) => {
      // Chèn lên đầu; tránh trùng nếu vô tình đã có (vd vừa fetch xong).
      if (!items.value.some((x) => x.id === n.id)) {
        items.value.unshift(n)
        if (!n.isRead) unreadCount.value++
      }
    })

    try {
      await connection.start()
      connected.value = true
    } catch {
      // Offline/lỗi kết nối: bỏ qua — DB vẫn là nguồn sự thật, sẽ thấy khi fetch lại.
      connection = null
    }
  }

  async function disconnect() {
    connected.value = false
    if (connection) {
      const c = connection
      connection = null
      await c.stop().catch(() => {})
    }
  }

  async function markRead(id: number) {
    // Cập nhật UI ngay (optimistic); gọi API best-effort, lỗi mạng không làm hỏng luồng gọi (vd: điều hướng).
    const n = items.value.find((x) => x.id === id)
    if (n && !n.isRead) { n.isRead = true; unreadCount.value = Math.max(0, unreadCount.value - 1) }
    await notificationApi.read(id).catch(() => {})
  }

  async function markAllRead() {
    // Cập nhật UI ngay (optimistic) để badge + chấm đỏ mất tức thì; gọi API chạy nền.
    items.value.forEach((n) => (n.isRead = true))
    unreadCount.value = 0
    await notificationApi.readAll().catch(() => {})
  }

  return { items, unreadCount, connected, fetch, connect, disconnect, markRead, markAllRead }
})
