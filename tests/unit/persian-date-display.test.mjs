import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('Shop and Admin display Persian dates consistently in Tehran time', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage({timezoneId: 'America/New_York'});
  await page.route('https://example.test/', route => route.fulfill({contentType: 'text/html', body: ''}));
  await page.goto('https://example.test/');
  await page.setContent('<div id="admin-record-modal"><h2 id="admin-record-title"></h2><div data-admin-record-body></div></div><time data-admin-date datetime="2024-03-20"></time>');
  await page.evaluate(() => { window.bootstrap = {Modal: {getOrCreateInstance: () => ({show() {}})}}; });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-ui.js'});
  await page.addScriptTag({path: 'Shop/wwwroot/shop/js/core/storage.js'});
  await page.addScriptTag({path: 'Shop/wwwroot/shop/js/store/cart.js'});
  await page.addScriptTag({path: 'Shop/wwwroot/shop/js/core/ui-core.js'});
  const actual = await page.evaluate(() => {
    const values = ['2024-03-20T21:00:00Z', '2024-03-20T21:00:00', '2024-03-21T00:30:00+03:30', '2024-03-20', 'invalid'];
    window.AdminUI.showRecord('کالا', {createdAtUtc: values[0], variants: [{updatedAtUtc: values[0]}]}, {layout: 'product'});
    return {admin: values.map(window.AdminUI.formatDateTime), shop: values.map(window.HNUI.formatDateTime), record: document.querySelector('[data-admin-record-body]').textContent};
  });
  assert.deepEqual(actual.admin.slice(0, 4), actual.shop.slice(0, 4));
  assert.equal(actual.admin[0], actual.admin[1]);
  assert.equal(actual.admin[0], actual.admin[2]);
  assert.match(actual.admin[0], /۱۴۰۳\/۱\/۲/);
  assert.match(actual.admin[0], /۰:۳۰/);
  assert.match(actual.admin[3], /۱۴۰۳\/۱\/۱/);
  assert.doesNotMatch(actual.admin[3], /:/);
  assert.equal(actual.admin[4], '—');
  assert.equal(actual.shop[4], '');
  assert.equal(actual.record.split(actual.admin[0]).length - 1, 2);
  assert.match(await page.locator('time[data-admin-date]').textContent(), /۱۴۰۳\/۱\/۱/);
  await page.evaluate(() => { document.body.insertAdjacentHTML('beforeend', '<time data-admin-date datetime="2024-03-20T21:00:00Z"></time>'); });
  await page.waitForFunction(() => document.querySelectorAll('time[data-admin-date]')[1].textContent.includes('۱۴۰۳'));
  assert.match(await page.locator('time[data-admin-date]').nth(1).textContent(), /۱۴۰۳\/۱\/۲/);
});
