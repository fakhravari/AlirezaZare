import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
const read = path => readFile(new URL('../../' + path, import.meta.url), 'utf8');

test('warehouse queue separates samples from verified payments and blocks sample dispatch', async () => {
    const api = await read('Admin/Areas/Store/Controllers/Api/StorePaidOrdersDispatchApiController.cs');
    const client = await read('Admin/wwwroot/js/admin-paid-orders-dispatch.js');
    const seed = await readFile('C:/AlirezaZare-build-artifacts/shop-features/Features/Store/Database/Seeds/warehouse-order-queue-demo.sql', 'utf8');
    for (const expected of ['warehouseQueue && order.PaymentMethod == "demo"', 'payment.Status == "verified"', 'payment.Amount == order.TotalAmount && payment.PaidAtUtc != null', '[Authorize(Roles = "system.full-access")]', 'order.ExternalOrderKey?.StartsWith("demo-order-") == true']) assert.ok(api.includes(expected), expected);
    assert.ok(client.includes('checkbox.disabled = item.isSample || !canDispatch'));
    assert.ok(!seed.includes('INSERT [Store].[PaymentAttempt]'));
});

test('warehouse queue limits access to the page permission and configured warehouse', async () => {
    const api = await read('Admin/Areas/Store/Controllers/Api/StorePaidOrdersDispatchApiController.cs');
    const filter = await read('Admin/Security/AdminAuthentication.cs');
    assert.ok(filter.includes('feature = "order-confirmation"'));
    assert.ok(api.includes('if (!warehouseQueue) return Forbid()'));
    assert.ok(api.includes('User.HasClaim("admin_warehouse", warehouseId.Value'));
});
