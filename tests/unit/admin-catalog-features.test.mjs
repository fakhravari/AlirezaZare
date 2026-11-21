import { test } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync } from 'node:fs';
import { chromium } from '@playwright/test';

test('category features distinguish inherited and direct selections', { skip: !existsSync(chromium.executablePath()) }, async t => {
  const browser = await chromium.launch({ headless: true });
  t.after(() => browser.close());
  const page = await browser.newPage({ viewport: { width: 375, height: 800 } });
  await page.route('https://example.test/', route => route.fulfill({ contentType: 'text/html', body: '' }));
  await page.goto('https://example.test/');
  await page.setContent(`<div dir="rtl" data-admin-page="catalog" data-admin-section="categories" data-admin-api="/api" data-can-upload-image="true" data-can-edit-category="true">
    <form data-admin-filter><input name="q"></form><div data-admin-status></div><span data-admin-count></span><div data-admin-results role="tree"></div><div data-admin-empty></div><div data-admin-pagination></div></div>
    <div id="category-content-modal"><form data-category-content-form><input name="key"><input name="intro"><input name="highlights"><input name="specs"><input name="__RequestVerificationToken" value="token"><p data-category-content-name></p>
    <section data-category-features><div data-feature-message></div><div data-feature-groups></div><input data-feature-name><select data-feature-kind><option value="1"></option></select><select data-feature-value-owner></select><input data-feature-value-name><button type="button" data-feature-save>save</button></section></form></div>`);
  await page.addScriptTag({ path: 'Admin/wwwroot/js/admin-ui.js' });
  await page.evaluate(() => {
    window.calls = [];
    window.bootstrap = { Modal: { getOrCreateInstance: () => ({ show() {} }) } };
    window.AdminUI = { ...AdminUI, notify() {}, getJson: async (url, options) => {
      window.calls.push({ url, body: options?.body });
      url = String(url);
      if (url.includes('/catalog/categories?')) return { items: [{ id: 'leaf', name: 'دریل', parentId: null, isActive: true }], total: 1, hasNext: false };
      if (url.includes('category-content')) return { intro: '', highlights: '[]', specs: '[]' };
      if (options?.method === 'PUT') return { message: 'ok' };
      if (url.endsWith('/categories/leaf')) return { direct: [2, 3], inherited: [1, 3] };
      return [{ id: 1, name: 'رنگ', kind: 1, isActive: true, values: [] }, { id: 2, name: 'توان', kind: 2, isActive: true, values: [] }, { id: 3, name: 'جنس', kind: 2, isActive: true, values: [] }];
    } };
  });
  await page.addScriptTag({ path: 'Admin/wwwroot/js/admin-data.js' });
  await page.evaluate(() => AdminPages.initialize(document));
  await page.locator('[data-category-content]').first().click();
  await page.locator('[data-feature-choice][value="1"]').waitFor();
  assert.equal(await page.locator('[data-feature-choice][value="1"]').isDisabled(), true);
  assert.equal(await page.locator('[data-feature-choice][value="2"]').isChecked(), true);
  await page.locator('[data-feature-save]').click();
  await page.waitForFunction(() => window.calls.some(call => call.body));
  const body = await page.evaluate(() => JSON.parse(window.calls.find(call => call.body).body));
  assert.deepEqual(body.featureIds.sort(), [2, 3]);
});
