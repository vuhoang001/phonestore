<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import ProgressSpinner from 'primevue/progressspinner'
import { wishlistApi } from '@/services'
import type { WishlistItem } from '@/types'
import { formatCurrency } from '@/composables/format'
import { useToast } from 'primevue/usetoast'

const router = useRouter()
const toast = useToast()
const items = ref<WishlistItem[]>([])
const loading = ref(true)
const placeholder = 'https://placehold.co/200x200?text=%20'

async function load() {
  loading.value = true
  try { items.value = await wishlistApi.mine() } finally { loading.value = false }
}

async function remove(productId: number) {
  await wishlistApi.toggle(productId)
  toast.add({ severity: 'success', summary: 'Đã bỏ khỏi yêu thích', life: 1500 })
  load()
}

onMounted(load)
</script>

<template>
  <h1 class="section-title">Sản phẩm yêu thích</h1>
  <div v-if="loading" class="center"><ProgressSpinner /></div>
  <div v-else-if="items.length" class="grid-products">
    <div v-for="w in items" :key="w.id" class="card">
      <img :src="w.primaryImage || placeholder" :alt="w.productName" @click="router.push(`/products/${w.slug}`)" />
      <div class="body">
        <strong class="name">{{ w.productName }}</strong>
        <span class="price">{{ formatCurrency(w.basePrice) }}</span>
        <div class="actions">
          <Button label="Xem" size="small" outlined @click="router.push(`/products/${w.slug}`)" />
          <Button icon="pi pi-trash" size="small" text severity="danger" @click="remove(w.productId)" />
        </div>
      </div>
    </div>
  </div>
  <div v-else class="empty">
    <i class="pi pi-heart" />
    <p>Chưa có sản phẩm yêu thích.</p>
    <Button label="Khám phá sản phẩm" @click="router.push('/products')" />
  </div>
</template>

<style scoped>
.center { display: flex; justify-content: center; padding: var(--sp-8); }
.empty { text-align: center; padding: var(--sp-8) var(--sp-4); display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.empty p { margin: 0; color: var(--text-muted); }
.card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  overflow: hidden;
  box-shadow: var(--shadow-sm);
  transition: transform var(--ease), box-shadow var(--ease), border-color var(--ease);
}
.card:hover { transform: translateY(-2px); box-shadow: var(--shadow-md); border-color: var(--brand-100); }
.card img { width: 100%; aspect-ratio: 1; object-fit: cover; cursor: pointer; }
.body { padding: var(--sp-3); display: flex; flex-direction: column; gap: var(--sp-2); }
.name { font-size: 0.95rem; }
.actions { display: flex; justify-content: space-between; align-items: center; }
</style>
