<script setup lang="ts">
// CRUD thương hiệu điện thoại (Apple, Samsung, Xiaomi...).
// Slug tự sinh từ tên (không dấu, gạch nối) — vẫn cho phép sửa tay.
import { ref, watch, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
import Textarea from 'primevue/textarea'
import FileUpload from 'primevue/fileupload'
import { brandsApi, uploadApi } from '@/services'
import type { Brand } from '@/types'
import { extractError } from '@/services/api'

const toast = useToast()
const confirm = useConfirm()

const brands = ref<Brand[]>([])
const loading = ref(true)
const showDialog = ref(false)
const saving = ref(false)
const uploading = ref(false)

// Form thương hiệu — có thêm sortOrder (BrandDto ở backend nhận field này).
interface BrandForm {
  id?: number; name: string; slug: string; logoUrl?: string; description?: string; sortOrder: number
}
const form = ref<BrandForm>(blank())
function blank(): BrandForm { return { name: '', slug: '', logoUrl: '', description: '', sortOrder: 0 } }

// Đánh dấu người dùng đã sửa slug tay -> ngừng tự sinh theo tên.
const slugTouched = ref(false)

/** Chuyển tên có dấu tiếng Việt thành slug: bỏ dấu, thường hoá, thay khoảng trắng bằng "-". */
function slugify(s: string): string {
  return s
    .normalize('NFD').replace(/[̀-ͯ]/g, '')
    .replace(/đ/g, 'd').replace(/Đ/g, 'D')
    .toLowerCase().trim()
    .replace(/[^a-z0-9\s-]/g, '')
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-')
}

// Tự sinh slug khi gõ tên (nếu chưa sửa slug tay).
watch(() => form.value.name, (name) => {
  if (!slugTouched.value) form.value.slug = slugify(name)
})

async function load() {
  loading.value = true
  try { brands.value = await brandsApi.list() }
  catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
  finally { loading.value = false }
}

function openNew() { form.value = blank(); slugTouched.value = false; showDialog.value = true }
function openEdit(b: Brand) {
  form.value = {
    id: b.id, name: b.name, slug: b.slug, logoUrl: b.logoUrl || '',
    description: b.description || '', sortOrder: (b as Brand & { sortOrder?: number }).sortOrder ?? 0
  }
  slugTouched.value = true // giữ nguyên slug hiện có khi sửa
  showDialog.value = true
}

// Upload logo từ máy -> nhận URL.
async function onUploadLogo(e: { files: File | File[] }) {
  const file = Array.isArray(e.files) ? e.files[0] : e.files
  if (!file) return
  uploading.value = true
  try {
    const { url } = await uploadApi.image(file)
    form.value.logoUrl = url
    toast.add({ severity: 'success', summary: 'Đã tải logo', life: 1800 })
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Lỗi tải logo', detail: extractError(err), life: 3000 })
  } finally { uploading.value = false }
}

async function save() {
  if (!form.value.name.trim()) { toast.add({ severity: 'warn', summary: 'Nhập tên thương hiệu', life: 2500 }); return }
  saving.value = true
  try {
    const payload = {
      name: form.value.name.trim(),
      slug: form.value.slug.trim() || slugify(form.value.name),
      logoUrl: form.value.logoUrl || undefined,
      description: form.value.description || undefined,
      sortOrder: form.value.sortOrder
    } as Partial<Brand>
    if (form.value.id) await brandsApi.update(form.value.id, payload)
    else await brandsApi.create(payload)
    showDialog.value = false
    await load()
    toast.add({ severity: 'success', summary: 'Đã lưu thương hiệu', life: 2000 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally { saving.value = false }
}

function remove(b: Brand) {
  confirm.require({
    message: `Xóa thương hiệu "${b.name}"?`, header: 'Xác nhận', icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Hủy', acceptLabel: 'Xóa', acceptClass: 'p-button-danger',
    accept: async () => {
      try { await brandsApi.remove(b.id); await load(); toast.add({ severity: 'success', summary: 'Đã xóa', life: 2000 }) }
      catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
    }
  })
}

onMounted(load)
</script>

<template>
  <div class="head">
    <h1>Quản lý thương hiệu</h1>
    <Button label="Thêm thương hiệu" icon="pi pi-plus" size="small" @click="openNew" />
  </div>

  <DataTable :value="brands" :loading="loading" stripedRows size="small" class="box"
    paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]">
    <Column field="id" header="ID" style="width: 60px" />
    <Column header="Logo" style="width: 80px">
      <template #body="{ data }">
        <img :src="data.logoUrl || 'https://placehold.co/48?text=?'" class="logo" alt="" />
      </template>
    </Column>
    <Column field="name" header="Tên" />
    <Column field="slug" header="Slug" />
    <Column header="Thứ tự" style="width: 90px">
      <template #body="{ data }">{{ data.sortOrder ?? 0 }}</template>
    </Column>
    <Column header="Thao tác" style="width: 120px">
      <template #body="{ data }">
        <Button icon="pi pi-pencil" text size="small" @click="openEdit(data)" />
        <Button icon="pi pi-trash" text size="small" severity="danger" @click="remove(data)" />
      </template>
    </Column>
    <template #empty>
      <div class="empty">
        <i class="pi pi-tags" />
        <p>Chưa có thương hiệu nào.</p>
        <Button label="Thêm thương hiệu" icon="pi pi-plus" size="small" @click="openNew" />
      </div>
    </template>
  </DataTable>

  <Dialog v-model:visible="showDialog" :header="form.id ? 'Sửa thương hiệu' : 'Thêm thương hiệu'" modal style="width: 520px">
    <div class="form">
      <label>Tên thương hiệu</label>
      <InputText v-model="form.name" class="w-full" placeholder="VD: Apple, Samsung, Xiaomi" />
      <label>Slug (đường dẫn)</label>
      <InputText v-model="form.slug" class="w-full" placeholder="tu-sinh-tu-ten" @input="slugTouched = true" />
      <small class="hint">Tự sinh từ tên — có thể sửa tay.</small>

      <label>Logo</label>
      <div class="logo-row">
        <img :src="form.logoUrl || 'https://placehold.co/64?text=?'" class="logo-preview" alt="" />
        <div class="logo-actions">
          <FileUpload mode="basic" customUpload auto accept="image/*" :maxFileSize="5000000"
            chooseLabel="Tải logo" chooseIcon="pi pi-upload" :disabled="uploading" @uploader="onUploadLogo" />
          <InputText v-model="form.logoUrl" class="w-full" placeholder="hoặc dán URL logo" />
        </div>
      </div>

      <label>Mô tả (tùy chọn)</label>
      <Textarea v-model="form.description" rows="3" class="w-full" autoResize placeholder="Giới thiệu ngắn về thương hiệu..." />

      <label>Thứ tự hiển thị</label>
      <InputNumber v-model="form.sortOrder" :min="0" class="w-full" inputClass="w-full" />
    </div>
    <template #footer>
      <Button label="Hủy" text @click="showDialog = false" />
      <Button label="Lưu" icon="pi pi-check" :loading="saving" @click="save" />
    </template>
  </Dialog>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; gap: var(--sp-3); margin-bottom: var(--sp-4); flex-wrap: wrap; }
.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); }
.logo { width: 48px; height: 48px; object-fit: contain; border-radius: var(--radius-sm); background: var(--surface-2); border: 1px solid var(--border); }
.empty { display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); padding: var(--sp-8) var(--sp-4); color: var(--text-muted); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.empty p { margin: 0; }
.form { display: flex; flex-direction: column; gap: var(--sp-1); }
.form label { font-weight: 600; font-size: 0.85rem; margin-top: var(--sp-2); }
.hint { color: var(--text-muted); font-size: 12px; }
.logo-row { display: flex; gap: var(--sp-3); align-items: flex-start; }
.logo-preview { width: 64px; height: 64px; object-fit: contain; border-radius: var(--radius); background: var(--surface-2); border: 1px solid var(--border); flex-shrink: 0; }
.logo-actions { flex: 1; display: flex; flex-direction: column; gap: var(--sp-2); min-width: 0; }
</style>
