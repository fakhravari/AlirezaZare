import test from 'node:test';
import assert from 'node:assert/strict';
import {runtime, source, plain} from '../helpers/runtime.mjs';
const key = 'hadi-norouzi-cart';
const loggedIn = (data, seed = {}) => runtime(data, {'hn-demo-auth': '1', ...seed});

test('required services are included exactly once, including after reload and legacy migration', () => {
  const r = loggedIn();
  const product = r.window.HNCatalog.find(1);
  const [item] = r.ui.cart.add({...product, price: 1});
  assert.equal(item.price, 24850000);
  assert.equal(item.options.accessories.filter(a => a.required && a.selected).length, 1);
  assert.equal(r.ui.cart.read()[0].price, 24850000);
  const reloaded = loggedIn(undefined, Object.fromEntries(r.values));
  assert.equal(reloaded.ui.cart.read()[0].price, 24850000);
  assert.equal(reloaded.ui.cart.add(product)[0].price, 24850000);
  assert.equal(reloaded.ui.cart.totals().price, 49700000);
});

test('variant and accessory prices use catalog values and ignore tampered duplicates', () => {
  const r = loggedIn();
  const p = r.window.HNCatalog.find(1);
  const [variantKey, feature] = Object.entries(p.variants)[0];
  const choice = feature.options.at(-1);
  const variant = {key: variantKey, label: 'fake', value: choice.label, productValue: choice.value, priceDelta: 999};
  const optional = p.accessories.find(a => !a.required);
  const [item] = r.ui.cart.add({...p, options: {
    variants: [variant, variant, {key: 'fake', value: 'fake', label: 'fake'}],
    accessories: [{...optional, selected: true, price: 999}, {...optional, selected: true}],
  }});
  assert.equal(item.price, Math.round((p.price + Number(choice.priceDelta || 0) + optional.price + .35) * 1e6));
  assert.equal(item.options.variants.filter(v => v.key === variantKey).length, 1);
  assert.equal(item.options.accessories.filter(a => a.name === optional.name).length, 1);
  assert.equal(item.name, p.name);
});

test('stock and per-product cart limits apply to add, updates and persisted rows', () => {
  const data = source(); data.products[0].stockQuantity = 3; data.products[0].maxCartQuantity = 2;
  const r = loggedIn(data);
  assert.equal(r.ui.cart.add(r.window.HNCatalog.find(1), 20)[0].quantity, 2);
  assert.equal(r.ui.cart.update(1, 30)[0].quantity, 2);
  assert.equal(r.ui.cart.add(r.window.HNCatalog.find(1), 1)[0].quantity, 2);
  assert.equal(r.events.filter(e => e.type === 'hn:cart-added').length, 1);
  assert.equal(r.events.filter(e => e.type === 'hn:cart-stock-limit').length, 3);
});

test('renaming an accessory preserves the selection through its stable ID', () => {
  const original = loggedIn(); const p = original.window.HNCatalog.find(1);
  const optional = p.accessories.find(a => !a.required);
  original.ui.cart.add({...p, options: {accessories: [{...optional, selected: true}]}});
  const changed = source();
  changed.products[0].accessories.find(a => a.id === optional.id).name = 'نام جدید متعلقه';
  const reloaded = loggedIn(changed, Object.fromEntries(original.values));
  const saved = reloaded.ui.cart.read()[0].options.accessories.find(a => a.id === optional.id);
  assert.equal(saved.selected, true); assert.equal(saved.name, 'نام جدید متعلقه');
  assert.equal(reloaded.ui.cart.read()[0].price, 26100000);
});

test('out-of-stock variant combinations cannot be added', () => {
  const data = source();
  const p = data.products[0];
  const combination = Object.entries(p.variants).map(([key, f]) => `${key}=${f.options[0].value}`).join('|');
  p.variantStock = {[combination]: 0};
  const r = loggedIn(data);
  assert.deepEqual(plain(r.ui.cart.add(r.window.HNCatalog.find(1))), []);
  assert.equal(r.events.some(e => e.type === 'hn:cart-added'), false);
});

test('new configurations replace the existing configuration of the same product code', () => {
  const r = loggedIn(); const p = r.window.HNCatalog.find(1);
  r.ui.cart.add(p, 2);
  const [key, f] = Object.entries(p.variants)[0]; const choice = f.options.at(-1);
  const items = r.ui.cart.add({...p, options: {variants: [{key, value: choice.label, productValue: choice.value}]}}, 1);
  assert.equal(items.length, 1); assert.equal(items[0].quantity, 1);
  assert.equal(items[0].options.variants[0].productValue, choice.value);
});

test('the same product in different sales plans stays in separate cart lines', () => {
  const r = loggedIn(); const p = r.window.HNCatalog.find(1);
  r.ui.cart.add({...p, salesPlanId: 'regular'}, 2);
  r.ui.cart.add({...p, salesPlanId: 'demo-time', price: 23058823}, 1);
  assert.equal(r.ui.cart.read().find(item => item.salesPlanId === 'demo-time').price, 23058823);
  let items = r.ui.cart.read();
  assert.equal(items.length, 2);
  assert.equal(items.map(item => item.salesPlanId).sort().join(','), 'demo-time,regular');
  assert.equal(r.events.filter(event => event.type === 'hn:cart-added').length, 2);
  assert.deepEqual(r.events.filter(event => event.type === 'hn:cart-added').map(event => event.detail.product.salesPlanId), ['regular', 'demo-time']);
  r.ui.cart.update('1', 2, 'demo-time');
  items = r.ui.cart.read();
  assert.equal(items.find(item => item.salesPlanId === 'regular').quantity, 2);
  assert.equal(items.find(item => item.salesPlanId === 'demo-time').quantity, 2);
  r.ui.cart.update('1', 0, 'demo-time');
  items = r.ui.cart.read();
  assert.equal(items.length, 1);
  assert.equal(items[0].salesPlanId, 'regular');
  r.ui.cart.add({...p, salesPlanId: 'demo-time'}, 1);
  r.ui.cart.remove('1', 'regular');
  items = r.ui.cart.read();
  assert.equal(items.length, 1);
  assert.equal(items[0].salesPlanId, 'demo-time');
});

test('future storage and failed writes cannot announce a successful cart addition', () => {
  for (const future of [true, false]) {
    const r = loggedIn(undefined, future ? {[key]: JSON.stringify({schemaVersion: 2, value: []})} : {});
    if (!future) r.window.localStorage.setItem = () => { throw new Error('quota'); };
    assert.deepEqual(plain(r.ui.cart.add(r.window.HNCatalog.find(1))), []);
    assert.equal(r.events.some(e => e.type === 'hn:cart-added'), false);
  }
});

test('anonymous additions request login without changing the cart', () => {
  const r = runtime(); assert.equal(r.ui.cart.add(r.window.HNCatalog.find(1)), null);
  assert.equal(r.values.has(key), false);
  assert.equal(r.events.at(-1).type, 'hn:auth-required');
});
