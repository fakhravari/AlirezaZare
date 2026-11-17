import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';

const source = fs.readFileSync('Shop/wwwroot/shop/js/store/cart.js', 'utf8');
function model(accessories) {
  const window = { HNProducts: [{ id: 1, name: 'Product', price: 1, image: 'images/product.webp', accessories }] };
  vm.runInNewContext(source, { window });
  return window.HNCartModel.create({ document: { querySelector: selector => selector === '#storeSalesPlans' ? { textContent: JSON.stringify({ offers: [{ productId: 1, salesPlanId: 'regular', isEnabled: true, stockQuantity: 5 }] }) } : null }, emit() {}, requireLogin: () => true, formatNumber: String, escapeHtml: String });
}
const current = { id: '42', productAccessoryId: 12, name: 'Cover', price: 3, required: false };
const line = (accessory, snapshot = null) => [{ id: '1', name: 'Product', quantity: 1, options: { accessories: [accessory] }, salesPlanSnapshot: snapshot }];

test('legacy cart restores accessory identity and quoted price by product relation across reads', () => {
  const cart = model([current]);
  cart.write(line({ id: 'old-key', productAccessoryId: 12, name: 'old name', selected: true }, { id: 'regular', accessoryPriceAmounts: { 'old-key': 2500000 } }));
  const item = cart.read()[0];
  assert.equal(item.options.accessories[0].id, '42');
  assert.equal(item.options.accessories[0].productAccessoryId, 12);
  assert.equal(item.options.accessories[0].price, 2.5);
  assert.equal(item.salesPlanSnapshot.accessoryPriceAmounts['42'], 2500000);
});

test('old name-only selections migrate only when the name identifies one accessory', () => {
  let cart = model([current]);
  assert.equal(cart.write(line({ name: 'Cover', selected: true }))[0].options.accessories[0].id, '42');
  cart = model([current, { ...current, id: '43', productAccessoryId: 13 }]);
  assert.equal(cart.write(line({ name: 'Cover', selected: true }))[0].options.accessories.filter(x => x.selected).length, 0);
});
