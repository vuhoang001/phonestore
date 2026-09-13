<script setup lang="ts">
// Thu cũ đổi mới (đặc thù điện thoại): khách khai máy cũ → admin định giá thu.
// Khách gửi yêu cầu và theo dõi trạng thái/định giá của các yêu cầu đã gửi.
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import ProgressSpinner from 'primevue/progressspinner'
import { tradeInApi } from '@/services'
import type { TradeIn } from '@/types'
import { useAuthStore } from '@/stores/auth'
import { formatCurrency, formatDate } from '@/composables/format'
import { extractError } from '@/services/api'

const auth = useAuthStore()
const router = useRouter()
const toast = useToast()

// Các mức tình trạng máy cũ để khách chọn nhanh (ảnh hưởng giá thu).
const conditions = [
  { label: 'Như mới (trên 99%)', value: 'Như mới (99%)' },
  { label: 'Đẹp (còn 90–98%)', value: 'Đẹp (90-98%)' },
  { label: 'Bình thường (trầy xước nhẹ)', value: 'Bình thường' },
  { label: 'Cũ / lỗi nhẹ', value: 'Cũ / lỗi nhẹ' }
]

const form = ref<{ oldDeviceModel: string; condition: string; note: string }>({
  oldDeviceModel: '',
  condition: conditions[1].value,
  note: ''
})
const submitting = ref(false)

// Nhãn + màu Tag cho trạng thái yêu cầu (khớp enum TradeInStatus backend).
const statusLabel: Record<string, string> = {
  Pending: 'Chờ định giá', Quoted: 'Đã báo giá', Accepted: 'Đã đồng ý', Rejected: 'Từ chối'
}
const statusSeverity: Record<string, string> = {
  Pending: 'warn', Quoted: 'info', Accepted: 'success', Rejected: 'danger'
}

const mine = ref<TradeIn[]>([])
const loadingMine = ref(false)

async function loadMine() {
  if (!auth.isAuthenticated) return
  loadingMine.value = true
  try { mine.value = await tradeInApi.mine() } finally { loadingMine.value = false }
}

async function submit() {
  if (!auth.isAuthenticated) {
    toast.add({ severity: 'info', summary: 'Cần đăng nhập', detail: 'Vui lòng đăng nhập để gửi yêu cầu thu cũ.', life: 3000 })
    router.push({ name: 'login', query: { redirect: '/trade-in' } })
    return
  }
  if (!form.value.oldDeviceModel.trim()) {
    toast.add({ severity: 'warn', summary: 'Thiếu thông tin', detail: 'Vui lòng nhập model máy cũ của bạn.', life: 3000 })
    return
  }
  submitting.value = true
  try {
    await tradeInApi.create({
      oldDeviceModel: form.value.oldDeviceModel.trim(),
      condition: form.value.condition,
      note: form.value.note.trim() || undefined
    })
    toast.add({ severity: 'success', summary: 'Đã gửi yêu cầu', detail: 'Chúng tôi sẽ định giá và phản hồi sớm.', life: 3500 })
    form.value.oldDeviceModel = ''
    form.value.note = ''
    await loadMine()
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 4000 })
  } finally {
    submitting.value = false
  }
}

onMounted(loadMine)
</script>

