<script setup lang="ts">
// Tra cứu bảo hành công khai theo IMEI hoặc mã đơn (đặc thù điện thoại).
import { ref } from 'vue'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import IconField from 'primevue/iconfield'
import InputIcon from 'primevue/inputicon'
import Tag from 'primevue/tag'
import ProgressSpinner from 'primevue/progressspinner'
import { warrantyApi } from '@/services'
import type { Warranty } from '@/types'
import { formatDate } from '@/composables/format'
import { extractError } from '@/services/api'

// Tra cứu bằng 1 trong 2: IMEI hoặc mã đơn. Người dùng nhập ô nào cũng được.
const mode = ref<'imei' | 'order'>('imei')
const query = ref('')
const loading = ref(false)
const searched = ref(false)
const results = ref<Warranty[]>([])
const error = ref('')

// Còn hạn hay hết hạn — ưu tiên trạng thái backend trả, fallback so ngày.
function isValid(w: Warranty): boolean {
  if (w.status) return /còn|valid|active/i.test(w.status)
  return new Date(w.endDate).getTime() >= Date.now()
}

async function lookup() {
  const q = query.value.trim()
  if (!q) { error.value = 'Vui lòng nhập IMEI hoặc mã đơn hàng.'; return }
  error.value = ''
  loading.value = true
  searched.value = false
  try {
    const res = mode.value === 'imei'
      ? await warrantyApi.lookup(q, undefined)
      : await warrantyApi.lookup(undefined, q)
    results.value = res.found ? res.items : []
  } catch (e) {
    error.value = extractError(e)
    results.value = []
  } finally {
    loading.value = false
    searched.value = true
  }
}
</script>

<template>
  <div class="wl">
    <div class="hero">
      <span class="hero-ic"><i class="pi pi-verified" /></span>
      <div>
        <h1>Tra cứu bảo hành</h1>
        <p class="intro">Nhập số IMEI trên máy hoặc mã đơn hàng để kiểm tra tình trạng bảo hành.</p>
      </div>
    </div>

    <div class="search-card surface-card">
      <div class="mode-tabs">
        <button class="mode" :class="{ active: mode === 'imei' }" @click="mode = 'imei'; query = ''"><i class="pi pi-hashtag" /> Theo IMEI</button>
        <button class="mode" :class="{ active: mode === 'order' }" @click="mode = 'order'; query = ''"><i class="pi pi-receipt" /> Theo mã đơn</button>
      </div>
      <div class="search-row">
        <IconField class="grow">
          <InputIcon :class="mode === 'imei' ? 'pi pi-hashtag' : 'pi pi-receipt'" />
          <InputText v-model="query" :placeholder="mode === 'imei' ? 'Nhập 15 số IMEI (bấm *#06# để xem)' : 'Nhập mã đơn hàng, VD: SV-000123'"
            class="w-full" @keyup.enter="lookup" />
        </IconField>
        <Button label="Tra cứu" icon="pi pi-search" :loading="loading" @click="lookup" />
      </div>
      <small v-if="error" class="err">{{ error }}</small>
    </div>

    <!-- Đang tra cứu -->
    <div v-if="loading" class="center"><ProgressSpinner /></div>

    <!-- Kết quả -->
    <template v-else-if="searched">
      <div v-if="results.length" class="results">
        <h2 class="section-title">Phiếu bảo hành ({{ results.length }})</h2>
        <div v-for="w in results" :key="w.id" class="wcard" :class="{ expired: !isValid(w) }">
          <div class="wc-top">
            <span class="wc-name">{{ w.productName }}</span>
            <Tag :value="isValid(w) ? 'Còn hạn' : 'Hết hạn'" :severity="isValid(w) ? 'success' : 'danger'" />
          </div>
          <div class="wc-meta">
            <span class="wc-row"><i class="pi pi-hashtag" /> IMEI: <b>{{ w.imei }}</b></span>
            <span class="wc-row"><i class="pi pi-receipt" /> Mã đơn: <b>{{ w.orderCode }}</b></span>
          </div>
          <div class="wc-dates">
            <div class="wd"><span class="wd-label">Bắt đầu</span><span class="wd-val">{{ formatDate(w.startDate) }}</span></div>
            <span class="wd-arrow"><i class="pi pi-arrow-right" /></span>
            <div class="wd"><span class="wd-label">Kết thúc</span><span class="wd-val">{{ formatDate(w.endDate) }}</span></div>
          </div>
        </div>
      </div>

      <!-- Không tìm thấy -->
      <div v-else class="empty">
        <i class="pi pi-search" />
        <p>Không tìm thấy phiếu bảo hành cho {{ mode === 'imei' ? 'IMEI' : 'mã đơn' }} này.</p>
        <small class="text-muted">Kiểm tra lại thông tin, hoặc liên hệ tổng đài 1900 6035 để được hỗ trợ.</small>
      </div>
    </template>

    <!-- Trạng thái ban đầu (chưa tra cứu) -->
    <div v-else class="hint">
      <i class="pi pi-info-circle" />
      <span>Mỗi máy khi giao được gán IMEI và tạo phiếu bảo hành tự động. Bạn có thể tra cứu bất cứ lúc nào.</span>
    </div>
  </div>
