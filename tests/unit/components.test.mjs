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
