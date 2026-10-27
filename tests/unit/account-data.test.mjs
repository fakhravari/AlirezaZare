import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

const source = readFileSync(new URL('../../Shop/wwwroot/shop/js/store/account-data.js', import.meta.url), 'utf8');
function read(data = {}) {
  const context = { window: {}, document: { getElementById: id => id in data ? { textContent: data[id] } : null } };
  vm.runInNewContext(source, context);
  return context.window.HNAccountData;
}
test('account bootstrap handles absent, malformed and future data without inventing offers', () => {
  for (const commerceData of [null, '{', '{"schemaVersion":2,"couponOffers":{"BAD":99}}']) {
    const result = read(commerceData === null ? {} : { commerceData });
    assert.equal(Object.keys(result.couponOffers).length, 0);
    assert.equal(result.coupons.length, 0);
  }
});
test('shared coupon projection preserves data and cannot be mutated by renderers', () => {
  const result = read({
    commerceData: JSON.stringify({ schemaVersion: 1, currency: 'IRT', couponOffers: { DEMO10: 10, BAD: 101 } }),
    accountData: JSON.stringify({ schemaVersion: 1, coupons: [{ code: 'DEMO10', uses: [{ invoice: 'A' }] }] })
  });
  assert.equal(result.couponOffers.DEMO10, 10);
  assert.equal(result.couponOffers.BAD, undefined);
  assert.equal(result.coupons[0].uses[0].invoice, 'A');
  assert.ok(Object.isFrozen(result.couponOffers));
  assert.ok(Object.isFrozen(result.coupons[0].uses[0]));
});
