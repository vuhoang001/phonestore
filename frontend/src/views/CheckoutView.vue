<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'
import Select from 'primevue/select'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
import RadioButton from 'primevue/radiobutton'
import Dialog from 'primevue/dialog'
import Checkbox from 'primevue/checkbox'
import Accordion from 'primevue/accordion'
import AccordionPanel from 'primevue/accordionpanel'
import AccordionHeader from 'primevue/accordionheader'
import AccordionContent from 'primevue/accordioncontent'
import AddressFields from '@/components/AddressFields.vue'
import { addressApi, shippingApi, orderApi, couponApi, paymentApi } from '@/services'
import type { Address, ShippingMethod, Coupon } from '@/types'
import { useCartStore } from '@/stores/cart'
import { formatCurrency, formatAddressLine } from '@/composables/format'
import { extractError } from '@/services/api'

const router = useRouter()
const toast = useToast()
const cart = useCartStore()
const placeholder = 'https://placehold.co/80x80?text=%20'

const addresses = ref<Address[]>([])
const shippingMethods = ref<ShippingMethod[]>([])
const selectedAddressId = ref<number | null>(null)
const selectedShippingId = ref<number | null>(null)
const paymentMethod = ref('Cod')
const note = ref('')
const couponCode = ref('')
const appliedCode = ref('')        // mã đã áp thành công (để hiển thị)
const discount = ref(0)
const placing = ref(false)
const availableCoupons = ref<Coupon[]>([])   // mã đang có hiệu lực để gợi ý

// ----- Trả góp -----
const installmentMonths = ref<number>(12)   // kỳ hạn khi chọn "Trả góp" (6/9/12)
const installmentPlans = [6, 9, 12]

const showAddrDialog = ref(false)
const newAddr = ref<Partial<Address>>({ isDefault: false })

const shippingFee = computed(() => shippingMethods.value.find((s) => s.id === selectedShippingId.value)?.baseFee ?? 0)
const total = computed(() => Math.max(0, cart.selectedSubTotal - discount.value + shippingFee.value))

// Trả góp 0% lãi suất → mỗi tháng = tổng / số kỳ (ước tính, làm tròn).
const isInstallment = computed(() => paymentMethod.value === 'Installment')
const monthlyAmount = computed(() =>
  isInstallment.value && installmentMonths.value > 0 ? Math.round(total.value / installmentMonths.value) : 0)

// Tóm tắt lựa chọn để hiện ở tiêu đề accordion khi thu gọn
const selectedAddress = computed(() => addresses.value.find((a) => a.id === selectedAddressId.value) || null)
const selectedShipping = computed(() => shippingMethods.value.find((s) => s.id === selectedShippingId.value) || null)
const selectedPayment = computed(() => paymentOptions.find((p) => p.value === paymentMethod.value) || null)
// Mục accordion đang mở (mở sẵn địa chỉ vì thường cần chọn trước)
const openPanels = ref<string[]>(['addr'])

// Chỉ 3 phương thức: COD / VNPAY / Trả góp (đặc thù điện thoại).
const paymentOptions = [
  { label: 'Thanh toán khi nhận hàng (COD)', value: 'Cod', icon: 'pi-money-bill', desc: 'Trả tiền mặt khi nhận hàng' },
  { label: 'Chuyển khoản (QR / Internet Banking)', value: 'VnPay', icon: 'pi-qrcode', desc: 'Thanh toán qua cổng VNPAY' },
  { label: 'Mua trả góp 0% lãi suất', value: 'Installment', icon: 'pi-calendar', desc: 'Chia nhỏ 6 / 9 / 12 tháng' }
]

async function loadData() {
  await cart.fetch()
  const [addr, ship, coups] = await Promise.all([
    addressApi.mine(), shippingApi.methods(), couponApi.available().catch(() => [])
  ])
  addresses.value = addr
  shippingMethods.value = ship
  availableCoupons.value = coups
  selectedAddressId.value = addr.find((a) => a.isDefault)?.id ?? addr[0]?.id ?? null
  selectedShippingId.value = ship[0]?.id ?? null
}

