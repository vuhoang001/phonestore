<script setup lang="ts">
// Ô nhập địa chỉ dùng chung: Tỉnh/Thành → Phường/Xã bậc thang theo dữ liệu hành chính
// Việt Nam 2 cấp (áp dụng từ 01/7/2025, 34 tỉnh + phường/xã). Dữ liệu đóng gói sẵn
// trong repo (src/data/vn-divisions.json) nên chạy offline, không phụ thuộc API ngoài.
import { computed, watch } from 'vue'
import Select from 'primevue/select'
import InputText from 'primevue/inputtext'
import IconField from 'primevue/iconfield'
import InputIcon from 'primevue/inputicon'
import type { Address } from '@/types'
import rawDivisions from '@/data/vn-divisions.json'

interface Division { name: string; wards: string[] }
const divisions = rawDivisions as Division[]
const provinceNames = divisions.map((p) => p.name)

const addr = defineModel<Partial<Address>>({ required: true })

// Bao dung dữ liệu cũ: nếu địa chỉ đã lưu có tỉnh/phường không nằm trong danh sách chuẩn
// (vd: bản ghi free-text trước đây), vẫn hiển thị được để sửa thay vì bị Select xoá trắng.
const provinceOptions = computed(() => {
  const cur = addr.value.province
  return cur && !provinceNames.includes(cur) ? [cur, ...provinceNames] : provinceNames
})
const wardOptions = computed(() => {
  const wards = divisions.find((p) => p.name === addr.value.province)?.wards ?? []
  const cur = addr.value.ward
  return cur && !wards.includes(cur) ? [cur, ...wards] : wards
})

// Đổi tỉnh/thành thì xoá phường/xã cũ (vì không còn thuộc tỉnh mới).
// Bỏ qua lần gán đầu khi mở form sửa (oldVal === undefined) để giữ giá trị đã lưu.
watch(
  () => addr.value.province,
  (newVal, oldVal) => {
    if (oldVal !== undefined && newVal !== oldVal) addr.value.ward = ''
  }
)
</script>

<template>
  <div class="addr-form">
    <div class="field">
      <label>Người nhận</label>
      <IconField class="w-full">
        <InputIcon class="pi pi-user" />
        <InputText v-model="addr.recipientName" placeholder="Họ và tên" class="w-full" />
      </IconField>
    </div>

    <div class="field">
      <label>Số điện thoại</label>
      <IconField class="w-full">
        <InputIcon class="pi pi-phone" />
        <InputText v-model="addr.phone" placeholder="Số điện thoại" class="w-full" />
      </IconField>
    </div>

    <div class="field">
      <label>Tỉnh / Thành phố</label>
      <Select
        v-model="addr.province"
        :options="provinceOptions"
        filter
        placeholder="Chọn tỉnh/thành"
        class="w-full"
      />
    </div>

    <div class="field">
      <label>Phường / Xã</label>
      <Select
        v-model="addr.ward"
        :options="wardOptions"
        filter
        :disabled="!addr.province"
        :placeholder="addr.province ? 'Chọn phường/xã' : 'Chọn tỉnh trước'"
        class="w-full"
      />
    </div>

    <div class="field full">
      <label>Số nhà, tên đường</label>
      <IconField class="w-full">
        <InputIcon class="pi pi-map-marker" />
        <InputText v-model="addr.detail" placeholder="VD: 12 Ngõ 5, Đội Cấn" class="w-full" />
      </IconField>
    </div>

    <div class="field full">
      <label>Ghi chú giao hàng <span class="opt">(tùy chọn)</span></label>
      <IconField class="w-full">
        <InputIcon class="pi pi-comment" />
        <InputText v-model="addr.note" placeholder="VD: Toà B, tầng 3, gọi trước khi giao" class="w-full" />
      </IconField>
    </div>
  </div>
</template>

<style scoped>
/* Bố cục 2 cột theo nhóm: Người nhận | SĐT — Tỉnh | Phường; các ô dài chiếm cả hàng. */
.addr-form {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--sp-3);
}
.field { display: flex; flex-direction: column; gap: 5px; min-width: 0; }
.field.full { grid-column: 1 / -1; }
.field label { font-size: 0.78rem; font-weight: 600; color: var(--text-2); }
.field label .opt { font-weight: 400; color: var(--text-muted); }

/* Xếp 1 cột trên màn hình hẹp để không bị chật */
@media (max-width: 520px) {
  .addr-form { grid-template-columns: 1fr; }
}
</style>
