import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFile, existsSync} from 'node:fs';
import {promisify} from 'node:util';
import {chromium} from '@playwright/test';

const read = promisify(readFile);

test('unit status is submitted from the definition form', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<div data-admin-definitions data-api="/api" data-definition-section="units" data-can-create="true" data-can-edit="true" data-can-deactivate="true" data-can-delete="true"><button data-definition-create>افزودن</button><form data-unit-group-form><input name="id"><input name="name"><input name="nameEn"><input name="sortOrder"><button type="submit"></button><button type="button" data-unit-group-cancel></button></form><div data-unit-tree></div><div data-definition-status></div></div><div id="admin-definition-modal"><h2 id="admin-definition-title"></h2><form data-definition-form><input name="id"><div data-definition-fields></div><button type="submit">ذخیره</button></form></div>`);
  await page.evaluate(() => {
    window.saved = [];
    window.unitActions = [];
    window.unitReadCount = 0;
    window.bootstrap = {Modal: {getOrCreateInstance: () => ({show() {}}), getInstance: () => ({hide() {}})}};
    window.AdminUI = {initStatusSwitch() {}, destroyDataTable() {}, refreshDataTable() {}, notify() {}, getJson: async (url, options) => {
      if (options.method === 'GET') {
        if (url.endsWith('/unit-groups')) return [{id: 1, name: 'جرم', idParent: null, isActive: true, sortOrder: 0, canDelete: false}];
        if (url.endsWith('/units')) { window.unitReadCount += 1; return [{id: 10, idParent: 1, parentName: 'جرم', nameFa: 'کیلوگرم', nameEn: 'Kilogram', isActive: true, canDelete: true}]; }
        return [];
      }
      if (options.method === 'DELETE') { window.unitActions.push(url); return {message: 'deleted'}; }
      if (url.endsWith('/units/10/deactivate')) window.unitActions.push(url);
      else window.saved.push(JSON.parse(options.body));
      return {message: 'ok'};
    }};
  });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-definitions.js'});
  await page.evaluate(() => window.AdminDefinitions.initialize(document));
  assert.equal(await page.locator('[data-unit-tree][role=tree]').count(), 1);
  assert.equal(await page.locator('[data-unit-tree] [role=treeitem]').count(), 2);
  assert.match(await page.locator('[data-unit-tree]').textContent(), /کیلوگرم/);
  assert.match(await page.locator('[data-unit-tree]').textContent(), /Kilogram/);
  const group = page.locator('[data-unit-tree] [role=treeitem]').first();
  assert.equal(await group.locator(':scope > .admin-permission-row [role=checkbox]').isDisabled(), true);
  assert.match(await group.innerText(), /برای غیرفعال‌کردن گروه، ۱ واحد فعال/u);
  await group.getByRole('button', {name: 'بازکردن جرم'}).click();
  const unit = page.locator('[data-unit-tree] [role=treeitem]').nth(1);
  assert.equal(await unit.getByRole('checkbox').getAttribute('aria-checked'), 'true');
  await unit.getByRole('button', {name: 'ویرایش'}).click();
  assert.equal(await page.locator('[data-definition-fields] [name=parentId]').inputValue(), '1');
  const readsBeforeStatusChange = await page.evaluate(() => window.unitReadCount);
  await unit.getByRole('checkbox').click();
  await page.waitForFunction(() => window.unitActions.length === 1);
  assert.deepEqual(await page.evaluate(() => window.unitActions), ['/api/units/10/deactivate']);
  assert.equal(await page.evaluate(() => window.unitReadCount), readsBeforeStatusChange);
  assert.equal(await unit.getByRole('checkbox').getAttribute('aria-checked'), 'false');
  assert.match(await unit.innerText(), /غیرفعال/u);
  assert.equal(await group.locator(':scope > .admin-permission-row [role=checkbox]').isDisabled(), false);
  assert.equal(await group.getByText(/برای غیرفعال‌کردن گروه/u).count(), 0);
  await unit.getByRole('button', {name: 'حذف'}).click();
  await page.waitForFunction(() => window.unitActions.length === 2);
  assert.deepEqual(await page.evaluate(() => window.unitActions), ['/api/units/10/deactivate', '/api/units/10']);
  assert.equal(await page.evaluate(() => window.unitReadCount), readsBeforeStatusChange);
  assert.equal(await page.locator('[data-unit-tree] [role=treeitem]').count(), 1);
  await page.locator('[data-definition-create]').click();
  await page.locator('[data-definition-form] [name=parentId]').selectOption('1');
  assert.equal(await page.locator('[data-definition-form] [name=isActive]').isChecked(), true);
  await page.locator('[data-definition-form] [name=nameFa]').fill('واحد');
  await page.locator('[data-definition-form] [name=nameEn]').fill('Unit');
  await page.locator('[data-definition-form] [name=isActive]').uncheck();
  await page.locator('[data-definition-form]').evaluate(form => form.requestSubmit());
  await page.waitForFunction(() => window.saved.length === 1);
  assert.equal((await page.evaluate(() => window.saved[0])).isActive, false);
  assert.equal((await page.evaluate(() => window.saved[0])).nameEn, 'Unit');
});

test('product editor limits base-unit changes to products without warehouse document lines', async () => {
  const api = await read('Admin/Areas/Store/Controllers/Api/StoreCatalogAdminApiController.cs', 'utf8');
  const markup = await read('Admin/Areas/Store/Views/Catalog/Index.cshtml', 'utf8');
  assert.match(api, /x\.Id == input\.UnitId && x\.IdParent != null && x\.IsActive/);
  assert.match(api, /from line in db\.WarehouseDocumentLines[\s\S]*join item in db\.InventoryItems[\s\S]*item\.IdProduct == productId/);
  assert.match(api, /if \(await HasWarehouseDocuments\(id, cancellationToken\)\)/);
  assert.match(api, /baseLink\.ConversionFactor = 1m/);
  assert.match(markup, /id="product-edit-unit"[^>]*name="unitId"/);
  assert.doesNotMatch(markup, /data-product-badge-create/);
});

test('warehouse unit hierarchy uses nullable parent links and keeps groups in Warehouse.Unit', async () => {
  const api = await read('Admin/Areas/Warehouse/Controllers/Api/WarehouseDefinitionsApiController.cs', 'utf8');
  const model = await read('Data/Infrastructure/Persistence/ShopDbContext.cs', 'utf8');
  const entityModel = await read('Data/Domain/Warehouse/Entities/WarehouseDefinitions.cs', 'utf8');
  const migration = await read('Shop/Features/Store/Database/Migrations/132-warehouse-unit-parent-tree.sql', 'utf8');
  const view = await read('Admin/Areas/Warehouse/Views/Features/Definitions.cshtml', 'utf8');
  assert.match(api, /"unit-groups" => Ok\(await db\.WarehouseUnits\.AsNoTracking\(\)\.Where\(x => x\.IdParent == null\)/);
  assert.match(api, /"units" => Ok\(await db\.WarehouseUnits\.AsNoTracking\(\)\.Where\(x => x\.IdParent != null\)/);
  assert.match(api, /entity\.IdParent = input\.ParentId/);
  assert.match(entityModel, /public long\? IdParent \{ get; set; \}/);
  assert.match(model, /HasForeignKey\(x => x\.IdParent\)\.OnDelete\(DeleteBehavior\.Restrict\)/);
  assert.doesNotMatch(model, /IsGroup|IdUnitGroup|WarehouseUnitGroup/);
  assert.match(migration, /\[IdParent\] BIGINT NULL/);
  assert.match(migration, /DROP COLUMN \[IdUnitGroup\]/);
  assert.match(migration, /DROP TABLE \[Warehouse\]\.\[UnitGroup\]/);
  assert.doesNotMatch(migration, /IsGroup/);
  assert.match(migration, /N'شمارشی'/);
  assert.match(view, /data-unit-tree/);
  assert.match(view, /data-unit-group-form/);
});

test('warehouse units expose Persian and English names without a code field', async () => {
  const model = await read('Data/Domain/Warehouse/Entities/WarehouseDefinitions.cs', 'utf8');
  const config = await read('Data/Infrastructure/Persistence/ShopDbContext.cs', 'utf8');
  const api = await read('Admin/Areas/Warehouse/Controllers/Api/WarehouseDefinitionsApiController.cs', 'utf8');
  const script = await read('Admin/wwwroot/js/admin-definitions.js', 'utf8');
  const schemaMigration = await read('Shop/Features/Store/Database/Migrations/130-warehouse-unit-name-languages.sql', 'utf8');
  const translationMigration = await read('Shop/Features/Store/Database/Migrations/131-warehouse-unit-english-names.sql', 'utf8');
  assert.match(model, /public string NameFa \{ get; set; \}/);
  assert.match(model, /public string\? NameEn \{ get; set; \}/);
  assert.doesNotMatch(model.slice(model.indexOf('public sealed class WarehouseUnit\n'), model.indexOf('public sealed class WarehouseParty')), /Code|public string Name \{|IsGroup|IdUnitGroup/);
  assert.match(config, /entity\.Property\(x => x\.NameFa\)\.HasMaxLength\(100\)\.IsRequired\(\)/);
  assert.match(api, /entity\.NameFa = input\.NameFa\.Trim\(\)/);
  assert.match(script, /\["nameFa", "نام فارسی"/);
  assert.match(script, /\["nameEn", "نام انگلیسی"/);
  assert.doesNotMatch(script.slice(script.indexOf('units: {'), script.indexOf('"document-types"')), /\["code"/);
  assert.match(schemaMigration, /sp_rename N'Warehouse\.Unit\.Name', N'NameFa'/);
  assert.match(schemaMigration, /DROP COLUMN \[Code\]/);
  assert.match(translationMigration, /WHEN 73 THEN N'Milliliter'/);
});

test('unused units and empty unit groups can be deleted only by full-access administrators', async () => {
  const api = await read('Admin/Areas/Warehouse/Controllers/Api/WarehouseDefinitionsApiController.cs', 'utf8');
  const view = await read('Admin/Areas/Warehouse/Views/Features/Definitions.cshtml', 'utf8');
  assert.match(api, /\[HttpDelete\("\{section\}\/\{id:long\}"\)\][\s\S]*?User\.IsInRole\("system\.full-access"\)/);
  assert.match(api, /!db\.WarehouseProductUnits\.Any\(link => link\.IdUnit == x\.Id\)/);
  assert.match(api, /!db\.WarehouseDocumentLines\.Any\(line => line\.IdUnit == x\.Id\)/);
  assert.match(api, /else if \(await db\.WarehouseUnits\.AnyAsync\(x => x\.IdParent == id/);
  assert.match(view, /data-can-delete="@\(User\.IsInRole\("system\.full-access"\)/);
});

test('warehouse definition pages rely on the sidebar instead of repeating the local feature links', async () => {
  const view = await read('Admin/Areas/Warehouse/Views/Features/Definitions.cshtml', 'utf8');
  const sidebar = await read('Admin/Views/Shared/_AdminSidebar.cshtml', 'utf8');
  const catalog = await read('Admin/Areas/Warehouse/Features/WarehouseAdminFeatureCatalog.cs', 'utf8');
  assert.doesNotMatch(view, /aria-label="تعریف‌های واحد و تنوع"/);
  assert.match(view, /data-unit-tree/);
  assert.match(view, /data-product-unit-tree/);
  assert.match(sidebar, /<span class="menu-text">واحدهای اندازه‌گیری<\/span>/);
  assert.match(catalog, /\["units"\] = new\("مدیریت انبار", "units", "واحدهای اندازه‌گیری"/);
});

test('editor locks a documented product unit, sends it on save, and unlocks the next product', {skip: !existsSync(chromium.executablePath())}, async t => {
  const markup = await read('Admin/Areas/Store/Views/Catalog/Index.cshtml', 'utf8');
  const modal = markup.slice(markup.indexOf('<div class="modal fade" id="product-editor-modal"'), markup.indexOf('<div class="modal fade" id="product-benefits-modal"'));
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<div data-admin-section="products"><button data-product-edit="0">first</button><button data-product-edit="1">second</button></div>${modal}`);
  await page.evaluate(() => {
    document.querySelector('[data-admin-section]').__adminRows = [{id: 1}, {id: 2}];
    window.bootstrap = {Modal: {getOrCreateInstance: () => ({show() {}}), getInstance: () => ({hide() {}})}};
    window.AdminUI = {setPriceValue: (input, value) => { input.value = value; }, priceValue: input => Number(input.value), notify() {}, getJson: async (url, options) => {
      if (options) { window.saved = JSON.parse(options.body); return {message: 'ok'}; }
      const id = Number(url.split('/').at(-1));
      return {product: {id, code: 'p', name: 'Product', englishName: 'Product', idBrand: 1, idCategory: 2, idBaseUnit: 3, priceAmount: 100, discountPercent: 0, maxPurchaseQuantity: 5}, brands: [{id: 1, name: 'Brand'}], categories: [{id: 2, name: 'Category'}], units: [{id: 3, name: 'Unit'}, {id: 4, name: 'Pack'}, {id: 5, name: 'Carton'}], unitConversions: [{idUnit: 4, unitName: 'Pack', conversionFactor: 12}], unavailableUnitIds: [], hasWarehouseDocuments: id === 1};
    }};
  });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-data.js'});
  await page.locator('[data-product-edit="0"]').click();
  await page.waitForFunction(() => document.querySelector('[name=unitId]').disabled);
  assert.match(await page.locator('[data-product-unit-help]').textContent(), /سند انبار/);
  await page.locator('[data-add-unit-conversion="edit"]').click();
  const conversionRows = page.locator('[data-unit-conversion-row]');
  await conversionRows.nth(1).locator('[data-unit-conversion-choice]').selectOption('5');
  await conversionRows.nth(1).locator('[name="conversionFactor"]').fill('24');
  await page.locator('[data-product-editor]').evaluate(form => form.requestSubmit());
  await page.waitForFunction(() => !!window.saved);
  const saved = await page.evaluate(() => window.saved);
  assert.equal(saved.unitId, 3);
  assert.deepEqual(saved.unitConversions, [{unitId: 4, conversionFactor: 12}, {unitId: 5, conversionFactor: 24}]);
  await page.locator('[data-product-edit="1"]').click();
  await page.waitForFunction(() => !document.querySelector('[name=unitId]').disabled);
  assert.match(await page.locator('[data-product-unit-help]').textContent(), /واحدهای فرعی/);
});

test('editing product details leaves the badge to the badge management page', async () => {
  const api = await read('Admin/Areas/Store/Controllers/Api/StoreCatalogAdminApiController.cs', 'utf8');
  const markup = await read('Admin/Areas/Store/Views/Catalog/Index.cshtml', 'utf8');
  const script = await read('Admin/wwwroot/js/admin-data.js', 'utf8');
  const update = api.slice(api.indexOf('public async Task<IActionResult> UpdateProduct('), api.indexOf('public sealed class EditorOption'));
  assert.doesNotMatch(update, /product\.IdBadge\s*=/);
  assert.doesNotMatch(markup, /name="badgeId"/);
  assert.doesNotMatch(script, /productEditor\.elements\.badgeId|form\.elements\.badgeId/);
});
