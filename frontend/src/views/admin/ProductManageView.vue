<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
import Textarea from 'primevue/textarea'
import Select from 'primevue/select'
import ToggleSwitch from 'primevue/toggleswitch'
import Tag from 'primevue/tag'
import IconField from 'primevue/iconfield'
import InputIcon from 'primevue/inputicon'
import FileUpload from 'primevue/fileupload'
import Tabs from 'primevue/tabs'
import TabList from 'primevue/tablist'
import Tab from 'primevue/tab'
import TabPanels from 'primevue/tabpanels'
import TabPanel from 'primevue/tabpanel'
import { productApi, categoryApi, brandsApi, uploadApi } from '@/services'
import type { ProductListItem, Category, Brand, ProductSpec } from '@/types'
import { formatCurrency } from '@/composables/format'
import { extractError } from '@/services/api'

const toast = useToast()
const confirm = useConfirm()

const products = ref<ProductListItem[]>([])
const categories = ref<Category[]>([])
const brands = ref<Brand[]>([])
const loading = ref(true)
const showDialog = ref(false)
const saving = ref(false)
const activeTab = ref('info') // tab đang mở trong dialog

// Tổng tồn kho các biến thể (hiển thị tóm tắt trên tab)
const variantStock = computed(() => form.value.variants.reduce((s, v) => s + (v.stockQuantity || 0), 0))

// Bộ lọc admin
const search = ref('')
const filterCat = ref<number | undefined>(undefined)
const stockFilter = ref<'all' | 'in' | 'out'>('all')
const stockOptions = [
  { label: 'Tất cả tồn kho', value: 'all' },
  { label: 'Còn hàng', value: 'in' },
  { label: 'Hết hàng', value: 'out' }
]

const displayed = computed(() => {
  if (stockFilter.value === 'in') return products.value.filter((p) => p.totalStock > 0)
  if (stockFilter.value === 'out') return products.value.filter((p) => p.totalStock === 0)
  return products.value
})

// Biến thể máy: Màu (+ mã màu swatch) × Dung lượng, có giá bán/giá vốn/tồn/SKU.
interface VariantForm {
  id?: number; color?: string; colorHex?: string; storage?: string
  price: number; cost: number; stockQuantity: number; sku?: string
}
interface ImageForm { url: string; isPrimary: boolean }
const form = ref<{
  id?: number; categoryId?: number; brandId?: number; name: string; description: string; basePrice: number; status: string
  warrantyMonths: number; installmentAvailable: boolean
  specifications: ProductSpec[]
  variants: VariantForm[]; images: ImageForm[]
}>(blank())

function blank() {
  return {
    name: '', description: '', basePrice: 0, status: 'Active',
    warrantyMonths: 12, installmentAvailable: false,
    specifications: [] as ProductSpec[],
    variants: [{ price: 0, cost: 0, stockQuantity: 0 }] as VariantForm[],
    images: [{ url: '', isPrimary: true }] as ImageForm[],
    categoryId: undefined as number | undefined, brandId: undefined as number | undefined
  }
}

const statusOptions = [
  { label: 'Đang bán', value: 'Active' },
  { label: 'Nháp', value: 'Draft' },
  { label: 'Ngừng bán', value: 'Inactive' }
]

async function load() {
  loading.value = true
  try {
    // pageSize tối đa backend cho phép là 100 — lấy đủ mọi trang để bảng quản trị hiển thị toàn bộ máy.
    const base = { keyword: search.value.trim() || undefined, categoryId: filterCat.value, pageSize: 100 }
    const first = await productApi.search({ ...base, page: 1 })
    const all = [...first.items]
    for (let p = 2; p <= (first.totalPages || 1); p++) {
      const res = await productApi.search({ ...base, page: p })
      all.push(...res.items)
    }
    products.value = all
  } finally { loading.value = false }
}

function openNew() { form.value = blank(); activeTab.value = 'info'; showDialog.value = true }

