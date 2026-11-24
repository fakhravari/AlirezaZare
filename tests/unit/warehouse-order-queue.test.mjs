import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
const read = path => readFile(new URL('../../' + path, import.meta.url), 'utf8');

test('warehouse queue requires verified payment for every order', async () => {
    const api = await read('Admin/Areas/Store/Controllers/Api/StorePaidOrdersDispatchApiController.cs');
    const client = await read('Admin/wwwroot/js/admin-paid-orders-dispatch.js');
    const seed = await read('Shop/Features/Store/Database/Seeds/warehouse-order-queue-demo.sql');
    for (const expected of ['payment.StatusReference.Code == "verified"', 'payment.Amount == order.TotalAmount && payment.PaidAtUtc != null', '[Authorize(Roles = "system.full-access")]']) assert.ok(api.includes(expected), expected);
    assert.match(api, /!payments\.Any\(payment => payment\.IdOrder == order\.Id && payment\.Amount == order\.TotalAmount\)/);
    assert.doesNotMatch(api, /demo-order-|IsSample/);
    assert.ok(client.includes('checkbox.disabled = !canDispatch'));
    assert.ok(!seed.includes('INSERT [Store].[PaymentAttempt]'));
});

test('warehouse queue limits access to the page permission and configured warehouse', async () => {
    const api = await read('Admin/Areas/Store/Controllers/Api/StorePaidOrdersDispatchApiController.cs');
    const filter = await read('Admin/Security/AdminAuthentication.cs');
    assert.ok(filter.includes('feature = "order-confirmation"'));
    assert.ok(api.includes('if (!warehouseQueue) return Forbid()'));
    assert.ok(api.includes('User.HasClaim("admin_warehouse", warehouseId.Value'));
});
