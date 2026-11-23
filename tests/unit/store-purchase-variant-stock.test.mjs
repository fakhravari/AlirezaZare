import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('cart caps variant and non-variant quantities by their warehouse stock', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  const salesPlans = {
    plans: [{id: 'regular', isDefault: true}],
    offers: [
      {productId: 1, salesPlanId: 'regular', stockQuantity: 5, maxPurchaseQuantity: 99, isEnabled: true},
      {productId: 2, salesPlanId: 'regular', stockQuantity: 3, maxPurchaseQuantity: 99, isEnabled: true},
    ],
    inventoryVariants: [
      {productId: 1, variantId: 11, stockBySalesPlan: {regular: 2}},
      {productId: 1, variantId: 12, stockBySalesPlan: {regular: 0}},
    ],
  };
  await page.setContent(`<script id="storeSalesPlans" type="application/json">${JSON.stringify(salesPlans)}</script>`);
  await page.evaluate(() => { window.HNProducts = [
    {id: 1, name: 'کالای تنوع‌دار', image: 'images/variant.jpg', price: 100, maxCartQuantity: 99},
    {id: 2, name: 'کالای ساده', image: 'images/simple.jpg', price: 200, maxCartQuantity: 99},
  ]; });
  await page.addScriptTag({path: 'Shop/wwwroot/shop/js/store/cart.js'});
  const result = await page.evaluate(() => {
    const cart = window.HNCartModel.create({document, emit() {}, requireLogin: () => true, formatNumber: String, escapeHtml: String});
    const add = (id, variantId, quantity, unitAmount) => cart.add({
      id: String(id), name: id === 1 ? 'کالای تنوع‌دار' : 'کالای ساده', variantId,
      salesPlanId: 'regular', price: unitAmount, image: id === 1 ? 'images/variant.jpg' : 'images/simple.jpg',
      salesPlanSnapshot: {id: 'regular', unitAmount, currency: 'IRT'},
      options: {inventoryVariant: variantId ? {id: variantId, name: 'آبی'} : null, priceOptions: [], accessories: []},
    }, quantity);
    add(1, 11, 5, 1000);
    add(1, 12, 1, 1000);
    add(2, null, 5, 2000);
    return cart.read().map(item => ({id: item.id, variantId: item.variantId, quantity: item.quantity, price: item.price}));
  });
  assert.deepEqual(result, [
    {id: '1', variantId: 11, quantity: 2, price: 1000},
    {id: '2', variantId: null, quantity: 3, price: 2000},
  ]);
});

test('checkout displays the saved order and starts payment for that order', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.route('https://example.test/Checkout?orderId=77', route => route.fulfill({body: '<main class="invoice-page"><div id="checkoutAddressPreview"></div><div id="checkoutPaymentMethod"></div><div id="savedOrderItems"></div><div id="savedOrderSummary"></div><button id="finalPayment" disabled>پرداخت</button><p id="paymentStatus"></p></main>', contentType: 'text/html'}));
  await page.goto('https://example.test/Checkout?orderId=77');
  await page.evaluate(() => {
    const order = {id: 77, number: 'INV-77', status: 'pending-payment', paymentMethod: 'zarinpal', subtotalAmount: 8001, couponCode: 'OFF', couponDiscountAmount: 500, totalAmount: 7500, addressSnapshotJson: JSON.stringify({recipient: 'مشتری', province: 'تهران', city: 'تهران', street: 'خیابان اول', postal: '1234567890', phone: '09120000000'}), items: [{name: 'کالا', variantName: 'آبی', salesPlanName: 'طرح ویژه', quantity: 2, unitAmount: 4000, priceReferences: [{component: 'variant', calculatedUnitAmount: 3700, priceBeforeDiscountAmount: 4000, discountPercent: 7.5}, {component: 'accessory', name: 'متعلقه', calculatedUnitAmount: 300}]}]};
    window.HNUI = {ready: callback => callback(), mount: (_name, _root, callback) => callback(), storage: {get: () => '', hydrate() {}}, cart: {read: () => []}, auth: {isLoggedIn: () => true}, formatNumber: String, setMessage: (node, message) => {if (node) node.textContent = message;}, api: {request: async () => ({ok: true, json: async () => ({orders: [order], addresses: []})})}, payment: {start: async id => {window.paidOrderId = id; throw new Error('درگاه در دسترس نیست');}}};
  });
  await page.addScriptTag({path: 'Shop/wwwroot/shop/js/store/order-price-components.js'});
  await page.addScriptTag({path: 'Shop/wwwroot/shop/js/pages/checkout.js'});
  await page.waitForFunction(() => !document.querySelector('#finalPayment').disabled);
  assert.match(await page.locator('#savedOrderSummary').innerText(), /INV-77/);
  assert.match(await page.locator('#savedOrderSummary').innerText(), /7500/);
  assert.match(await page.locator('#savedOrderSummary').innerText(), /OFF/);
  assert.match(await page.locator('#savedOrderSummary').innerText(), /جمع جزئیات این سفارش با مبلغ ثبت‌شده اختلاف دارد/);
  assert.match(await page.locator('#savedOrderItems').innerText(), /متعلقه/);
  assert.match(await page.locator('#savedOrderItems').innerText(), /طرح ویژه/);
  assert.match(await page.locator('#savedOrderItems').innerText(), /پیش از تخفیف: 4000 تومان/);
  assert.match(await page.locator('#savedOrderItems').innerText(), /تخفیف 7.5٪: 300 تومان/);
  assert.match(await page.locator('#savedOrderItems').innerText(), /300 تومان × 2 = 600 تومان/);
  assert.match(await page.locator('#checkoutAddressPreview').innerText(), /خیابان اول/);
  await page.locator('#finalPayment').click();
  assert.equal(await page.evaluate(() => window.paidOrderId), 77);
  assert.match(await page.locator('#paymentStatus').innerText(), /درگاه در دسترس نیست/);
  assert.equal(await page.locator('#finalPayment').isEnabled(), true);
});
