import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('warehouse document editor keeps the Persian and submitted dates in sync', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<div data-admin-vouchers data-api="/vouchers" data-voucher-feature="receipts" data-can-create="true" data-can-edit="true" data-can-delete="false">
    <button data-voucher-create>ثبت سند</button><div data-voucher-status></div><form data-voucher-filter><select name="status"><option value="">همهٔ وضعیت‌ها</option></select><select name="warehouseId" data-voucher-warehouses></select></form><button data-voucher-reset></button><table data-admin-datatable><tbody data-voucher-rows></tbody></table><div data-voucher-pagination></div></div>
    <div id="admin-voucher-modal"><h2 id="admin-voucher-title"></h2><form data-voucher-form><input name="id"><select name="warehouseId" data-voucher-warehouse></select><select name="voucherTypeId" data-voucher-types></select><span data-voucher-types-help></span><select name="salesPlanId" data-voucher-plans></select><select name="partyId" data-voucher-parties></select><label for="voucher-date">تاریخ سند</label><input id="voucher-date" name="voucherDate" type="date"><select name="status"><option value="D">موقت</option><option value="F">قطعی</option></select><input name="reason"><label data-reference-label></label><input name="externalReference" data-voucher-reference><table><tbody data-voucher-lines></tbody></table><button type="button" data-voucher-line-add>افزودن قلم</button><button type="submit">ذخیره</button></form></div>`);
  await page.addScriptTag({path: 'Admin/wwwroot/lib/bootstrap/bootstrap.bundle.min.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/lib/persian-datetimepicker/mds.bs.datetimepicker.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-persian-datetime.js'});
  await page.evaluate(() => {
    window.saved = [];
    window.bootstrap.Modal.getOrCreateInstance = () => ({show() {}, hide() {}});
    const detail = {id: 12, warehouseId: 2, voucherTypeId: 3, partyId: 4, salesPlanId: 5, voucherDate: '2026-11-04', status: 'F', reason: 'رسید', externalReference: null, lines: [{inventoryItemId: 6, unitId: 7, salesPlanId: 5, quantity: 2}]};
    const item = {id: 12, voucherNumber: 101, voucherDate: detail.voucherDate, voucherType: 'رسید', warehouse: 'انبار', salesPlan: 'طرح', party: 'مرجع', status: 'F', affectsStock: true};
    window.AdminUI = {destroyDataTable() {}, refreshDataTable() {}, renderNumberedPagination() {}, notify(message) { if (message.includes('ناموفق')) throw Error(message); }, formatDateTime(value) {return value;}, async getJson(url, options) {
      if (options.method === 'PUT') { window.saved.push(JSON.parse(options.body)); return {message: 'ok'}; }
      if (url.endsWith('/lookups')) return {statuses: [{code: "D", nameFa: "موقت"}, {code: "F", nameFa: "قطعی"}], warehouses: [{id: 2, name: 'انبار', companyName: 'شرکت', code: 'A'}], types: [{id: 3, name: 'رسید', code: 'R', direction: 'R'}], parties: [{id: 4, name: 'مرجع', partyType: 'Other'}], plans: [{id: 5, displayName: 'طرح'}], ownerLinks: [{idWarehouse: 2, idStockOwner: 5}], inventoryItems: [{id: 6, productName: 'کالا', units: [{id: 7, name: 'عدد', conversionFactor: 1}]}]};
      if (url.endsWith('/12')) return detail;
      return {items: [item], total: 1, pageSize: 50};
    }};
  });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-vouchers.js'});
  await page.evaluate(() => window.AdminVouchers.initialize(document));
  await page.getByRole('button', {name: 'ویرایش', exact: true}).click();
  await page.waitForFunction(() => document.querySelector('#voucher-date').value === '2026-11-04' && document.querySelector('#voucher-date-persian').value);
  assert.match(await page.locator('#voucher-date-persian').inputValue(), /۱۴۰۵\/۰۸\/۱۳/);
  await page.locator('[data-voucher-form] [type=submit]').click();
  await page.waitForFunction(() => window.saved.length === 1);
  assert.equal((await page.evaluate(() => window.saved[0])).voucherDate, '2026-11-04');
  await page.getByRole('button', {name: 'ثبت سند', exact: true}).click();
  await page.waitForFunction(() => document.querySelector('#voucher-date').value && document.querySelector('#voucher-date-persian').value);
  const now = await page.evaluate(() => new Date(Date.now() - new Date().getTimezoneOffset() * 60000).toISOString().slice(0, 10));
  assert.equal(await page.locator('#voucher-date').inputValue(), now);
  await page.getByRole('button', {name: 'ویرایش', exact: true}).click();
  await page.waitForFunction(() => document.querySelector('#voucher-date').value === '2026-11-04' && document.querySelector('#voucher-date-persian').value.includes('۱۴۰۵/۰۸/۱۳'));
  assert.match(await page.locator('#voucher-date-persian').inputValue(), /۱۴۰۵\/۰۸\/۱۳/);
});
