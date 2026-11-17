import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('option prices are edited independently per plan and permit negative amounts', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  const writes = [];
  await page.route('https://example.test/**', route => {
    const request = route.request(), path = new URL(request.url()).pathname;
    if (request.method() === 'PUT') { writes.push({path, body: request.postDataJSON(), token: request.headers().requestverificationtoken}); return route.fulfill({json: {priceVersionId: 4}}); }
    if (path.endsWith('/product-offers')) return route.fulfill({json: {items: [], total: 0, page: 1}});
    if (path.endsWith('/option-prices')) return route.fulfill({json: {product: {id: 1, name: 'Product'}, plans: [{id: 10, storeSalesPlanKey: 'regular', displayName: 'Regular', isRegular: true}, {id: 20, storeSalesPlanKey: 'special', displayName: 'Special'}], options: [{id: 7, optionLabel: 'Color', valueLabel: 'Red'}], prices: [{idOptionValue: 7, idSalesPlan: 10, priceAmount: 100}, {idOptionValue: 7, idSalesPlan: 20, priceAmount: -50}]}});
    return route.fulfill({contentType: 'text/html', body: ''});
  });
  await page.goto('https://example.test/');
  await page.setContent(`<div data-store-variant-prices data-api="/api/prices" data-can-edit="true"><input name="__RequestVerificationToken" value="token"><form data-offer-filter><select name="productIds" multiple><option value="1" selected>Product</option></select></form><button data-option-prices-selected>Options</button><button data-offer-refresh>Refresh</button><span data-offer-count></span><span data-offer-status></span><table><tbody data-offer-rows></tbody></table></div><div id="option-prices-modal"><p data-option-prices-name></p><select data-option-prices-plan></select><span data-option-prices-status></span><table><tbody data-option-prices-rows></tbody></table></div>`);
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-ui.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-variant-prices.js'});
  await page.evaluate(() => { window.confirm = () => true; });
  await page.evaluate(() => window.AdminVariantPrices.initialize(document));
  await page.locator('[data-option-prices-selected]').click();
  await page.waitForSelector('[data-option-prices-rows] input');
  const plan = page.locator('[data-option-prices-plan]'), row = page.locator('[data-option-prices-rows] tr');
  assert.equal(await row.locator('input').inputValue(), '100');
  await plan.selectOption('special');
  assert.equal(await row.locator('select').inputValue(), '-1');
  assert.equal(await row.locator('input').inputValue(), '50');
  await row.locator('input').fill('75');
  await Promise.all([page.waitForResponse(response => response.request().method() === 'PUT'), row.locator('button').click()]);
  assert.deepEqual(writes, [{path: '/api/prices/products/1/options/7/plans/special', body: {priceAmount: -75}, token: 'token'}]);
  await plan.selectOption('regular');
  assert.equal(await row.locator('input').inputValue(), '100');
  await plan.selectOption('special');
  assert.equal(await row.locator('input').inputValue(), '75');
});
