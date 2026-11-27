import { test } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync } from 'node:fs';
import { chromium } from '@playwright/test';

test('product feature editor displays color swatches and saves selected feature values', { skip: !existsSync(chromium.executablePath()) }, async t => {
  const browser = await chromium.launch({ headless: true });
  t.after(() => browser.close());
  const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
  await page.route('https://example.test/', route => route.fulfill({ contentType: 'text/html', body: '' }));
  await page.goto('https://example.test/');
  await page.setContent(`<main data-admin-page="catalog" data-admin-section="products" data-admin-api="/api/admin/store/catalog/products" data-can-upload-image="true">
    <form data-admin-filter><input name="q"></form><div data-admin-status></div><span data-admin-count></span>
    <table data-admin-datatable><tbody data-admin-results></tbody></table><div data-admin-empty></div><div data-admin-pagination></div>
    <button type="button" data-product-features="0">ویژگی‌های کالا</button>
  </main>
  <div id="product-features-modal"><form data-product-features-form><input type="hidden" name="productId"><input name="__RequestVerificationToken" value="csrf">
    <p data-product-features-name></p><p data-product-features-status role="status"></p><div data-product-features-choices></div><button type="submit" disabled>ذخیره</button>
  </form></div>`);
  await page.addScriptTag({ path: 'Admin/wwwroot/js/admin-ui.js' });
  await page.evaluate(() => {
    window.calls = [];
    window.bootstrap = { Modal: { getOrCreateInstance: () => ({ show() {} }), getInstance: () => ({ hide() {} }) } };
    window.AdminUI = { notify() {}, formatNumber: String, destroyDataTable() {}, refreshDataTable() {}, withIcon(button) { button.innerHTML = '<i></i>'; }, getJson: async (input, options) => {
      const url = new URL(String(input), window.location.origin).pathname;
      window.calls.push({ url, method: options?.method || 'GET', body: options?.body, headers: options?.headers });
      if (url === '/api/admin/store/catalog/products') return { items: [{ id: 41, name: 'کالای آزمایشی', code: 'T-41' }], total: 1, page: 1, pageSize: 50 };
      if (url === '/api/admin/store/catalog-features') return [{ id: 7, name: 'رنگ', kind: 2, isActive: true, values: [{ id: 71, name: 'آبی', isActive: true, hexCode: '#2563EB' }, { id: 72, name: 'قرمز', isActive: true, hexCode: '#DC2626' }] }];
      if (url.endsWith('/catalog-features/products/41') && options?.method === 'PUT') return { message: 'saved' };
      if (url.endsWith('/catalog-features/products/41')) return { featureIds: [7], selections: [{ idFeature: 7, idFeatureValue: 71 }] };
      return {};
    } };
  });
  await page.addScriptTag({ path: 'Admin/wwwroot/js/admin-data.js' });
  await page.evaluate(() => {
    document.querySelector('main').__adminRows = [{ id: 41, name: 'کالای آزمایشی', code: 'T-41' }];
  });

  const featureAction = page.locator('[data-product-features]');
  await featureAction.waitFor();
  await featureAction.click();
  const blue = page.locator('[data-product-feature="7"][value="71"]');
  try { await blue.waitFor({ timeout: 2000 }); }
  catch { throw new Error(JSON.stringify(await page.evaluate(() => ({ status: document.querySelector('[data-product-features-status]')?.textContent, calls: window.calls })))); }
  assert.equal(await blue.isChecked(), true);
  assert.equal(await page.locator('[data-product-features-choices] [style*="background-color"]').count(), 2);

  await page.setViewportSize({ width: 1440, height: 900 });
  await page.locator('[data-product-feature="7"][value="72"]').check();
  await page.locator('[data-product-features-form] button[type="submit"]').click();
  await page.waitForFunction(() => window.calls.some(call => call.method === 'PUT' && call.url.endsWith('/catalog-features/products/41')));
  const saved = await page.evaluate(() => window.calls.find(call => call.method === 'PUT' && call.url.endsWith('/catalog-features/products/41')));
  assert.deepEqual(JSON.parse(saved.body), { selections: [{ featureId: 7, valueId: 71 }, { featureId: 7, valueId: 72 }] });
  assert.equal(saved.headers.RequestVerificationToken, 'csrf');
});