async function saveAddress() {
  try {
    const created = await addressApi.create(newAddr.value)
    addresses.value = await addressApi.mine()
    selectedAddressId.value = created.id
    showAddrDialog.value = false
    newAddr.value = { isDefault: false }
    toast.add({ severity: 'success', summary: 'Đã thêm địa chỉ', life: 2000 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Lỗi', detail: extractError(e), life: 3000 })
  }
}

async function applyCoupon() {
  if (!couponCode.value) return
  try {
    const c = await couponApi.validate(couponCode.value, cart.selectedSubTotal)
    discount.value = c.discountType === 'Percentage'
      ? Math.round(cart.selectedSubTotal * c.discountValue / 100)
      : c.discountValue
    appliedCode.value = c.code
    toast.add({ severity: 'success', summary: 'Áp mã thành công', detail: `Giảm ${formatCurrency(discount.value)}`, life: 2500 })
  } catch (e) {
    discount.value = 0; appliedCode.value = ''
    toast.add({ severity: 'warn', summary: 'Mã không hợp lệ', detail: extractError(e), life: 3000 })
  }
}
function clearCoupon() { couponCode.value = ''; appliedCode.value = ''; discount.value = 0 }
// Nhãn hiển thị mức giảm của mã
function couponLabel(c: Coupon) {
  return c.discountType === 'Percentage' ? `Giảm ${c.discountValue}%` : `Giảm ${formatCurrency(c.discountValue)}`
}

// ----- Popup chọn khuyến mãi -----
const showCouponDialog = ref(false)
const pickedCode = ref('')

// Tính mức giảm thực tế + điều kiện cho từng mã theo đơn hiện tại
const couponCalc = computed(() =>
  availableCoupons.value.map((c) => {
    const eligible = cart.selectedSubTotal >= c.minOrderAmount
    const raw = c.discountType === 'Percentage'
      ? Math.round(cart.selectedSubTotal * c.discountValue / 100)
      : c.discountValue
    return { coupon: c, eligible, discount: eligible ? raw : 0 }
  })
)
// Gợi ý: mã đủ điều kiện cho mức giảm CAO NHẤT
const bestCode = computed(() => {
  const ok = couponCalc.value.filter((x) => x.eligible)
  if (!ok.length) return ''
  return ok.reduce((a, b) => (b.discount > a.discount ? b : a)).coupon.code
})

function openCouponDialog() {
  pickedCode.value = appliedCode.value || bestCode.value
  showCouponDialog.value = true
}
async function applyFromInput() {
  await applyCoupon()
  if (appliedCode.value) showCouponDialog.value = false
}
async function confirmPick() {
  if (!pickedCode.value) return
  couponCode.value = pickedCode.value
  await applyCoupon()
  if (appliedCode.value) showCouponDialog.value = false
}

// Khi đổi sang trả góp, mở sẵn panel thanh toán để người dùng thấy chọn kỳ hạn.
watch(paymentMethod, (m) => {
  if (m === 'Installment' && !openPanels.value.includes('pay')) openPanels.value.push('pay')
})

async function placeOrder() {
  if (!selectedAddressId.value) {
    toast.add({ severity: 'warn', summary: 'Vui lòng chọn địa chỉ giao hàng', life: 3000 })
    return
  }
  placing.value = true
  try {
    const order = await orderApi.create({
      addressId: selectedAddressId.value,
      shippingMethodId: selectedShippingId.value ?? undefined,
      paymentMethod: paymentMethod.value,
      couponCode: couponCode.value || undefined,
      note: note.value || undefined,
      cartItemIds: cart.selectedItems.map((i) => i.id),
      // Chỉ gửi kỳ hạn khi mua trả góp.
      installmentMonths: isInstallment.value ? installmentMonths.value : undefined
    })
    await cart.fetch()

    // Thanh toán online: lấy URL cổng VNPAY rồi chuyển hướng người dùng sang đó.
    if (paymentMethod.value === 'VnPay') {
      const { paymentUrl } = await paymentApi.vnpay(order.id)
      window.location.href = paymentUrl
      return
    }

    toast.add({ severity: 'success', summary: 'Đặt hàng thành công!', detail: order.orderCode, life: 3000 })
    router.push(`/orders/${order.id}`)
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Đặt hàng thất bại', detail: extractError(e), life: 4000 })
  } finally {
    placing.value = false
  }
}

onMounted(loadData)
</script>

<template>
  <h1 class="section-title">Thanh toán</h1>
  <div v-if="cart.selectedItems.length" class="checkout">
    <div class="col-main">
      <Accordion v-model:value="openPanels" multiple class="ck-acc">
        <!-- Địa chỉ -->
        <AccordionPanel value="addr">
          <AccordionHeader>
            <span class="acc-h">
              <span class="acc-title"><i class="pi pi-map-marker" /> Địa chỉ giao hàng</span>
              <span class="acc-sum">{{ selectedAddress ? selectedAddress.recipientName + ' · ' + selectedAddress.phone : 'Chưa chọn địa chỉ' }}</span>
            </span>
          </AccordionHeader>
          <AccordionContent>
            <div v-if="addresses.length" class="acc-actions">
              <Button label="Thêm địa chỉ" icon="pi pi-plus" size="small" text @click="showAddrDialog = true" />
            </div>
            <div v-if="addresses.length" class="addr-list">
              <label v-for="a in addresses" :key="a.id" class="addr-item" :class="{ active: selectedAddressId === a.id }">
                <RadioButton v-model="selectedAddressId" :value="a.id" />
                <div class="pick-main">
                  <strong>{{ a.recipientName }} · {{ a.phone }}</strong>
                  <small>{{ formatAddressLine(a) }}</small>
                </div>
              </label>
            </div>
            <div v-else class="addr-empty">
              <i class="pi pi-map-marker" />
              <p>Chưa có địa chỉ giao hàng.</p>
              <Button label="Thêm địa chỉ" icon="pi pi-plus" size="small" @click="showAddrDialog = true" />
            </div>
          </AccordionContent>
        </AccordionPanel>

        <!-- Vận chuyển -->
        <AccordionPanel value="ship">
          <AccordionHeader>
            <span class="acc-h">
              <span class="acc-title"><i class="pi pi-truck" /> Phương thức vận chuyển</span>
              <span class="acc-sum">{{ selectedShipping ? selectedShipping.name + ' · ' + formatCurrency(selectedShipping.baseFee) : 'Chưa chọn' }}</span>
            </span>
          </AccordionHeader>
          <AccordionContent>
            <label v-for="s in shippingMethods" :key="s.id" class="pick" :class="{ active: selectedShippingId === s.id }">
              <RadioButton v-model="selectedShippingId" :value="s.id" />
              <span class="pick-main"><strong>{{ s.name }}</strong><small>Dự kiến {{ s.estimatedDays }} ngày</small></span>
              <span class="price">{{ formatCurrency(s.baseFee) }}</span>
            </label>
          </AccordionContent>
        </AccordionPanel>

        <!-- Thanh toán -->
        <AccordionPanel value="pay">
          <AccordionHeader>
            <span class="acc-h">
              <span class="acc-title"><i class="pi pi-credit-card" /> Phương thức thanh toán</span>
              <span class="acc-sum">{{ selectedPayment ? selectedPayment.label : 'Chưa chọn' }}</span>
            </span>
          </AccordionHeader>
          <AccordionContent>
            <label v-for="p in paymentOptions" :key="p.value" class="pick" :class="{ active: paymentMethod === p.value }">
              <RadioButton v-model="paymentMethod" :value="p.value" />
              <span class="pay-ic"><i class="pi" :class="p.icon" /></span>
              <span class="pick-main"><strong>{{ p.label }}</strong><small>{{ p.desc }}</small></span>
            </label>

            <!-- Chọn kỳ hạn trả góp khi phương thức là Trả góp -->
            <div v-if="isInstallment" class="inst-panel">
              <div class="inst-panel-head"><i class="pi pi-calendar" /> Chọn kỳ hạn trả góp</div>
              <div class="inst-plans">
                <button v-for="m in installmentPlans" :key="m" class="inst-plan"
                  :class="{ active: installmentMonths === m }" @click="installmentMonths = m">
                  <span class="ip-mo">{{ m }} tháng</span>
                  <span class="ip-price">{{ formatCurrency(Math.round(total / m)) }}<small>/tháng</small></span>
                </button>
              </div>
              <p class="inst-panel-hint">Trả góp 0% lãi suất · số tiền/tháng là ước tính theo tổng đơn hiện tại.</p>
            </div>
          </AccordionContent>
        </AccordionPanel>

      </Accordion>

      <!-- Chọn khuyến mãi (cùng cột với địa chỉ/vận chuyển/thanh toán) -->
      <button class="cp-trigger" @click="openCouponDialog">
        <span class="cpt-left"><i class="pi pi-ticket" />
          <template v-if="appliedCode">Mã giảm giá: <b>{{ appliedCode }}</b></template>
          <template v-else>Chọn hoặc nhập mã giảm giá</template>
        </span>
        <span class="cpt-right">
          <em v-if="discount">-{{ formatCurrency(discount) }}</em>
          <i class="pi pi-angle-right" />
        </span>
      </button>

      <!-- Ghi chú cho người bán — đặt cuối cột phải để hai cột cân đối -->
      <div class="note-card">
        <label class="nc-label"><i class="pi pi-pencil" /> Ghi chú cho người bán</label>
        <Textarea v-model="note" rows="2" placeholder="Lời nhắn cho người bán (tuỳ chọn)…" class="w-full nc-ta" autoResize />
      </div>
    </div>

    <!-- Tóm tắt -->
    <aside class="col-summary">
      <div class="box">
        <h3><i class="pi pi-shopping-bag sec-ic" /> Đơn hàng ({{ cart.selectedCount }})</h3>
        <div class="items-list">
          <div v-for="item in cart.selectedItems" :key="item.id" class="mini-item">
            <span class="mi-thumb">
              <img :src="item.imageUrl || placeholder" :alt="item.productName" loading="lazy" />
              <span class="mi-qty">{{ item.quantity }}</span>
            </span>
            <span class="mi-info">
              <span class="mi-name">{{ item.productName }}</span>
              <span v-if="item.variantInfo" class="mi-variant">{{ item.variantInfo }}</span>
            </span>
            <span class="mi-price">{{ formatCurrency(item.lineTotal) }}</span>
          </div>
        </div>
        <hr />

        <div class="row"><span>Tạm tính</span><span>{{ formatCurrency(cart.selectedSubTotal) }}</span></div>
        <div class="row" v-if="discount"><span>Giảm giá</span><span class="minus">-{{ formatCurrency(discount) }}</span></div>
        <div class="row"><span>Phí vận chuyển</span><span>{{ formatCurrency(shippingFee) }}</span></div>
        <hr />
        <div class="row total"><span>Tổng cộng</span><span class="price">{{ formatCurrency(total) }}</span></div>
        <!-- Dòng trả góp khi chọn phương thức Trả góp -->
        <div v-if="isInstallment" class="inst-summary">
          <i class="pi pi-calendar" /> Trả góp {{ installmentMonths }} tháng ·
          <strong>{{ formatCurrency(monthlyAmount) }}</strong>/tháng
        </div>
        <Button label="Đặt hàng" icon="pi pi-check" class="w-full mt-3 place-btn" :loading="placing" @click="placeOrder" />
        <p class="secure-note"><i class="pi pi-lock" /> Thanh toán an toàn & bảo mật</p>
      </div>
    </aside>
  </div>
  <div v-else class="text-muted">Giỏ hàng trống. <router-link to="/products" class="link">Mua sắm ngay</router-link></div>

  <!-- Dialog thêm địa chỉ -->
  <Dialog v-model:visible="showAddrDialog" header="Thêm địa chỉ" modal style="width: 540px">
    <div class="form-grid">
      <AddressFields v-model="newAddr" />
      <label class="chk"><Checkbox v-model="newAddr.isDefault" :binary="true" /> Đặt làm mặc định</label>
    </div>
    <template #footer>
      <Button label="Hủy" text @click="showAddrDialog = false" />
      <Button label="Lưu" @click="saveAddress" />
    </template>
  </Dialog>

  <!-- Dialog chọn khuyến mãi -->
  <Dialog v-model:visible="showCouponDialog" header="Chọn Mã Giảm Giá" modal :style="{ width: '460px' }" class="coupon-dialog">
    <div class="cd-input">
      <InputText v-model="couponCode" placeholder="Nhập mã giảm giá" class="w-full" @keyup.enter="applyFromInput" />
      <Button label="Áp dụng" outlined @click="applyFromInput" />
    </div>

    <p v-if="bestCode" class="cd-rcm"><i class="pi pi-star-fill" /> Gợi ý: dùng <b>{{ bestCode }}</b> để được giảm nhiều nhất.</p>

    <div class="cd-list">
      <label v-for="cc in couponCalc" :key="cc.coupon.id" class="cd-item"
        :class="{ active: pickedCode === cc.coupon.code, disabled: !cc.eligible }">
        <RadioButton v-model="pickedCode" :value="cc.coupon.code" :disabled="!cc.eligible" />
        <span class="cd-ticket"><i class="pi pi-ticket" /></span>
        <span class="cd-body">
          <span class="cd-top">
            <b class="cd-code">{{ cc.coupon.code }}</b>
            <span v-if="cc.coupon.code === bestCode" class="cd-best">Nên chọn</span>
          </span>
          <span class="cd-desc">{{ couponLabel(cc.coupon) }} · Đơn tối thiểu {{ formatCurrency(cc.coupon.minOrderAmount) }}</span>
          <span v-if="cc.eligible" class="cd-save">Áp vào đơn này: giảm {{ formatCurrency(cc.discount) }}</span>
          <span v-else class="cd-need">Mua thêm {{ formatCurrency(cc.coupon.minOrderAmount - cart.selectedSubTotal) }} để dùng</span>
        </span>
      </label>
      <div v-if="!couponCalc.length" class="cd-empty">Chưa có mã giảm giá khả dụng.</div>
    </div>

    <template #footer>
      <Button label="Không dùng mã" text @click="clearCoupon(); showCouponDialog = false" />
      <Button label="Áp dụng" icon="pi pi-check" :disabled="!pickedCode" @click="confirmPick" />
    </template>
  </Dialog>
</template>

<style scoped>
.checkout { display: grid; grid-template-columns: minmax(0, 1fr) 420px; gap: var(--sp-5); align-items: start; }
/* Đảo bên: Đơn hàng làm cột chính bên TRÁI, form (địa chỉ/vận chuyển/thanh toán) bên PHẢI */
.col-summary { order: 1; }
.col-main { order: 2; }
.box { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); padding: var(--sp-5); margin-bottom: var(--sp-4); }
.box-head { display: flex; justify-content: space-between; align-items: center; }
h3 { margin: 0 0 var(--sp-4); display: flex; align-items: center; gap: 8px; font-size: 1rem; }
.sec-ic { color: var(--brand); font-size: 14px; }

/* Accordion form: mỗi mục 1 thẻ, bấm tiêu đề để xổ ra/thu vào */
.ck-acc :deep(.p-accordionpanel) { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); margin-bottom: var(--sp-3); overflow: hidden; }
.ck-acc :deep(.p-accordionheader) { padding: var(--sp-3) var(--sp-4); align-items: center; }
.ck-acc :deep(.p-accordionpanel.p-accordionpanel-active .p-accordionheader) { padding-bottom: var(--sp-2); }
.ck-acc :deep(.p-accordioncontent-content) { padding: 0 var(--sp-4) var(--sp-3); background: transparent; }
.ck-acc :deep(.p-accordionheader-toggle-icon) { color: var(--text-muted); }
.acc-h { display: flex; flex-direction: column; gap: 2px; text-align: left; min-width: 0; }
.acc-title { font-weight: 600; font-size: 14px; display: inline-flex; align-items: center; gap: 8px; }
.acc-title .pi { color: var(--brand); font-size: 13px; }
.acc-sum { font-size: 12px; color: var(--text-muted); margin-left: 21px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.acc-actions { display: flex; justify-content: flex-end; margin-bottom: var(--sp-2); }

/* Ô chọn (địa chỉ / vận chuyển / thanh toán) — gọn */
.addr-item, .pick { display: flex; align-items: center; gap: var(--sp-2); padding: 8px 10px; border: 1px solid var(--border); border-radius: var(--radius); margin-bottom: 6px; cursor: pointer; transition: border-color var(--ease), background var(--ease); }
.addr-item:last-child, .pick:last-child { margin-bottom: 0; }
.addr-item:hover, .pick:hover { border-color: var(--brand-100); background: var(--surface-2); }
.addr-item.active, .pick.active { border-color: var(--brand); background: var(--brand-50); }
.pick-main { display: flex; flex-direction: column; gap: 0; min-width: 0; }
.pick-main strong { font-size: 13px; font-weight: 600; }
.pick-main small { font-size: 11px; color: var(--text-muted); }
.pick .price { margin-left: auto; font-size: 13px; white-space: nowrap; }
.pay-ic { width: 26px; height: 26px; border-radius: var(--radius-sm); background: var(--brand-50); color: var(--brand); display: grid; place-items: center; flex-shrink: 0; }
.pay-ic .pi { font-size: 12px; }
/* Radio nhỏ lại cho gọn trong accordion */
.ck-acc :deep(.p-radiobutton) { width: 18px; height: 18px; }
.ck-acc :deep(.p-radiobutton-box) { width: 18px; height: 18px; }

/* Khối chọn kỳ hạn trả góp */
.inst-panel { margin-top: var(--sp-3); border: 1px solid var(--brand-100); background: var(--brand-50); border-radius: var(--radius); padding: var(--sp-3); }
.inst-panel-head { display: flex; align-items: center; gap: 6px; font-size: 13px; font-weight: 700; color: var(--brand-dark); margin-bottom: var(--sp-3); }
.inst-panel-head .pi { color: var(--brand); }
.inst-plans { display: grid; grid-template-columns: repeat(3, 1fr); gap: var(--sp-2); }
.inst-plan { display: flex; flex-direction: column; gap: 2px; align-items: center; background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-sm); padding: 8px 6px; cursor: pointer; font-family: inherit; transition: all var(--ease); }
.inst-plan:hover { border-color: var(--brand); }
.inst-plan.active { border-color: var(--brand); background: var(--brand-100); }
.ip-mo { font-size: 12px; font-weight: 600; color: var(--text-2); }
.ip-price { font-size: 13px; font-weight: 800; color: var(--price); }
.ip-price small { font-size: 10px; font-weight: 500; color: var(--text-muted); }
.inst-panel-hint { margin: var(--sp-3) 0 0; font-size: 11.5px; color: var(--text-2); }

