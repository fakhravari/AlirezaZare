import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync, readFileSync} from 'node:fs';
import {chromium} from '@playwright/test';

for (const [feature, expected] of [['dispatches', 'حواله دستی'], ['returns', 'بازگشت کالا']]) test(`${feature} selects manual types using their classification`, {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<div data-admin-documents data-api="/documents" data-document-feature="${feature}" data-can-create="true" data-can-edit="true" data-can-delete="false">
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
      if (url.endsWith('/lookups')) return {statuses: [{code: "D", nameFa: "موقت"}, {code: "F", nameFa: "قطعی"}], warehouses: [{id: 2, name: 'انبار', companyName: 'شرکت', code: 'A'}], types: [{id: 3, name: 'حواله دستی', direction: 'I', isSystemSalesDispatch: false}, {id: 9, name: 'حواله فروش', direction: 'I', isSystemSalesDispatch: true}, {id: 10, name: 'بازگشت کالا', direction: 'R', isReturn: true}], parties: [{id: 4, name: 'مرجع', partyType: 'Other'}], plans: [{id: 5, displayName: 'طرح'}], ownerLinks: [{idWarehouse: 2, idStockOwner: 5}], inventoryItems: [{id: 6, productName: 'کالا', units: [{id: 7, name: 'عدد', conversionFactor: 1}]}]};
      if (url.endsWith('/12')) return detail;
      return {items: [item], total: 1, pageSize: 50};
    }};
  });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-documents.js'});
  await page.evaluate(() => window.AdminDocuments.initialize(document));
  assert.deepEqual(await page.locator('[data-document-types] option').allTextContents(), ['انتخاب نوع سند', expected]);
});

test('manual documents reject the sales dispatch type without changing the order dispatch path', () => {
  const api = readFileSync('Admin/Areas/Warehouse/Controllers/Api/WarehouseDocumentsApiController.cs', 'utf8');
  const dispatch = readFileSync('Admin/Areas/Store/Controllers/Api/StorePaidOrdersDispatchApiController.cs', 'utf8');
  assert.ok(api.includes('x.IsActive && !x.IsSystemSalesDispatch'));
  assert.ok(api.includes('if (selectedType.IsSystemSalesDispatch)'));
  assert.ok(dispatch.includes('x.IsSystemSalesDispatch && x.IsActive'));
});
