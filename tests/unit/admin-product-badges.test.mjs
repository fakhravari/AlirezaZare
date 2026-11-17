import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {readFile} from 'node:fs/promises';
import {chromium} from '@playwright/test';

test('badge management searches products and assigns or clears their badge', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  const requests = [];
  let assignedBadge = 1;
  await page.route('https://example.test/**', route => {
    const req = route.request(), url = new URL(req.url());
    requests.push({path: url.pathname, query: url.searchParams.get('q'), badgeId: url.searchParams.get('badgeId'), withoutBadge: url.searchParams.get('withoutBadge'), page: url.searchParams.get('page'), method: req.method(), body: req.postDataJSON()});
    if (url.pathname.endsWith('/products/7')) { assignedBadge = req.postDataJSON().badgeId; return route.fulfill({json: {saved: true}}); }
    if (url.pathname.endsWith('/products')) return route.fulfill({json: {items: [{id: 7, code: '123', name: 'کالا', idProductBadge: assignedBadge}], total: 1, page: 1, pageSize: 30}});
    if (url.pathname.endsWith('/product-badges')) return route.fulfill({json: [{id: 1, title: 'ویژه', backgroundColor: '#FFDD00', productCount: 1}]});
    if (url.pathname.endsWith('/product-badges/1')) return route.fulfill({json: {id: 1, title: 'ویژه', backgroundColor: '#224466'}});
    return route.fulfill({contentType: 'text/html', body: ''});
  });
  await page.goto('https://example.test/');
  await page.setContent(`<div data-product-badges><input name="__RequestVerificationToken" value="token"><form data-badge-form><input name="id"><input name="title" required><input name="backgroundColor" type="color" value="#102a43"><button>save</button></form><button data-badge-reset></button><table><tbody data-badge-rows></tbody></table><form data-badge-search><input name="q"><select name="badgeFilter"></select><button>search</button></form><p data-badge-status></p><table><tbody data-badge-products></tbody></table><button data-badge-prev></button><span data-badge-page></span><button data-badge-next></button></div>`);
  await page.evaluate(() => { window.AdminUI = {getJson: async (url, options) => (await fetch(url, options)).json(), notify() {}}; });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-product-badges.js'});
  await page.evaluate(() => window.AdminProductBadges.initialize(document));
  await page.waitForSelector('[data-badge-products] select');
  assert.equal(await page.locator('[data-badge-rows] .badge').evaluate(el => el.style.color), 'rgb(0, 0, 0)');
  assert.equal(await page.locator('[data-badge-rows] .badge').evaluate(el => el.style.border), '1px solid rgb(0, 0, 0)');
  await page.locator('[data-badge-rows] button').first().click();
  assert.equal(await page.locator('[name=backgroundColor]').inputValue(), '#ffdd00');
  await page.locator('[name=backgroundColor]').fill('#224466');
  await Promise.all([page.waitForResponse(response => response.url().endsWith('/product-badges/1')), page.locator('[data-badge-form] button').click()]);
  assert.deepEqual(requests.find(r => r.path.endsWith('/product-badges/1')).body, {title: 'ویژه', backgroundColor: '#224466'});
  assert.equal(await page.locator('[data-badge-rows] button').last().isDisabled(), true);
  await page.locator('[data-badge-search] input').fill('123');
  await page.locator('[data-badge-search] select').selectOption('1');
  await Promise.all([page.waitForResponse(response => new URL(response.url()).searchParams.get('q') === '123'), page.locator('[data-badge-search] button').click()]);
  await page.waitForFunction(() => document.querySelector('[data-badge-products] select')?.value === '1');
  assert.equal(await page.locator('[data-badge-products] select option', {hasText: 'بدون نشان'}).count(), 0);
  page.once('dialog', dialog => dialog.accept());
  await Promise.all([page.waitForResponse(response => response.url().endsWith('/products/7')), page.getByRole('button', {name: 'حذف نشان'}).click()]);
  assert.ok(requests.some(r => r.path.endsWith('/products') && r.query === '123' && r.badgeId === '1' && r.page === '1'));
  assert.deepEqual(requests.find(r => r.path.endsWith('/products/7')).body, {badgeId: null});
  await page.waitForFunction(() => document.querySelector('[data-badge-products] select')?.value === '');
  assert.equal(await page.locator('[data-badge-products] button').count(), 1);
  assert.equal(await page.locator('[data-badge-products] button').isDisabled(), true);
  await page.locator('[data-badge-products] select').selectOption('1');
  await Promise.all([page.waitForResponse(response => response.url().endsWith('/products/7')), page.locator('[data-badge-products] button').click()]);
  assert.deepEqual(requests.filter(r => r.path.endsWith('/products/7')).at(-1).body, {badgeId: 1});
  await page.locator('[data-badge-search] select').selectOption('none');
  await Promise.all([page.waitForResponse(response => new URL(response.url()).searchParams.get('withoutBadge') === 'true'), page.locator('[data-badge-search] button').click()]);
  assert.ok(requests.some(r => r.path.endsWith('/products') && r.withoutBadge === 'true' && r.badgeId === null));
});

