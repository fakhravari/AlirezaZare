import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFile, existsSync} from 'node:fs';
import {promisify} from 'node:util';
import {chromium} from '@playwright/test';

const read = promisify(readFile);

test('unit status is submitted from the definition form', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<div data-admin-definitions data-api="/api" data-definition-section="units" data-can-create="true" data-can-edit="true" data-can-deactivate="true"><button data-definition-create>افزودن</button><div data-definition-status></div><table data-admin-datatable><thead data-definition-head></thead><tbody data-definition-rows></tbody></table></div><div id="admin-definition-modal"><h2 id="admin-definition-title"></h2><form data-definition-form><input name="id"><div data-definition-fields></div><button type="submit">ذخیره</button></form></div>`);
  await page.evaluate(() => {
    window.saved = [];
    window.bootstrap = {Modal: {getOrCreateInstance: () => ({show() {}}), getInstance: () => ({hide() {}})}};
    window.AdminUI = {initStatusSwitch() {}, destroyDataTable() {}, refreshDataTable() {}, notify() {}, getJson: async (url, options) => {
      if (options.method === 'GET') return [];
      window.saved.push(JSON.parse(options.body)); return {message: 'ok'};
    }};
  });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-definitions.js'});
  await page.evaluate(() => window.AdminDefinitions.initialize(document));
  await page.locator('[data-definition-create]').click();
  assert.equal(await page.locator('[name=isActive]').isChecked(), true);
  await page.locator('[name=code]').fill('unit');
  await page.locator('[name=name]').fill('واحد');
  await page.locator('[name=isActive]').uncheck();
  await page.locator('[data-definition-form]').evaluate(form => form.requestSubmit());
  await page.waitForFunction(() => window.saved.length === 1);
  assert.equal((await page.evaluate(() => window.saved[0])).isActive, false);
});

test('product editor limits base-unit changes to products without warehouse document lines', async () => {
  const api = await read('Admin/Areas/Store/Controllers/Api/StoreCatalogAdminApiController.cs', 'utf8');
  const markup = await read('Admin/Areas/Store/Views/Catalog/Index.cshtml', 'utf8');
  assert.match(api, /x\.Id == input\.UnitId && x\.IsActive/);
  assert.match(api, /from line in db\.WarehouseDocumentLines[\s\S]*join item in db\.InventoryItems[\s\S]*item\.IdProduct == id/);
  assert.match(api, /baseLink\.ConversionFactor = 1m/);
  assert.match(markup, /id="product-edit-unit"[^>]*name="unitId"/);
  assert.doesNotMatch(markup, /data-product-badge-create/);
});

test('editing product details leaves the badge to the badge management page', async () => {
  const api = await read('Admin/Areas/Store/Controllers/Api/StoreCatalogAdminApiController.cs', 'utf8');
  const markup = await read('Admin/Areas/Store/Views/Catalog/Index.cshtml', 'utf8');
  const script = await read('Admin/wwwroot/js/admin-data.js', 'utf8');
  const update = api.slice(api.indexOf('public async Task<IActionResult> UpdateProduct('), api.indexOf('public sealed class EditorOption'));
  assert.doesNotMatch(update, /product\.IdBadge\s*=/);
  assert.doesNotMatch(markup, /name="badgeId"/);
  assert.doesNotMatch(script, /productEditor\.elements\.badgeId|form\.elements\.badgeId/);
});
