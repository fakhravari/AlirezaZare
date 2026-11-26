import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import test from 'node:test';

const read = path => readFile(new URL(`../../${path}`, import.meta.url), 'utf8');

test('Store.Order derives payment method and item count without redundant columns', async () => {
  const entity = await read('Data/Domain/Account/Entities/AccountProfile.cs');
  const order = entity.slice(entity.indexOf('public sealed class StoreCustomerOrderReference'), entity.indexOf('public sealed class StoreCustomerOrderItem'));
  assert.doesNotMatch(order, /\b(?:PaymentMethod|ItemCount)\b/);

  const migration = await read('Shop/Features/Store/Database/Migrations/175-remove-order-payment-method-item-count.sql');
  assert.match(migration, /DROP COLUMN/);
  assert.match(migration, /N'PaymentMethod'/);
  assert.match(migration, /N'ItemCount'/);

  const checkout = await read('Data/Infrastructure/Account/AccountStateQueryService.cs');
  const admin = await read('Data/Infrastructure/Store/StoreOrdersAdminData.cs');
  const dispatch = await read('Data/Infrastructure/Warehouse/StorePaidOrderDispatchData.cs');
  assert.match(checkout, /PaymentMethod = x\.Bank\.Code/);
  assert.match(checkout, /ItemCount = x\.Items\.Sum\(item => item\.Quantity\)/);
  assert.match(admin, /PaymentMethod = x\.order\.Bank\.Code/);
  assert.match(admin, /item\.IdOrder == x\.order\.Id[\s\S]*?Sum\(item => \(int\?\)item\.Quantity\)/);
  assert.match(dispatch, /item\.IdOrder == x\.order\.Id[\s\S]*?Sum\(item => \(int\?\)item\.Quantity\)/);
});
