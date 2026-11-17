import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync, existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('product editor keeps requested rows after the shared modal setup', {skip: !existsSync(chromium.executablePath())}, async t => {
  const markup = readFileSync('Admin/Areas/Store/Views/Catalog/Index.cshtml', 'utf8');
  const modal = markup.slice(markup.indexOf('<div class="modal fade" id="product-editor-modal"'), markup.indexOf('<div class="modal fade" id="product-benefits-modal"'));
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<div dir="rtl">${modal}</div>`);
  await page.addStyleTag({path: 'Admin/wwwroot/color_admin/assets/css/vendor.min.css'});
  await page.addStyleTag({path: 'Admin/wwwroot/color_admin/assets/css/default/app-rtl.min.css'});
  await page.addStyleTag({path: 'Admin/wwwroot/css/admin-ui.css'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-ui.js'});
  await page.evaluate(() => {
    const modal = document.getElementById('product-editor-modal');
    modal.style.display = 'block';
    modal.dispatchEvent(new Event('show.bs.modal', {bubbles: true}));
  });
  for (const width of [900, 1440, 375]) {
    await page.setViewportSize({width, height: 900});
    const positions = await page.evaluate(() => Object.fromEntries(['name', 'english', 'brand', 'category', 'price', 'discount', 'limit'].map(key => [key, document.querySelector(`.admin-product-${key}`).getBoundingClientRect().top])));
    if (width >= 768) {
      assert.equal(positions.name, positions.english);
      assert.equal(positions.brand, positions.category);
      assert.equal(positions.price, positions.discount);
      assert.equal(positions.price, positions.limit);
      assert.ok(positions.name < positions.brand && positions.brand < positions.price);
    } else {
      assert.ok(positions.name < positions.english && positions.english < positions.brand && positions.price < positions.discount && positions.discount < positions.limit);
    }
  }
  assert.equal(await page.locator('#product-edit-english').evaluate(input => getComputedStyle(input).direction), 'ltr');
  assert.doesNotMatch(await page.locator('#product-editor-modal .modal-body').innerText(), /تکمیل همهٔ فیلدها الزامی است|\?/);
  assert.equal(await page.locator('#product-edit-name').getAttribute('required'), '');
});
