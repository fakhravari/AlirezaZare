import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('warehouse document editor keeps the Persian and submitted dates in sync', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<div data-admin-documents data-api="/documents" data-document-feature="receipts" data-can-create="true" data-can-edit="true" data-can-delete="false">
    <button data-document-create>ثبت سند</button><div data-document-status></div><form data-document-filter><select name="status"><option value="">همهٔ وضعیت‌ها</option></select><select name="warehouseId" data-document-warehouses></select></form><button data-document-reset></button><table data-admin-datatable><tbody data-document-rows></tbody></table><div data-document-pagination></div></div>
    <div id="admin-document-modal"><h2 id="admin-document-title"></h2><form data-document-form><input name="id"><select name="warehouseId" data-document-warehouse></select><select name="documentTypeId" data-document-types></select><span data-document-types-help></span><select name="salesPlanId" data-document-plans></select><select name="partyId" data-document-parties></select><label for="document-date">تاریخ سند</label><input id="document-date" name="documentDate" type="date"><select name="status"><option value="D">موقت</option><option value="F">قطعی</option></select><input name="reason"><label data-reference-label></label><input name="externalReference" data-document-reference><table><tbody data-document-lines></tbody></table><button type="button" data-document-line-add>افزودن قلم</button><button type="submit">ذخیره</button></form></div>`);
  await page.addScriptTag({path: 'Admin/wwwroot/lib/bootstrap/bootstrap.bundle.min.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/lib/persian-datetimepicker/mds.bs.datetimepicker.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-persian-datetime.js'});
  await page.evaluate(() => {
    window.saved = [];
    window.bootstrap.Modal.getOrCreateInstance = () => ({show() {}, hide() {}});
    const detail = {id: 12, warehouseId: 2, documentTypeId: 3, partyId: 4, salesPlanId: 5, date: '2026-11-04', status: 'F', reason: 'رسید', externalReference: null, lines: [{inventoryItemId: 6, unitId: 7, salesPlanId: 5, quantity: 2}]};
    const item = {id: 12, number: 101, date: detail.date, type: 'رسید', warehouse: 'انبار', salesPlan: 'طرح', party: 'مرجع', status: 'F', affectsStock: true};
    window.AdminUI = {destroyDataTable() {}, refreshDataTable() {}, renderNumberedPagination() {}, notify(message) { if (message.includes('ناموفق')) throw Error(message); }, formatDateTime(value) {return value;}, async getJson(url, options) {
      if (options.method === 'PUT') { window.saved.push(JSON.parse(options.body)); return {message: 'ok'}; }
      if (url.endsWith('/lookups')) return {statuses: [{code: "D", nameFa: "موقت"}, {code: "F", nameFa: "قطعی"}], warehouses: [{id: 2, name: 'انبار', companyName: 'شرکت', code: 'A'}], types: [{id: 3, name: 'رسید', code: 'R', direction: 'R'}], parties: [{id: 4, name: 'مرجع', partyType: 'Other'}], plans: [{id: 5, displayName: 'طرح'}], ownerLinks: [{idWarehouse: 2, idStockOwner: 5}], inventoryItems: [{id: 6, productName: 'کالا', units: [{id: 7, name: 'عدد', conversionFactor: 1}]}]};
      if (url.endsWith('/12')) return detail;
      return {items: [item], total: 1, pageSize: 50};
    }};
  });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-documents.js'});
  await page.evaluate(() => window.AdminDocuments.initialize(document));
  await page.getByRole('button', {name: 'ویرایش', exact: true}).click();
  await page.waitForFunction(() => document.querySelector('#document-date').value === '2026-11-04' && document.querySelector('#document-date-persian').value);
  assert.match(await page.locator('#document-date-persian').inputValue(), /۱۴۰۵\/۰۸\/۱۳/);
  await page.locator('[data-document-form] [type=submit]').click();
  await page.waitForFunction(() => window.saved.length === 1);
  assert.equal((await page.evaluate(() => window.saved[0])).documentDate, '2026-11-04');
  await page.getByRole('button', {name: 'ثبت سند', exact: true}).click();
  await page.waitForFunction(() => document.querySelector('#document-date').value && document.querySelector('#document-date-persian').value);
  const now = await page.evaluate(() => new Date(Date.now() - new Date().getTimezoneOffset() * 60000).toISOString().slice(0, 10));
  assert.equal(await page.locator('#document-date').inputValue(), now);
  await page.getByRole('button', {name: 'ویرایش', exact: true}).click();
  await page.waitForFunction(() => document.querySelector('#document-date').value === '2026-11-04' && document.querySelector('#document-date-persian').value.includes('۱۴۰۵/۰۸/۱۳'));
  assert.match(await page.locator('#document-date-persian').inputValue(), /۱۴۰۵\/۰۸\/۱۳/);
});
