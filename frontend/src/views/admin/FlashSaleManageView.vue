<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
import Select from 'primevue/select'
import DatePicker from 'primevue/datepicker'
import ToggleSwitch from 'primevue/toggleswitch'
import Tag from 'primevue/tag'
import { flashSaleApi, productApi } from '@/services'
import type { FlashSale, ProductListItem } from '@/types'
import { formatDate } from '@/composables/format'
import { extractError } from '@/services/api'

const toast = useToast()
const confirm = useConfirm()

const sales = ref<FlashSale[]>([])
const loading = ref(true)
const showDialog = ref(false)
const saving = ref(false)
const editingId = ref<number | null>(null)

// Danh sách sản phẩm để chọn trong form (nạp 1 lần khi mở dialog)
const products = ref<ProductListItem[]>([])

interface FormItem { productId: number | null; flashPrice: number | null; quantityLimit: number | null }
interface Form { name: string; startAt: Date; endAt: Date; isActive: boolean; items: FormItem[] }

const form = ref<Form>(blank())
function blank(): Form {
  const start = new Date()
  const end = new Date(); end.setDate(end.getDate() + 1)
  return { name: '', startAt: start, endAt: end, isActive: true, items: [emptyItem()] }
}
function emptyItem(): FormItem { return { productId: null, flashPrice: null, quantityLimit: 100 } }

// ----- Trạng thái hiển thị của 1 phiên -----
function statusLabel(s: FlashSale): string {
  if (!s.isActive) return 'Đã tắt'
  if (s.isRunning) return 'Đang chạy'
  return new Date(s.endAt).getTime() < Date.now() ? 'Hết hạn' : 'Sắp diễn ra'
}
function statusSeverity(s: FlashSale): string {
  if (!s.isActive) return 'warn'
  if (s.isRunning) return 'success'
  return 'secondary'
}

async function load() {
  loading.value = true
  try { sales.value = await flashSaleApi.all() }
  catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { loading.value = false }
}

async function ensureProducts() {
  if (products.value.length) return
  try {
    // pageSize tối đa backend cho phép là 100 (xin >100 sẽ bị rớt về mặc định 12).
    // Lấy đủ mọi trang để dropdown luôn có sản phẩm đã gắn trong Flash Sale khi Sửa.
    const first = await productApi.search({ page: 1, pageSize: 100 })
    const all = [...first.items]
    for (let p = 2; p <= (first.totalPages || 1); p++) {
      const res = await productApi.search({ page: p, pageSize: 100 })
      all.push(...res.items)
    }
    products.value = all
  } catch { /* để trống nếu lỗi, Select sẽ rỗng */ }
}

async function openNew() {
  editingId.value = null
  form.value = blank()
  await ensureProducts()
  showDialog.value = true
}

async function openEdit(s: FlashSale) {
  editingId.value = s.id
  form.value = {
    name: s.name,
    startAt: new Date(s.startAt),
    endAt: new Date(s.endAt),
    isActive: s.isActive,
    items: s.items.length
      ? s.items.map((it) => ({ productId: it.productId, flashPrice: it.flashPrice, quantityLimit: it.quantityLimit }))
      : [emptyItem()]
  }
  await ensureProducts()
  showDialog.value = true
}

function addItem() { form.value.items.push(emptyItem()) }
function removeItem(i: number) {
  form.value.items.splice(i, 1)
  if (!form.value.items.length) form.value.items.push(emptyItem())
}

async function save() {
  // Kiểm tra tối thiểu
  if (!form.value.name.trim()) {
    toast.add({ severity: 'warn', summary: 'Thiếu tên', detail: 'Nhập tên Flash Sale', life: 2500 }); return
  }
  const items = form.value.items.filter((it) => it.productId && it.flashPrice != null)
  if (!items.length) {
    toast.add({ severity: 'warn', summary: 'Thiếu sản phẩm', detail: 'Thêm ít nhất 1 sản phẩm', life: 2500 }); return
  }
  const payload = {
    name: form.value.name.trim(),
    startAt: form.value.startAt.toISOString(),
    endAt: form.value.endAt.toISOString(),
    isActive: form.value.isActive,
    items: items.map((it) => ({
      productId: it.productId,
      flashPrice: it.flashPrice,
      quantityLimit: it.quantityLimit ?? 0
    }))
  }
  saving.value = true
  try {
    if (editingId.value) {
      await flashSaleApi.update(editingId.value, payload)
      toast.add({ severity: 'success', summary: 'Đã cập nhật Flash Sale', life: 2000 })
    } else {
      await flashSaleApi.create(payload)
      toast.add({ severity: 'success', summary: 'Đã tạo Flash Sale', life: 2000 })
    }
    showDialog.value = false
    await load()
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    saving.value = false
  }
}

function remove(s: FlashSale) {
  confirm.require({
    message: `Xóa Flash Sale "${s.name}"?`, header: 'Xác nhận', icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Hủy', acceptLabel: 'Xóa', acceptClass: 'p-button-danger',
    accept: async () => {
      try {
        await flashSaleApi.remove(s.id)
        await load()
        toast.add({ severity: 'success', summary: 'Đã xóa', life: 2000 })
      } catch (e) {
        toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
      }
    }
  })
}

onMounted(load)
</script>

