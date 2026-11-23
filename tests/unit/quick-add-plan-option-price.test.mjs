import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('quick add records the selected plan and server quote without legacy price options', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  let quoteRequest;
  await page.route('https://example.test/**', route => {
    if (route.request().url().endsWith('/api/store/sales-plans/quote')) {
      quoteRequest = route.request().postDataJSON();
      return route.fulfill({json: {productId: 1, salesPlanId: 'special', unitAmount: 2000000, currency: 'IRT', accessoryAmounts: {}}});
    }
    return route.fulfill({contentType: 'text/html', body: ''});
  });
  await page.goto('https://example.test/');
  await page.setContent(`<script id="storeSalesPlans" type="application/json">${JSON.stringify({plans: [{id: 'regular', isDefault: true}], priceOptions: {1: [{key: 'color', label: 'Color', values: [{id: 7, key: 'red', label: 'Red', priceDelta: 100000, priceBySalesPlan: {regular: 100000, special: -250000}}]}]}})}</script><button class="add-cart" data-id="1" data-name="Product" data-sales-plan-id="special">Add</button>`);
  await page.evaluate(() => {
    window.HNProducts = [{id: 1, name: 'Product', accessories: []}];
    window.HNStorage = {create: () => ({get: () => '1'})};
    window.HNCartModel = {create: () => ({read: () => [], add: product => { window.savedProduct = product; return [{id: product.id, quantity: 1}]; }})};
  });
  await page.addScriptTag({path: 'Shop/wwwroot/shop/js/core/ui-core.js'});
  await page.locator('.add-cart').click();
  await page.waitForFunction(() => !!window.savedProduct);
  assert.equal(quoteRequest.salesPlanId, 'special');
  assert.equal(quoteRequest.priceOptions, undefined);
  const product = await page.evaluate(() => window.savedProduct);
  assert.equal(product.salesPlanSnapshot.unitAmount, 2000000);
  assert.equal(product.options?.priceOptions, undefined);
});
