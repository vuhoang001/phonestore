<script setup lang="ts">
// Mã đơn trong báo cáo → mở chi tiết đơn ở trang quản lý đơn (dùng ?open=<id> có sẵn).
import { useRouter } from 'vue-router'

const props = defineProps<{ id?: number; code: string }>()
const router = useRouter()

function open() {
  if (!props.id) return
  router.push({ name: 'admin-orders', query: { open: String(props.id) } })
}
</script>

<template>
  <a v-if="id" class="rp-link" role="button" tabindex="0" @click="open" @keydown.enter="open">
    {{ code }}<i class="pi pi-external-link" />
  </a>
  <span v-else>{{ code }}</span>
</template>
