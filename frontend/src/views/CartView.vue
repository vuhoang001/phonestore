<script setup lang="ts">
import { onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import InputNumber from 'primevue/inputnumber'
import Checkbox from 'primevue/checkbox'
import { useCartStore } from '@/stores/cart'
import { formatCurrency } from '@/composables/format'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import { extractError } from '@/services/api'

const cart = useCartStore()
const router = useRouter()
const toast = useToast()
const confirm = useConfirm()
const placeholder = 'https://placehold.co/80x80?text=%20'

const allIds = computed(() => cart.cart?.items.map((i) => i.id) ?? [])
const allSelected = computed({
  get: () => allIds.value.length > 0 && cart.selectedIds.length === allIds.value.length,
  set: (v: boolean) => { cart.selectedIds = v ? [...allIds.value] : [] }
})

async function changeQty(itemId: number, qty: number) {
  try {
    await cart.updateItem(itemId, qty)
  } catch (e) {
    toast.add({ severity: 'warn', summary: 'Lỗi', detail: extractError(e), life: 3000 })
    cart.fetch()
  }
}

async function remove(itemId: number) {
  await cart.removeItem(itemId)
  cart.selectedIds = cart.selectedIds.filter((id) => id !== itemId)
}

// Xóa tất cả sản phẩm đang được chọn (có xác nhận)
function removeSelected() {
  const ids = [...cart.selectedIds]
  if (!ids.length) { toast.add({ severity: 'warn', summary: 'Chưa chọn sản phẩm nào', life: 2000 }); return }
  confirm.require({
    message: `Xóa ${ids.length} sản phẩm đã chọn khỏi giỏ hàng?`,
    header: 'Xác nhận xóa', icon: 'pi pi-trash', acceptLabel: 'Xóa', rejectLabel: 'Không',
    acceptClass: 'p-button-danger',
    accept: async () => {
      try {
        await cart.removeMany(ids)
        toast.add({ severity: 'success', summary: `Đã xóa ${ids.length} sản phẩm`, life: 2000 })
      } catch (e) {
        toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
        cart.fetch()
      }
    }
  })
}

function checkout() {
  if (cart.selectedItems.length === 0) {
    toast.add({ severity: 'warn', summary: 'Chưa chọn sản phẩm nào', life: 2500 })
    return
  }
  router.push('/checkout')
}

onMounted(async () => {
  await cart.fetch()
  cart.selectedIds = allIds.value // mặc định chọn tất cả
})
</script>

<template>
  <h1 class="section-title">Giỏ hàng</h1>

  <div v-if="cart.cart && cart.cart.items.length" class="cart-layout">
    <div class="items">
      <div class="list-head">
        <Checkbox v-model="allSelected" :binary="true" />
        <span>Chọn tất cả ({{ cart.cart.items.length }})</span>
        <Button v-if="cart.selectedIds.length" class="del-sel" :label="`Xóa (${cart.selectedIds.length})`"
          icon="pi pi-trash" text severity="danger" size="small" @click="removeSelected" />
      </div>

      <div class="item-scroll">
        <div v-for="item in cart.cart.items" :key="item.id" class="cart-item"
          :class="{ picked: cart.selectedIds.includes(item.id) }">
          <Checkbox class="chk" v-model="cart.selectedIds" :value="item.id" />
          <img :src="item.imageUrl || placeholder" :alt="item.productName"
            class="thumb cursor-pointer" @click="router.push(`/products/${item.productId}`)" />
          <div class="meta">
            <strong class="pname" @click="router.push(`/products/${item.productId}`)">{{ item.productName }}</strong>
            <span v-if="item.variantInfo" class="text-muted">{{ item.variantInfo }}</span>
            <span class="price">{{ formatCurrency(item.price) }}</span>
          </div>
          <InputNumber class="qty" :modelValue="item.quantity" :min="1" :max="item.stockQuantity" showButtons
            buttonLayout="horizontal" :inputStyle="{ width: '2.4rem' }" @update:modelValue="(v) => changeQty(item.id, v)" />
          <div class="line-total">{{ formatCurrency(item.lineTotal) }}</div>
          <Button class="del" icon="pi pi-trash" text severity="danger" size="small" @click="remove(item.id)" />
        </div>
      </div>
    </div>

    <aside class="summary">
      <h3>Tóm tắt đơn</h3>
      <div class="row"><span>Đã chọn</span><span>{{ cart.selectedCount }} sản phẩm</span></div>
      <div class="row"><span>Tạm tính</span><span>{{ formatCurrency(cart.selectedSubTotal) }}</span></div>
      <div class="row text-muted"><span>Phí ship</span><span>Tính ở bước sau</span></div>
      <hr />
      <div class="row total"><span>Tổng</span><span class="price">{{ formatCurrency(cart.selectedSubTotal) }}</span></div>
      <Button :label="`Mua hàng (${cart.selectedItems.length})`" icon="pi pi-arrow-right"
        class="w-full mt-3" :disabled="cart.selectedItems.length === 0" @click="checkout" />
    </aside>
  </div>

  <div v-else class="empty">
    <i class="pi pi-shopping-cart" style="font-size: 3rem; color: #d1d5db" />
    <p>Giỏ hàng của bạn đang trống.</p>
    <Button label="Tiếp tục mua sắm" @click="router.push('/products')" />
  </div>
</template>

<style scoped>
.cart-layout { display: grid; grid-template-columns: 1fr 320px; gap: var(--sp-4); align-items: start; }
/* Gom cả danh sách vào 1 thẻ gọn; các dòng ngăn nhau bằng đường kẻ mảnh */
.items { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); overflow: hidden; }
.list-head { display: flex; align-items: center; gap: var(--sp-3); padding: var(--sp-2) var(--sp-4); border-bottom: 1px solid var(--border); font-weight: 600; font-size: 14px; }
.del-sel { margin-left: auto; }
/* Vùng cuộn dài lấp gần hết màn hình rồi mới cuộn (bớt khoảng trống trên) */
.item-scroll { max-height: calc(100vh - 200px); overflow-y: auto; }
.item-scroll::-webkit-scrollbar { width: 8px; }
.item-scroll::-webkit-scrollbar-thumb { background: var(--border-strong); border-radius: var(--radius-pill); }
.item-scroll::-webkit-scrollbar-track { background: transparent; }
/* Cột cố định cho SL & giá → thẳng hàng dọc, không xộc xệch */
.cart-item {
  display: grid; grid-template-columns: auto 48px 1fr 116px 108px 36px; gap: var(--sp-3); align-items: center;
  padding: var(--sp-3) var(--sp-4); border-bottom: 1px solid var(--border); transition: background var(--ease);
}
.cart-item:last-child { border-bottom: none; }
.cart-item.picked { background: var(--brand-50); }
.cart-item img { width: 48px; height: 48px; object-fit: cover; border-radius: var(--radius-sm); }
.meta { display: flex; flex-direction: column; gap: 1px; min-width: 0; }
.pname { font-size: 13.5px; cursor: pointer; overflow: hidden; text-overflow: ellipsis; display: -webkit-box; -webkit-line-clamp: 1; -webkit-box-orient: vertical; }
.meta .text-muted { font-size: 11.5px; }
.meta .price { font-size: 12.5px; color: var(--text-2); }
/* Ô chọn số lượng: căn giữa cột, cỡ đồng nhất mọi dòng */
.qty { justify-self: center; }
.qty :deep(.p-inputnumber-button) { width: 1.9rem; }
.line-total { font-weight: 700; text-align: right; color: var(--price); font-size: 14px; }
.summary { background: var(--surface); padding: var(--sp-5); border-radius: var(--radius-lg); position: sticky; top: 84px; }
.summary .row { display: flex; justify-content: space-between; margin: var(--sp-2) 0; }
.summary .total { font-size: 1.15rem; font-weight: 700; }
.empty { text-align: center; padding: 4rem; display: flex; flex-direction: column; align-items: center; gap: var(--sp-4); }
@media (max-width: 768px) {
  .cart-layout { grid-template-columns: 1fr; }
  .item-scroll { max-height: 62vh; }
  /* Bố cục 2 hàng gọn: (chọn · ảnh · tên · xóa) rồi (số lượng · thành tiền) */
  .cart-item {
    grid-template-columns: auto 48px 1fr auto;
    grid-template-areas: "chk img meta del" "chk img qty total";
    row-gap: var(--sp-2); column-gap: var(--sp-3);
  }
  .cart-item .chk { grid-area: chk; }
  .cart-item .thumb { grid-area: img; align-self: start; }
  .cart-item .meta { grid-area: meta; }
  .cart-item .qty { grid-area: qty; justify-self: start; }
  .cart-item .line-total { grid-area: total; align-self: center; }
  .cart-item .del { grid-area: del; justify-self: end; }
}
</style>
