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
import Tag from 'primevue/tag'
import { couponApi } from '@/services'
import type { Coupon } from '@/types'
import { formatCurrency, formatDate } from '@/composables/format'
import { extractError } from '@/services/api'

const toast = useToast()
const confirm = useConfirm()
const coupons = ref<Coupon[]>([])
const loading = ref(true)
const showDialog = ref(false)

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const form = ref<any>(blank())
function blank() {
  const now = new Date()
  const end = new Date(); end.setMonth(end.getMonth() + 1)
  return { code: '', discountType: 'Percentage', discountValue: 10, minOrderAmount: 0, usageLimit: 100, startDate: now, endDate: end }
}

const typeOptions = [
  { label: 'Phần trăm (%)', value: 'Percentage' },
  { label: 'Số tiền cố định', value: 'FixedAmount' }
]

async function load() {
  loading.value = true
  try { coupons.value = await couponApi.all() } finally { loading.value = false }
}

function openNew() { form.value = blank(); showDialog.value = true }

async function save() {
  try {
    await couponApi.create({
      code: form.value.code, discountType: form.value.discountType,
      discountValue: form.value.discountValue, minOrderAmount: form.value.minOrderAmount,
      usageLimit: form.value.usageLimit,
      startDate: (form.value.startDate as Date).toISOString(),
      endDate: (form.value.endDate as Date).toISOString()
    })
    showDialog.value = false
    await load()
    toast.add({ severity: 'success', summary: 'Đã tạo mã', life: 2000 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  }
}

function remove(c: Coupon) {
  confirm.require({
    message: `Xóa mã "${c.code}"?`, header: 'Xác nhận', icon: 'pi pi-exclamation-triangle',
    accept: async () => { await couponApi.remove(c.id); await load() }
  })
}

onMounted(load)
</script>

<template>
  <div class="head">
    <h1>Quản lý mã giảm giá</h1>
    <Button label="Tạo mã" icon="pi pi-plus" size="small" @click="openNew" />
  </div>

  <DataTable :value="coupons" :loading="loading" stripedRows size="small" class="box"
    paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
    <Column field="code" header="Mã" />
    <Column header="Loại">
      <template #body="{ data }">{{ data.discountType === 'Percentage' ? 'Phần trăm' : 'Cố định' }}</template>
    </Column>
    <Column header="Giá trị">
      <template #body="{ data }">{{ data.discountType === 'Percentage' ? data.discountValue + '%' : formatCurrency(data.discountValue) }}</template>
    </Column>
    <Column header="Đơn tối thiểu"><template #body="{ data }">{{ formatCurrency(data.minOrderAmount) }}</template></Column>
    <Column header="Đã dùng"><template #body="{ data }">{{ data.usedCount }}/{{ data.usageLimit }}</template></Column>
    <Column header="Hạn dùng"><template #body="{ data }">{{ formatDate(data.endDate) }}</template></Column>
    <Column header="Trạng thái">
      <template #body="{ data }">
        <Tag :value="data.isActive ? 'Hoạt động' : 'Tắt'" :severity="data.isActive ? 'success' : 'secondary'" />
      </template>
    </Column>
    <Column header="" style="width: 60px">
      <template #body="{ data }"><Button icon="pi pi-trash" text size="small" severity="danger" @click="remove(data)" /></template>
    </Column>
    <template #empty>
      <div class="empty">
        <i class="pi pi-ticket" />
        <p>Chưa có mã giảm giá nào.</p>
        <Button label="Tạo mã" icon="pi pi-plus" size="small" @click="openNew" />
      </div>
    </template>
  </DataTable>

  <Dialog v-model:visible="showDialog" header="Tạo mã giảm giá" modal style="width: 480px">
    <div class="form">
      <label>Mã code</label>
      <InputText v-model="form.code" class="w-full" placeholder="VD: SALE20" />
      <label>Loại giảm giá</label>
      <Select v-model="form.discountType" :options="typeOptions" optionLabel="label" optionValue="value" class="w-full" />
      <div class="two">
        <div><label>Giá trị</label><InputNumber v-model="form.discountValue" :min="0" class="w-full" inputClass="w-full" /></div>
        <div><label>Đơn tối thiểu</label><InputNumber v-model="form.minOrderAmount" :min="0" class="w-full" inputClass="w-full" /></div>
      </div>
      <label>Giới hạn lượt dùng</label>
      <InputNumber v-model="form.usageLimit" :min="1" class="w-full" inputClass="w-full" />
      <div class="two">
        <div><label>Bắt đầu</label><DatePicker v-model="form.startDate" dateFormat="dd/mm/yy" class="w-full" /></div>
        <div><label>Kết thúc</label><DatePicker v-model="form.endDate" dateFormat="dd/mm/yy" class="w-full" /></div>
      </div>
    </div>
    <template #footer>
      <Button label="Hủy" text @click="showDialog = false" />
      <Button label="Lưu" @click="save" />
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
@media (max-width: 640px) { .two { grid-template-columns: 1fr; } }
</style>
