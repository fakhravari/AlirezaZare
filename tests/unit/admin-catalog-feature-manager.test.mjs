import { test } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync } from 'node:fs';
import { chromium } from '@playwright/test';

test('catalog feature manager creates and edits color metadata', { skip: !existsSync(chromium.executablePath()) }, async t => {
  const browser = await chromium.launch({ headless: true });
  t.after(() => browser.close());
  const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
  await page.route('https://example.test/', route => route.fulfill({ contentType: 'text/html', body: '' }));
  await page.goto('https://example.test/');
  await page.setContent(`<div dir="rtl" data-admin-page="catalog" data-admin-section="attributes" data-admin-api="/api/admin/store/catalog/attributes">
    <form data-admin-filter><input name="q"></form><div data-admin-status></div><span data-admin-count></span><div data-admin-results></div><div data-admin-pagination></div>
    <section data-catalog-feature-manager><input name="__RequestVerificationToken" value="token"><div data-feature-manager-status></div>
      <div data-reference-feature-list="1"></div><form data-reference-feature-create><input name="kind" value="1"><input name="name"><button type="submit">add</button></form>
      <div data-reference-feature-list="2"></div><form data-reference-feature-create><input name="kind" value="2"><input name="name"><button type="submit">add</button></form>
    </section></div>`);
  await page.evaluate(() => {
    window.calls = [];
    window.features = [
      { id: 1, name: 'سایز', kind: 1, isActive: true, values: [{ id: 10, name: 'بزرگ', isActive: true }] },
      { id: 2, name: 'رنگ', kind: 2, isActive: true, values: [{ id: 20, name: 'آبی', isActive: true, valueKey: 'blue', nameEn: 'Blue', hexCode: '#2563EB' }] },
    ];
    window.AdminUI = { getJson: async (input, options) => {
      const url = String(input);
      window.calls.push({ url, method: options?.method || 'GET', body: options?.body });
      if (url.includes('/catalog/attributes')) return { items: [], total: 0, page: 1, pageSize: 50 };
      if (url.endsWith('/catalog-features') && (options?.method || 'GET') === 'GET') return window.features;
      if (options?.method === 'POST' && url.endsWith('/2/values')) {
        const body = JSON.parse(options.body);
        window.features[1].values.push({ id: 21, ...body, isActive: true });
        return { id: 21, name: body.name };
      }
      if (options?.method === 'PUT' && url.endsWith('/2/values/20')) {
        Object.assign(window.features[1].values[0], JSON.parse(options.body));
        return { message: 'saved' };
      }
      return {};
    }, notify() {}, formatNumber: String };
  });
  await page.addScriptTag({ path: 'Admin/wwwroot/js/admin-data.js' });
  await page.evaluate(() => AdminPages.initialize(document));

  const colorList = page.locator('[data-reference-feature-list="2"]');
  const colorRoot = colorList.locator('[role="treeitem"]').first();
  await colorRoot.waitFor();
  assert.equal(await colorRoot.locator('strong').innerText(), '2 · رنگ');
  assert.equal(await colorRoot.locator('[data-reference-rename-feature]').isDisabled(), true);
  await colorRoot.locator('.admin-permission-toggle').click();
  const colorEditor = colorList.locator('[data-reference-value-update="20"]');
  await colorEditor.waitFor();
  assert.equal(await colorList.locator('[style*="background-color"]').count(), 1);

  const createForm = colorList.locator('[data-reference-value-create="2"]');
  await createForm.locator('[name="name"]').fill('آبی آسمانی');
  await createForm.locator('[name="nameEn"]').fill('Sky blue');
  await createForm.locator('[name="valueKey"]').fill('sky-blue');
  await createForm.locator('[name="hexCode"]').fill('#38BDF8');
  await createForm.locator('button[type="submit"]').click();
  await colorList.getByText(/آبی آسمانی/).waitFor();
  const createCall = await page.evaluate(() => window.calls.find(call => call.method === 'POST' && call.url.endsWith('/2/values')));
  assert.deepEqual(JSON.parse(createCall.body), { name: 'آبی آسمانی', nameEn: 'Sky blue', valueKey: 'sky-blue', hexCode: '#38BDF8' });

  const updateForm = colorList.locator('[data-reference-value-update="20"]');
  await updateForm.locator('[name="nameEn"]').fill('Azure');
  await updateForm.locator('[name="hexCode"]').fill('#1D4ED8');
  await updateForm.locator('button[type="submit"]').click();
  await page.waitForFunction(() => window.calls.some(call => call.method === 'PUT' && call.url.endsWith('/2/values/20')));
  const updateCall = await page.evaluate(() => window.calls.find(call => call.method === 'PUT' && call.url.endsWith('/2/values/20')));
  assert.deepEqual(JSON.parse(updateCall.body), { name: 'آبی', nameEn: 'Azure', valueKey: 'blue', hexCode: '#1D4ED8' });
  assert.equal(await page.locator('[data-reference-feature-list="1"] [role="treeitem"]').count(), 1);
});
