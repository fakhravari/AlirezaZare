import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('warehouse allocation stores its name directly without a warehouse-type lookup', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<div data-admin-definitions data-api="/api" data-definition-section="warehouses" data-full-access="true" data-can-create="true" data-can-edit="true" data-can-deactivate="false"><button data-definition-create></button><div data-definition-status></div><table data-admin-datatable><thead data-definition-head></thead><tbody data-definition-rows></tbody></table></div><div id="admin-definition-modal"><form data-definition-form><input name="id"><div data-definition-fields></div><button type="submit">save</button></form></div>`);
  await page.evaluate(() => {
    window.requests = [];
    window.AdminUI = {
      destroyDataTable() {}, refreshDataTable() {}, initStatusSwitch() {}, notify() {},
      getJson: async (url, options = {}) => {
        if (url.endsWith('/lookups')) return {warehouses: [], companies: [{id: 1, name: 'برق شیراز', isActive: true}], plans: [{id: 2, displayName: 'دیجی‌اف', isActive: true}], products: [], units: []};
        if (url.endsWith('/warehouses') && !options.method) return [];
        if (url.endsWith('/warehouses') && options.method === 'POST') { window.requests.push({url, body: JSON.parse(options.body)}); return {message: 'ok'}; }
        throw new Error(`Unexpected request: ${url}`);
      }
    };
  });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-definitions.js'});
  await page.evaluate(() => window.AdminDefinitions.initialize(document));
  await page.locator('[data-definition-create]').click();
  const form = page.locator('[data-definition-form]');
  assert.equal(await form.locator('[name=name]').getAttribute('type'), 'text');
  await form.locator('[name=companyId]').selectOption('1');
  await form.locator('[name=code]').fill('SALES-MAIN');
  await form.locator('[name=name]').fill('انبار فروش');
  await form.locator('[name=ownerSalesPlanId]').selectOption('2');
  await form.locator('[type=submit]').click();
  await page.waitForFunction(() => window.requests.length === 1);
  assert.deepEqual(await page.evaluate(() => window.requests[0]), {
    url: '/api/warehouses',
    body: {companyId: 1, code: 'SALES-MAIN', name: 'انبار فروش', isSalesWarehouse: false, ownerSalesPlanId: 2, isSiteWarehouse: false, isActive: true}
  });
});