test('badge definition and assignment tabs switch independently', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  let markup = await readFile('Admin/Areas/Store/Views/Catalog/Badges.cshtml', 'utf8');
  markup = markup.replace(/\s*@await Html\.PartialAsync\([^\n]+\)/, '').replace('@Html.AntiForgeryToken()', '<input name="__RequestVerificationToken" value="token">');
  await page.setContent(markup);
  await page.addScriptTag({path: 'Admin/wwwroot/lib/bootstrap/bootstrap.bundle.min.js'});
  assert.equal(await page.locator('#badge-definitions').evaluate(el => el.classList.contains('show')), true);
  await page.locator('#badge-assignments-tab').click();
  await page.waitForFunction(() => document.querySelector('#badge-assignments').classList.contains('show'));
  assert.equal(await page.locator('#badge-assignments-tab').getAttribute('aria-selected'), 'true');
  assert.equal(await page.locator('#badge-definitions-tab').getAttribute('aria-selected'), 'false');
  await page.locator('#badge-definitions-tab').click();
  await page.waitForFunction(() => document.querySelector('#badge-definitions').classList.contains('show'));
});

test('badge data loads when navigating from the admin sidebar and on return', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  const badgeContent = `<div data-product-badges><input name="__RequestVerificationToken" value="token"><form data-badge-form><input name="id"><input name="title"></form><button data-badge-reset></button><table><tbody data-badge-rows></tbody></table><form data-badge-search><input name="q"><select name="badgeFilter"></select></form><p data-badge-status></p><table><tbody data-badge-products></tbody></table><button data-badge-prev></button><span data-badge-page></span><button data-badge-next></button></div>`;
  await page.route('https://example.test/**', route => {
    const url = new URL(route.request().url());
    if (url.pathname.endsWith('/api/admin/store/product-badges/products')) return route.fulfill({json: {items: [{id: 7, code: '123', name: 'کالا', idProductBadge: 1}], total: 1, pageSize: 30}});
    if (url.pathname.endsWith('/api/admin/store/product-badges')) return route.fulfill({json: [{id: 1, title: 'ویژه', productCount: 1}]});
    return route.fulfill({contentType: 'text/html', body: `<html><title>پنل</title><div id="content">${url.pathname === '/badges' ? badgeContent : '<p>خانه</p>'}</div></html>`});
  });
  await page.goto('https://example.test/home');
  await page.setContent('<aside id="sidebar"><a data-admin-nav href="/badges">نشان‌ها</a><a data-admin-nav href="/home">خانه</a></aside><main id="content">خانه</main>');
  await page.evaluate(() => { window.AdminUI = {getJson: async (url, options) => (await fetch(url, options)).json(), notify() {}}; });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-product-badges.js'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-shell.js'});
  for (let i = 0; i < 2; i++) {
    await page.getByRole('link', {name: 'نشان‌ها'}).click();
    await page.waitForSelector('[data-badge-products] select');
    assert.equal(await page.locator('[data-badge-rows] tr').count(), 1);
    assert.equal(await page.locator('[data-badge-products] tr').count(), 1);
    await page.getByRole('link', {name: 'خانه'}).click();
    await page.waitForSelector('#content p');
  }
});
