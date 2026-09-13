<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import Button from 'primevue/button'
import { infoPages } from '@/data/infoPages'

const route = useRoute()
// Nội dung theo meta.key của route (mỗi link footer là 1 key).
const page = computed(() => infoPages[(route.meta.key as string) || ''] || null)
</script>

<template>
  <div v-if="page" class="info-page">
    <div class="hero">
      <span class="hero-ic"><i class="pi" :class="page.icon" /></span>
      <div>
        <h1>{{ page.title }}</h1>
        <p v-if="page.intro" class="intro">{{ page.intro }}</p>
      </div>
    </div>

    <div class="content surface-card">
      <section v-for="(s, i) in page.sections" :key="i" class="sec">
        <h2 v-if="s.heading">{{ s.heading }}</h2>
        <p v-for="(p, pi) in s.paragraphs || []" :key="'p' + pi">{{ p }}</p>
        <ul v-if="s.list">
          <li v-for="(l, li) in s.list" :key="'l' + li"><i class="pi pi-check" /> <span>{{ l }}</span></li>
        </ul>
      </section>
    </div>

    <div class="foot-nav">
      <router-link to="/products" class="link"><i class="pi pi-arrow-left" /> Tiếp tục mua sắm</router-link>
      <router-link to="/lien-he" class="link" v-if="route.meta.key !== 'contact'">Cần hỗ trợ? Liên hệ →</router-link>
    </div>
  </div>

  <div v-else class="empty">
    <i class="pi pi-file" />
    <p>Không tìm thấy nội dung.</p>
    <Button label="Về trang chủ" @click="$router.push('/')" />
  </div>
</template>

<style scoped>
.info-page { max-width: 860px; margin: 0 auto; }
.hero { display: flex; align-items: center; gap: var(--sp-4); margin-bottom: var(--sp-5); }
.hero-ic { width: 56px; height: 56px; border-radius: var(--radius-lg); display: grid; place-items: center;
  background: var(--brand-50); color: var(--brand); flex-shrink: 0; }
.hero-ic .pi { font-size: 1.6rem; }
.hero h1 { margin: 0; font-size: 1.6rem; }
.intro { margin: 4px 0 0; color: var(--text-2); }

.content { padding: var(--sp-6); }
.sec { padding: var(--sp-2) 0; }
.sec + .sec { border-top: 1px solid var(--border); margin-top: var(--sp-4); padding-top: var(--sp-5); }
.sec h2 { font-size: 1.05rem; margin: 0 0 var(--sp-3); color: var(--text); }
.sec p { margin: 0 0 var(--sp-3); line-height: 1.7; color: var(--text-2); }
.sec p:last-child { margin-bottom: 0; }
.sec ul { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: var(--sp-3); }
.sec ul li { display: flex; align-items: flex-start; gap: var(--sp-3); line-height: 1.6; color: var(--text-2); }
.sec ul li .pi { color: var(--brand); font-size: 12px; margin-top: 5px; flex-shrink: 0; }

.foot-nav { display: flex; justify-content: space-between; flex-wrap: wrap; gap: var(--sp-3); margin: var(--sp-5) 0; }
.foot-nav .link { color: var(--brand); font-weight: 600; display: inline-flex; align-items: center; gap: 6px; }
.foot-nav .link:hover { color: var(--brand-dark); }

.empty { text-align: center; padding: var(--sp-8); display: flex; flex-direction: column; align-items: center; gap: var(--sp-3); }
.empty .pi { font-size: 2.75rem; color: #d1d5db; }
.empty p { margin: 0; color: var(--text-muted); }
</style>
