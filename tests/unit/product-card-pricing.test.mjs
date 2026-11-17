import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import vm from 'node:vm';

test('cards price the same default variant, option and required accessory as a quote', async () => {
  const source = await readFile('Shop/wwwroot/shop/js/store/product-card.js', 'utf8');
  const snapshot = {
    plans: [{ id: 'regular', isDefault: true }, { id: 'special' }],
    offers: [
      { productId: 1, salesPlanId: 'regular', priceAmount: 1000, discountPercent: 10, isEnabled: true, stockQuantity: 2, accessoryPriceAmounts: { case: 50 } },
      { productId: 1, salesPlanId: 'special', priceAmount: 800, discountPercent: 0, isEnabled: true, stockQuantity: 2, accessoryPriceAmounts: { case: 70 } },
    ],
    inventoryVariants: [{ productId: 1, isDefault: true, stockBySalesPlan: { regular: 2, special: 2 }, priceBySalesPlan: { special: 600 }, discountBySalesPlan: { special: 25 } }],
    priceOptions: { 1: [{ values: [{ priceDelta: -100, priceBySalesPlan: { regular: -100, special: 30 } }, { priceDelta: 50 }] }] },
  };
  const document = { querySelector: () => ({ textContent: JSON.stringify(snapshot) }) };
  const window = { HNUI: { escapeHtml: String, formatNumber: String }, HNProductUrl: () => '/product' };
  vm.runInNewContext(source, { window, document });
  const product = { id: 1, name: 'test', price: 0.0009, image: 'image', accessories: [{ id: 'case', required: true, price: 0.00005 }] };
  assert.equal(window.HNProductCard.currentPrice(product), 850);
  assert.equal(window.HNProductCard.currentPrice(product, { salesPlanId: 'special' }), 550);
  const markup = window.HNProductCard.render(product, { salesPlanId: 'special', salesPlanName: 'special' });
  assert.match(markup, /data-price="550"/);
  assert.match(markup, /product-price-current">550/);
  assert.match(markup, /price-old">700/);
  assert.match(markup, /product-price-discount">٪25/);
});
