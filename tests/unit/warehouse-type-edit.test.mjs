import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('warehouse definition rename updates list and allocations, with cancel and permissions', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<div data-admin-definitions data-api="/api" data-definition-section="warehouses" data-full-access="true" data-can-create="true" data-can-edit="true" data-can-deactivate="false"><form data-warehouse-type-form><input name="name" required maxlength="200"><button type="submit">افزودن نام انبار</button><button type="button" data-warehouse-type-cancel hidden>انصراف</button></form><ul data-warehouse-types></ul><button data-definition-create></button><div data-definition-status></div><table data-admin-datatable><thead data-definition-head></thead><tbody data-definition-rows></tbody></table></div><div id="admin-definition-modal"><form data-definition-form><input name="id"><div data-definition-fields></div><button type="submit">save</button></form></div>`);
  await page.evaluate(() => {
    window.warehouseName = 'انبار اول'; window.requests = [];
    window.AdminUI = {destroyDataTable() {}, refreshDataTable() {}, notify() {}, getJson: async (url, options) => {
      if (options.method === 'PUT') { window.requests.push({url, body: JSON.parse(options.body)}); window.warehouseName = JSON.parse(options.body).name; return {message: 'ok'}; }
      if (url.endsWith('/warehouse-types')) return [{id: 7, name: window.warehouseName}];
      if (url.endsWith('/warehouses')) return [{id: 3, name: window.warehouseName, companyName: 'شرکت', code: 'W', isActive: true}];
      throw new Error('Unexpected request');
    }};
  });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-definitions.js'});
  await page.evaluate(() => window.AdminDefinitions.initialize(document));
  const form = page.locator('[data-warehouse-type-form]');
  await page.getByRole('button', {name: 'ویرایش نام انبار اول'}).click();
  assert.equal(await form.locator('[name=name]').inputValue(), 'انبار اول');
  await form.locator('[name=name]').fill('انبار اصلاح شده');
  await form.locator('[type=submit]').click();
  await page.waitForFunction(() => window.requests.length === 1 && document.querySelector('[data-definition-rows]').textContent.includes('انبار اصلاح شده'));
  assert.deepEqual(await page.evaluate(() => window.requests[0]), {url: '/api/warehouse-types/7', body: {name: 'انبار اصلاح شده'}});
  assert.equal(await form.locator('[name=name]').inputValue(), '');
  await page.getByRole('button', {name: 'ویرایش نام انبار اصلاح شده'}).click();
  await page.locator('[data-warehouse-type-cancel]').click();
  assert.equal(await form.locator('[name=name]').inputValue(), '');
  assert.equal(await form.locator('[type=submit]').innerText(), 'افزودن نام انبار');
  assert.equal(await page.evaluate(() => window.requests.length), 1);
  await page.evaluate(async () => {
    const old = document.querySelector('[data-admin-definitions]');
    const replacement = old.cloneNode(true); replacement.dataset.canCreate = 'false'; old.replaceWith(replacement);
    await window.AdminDefinitions.initialize(document);
  });
  assert.equal(await form.isHidden(), true);
  await page.getByRole('button', {name: 'ویرایش نام انبار اصلاح شده'}).click();
  assert.equal(await form.isVisible(), true);
  await page.locator('[data-warehouse-type-cancel]').click();
  assert.equal(await form.isHidden(), true);
  await page.evaluate(async () => {
    const old = document.querySelector('[data-admin-definitions]');
    const replacement = old.cloneNode(true); replacement.dataset.fullAccess = 'false'; old.replaceWith(replacement);
    await window.AdminDefinitions.initialize(document);
  });
  assert.equal(await page.getByRole('button', {name: 'ویرایش نام انبار اصلاح شده'}).count(), 0);
});
