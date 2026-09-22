<script setup lang="ts">
// Thanh So sánh nổi (đáy màn hình): hiện khi khách đã chọn ≥1 máy để so sánh.
// Cho phép xem nhanh các máy đã chọn, bỏ bớt, xóa hết, và mở trang /compare.
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import { useCompareStore } from '@/stores/compare'

const compare = useCompareStore()
const router = useRouter()
const placeholder = 'https://placehold.co/80x80/f4f6fb/c8d0e0?text=%20'

function goCompare() {
  router.push({ name: 'compare' })
}
</script>

<template>
  <transition name="slide-up">
    <div v-if="compare.count" class="compare-bar">
      <div class="container bar-inner">
        <div class="lead">
          <i class="pi pi-sliders-h" />
          <span class="label">So sánh <b>{{ compare.count }}</b>/{{ compare.MAX }}</span>
        </div>

        <div class="chips">
          <div v-for="p in compare.items" :key="p.id" class="chip">
            <img :src="p.image || placeholder" :alt="p.name" />
            <span class="chip-name">{{ p.name }}</span>
            <button class="chip-x" :aria-label="`Bỏ ${p.name}`" @click="compare.remove(p.id)">
              <i class="pi pi-times" />
            </button>
          </div>
        </div>

        <div class="bar-actions">
          <Button label="Xóa hết" icon="pi pi-trash" text severity="secondary" size="small" @click="compare.clear()" />
          <Button label="So sánh ngay" icon="pi pi-arrow-right" iconPos="right" size="small"
            :disabled="compare.count < 2" @click="goCompare" />
        </div>
      </div>
    </div>
  </transition>
</template>

<style scoped>
.compare-bar {
  position: fixed; left: 0; right: 0; bottom: 0; z-index: 120;
  background: rgba(255, 255, 255, 0.82);
  backdrop-filter: saturate(180%) blur(20px);
  -webkit-backdrop-filter: saturate(180%) blur(20px);
  border-top: 1px solid var(--border);
  box-shadow: 0 -8px 30px rgba(0, 0, 0, 0.06);
}
.bar-inner { display: flex; align-items: center; gap: var(--sp-4); min-height: 72px; padding: var(--sp-3) 0; }

.lead { display: flex; align-items: center; gap: 8px; flex-shrink: 0; color: var(--text); }
.lead .pi { color: var(--brand); font-size: 1.05rem; }
.lead .label { font-size: 14px; }
.lead b { color: var(--brand); }

.chips { display: flex; align-items: center; gap: var(--sp-2); flex: 1; overflow-x: auto; scrollbar-width: none; }
.chips::-webkit-scrollbar { display: none; }
.chip {
  display: inline-flex; align-items: center; gap: 8px; flex-shrink: 0;
  background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-pill);
  padding: 4px 6px 4px 4px; max-width: 220px;
}
.chip img { width: 34px; height: 34px; object-fit: contain; background: #fff; border-radius: 50%; flex-shrink: 0; }
.chip-name { font-size: 12.5px; color: var(--text-2); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.chip-x {
  width: 20px; height: 20px; flex-shrink: 0; border: none; cursor: pointer; border-radius: 50%;
  background: var(--surface-2); color: var(--text-muted); display: grid; place-items: center; transition: all var(--ease);
}
.chip-x:hover { background: var(--sale); color: #fff; }
.chip-x .pi { font-size: 10px; }

.bar-actions { display: flex; align-items: center; gap: var(--sp-1); flex-shrink: 0; margin-left: auto; }

/* Hiệu ứng trượt lên khi xuất hiện */
.slide-up-enter-active, .slide-up-leave-active { transition: transform var(--ease), opacity var(--ease); }
.slide-up-enter-from, .slide-up-leave-to { transform: translateY(100%); opacity: 0; }

@media (max-width: 640px) {
  .lead .label { display: none; }
  .chip-name { display: none; }
  .bar-actions :deep(.p-button-label) { display: none; }
  .bar-actions :deep(.p-button) { padding-left: 10px; padding-right: 10px; }
}
</style>
