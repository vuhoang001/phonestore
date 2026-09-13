<script setup lang="ts">
// Chuông thông báo dùng chung cho cả MainLayout (nền cam → prop light) và AdminLayout (nền trắng).
// Dữ liệu + realtime lấy từ useNotificationStore (đã kết nối SignalR).
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import Badge from 'primevue/badge'
import Popover from 'primevue/popover'
import { useNotificationStore } from '@/stores/notification'
import { formatDate } from '@/composables/format'

defineProps<{ light?: boolean }>()
const router = useRouter()
const notif = useNotificationStore()
const panel = ref()

function toggle(e: Event) { panel.value.toggle(e) }
function open(n: { id: number; isRead: boolean; link?: string }) {
  panel.value.hide()
  // Điều hướng NGAY tới đích (vd: /admin/orders?open=<id>) — không để việc đánh dấu đã đọc chặn.
  if (n.link) router.push(n.link)
  if (!n.isRead) notif.markRead(n.id).catch(() => {})   // best-effort, lỗi API không ảnh hưởng điều hướng
}
</script>

<template>
  <button class="bell" :class="{ light }" aria-label="Thông báo" @click="toggle">
    <i class="pi pi-bell" />
    <Badge v-if="notif.unreadCount" :value="notif.unreadCount" class="bell-badge" />
  </button>

  <Popover ref="panel" class="notif-pop">
    <div class="notif-panel">
      <div class="notif-top">
        <strong>Thông báo <span v-if="notif.unreadCount" class="nt-count">{{ notif.unreadCount }}</span></strong>
        <button v-if="notif.unreadCount" class="mark-all" @click="notif.markAllRead()">Đánh dấu đã đọc</button>
      </div>
      <div v-if="notif.items.length" class="notif-list">
        <button v-for="n in notif.items.slice(0, 8)" :key="n.id" class="notif-item" :class="{ unread: !n.isRead }" @click="open(n)">
          <span class="ni-dot" :class="[n.type, { hidden: n.isRead }]" />
          <span class="ni-body">
            <strong>{{ n.title }}</strong>
            <span class="ni-msg">{{ n.message }}</span>
            <span class="ni-time">{{ formatDate(n.createdAt) }}</span>
          </span>
        </button>
      </div>
      <p v-else class="notif-empty">Chưa có thông báo nào.</p>
      <router-link to="/notifications" class="notif-all" @click="panel.hide()">Xem tất cả</router-link>
    </div>
  </Popover>
</template>

<style scoped>
/* Đồng bộ với nút giỏ hàng (.icon-btn): nút tròn 40×40, icon 1.4rem */
.bell { position: relative; color: var(--text); background: none; border: none; cursor: pointer;
  display: inline-grid; place-items: center; width: 40px; height: 40px; border-radius: 50%; transition: background var(--ease); }
.bell.light { color: #fff; }
.bell .pi { font-size: 1.4rem; }
.bell:hover { background: var(--surface-2); }
.bell.light:hover { background: rgba(255, 255, 255, 0.15); }
/* Badge giống hệt cart-badge để không lệch */
.bell-badge { position: absolute; top: 4px; right: 4px; min-width: 17px; height: 17px; line-height: 17px; padding: 0 4px;
  font-size: 10px; font-weight: 700; border-radius: var(--radius-pill);
  background: var(--brand) !important; color: #fff !important; }
.bell.light .bell-badge { background: #fff !important; color: var(--brand) !important; box-shadow: 0 0 0 2px var(--brand); }

.notif-panel { width: 340px; max-width: 90vw; }
.notif-top { display: flex; justify-content: space-between; align-items: center; padding-bottom: var(--sp-2); border-bottom: 1px solid var(--border); margin-bottom: var(--sp-2); }
.mark-all { background: none; border: none; color: var(--brand); cursor: pointer; font-size: 12px; font-family: inherit; }
.mark-all:hover { text-decoration: underline; }
.nt-count { display: inline-grid; place-items: center; min-width: 18px; height: 18px; padding: 0 5px; margin-left: 4px;
  background: var(--brand); color: #fff; border-radius: var(--radius-pill); font-size: 11px; font-weight: 700; }
.notif-list { display: flex; flex-direction: column; gap: 2px; max-height: 360px; overflow-y: auto; }
.notif-item { display: flex; gap: 10px; text-align: left; background: none; border: none; border-left: 3px solid transparent;
  cursor: pointer; padding: 9px 8px; border-radius: var(--radius-sm); font-family: inherit; transition: background var(--ease); }
.notif-item:hover { background: var(--surface-2); }
/* Chưa đọc: nổi bật (nền cam nhạt + gạch cam trái + tiêu đề đậm cam) */
.notif-item.unread { background: var(--brand-50); border-left-color: var(--brand); }
.notif-item.unread .ni-body strong { color: var(--brand-dark); }
.ni-dot { width: 8px; height: 8px; border-radius: 50%; margin-top: 6px; flex-shrink: 0; background: var(--brand); }
.ni-dot.promotion { background: #ffab00; }
.ni-dot.hidden { visibility: hidden; }
.ni-body { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.ni-body strong { font-size: 13px; }
.ni-msg { font-size: 12px; color: var(--text-2); }
.ni-time { font-size: 11px; color: var(--text-muted); }
.notif-empty { text-align: center; color: var(--text-muted); padding: var(--sp-4); font-size: 13px; }
.notif-all { display: block; text-align: center; color: var(--brand); font-weight: 500; padding-top: var(--sp-2); border-top: 1px solid var(--border); margin-top: var(--sp-2); font-size: 13px; }
</style>
