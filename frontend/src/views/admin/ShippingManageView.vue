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
import ToggleSwitch from 'primevue/toggleswitch'
import Tag from 'primevue/tag'
import { shippingApi } from '@/services'
import type { ShippingMethod } from '@/types'
import { formatCurrency } from '@/composables/format'
import { extractError } from '@/services/api'

const toast = useToast()
const confirm = useConfirm()
const methods = ref<ShippingMethod[]>([])
const loading = ref(true)
const showDialog = ref(false)
const saving = ref(false)

const form = ref<Partial<ShippingMethod>>(blank())
function blank(): Partial<ShippingMethod> {
  return { name: '', baseFee: 0, estimatedDays: 1, isActive: true }
}
const isEdit = ref(false)

async function load() {
  loading.value = true
  try { methods.value = await shippingApi.all() } finally { loading.value = false }
}

function openNew() { form.value = blank(); isEdit.value = false; showDialog.value = true }
function openEdit(m: ShippingMethod) { form.value = { ...m }; isEdit.value = true; showDialog.value = true }

async function save() {
  if (!form.value.name?.trim()) {
    toast.add({ severity: 'warn', summary: 'Vui lòng nhập tên phương thức', life: 2500 })
    return
  }
  saving.value = true
  try {
    const payload = {
      name: form.value.name,
      baseFee: form.value.baseFee ?? 0,
      estimatedDays: form.value.estimatedDays ?? 1,
      isActive: form.value.isActive ?? true
    }
    if (isEdit.value && form.value.id) await shippingApi.update(form.value.id, payload)
    else await shippingApi.create(payload)
    showDialog.value = false
    await load()
    toast.add({ severity: 'success', summary: isEdit.value ? 'Đã cập nhật' : 'Đã tạo phương thức', life: 2000 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally {
    saving.value = false
  }
}

// Bật/tắt nhanh ngay trên bảng.
async function toggle(m: ShippingMethod) {
  try {
    await shippingApi.update(m.id, { name: m.name, baseFee: m.baseFee, estimatedDays: m.estimatedDays, isActive: !m.isActive })
    await load()
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  }
}

function remove(m: ShippingMethod) {
  confirm.require({
    message: `Xóa phương thức "${m.name}"?`, header: 'Xác nhận', icon: 'pi pi-exclamation-triangle',
    accept: async () => {
      try { await shippingApi.remove(m.id); await load() }
      catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
    }
  })
}

onMounted(load)
</script>

<template>
  <div class="head">
    <h1>Quản lý vận chuyển</h1>
    <Button label="Thêm phương thức" icon="pi pi-plus" size="small" @click="openNew" />
  </div>

  <DataTable :value="methods" :loading="loading" stripedRows class="box"
    paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
    <Column field="name" header="Tên phương thức" />
    <Column header="Phí"><template #body="{ data }">{{ formatCurrency(data.baseFee) }}</template></Column>
    <Column header="Thời gian giao"><template #body="{ data }">{{ data.estimatedDays }} ngày</template></Column>
    <Column header="Trạng thái">
      <template #body="{ data }">
        <Tag :value="data.isActive ? 'Đang bật' : 'Đã tắt'" :severity="data.isActive ? 'success' : 'secondary'"
             style="cursor: pointer" @click="toggle(data)" />
      </template>
    </Column>
    <Column header="" style="width: 110px">
      <template #body="{ data }">
        <Button icon="pi pi-pencil" text size="small" @click="openEdit(data)" />
        <Button icon="pi pi-trash" text size="small" severity="danger" @click="remove(data)" />
      </template>
    </Column>
    <template #empty>
      <div class="empty">
        <i class="pi pi-truck" />
        <p>Chưa có phương thức vận chuyển nào.</p>
        <Button label="Thêm phương thức" icon="pi pi-plus" size="small" @click="openNew" />
      </div>
    </template>
  </DataTable>

  <Dialog v-model:visible="showDialog" :header="isEdit ? 'Sửa phương thức' : 'Thêm phương thức'" modal style="width: 440px">
    <div class="form">
      <label>Tên phương thức</label>
      <InputText v-model="form.name" class="w-full" placeholder="VD: Giao tiêu chuẩn" />
      <div class="two">
        <div><label>Phí (đ)</label><InputNumber v-model="form.baseFee" :min="0" class="w-full" inputClass="w-full" /></div>
        <div><label>Số ngày giao</label><InputNumber v-model="form.estimatedDays" :min="0" class="w-full" inputClass="w-full" /></div>
      </div>
      <label class="switch"><ToggleSwitch v-model="form.isActive" /> Kích hoạt (hiển thị ở trang thanh toán)</label>
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
.empty { display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); padding: var(--sp-8) var(--sp-4); color: var(--text-muted); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.empty p { margin: 0; }
.form { display: flex; flex-direction: column; gap: var(--sp-1); }
.form label { font-weight: 600; font-size: 0.85rem; margin-top: var(--sp-2); }
.two { display: grid; grid-template-columns: 1fr 1fr; gap: var(--sp-4); }
.switch { display: flex; align-items: center; gap: var(--sp-2); margin-top: var(--sp-3); }
@media (max-width: 640px) { .two { grid-template-columns: 1fr; } }
</style>
