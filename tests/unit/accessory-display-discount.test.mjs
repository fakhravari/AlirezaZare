import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {readFile} from 'node:fs/promises';
import vm from 'node:vm';
import {chromium} from '@playwright/test';

test('display percentage changes the badge without changing the amount', async () => {
  const source = await readFile('Shop/wwwroot/shop/js/store/commerce.js', 'utf8');
  const start = source.indexOf('            const accessoryPrice = (item) =>');
  const end = source.indexOf('            const accessoryMarkup =', start);
  assert.ok(start >= 0 && end > start);
  const render = vm.runInNewContext(`(() => { const selectedOffer = () => ({ accessoryPriceAmounts: { test: 1200 } }); const fa = String; ${source.slice(start, end)} return accessoryPriceMarkup; })()`);
  const marked = render({id: 'test', price: 0.0012, displayDiscountPercent: 12.5});
  assert.match(marked, /data-accessory-current-amount>1200</);
  assert.match(marked, /data-accessory-discount>12\.5٪ تخفیف/);
  assert.match(render({id: 'test', price: 0.0012, displayDiscountPercent: 0}), /data-accessory-discount hidden/);
});

test('accessory editor saves optional display percentage alongside the unchanged price', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.goto('about:blank');
  await page.setContent(`<div data-admin-section="products"><button data-product-accessories="0">Manage</button></div><div id="product-accessories-modal" data-product-accessories-manager><div data-product-accessories-name></div><div data-product-accessories-status></div><div data-product-accessories-rows></div><form data-product-accessory-form><input name="__RequestVerificationToken" value="token"><input name="id"><span data-product-accessory-form-title></span><input name="name" required><textarea name="description"></textarea><input name="priceAmount" required><input name="displayDiscountPercent" type="number" min="0" max="99.99" step="0.01"><input name="sortOrder" type="number" value="0"><input name="isRequired" type="checkbox"><input name="isActive" type="checkbox"><button type="submit">Save</button></form></div>`);
  await page.evaluate(() => {
    document.querySelector('[data-admin-section]').__adminRows = [{id: 1, name: 'Product', code: 'P'}];
    window.AdminUI = {setPriceValue: (input, value) => { input.value = value; }, priceValue: input => Number(input.value), formatNumber: String, notify: () => {}, getJson: async (url, options) => { if (!options) return []; window.savedAccessory = JSON.parse(options.body); return {message: 'ok'}; }};
  });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-data.js'});
  await page.locator('[data-product-accessories]').click();
  await page.locator('[name=name]').fill('Warranty');
  await page.locator('[name=priceAmount]').fill('1000');
  await page.locator('[name=displayDiscountPercent]').fill('12.5');
  await page.locator('[data-product-accessory-form] button').click();
  await page.waitForFunction(() => !!window.savedAccessory);
  const saved = await page.evaluate(() => window.savedAccessory);
  assert.equal(saved.priceAmount, 1000);
  assert.equal(saved.displayDiscountPercent, 12.5);
});
