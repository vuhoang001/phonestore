// Store So sánh máy: giữ danh sách sản phẩm khách chọn để đối chiếu (tối đa 4 máy).
// Lưu localStorage để không mất khi chuyển trang / F5. Chỉ giữ thông tin tối thiểu đủ
// render thanh so sánh; trang /compare sẽ tải chi tiết đầy đủ theo id khi cần.
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { ProductListItem } from '@/types'

// Thông tin tối thiểu của 1 máy trong danh sách so sánh.
export interface CompareEntry {
  id: number
  name: string
  slug: string
  image?: string
}

const STORAGE_KEY = 'compare-items'
const MAX = 4 // giới hạn số máy so sánh cùng lúc (bảng đọc được, khớp mốc responsive)

function load(): CompareEntry[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? (JSON.parse(raw) as CompareEntry[]) : []
  } catch {
    return []
  }
}

export const useCompareStore = defineStore('compare', () => {
  const items = ref<CompareEntry[]>(load())

  const count = computed(() => items.value.length)
  const isFull = computed(() => items.value.length >= MAX)
  const ids = computed(() => items.value.map((i) => i.id))

  function persist() {
    try { localStorage.setItem(STORAGE_KEY, JSON.stringify(items.value)) } catch { /* bỏ qua nếu đầy/không có quyền */ }
  }

  function has(id: number) {
    return items.value.some((i) => i.id === id)
  }

  // Thêm/bỏ 1 máy. Trả về trạng thái để nơi gọi hiện toast phù hợp.
  function toggle(p: ProductListItem): { added: boolean; full: boolean } {
    const existing = items.value.findIndex((i) => i.id === p.id)
    if (existing >= 0) {
      items.value.splice(existing, 1)
      persist()
      return { added: false, full: false }
    }
    if (items.value.length >= MAX) return { added: false, full: true }
    items.value.push({ id: p.id, name: p.name, slug: p.slug, image: p.primaryImage })
    persist()
    return { added: true, full: false }
  }

  function remove(id: number) {
    const i = items.value.findIndex((e) => e.id === id)
    if (i >= 0) { items.value.splice(i, 1); persist() }
  }

  function clear() {
    items.value = []
    persist()
  }

  return { items, count, isFull, ids, MAX, has, toggle, remove, clear }
})
