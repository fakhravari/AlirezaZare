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
        if (url.endsWith('/variant-names')) return [{id:1,idParent:null,name:'رنگ',isActive:true},{id:11,idParent:1,name:'آبی',isActive:true}];
        if (url.endsWith('/variants')) return [{id: 21, idProduct: 10, productName: 'کالای نمونه', name: 'آبی', idVariantName: 11, isDefault: true, isActive: true, attributes: [{attributeId: 5, valueId: 8, key: 'color', label: 'رنگ', value: 'black', display: 'مشکی'}]}];
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
  assert.equal(saved.body.variantNameId, 11);
  assert.equal(await page.locator('select[name=variantNameId] option:checked').textContent(), 'رنگ / آبی');
  assert.equal('code' in saved.body, false);
  assert.deepEqual(saved.body.attributes, [{attributeId: 5, valueId: 9}]);
});

test('shared variant name tree creates children and edits existing nodes', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<div data-admin-definitions data-api="/api" data-definition-section="variants" data-can-create="true" data-can-edit="true" data-can-deactivate="false"><form data-variant-name-form><input name="id"><select name="idParent"></select><input name="name"><button type="submit">ثبت نام</button><button type="button" data-variant-name-cancel hidden>انصراف</button></form><ul data-variant-name-tree></ul><div data-definition-status></div><button data-definition-create></button><table data-admin-datatable><thead data-definition-head></thead><tbody data-definition-rows></tbody></table></div><div id="admin-definition-modal"><h2 id="admin-definition-title"></h2><form data-definition-form><input name="id"><div data-definition-fields></div><button type="submit">ذخیره</button></form></div>`);
  await page.evaluate(() => {
    window.requests = [];
    window.AdminUI = {destroyDataTable() {}, refreshDataTable() {}, notify() {}, initStatusSwitch() {}, getJson: async (url, options = {}) => {
      const method = options.method || 'GET'; window.requests.push({url, method, body: options.body ? JSON.parse(options.body) : null});
      if (method !== 'GET') return {message: 'ذخیره شد'};
      if (url.endsWith('/variant-names')) return [{id: 1, idParent: null, name: 'رنگ', isActive: true}, {id: 2, idParent: 1, name: 'آبی', isActive: true}];
      if (url.endsWith('/variants')) return [];
      return {warehouses: [], products: []};
    }};
  });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-definitions.js'});
  await page.evaluate(() => window.AdminDefinitions.initialize(document));
  const form = page.locator('[data-variant-name-form]');
  assert.equal(await form.locator('[name=idParent] option[value="2"]').textContent(), 'رنگ / آبی');
  await form.locator('[name=idParent]').selectOption('1');
  await form.locator('[name=name]').fill('سبز');
  await form.locator('[type=submit]').click();
  await page.waitForFunction(() => window.requests.some(r => r.method === 'POST'));
  assert.deepEqual((await page.evaluate(() => window.requests.find(r => r.method === 'POST'))).body, {name: 'سبز', idParent: 1});
  await page.locator('[data-variant-name-tree] button').last().click();
  assert.equal(await form.locator('[name=name]').inputValue(), 'آبی');
  await form.locator('[name=name]').fill('لاجوردی');
  await form.locator('[type=submit]').click();
  await page.waitForFunction(() => window.requests.some(r => r.method === 'PUT'));
  const saved = await page.evaluate(() => window.requests.find(r => r.method === 'PUT'));
  assert.equal(saved.url, '/api/variant-names/2');
  assert.deepEqual(saved.body, {name: 'لاجوردی', idParent: 1});
});
