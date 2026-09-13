<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Dialog from 'primevue/dialog'
import Checkbox from 'primevue/checkbox'
import Tag from 'primevue/tag'
import AddressFields from '@/components/AddressFields.vue'
import { addressApi, authApi, couponApi, uploadApi } from '@/services'
import type { Address, Coupon } from '@/types'
import { useAuthStore } from '@/stores/auth'
import { formatCurrency, formatAddressLine } from '@/composables/format'
import { extractError } from '@/services/api'

const auth = useAuthStore()
const toast = useToast()
const confirm = useConfirm()

// ----- Menu section (gộp Sổ địa chỉ vào Hồ sơ nên bỏ mục riêng) -----
const section = ref<'profile' | 'password' | 'vouchers'>('profile')
const menu = [
  { key: 'profile', label: 'Hồ sơ & địa chỉ', icon: 'pi pi-user' },
  { key: 'password', label: 'Đổi mật khẩu', icon: 'pi pi-lock' },
  { key: 'vouchers', label: 'Ví voucher', icon: 'pi pi-ticket' }
] as const

// ----- Profile -----
const profile = ref({ fullName: auth.user?.fullName || '', phone: auth.user?.phone || '' })
const savingProfile = ref(false)
async function saveProfile() {
  savingProfile.value = true
  try {
    // Gửi kèm avatar hiện tại để lần lưu tên/SĐT không xoá mất ảnh
    const u = await authApi.updateProfile({ ...profile.value, avatarUrl: auth.user?.avatarUrl })
    auth.updateUser(u)
    toast.add({ severity: 'success', summary: 'Đã cập nhật hồ sơ', life: 2000 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally { savingProfile.value = false }
}

// ----- Avatar -----
const avatarInput = ref<HTMLInputElement>()
const uploadingAvatar = ref(false)
function pickAvatar() { avatarInput.value?.click() }
// Tải ảnh lên MinIO rồi lưu ngay vào hồ sơ để avatar cập nhật tức thì
async function onAvatarChange(e: Event) {
  const input = e.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return
  uploadingAvatar.value = true
  try {
    const { url } = await uploadApi.avatar(file)
    const u = await authApi.updateProfile({ ...profile.value, avatarUrl: url })
    auth.updateUser(u)
    toast.add({ severity: 'success', summary: 'Đã cập nhật ảnh đại diện', life: 2000 })
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(err), life: 3000 })
  } finally { uploadingAvatar.value = false; input.value = '' }
}
async function removeAvatar() {
  uploadingAvatar.value = true
  try {
    const u = await authApi.updateProfile({ ...profile.value, avatarUrl: undefined })
    auth.updateUser(u)
    toast.add({ severity: 'success', summary: 'Đã xoá ảnh đại diện', life: 2000 })
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(err), life: 3000 })
  } finally { uploadingAvatar.value = false }
}

// ----- Password -----
const pwd = ref({ currentPassword: '', newPassword: '', confirm: '' })
const savingPwd = ref(false)
async function changePassword() {
  if (pwd.value.newPassword !== pwd.value.confirm) {
    toast.add({ severity: 'warn', summary: 'Mật khẩu xác nhận không khớp', life: 2500 }); return
  }
  savingPwd.value = true
  try {
    await authApi.changePassword({ currentPassword: pwd.value.currentPassword, newPassword: pwd.value.newPassword })
    pwd.value = { currentPassword: '', newPassword: '', confirm: '' }
    toast.add({ severity: 'success', summary: 'Đổi mật khẩu thành công', life: 2000 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  } finally { savingPwd.value = false }
}

// ----- Vouchers -----
const coupons = ref<Coupon[]>([])
const savedCodes = ref<string[]>(JSON.parse(localStorage.getItem('savedVouchers') || '[]'))
function isSaved(code: string) { return savedCodes.value.includes(code) }
function toggleSave(c: Coupon) {
  savedCodes.value = isSaved(c.code) ? savedCodes.value.filter((x) => x !== c.code) : [...savedCodes.value, c.code]
  localStorage.setItem('savedVouchers', JSON.stringify(savedCodes.value))
}
async function copyCode(code: string) {
  try { await navigator.clipboard.writeText(code); toast.add({ severity: 'success', summary: 'Đã sao chép ' + code, life: 1500 }) } catch { /* noop */ }
}

// ----- Addresses -----
const addresses = ref<Address[]>([])
const showDialog = ref(false)
const editing = ref<Partial<Address>>({})
async function loadAddresses() { addresses.value = await addressApi.mine() }
function openNew() { editing.value = { isDefault: false }; showDialog.value = true }
function openEdit(a: Address) { editing.value = { ...a }; showDialog.value = true }
async function saveAddress() {
  try {
    if (editing.value.id) await addressApi.update(editing.value.id, editing.value)
    else await addressApi.create(editing.value)
    showDialog.value = false
    await loadAddresses()
    toast.add({ severity: 'success', summary: 'Đã lưu địa chỉ', life: 2000 })
  } catch (e) { toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 }) }
}
function removeAddress(a: Address) {
  confirm.require({
    message: `Xóa địa chỉ của ${a.recipientName}?`, header: 'Xác nhận', icon: 'pi pi-exclamation-triangle',
    accept: async () => { await addressApi.remove(a.id); await loadAddresses() }
  })
}

