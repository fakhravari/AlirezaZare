import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('offer and history filters send product and date bounds and reject reversed ranges', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  const requests = [];
  await page.route('https://example.test/**', route => {
    const url = new URL(route.request().url());
    if (url.pathname.startsWith('/api/prices/')) {
      requests.push(url);
      return route.fulfill({json: url.pathname.endsWith('price-history')
        ? [{id: 3, idProduct: 1, productName: 'کالای تست', priceAmount: 12000, createdAtUtc: '2024-03-20T12:00:00Z'}]
        : {items: [], total: 0, page: 1}});
    }
    return route.fulfill({contentType: 'text/html', body: ''});
  });
  await page.goto('https://example.test/');
  await page.setContent(`<div data-store-variant-prices data-api="/api/prices" data-can-edit="true">
    <form data-offer-filter><input name="q"><input name="from" type="date"><input name="to" type="date"><button>اعمال</button></form>
    <button data-offer-refresh></button><span data-offer-count></span><div data-offer-status></div><table><tbody data-offer-rows></tbody></table>
    <form data-history-filter><input name="q"><input name="from" type="date"><input name="to" type="date"><button>اعمال</button></form>
    <button data-price-history-refresh></button><div data-price-history-status></div><table><tbody data-price-history-rows></tbody></table>
  </div>`);
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-ui.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-variant-prices.js'});
  await page.evaluate(() => window.AdminVariantPrices.initialize(document));
  await page.waitForFunction(() => document.querySelector('[data-price-history-rows]')?.textContent.includes('کالای تست'));
  for (const selector of ['[data-offer-filter]', '[data-history-filter]']) {
    const form = page.locator(selector);
    await form.locator('[name=q]').fill('کالا');
    await form.locator('[name=from]').fill('2024-03-20');
    await form.locator('[name=to]').fill('2024-03-21');
    await form.locator('button').click();
  }
  await page.waitForFunction(() => document.querySelector('[data-price-history-status]')?.textContent.includes('نسخه'));
  const history = requests.filter(url => url.pathname.endsWith('price-history')).at(-1);
  const offers = requests.filter(url => url.pathname.endsWith('product-offers')).at(-1);
  for (const url of [history, offers]) {
    assert.equal(url.searchParams.get('q'), 'کالا');
    assert.equal(url.searchParams.get('from'), '2024-03-20');
    assert.equal(url.searchParams.get('to'), '2024-03-21');
  }
  const count = requests.length;
  await page.locator('[data-history-filter] [name=from]').fill('2024-03-22');
  await page.locator('[data-history-filter] button').click();
  assert.match(await page.locator('[data-price-history-status]').textContent(), /تاریخ شروع/);
  assert.equal(requests.length, count);
});

test('plan and variant filters send product and dates to their endpoints', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  const requests = [];
  await page.route('https://example.test/**', route => {
    const url = new URL(route.request().url());
    if (url.pathname.startsWith('/api/prices')) {
      requests.push(url);
      return route.fulfill({json: url.pathname.endsWith('/plans') ? [] : {items: [], total: 0, page: 1}});
    }
    return route.fulfill({contentType: 'text/html', body: ''});
  });
  await page.goto('https://example.test/');
  await page.setContent(`<div data-store-variant-prices data-api="/api/prices" data-can-edit="true">
    <form data-plan-filter><input name="q"><input name="from" type="date"><input name="to" type="date"><button>اعمال</button></form>
    <form data-plan-create><button></button></form>
    <form data-plan-settings><select name="id"></select><input name="code"><input name="displayName"><input name="planType"><input name="startsAtUtc"><input name="endsAtUtc"><input name="label"><input name="showCountdown" type="checkbox"><button type="button" data-plan-details-save></button><button type="button" data-plan-delete></button><button type="button" data-plan-publish></button><button type="button" data-plan-unpublish></button><button type="submit"></button><span data-plan-status></span></form>
    <form data-price-filter><input name="q"><input name="from" type="date"><input name="to" type="date"><button>اعمال</button></form>
    <span data-price-count></span><div data-price-status></div><table><tbody data-price-rows></tbody></table>
  </div>`);
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-ui.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-variant-prices.js'});
  await page.evaluate(() => window.AdminVariantPrices.initialize(document));
  await page.waitForFunction(() => document.querySelector('[data-plan-status]').textContent.includes('یافت نشد'));
  for (const selector of ['[data-plan-filter]', '[data-price-filter]']) {
    const form = page.locator(selector);
    await form.locator('[name=q]').fill('123');
    await form.locator('[name=from]').fill('2024-03-20');
    await form.locator('[name=to]').fill('2024-03-21');
    await form.locator('button').click();
  }
  await page.waitForFunction(() => document.querySelector('[data-price-status]').textContent.includes('پیدا نشد'));
  for (const path of ['/api/prices/plans', '/api/prices']) {
    const url = requests.filter(item => item.pathname === path).at(-1);
    assert.equal(url.searchParams.get('q'), '123');
    assert.equal(url.searchParams.get('from'), '2024-03-20');
    assert.equal(url.searchParams.get('to'), '2024-03-21');
  }
});
