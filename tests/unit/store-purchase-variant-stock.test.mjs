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

test('checkout revalidates and submits variant and non-variant lines with their prices', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  const address = {id: '42', title: 'خانه', recipient: 'مشتری', phone: '09120000000', province: 'تهران', city: 'تهران', street: 'خیابان اول', postal: '1234567890'};
  const lines = [
    {id: '1', variantId: 11, salesPlanId: 'regular', quantity: 2, price: 1000, options: {priceOptions: [], accessories: []}},
    {id: '2', variantId: null, salesPlanId: 'regular', quantity: 3, price: 2000, options: {priceOptions: [], accessories: []}},
  ];
  await page.setContent(`<main class="invoice-page" data-address-id="42" data-payment-method="zarinpal"><div id="checkoutPanel"></div><div id="checkoutAddressPreview"></div><div id="checkoutPaymentMethod"></div><div id="cartView"><a class="cart-checkout-link" href="/Checkout">ادامه</a></div><div class="cart-address-section"></div><div class="cart-payment-section"></div><button id="finalPayment" disabled>ثبت</button><p id="paymentStatus"></p><p id="paymentMethodStatus"></p><section id="orderConfirmation" hidden><h2 id="orderConfirmationTitle"></h2><b id="confirmedOrderNumber"></b><span id="confirmedSubtotal"></span><span id="confirmedTotal"></span><div id="confirmedCouponRow" hidden><b id="confirmedCouponCode"></b><span id="confirmedDiscount"></span></div></section></main>`);
  await page.evaluate(({lines, address}) => {
    window.requests = [];
    const response = payload => ({ok: true, json: async () => payload});
    window.HNUI = {
      ready: callback => callback(), mount: (_name, _root, callback) => callback(),
      storage: {get: key => key === 'hn-delivery-addresses' ? JSON.stringify([address]) : '', remove() {}, hydrate() {}},
      cart: {read: () => lines, write: items => { window.cartAfterPurchase = items; }},
      auth: {isLoggedIn: () => true, requireLogin: () => true}, formatNumber: String,
      setMessage() {}, setMessageType() {},
      api: {request: async (url, options = {}) => {
        const body = options.body ? JSON.parse(options.body) : null;
        window.requests.push({url, method: options.method || 'GET', body});
        if (url === 'store/sales-plans/validate-cart') return response({valid: true, lines: body.lines.map(line => ({productId: line.productId, salesPlanId: line.salesPlanId, quote: {variantId: line.variantId, unitAmount: line.productId === 1 ? 1000 : 2000}}))});
        if (url === 'account/checkout') return response({id: 77, number: 'INV-77', subtotalAmount: 8000, totalAmount: 8000});
        if (url === 'account/state') return response({addresses: [address]});
        return response({});
      }},
    };
  }, {lines, address});
  await page.addScriptTag({path: 'Shop/wwwroot/shop/js/pages/checkout.js'});
  await page.waitForFunction(() => window.requests.some(item => item.url === 'store/sales-plans/validate-cart'));
  await page.waitForFunction(() => document.querySelector('#finalPayment')?.disabled === false);
  await page.locator('#finalPayment').click();
  await page.waitForFunction(() => window.requests.some(item => item.url === 'account/checkout'));

  const requests = await page.evaluate(() => window.requests);
  const validation = requests.find(item => item.url === 'store/sales-plans/validate-cart');
  const savedCart = requests.find(item => item.url === 'account/cart');
  const checkout = requests.find(item => item.url === 'account/checkout');
  assert.deepEqual(validation.body.lines.map(line => [line.productId, line.variantId, line.quantity]), [[1, 11, 2], [2, null, 3]]);
  assert.deepEqual(savedCart.body.items.map(line => [line.productId, line.variantId, line.quantity]), [[1, 11, 2], [2, null, 3]]);
  assert.deepEqual(checkout.body, {addressId: 42, paymentMethod: 'zarinpal', couponCode: null});
  assert.equal(await page.locator('#orderConfirmation').isVisible(), true);
  assert.equal(await page.locator('#confirmedOrderNumber').textContent(), 'INV-77');
});

test('checkout refuses invoice submission when the server reports insufficient variant stock', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  const address = {id: '42', title: 'خانه', recipient: 'مشتری', phone: '09120000000', province: 'تهران', city: 'تهران', street: 'خیابان اول', postal: '1234567890'};
  await page.setContent(`<main class="invoice-page" data-address-id="42" data-payment-method="zarinpal"><div id="checkoutAddressPreview"></div><div id="checkoutPaymentMethod"></div><div id="cartView"><a class="cart-checkout-link" href="/Checkout">ادامه</a></div><div class="cart-address-section"></div><div class="cart-payment-section"></div><button id="finalPayment">ثبت</button><p id="paymentStatus"></p><p id="paymentMethodStatus"></p></main>`);
  await page.evaluate(address => {
    window.requests = [];
    const line = {id: '1', variantId: 11, salesPlanId: 'regular', quantity: 2, price: 1000, options: {priceOptions: [], accessories: []}};
    window.HNUI = {
      ready: callback => callback(), mount: (_name, _root, callback) => callback(),
      storage: {get: key => key === 'hn-delivery-addresses' ? JSON.stringify([address]) : '', remove() {}, hydrate() {}},
      cart: {read: () => [line], write() {}}, auth: {isLoggedIn: () => false, requireLogin: () => true},
      formatNumber: String, setMessage: (node, message) => { if (node) node.textContent = message; }, setMessageType() {},
      api: {request: async (url, options = {}) => {
        window.requests.push({url, body: options.body ? JSON.parse(options.body) : null});
        if (url === 'store/sales-plans/validate-cart') return {ok: false, json: async () => ({code: 'insufficient-stock'})};
        return {ok: true, json: async () => ({})};
      }},
    };
  }, address);
  await page.addScriptTag({path: 'Shop/wwwroot/shop/js/pages/checkout.js'});
  await page.waitForFunction(() => window.requests.some(item => item.url === 'store/sales-plans/validate-cart'));
  await page.waitForTimeout(25);
  assert.equal(await page.locator('#finalPayment').isDisabled(), true);
  assert.equal(await page.evaluate(() => window.requests.some(item => item.url === 'account/cart' || item.url === 'account/checkout')), false);
  assert.match(await page.locator('#paymentStatus').textContent(), /موجودی کافی/u);
});
