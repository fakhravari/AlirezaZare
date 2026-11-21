import { test } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync } from 'node:fs';
import { chromium } from '@playwright/test';

test('catalog attribute page manages two central feature sets and their values', { skip: !existsSync(chromium.executablePath()) }, async t => {
  const browser = await chromium.launch({ headless: true });
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.route('https://example.test/', route => route.fulfill({ contentType: 'text/html', body: '' }));
  await page.goto('https://example.test/');
  await page.setContent(`<div data-admin-page="catalog" data-admin-section="attributes" data-admin-api="/api/admin/store/catalog/attributes">
    <form data-admin-filter><input name="q"></form><div data-admin-status></div><span data-admin-count></span><div data-admin-results></div><div data-admin-pagination></div>
    <section data-catalog-feature-manager><input name="__RequestVerificationToken" value="token"><div data-feature-manager-status></div>
      <div data-reference-feature-list="1"></div><form data-reference-feature-create><input name="kind" value="1"><input name="name"><button type="submit">add</button></form>
      <div data-reference-feature-list="2"></div><form data-reference-feature-create><input name="kind" value="2"><input name="name"><button type="submit">add</button></form>
    </section></div>`);
  await page.evaluate(() => {
    window.features = [
      { id: 1, name: 'رنگ', kind: 1, isActive: true, values: [{ id: 10, name: 'آبی', isActive: true }] },
      { id: 2, name: 'توان', kind: 2, isActive: true, values: [] },
    ];
    window.AdminUI = { getJson: async (url, options) => {
      if (url instanceof URL) return { items: [], total: 0, page: 1, pageSize: 50 };
      if (options?.method === 'POST') {
        const body = JSON.parse(options.body);
        if (url.endsWith('/values')) window.features.find(x => x.id === Number(url.split('/').at(-2))).values.push({ id: 11, name: body.name, isActive: true });
        else window.features.push({ id: 3, name: body.name, kind: body.kind, isActive: true, values: [] });
        return {};
      }
      if (options?.method === 'PUT') return {};
      return window.features;
    }, notify() {}, formatNumber: String };
  });
  await page.addScriptTag({ path: 'Admin/wwwroot/js/admin-data.js' });
  await page.evaluate(() => AdminPages.initialize(document));
  await page.locator('[data-reference-feature-list="1"] article').waitFor();
  assert.equal(await page.locator('[data-reference-feature-list="1"] strong').innerText(), 'رنگ');
  assert.equal(await page.locator('[data-reference-feature-list="2"] strong').innerText(), 'توان');
  await page.locator('[data-reference-feature-create]').first().locator('[name="name"]').fill('سایز');
  await page.locator('[data-reference-feature-create]').first().locator('button').click();
  await page.locator('[data-reference-feature-list="1"] strong').filter({ hasText: 'سایز' }).waitFor();
  await page.locator('[data-reference-value-create="3"] [name="name"]').fill('بزرگ');
  await page.locator('[data-reference-value-create="3"] button').click();
  await page.locator('[data-reference-feature-list="1"] button').filter({ hasText: 'بزرگ' }).waitFor();
  assert.equal(await page.locator('[data-reference-feature-list="2"] article').count(), 1);
});
