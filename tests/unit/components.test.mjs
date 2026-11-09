import test from 'node:test';
import assert from 'node:assert/strict';
import {runtime} from '../helpers/runtime.mjs';

test('a module mounts only once per root and failed initialization can be retried', () => {
  const {ui} = runtime(); const root = {}; let count = 0;
  assert.equal(ui.mount('panel', root, () => count++), true);
  assert.equal(ui.mount('panel', root, () => count++), false);
  assert.equal(ui.mount('panel', {}, () => count++), true);
  assert.equal(count, 2);
  assert.throws(() => ui.mount('failed', root, () => { throw new Error('failed'); }));
  assert.equal(ui.mount('failed', root, () => count++), true);
});

test('loading shared modules again does not install duplicate global listeners or APIs', () => {
  const r = runtime(); const before = r.listeners.length; const api = r.window.HNUI;
  for (const name of ['core/storage.js', 'store/cart.js', 'core/ui-core.js', 'store/catalog.js', 'store/product-card.js']) r.load(name);
  assert.equal(r.listeners.length, before); assert.equal(r.window.HNUI, api);
});

test('shared events reject unknown names and invalid payloads', () => {
  const {ui, events} = runtime();
  assert.throws(() => ui.emit('hn:unknown'));
  assert.throws(() => ui.emit(ui.EVENTS.cartChange, {}));
  assert.throws(() => ui.emit(ui.EVENTS.authChange, 'true'));
  assert.throws(() => ui.emit(ui.EVENTS.cartAdded, {}));
  assert.throws(() => ui.emit(ui.EVENTS.cartStockLimit, {}));
  assert.throws(() => ui.emit(ui.EVENTS.authRequired, {}));
  ui.emit(ui.EVENTS.cartChange, []); assert.equal(events.at(-1).type, ui.EVENTS.cartChange);
});

test('shared API client uses the configured app path and same-origin JSON defaults', async () => {
  const {ui, window} = runtime(undefined, undefined, '/shop/api/');
  let request;
  window.fetch = async (url, options) => { request = {url, options}; return {ok: true}; };
  const response = await ui.api.request('store/sales-plans/quote', {
    method: 'POST', headers: {'Content-Type': 'application/json'}, body: '{}',
  });
  assert.equal(response.ok, true);
  assert.equal(request.url, 'https://shop.example/shop/api/store/sales-plans/quote');
  assert.equal(request.options.credentials, 'same-origin');
  assert.equal(request.options.headers.Accept, 'application/json');
  assert.equal(request.options.headers['Content-Type'], 'application/json');
  assert.equal(request.options.method, 'POST');
});

test('shared API client rejects origins and paths outside its configured base', async () => {
  const {ui, window} = runtime();
  let requestCount = 0;
  window.fetch = async () => { requestCount++; return {ok: true}; };
  await assert.rejects(ui.api.request('https://attacker.example/api/auth/email-code'), {name: 'TypeError'});
  await assert.rejects(ui.api.request('../outside'), {name: 'TypeError'});
  assert.equal(requestCount, 0);
  assert.throws(() => runtime(undefined, undefined, 'https://attacker.example/api/'), /same-origin/);
});

test('shared API client reports timeouts and does not retry requests', async () => {
  const {ui, window} = runtime();
  window.setTimeout = callback => { queueMicrotask(callback); return 1; };
  window.clearTimeout = () => {};
  let requestCount = 0;
  window.fetch = (_url, {signal}) => {
    requestCount++;
    return new Promise((resolve, reject) => signal.addEventListener('abort', () => reject(new Error('aborted')), {once: true}));
  };
  await assert.rejects(ui.api.request('auth/email-code', {method: 'POST', body: '{}'}), {name: 'TimeoutError'});
  assert.equal(requestCount, 1);
});

test('payment starts with a CSRF token and refuses an insecure gateway URL', async () => {
  const {ui, window} = runtime();
  const requests = [];
  window.fetch = async (url, options) => {
    requests.push({url, options});
    return {ok: true, json: async () => requests.length === 1 ? {token: 'csrf-token'} : {redirectUrl: 'http://gateway.example/pay'}};
  };
  await assert.rejects(ui.payment.start(123), /نشانی درگاه معتبر نیست/);
  assert.equal(requests.length, 2);
  assert.equal(requests[1].options.method, 'POST');
  assert.equal(requests[1].options.headers.RequestVerificationToken, 'csrf-token');
  assert.match(requests[1].url, /\/api\/payment\/orders\/123\/start$/);
});

test('all card contexts share escaped identity, price, media and action contracts', () => {
  const {window} = runtime(); const original = window.HNCatalog.find(1);
  const p = {...original, name: '<img src=x onerror=alert(1)>"', image: 'bad" onerror="bad'};
  for (const variant of ['grid', 'slider', 'search', 'related', 'favorite']) {
    const html = window.HNProductCard.render(p, {variant});
    assert.ok(html.includes(window.HNProductCard.price(p)));
    assert.ok(html.includes(window.HNProductUrl(p)));
    assert.ok(!html.includes('<img src=x')); assert.ok(html.includes('&lt;img'));
    assert.equal((html.match(/class="favorite"/g) || []).length, 1);
    assert.equal((html.match(/class="compare-trigger"/g) || []).length, 1);
    assert.ok(!html.includes('onerror="bad'));
    assert.ok(!html.includes('>Bosch<'));
    if (variant !== 'favorite') assert.ok(html.includes('data-id="1"'));
  }
});

test('sales-plan cards show their own amount, stock, URL and cart plan identity', () => {
  const {window} = runtime();
  const product = window.HNCatalog.find(1);
  const html = window.HNProductCard.render(product, {
    variant: 'slider', salesPlanId: 'digi-time', priceAmount: 25000000,
    discountPercent: 10, stockQuantity: 2, salesPlanName: 'دیجی‌تایم', salesPlanCode: 'DIGI-TIME', salesPlanLabel: 'پیشنهاد زمان‌دار',
  });
  assert.ok(html.includes('salesPlanId=digi-time'));
  assert.ok(html.includes('data-sales-plan-id="digi-time"'));
  assert.ok(html.includes('data-sales-plan-name="دیجی‌تایم"'));
  assert.ok(html.includes('data-price="22500000"'));
  assert.ok(html.includes(window.HNProductCard.price(product, {priceAmount: 25000000, discountPercent: 10})));
});
