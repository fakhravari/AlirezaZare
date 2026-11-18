import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';

test('warehouse stock belongs to reports and retains independent permission and active state', async () => {
  const sidebar = await readFile('Admin/Views/Shared/_AdminSidebar.cshtml', 'utf8');
  const structure = sidebar.slice(sidebar.indexOf('<div id="warehouse-structure"'), sidebar.indexOf('<div class="menu-item has-sub @(reportsOpen'));
  const reports = sidebar.slice(sidebar.indexOf('<div id="warehouse-reports"'), sidebar.indexOf('<div class="menu-item has-sub @(operationsOpen'));
  assert.match(sidebar, /menu-text">تعریف شرکت و انبار</);
  assert.doesNotMatch(structure, /asp-controller="Stock"/);
  assert.match(reports, /Can\("Warehouse:Stock:Index"\)[\s\S]*asp-controller="Stock"/);
  assert.match(sidebar, /bool canWarehouseReports = Can\("Warehouse:Features:Index:movements"\) \|\| Can\("Warehouse:Stock:Index"\)/);
  assert.match(sidebar, /bool reportsOpen = inWarehouse && \(IsFeature\("movements"\) \|\| string\.Equals\(controller, "Stock"/);
  assert.doesNotMatch(sidebar.match(/bool warehouseStructureOpen[^\n]+/)[0], /"Stock"/);
});
