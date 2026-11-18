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
  assert.match(api, /from line in db\.WarehouseDocumentLines[\s\S]*join item in db\.InventoryItems[\s\S]*item\.IdProduct == productId/);
  assert.match(api, /if \(await HasWarehouseDocuments\(id, cancellationToken\)\)/);
  assert.match(api, /baseLink\.ConversionFactor = 1m/);
  assert.match(markup, /id="product-edit-unit"[^>]*name="unitId"/);
  assert.doesNotMatch(markup, /data-product-badge-create/);
});

test('editor locks a documented product unit, sends it on save, and unlocks the next product', {skip: !existsSync(chromium.executablePath())}, async t => {
  const markup = await read('Admin/Areas/Store/Views/Catalog/Index.cshtml', 'utf8');
  const modal = markup.slice(markup.indexOf('<div class="modal fade" id="product-editor-modal"'), markup.indexOf('<div class="modal fade" id="product-benefits-modal"'));
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<div data-admin-section="products"><button data-product-edit="0">first</button><button data-product-edit="1">second</button></div>${modal}`);
  await page.evaluate(() => {
    document.querySelector('[data-admin-section]').__adminRows = [{id: 1}, {id: 2}];
    window.bootstrap = {Modal: {getOrCreateInstance: () => ({show() {}}), getInstance: () => ({hide() {}})}};
    window.AdminUI = {setPriceValue: (input, value) => { input.value = value; }, priceValue: input => Number(input.value), notify() {}, getJson: async (url, options) => {
      if (options) { window.saved = JSON.parse(options.body); return {message: 'ok'}; }
      const id = Number(url.split('/').at(-1));
      return {product: {id, code: 'p', name: 'Product', englishName: 'Product', idBrand: 1, idCategory: 2, idBaseUnit: 3, priceAmount: 100, discountPercent: 0, maxPurchaseQuantity: 5}, brands: [{id: 1, name: 'Brand'}], categories: [{id: 2, name: 'Category'}], units: [{id: 3, name: 'Unit'}], hasWarehouseDocuments: id === 1};
    }};
  });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-data.js'});
  await page.locator('[data-product-edit="0"]').click();
  await page.waitForFunction(() => document.querySelector('[name=unitId]').disabled);
  assert.match(await page.locator('[data-product-unit-help]').textContent(), /سند انبار/);
  await page.locator('[data-product-editor]').evaluate(form => form.requestSubmit());
  await page.waitForFunction(() => !!window.saved);
  assert.equal((await page.evaluate(() => window.saved)).unitId, 3);
  await page.locator('[data-product-edit="1"]').click();
  await page.waitForFunction(() => !document.querySelector('[name=unitId]').disabled);
  assert.match(await page.locator('[data-product-unit-help]').textContent(), /واحدهای فرعی/);
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
