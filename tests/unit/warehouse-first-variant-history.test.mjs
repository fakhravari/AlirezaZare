import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';

test('first active variant is protected from existing unvaried document, movement and balance records', async () => {
  const source = await readFile('Admin/Areas/Warehouse/Controllers/Api/WarehouseDefinitionsApiController.cs', 'utf8');
  const block = source.slice(source.indexOf('// Activating the first variant'), source.indexOf('if (!await ValidVariantAttributesAsync'));
  assert.ok(block.includes('input.IsActive && !entity.IsActive'));
  assert.ok(block.includes('x.IdProduct == productId && x.Id != (id ?? 0) && x.IsActive'));
  for (const evidence of ['db.VoucherLines', 'db.WarehouseStockMovements', 'db.StockBalances']) assert.ok(block.includes(evidence));
  assert.equal((block.match(/item.IdProductVariant == null/g) || []).length, 3);
  assert.ok(block.includes('hasUnvariedVouchers || hasUnvariedMovements || hasUnvariedBalance'));
  assert.ok(block.includes('return Conflict('));
});
