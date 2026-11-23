import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync, readFileSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('last active variant action is disabled using all variants, even when filtered', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless:true}); t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<div data-admin-definitions data-api="/api" data-definition-section="variants" data-can-create="true" data-can-edit="false" data-can-deactivate="true"><select data-variant-product-filter><option value="">همه</option></select><select data-variant-status-filter><option value="">همه</option><option value="active">فعال</option><option value="inactive">غیرفعال</option></select><div data-definition-status></div><button data-definition-create></button><table data-admin-datatable><thead data-definition-head></thead><tbody data-definition-rows></tbody></table></div><div id="admin-definition-modal"><form data-definition-form><input name="id"><div data-definition-fields></div></form></div>`);
  await page.evaluate(() => { window.AdminUI = {destroyDataTable(){},refreshDataTable(){},notify(){},getJson:async url => url.endsWith('/variant-names') ? [] : [
    {id:1,idProduct:10,productName:'الف',name:'تنها فعال',isActive:true,canDeactivate:false,deactivateBlockReason:'حداقل یک تنوع فعال باید باقی بماند'},
    {id:2,idProduct:10,productName:'الف',name:'غیرفعال',isActive:false},
    {id:3,idProduct:20,productName:'ب',name:'اول',isActive:true,isDefault:true,canDeactivate:false,deactivateBlockReason:'تنوع پیش‌فرض است'},
    {id:4,idProduct:20,productName:'ب',name:'دوم',isActive:true,canDeactivate:true}
  ]}; });
  await page.addScriptTag({path:'Admin/wwwroot/js/admin-definitions.js'});
  await page.evaluate(() => window.AdminDefinitions.initialize(document));
  await page.locator('[data-variant-status-filter]').selectOption('active');
  let rows=page.locator('[data-definition-rows] tr');
  const last=rows.filter({hasText:'تنها فعال'}).locator('button');
  assert.equal(await last.isDisabled(),true);
  assert.match(await last.getAttribute('title'), /حداقل یک تنوع فعال/);
  const defaultButton = rows.filter({hasText:'اول'}).locator('button');
  assert.equal(await defaultButton.isDisabled(),true);
  assert.match(await defaultButton.getAttribute('title'), /پیش‌فرض/);
  assert.equal(await rows.filter({hasText:'دوم'}).locator('button').isEnabled(),true);
});

test('deactivation and update endpoints both guard the last active variant', () => {
  const source=readFileSync('Admin/Areas/Warehouse/Controllers/Api/WarehouseDefinitionsApiController.cs','utf8');
  assert.ok(source.includes('variant.IsActive && !await db.ProductVariants.AnyAsync(x => x.IdProduct == variant.IdProduct && x.Id != variant.Id && x.IsActive'));
  assert.ok(source.includes('id.HasValue && entity.IsActive && !input.IsActive'));
  assert.ok(source.includes('x.IdProduct == productId && x.Id != entity.Id && x.IsActive'));
  assert.equal((source.match(/آخرین تنوع فعال را نمی‌توان غیرفعال کرد/g) || []).length, 2);
  assert.ok(source.includes('if (variant.IsDefault)'));
  assert.ok(source.includes('if (entity.IsDefault && !input.IsActive)'));
  assert.ok(source.includes('if (section is not ("units" or "unit-groups")) return NotFound()')); 
});
