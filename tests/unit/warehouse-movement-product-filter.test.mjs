import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

for (const mode of ['stock', 'summary', 'audit']) test(`movement ${mode} page applies the appropriate product filter`, {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true}); t.after(() => browser.close());
  const page = await browser.newPage(); const searches = [];
  await page.route('https://example.test/**', route => {
    const url = new URL(route.request().url());
    if (url.pathname === '/api/movements/products') { searches.push(url.searchParams.get('search')); return route.fulfill({json: {items: [{id: 42, name: 'کالای آزمون', code: 'AZ-42'}], more: false}}); }
    return route.fulfill({contentType: 'text/html', body: ''});
  });
  await page.goto('https://example.test/');
  await page.setContent(`<div data-admin-movements data-mode="${mode}" data-api="/api/movements"><div data-movement-title></div><form data-movement-filter><select name="productId" data-remote-product-url="/api/movements/products"><option value="">همهٔ کالاها</option></select><input name="search"><input name="salesPlan" value="plan"><select name="warehouseId"><option value="">همهٔ انبارها</option></select><button type="submit">جست‌وجو</button><button type="reset">پاک‌کردن</button></form><div data-movement-status></div><table data-admin-datatable><thead data-movement-head></thead><tbody data-movement-rows></tbody></table><div data-movement-pagination></div></div>`);
  await page.evaluate(() => { window.requests=[]; window.AdminUI={destroyDataTable(){},refreshDataTable(){},renderNumberedPagination(){},formatDateTime(x){return x;},async getJson(url){window.requests.push(url);return url.includes('/warehouses') ? [] : {items: [], total: 0, pageSize: 50};}}; });
  for (const file of ['Admin/wwwroot/color_admin/assets/js/vendor.min.js','Admin/wwwroot/color_admin/assets/plugins/select2/dist/js/select2.min.js','Admin/wwwroot/js/admin-selects.js','Admin/wwwroot/js/admin-movements.js']) await page.addScriptTag({path:file});
  await page.evaluate(() => window.AdminMovements.initialize(document));
  if (mode === 'audit') {
    assert.equal(await page.locator('[name=productId]').isDisabled(), true);
    assert.equal(await page.locator('[name=salesPlan]').isDisabled(), true);
    const request = await page.evaluate(() => window.requests.findLast(url => url.includes('audit=true')));
    assert.ok(request);
    assert.ok(!request.includes('productId=') && !request.includes('salesPlan='));
    return;
  }
  await page.locator('.select2-selection').first().click();
  await page.locator('.select2-search__field').last().fill('AZ-42');
  await page.waitForResponse(response => new URL(response.url()).searchParams.get('search') === 'AZ-42');
  await page.getByRole('option', {name: /کالای آزمون/}).click();
  assert.equal(await page.locator('[name=productId]').inputValue(), '42');
  await page.getByRole('button', {name:'جست‌وجو'}).click();
  await page.waitForFunction(mode => window.requests.some(url => url.includes('productId=42') && (mode !== 'summary' || url.includes('summary=true'))), mode);
  await page.getByRole('button', {name:'پاک‌کردن'}).click();
  await page.waitForFunction(() => document.querySelector('[name=productId]').value === '');
  assert.ok(searches.includes('AZ-42'));
});
