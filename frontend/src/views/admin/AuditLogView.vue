<script setup lang="ts">
import { ref, onMounted } from 'vue'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'
import { auditApi, type AuditLog } from '@/services'
import { formatDate } from '@/composables/format'

const logs = ref<AuditLog[]>([])
const total = ref(0)
const loading = ref(true)
const page = ref(1)
const pageSize = 20

async function load() {
  loading.value = true
  try {
    const res = await auditApi.list(page.value, pageSize)
    logs.value = res.items
    total.value = res.totalItems
  } finally {
    loading.value = false
  }
}

function onPage(e: { page: number }) {
  page.value = e.page + 1
  load()
}

// Màu tag theo loại hành động.
function sev(action: string) {
  if (action.includes('Deleted')) return 'danger'
  if (action.includes('Created')) return 'success'
  return 'info'
}

onMounted(load)
</script>

<template>
  <div class="head"><h1>Nhật ký thao tác</h1></div>
  <DataTable :value="logs" :loading="loading" stripedRows class="box"
    lazy paginator :rows="pageSize" :totalRecords="total" @page="onPage">
    <Column header="Thời gian" style="width: 180px">
      <template #body="{ data }">{{ formatDate(data.createdAt) }}</template>
    </Column>
    <Column header="Hành động">
      <template #body="{ data }"><Tag :value="data.action" :severity="sev(data.action)" /></template>
    </Column>
    <Column header="Đối tượng">
      <template #body="{ data }">{{ data.entityType }}<span v-if="data.entityId"> #{{ data.entityId }}</span></template>
    </Column>
    <Column field="userId" header="Người thực hiện" style="width: 130px">
      <template #body="{ data }">{{ data.userId ? 'User #' + data.userId : '—' }}</template>
    </Column>
    <Column field="detail" header="Chi tiết" />
    <template #empty><p class="text-muted" style="padding: var(--sp-4)">Chưa có nhật ký nào.</p></template>
  </DataTable>
</template>

<style scoped>
.head { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--sp-4); }
.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); }
</style>
