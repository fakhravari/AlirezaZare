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

test('bulk save validates drafts and retains failed rows for retry', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  const puts = [];
  let failSecond = true;
  await page.route('https://example.test/**', async route => {
    const req = route.request();
    if (req.url().includes('/api/prices')) {
      if (req.method() === 'PUT') {
        puts.push({url: req.url(), body: req.postDataJSON()});
        if (req.url().includes('/2/') && failSecond) return route.fulfill({status: 400, json: {detail: 'خطای آزمایشی'}});
        return route.fulfill({json: {saved: true}});
      }
      return route.fulfill({json: {items: [1, 2].map(id => ({variantId: id, salesPlanId: 'regular', productName: `کالا ${id}`, variantName: 'آبی', salesPlanName: 'عادی', priceAmount: 1000, discountPercent: 0, isEnabled: true})), total: 2}});
    }
    return route.fulfill({contentType: 'text/html', body: ''});
  });
  await page.goto('https://example.test/');
  await page.setContent('<div data-store-variant-prices data-api="/api/prices" data-can-edit="true"><form data-price-filter></form><div data-price-status></div><span data-price-count></span><table><tbody data-price-rows></tbody></table><button data-price-save-all disabled>ذخیره</button><span data-price-save-all-status></span></div>');
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-ui.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-variant-prices.js'});
  await page.evaluate(() => window.AdminVariantPrices.initialize(document));
  const amounts = page.locator('[data-price-rows] input[data-admin-price]');
  await amounts.first().waitFor();
  await amounts.nth(0).fill('2500');
  await amounts.nth(1).fill('');
  const saveAll = page.locator('[data-price-save-all]');
  await saveAll.click();
  assert.equal(puts.length, 0);
  await amounts.nth(1).fill('3500');
  await saveAll.click();
  await page.waitForFunction(() => document.querySelector('[data-price-save-all-status]').textContent.includes('ردیف ذخیره نشد'));
  assert.deepEqual(puts.map(x => x.body.priceAmount), [2500, 3500]);
  assert.equal(await amounts.nth(1).inputValue(), '3,500');
  assert.equal(await saveAll.isEnabled(), true);
  failSecond = false;
  await saveAll.click();
  await page.waitForFunction(() => document.querySelector('[data-price-save-all-status]').textContent === '۱ ردیف ذخیره شد.');
  assert.equal(await saveAll.isDisabled(), true);
  assert.equal(puts.length, 3);
  assert.match(puts[2].url, /\/2\/regular$/);
});
