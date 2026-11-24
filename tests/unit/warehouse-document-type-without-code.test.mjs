import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('document type editor saves return classification without a code', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<div data-admin-definitions data-api="/api" data-definition-section="voucher-types" data-can-create="true" data-can-edit="true" data-can-deactivate="true"><button data-definition-create>افزودن</button><div data-definition-status></div><table data-admin-datatable><thead data-definition-head></thead><tbody data-definition-rows></tbody></table></div><div id="admin-definition-modal"><h2 id="admin-definition-title"></h2><form data-definition-form><input name="id"><div data-definition-fields></div><button type="submit">ذخیره</button></form></div>`);
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
  assert.equal(await page.locator('[data-definition-form] [name=code]').count(), 0);
  await page.locator('[data-definition-form] [name=name]').fill('بازگشت کالا');
  await page.locator('[data-definition-form] [name=direction]').selectOption('R');
  await page.locator('[data-definition-form] [name=isReturn]').check();
  await page.locator('[data-definition-form]').evaluate(form => form.requestSubmit());
  await page.waitForFunction(() => window.saved.length === 1);
  const saved = await page.evaluate(() => window.saved[0]);
  assert.equal(saved.isReturn, true);
  assert.equal('code' in saved, false);
});
