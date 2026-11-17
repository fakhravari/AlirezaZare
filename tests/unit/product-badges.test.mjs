import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import vm from 'node:vm';

test('product badges appear only in the regular sales plan', async () => {
  const js = await readFile('Shop/wwwroot/shop/js/store/product-card.js', 'utf8');
  const data = {plans: [{id: 'normal-plan', isDefault: true}, {id: 'special', isDefault: false}], offers: [], inventoryVariants: []};
  const document = {querySelector: () => ({textContent: JSON.stringify(data)})};
  const window = {HNUI: {escapeHtml: String, formatNumber: String}, HNProductUrl: () => '/product'};
  vm.runInNewContext(js, {window, document});
  const product = {id: 1, name: 'محصول', price: 1, badge: 'نشان نمونه', image: 'image'};
  const render = options => window.HNProductCard.render(product, options);
  assert.match(render({}), /نشان نمونه/);
  assert.match(render({salesPlanId: 'normal-plan', salesPlanName: 'عادی'}), /نشان نمونه/);
  assert.doesNotMatch(render({salesPlanId: 'special', salesPlanName: 'طرح ویژه'}), /نشان نمونه/);
  assert.match(render({salesPlanId: 'special', salesPlanName: 'طرح ویژه'}), /طرح ویژه/);
  assert.doesNotMatch(render({salesPlanId: 'unknown'}), /نشان نمونه/);
});

test('badge migration transfers old values before dropping the legacy column', async () => {
  const sql = await readFile('Shop/Features/Store/Database/Migrations/106-product-badges.sql', 'utf8');
  assert.match(sql, /INSERT Store\.ProductBadge[\s\S]+UPDATE p SET IdProductBadge[\s\S]+DROP COLUMN Badge/);
  assert.match(sql, /FOREIGN KEY \(IdProductBadge\) REFERENCES Store\.ProductBadge\(Id\)/);
  assert.match(sql, /ROLLBACK TRANSACTION/);
});
