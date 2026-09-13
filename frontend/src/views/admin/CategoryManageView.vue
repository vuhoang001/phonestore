<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import { categoryApi } from '@/services'
import type { Category } from '@/types'
import { extractError } from '@/services/api'

const toast = useToast()
const confirm = useConfirm()
const categories = ref<Category[]>([])
const loading = ref(true)
const showDialog = ref(false)
const form = ref<{ id?: number; name: string; parentId?: number; imageUrl?: string }>({ name: '' })

async function load() {
  loading.value = true
  try { categories.value = await categoryApi.all() } finally { loading.value = false }
}

function openNew() { form.value = { name: '' }; showDialog.value = true }
function openEdit(c: Category) { form.value = { id: c.id, name: c.name, parentId: c.parentId, imageUrl: c.imageUrl }; showDialog.value = true }

async function save() {
  try {
    const payload = { name: form.value.name, parentId: form.value.parentId, imageUrl: form.value.imageUrl }
    if (form.value.id) await categoryApi.update(form.value.id, payload)
    else await categoryApi.create(payload)
    showDialog.value = false
    await load()
    toast.add({ severity: 'success', summary: 'Đã lưu', life: 2000 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  }
}

function remove(c: Category) {
  confirm.require({
    message: `Xóa danh mục "${c.name}"?`, header: 'Xác nhận', icon: 'pi pi-exclamation-triangle',
    accept: async () => {
      try { await categoryApi.remove(c.id); await load() }
      catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
    }
  })
}

function parentName(id?: number) { return id ? categories.value.find((c) => c.id === id)?.name || '—' : '—' }

onMounted(load)
</script>

<template>
  <div class="head">
    <h1>Quản lý danh mục</h1>
    <Button label="Thêm danh mục" icon="pi pi-plus" size="small" @click="openNew" />
  </div>

  <DataTable :value="categories" :loading="loading" stripedRows size="small" class="box"
    paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
    <Column field="id" header="ID" style="width: 60px" />
    <Column field="name" header="Tên" />
    <Column field="slug" header="Slug" />
    <Column header="Danh mục cha"><template #body="{ data }">{{ parentName(data.parentId) }}</template></Column>
    <Column header="Thao tác" style="width: 120px">
      <template #body="{ data }">
        <Button icon="pi pi-pencil" text size="small" @click="openEdit(data)" />
        <Button icon="pi pi-trash" text size="small" severity="danger" @click="remove(data)" />
      </template>
    </Column>
    <template #empty>
      <div class="empty">
        <i class="pi pi-sitemap" />
        <p>Chưa có danh mục nào.</p>
        <Button label="Thêm danh mục" icon="pi pi-plus" size="small" @click="openNew" />
      </div>
    </template>
  </DataTable>

  <Dialog v-model:visible="showDialog" :header="form.id ? 'Sửa danh mục' : 'Thêm danh mục'" modal style="width: 440px">
    <div class="form">
      <label>Tên danh mục</label>
      <InputText v-model="form.name" class="w-full" />
      <label>Danh mục cha (tùy chọn)</label>
      <Select v-model="form.parentId" :options="[{ label: '— Không —', value: undefined }, ...categories.map(c => ({ label: c.name, value: c.id }))]"
        optionLabel="label" optionValue="value" class="w-full" />
      <label>Ảnh URL (tùy chọn)</label>
      <InputText v-model="form.imageUrl" class="w-full" />
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
</style>
