import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync, readFileSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('product unit history shows only inactive links and provides no reactivation controls', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<div data-admin-definitions data-api="/api" data-definition-section="product-units" data-can-create="true" data-can-edit="true" data-can-deactivate="true"><div data-definition-status></div><div data-product-unit-tree></div></div><div id="admin-definition-modal"><h2 id="admin-definition-title"></h2><form data-definition-form><input name="id"><div data-definition-fields></div><button type="submit"></button></form></div>`);
  await page.evaluate(() => {
    window.units = [
      {id: 1, idProduct: 10, productName: 'کالای نمونه', idUnit: 1, unitName: 'عدد', conversionFactor: 1, isBaseUnit: true, isActive: true},
      {id: 2, idProduct: 10, productName: 'کالای نمونه', idUnit: 2, unitName: 'بسته', conversionFactor: 12, isBaseUnit: false, isActive: false},
      {id: 3, idProduct: 11, productName: 'کالای دوم', idUnit: 1, unitName: 'عدد', conversionFactor: 1, isBaseUnit: true, isActive: true}
    ];
    window.AdminUI = {
      destroyDataTable() {}, refreshDataTable() {}, notify() {}, initStatusSwitch() {},
      getJson: async url => url.endsWith('/lookups') ? {warehouses: [], products: [], units: []} : window.units
    };
  });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-definitions.js'});
  await page.evaluate(() => window.AdminDefinitions.initialize(document));

  assert.equal(await page.locator('[data-definition-create]').count(), 0);
  assert.match(readFileSync('Admin/Areas/Warehouse/Views/Features/Definitions.cshtml', 'utf8'), /Model\.FeatureId != "references" && Model\.FeatureId != "product-units"/);
  const branches = page.locator('[data-product-unit-tree] details');
  assert.equal(await branches.count(), 1);
  assert.match(await branches.nth(0).locator('summary').innerText(), /کالای نمونه\s*۱ واحد/u);
  await branches.nth(0).locator('summary').click();
  assert.match(await branches.nth(0).innerText(), /بسته/u);
  assert.match(await branches.nth(0).innerText(), /غیرفعال/u);
  assert.equal(await page.locator('[data-product-unit-tree] button').count(), 0);
});
