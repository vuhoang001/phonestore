<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import Paginator from 'primevue/paginator'
import { useNotificationStore } from '@/stores/notification'
import { formatDate } from '@/composables/format'

const router = useRouter()
const notif = useNotificationStore()

// Phân trang phía client trên danh sách đã tải.
const first = ref(0)
const pageSize = ref(8)
const paged = computed(() => notif.items.slice(first.value, first.value + pageSize.value))
function onPage(e: { first: number; rows: number }) {
  first.value = e.first; pageSize.value = e.rows
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

function open(n: { id: number; isRead: boolean; link?: string }) {
  // Điều hướng ngay; đánh dấu đã đọc best-effort để lỗi API không chặn.
  if (n.link) router.push(n.link)
  if (!n.isRead) notif.markRead(n.id).catch(() => {})
}

onMounted(() => notif.fetch())
</script>

<template>
  <div class="head">
    <h1 class="section-title">Thông báo <span v-if="notif.unreadCount" class="badge-new">{{ notif.unreadCount }} mới</span></h1>
    <Button v-if="notif.unreadCount" label="Đánh dấu đã đọc tất cả" icon="pi pi-check" size="small" text @click="notif.markAllRead()" />
  </div>

  <div v-if="notif.items.length" class="wrap">
    <div class="list">
      <button v-for="n in paged" :key="n.id" class="item" :class="{ unread: !n.isRead }" @click="open(n)">
        <span class="ic" :class="n.type"><i class="pi" :class="n.type === 'promotion' ? 'pi-gift' : 'pi-shopping-bag'" /></span>
        <span class="body">
          <strong>{{ n.title }}</strong>
          <span class="msg">{{ n.message }}</span>
          <span class="time"><i class="pi pi-clock" /> {{ formatDate(n.createdAt) }}</span>
        </span>
        <span v-if="!n.isRead" class="dot" />
      </button>
    </div>
    <Paginator v-if="notif.items.length > pageSize" :first="first" :rows="pageSize" :totalRecords="notif.items.length"
      :rowsPerPageOptions="[8, 15, 30]" @page="onPage" class="pager" />
  </div>
  <div v-else class="empty">
    <i class="pi pi-bell" />
    <p>Bạn chưa có thông báo nào.</p>
  </div>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--sp-3); flex-wrap: wrap; gap: var(--sp-2); }
.badge-new { font-size: 12px; font-weight: 700; color: #fff; background: var(--brand); padding: 2px 9px; border-radius: var(--radius-pill); vertical-align: middle; margin-left: 6px; }

/* Danh sách gọn, cuộn trong khung — không kéo dài vô tận */
.list { display: flex; flex-direction: column; gap: var(--sp-2); max-height: calc(100vh - 230px); overflow-y: auto; padding-right: 4px; }
.list::-webkit-scrollbar { width: 7px; }
.list::-webkit-scrollbar-thumb { background: var(--border-strong); border-radius: var(--radius-pill); }

.item {
  display: flex; align-items: center; gap: var(--sp-3);
  background: var(--surface); border: 1px solid var(--border); border-left: 3px solid transparent;
  border-radius: var(--radius); padding: var(--sp-3); cursor: pointer; font-family: inherit; text-align: left;
  transition: border-color var(--ease), background var(--ease), box-shadow var(--ease);
}
.item:hover { box-shadow: var(--shadow); }
/* Chưa đọc: nổi bật rõ (nền cam nhạt + gạch cam trái + tiêu đề đậm cam) */
.item.unread { background: var(--brand-50); border-color: var(--brand-100); border-left-color: var(--brand); }
.item.unread strong { color: var(--brand-dark); }

.ic { width: 36px; height: 36px; border-radius: 50%; display: grid; place-items: center; background: var(--brand-100); color: var(--brand); flex-shrink: 0; }
.ic .pi { font-size: 15px; }
.ic.promotion { background: #fff4d6; color: #d97706; }
.body { display: flex; flex-direction: column; gap: 2px; flex: 1; min-width: 0; }
.body strong { font-size: 14px; }
.msg { color: var(--text-2); font-size: 13px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.time { color: var(--text-muted); font-size: 11.5px; display: inline-flex; align-items: center; gap: 4px; }
.time .pi { font-size: 10px; }
.dot { width: 9px; height: 9px; border-radius: 50%; background: var(--brand); flex-shrink: 0; }

.pager { margin-top: var(--sp-3); }
.empty { text-align: center; padding: var(--sp-8) var(--sp-4); display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.empty p { margin: 0; color: var(--text-muted); }
</style>
