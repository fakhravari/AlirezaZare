import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('price inputs format digits, validate and send numeric variant prices', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.route('https://example.test/**', async route => {
    const request = route.request();
    if (request.url().includes('/api/prices')) {
      if (request.method() === 'PUT') {
        await page.evaluate(body => window.savedPrices.push(JSON.parse(body)), request.postData());
        return route.fulfill({json: {message: 'ok'}});
      }
      return route.fulfill({json: {items: [{variantId: 1, salesPlanId: 'regular', productName: 'کالا', variantName: 'آبی', salesPlanName: 'عادی', priceAmount: 1234567, discountPercent: 5, isEnabled: true}], total: 1, page: 1}});
    }
    return route.fulfill({contentType: 'text/html', body: ''});
  });
  await page.goto('https://example.test/');
  await page.setContent('<form><label for="p">قیمت</label><input id="p" data-admin-price type="text" max="9007199254740991" required></form><div data-store-variant-prices data-api="/api/prices" data-can-edit="true"><form data-price-filter><input></form><div data-price-status></div><span data-price-count></span><table><tbody data-price-rows></tbody></table></div>');
  await page.evaluate(() => { window.savedPrices = []; });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-ui.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-variant-prices.js'});
  const field = page.locator('#p');
  await field.fill('۱۲۳۴۵۶۷');
  assert.equal(await field.inputValue(), '1,234,567');
  assert.equal(await field.evaluate(el => window.AdminUI.priceValue(el)), 1234567);
  await field.fill('١٢٣٤٥٦');
  assert.equal(await field.inputValue(), '123,456');
  await field.fill('-10');
  assert.equal(await field.evaluate(el => el.validity.valid), false);
  await field.fill('12.5');
  assert.equal(await field.evaluate(el => el.validity.valid), false);
  await field.fill('9007199254740992');
  assert.equal(await field.evaluate(el => el.validity.valid), false);
  await field.fill('');
  assert.equal(await field.evaluate(el => el.validity.valueMissing), true);
  await page.evaluate(() => window.AdminVariantPrices.initialize(document));
  const amount = page.locator('[data-price-rows] input[data-admin-price]');
  await amount.waitFor();
  assert.equal(await amount.inputValue(), '1,234,567');
  await amount.fill('۲۵۰۰۰۰۰');
  const save = page.locator('[data-price-rows] button').first();
  await save.evaluate(button => button.removeAttribute('data-admin-confirm'));
  await save.click();
  await page.waitForFunction(() => window.savedPrices.length === 1);
  assert.equal((await page.evaluate(() => window.savedPrices[0])).priceAmount, 2500000);
});
