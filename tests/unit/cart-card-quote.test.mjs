import test from 'node:test';
import assert from 'node:assert/strict';
import {runtime} from '../helpers/runtime.mjs';

function cardRuntime() {
  const r = runtime(undefined, {'hn-demo-auth': '1'});
  const product = r.window.HNCatalog.find(1);
  const button = {
    dataset: {id: '1', name: product.name, price: '23058823', image: product.image,
      salesPlanId: 'digi-time', salesPlanLabel: '', stockQuantity: '4', maxPurchaseQuantity: '2'},
    classList: {add() {}, remove() {}}, querySelector() { return null; }, append() {},
  };
  r.document.createElement = () => ({classList: {add() {}, remove() {}}, setAttribute() {}, append() {}});
  r.window.clearTimeout = () => {};
  r.window.setTimeout = () => 0;
  const listener = r.listeners.find(([type]) => type === 'click')[1];
  return {r, button, listener};
}

test('plan card uses server quote including required accessory prices', async () => {
  const {r, button, listener} = cardRuntime();
  let request;
  r.context.fetch = async (url, options) => {
    request = {url, ...JSON.parse(options.body)};
    return {ok: true, json: async () => ({productId: 1, salesPlanId: 'digi-time',
      planName: 'Digi Time', unitAmount: 23303823, currency: 'IRT',
      accessoryAmounts: {'shipping-insurance': 245000}})};
  };
  await listener({target: {closest: () => button}});
  assert.equal(request.url, '/api/store/sales-plans/quote');
  assert.deepEqual(JSON.parse(JSON.stringify(request.accessoryIds)), ['shipping-insurance']);
  const [item] = r.ui.cart.read();
  assert.equal(item.price, 23303823);
  assert.equal(item.options.accessories.find(option => option.id === 'shipping-insurance').price, 0.245);
  assert.equal(button.disabled, false);
});

test('unavailable plan quote does not add a cart item', async () => {
  const {r, button, listener} = cardRuntime();
  r.context.fetch = async () => ({ok: false});
  await listener({target: {closest: () => button}});
  assert.equal(r.ui.cart.read().length, 0);
  assert.equal(button.disabled, false);
});
