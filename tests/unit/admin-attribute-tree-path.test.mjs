import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('attribute tree table shows a database-derived path column', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  const errors = []; page.on("pageerror", error => errors.push(error.message));
  await page.route("https://example.test/", route => route.fulfill({contentType: "text/html", body: ""}));
  await page.goto("https://example.test/");
  await page.setContent('<div data-admin-page="catalog" data-admin-section="attributes" data-admin-api="/api/admin/store/catalog/attributes"><form data-admin-filter><input name="q"></form><div data-admin-status></div><div data-admin-count></div><table data-admin-datatable><tbody data-admin-results></tbody></table><div data-admin-empty></div><div data-admin-pagination></div></div>');
  await page.evaluate(() => {
    window.AdminUI = {
      async getJson() { return {items: [{id: 4, key: 'color', label: 'رنگ', sourceKey: 'color', filterKey: 'color', isFilterable: true, isHighlight: false, isActive: true}], total: 1, page: 1, pageSize: 200}; },
      destroyDataTable() {}, refreshDataTable() {}, renderNumberedPagination() {}, notify() {}, withIcon() {}, formatNumber: String
    };
  });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-data.js'});
  await page.evaluate(() => AdminPages.initialize(document));
  const row = page.locator('[data-admin-results] tr.admin-attribute-row');
  await page.waitForTimeout(500);
  assert.equal(await row.count(), 1, `${await page.locator("[data-admin-status]").textContent()} | ${errors.join(";")}`);
  assert.equal(await row.locator('td').count(), 9);
  assert.equal(await row.locator('td').nth(2).textContent(), 'ویژگی‌های کالا / رنگ');
});
