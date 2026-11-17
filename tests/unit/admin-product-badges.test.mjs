import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('badge management searches products and assigns or clears their badge', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  const requests = [];
  await page.route('https://example.test/**', route => {
    const req = route.request(), url = new URL(req.url());
    requests.push({path: url.pathname, query: url.searchParams.get('q'), method: req.method(), body: req.postDataJSON()});
    if (url.pathname.endsWith('/products/7')) return route.fulfill({json: {saved: true}});
    if (url.pathname.endsWith('/products')) return route.fulfill({json: {items: [{id: 7, code: '123', name: 'کالا', idProductBadge: 1}], total: 1, page: 1, pageSize: 30}});
    if (url.pathname.endsWith('/product-badges')) return route.fulfill({json: [{id: 1, title: 'ویژه', productCount: 1}]});
    return route.fulfill({contentType: 'text/html', body: ''});
  });
  await page.goto('https://example.test/');
  await page.setContent(`<div data-product-badges><input name="__RequestVerificationToken" value="token"><form data-badge-form><input name="id"><input name="title" required><button>save</button></form><button data-badge-reset></button><table><tbody data-badge-rows></tbody></table><form data-badge-search><input name="q"><button>search</button></form><p data-badge-status></p><table><tbody data-badge-products></tbody></table><button data-badge-prev></button><span data-badge-page></span><button data-badge-next></button></div>`);
  await page.evaluate(() => { window.AdminUI = {getJson: async (url, options) => (await fetch(url, options)).json(), notify() {}}; });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-product-badges.js'});
  await page.waitForSelector('[data-badge-products] select');
  assert.equal(await page.locator('[data-badge-rows] button').last().isDisabled(), true);
  await page.locator('[data-badge-search] input').fill('123');
  await Promise.all([page.waitForResponse(response => new URL(response.url()).searchParams.get('q') === '123'), page.locator('[data-badge-search] button').click()]);
  await page.waitForFunction(() => document.querySelector('[data-badge-products] select')?.value === '1');
  await page.locator('[data-badge-products] select').selectOption('');
  await Promise.all([page.waitForResponse(response => response.url().endsWith('/products/7')), page.locator('[data-badge-products] button').click()]);
  assert.ok(requests.some(r => r.path.endsWith('/products') && r.query === '123'));
  assert.deepEqual(requests.find(r => r.path.endsWith('/products/7')).body, {badgeId: null});
});
