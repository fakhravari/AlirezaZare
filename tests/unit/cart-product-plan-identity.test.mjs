import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

test('cart replaces selections within a product and plan, and keeps plans separate', () => {
  const plans = { plans: [{ id: 'regular', isDefault: true }, { id: 'sale' }], offers: ['regular', 'sale'].map(salesPlanId => ({ productId: 1, salesPlanId, isEnabled: true, stockQuantity: 10, accessoryPriceAmounts: { '21': 500000 } })), inventoryVariants: [11, 12].map(variantId => ({ productId: 1, variantId, stockBySalesPlan: { regular: 10, sale: 10 }, priceBySalesPlan: { regular: 1000000, sale: 1000000 } })) };
  const document = { querySelector: selector => selector === '#storeSalesPlans' ? { textContent: JSON.stringify(plans) } : null };
  const product = { id: 1, name: 'Item', image: 'images/item.jpg', price: 1, maxCartQuantity: 10, accessories: [{ id: '21', productAccessoryId: 31, name: 'Extra', price: .5 }] };
  const window = { HNProducts: [product] };
  vm.runInNewContext(readFileSync('Shop/wwwroot/shop/js/store/cart.js', 'utf8'), { window, document });
  const cart = window.HNCartModel.create({ document, emit() {}, requireLogin: () => true, formatNumber: String, escapeHtml: String });
  const line = (variantId, salesPlanId, selected) => ({ ...product, id: '1', variantId, salesPlanId, price: selected ? 1500000 : 1000000, salesPlanSnapshot: { id: salesPlanId, unitAmount: selected ? 1500000 : 1000000 }, options: { inventoryVariant: { id: variantId, name: String(variantId) }, accessories: [{ ...product.accessories[0], selected }] } });
  cart.add(line(11, 'regular', false));
  cart.add(line(12, 'regular', true));
  cart.add(line(11, 'sale', false));
  let result = JSON.parse(JSON.stringify(cart.read()));
  assert.equal(result.length, 2);
  assert.equal(result[0].quantity, 2);
  assert.equal(result[0].variantId, 12);
  assert.equal(result[0].options.accessories[0].selected, true);
  assert.equal(result[0].price, 1500000);
  cart.restoreAccountLines([{ ...line(11, 'regular', false), quantity: 2 }, { ...line(12, 'regular', true), quantity: 3 }]);
  result = JSON.parse(JSON.stringify(cart.read()));
  assert.equal(result.length, 1);
  assert.equal(result[0].quantity, 5);
  assert.equal(result[0].variantId, 12);
  assert.equal(result[0].price, 1500000);
});
