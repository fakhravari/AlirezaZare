import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('warehouse document deletion is available only for drafts', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<div data-admin-vouchers data-api="/vouchers" data-voucher-feature="receipts" data-can-create="true" data-can-edit="true" data-can-delete="true"><button data-voucher-create>ثبت</button><div data-voucher-status></div><form data-voucher-filter><select name="status"><option value="">همهٔ وضعیت‌ها</option></select><select name="warehouseId" data-voucher-warehouses></select></form><button data-voucher-reset></button><table data-admin-datatable><tbody data-voucher-rows></tbody></table><div data-voucher-pagination></div></div><div id="admin-voucher-modal"><form data-voucher-form><select name="status"></select><select name="warehouseId" data-voucher-warehouse></select><select data-voucher-types></select><span data-voucher-types-help></span><select data-voucher-plans></select><select data-voucher-parties></select><label data-reference-label></label><input data-voucher-reference><button data-voucher-line-add type="button"></button></form></div>`);
  await page.evaluate(() => {
    window.deleted = [];
    window.bootstrap = {Modal: {getOrCreateInstance: () => ({show(){}, hide(){}})}};
    const items = [
      {id: 1, voucherNumber: 101, voucherDate: '2026-11-01', voucherType: 'رسید', warehouse: 'انبار', salesPlan: 'طرح', party: 'مرجع', status: 'F', affectsStock: true},
      {id: 2, voucherNumber: 102, voucherDate: '2026-11-02', voucherType: 'رسید', warehouse: 'انبار', salesPlan: 'طرح', party: 'مرجع', status: 'D', affectsStock: true}
    ];
    window.AdminUI = {destroyDataTable() {}, refreshDataTable() {}, renderNumberedPagination() {}, notify() {}, formatDateTime(value) {return value;}, async getJson(url, options) {
      if (options.method === 'DELETE') { window.deleted.push(url); return {deleted: true}; }
      if (url.endsWith('/lookups')) return {statuses: [{code: "D", nameFa: "موقت"}, {code: "F", nameFa: "قطعی"}], warehouses: [], types: [], parties: [], plans: [], ownerLinks: [], inventoryItems: []};
      return {items: items.filter(item => !window.deleted.includes(`/vouchers/${item.id}`)), total: 2, pageSize: 50};
    }};
  });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-vouchers.js'});
  await page.evaluate(() => window.AdminVouchers.initialize(document));
  const rows = page.locator('[data-voucher-rows] tr');
  await rows.nth(1).waitFor();
  assert.equal(await rows.nth(0).getByRole('button', {name: 'حذف'}).isDisabled(), true);
  assert.match(await rows.nth(0).innerText(), /ابتدا سند را موقت کنید/);
  assert.equal(await rows.nth(1).getByRole('button', {name: 'حذف'}).isEnabled(), true);
  await rows.nth(1).getByRole('button', {name: 'حذف'}).click();
  await page.waitForFunction(() => window.deleted.length === 1);
  assert.deepEqual(await page.evaluate(() => window.deleted), ['/vouchers/2']);
});
