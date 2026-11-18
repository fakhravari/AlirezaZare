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
    window.AdminUI = {setPriceValue: (input, value) => { input.value = value; }, priceValue: input => Number(input.value), formatNumber: String, notify: () => {}, getJson: async (url, options) => { if (!options) return [{id: 4, name: 'Warranty', priceAmount: 1000, displayDiscountPercent: 12.5, isRequired: true, isActive: true}, {id: 5, name: 'Cover', priceAmount: 0, isRequired: false, isActive: false}]; window.savedAccessory = JSON.parse(options.body); return {message: 'ok'}; }};
  });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-data.js'});
  await page.locator('[data-product-accessories]').click();
  await page.waitForSelector('.admin-accessory-item');
  assert.equal(await page.locator('.admin-accessory-item').count(), 2);
  assert.match(await page.locator('.admin-accessory-item').nth(0).innerText(), /Warranty[\s\S]*1000 تومان[\s\S]*اجباری[\s\S]*فعال[\s\S]*12\.5/);
  assert.match(await page.locator('.admin-accessory-item').nth(1).innerText(), /Cover[\s\S]*اختیاری[\s\S]*غیرفعال/);
  await page.locator('[data-product-accessory-edit="0"]').click();
  assert.equal(await page.locator('[name=name]').inputValue(), 'Warranty');
  assert.equal(await page.locator('[name=displayDiscountPercent]').inputValue(), '12.5');
  await page.locator('[name=name]').fill('Warranty');
  await page.locator('[name=priceAmount]').fill('1000');
  await page.locator('[name=displayDiscountPercent]').fill('12.5');
  await page.locator('[data-product-accessory-form] button').click();
  await page.waitForFunction(() => !!window.savedAccessory);
  const saved = await page.evaluate(() => window.savedAccessory);
  assert.equal(saved.priceAmount, 1000);
  assert.equal(saved.displayDiscountPercent, 12.5);
});

test('accessory controls share a desktop row and action buttons align left', {skip: !existsSync(chromium.executablePath())}, async t => {
  const markup = await readFile('Admin/Areas/Store/Views/Catalog/Index.cshtml', 'utf8');
  const modal = markup.slice(markup.indexOf('<div class="modal fade" id="product-accessories-modal"'), markup.indexOf('<div class="modal fade" id="product-editor-modal"'));
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<div dir="rtl">${modal}</div>`);
  await page.addStyleTag({path: 'Admin/wwwroot/color_admin/assets/css/vendor.min.css'});
  await page.addStyleTag({path: 'Admin/wwwroot/color_admin/assets/css/default/app-rtl.min.css'});
  await page.addStyleTag({path: 'Admin/wwwroot/css/admin-ui.css'});
  await page.locator('#product-accessories-modal').evaluate(el => { el.style.display = 'block'; });
  for (const width of [900, 1440, 375]) {
    await page.setViewportSize({width, height: 900});
    const geometry = await page.evaluate(() => {
      const grid = document.querySelector('.admin-product-accessory-grid');
      const fields = [...grid.children].map(el => ({top: el.getBoundingClientRect().top, left: el.getBoundingClientRect().left, right: el.getBoundingClientRect().right}));
      const actions = document.querySelector('.admin-accessory-actions').getBoundingClientRect();
      const buttons = [...document.querySelectorAll('.admin-accessory-actions button')].map(el => el.getBoundingClientRect());
      const controls = [...grid.querySelectorAll('.form-control, .form-check')].map(el => el.getBoundingClientRect().top);
      return {fields, controls, actions: {left: actions.left}, buttons: buttons.map(rect => ({left: rect.left}))};
    });
    assert.ok(Math.abs(Math.min(...geometry.buttons.map(button => button.left)) - geometry.actions.left) < 2);
    if (width >= 768) {
      assert.ok(geometry.fields.every(field => field.top === geometry.fields[0].top));
      assert.ok(Math.max(...geometry.controls) - Math.min(...geometry.controls) < 3, `controls are not aligned: ${geometry.controls}`);
      for (let i = 1; i < geometry.fields.length; i++) assert.ok(geometry.fields[i].right <= geometry.fields[i - 1].left);
    } else {
      for (let i = 1; i < geometry.fields.length; i++) assert.ok(geometry.fields[i].top > geometry.fields[i - 1].top);
    }
  }
});