async function openEdit(p: ProductListItem) {
  const detail = await productApi.byId(p.id)
  form.value = {
    id: detail.id, categoryId: detail.categoryId, brandId: detail.brandId, name: detail.name,
    description: detail.description || '', basePrice: detail.basePrice, status: detail.status,
    warrantyMonths: detail.warrantyMonths ?? 12, installmentAvailable: detail.installmentAvailable ?? false,
    specifications: detail.specifications.map((s) => ({ group: s.group, name: s.name, value: s.value })),
    variants: detail.variants.map((v) => ({
      id: v.id, color: v.color, colorHex: v.colorHex, storage: v.storage,
      price: v.price, cost: v.cost ?? 0, stockQuantity: v.stockQuantity, sku: v.sku
    })),
    images: detail.images.map((i) => ({ url: i.url, isPrimary: i.isPrimary }))
  }
  activeTab.value = 'info'
  showDialog.value = true
}

function addVariant() { form.value.variants.push({ price: form.value.basePrice, cost: 0, stockQuantity: 0 }) }

// ---------- Thông số kỹ thuật (group / name / value) ----------
function addSpec() { form.value.specifications.push({ group: '', name: '', value: '' }) }
function removeSpec(i: number) { form.value.specifications.splice(i, 1) }

// ---------- Trình sinh biến thể theo ma trận Màu × Dung lượng ----------
// Màu nhập dạng "Đen:#111111, Trắng:#ffffff" (mã màu tuỳ chọn sau dấu hai chấm).
const genColors = ref('')
const genStorages = ref('')
const genPrice = ref(0)
const genCost = ref(0)
const genStock = ref(0)

interface ParsedColor { name: string; hex?: string }
function parseColors(s: string): ParsedColor[] {
  return s.split(',').map((x) => x.trim()).filter(Boolean).map((token) => {
    const [name, hex] = token.split(':').map((t) => t.trim())
    return { name, hex: hex || undefined }
  })
}
function parseList(s: string): string[] {
  return s.split(',').map((x) => x.trim()).filter(Boolean)
}

// Sinh SKU gợi ý từ tên máy + dung lượng + màu.
function suggestSku(color?: string, storage?: string): string {
  const base = form.value.name.trim().split(/\s+/).map((w) => w[0]).join('').toUpperCase().slice(0, 4) || 'SP'
  const st = (storage || '').replace(/\s+/g, '').toUpperCase()
  const cl = (color || '').normalize('NFD').replace(/[̀-ͯ]/g, '').replace(/\s+/g, '').toUpperCase().slice(0, 3)
  return [base, st, cl].filter(Boolean).join('-')
}

function generateVariants() {
  const colors = parseColors(genColors.value)
  const storages = parseList(genStorages.value)
  if (!colors.length && !storages.length) {
    toast.add({ severity: 'warn', summary: 'Nhập ít nhất một danh sách Màu hoặc Dung lượng', life: 2500 }); return
  }
  const colorList: (ParsedColor | undefined)[] = colors.length ? colors : [undefined]
  const storageList: (string | undefined)[] = storages.length ? storages : [undefined]
  const price = genPrice.value || form.value.basePrice
  const combos: VariantForm[] = []
  for (const c of colorList) for (const s of storageList) {
    // Bỏ qua tổ hợp đã có (cùng Màu + Dung lượng) để không trùng.
    const dup = form.value.variants.some((v) => (v.color || '') === (c?.name || '') && (v.storage || '') === (s || ''))
    if (!dup) combos.push({
      color: c?.name, colorHex: c?.hex, storage: s,
      price, cost: genCost.value || 0, stockQuantity: genStock.value || 0,
      sku: suggestSku(c?.name, s)
    })
  }
  if (!combos.length) { toast.add({ severity: 'info', summary: 'Các tổ hợp đã có sẵn', life: 2000 }); return }
  // Xoá biến thể rỗng mặc định khi sinh lần đầu.
  form.value.variants = form.value.variants.filter((v) => v.color || v.storage || v.price || v.stockQuantity || v.sku)
  form.value.variants.push(...combos)
  toast.add({ severity: 'success', summary: `Đã sinh ${combos.length} biến thể`, life: 2000 })
}

