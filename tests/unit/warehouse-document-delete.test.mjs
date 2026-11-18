import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('warehouse document deletion is available only for drafts', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<div data-admin-documents data-api="/documents" data-document-feature="receipts" data-can-create="true" data-can-edit="true" data-can-delete="true"><button data-document-create>ثبت</button><div data-document-status></div><form data-document-filter><select name="warehouseId" data-document-warehouses></select></form><button data-document-reset></button><table data-admin-datatable><tbody data-document-rows></tbody></table><div data-document-pagination></div></div><div id="admin-document-modal"><form data-document-form><select name="warehouseId" data-document-warehouse></select><select data-document-types></select><span data-document-types-help></span><select data-document-plans></select><select data-document-parties></select><label data-reference-label></label><input data-document-reference><button data-document-line-add type="button"></button></form></div>`);
  await page.evaluate(() => {
    window.deleted = [];
    window.bootstrap = {Modal: {getOrCreateInstance: () => ({show(){}, hide(){}})}};
    const items = [
      {id: 1, number: 101, date: '2026-11-01', type: 'رسید', warehouse: 'انبار', salesPlan: 'طرح', party: 'مرجع', status: 'F', affectsStock: true},
      {id: 2, number: 102, date: '2026-11-02', type: 'رسید', warehouse: 'انبار', salesPlan: 'طرح', party: 'مرجع', status: 'D', affectsStock: true}
    ];
    window.AdminUI = {destroyDataTable() {}, refreshDataTable() {}, renderNumberedPagination() {}, notify() {}, formatDateTime(value) {return value;}, async getJson(url, options) {
      if (options.method === 'DELETE') { window.deleted.push(url); return {deleted: true}; }
      if (url.endsWith('/lookups')) return {warehouses: [], types: [], parties: [], plans: [], ownerLinks: [], inventoryItems: []};
      return {items: items.filter(item => !window.deleted.includes(`/documents/${item.id}`)), total: 2, pageSize: 50};
    }};
  });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-documents.js'});
  await page.evaluate(() => window.AdminDocuments.initialize(document));
  const rows = page.locator('[data-document-rows] tr');
  await rows.nth(1).waitFor();
  assert.equal(await rows.nth(0).getByRole('button', {name: 'حذف'}).isDisabled(), true);
  assert.match(await rows.nth(0).innerText(), /ابتدا سند را موقت کنید/);
  assert.equal(await rows.nth(1).getByRole('button', {name: 'حذف'}).isEnabled(), true);
  await rows.nth(1).getByRole('button', {name: 'حذف'}).click();
  await page.waitForFunction(() => window.deleted.length === 1);
  assert.deepEqual(await page.evaluate(() => window.deleted), ['/documents/2']);
});
