import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import vm from 'node:vm';

test('cards use the selected variant price and required accessory without price options', async () => {
  const source = await readFile('Shop/wwwroot/shop/js/store/product-card.js', 'utf8');
  const snapshot = {
    plans: [{ id: 'regular', isDefault: true }, { id: 'special' }],
    offers: [
      { productId: 1, salesPlanId: 'regular', priceAmount: 1000, discountPercent: 10, finalPriceAmount: 900, isEnabled: true, stockQuantity: 2, accessoryPriceAmounts: { case: 50 } },
      { productId: 1, salesPlanId: 'special', priceAmount: 800, discountPercent: 0, finalPriceAmount: 800, isEnabled: true, stockQuantity: 2, accessoryPriceAmounts: { case: 70 } },
    ],
    inventoryVariants: [{ productId: 1, isDefault: true, stockBySalesPlan: { regular: 2, special: 2 }, priceBySalesPlan: { regular: 1000, special: 600 }, discountBySalesPlan: { regular: 10, special: 25 }, finalPriceBySalesPlan: { regular: 900, special: 450 } }],
  };
  const document = { querySelector: () => ({ textContent: JSON.stringify(snapshot) }) };
  const window = { HNUI: { escapeHtml: String, formatNumber: String }, HNProductUrl: () => '/product' };
  vm.runInNewContext(source, { window, document });
  const product = { id: 1, name: 'test', price: 0.0009, image: 'image', accessories: [{ id: 'case', required: true, price: 0.00005 }] };
  assert.equal(window.HNProductCard.currentPrice(product), 950);
  assert.equal(window.HNProductCard.currentPrice(product, { salesPlanId: 'special' }), 520);
  const markup = window.HNProductCard.render(product, { salesPlanId: 'special', salesPlanName: 'special' });
  assert.match(markup, /data-price="520"/);
  assert.match(markup, /product-price-current">520/);
  assert.match(markup, /price-old">670/);
  assert.match(markup, /product-price-discount">٪25/);
});

test('cards do not fall back to the product base price when a default variant has no plan price', async () => {
  const source = await readFile('Shop/wwwroot/shop/js/store/product-card.js', 'utf8');
  const snapshot = {
    plans: [{ id: 'regular', isDefault: true }],
    offers: [{ productId: 1, salesPlanId: 'regular', priceAmount: 1000, discountPercent: 0, isEnabled: true, stockQuantity: 2 }],
    inventoryVariants: [{ productId: 1, isDefault: true, stockBySalesPlan: { regular: 2 }, priceBySalesPlan: {} }],
  };
  const document = { querySelector: () => ({ textContent: JSON.stringify(snapshot) }) };
  const window = { HNUI: { escapeHtml: String, formatNumber: String }, HNProductUrl: () => '/product' };
  vm.runInNewContext(source, { window, document });
  const product = { id: 1, name: 'test', price: 0.001, image: 'image', accessories: [] };
  assert.equal(window.HNProductCard.currentPrice(product), null);
  assert.match(window.HNProductCard.render(product), /قیمت تنوع ثبت نشده/);
  assert.match(window.HNProductCard.render(product), /disabled/);
});

test('cards disable purchase when the stored final price is missing', async () => {
  const source = await readFile('Shop/wwwroot/shop/js/store/product-card.js', 'utf8');
  const snapshot = {
    plans: [{ id: 'regular', isDefault: true }],
    offers: [{ productId: 1, salesPlanId: 'regular', priceAmount: 1000, isEnabled: true, stockQuantity: 2 }],
    inventoryVariants: [],
  };
  const document = { querySelector: () => ({ textContent: JSON.stringify(snapshot) }) };
  const window = { HNUI: { escapeHtml: String, formatNumber: String }, HNProductUrl: () => '/product' };
  vm.runInNewContext(source, { window, document });
  const product = { id: 1, name: 'test', image: 'image', accessories: [] };
  assert.equal(window.HNProductCard.currentPrice(product), null);
  assert.match(window.HNProductCard.render(product), /disabled aria-disabled="true" aria-label="قیمت ناموجود"/);
});
