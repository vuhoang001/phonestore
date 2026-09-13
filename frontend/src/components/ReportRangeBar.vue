<script setup lang="ts">
import { ref, onMounted } from 'vue'
import DatePicker from 'primevue/datepicker'

const emit = defineEmits<{ change: [{ from: string; to: string }] }>()
const range = ref<[Date, Date]>([daysAgo(29), new Date()])

function daysAgo(n: number) { const d = new Date(); d.setDate(d.getDate() - n); return d }
function iso(d: Date) { return d.toISOString().slice(0, 10) }

const presets = [
  { label: '7 ngày', days: 6 }, { label: '30 ngày', days: 29 },
  { label: '90 ngày', days: 89 }, { label: 'Năm nay', days: -1 }
]
function apply(days: number) {
  if (days === -1) { const y = new Date(); range.value = [new Date(y.getFullYear(), 0, 1), new Date()] }
  else range.value = [daysAgo(days), new Date()]
  emitChange()
}
function emitChange() {
  if (range.value[0] && range.value[1]) emit('change', { from: iso(range.value[0]), to: iso(range.value[1]) })
}
onMounted(emitChange)
</script>

<template>
  <div class="range-bar">
    <button v-for="p in presets" :key="p.label" class="preset" @click="apply(p.days)">{{ p.label }}</button>
    <DatePicker v-model="range" selectionMode="range" dateFormat="dd/mm/yy" :manualInput="false" size="small" showIcon @update:modelValue="emitChange" />
  </div>
</template>

<style scoped>
.range-bar { display: flex; align-items: center; gap: var(--sp-2); flex-wrap: wrap; margin-bottom: var(--sp-4); }
.preset { background: var(--surface); border: 1px solid var(--border); color: var(--text-2); padding: 5px 12px; border-radius: var(--radius-pill); cursor: pointer; font-family: inherit; font-size: 13px; transition: all var(--ease); }
.preset:hover { border-color: var(--brand); color: var(--brand); }
</style>
