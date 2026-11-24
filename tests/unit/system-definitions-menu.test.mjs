import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';

test('warehouse definition pages are grouped under system management with their existing permissions', async () => {
  const sidebar = await readFile('Admin/Views/Shared/_AdminSidebar.cshtml', 'utf8');
  const systemStart = sidebar.indexOf('<summary class="menu-header">مدیریت سامانه</summary>');
  const systemEnd = sidebar.indexOf('</details>', systemStart);
  const system = sidebar.slice(systemStart, systemEnd);
  assert.ok(system.includes('تعاریف پایهٔ انبار'));
  for (const feature of ['companies', 'references', 'units', 'product-units', 'variants', 'stock-owners', 'sales-plan-mapping', 'voucher-types']) {
    assert.ok(system.includes(`asp-route-feature="${feature}"`), `missing ${feature} under system management`);
    assert.ok(system.includes(`Warehouse:Features:Index:${feature}`), `missing existing permission for ${feature}`);
  }
  assert.ok(system.includes('Warehouse:Warehouses:Index'));
  assert.ok(system.indexOf('asp-route-feature="companies"') < system.indexOf('asp-controller="Warehouses"'));
  assert.ok(!sidebar.includes('id="warehouse-settings"'));
  assert.ok(!sidebar.includes('id="warehouse-structure"'));
});

test('permission migration preserves existing effective route grants while moving definition permissions', async () => {
  const migration = await readFile('Shop/Features/Store/Database/Migrations/127-move-warehouse-definitions-to-system.sql', 'utf8');
  assert.ok(migration.includes("N'system.definitions.group'"));
  assert.ok(migration.includes('JOIN @Targets target ON target.[Id] = beforeMove.[IdPermission]'));
  assert.ok(migration.includes('Moving warehouse definitions would change effective role permissions.'));
  assert.ok(migration.includes("N'warehouse.structure.group' AND [IsActive] = 1"));
});
