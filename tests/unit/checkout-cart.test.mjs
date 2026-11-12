import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';
import {root} from '../helpers/runtime.mjs';
import path from 'node:path';

function checkoutCart() {
  let link;
  const makeLink = () => ({attributes: {}, classList: {toggle(name, value) { this[name] = value; }}, setAttribute(key, value) { this.attributes[key] = value; }});
  link = makeLink();
  const listeners = {};
  const address = {id:'home', title:'Home', recipient:'Test User', phone:'09123456789', province:'Test', street:'Street', postal:'1234567890'};
  const values = {'hn-delivery-addresses': JSON.stringify([address]), 'hn-selected-address':'home'};
  let items = [{id:'1', price:100, quantity:1, salesPlanId:'regular'}];
  const document = {querySelector: selector => selector === '#cartView .cart-checkout-link' ? link : null,
    querySelectorAll: () => [], addEventListener: (name, fn) => { listeners[name] = fn; }};
  const ui = {ready: fn => fn(), mount: (name, root, fn) => fn(), auth: {isLoggedIn: () => true},
    storage: {get: (key, fallback) => values[key] ?? fallback, set: (key, value) => {values[key] = value;}, hydrate: (key, value) => {values[key] = value; return true;}},
    api: {request: async () => ({ok: true, json: async () => ({addresses: [address], checkout: {selectedAddressId: "home", paymentMethod: "zarinpal"}})})},
    cart: {read: () => items}, setMessage() {}};
  const window = {HNUI: ui, addEventListener() {}};
  vm.runInNewContext(fs.readFileSync(path.join(root, 'Shop/wwwroot/shop/js/pages/checkout.js'), 'utf8'), {window, document, console});
  return {getLink: () => link, replaceLink: () => {link = makeLink();}, listeners, setItems: value => {items = value;}};
}

test('account address enables cart preview and regenerated link follows cart state', async () => {
  const page = checkoutCart();
  await new Promise(resolve => setImmediate(resolve));
  assert.equal(page.getLink().attributes['aria-disabled'], 'false');
  page.replaceLink();
  page.listeners['hn:cart-change']();
  assert.equal(page.getLink().attributes['aria-disabled'], 'false');
  page.setItems([]);
  page.listeners['hn:cart-change']();
  assert.equal(page.getLink().attributes['aria-disabled'], 'true');
});

function deferred() {
  let resolve;
  const promise = new Promise(done => { resolve = done; });
  return {promise, resolve};
}

function checkoutPayment() {
  const handlers = {}, requests = [];
  const address = {id:'1', title:'Home', recipient:'Test', phone:'09123456789', province:'Test', street:'Street', postal:'1234567890'};
  const values = {'hn-delivery-addresses': JSON.stringify([address]), 'hn-selected-address':'1'};
  const items = [{id:'1', price:100, quantity:1, salesPlanId:'regular'}];
  const payment = {disabled: true, addEventListener: (name, fn) => { handlers.payment = fn; }};
  const status = {};
  const document = {querySelector: selector => selector === '#finalPayment' ? payment : selector === '#paymentStatus' ? status : null,
    querySelectorAll: () => [], addEventListener: (name, fn) => { handlers[name] = fn; }};
  let loggedIn = true;
  const ui = {ready: fn => fn(), mount: (name, root, fn) => fn(), auth: {isLoggedIn: () => loggedIn},
    storage: {get: (key, fallback) => values[key] ?? fallback, set: (key, value) => {values[key] = value;}, hydrate: (key, value) => {values[key] = value; return true;}},
    cart: {read: () => items, write() {}}, setMessage: (element, message) => { if (element) element.message = message; },
    api: {request: (url, options) => {
      if (url === "account/state") return Promise.resolve({ok: true, json: async () => ({addresses: [address], checkout: {selectedAddressId: "1", paymentMethod: "zarinpal"}})});
      const pending = deferred(); requests.push({url, options, ...pending}); return pending.promise;
    }}};
  const window = {HNUI: ui, addEventListener() {}};
  vm.runInNewContext(fs.readFileSync(path.join(root, 'Shop/wwwroot/shop/js/pages/checkout.js'), 'utf8'), {window, document, console});
  return {handlers, requests, payment, status, logIn: () => {loggedIn = true;}};
}

const tick = () => new Promise(resolve => setImmediate(resolve));
const validQuote = {valid:true, lines:[{productId:1, salesPlanId:'regular', quote:{unitAmount:100}}]};

test('checkout reports a failed cart validation and keeps payment disabled', async () => {
  const page = checkoutPayment();
  page.requests[0].resolve({ok:false});
  await tick();
  assert.equal(page.payment.disabled, true);
  assert.match(page.status.message, /قیمت یا موجودی/);
});

test('checkout ignores an older validation whose response body finishes late', async () => {
  const page = checkoutPayment();
  const oldBody = deferred();
  page.requests[0].resolve({ok:true, json: () => oldBody.promise});
  await tick();
  page.handlers['hn:cart-change']();
  page.requests[1].resolve({ok:true, json: async () => validQuote});
  await tick();
  assert.equal(page.payment.disabled, false);
  oldBody.resolve({valid:false, lines:[]});
  await tick();
  assert.equal(page.payment.disabled, false);
});

test('checkout stays disabled while its order request is pending despite cart validation', async () => {
  const page = checkoutPayment();
  page.requests[0].resolve({ok:true, json: async () => validQuote});
  await tick();
  page.logIn();
  const submission = page.handlers.payment();
  assert.equal(page.payment.disabled, true);
  const order = page.requests.find(request => request.url === 'account/checkout');
  assert.ok(order);
  assert.equal(JSON.parse(order.options.body).lines[0].expectedUnitAmount, 100);
  page.handlers['hn:cart-change']();
  const validation = page.requests.filter(request => request.url === 'store/sales-plans/validate-cart').at(-1);
  validation.resolve({ok:true, json: async () => validQuote});
  await tick();
  assert.equal(page.payment.disabled, true);
  order.resolve({ok:false, json: async () => ({})});
  await submission;
  assert.equal(page.payment.disabled, false);
});

test('a price conflict invalidates a validation still in flight', async () => {
  const page = checkoutPayment();
  page.requests[0].resolve({ok:true, json: async () => validQuote});
  await tick();
  page.logIn();
  const submission = page.handlers.payment();
  const order = page.requests.find(request => request.url === 'account/checkout');
  page.handlers['hn:cart-change']();
  const validation = page.requests.filter(request => request.url === 'store/sales-plans/validate-cart').at(-1);
  order.resolve({ok:false, json: async () => ({code:'price-or-stock-changed'})});
  await submission;
  validation.resolve({ok:true, json: async () => validQuote});
  await tick();
  assert.equal(page.payment.disabled, true);
  assert.match(page.status.message, /قیمت یا موجودی/);
});

test('an oversized order keeps its explanation visible and permits editing', async () => {
  const page = checkoutPayment();
  page.requests[0].resolve({ok:true, json: async () => validQuote});
  await tick();
  page.logIn();
  const submission = page.handlers.payment();
  const order = page.requests.find(request => request.url === 'account/checkout');
  order.resolve({ok:false, json: async () => ({code:'order-amount-out-of-range'})});
  await submission;
  assert.match(page.status.message, /مبلغ سفارش بیش از سقف مجاز/);
  assert.equal(page.payment.disabled, false);
});