onMounted(async () => {
  await loadAddresses()
  coupons.value = await couponApi.available().catch(() => [])
})
</script>

<template>
  <div class="account">
    <aside class="side">
      <div class="user-chip">
        <span class="avatar">
          <img v-if="auth.user?.avatarUrl" :src="auth.user.avatarUrl" alt="" />
          <template v-else>{{ auth.user?.fullName?.[0] || 'U' }}</template>
        </span>
        <div><strong>{{ auth.user?.fullName }}</strong><div class="text-muted">{{ auth.user?.email }}</div></div>
      </div>
      <button v-for="m in menu" :key="m.key" class="side-item" :class="{ active: section === m.key }" @click="section = m.key">
        <i :class="m.icon" /> {{ m.label }}
      </button>
    </aside>

    <div class="content">
      <!-- Hồ sơ + Sổ địa chỉ (gộp chung) -->
      <section v-if="section === 'profile'" class="surface-card">
        <h3>Hồ sơ của tôi</h3>

        <!-- Ảnh đại diện -->
        <div class="avatar-row">
          <div class="avatar-lg" :class="{ loading: uploadingAvatar }">
            <img v-if="auth.user?.avatarUrl" :src="auth.user.avatarUrl" alt="Ảnh đại diện" />
            <span v-else class="avatar-initial">{{ auth.user?.fullName?.[0] || 'U' }}</span>
            <span v-if="uploadingAvatar" class="avatar-spin"><i class="pi pi-spin pi-spinner" /></span>
          </div>
          <div class="avatar-meta">
            <div class="avatar-actions">
              <Button label="Đổi ảnh" icon="pi pi-camera" size="small" :disabled="uploadingAvatar" @click="pickAvatar" />
              <Button v-if="auth.user?.avatarUrl" label="Xoá ảnh" icon="pi pi-trash" text severity="danger" size="small" :disabled="uploadingAvatar" @click="removeAvatar" />
            </div>
            <small class="text-muted">JPG, PNG, WEBP hoặc GIF — tối đa 5MB.</small>
          </div>
          <input ref="avatarInput" type="file" accept="image/*" hidden @change="onAvatarChange" />
        </div>

        <div class="form">
          <label>Email</label>
          <InputText :modelValue="auth.user?.email" disabled class="w-full" />
          <label>Họ tên</label>
          <InputText v-model="profile.fullName" class="w-full" />
          <label>Số điện thoại</label>
          <InputText v-model="profile.phone" class="w-full" />
          <Button label="Lưu thay đổi" :loading="savingProfile" class="mt-3" @click="saveProfile" />
        </div>

        <!-- Sổ địa chỉ (gộp vào cùng trang hồ sơ) -->
        <div class="sub-divider"></div>
        <div class="head-row">
          <div><h3 class="sub-h">Sổ địa chỉ</h3><small class="text-muted">Địa chỉ nhận hàng khi thanh toán</small></div>
          <Button label="Thêm địa chỉ" icon="pi pi-plus" size="small" @click="openNew" />
        </div>
        <div v-if="addresses.length" class="addr-grid">
          <div v-for="a in addresses" :key="a.id" class="addr-card" :class="{ 'is-default': a.isDefault }">
            <span class="addr-pin"><i class="pi pi-map-marker" /></span>
            <div class="addr-body">
              <div class="addr-top">
                <strong>{{ a.recipientName }}</strong>
                <span class="addr-phone">{{ a.phone }}</span>
                <Tag v-if="a.isDefault" value="Mặc định" severity="success" />
              </div>
              <div class="addr-line">{{ formatAddressLine(a) }}</div>
            </div>
            <div class="addr-actions">
              <Button icon="pi pi-pencil" text rounded size="small" aria-label="Sửa" @click="openEdit(a)" />
              <Button icon="pi pi-trash" text rounded size="small" severity="danger" aria-label="Xoá" @click="removeAddress(a)" />
            </div>
          </div>
        </div>
        <div v-else class="addr-empty">
          <i class="pi pi-map-marker" />
          <span>Chưa có địa chỉ nào — thêm địa chỉ để thanh toán nhanh hơn.</span>
        </div>
      </section>

      <!-- Đổi mật khẩu -->
      <section v-else-if="section === 'password'" class="surface-card">
        <h3>Đổi mật khẩu</h3>
        <div class="form">
          <label>Mật khẩu hiện tại</label>
          <Password v-model="pwd.currentPassword" :feedback="false" toggleMask fluid inputClass="w-full" />
          <label>Mật khẩu mới</label>
          <Password v-model="pwd.newPassword" toggleMask fluid inputClass="w-full" />
          <label>Xác nhận mật khẩu mới</label>
          <Password v-model="pwd.confirm" :feedback="false" toggleMask fluid inputClass="w-full" />
          <Button label="Đổi mật khẩu" :loading="savingPwd" class="mt-3" @click="changePassword" />
        </div>
      </section>

      <!-- Ví voucher -->
      <section v-else-if="section === 'vouchers'" class="surface-card">
        <h3>Ví voucher</h3>
        <p class="text-muted">Lưu mã và dùng khi thanh toán.</p>
        <div v-if="coupons.length" class="voucher-list">
          <div v-for="c in coupons" :key="c.id" class="voucher" :class="{ saved: isSaved(c.code) }">
            <div class="v-left">
              <div class="v-val">{{ c.discountType === 'Percentage' ? 'Giảm ' + c.discountValue + '%' : 'Giảm ' + formatCurrency(c.discountValue) }}</div>
              <div class="v-min">Đơn tối thiểu {{ formatCurrency(c.minOrderAmount) }}</div>
              <div class="v-code" @click="copyCode(c.code)">Mã: <b>{{ c.code }}</b> <i class="pi pi-copy" /></div>
            </div>
            <Button :label="isSaved(c.code) ? 'Bỏ lưu' : 'Lưu'" :outlined="isSaved(c.code)" size="small" @click="toggleSave(c)" />
          </div>
        </div>
        <p v-else class="text-muted">Hiện chưa có voucher khả dụng.</p>
      </section>
    </div>
  </div>

  <Dialog v-model:visible="showDialog" :header="editing.id ? 'Sửa địa chỉ' : 'Thêm địa chỉ'" modal style="width: 540px">
    <div class="form">
      <AddressFields v-model="editing" />
      <label class="chk"><Checkbox v-model="editing.isDefault" :binary="true" /> Đặt làm mặc định</label>
    </div>
    <template #footer>
      <Button label="Hủy" text @click="showDialog = false" />
      <Button label="Lưu" @click="saveAddress" />
    </template>
  </Dialog>
