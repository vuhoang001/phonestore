import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { Cart } from '@/types'
import { cartApi } from '@/services'

export const useCartStore = defineStore('cart', () => {
  const cart = ref<Cart | null>(null)
  const loading = ref(false)
  // Id các dòng giỏ đang được tick chọn để thanh toán (Shopee-style).
  const selectedIds = ref<number[]>([])

  const itemCount = computed(() => cart.value?.totalQuantity ?? 0)
  const subTotal = computed(() => cart.value?.subTotal ?? 0)

  // Các dòng đang được chọn (nếu chưa chọn gì thì coi như toàn bộ giỏ).
  const selectedItems = computed(() => {
    const items = cart.value?.items ?? []
    if (selectedIds.value.length === 0) return items
    return items.filter((i) => selectedIds.value.includes(i.id))
  })
  const selectedSubTotal = computed(() => selectedItems.value.reduce((s, i) => s + i.lineTotal, 0))
  const selectedCount = computed(() => selectedItems.value.reduce((s, i) => s + i.quantity, 0))

  // Giỏ hoạt động cho cả khách (guest token) lẫn user đã đăng nhập — backend tự phân biệt.
  async function fetch() {
    loading.value = true
    try {
      cart.value = await cartApi.get()
    } catch {
      cart.value = null
    } finally {
      loading.value = false
    }
  }

  // Xóa trạng thái giỏ khỏi bộ nhớ (khi đăng xuất) — không gọi API.
  function reset() {
    cart.value = null
    selectedIds.value = []
  }

  async function add(variantId: number, quantity = 1) {
    cart.value = await cartApi.add(variantId, quantity)
  }

  async function updateItem(itemId: number, quantity: number) {
    cart.value = await cartApi.update(itemId, quantity)
  }

  async function removeItem(itemId: number) {
    cart.value = await cartApi.remove(itemId)
  }

  async function clear() {
    await cartApi.clear()
    cart.value = null
  }

  // Xoá nhiều mục đã chọn (không có API bulk → gọi tuần tự, cart cập nhật dần)
  async function removeMany(ids: number[]) {
    for (const id of ids) cart.value = await cartApi.remove(id)
    selectedIds.value = selectedIds.value.filter((id) => !ids.includes(id))
  }

  return {
    cart, loading, selectedIds, itemCount, subTotal,
    selectedItems, selectedSubTotal, selectedCount,
    fetch, add, updateItem, removeItem, removeMany, clear, reset
  }
})