<template>
  <div class="head">
    <h1>Quản lý Flash Sale</h1>
    <Button label="Tạo Flash Sale" icon="pi pi-bolt" size="small" @click="openNew" />
  </div>

  <DataTable :value="sales" :loading="loading" stripedRows size="small" class="box" rowHover
    paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
    <Column field="name" header="Tên" />
    <Column header="Khung giờ">
      <template #body="{ data }">{{ formatDate(data.startAt) }} → {{ formatDate(data.endAt) }}</template>
    </Column>
    <Column header="Trạng thái">
      <template #body="{ data }">
        <Tag :value="statusLabel(data)" :severity="statusSeverity(data)" />
      </template>
    </Column>
    <Column header="Số sản phẩm">
      <template #body="{ data }">{{ data.items.length }}</template>
    </Column>
    <Column header="" style="width: 110px">
      <template #body="{ data }">
        <div class="actions">
          <Button icon="pi pi-pencil" text size="small" @click="openEdit(data)" />
          <Button icon="pi pi-trash" text size="small" severity="danger" @click="remove(data)" />
        </div>
      </template>
    </Column>
    <template #empty>
      <div class="empty">
        <i class="pi pi-bolt" />
        <p>Chưa có phiên Flash Sale nào.</p>
        <Button label="Tạo Flash Sale" icon="pi pi-bolt" size="small" @click="openNew" />
      </div>
    </template>
  </DataTable>

  <Dialog v-model:visible="showDialog" :header="editingId ? 'Sửa Flash Sale' : 'Tạo Flash Sale'" modal
    :style="{ width: '820px' }">
    <div class="form">
      <label>Tên phiên</label>
      <InputText v-model="form.name" class="w-full" placeholder="VD: Flash Sale Cuối Tuần" />

      <div class="two">
        <div><label>Bắt đầu</label>
          <DatePicker v-model="form.startAt" showTime hourFormat="24" dateFormat="dd/mm/yy" class="w-full" />
        </div>
        <div><label>Kết thúc</label>
          <DatePicker v-model="form.endAt" showTime hourFormat="24" dateFormat="dd/mm/yy" class="w-full" />
        </div>
      </div>

      <div class="switch-row">
        <ToggleSwitch v-model="form.isActive" inputId="fs-active" />
        <label for="fs-active" class="switch-label">Đang bật</label>
      </div>

      <div class="items-head">
        <label>Sản phẩm áp dụng</label>
        <Button label="Thêm sản phẩm" icon="pi pi-plus" text size="small" @click="addItem" />
      </div>

      <div class="items-table">
        <div class="it-head">
          <span>Sản phẩm</span><span>Giá flash</span><span>Suất</span><span></span>
        </div>
        <div v-for="(item, i) in form.items" :key="i" class="item-row">
          <Select v-model="item.productId" :options="products" optionLabel="name" optionValue="id"
            filter placeholder="Chọn sản phẩm" class="cell-select" />
          <InputNumber v-model="item.flashPrice" :min="0" placeholder="Giá flash" mode="currency"
            currency="VND" locale="vi-VN" class="cell-num" inputClass="w-full" />
          <InputNumber v-model="item.quantityLimit" :min="0" placeholder="Suất" class="cell-num" inputClass="w-full" />
          <Button icon="pi pi-times" text severity="danger" class="cell-del" @click="removeItem(i)" />
        </div>
        <div v-if="!form.items.length" class="it-empty">Chưa có sản phẩm — bấm "Thêm sản phẩm" để thêm.</div>
      </div>
    </div>
    <template #footer>
      <Button label="Hủy" text @click="showDialog = false" />
      <Button label="Lưu" :loading="saving" @click="save" />
    </template>
  </Dialog>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; gap: var(--sp-3); margin-bottom: var(--sp-4); flex-wrap: wrap; }
.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); }
.actions { display: flex; gap: var(--sp-1); }
.empty { display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); padding: var(--sp-8) var(--sp-4); color: var(--text-muted); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.empty p { margin: 0; }

.form { display: flex; flex-direction: column; gap: var(--sp-1); }
.form label { font-weight: 600; font-size: 0.85rem; margin-top: var(--sp-2); }
.two { display: grid; grid-template-columns: 1fr 1fr; gap: var(--sp-4); }

.switch-row { display: flex; align-items: center; gap: var(--sp-3); margin-top: var(--sp-3); }
.switch-label { margin-top: 0 !important; }

.items-head { display: flex; align-items: center; justify-content: space-between; margin-top: var(--sp-3); }

/* Bảng sản phẩm: tiêu đề cột + hàng canh thẳng hàng tuyệt đối */
.items-table { border: 1px solid var(--border); border-radius: var(--radius); overflow: hidden; margin-top: var(--sp-2); }
.it-head, .item-row { display: grid; grid-template-columns: 1fr 160px 96px 40px; gap: var(--sp-3); align-items: center; }
.it-head { background: var(--surface-2); padding: 10px var(--sp-4); font-size: 11px; font-weight: 600; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.02em; }
.item-row { padding: 10px var(--sp-4); border-top: 1px solid var(--border); }
/* CHỐT lệch cột: cho phép ô co lại, không nới theo nội dung dài */
.item-row > * { min-width: 0; }
.cell-select, .cell-num { width: 100%; }
/* Tên sản phẩm dài -> cắt bằng "…" thay vì đẩy rộng cột */
.cell-select :deep(.p-select-label) { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.cell-del { justify-self: center; }
.it-empty { padding: var(--sp-4); text-align: center; color: var(--text-muted); font-size: 13px; }

@media (max-width: 640px) {
  .two { grid-template-columns: 1fr; }
  .it-head, .item-row { grid-template-columns: 1fr 110px 66px 32px; gap: var(--sp-2); padding: 8px var(--sp-3); }
}
</style>
