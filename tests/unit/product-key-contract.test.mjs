import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';

test('product creation and editing use a unique product key, with ID as the creation fallback', async () => {
  const api = await readFile('Data/Infrastructure/Store/StoreCatalogAdminData.cs', 'utf8');
  const markup = await readFile('Admin/Areas/Store/Views/Catalog/Index.cshtml', 'utf8');
  const model = await readFile('Data/Domain/Warehouse/Entities/WarehouseProduct.cs', 'utf8');
  assert.match(api, /if \(key\.Length == 0\) key = id\.ToString/);
  assert.match(api, /x\.Id != id && x\.ProductKey == key/);
  assert.match(api, /x => x\.ProductKey == key/);
  assert.match(api, /key\.Length is < 1 or > 100/);
  assert.match(markup, /id="new-product-key"[^>]*name="key"(?![^>]*required)/);
  assert.match(markup, /id="product-edit-key"[^>]*name="key"[^>]*required/);
  assert.doesNotMatch(model, /public string Code/);
});

test('product key migration archives legacy codes and rejects invalid keys', async () => {
  const sql = await readFile('Shop/Features/Store/Database/Migrations/105-product-key-code.sql', 'utf8');
  assert.match(sql, /INSERT Warehouse\.ProductCodeArchive105[\s\S]+SELECT Id, Code, ProductKey FROM Warehouse\.Product/);
  assert.match(sql, /GROUP BY ProductKey HAVING COUNT\(\*\) > 1/);
  assert.match(sql, /NULLIF\(LTRIM\(RTRIM\(ProductKey\)\), N''\) IS NULL/);
  assert.match(sql, /CREATE UNIQUE INDEX UX_Product_ProductKey_105/);
  assert.match(sql, /ROLLBACK TRANSACTION/);
});
