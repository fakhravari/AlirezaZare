import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';

test('warehouse stock belongs to reports and retains independent permission and active state', async () => {
  const sidebar = await readFile('Admin/Views/Shared/_AdminSidebar.cshtml', 'utf8');
  const structureStart = sidebar.indexOf('<div id="system-definitions"');
  const reportsStart = sidebar.indexOf('<div id="warehouse-reports"');
  const structure = sidebar.slice(structureStart, sidebar.indexOf('</details>', structureStart));
  const reports = sidebar.slice(reportsStart, sidebar.indexOf('</details>', reportsStart));
  assert.match(sidebar, /menu-text">تعاریف پایهٔ انبار</);
  assert.doesNotMatch(structure, /asp-controller="Stock"/);
  assert.match(reports, /Can\("Warehouse:Stock:Index"\)[\s\S]*asp-controller="Stock"/);
  assert.match(sidebar, /bool canWarehouseReports = Can\("Warehouse:Features:Index:movements"\) \|\| Can\("Warehouse:Stock:Index"\)/);
  assert.match(sidebar, /bool reportsOpen = inWarehouse && \(IsFeature\("movements"\) \|\| string\.Equals\(controller, "Stock"/);
  assert.doesNotMatch(sidebar.match(/bool warehouseStructureOpen[^\n]+/)[0], /"Stock"/);
});


test('warehouse movement reports have separate menu routes with the existing permission', async () => {
  const sidebar = await readFile('Admin/Views/Shared/_AdminSidebar.cshtml', 'utf8');
  const reportsStart = sidebar.indexOf('<div id="warehouse-reports"');
  const reports = sidebar.slice(reportsStart, sidebar.indexOf('</details>', reportsStart));
  for (const mode of ['stock', 'summary', 'audit']) assert.ok(reports.includes('movementSection == "' + mode + '"'));
  for (const mode of ['summary', 'audit']) assert.ok(reports.includes('asp-route-section="' + mode + '"'));
  assert.equal((reports.match(/Can\("Warehouse:Features:Index:movements"\)/g) || []).length, 3);
  const view = await readFile('Admin/Areas/Warehouse/Views/Features/Movements.cshtml', 'utf8');
  assert.ok(view.includes('data-mode="@ViewData["MovementSection"]"'));
  assert.doesNotMatch(view, /data-movement-mode=/);
});

test('variant name reference and variant definitions have separate pages and permissions', async () => {
  const sidebar = await readFile('Admin/Views/Shared/_AdminSidebar.cshtml', 'utf8');
  assert.match(sidebar, /asp-route-feature="variant-names"[\s\S]*?تنوع محصول/);
  assert.match(sidebar, /Warehouse:Features:Index:variant-names/);
  assert.match(sidebar, /asp-route-feature="variants"[\s\S]*?اتصال تنوع به محصول/);
  assert.match(sidebar, /Warehouse:Features:Index:variants/);
  const view = await readFile('Admin/Areas/Warehouse/Views/Features/Definitions.cshtml', 'utf8');
  assert.match(view, /Model\.FeatureId == "variant-names"[\s\S]*data-variant-name-tree/);
  assert.match(view, /Model\.FeatureId != "variant-names"[\s\S]*data-admin-datatable/);
  const api = await readFile('Data/Infrastructure/Warehouse/WarehouseDefinitionsAdminData.cs', 'utf8');
  assert.match(api, /SaveVariantName[\s\S]*?if \(!CanAccessVariantNames\(\)\) return Denied\(\)/);
  assert.match(api, /private bool CanAccessVariantNames\(\)[\s\S]*Warehouse:Features:Index:variant-names/);
  const migration = await readFile('Shop/Features/Store/Database/Migrations/143-split-variant-definition-pages.sql', 'utf8');
  assert.match(migration, /warehouse\.variant-names/);
  assert.match(migration, /@VariantRoles/);
});