.col-summary .box { position: sticky; top: 80px; }
.items-list { display: flex; flex-direction: column; gap: var(--sp-3); max-height: 300px; overflow-y: auto; padding-right: 4px; }
/* Dòng sản phẩm: [ảnh + badge SL] | Tên/biến thể | Giá */
.mini-item { display: grid; grid-template-columns: auto 1fr auto; gap: var(--sp-3); align-items: center; font-size: 14px; }
.mi-thumb { position: relative; width: 44px; height: 44px; flex-shrink: 0; }
.mi-thumb img { width: 100%; height: 100%; object-fit: contain; border-radius: var(--radius-sm); border: 1px solid var(--border); background: #fff; }
.mi-qty { position: absolute; top: -7px; right: -7px; min-width: 20px; height: 20px; padding: 0 5px; display: inline-grid; place-items: center; background: var(--brand); color: #fff; border: 2px solid var(--surface); border-radius: var(--radius-pill); font-weight: 700; font-size: 11px; line-height: 1; }
.mi-info { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.mi-name { color: var(--text); font-weight: 600; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.mi-variant { font-size: 11px; color: var(--text-muted); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.mi-price { font-weight: 700; color: var(--text); white-space: nowrap; }

/* Nút mở popup chọn khuyến mãi — thẻ riêng trong cột phải */
.cp-trigger { width: 100%; display: flex; align-items: center; justify-content: space-between; gap: var(--sp-2); background: var(--brand-50); border: 1px dashed var(--brand-100); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); padding: var(--sp-3) var(--sp-4); cursor: pointer; font-family: inherit; font-size: 13px; color: var(--text); transition: border-color var(--ease); }
.cp-trigger:hover { border-color: var(--brand); }
.cpt-left { display: inline-flex; align-items: center; gap: 8px; }
.cpt-left .pi { color: var(--brand); }
.cpt-right { display: inline-flex; align-items: center; gap: 6px; color: var(--text-muted); }
.cpt-right em { color: var(--success); font-style: normal; font-weight: 700; font-size: 13px; }

/* Dialog chọn khuyến mãi */
.cd-input { display: flex; gap: var(--sp-2); margin-bottom: var(--sp-3); }
.cd-input .w-full { flex: 1; min-width: 0; }
.cd-rcm { display: flex; align-items: center; gap: 6px; background: var(--brand-50); border-radius: var(--radius-sm); padding: 8px 12px; margin: 0 0 var(--sp-3); font-size: 12.5px; color: var(--brand-dark); }
.cd-rcm .pi { color: var(--brand); }
.cd-list { display: flex; flex-direction: column; gap: var(--sp-2); max-height: 340px; overflow-y: auto; }
.cd-item { display: flex; align-items: center; gap: var(--sp-3); border: 1px solid var(--border); border-radius: var(--radius); padding: var(--sp-3); cursor: pointer; transition: border-color var(--ease), background var(--ease); }
.cd-item:hover:not(.disabled) { border-color: var(--brand-100); }
.cd-item.active { border-color: var(--brand); background: var(--brand-50); }
.cd-item.disabled { opacity: 0.55; cursor: not-allowed; }
.cd-ticket { width: 40px; height: 40px; flex-shrink: 0; border-radius: var(--radius-sm); background: linear-gradient(135deg, var(--brand), var(--brand-light)); color: #fff; display: grid; place-items: center; }
.cd-ticket .pi { font-size: 17px; }
.cd-body { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.cd-top { display: flex; align-items: center; gap: 8px; }
.cd-code { font-size: 14px; font-weight: 700; }
.cd-best { font-size: 10px; font-weight: 700; color: #fff; background: var(--brand); padding: 1px 7px; border-radius: var(--radius-pill); }
.cd-desc { font-size: 12px; color: var(--text-muted); }
.cd-save { font-size: 12.5px; color: var(--success); font-weight: 600; }
.cd-need { font-size: 12px; color: var(--text-muted); font-style: italic; }
.cd-empty { text-align: center; color: var(--text-muted); padding: var(--sp-5); font-size: 13px; }

/* Thẻ ghi chú — nằm cuối cột phải, cùng phong cách thẻ với accordion/coupon */
.note-card { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); padding: var(--sp-3) var(--sp-4); margin-top: var(--sp-3); display: flex; flex-direction: column; gap: 8px; }
.note-card .nc-label { font-size: 13px; font-weight: 600; color: var(--text-2); display: inline-flex; align-items: center; gap: 6px; }
.note-card .nc-label .pi { color: var(--brand); font-size: 12px; }
.note-card :deep(.p-textarea) { font-size: 13px; padding: 8px 10px; min-height: 56px; }
.note-card :deep(.p-textarea)::placeholder { font-size: 13px; color: var(--text-muted); }

.row { display: flex; justify-content: space-between; margin: 6px 0; font-size: 14px; }
.row .minus { color: var(--success); }
.total { font-size: 1.15rem; font-weight: 800; margin-top: 4px; }
.inst-summary { display: flex; align-items: center; gap: 5px; margin-top: var(--sp-2); padding: 8px 10px; background: var(--brand-50); border-radius: var(--radius-sm); font-size: 12.5px; color: var(--brand-dark); }
.inst-summary .pi { color: var(--brand); font-size: 12px; }
.inst-summary strong { color: var(--price); }
.place-btn { margin-top: var(--sp-3); }
.secure-note { display: flex; align-items: center; justify-content: center; gap: 6px; margin: var(--sp-3) 0 0; font-size: 12px; color: var(--text-muted); }

.addr-empty { display: flex; flex-direction: column; align-items: center; gap: var(--sp-2); padding: var(--sp-5) var(--sp-4); color: var(--text-muted); text-align: center; }
.addr-empty .pi { font-size: 2rem; color: #d1d5db; }
.addr-empty p { margin: 0; font-size: 13px; }

.form-grid { display: flex; flex-direction: column; gap: 0.75rem; }
.chk { display: flex; align-items: center; gap: 0.5rem; }
.link { color: var(--brand); }
@media (max-width: 900px) {
  .checkout { grid-template-columns: 1fr; }
  /* Mobile: form (địa chỉ/vận chuyển/thanh toán) lên trước, đơn hàng + nút đặt xuống cuối */
  .col-main { order: 1; }
  .col-summary { order: 2; }
  .col-summary .box { position: static; }
}
</style>
