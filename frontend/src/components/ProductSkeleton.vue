<script setup lang="ts">
import Skeleton from 'primevue/skeleton'
defineProps<{ count?: number }>()
</script>

<template>
  <div v-for="n in (count || 10)" :key="n" class="sk-card">
    <Skeleton height="0" class="sk-img" />
    <div class="sk-body">
      <Skeleton width="100%" height="0.9rem" />
      <Skeleton width="70%" height="0.9rem" />
      <Skeleton width="45%" height="1.1rem" class="mt-1" />
      <div class="sk-foot">
        <Skeleton width="30%" height="0.7rem" />
        <Skeleton width="30%" height="0.7rem" />
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Khung khớp hình dạng ProductCard: ảnh vuông + 2 dòng tên + giá + footer */
.sk-card { background: var(--surface); border: 1px solid transparent; border-radius: var(--radius); overflow: hidden; display: flex; flex-direction: column; }
.sk-img { aspect-ratio: 1; width: 100%; border-radius: 0; }
.sk-body { padding: var(--sp-3); display: flex; flex-direction: column; gap: var(--sp-2); }
.sk-foot { display: flex; justify-content: space-between; margin-top: var(--sp-1); }
.mt-1 { margin-top: var(--sp-1); }
</style>
