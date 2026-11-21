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
  await page.setContent('<div class="modal" id="admin-record-modal"><div class="modal-dialog modal-lg"><div class="modal-content"><h2 id="admin-record-title"></h2><div class="modal-body" data-admin-record-body></div></div></div></div>');
  await page.evaluate(() => { window.bootstrap = {Modal: {getOrCreateInstance: () => ({show() {}})}}; });
  await page.addStyleTag({content: 'body { margin: 0; } .modal { position: fixed; inset: 0; }'});
  await page.addStyleTag({path: 'Admin/wwwroot/css/admin-ui.css'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-ui.js'});
  await page.evaluate(() => window.AdminUI.showRecord('کالا', {
    name: 'کالای نمونه', englishName: 'Sample', variants: [{name: 'آبی', price: 10}, {name: 'قرمز', price: 20}],
    media: {mainPath: 'https://example.com/image.jpg', cardPath: 'https://example.com/card.jpg', thumbnailPath: 'https://example.com/thumb.jpg', imageList: ['https://example.com/extra.jpg'], videoList: ['https://example.com/video.mp4']}
  }, {layout: 'product', groups: [{title: 'مشخصات', keys: ['name', 'englishName']}, {title: 'تنوع', keys: ['variants']}, {title: 'رسانه', keys: ['media']}]}));
  assert.equal(await page.locator('.admin-record-section > .admin-record-table-scroll > table').count(), 3);
  const details = page.locator('.admin-record-section').first();
  assert.equal(await details.locator('> .admin-record-table-scroll > table > thead th[scope="col"]').count(), 2);
  assert.deepEqual(await details.locator('> .admin-record-table-scroll > table > tbody > tr > td').allTextContents(), ['کالای نمونه', 'Sample']);
  assert.equal(await page.locator('.admin-record-table-nested tbody tr').count(), 2);
  assert.equal(await page.locator('.admin-record-media-gallery img').count(), 4);
  assert.equal(await page.locator('.admin-record-media-gallery video[controls]').count(), 1);
  for (const width of [375, 1440]) {
    await page.setViewportSize({width, height: 900});
    const positions = await page.locator('.admin-record-media-gallery figure').evaluateAll(cards => cards.map(card => ({x: card.getBoundingClientRect().x, y: card.getBoundingClientRect().y})));
    assert.equal(new Set(positions.slice(0, 3).map(position => position.y)).size, 1);
    assert.equal(new Set(positions.slice(0, 3).map(position => position.x)).size, 3);
    assert.ok(positions[3].y > positions[0].y);
  }
  await page.setViewportSize({width: 375, height: 812});
  assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
  assert.ok((await page.locator('.modal-dialog').boundingBox()).width <= 375);
  await page.setViewportSize({width: 1440, height: 900});
  assert.ok((await page.locator('.modal-dialog').boundingBox()).width >= 1400);
  const headers = await details.locator('> .admin-record-table-scroll > table > thead th').evaluateAll(cells => cells.map(cell => ({x: cell.getBoundingClientRect().x, y: cell.getBoundingClientRect().y})));
  assert.equal(headers[0].y, headers[1].y);
  assert.notEqual(headers[0].x, headers[1].x);
  await page.evaluate(() => window.AdminUI.showRecord('سفارش', {name: 'سفارش نمونه'}));
  assert.equal(await page.locator('.admin-record-section > dl').count(), 1);
  assert.equal(await page.locator('#admin-record-modal').evaluate(node => node.classList.contains('admin-record-product')), false);
});
