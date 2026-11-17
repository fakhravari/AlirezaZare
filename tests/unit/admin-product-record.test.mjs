import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('product details use tables and retain media previews without changing other records', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage({viewport: {width: 375, height: 812}});
  await page.route('https://example.test/', route => route.fulfill({contentType: 'text/html', body: ''}));
  await page.route('https://example.com/**', route => route.fulfill({contentType: 'image/png', body: Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=', 'base64')}));
  await page.goto('https://example.test/');
  await page.setContent('<div id="admin-record-modal"><h2 id="admin-record-title"></h2><div data-admin-record-body></div></div>');
  await page.evaluate(() => { window.bootstrap = {Modal: {getOrCreateInstance: () => ({show() {}})}}; });
  await page.addStyleTag({path: 'Admin/wwwroot/css/admin-ui.css'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-ui.js'});
  await page.evaluate(() => window.AdminUI.showRecord('کالا', {
    name: 'کالای نمونه', variants: [{name: 'آبی', price: 10}, {name: 'قرمز', price: 20}],
    media: {mainPath: 'https://example.com/image.jpg', videoList: ['https://example.com/video.mp4']}
  }, {layout: 'product', groups: [{title: 'مشخصات', keys: ['name']}, {title: 'تنوع', keys: ['variants']}, {title: 'رسانه', keys: ['media']}]}));
  assert.equal(await page.locator('.admin-record-section > table').count(), 3);
  assert.equal(await page.locator('.admin-record-table-nested tbody tr').count(), 2);
  assert.equal(await page.locator('.admin-record-media-gallery img').count(), 1);
  assert.equal(await page.locator('.admin-record-media-gallery video[controls]').count(), 1);
  assert.equal(await page.locator('.admin-record-section > table th[scope="row"]').count(), 3);
  assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
  await page.evaluate(() => window.AdminUI.showRecord('سفارش', {name: 'سفارش نمونه'}));
  assert.equal(await page.locator('.admin-record-section > dl').count(), 1);
  assert.equal(await page.locator('#admin-record-modal').evaluate(node => node.classList.contains('admin-record-product')), false);
});
