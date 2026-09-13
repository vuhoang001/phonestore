<script setup lang="ts">
// Tên sản phẩm trong báo cáo → mở trang chi tiết sản phẩm (tab mới, giữ nguyên báo cáo).
// Report DTO chỉ có id + tên → tra slug qua API rồi điều hướng theo route /products/:slug.
import { ref } from 'vue'
import { productApi } from '@/services'

const props = defineProps<{ id?: number; name: string }>()
const loading = ref(false)

async function open() {
  if (!props.id || loading.value) return
  loading.value = true
  try {
    const p = await productApi.byId(props.id)
    window.open(`/products/${p.slug}`, '_blank')
  } catch {
    // SP có thể đã ẩn/xoá — mở danh sách sản phẩm để tìm.
    window.open('/products', '_blank')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <a v-if="id" class="rp-link" role="button" tabindex="0" @click="open" @keydown.enter="open">
    {{ name }}<i class="pi" :class="loading ? 'pi-spin pi-spinner' : 'pi-external-link'" />
  </a>
  <span v-else>{{ name }}</span>
</template>