</template>

<style scoped>
.account { display: grid; grid-template-columns: 240px 1fr; gap: var(--sp-4); align-items: start; }
.side { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); padding: var(--sp-3); position: sticky; top: 84px; }
.user-chip { display: flex; align-items: center; gap: 10px; padding: var(--sp-3); border-bottom: 1px solid var(--border); margin-bottom: var(--sp-2); }
.avatar { width: 40px; height: 40px; border-radius: 50%; background: var(--brand); color: #fff; display: grid; place-items: center; font-weight: 700; text-transform: uppercase; overflow: hidden; flex-shrink: 0; }
.avatar img { width: 100%; height: 100%; object-fit: cover; }
.user-chip .text-muted { font-size: 12px; }
.side-item { display: flex; align-items: center; gap: 10px; width: 100%; background: none; border: none; padding: var(--sp-3); border-radius: var(--radius); cursor: pointer; font-family: inherit; font-size: 14px; color: var(--text-2); text-align: left; transition: background var(--ease), color var(--ease); }
.side-item:hover { background: var(--surface-2); }
.side-item.active { background: var(--brand-50); color: var(--brand); font-weight: 600; }
.content h3 { margin: 0 0 var(--sp-4); }

/* Ảnh đại diện */
.avatar-row { display: flex; align-items: center; gap: var(--sp-4); margin-bottom: var(--sp-4); }
.avatar-lg { position: relative; width: 96px; height: 96px; border-radius: 50%; flex-shrink: 0; background: var(--brand); color: #fff; display: grid; place-items: center; overflow: hidden; border: 3px solid var(--surface); box-shadow: var(--shadow-sm); }
.avatar-lg img { width: 100%; height: 100%; object-fit: cover; }
.avatar-initial { font-size: 2.2rem; font-weight: 700; text-transform: uppercase; }
.avatar-spin { position: absolute; inset: 0; display: grid; place-items: center; background: rgba(0,0,0,.35); color: #fff; font-size: 1.3rem; }
.avatar-meta { display: flex; flex-direction: column; gap: 8px; }
.avatar-actions { display: flex; align-items: center; gap: var(--sp-2); flex-wrap: wrap; }
.avatar-meta small { font-size: 12px; }

/* Ngăn cách hồ sơ ↔ sổ địa chỉ */
.sub-divider { height: 1px; background: var(--border); margin: var(--sp-5) 0 var(--sp-4); }
.sub-h { font-size: 1rem; }
.form { display: flex; flex-direction: column; gap: 0.4rem; max-width: 420px; }
.form label { font-weight: 600; font-size: 0.85rem; margin-top: 0.5rem; }
.head-row { display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: var(--sp-4); flex-wrap: wrap; gap: var(--sp-2); }
.head-row h3 { margin: 0 0 2px; }
.head-row small { font-size: 12px; }

/* Thẻ địa chỉ — mỗi địa chỉ 1 thẻ, có icon ghim, gọn gàng */
.addr-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: var(--sp-3); }
.addr-card { display: flex; align-items: flex-start; gap: var(--sp-3); background: var(--surface-2); border: 1px solid var(--border); border-radius: var(--radius); padding: var(--sp-3); transition: border-color var(--ease), box-shadow var(--ease); }
.addr-card:hover { border-color: var(--brand-100); box-shadow: var(--shadow-sm); }
.addr-card.is-default { border-color: var(--brand-100); background: var(--brand-50); }
.addr-pin { width: 34px; height: 34px; border-radius: 50%; flex-shrink: 0; display: grid; place-items: center; background: var(--surface); color: var(--brand); border: 1px solid var(--border); }
.addr-card.is-default .addr-pin { background: var(--brand); color: #fff; border-color: var(--brand); }
.addr-body { flex: 1; min-width: 0; }
.addr-top { display: flex; align-items: center; gap: var(--sp-2); flex-wrap: wrap; }
.addr-top strong { font-size: 0.92rem; }
.addr-phone { color: var(--text-2); font-size: 13px; }
.addr-line { color: var(--text-muted); font-size: 13px; margin-top: 3px; line-height: 1.45; }
.addr-actions { display: flex; gap: 2px; flex-shrink: 0; }
.addr-empty { display: flex; align-items: center; gap: var(--sp-3); padding: var(--sp-4); background: var(--surface-2); border: 1px dashed var(--border-strong); border-radius: var(--radius); color: var(--text-muted); font-size: 14px; }
.addr-empty .pi { font-size: 1.4rem; color: var(--brand); opacity: 0.7; }
.voucher-list { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: var(--sp-3); }
.voucher { display: flex; align-items: center; gap: var(--sp-3); border: 1px dashed var(--brand-100); border-radius: var(--radius); padding: var(--sp-3); transition: border-color var(--ease), background var(--ease); }
.voucher:hover { border-color: var(--brand); }
.voucher.saved { background: var(--brand-50); }
.v-left { flex: 1; }
.v-val { font-weight: 700; color: var(--brand); }
.v-min { font-size: 12px; color: var(--text-muted); }
.v-code { font-size: 13px; margin-top: 4px; cursor: pointer; }
.v-code .pi { font-size: 11px; color: var(--text-muted); }
.chk { display: flex; align-items: center; gap: 0.5rem; }
@media (max-width: 900px) {
  .account { grid-template-columns: 1fr; }
  .side { position: static; display: flex; overflow-x: auto; gap: var(--sp-2); }
  .user-chip { display: none; }
  .side-item { flex-shrink: 0; white-space: nowrap; }
}
</style>
