<script setup lang="ts">
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
const router = useRouter()
</script>

<template>
  <div class="nf">
    <i class="pi pi-compass nf-icon"></i>
    <h1>404</h1>
    <p class="nf-desc">Trang bạn tìm không tồn tại hoặc đã được di chuyển.</p>
    <Button label="Về trang chủ" icon="pi pi-home" @click="router.push('/')" />
  </div>
</template>

<style scoped>
.nf {
  min-height: 70vh;
  display: flex; flex-direction: column;
  align-items: center; justify-content: center;
  gap: var(--sp-3);
  text-align: center;
  padding: var(--sp-6) var(--sp-4);
}
.nf-icon { font-size: 3rem; color: var(--border-strong); margin-bottom: var(--sp-2); }
.nf h1 { font-size: 4rem; margin: 0; color: var(--brand); letter-spacing: -0.03em; }
.nf-desc { margin: 0 0 var(--sp-2); color: var(--text-muted); max-width: 32ch; }
</style>
