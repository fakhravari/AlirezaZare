import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync, readFileSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('variant filters combine product and status and retain date order', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless:true}); t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<div data-admin-definitions data-api="/api" data-definition-section="variants" data-can-create="true" data-can-edit="false" data-can-deactivate="false"><select data-variant-product-filter><option value="">همهٔ کالاها</option></select><select data-variant-status-filter><option value="">همه</option><option value="active">فعال</option><option value="inactive">غیرفعال</option></select><div data-definition-status></div><button data-definition-create>افزودن</button><table data-admin-datatable><thead data-definition-head></thead><tbody data-definition-rows></tbody></table></div><div id="admin-definition-modal"><form data-definition-form><input name="id"><div data-definition-fields></div></form></div>`);
  await page.evaluate(() => {
    window.AdminUI = {destroyDataTable(){},refreshDataTable(){},formatDateTime(value){return value;},notify(){},getJson:async url => url.endsWith('/variant-names') ? [] : [
      {id:3,idProduct:10,productName:'کالای الف',name:'جدید فعال',createdAtUtc:'2026-11-21',isActive:true},
      {id:2,idProduct:20,productName:'کالای ب',name:'میانی فعال',createdAtUtc:'2026-11-20',isActive:true},
      {id:1,idProduct:10,productName:'کالای الف',name:'قدیم غیرفعال',createdAtUtc:'2026-11-19',isActive:false}
    ]};
  });
  await page.addScriptTag({path:'Admin/wwwroot/js/admin-definitions.js'});
  await page.evaluate(() => window.AdminDefinitions.initialize(document));
  const rows=page.locator('[data-definition-rows] tr');
  assert.deepEqual(await rows.locator('td:nth-child(2)').allTextContents(), ['جدید فعال','میانی فعال','قدیم غیرفعال']);
  assert.equal(await page.locator('[data-variant-product-filter] option').count(),3);
  await page.locator('[data-variant-product-filter]').selectOption('10');
  assert.deepEqual(await rows.locator('td:nth-child(2)').allTextContents(), ['جدید فعال','قدیم غیرفعال']);
  await page.locator('[data-variant-status-filter]').selectOption('inactive');
  assert.deepEqual(await rows.locator('td:nth-child(2)').allTextContents(), ['قدیم غیرفعال']);
  await page.locator('[data-variant-product-filter]').selectOption('20');
  assert.equal(await rows.count(),0);
  assert.match(await page.locator('[data-definition-status]').textContent(), /تنوعی/);
  await page.locator('[data-variant-status-filter]').selectOption('active');
  assert.deepEqual(await rows.locator('td:nth-child(2)').allTextContents(), ['میانی فعال']);
  await page.locator('[data-variant-product-filter]').selectOption('');
  await page.locator('[data-variant-status-filter]').selectOption('');
  assert.equal(await rows.count(),3);
});

test('variant API returns newest creation dates first with stable ordering', () => {
  const source = readFileSync('Data/Infrastructure/Warehouse/WarehouseDefinitionsAdminData.cs','utf8');
  assert.ok(source.includes('.OrderByDescending(variantProduct => variantProduct.variant.CreatedAtUtc)'));
  assert.ok(source.includes('.ThenByDescending(variantProduct => variantProduct.variant.Id)'));
  assert.match(source, /variantProduct\.variant\.IsActive,[\s\S]*?variantProduct\.variant\.CreatedAtUtc/);
  assert.ok(source.includes('entity.CreatedAtUtc = DateTime.UtcNow;'));
});