const uploading = ref(false)
// Upload ảnh từ máy -> server lưu local -> nhận URL.
async function onUploadImages(e: { files: File | File[] }) {
  const files = Array.isArray(e.files) ? e.files : [e.files]
  uploading.value = true
  try {
    for (const file of files) {
      const { url } = await uploadApi.image(file)
      form.value.images.push({ url, isPrimary: form.value.images.length === 0 })
    }
    toast.add({ severity: 'success', summary: `Đã tải ${files.length} ảnh`, life: 1800 })
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Lỗi tải ảnh', detail: extractError(err), life: 3000 })
  } finally { uploading.value = false }
}
function removeImage(i: number) {
  const wasPrimary = form.value.images[i].isPrimary
  form.value.images.splice(i, 1)
  if (wasPrimary && form.value.images.length) form.value.images[0].isPrimary = true
}
function setPrimary(i: number) { form.value.images.forEach((im, idx) => (im.isPrimary = idx === i)) }

async function save() {
  if (!form.value.categoryId) { toast.add({ severity: 'warn', summary: 'Chọn danh mục', life: 2500 }); return }
  if (!form.value.brandId) { toast.add({ severity: 'warn', summary: 'Chọn thương hiệu', life: 2500 }); return }
  saving.value = true
  try {
    // Lọc thông số hợp lệ (đủ tên + giá trị).
    const specs = form.value.specifications.filter((s) => s.name.trim() && s.value.trim())
    const commonPayload = {
      categoryId: form.value.categoryId, brandId: form.value.brandId, name: form.value.name,
      description: form.value.description, basePrice: form.value.basePrice, status: form.value.status,
      warrantyMonths: form.value.warrantyMonths, installmentAvailable: form.value.installmentAvailable,
      specifications: specs, variants: form.value.variants
    }
    if (form.value.id) {
      await productApi.update(form.value.id, commonPayload)
    } else {
      await productApi.create({ ...commonPayload, images: form.value.images.filter((i) => i.url) })
    }
    showDialog.value = false
    await load()
    toast.add({ severity: 'success', summary: 'Đã lưu sản phẩm', life: 2000 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally { saving.value = false }
}

function remove(p: ProductListItem) {
  confirm.require({
    message: `Xóa sản phẩm "${p.name}"?`, header: 'Xác nhận', icon: 'pi pi-exclamation-triangle',
    accept: async () => {
      try { await productApi.remove(p.id); await load(); toast.add({ severity: 'success', summary: 'Đã xóa', life: 2000 }) }
      catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
    }
  })
}

const categoryOptions = ref<{ label: string; value: number }[]>([])
const brandOptions = ref<{ label: string; value: number }[]>([])
onMounted(async () => {
  const [cats, brs] = await Promise.all([categoryApi.all(), brandsApi.list().catch(() => [] as Brand[])])
  categories.value = cats
  brands.value = brs
  categoryOptions.value = cats.map((c) => ({ label: c.name, value: c.id }))
  brandOptions.value = brs.map((b) => ({ label: b.name, value: b.id }))
  await load()
})
</script>

<template>
  <div class="head">
    <h1>Quản lý sản phẩm</h1>
    <Button label="Thêm sản phẩm" icon="pi pi-plus" size="small" @click="openNew" />
  </div>

  <div class="filter-bar">
    <IconField iconPosition="left" class="grow">
      <InputIcon class="pi pi-search" />
      <InputText v-model="search" placeholder="Tìm tên máy..." size="small" class="w-full" @keyup.enter="load" />
    </IconField>
    <Select v-model="filterCat" :options="categoryOptions" optionLabel="label" optionValue="value"
      placeholder="Danh mục" showClear size="small" style="width: 180px" @change="load" />
    <Select v-model="stockFilter" :options="stockOptions" optionLabel="label" optionValue="value" size="small" style="width: 150px" />
    <Button label="Lọc" icon="pi pi-filter" size="small" @click="load" />
    <span class="count">{{ displayed.length }} sản phẩm</span>
  </div>

  <DataTable :value="displayed" :loading="loading" paginator :rows="10" :rowsPerPageOptions="[10, 20, 50]" stripedRows size="small" class="box">
    <Column field="id" header="ID" style="width: 60px" />
    <Column header="Ảnh" style="width: 80px">
      <template #body="{ data }">
        <img :src="data.primaryImage || 'https://placehold.co/48'" class="thumb" alt="" />
      </template>
    </Column>
    <Column field="name" header="Tên máy" />
    <Column field="brandName" header="Hãng" style="width: 120px" />
    <Column header="Giá">
      <template #body="{ data }">{{ formatCurrency(data.basePrice) }}</template>
    </Column>
    <Column header="Tồn kho">
      <template #body="{ data }">
        <Tag :value="String(data.totalStock)" :severity="data.totalStock > 0 ? 'success' : 'danger'" />
      </template>
    </Column>
    <Column header="Thao tác" style="width: 120px">
      <template #body="{ data }">
        <Button icon="pi pi-pencil" text size="small" @click="openEdit(data)" />
        <Button icon="pi pi-trash" text size="small" severity="danger" @click="remove(data)" />
      </template>
    </Column>
    <template #empty>
      <div class="empty">
        <i class="pi pi-mobile" />
        <p>Chưa có sản phẩm nào phù hợp.</p>
        <Button label="Thêm sản phẩm" icon="pi pi-plus" size="small" @click="openNew" />
      </div>
    </template>
  </DataTable>

  <Dialog v-model:visible="showDialog" :header="form.id ? 'Sửa sản phẩm' : 'Thêm sản phẩm'" modal
    class="prod-dialog" :style="{ width: '860px' }">
    <Tabs v-model:value="activeTab" class="prod-tabs">
      <TabList>
        <Tab value="info"><i class="pi pi-align-left" /> Thông tin</Tab>
        <Tab value="specs"><i class="pi pi-list" /> Thông số<span class="tab-count">{{ form.specifications.length }}</span></Tab>
        <Tab value="variants"><i class="pi pi-sliders-h" /> Biến thể<span class="tab-count">{{ form.variants.length }}</span></Tab>
        <Tab v-if="!form.id" value="images"><i class="pi pi-images" /> Hình ảnh<span class="tab-count">{{ form.images.length }}</span></Tab>
      </TabList>

      <TabPanels>
        <!-- TAB 1 · Thông tin cơ bản -->
        <TabPanel value="info">
          <div class="form">
            <label>Tên sản phẩm</label>
            <InputText v-model="form.name" class="w-full" placeholder="VD: iPhone 15 Pro Max" />
            <div class="two">
              <div><label>Danh mục</label>
                <Select v-model="form.categoryId" :options="categoryOptions" optionLabel="label" optionValue="value"
                  placeholder="Chọn danh mục" class="w-full" />
              </div>
              <div><label>Thương hiệu</label>
                <Select v-model="form.brandId" :options="brandOptions" optionLabel="label" optionValue="value"
                  placeholder="Chọn hãng" class="w-full" />
              </div>
            </div>
            <div class="two">
              <div><label>Giá gốc</label><InputNumber v-model="form.basePrice" :min="0" :suffix="' ₫'" inputClass="w-full" class="w-full" /></div>
              <div><label>Trạng thái</label><Select v-model="form.status" :options="statusOptions" optionLabel="label" optionValue="value" class="w-full" /></div>
            </div>
            <div class="two">
              <div><label>Bảo hành (tháng)</label><InputNumber v-model="form.warrantyMonths" :min="0" :max="60" inputClass="w-full" class="w-full" /></div>
              <div class="switch-cell">
                <label>Hỗ trợ trả góp</label>
                <div class="switch-inline"><ToggleSwitch v-model="form.installmentAvailable" /> <span>{{ form.installmentAvailable ? 'Có' : 'Không' }}</span></div>
              </div>
            </div>
            <label>Mô tả</label>
            <Textarea v-model="form.description" rows="4" class="w-full" placeholder="Mô tả chi tiết máy..." autoResize />
          </div>
        </TabPanel>

        <!-- TAB 2 · Thông số kỹ thuật (group / name / value) -->
        <TabPanel value="specs">
          <div class="form">
            <div class="var-summary">
              <span><i class="pi pi-list" /> <strong>{{ form.specifications.length }}</strong> dòng thông số</span>
              <Button label="Thêm dòng" icon="pi pi-plus" size="small" text class="ml-auto" @click="addSpec" />
            </div>
            <template v-if="form.specifications.length">
              <div class="spec-head"><span>Nhóm</span><span>Tên</span><span>Giá trị</span><span></span></div>
              <div v-for="(sp, i) in form.specifications" :key="i" class="spec-row">
                <InputText v-model="sp.group" placeholder="VD: Màn hình" />
                <InputText v-model="sp.name" placeholder="VD: Kích thước" />
                <InputText v-model="sp.value" placeholder="VD: 6.7 inch" />
                <Button icon="pi pi-times" text severity="danger" @click="removeSpec(i)" />
              </div>
            </template>
            <div v-else class="empty-mini">
              <i class="pi pi-list" />
              <span>Chưa có thông số — bấm "Thêm dòng". Các dòng cùng "Nhóm" sẽ gom lại khi hiển thị cho khách.</span>
            </div>
          </div>
        </TabPanel>

        <!-- TAB 3 · Biến thể (Màu × Dung lượng / Giá / Vốn / Tồn / SKU) -->
        <TabPanel value="variants">
          <div class="form">
            <div class="var-summary">
              <span><i class="pi pi-tags" /> <strong>{{ form.variants.length }}</strong> biến thể</span>
              <span><i class="pi pi-box" /> Tổng tồn kho: <strong>{{ variantStock }}</strong></span>
              <Button label="Thêm biến thể" icon="pi pi-plus" size="small" text class="ml-auto" @click="addVariant" />
            </div>

            <!-- Trình sinh nhanh ma trận Màu × Dung lượng -->
            <div class="gen-box">
              <div class="gen-title">
                <i class="pi pi-th-large" /> Sinh nhanh theo ma trận Màu × Dung lượng
                <i class="pi pi-info-circle gen-info"
                   v-tooltip.top="'Màu ghi kèm mã: Đen:#111111, Trắng:#ffffff. Dung lượng: 128GB, 256GB. Tự tạo mọi tổ hợp + SKU gợi ý. Bỏ trống giá để dùng giá gốc.'" />
              </div>
              <div class="gen-row">
                <InputText v-model="genColors" placeholder="Màu: Đen:#111111, Trắng:#ffffff" class="w-full" />
                <InputText v-model="genStorages" placeholder="Dung lượng: 128GB, 256GB, 512GB" class="w-full" />
              </div>
              <div class="gen-row">
                <InputNumber v-model="genPrice" placeholder="Giá bán" :min="0" inputClass="w-full" class="w-full" />
                <InputNumber v-model="genCost" placeholder="Giá vốn" :min="0" inputClass="w-full" class="w-full" />
                <InputNumber v-model="genStock" placeholder="Tồn mỗi loại" :min="0" inputClass="w-full" class="w-full" />
                <Button label="Sinh" icon="pi pi-bolt" size="small" outlined @click="generateVariants" />
              </div>
            </div>

            <template v-if="form.variants.length">
              <div class="variant-head">
                <span>Màu</span><span>Mã màu</span><span>Dung lượng</span><span>Giá bán</span><span>Giá vốn</span><span>Tồn</span><span>SKU</span><span></span>
              </div>
              <div v-for="(v, i) in form.variants" :key="i" class="variant-row">
                <InputText v-model="v.color" placeholder="—" />
                <div class="hex-cell">
                  <input type="color" class="hex-pick" :value="v.colorHex || '#1e6fff'" @input="v.colorHex = ($event.target as HTMLInputElement).value" />
                  <InputText v-model="v.colorHex" placeholder="#hex" />
                </div>
                <InputText v-model="v.storage" placeholder="—" />
                <InputNumber v-model="v.price" :min="0" inputClass="w-full" />
                <InputNumber v-model="v.cost" :min="0" inputClass="w-full" />
                <InputNumber v-model="v.stockQuantity" :min="0" inputClass="w-full" />
                <InputText v-model="v.sku" placeholder="SKU" />
                <Button icon="pi pi-times" text severity="danger" @click="form.variants.splice(i, 1)" />
              </div>
            </template>
            <div v-else class="empty-mini">
              <i class="pi pi-inbox" />
              <span>Chưa có biến thể — thêm thủ công hoặc dùng trình sinh phía trên.</span>
            </div>
          </div>
        </TabPanel>

        <!-- TAB 4 · Hình ảnh (chỉ khi tạo mới) -->
        <TabPanel v-if="!form.id" value="images">
          <div class="form">
            <FileUpload mode="basic" customUpload auto multiple accept="image/*" :maxFileSize="5000000"
              chooseLabel="Chọn ảnh từ máy" chooseIcon="pi pi-upload" :disabled="uploading" @uploader="onUploadImages" />
            <small class="text-muted">Ảnh lưu trên máy chủ (local). Bấm vào ảnh để chọn làm ảnh chính.</small>
            <div v-if="form.images.length" class="img-grid">
              <div v-for="(img, i) in form.images" :key="i" class="img-thumb" :class="{ primary: img.isPrimary }" @click="setPrimary(i)">
                <img :src="img.url" alt="" />
                <span v-if="img.isPrimary" class="badge">Ảnh chính</span>
                <button class="rm" @click.stop="removeImage(i)"><i class="pi pi-times" /></button>
              </div>
            </div>
            <div v-else class="empty-mini">
              <i class="pi pi-image" />
              <span>Chưa có ảnh nào. Tải ảnh từ máy để bắt đầu.</span>
            </div>
          </div>
        </TabPanel>
      </TabPanels>
    </Tabs>

    <p v-if="form.id" class="edit-note"><i class="pi pi-info-circle" /> Sửa ảnh chi tiết nằm ngoài phạm vi demo — chỉnh ảnh khi tạo mới sản phẩm.</p>

    <template #footer>
      <Button label="Hủy" text @click="showDialog = false" />
      <Button label="Lưu" icon="pi pi-check" :loading="saving" @click="save" />
    </template>
  </Dialog>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; gap: var(--sp-3); margin-bottom: var(--sp-4); flex-wrap: wrap; }
.filter-bar { display: flex; gap: var(--sp-2); align-items: center; margin-bottom: var(--sp-3); flex-wrap: wrap; }
.filter-bar .grow { flex: 1; min-width: 200px; }
.filter-bar .count { color: var(--text-muted); font-size: 13px; margin-left: auto; }
.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); }
.thumb { width: 48px; height: 48px; object-fit: contain; border-radius: var(--radius-sm); background: var(--surface-2); border: 1px solid var(--border); }
.empty { display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); padding: var(--sp-8) var(--sp-4); color: var(--text-muted); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.empty p { margin: 0; }
.img-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(84px, 1fr)); gap: var(--sp-2); margin-top: var(--sp-2); }
.img-thumb { position: relative; aspect-ratio: 1; border: 2px solid var(--border); border-radius: var(--radius-sm); overflow: hidden; cursor: pointer; }
.img-thumb.primary { border-color: var(--brand); }
.img-thumb img { width: 100%; height: 100%; object-fit: cover; }
.img-thumb .badge { position: absolute; bottom: 0; left: 0; right: 0; background: var(--brand); color: #fff; font-size: 10px; text-align: center; padding: 1px; }
.img-thumb .rm { position: absolute; top: 2px; right: 2px; width: 18px; height: 18px; border: none; border-radius: 50%; background: rgba(0,0,0,.55); color: #fff; cursor: pointer; display: grid; place-items: center; padding: 0; }
.img-thumb .rm .pi { font-size: 10px; }
.form { display: flex; flex-direction: column; gap: 0.4rem; }
.form label { font-weight: 600; font-size: 0.85rem; margin-top: 0.5rem; }
.form > label:first-child { margin-top: 0; }
.two { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
.switch-cell { display: flex; flex-direction: column; }
.switch-inline { display: flex; align-items: center; gap: var(--sp-2); height: 40px; }
.switch-inline span { font-size: 14px; color: var(--text-2); }

/* Tabs trong dialog */
.prod-tabs :deep(.p-tabpanels) { padding: var(--sp-4) 0 0; min-height: 300px; }
.prod-tabs :deep(.p-tab) { display: inline-flex; align-items: center; gap: 6px; }
.tab-count { background: var(--brand-50); color: var(--brand); font-size: 11px; font-weight: 700; min-width: 18px; height: 18px; padding: 0 5px; border-radius: var(--radius-pill); display: inline-grid; place-items: center; margin-left: 6px; }

/* Tóm tắt */
.var-summary { display: flex; align-items: center; gap: var(--sp-4); flex-wrap: wrap; font-size: 13px; color: var(--text-2); margin-bottom: var(--sp-2); }
.var-summary .pi { color: var(--brand); font-size: 12px; }
.var-summary .ml-auto { margin-left: auto; }

/* Trình sinh ma trận */
.gen-box { background: var(--surface-2); border: 1px dashed var(--border-strong); border-radius: var(--radius); padding: var(--sp-3); display: flex; flex-direction: column; gap: var(--sp-2); margin-bottom: var(--sp-3); }
.gen-title { font-weight: 600; font-size: 13px; display: flex; align-items: center; gap: 6px; color: var(--text); }
.gen-title .pi { color: var(--brand); }
.gen-title .gen-info { color: var(--text-muted); font-size: 12px; cursor: help; transition: color var(--ease); }
.gen-title .gen-info:hover { color: var(--brand); }
.gen-row { display: flex; gap: var(--sp-2); align-items: center; }
.gen-row > .w-full { flex: 1; }

/* Bảng thông số */
.spec-head { display: grid; grid-template-columns: 1fr 1fr 1.4fr auto; gap: 0.5rem; padding: 0 0 4px; font-size: 11px; font-weight: 600; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.02em; }
.spec-head span:last-child { width: 34px; }
.spec-row { display: grid; grid-template-columns: 1fr 1fr 1.4fr auto; gap: 0.5rem; align-items: center; margin-top: 0.4rem; }

/* Bảng biến thể */
.variant-head { display: grid; grid-template-columns: 1fr 1.2fr 1fr 1.1fr 1.1fr 0.8fr 1.1fr auto; gap: 0.4rem; padding: 0 0 4px; font-size: 11px; font-weight: 600; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.02em; }
.variant-head span:last-child { width: 34px; }
.variant-row { display: grid; grid-template-columns: 1fr 1.2fr 1fr 1.1fr 1.1fr 0.8fr 1.1fr auto; gap: 0.4rem; align-items: center; margin-top: 0.4rem; }
.variant-row :deep(.p-inputnumber) { width: 100%; }
.variant-row :deep(.p-inputtext) { width: 100%; }
.hex-cell { display: flex; align-items: center; gap: 4px; min-width: 0; }
.hex-pick { width: 28px; height: 32px; padding: 0; border: 1px solid var(--border); border-radius: var(--radius-sm); background: none; cursor: pointer; flex-shrink: 0; }

/* Empty state nhỏ trong tab */
.empty-mini { display: flex; flex-direction: column; align-items: center; gap: var(--sp-2); padding: var(--sp-6) var(--sp-4); color: var(--text-muted); text-align: center; }
.empty-mini .pi { font-size: 2rem; color: #d1d5db; }
.empty-mini span { font-size: 13px; max-width: 380px; }

.edit-note { margin: var(--sp-3) 0 0; font-size: 12px; color: var(--text-muted); display: flex; align-items: center; gap: 6px; }
.edit-note .pi { color: var(--brand); }

@media (max-width: 640px) {
  .two { grid-template-columns: 1fr; }
  .variant-head, .spec-head { display: none; }
  .variant-row { grid-template-columns: 1fr 1fr auto; }
  .spec-row { grid-template-columns: 1fr 1fr auto; }
  .gen-row { flex-wrap: wrap; }
}
</style>
