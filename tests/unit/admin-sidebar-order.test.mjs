import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

test('management menus progress from base definitions to operational pages', async () => {
  const sidebar = await readFile('Admin/Views/Shared/_AdminSidebar.cshtml', 'utf8');
  const ordered = markers => {
    const positions = markers.map(marker => sidebar.indexOf(marker));
    assert.ok(positions.every(position => position >= 0));
    assert.deepEqual(positions, [...positions].sort((a, b) => a - b));
  };

  ordered([
    'asp-route-feature="companies"',
    'asp-controller="Warehouses"',
    'asp-route-feature="units"',
    'asp-route-feature="references"',
    'asp-route-feature="document-types"',
    'asp-route-feature="stock-owners"',
    'asp-route-feature="sales-plan-mapping"',
    'asp-route-feature="variants"',
    'asp-route-feature="product-units"',
  ]);
  ordered([
    'asp-action="Categories"',
    'asp-action="Brands"',
    'asp-action="Attributes"',
    'asp-action="Badges"',
    'asp-action="Products"',
    'asp-action="PaymentInstitutions"',
  ]);
  ordered([
    'has-sub @(operationsOpen',
    'has-sub @(reportsOpen',
  ]);
});
