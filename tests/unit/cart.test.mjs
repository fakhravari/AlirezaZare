import test from 'node:test';
import assert from 'node:assert/strict';
import {runtime, source, plain} from '../helpers/runtime.mjs';
const key = 'hadi-norouzi-cart';
const loggedIn = (data, seed = {}) => {
  const catalog = data || source();
  const r = runtime(catalog, {'hn-demo-auth': '1', ...seed});
  const plans = {priceOptions: Object.fromEntries(catalog.products.map(product => [product.id,
    Object.entries(product.variants || {}).map(([key, feature]) => ({key, label: feature.label,
      values: feature.options.map(option => ({key: option.value, label: option.label, priceDelta: (option.priceDelta || 0) * 1e6}))}))]))};
  const query = r.document.querySelector;
  r.document.querySelector = selector => selector === '#storeSalesPlans' ? {textContent: JSON.stringify(plans)} : query(selector);
  return r;
};

test('cart accepts product images on the configured CDN and rejects other origins', () => {
  const data = source();
  data.media['product-1'].card = `https://cdn.example.test/${data.media['product-1'].card}`;
  const r = loggedIn(data);
  const query = r.document.querySelector;
  r.document.querySelector = selector => selector === 'meta[name="media-cdn-base-url"]'
    ? {content: 'https://cdn.example.test/'} : query(selector);
  assert.equal(r.ui.cart.add(r.window.HNCatalog.find(1)).length, 1);
  assert.match(r.ui.cart.read()[0].image, /^https:\/\/cdn\.example\.test\/content\/products\//);

  const bad = source();
  bad.media['product-1'].card = 'https://other.example.test/content/products/image.jpg';
  const other = loggedIn(bad);
  const otherQuery = other.document.querySelector;
  other.document.querySelector = selector => selector === 'meta[name="media-cdn-base-url"]'
    ? {content: 'https://cdn.example.test/'} : otherQuery(selector);
  assert.equal(other.ui.cart.add(other.window.HNCatalog.find(1)).length, 0);
});

test('required services are included exactly once and restored from the account API', () => {
  const r = loggedIn();
  const product = r.window.HNCatalog.find(1);
  const [item] = r.ui.cart.add({...product, price: 1});
  assert.equal(item.price, 24850000);
  assert.equal(item.options.accessories.filter(a => a.required && a.selected).length, 1);
  assert.equal(r.ui.cart.read()[0].price, 24850000);
  const saved = r.ui.cart.read()[0];
  const reloaded = loggedIn();
  assert.equal(reloaded.ui.cart.read().length, 0);
  reloaded.ui.cart.restoreAccountLines([{id: 1, quantity: 1, salesPlanId: 'regular', options: saved.options}]);
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
  assert.equal(item.options.priceOptions.filter(v => v.key === variantKey).length, 1);
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
  const accountLine = original.ui.cart.read()[0];
  const reloaded = loggedIn(changed);
  reloaded.ui.cart.restoreAccountLines([{id: 1, quantity: 1, salesPlanId: 'regular', options: accountLine.options}]);
  const saved = reloaded.ui.cart.read()[0].options.accessories.find(a => a.id === optional.id);
  assert.equal(saved.selected, true); assert.equal(saved.name, 'نام جدید متعلقه');
  assert.equal(reloaded.ui.cart.read()[0].price, 26100000);
});

test('out-of-stock warehouse variants cannot be added', () => {
  const r = loggedIn();
  const query = r.document.querySelector;
  r.document.querySelector = selector => selector === '#storeSalesPlans'
    ? {textContent: JSON.stringify({inventoryVariants: [{productId: 1, variantId: 42, stockBySalesPlan: {regular: 0}}]})}
    : query(selector);
  assert.deepEqual(plain(r.ui.cart.add({...r.window.HNCatalog.find(1), variantId: 42})), []);
  assert.equal(r.events.some(e => e.type === 'hn:cart-added'), false);
});

test('account cart restores distinct warehouse variants and avoids duplicate identities', () => {
  const r = loggedIn();
  const query = r.document.querySelector;
  r.document.querySelector = selector => selector === '#storeSalesPlans'
    ? {textContent: JSON.stringify({inventoryVariants: [42, 43].map(variantId => ({productId: 1, variantId, stockBySalesPlan: {regular: 5}}))})}
    : query(selector);
  const lines = [42, 43].map(variantId => ({id: 1, variantId, salesPlanId: 'regular', quantity: 1, options: {}}));
  assert.deepEqual(plain(r.ui.cart.restoreAccountLines([...lines, lines[0]]).map(item => item.variantId)), [42, 43]);
  assert.deepEqual(plain(r.ui.cart.restoreAccountLines(lines).map(item => item.variantId)), [42, 43]);
  assert.deepEqual(plain(r.ui.cart.read().map(item => item.variantId)), [42, 43]);
});

test('new configurations replace the existing configuration of the same product code', () => {
  const r = loggedIn(); const p = r.window.HNCatalog.find(1);
  r.ui.cart.add(p, 2);
  const [key, f] = Object.entries(p.variants)[0]; const choice = f.options.at(-1);
  const items = r.ui.cart.add({...p, options: {variants: [{key, value: choice.label, productValue: choice.value}]}}, 1);
  assert.equal(items.length, 1); assert.equal(items[0].quantity, 1);
  assert.equal(items[0].options.priceOptions[0].productValue, choice.value);
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

test('cart lines follow configured sales-plan type order and expose grouped labels', () => {
  const r = loggedIn();
  const querySelector = r.document.querySelector;
  const plans = {
    plans: [
      {id: 'regular', name: 'فروش عادی', type: 'regular', isDefault: true},
      {id: 'digi-time', name: 'دیجی‌تایم', type: 'digi-time'},
      {id: 'digi-af', name: 'دیجی‌اف', type: 'digi-af'},
    ],
  };
  r.document.querySelector = selector => selector === '#storeSalesPlans'
    ? {textContent: JSON.stringify(plans)}
    : querySelector(selector);
  const product = r.window.HNCatalog.find(1);
  r.ui.cart.add({...product, salesPlanId: 'regular'});
  r.ui.cart.add({...product, salesPlanId: 'digi-af', price: 20000000});
  r.ui.cart.add({...product, salesPlanId: 'digi-time', price: 23000000});

  const items = r.ui.cart.read();
  assert.deepEqual(JSON.parse(JSON.stringify(items.map(item => item.salesPlanId))), ['regular', 'digi-time', 'digi-af']);
  assert.deepEqual(JSON.parse(JSON.stringify(r.ui.cart.groupBySalesPlan(items).map(group => group.name))), ['فروش عادی', 'دیجی‌تایم', 'دیجی‌اف']);
  assert.equal(r.ui.cart.groupBySalesPlan(items).reduce((sum, group) => sum + group.items.length, 0), 3);
});

test('legacy browser cart data is discarded and never restored', () => {
  const future = JSON.stringify({schemaVersion: 2, value: []});
  const r = loggedIn(undefined, {[key]: future});
  r.window.localStorage.setItem = () => { throw new Error('quota'); };
  assert.equal(r.ui.storage.legacy(key), null);
  assert.equal(r.ui.cart.read().length, 0);
  assert.equal(r.ui.cart.add(r.window.HNCatalog.find(1)).length, 1);
  assert.equal(r.values.has(key), false);
});

test('anonymous additions request login without changing the cart', () => {
  const r = runtime(); assert.equal(r.ui.cart.add(r.window.HNCatalog.find(1)), null);
  assert.equal(r.values.has(key), false);
  assert.equal(r.events.at(-1).type, 'hn:auth-required');
});
