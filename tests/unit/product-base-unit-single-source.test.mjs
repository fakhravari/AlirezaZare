import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';

test('product unit link is the only persisted base-unit source', async () => {
  const [product, catalogApi, warehouseApi, migration] = await Promise.all([
    readFile('Data/Domain/Warehouse/Entities/WarehouseProduct.cs', 'utf8'),
    readFile('Admin/Areas/Store/Controllers/Api/StoreCatalogAdminApiController.cs', 'utf8'),
    readFile('Admin/Areas/Warehouse/Controllers/Api/WarehouseDefinitionsApiController.cs', 'utf8'),
    readFile('Shop/Features/Store/Database/Migrations/128-product-unit-single-base-source.sql', 'utf8')
  ]);

  assert.doesNotMatch(product, /IdBaseUnit/);
  assert.match(catalogApi, /x\.IdProduct == id && x\.IsActive && x\.IsBaseUnit/);
  assert.match(warehouseApi, /x\.IdProduct == input\.ProductId && x\.IsBaseUnit && x\.IsActive/);
  assert.match(migration, /SET \[IsBaseUnit\] = CASE WHEN link\.\[IdUnit\] = product\.\[IdBaseUnit\]/);
  assert.match(migration, /DROP COLUMN \[IdBaseUnit\]/);
  assert.match(migration, /UNIQUE INDEX \[UX_ProductUnit_OneActiveBasePerProduct\]/);
});