<template>
  <div class="ti-wrap">
    <header class="ti-hero">
      <div class="ti-hero-txt">
        <span class="badge"><i class="pi pi-sync" /> Thu cũ đổi mới</span>
        <h1>Lên đời điện thoại — thu cũ giá tốt</h1>
        <p>Khai báo máy cũ của bạn, chúng tôi định giá minh bạch và trừ thẳng vào máy mới. Nhanh gọn, không ép giá.</p>
        <ul class="perks">
          <li><i class="pi pi-check-circle" /> Định giá trong ngày</li>
          <li><i class="pi pi-check-circle" /> Trừ trực tiếp khi mua máy mới</li>
          <li><i class="pi pi-check-circle" /> Hỗ trợ mọi hãng: Apple, Samsung, Xiaomi…</li>
        </ul>
      </div>
    </header>

    <div class="ti-grid">
      <!-- Form gửi yêu cầu -->
      <section class="card form-card">
        <h2><i class="pi pi-mobile" /> Gửi yêu cầu định giá</h2>
        <div class="field">
          <label>Model máy cũ <span class="req">*</span></label>
          <InputText v-model="form.oldDeviceModel" placeholder="VD: iPhone 13 Pro 128GB" />
        </div>
        <div class="field">
          <label>Tình trạng máy</label>
          <Select v-model="form.condition" :options="conditions" optionLabel="label" optionValue="value" class="w-full" />
        </div>
        <div class="field">
          <label>Ghi chú (tùy chọn)</label>
          <Textarea v-model="form.note" rows="3" autoResize placeholder="Pin chai, còn hộp/sạc, tình trạng màn hình…" />
        </div>
        <Button :label="submitting ? 'Đang gửi…' : 'Gửi yêu cầu'" icon="pi pi-send" :loading="submitting" @click="submit" />
        <p v-if="!auth.isAuthenticated" class="hint"><i class="pi pi-info-circle" /> Bạn cần đăng nhập để gửi và theo dõi yêu cầu.</p>
      </section>

      <!-- Yêu cầu của tôi -->
      <section class="card">
        <h2><i class="pi pi-list" /> Yêu cầu của tôi</h2>
        <div v-if="!auth.isAuthenticated" class="empty">
          <i class="pi pi-user" />
          <p>Đăng nhập để xem lịch sử và kết quả định giá.</p>
          <Button label="Đăng nhập" outlined size="small" @click="router.push({ name: 'login', query: { redirect: '/trade-in' } })" />
        </div>
        <div v-else-if="loadingMine" class="center"><ProgressSpinner style="width:42px;height:42px" /></div>
        <DataTable v-else :value="mine" size="small" stripedRows paginator :rows="8">
          <Column header="Máy cũ" field="oldDeviceModel" />
          <Column header="Tình trạng" field="condition" />
          <Column header="Định giá">
            <template #body="{ data }">
              <b v-if="data.quotedPrice > 0" class="price">{{ formatCurrency(data.quotedPrice) }}</b>
              <span v-else class="muted">—</span>
            </template>
          </Column>
          <Column header="Trạng thái">
            <template #body="{ data }"><Tag :value="statusLabel[data.status] || data.status" :severity="statusSeverity[data.status]" /></template>
          </Column>
          <Column header="Ngày gửi">
            <template #body="{ data }">{{ formatDate(data.createdAt) }}</template>
          </Column>
          <template #empty><div class="empty-row">Bạn chưa gửi yêu cầu thu cũ nào.</div></template>
        </DataTable>
      </section>
    </div>
  </div>
</template>

<style scoped>
.ti-wrap { max-width: 1080px; margin: 0 auto; padding: var(--sp-4) 0 var(--sp-8); }

.ti-hero { background: linear-gradient(120deg, var(--brand-50), var(--surface)); border: 1px solid var(--border); border-radius: var(--radius-lg); padding: var(--sp-6); margin-bottom: var(--sp-4); }
.badge { display: inline-flex; align-items: center; gap: 6px; background: var(--brand); color: #fff; font-size: 12px; font-weight: 600; padding: 4px 12px; border-radius: var(--radius-pill); }
.ti-hero h1 { margin: var(--sp-3) 0 var(--sp-2); font-size: 1.6rem; }
.ti-hero p { color: var(--text-2); margin: 0 0 var(--sp-3); max-width: 620px; }
.perks { list-style: none; padding: 0; margin: 0; display: flex; flex-wrap: wrap; gap: var(--sp-2) var(--sp-5); }
.perks li { display: flex; align-items: center; gap: 6px; font-size: 13px; color: var(--text-2); }
.perks .pi { color: var(--ok); }

.ti-grid { display: grid; grid-template-columns: 1fr 1.15fr; gap: var(--sp-4); }
.card { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); padding: var(--sp-5); }
.card h2 { display: flex; align-items: center; gap: 8px; margin: 0 0 var(--sp-4); font-size: 1.05rem; }
.card h2 .pi { color: var(--brand); }

.field { margin-bottom: var(--sp-3); display: flex; flex-direction: column; gap: 6px; }
.field label { font-size: 13px; color: var(--text-2); font-weight: 500; }
.field .req { color: var(--price); }
.field :deep(.p-inputtext), .field :deep(.p-select), .field :deep(.p-textarea) { width: 100%; }
.form-card :deep(.p-button) { width: 100%; }
.hint { display: flex; align-items: center; gap: 6px; margin: var(--sp-3) 0 0; font-size: 12.5px; color: var(--text-muted); }

.price { color: var(--price); }
.muted { color: var(--text-muted); }
.center { display: flex; justify-content: center; padding: var(--sp-6); }
.empty { text-align: center; padding: var(--sp-6) var(--sp-4); color: var(--text-muted); display: flex; flex-direction: column; align-items: center; gap: var(--sp-2); }
.empty .pi { font-size: 1.8rem; opacity: .5; }
.empty-row { padding: var(--sp-5); text-align: center; color: var(--text-muted); }

@media (max-width: 900px) { .ti-grid { grid-template-columns: 1fr; } }
</style>
