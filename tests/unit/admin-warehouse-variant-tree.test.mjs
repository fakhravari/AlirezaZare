import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('variant editor fixes the product and saves attributes selected from its tree', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<div data-admin-definitions data-api="/api" data-definition-section="variants" data-can-create="true" data-can-edit="true" data-can-deactivate="false"><div data-definition-status></div><button data-definition-create></button><table data-admin-datatable><thead data-definition-head></thead><tbody data-definition-rows></tbody></table></div><div id="admin-definition-modal"><h2 id="admin-definition-title"></h2><form data-definition-form><input name="id"><div data-definition-fields></div><button type="submit">ذخیره</button></form></div>`);
  await page.evaluate(() => {
    window.requests = [];
    window.AdminUI = {
      destroyDataTable() {}, refreshDataTable() {}, notify() {}, initStatusSwitch() {},
      getJson: async (url, options = {}) => {
        window.requests.push({url, method: options.method || 'GET', body: options.body ? JSON.parse(options.body) : null});
        if (url.endsWith('/variants')) return [{id: 21, idProduct: 10, productName: 'کالای نمونه', name: 'مدل قدیمی', code: 'V-21', isDefault: true, isActive: true, attributes: [{attributeId: 5, valueId: 8, key: 'color', label: 'رنگ', value: 'black', display: 'مشکی'}]}];
        if (url.endsWith('/lookups')) return {warehouses: [], products: [{id: 10, name: 'کالای نمونه'}]};
        if (url.includes('/variant-options/10')) return [{id: 5, key: 'color', label: 'رنگ', values: [{id: 8, key: 'black', label: 'مشکی'}, {id: 9, key: 'white', label: 'سفید'}]}];
        return {id: 21, message: 'ذخیره شد'};
      }
    };
  });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-definitions.js'});
  await page.evaluate(() => window.AdminDefinitions.initialize(document));
  await page.getByRole('button', {name: 'ویرایش'}).click();

  const product = page.locator('[data-definition-fields] input[readonly]');
  assert.equal(await product.inputValue(), 'کالای نمونه');
  assert.equal(await page.locator('[data-definition-fields] select[name="productId"]').count(), 0);
  const selects = page.locator('[data-variant-attribute] select');
  assert.equal(await selects.count(), 2);
  assert.equal(await selects.nth(0).inputValue(), '5');
  assert.equal(await selects.nth(1).inputValue(), '8');

  await selects.nth(1).selectOption('9');
  await page.locator('[data-definition-form] button[type="submit"]').click();
  const saved = await page.evaluate(() => window.requests.find(request => request.method === 'PUT'));
  assert.equal(saved.body.productId, 10);
  assert.deepEqual(saved.body.attributes, [{attributeId: 5, valueId: 9}]);
});
