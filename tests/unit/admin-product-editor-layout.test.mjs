import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync, existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('product creation and editor keep requested rows after the shared modal setup', {skip: !existsSync(chromium.executablePath())}, async t => {
  const markup = readFileSync('Admin/Areas/Store/Views/Catalog/Index.cshtml', 'utf8');
  const createModal = markup.slice(markup.indexOf('<div class="modal fade" id="product-create-modal"'), markup.indexOf('<div class="modal fade" id="product-accessories-modal"'));
  const editModal = markup.slice(markup.indexOf('<div class="modal fade" id="product-editor-modal"'), markup.indexOf('<div class="modal fade" id="product-benefits-modal"'));
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.setContent(`<div dir="rtl">${createModal}${editModal}</div>`);
  await page.addStyleTag({path: 'Admin/wwwroot/color_admin/assets/css/vendor.min.css'});
  await page.addStyleTag({path: 'Admin/wwwroot/color_admin/assets/css/default/app-rtl.min.css'});
  await page.addStyleTag({path: 'Admin/wwwroot/css/admin-ui.css'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-ui.js'});
  await page.evaluate(() => {
    for (const id of ['product-create-modal', 'product-editor-modal']) {
      const modal = document.getElementById(id);
      modal.style.display = 'block';
      modal.dispatchEvent(new Event('show.bs.modal', {bubbles: true}));
    }
  });
  for (const width of [900, 1440, 375]) {
    await page.setViewportSize({width, height: 900});
    for (const id of ['product-create-modal', 'product-editor-modal']) {
      const positions = await page.evaluate(id => Object.fromEntries(['name', 'english', 'unit', 'category', 'brand', 'price', 'discount', 'limit'].map(key => [key, document.querySelector(`#${id} .admin-product-${key}`).getBoundingClientRect().top])), id);
      if (width >= 768) {
        assert.equal(positions.name, positions.english);
        assert.equal(positions.name, positions.unit);
        assert.equal(positions.category, positions.brand);
        assert.equal(positions.price, positions.discount);
        assert.equal(positions.price, positions.limit);
        assert.ok(positions.name < positions.category && positions.category < positions.price);
        const widths = await page.evaluate(id => Object.fromEntries(['name', 'english', 'unit', 'category', 'brand'].map(key => [key, document.querySelector(`#${id} .admin-product-${key}`).getBoundingClientRect().width])), id);
        assert.ok(widths.name > widths.unit && widths.english > widths.unit);
        assert.ok(widths.category > widths.brand);
      } else {
        assert.ok(positions.name < positions.english && positions.english < positions.unit && positions.unit < positions.category && positions.category < positions.brand && positions.brand < positions.price && positions.price < positions.discount && positions.discount < positions.limit);
      }
    }
  }
  assert.equal(await page.locator('#product-edit-english').evaluate(input => getComputedStyle(input).direction), 'ltr');
  assert.doesNotMatch(await page.locator('#product-editor-modal .modal-body').innerText(), /تکمیل همهٔ فیلدها الزامی است|\?/);
  assert.equal(await page.locator('#product-edit-name').getAttribute('required'), '');
});
