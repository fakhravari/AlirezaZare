import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('price history uses the shared table without DataTables paging after refresh', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  let requests = 0;
  await page.route('https://example.test/**', route => {
    if (new URL(route.request().url()).pathname.endsWith('/price-history')) {
      requests++;
      return route.fulfill({json: Array.from({length: 60}, (_, i) => ({id: i + 1, idProduct: 1, productName: 'Product', idSalesPlan: 2, salesPlanName: 'Regular', targetType: 'option', targetId: 7, priceAmount: 1000, discountPercent: 10, finalPriceAmount: 900, idChangedBy: 3, idPreviousVersion: i === 0 ? 42 : null, createdAtUtc: '2024-03-20T12:00:00Z'}))});
    }
    return route.fulfill({contentType: 'text/html', body: ''});
  });
  await page.goto('https://example.test/');
  await page.setContent(`<div data-store-variant-prices data-api="/api/prices">
    <form data-history-filter><input name="q"><input name="from"><input name="to"><button>Filter</button></form>
    <button data-price-history-refresh>Refresh</button><div data-price-history-status></div>
    <table data-admin-datatable data-admin-table-mode="server"><thead><tr>${'<th>Column</th>'.repeat(12)}</tr></thead><tbody data-price-history-rows></tbody></table>
  </div>`);
  await page.addScriptTag({path: 'Admin/wwwroot/color_admin/assets/js/vendor.min.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/color_admin/assets/plugins/datatables.net/js/dataTables.min.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/color_admin/assets/plugins/datatables.net-bs5/js/dataTables.bootstrap5.min.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-ui.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-variant-prices.js'});
  await page.evaluate(() => window.AdminVariantPrices.initialize(document));
  await page.waitForFunction(() => document.querySelectorAll('[data-price-history-rows] tr').length === 60);
  assert.deepEqual(await page.locator('[data-price-history-rows] tr').first().locator('td').allTextContents(), ['#1', 'Product', 'Regular', 'ویژگی #7', '۱٬۰۰۰', '۱۰', '۹۰۰', 'سابقه', await page.evaluate(() => window.AdminUI.formatDateTime('2024-03-20T12:00:00Z')), '#42', '#3', '—']);
  const table = page.locator('table');
  assert.equal(await table.evaluate(node => window.AdminUI.getDataTable(node).settings()[0].oFeatures.bPaginate), false);
  assert.equal(await page.locator('.dt-paging').count(), 0);
  await page.locator('[data-price-history-refresh]').click();
  await page.waitForFunction(() => document.querySelectorAll('[data-price-history-rows] tr').length === 60 && !!window.AdminUI.getDataTable(document.querySelector('table')));
  assert.equal(requests, 2);
  assert.equal(await page.locator('.dt-paging').count(), 0);
});