</template>

<style scoped>
.wl { max-width: 760px; margin: 0 auto; }
.hero { display: flex; align-items: center; gap: var(--sp-4); margin-bottom: var(--sp-5); }
.hero-ic { width: 56px; height: 56px; border-radius: var(--radius-lg); display: grid; place-items: center; background: var(--brand-50); color: var(--brand); flex-shrink: 0; }
.hero-ic .pi { font-size: 1.6rem; }
.hero h1 { margin: 0; font-size: 1.6rem; }
.intro { margin: 4px 0 0; color: var(--text-2); }

.search-card { padding: var(--sp-5); margin-bottom: var(--sp-5); }
.mode-tabs { display: inline-flex; gap: 4px; background: var(--surface-2); border: 1px solid var(--border); border-radius: var(--radius-pill); padding: 4px; margin-bottom: var(--sp-4); }
.mode { display: inline-flex; align-items: center; gap: 6px; background: none; border: none; border-radius: var(--radius-pill); padding: 7px 16px; cursor: pointer; font-family: inherit; font-size: 13px; font-weight: 600; color: var(--text-2); transition: all var(--ease); }
.mode .pi { font-size: 12px; }
.mode.active { background: var(--brand); color: #fff; box-shadow: var(--shadow-sm); }

.search-row { display: flex; gap: var(--sp-3); align-items: stretch; }
.search-row .grow { flex: 1; min-width: 0; }
.err { display: block; margin-top: var(--sp-2); color: var(--danger); font-size: 13px; }

.center { display: flex; justify-content: center; padding: var(--sp-8); }

.results { margin-top: var(--sp-2); }
.wcard { background: var(--surface); border: 1px solid var(--border); border-left: 4px solid var(--success); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); padding: var(--sp-4); margin-bottom: var(--sp-3); transition: box-shadow var(--ease); }
.wcard:hover { box-shadow: var(--shadow); }
.wcard.expired { border-left-color: var(--danger); }
.wc-top { display: flex; align-items: center; justify-content: space-between; gap: var(--sp-3); margin-bottom: var(--sp-3); }
.wc-name { font-weight: 700; font-size: 1rem; }
.wc-meta { display: flex; flex-wrap: wrap; gap: var(--sp-4); margin-bottom: var(--sp-3); }
.wc-row { display: inline-flex; align-items: center; gap: 6px; font-size: 13px; color: var(--text-2); }
.wc-row .pi { color: var(--brand); font-size: 12px; }
.wc-row b { color: var(--text); letter-spacing: 0.02em; }
.wc-dates { display: flex; align-items: center; gap: var(--sp-4); background: var(--surface-2); border-radius: var(--radius); padding: var(--sp-3) var(--sp-4); }
.wd { display: flex; flex-direction: column; gap: 2px; }
.wd-label { font-size: 11px; color: var(--text-muted); }
.wd-val { font-size: 13px; font-weight: 600; }
.wd-arrow { color: var(--text-muted); }

.empty { text-align: center; padding: var(--sp-8) var(--sp-4); display: flex; flex-direction: column; align-items: center; gap: var(--sp-2); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.empty p { margin: 0; color: var(--text-2); font-weight: 500; }

.hint { display: flex; align-items: center; gap: var(--sp-3); background: var(--brand-50); border: 1px solid var(--brand-100); border-radius: var(--radius); padding: var(--sp-4); color: var(--text-2); font-size: 14px; }
.hint .pi { color: var(--brand); font-size: 1.2rem; flex-shrink: 0; }

@media (max-width: 640px) {
  .search-row { flex-direction: column; }
}
</style>
