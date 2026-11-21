import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('category tree nests branches, keeps actions mapped and searches ancestors', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage({viewport: {width: 375, height: 800}});
  await page.route('https://example.test/', route => route.fulfill({contentType: 'text/html', body: ''}));
  await page.goto('https://example.test/');
  await page.setContent(`<div dir="rtl" data-admin-page="catalog" data-admin-section="categories" data-admin-api="/api" data-can-upload-image="true" data-can-edit-category="true">
    <form data-admin-filter><input name="q"><button type="submit">جستجو</button><button type="button" data-category-tree-expand>بازکردن همه</button><button type="button" data-category-tree-collapse>بستن همه</button></form>
    <div data-admin-status></div><span data-admin-count></span><div data-admin-results role="tree"></div><div data-admin-empty></div><div data-admin-pagination></div></div>`);
  await page.addStyleTag({path: 'Admin/wwwroot/css/admin-ui.css'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-ui.js'});
  await page.evaluate(() => { window.AdminUI = {...AdminUI, getJson: async () => ({items: [
    {id:'root', name:'ابزار', parentId:null, isActive:true},
    {id:'child', name:'برقی', parentId:'root', isActive:true},
    {id:'leaf', name:'دریل', parentId:'child', isActive:false},
    {id:'second', name:'دیگر', parentId:null, isActive:true}
  ], total:4, hasNext:false})}; });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-data.js'});
  await page.evaluate(() => AdminPages.initialize(document));
  const tree = page.locator('[role=tree]');
  await tree.getByText('ابزار', {exact:true}).waitFor();
  assert.equal(await tree.getByText('برقی', {exact:true}).isVisible(), false);
  await tree.getByRole('button', {name:'بازکردن زیرشاخه‌های ابزار'}).click();
  assert.equal(await tree.getByText('برقی', {exact:true}).isVisible(), true);
  assert.equal(await tree.getByText('دریل', {exact:true}).isVisible(), false);
  await tree.getByRole('button', {name:'بازکردن زیرشاخه‌های برقی'}).press('Enter');
  assert.equal(await tree.getByText('دریل', {exact:true}).isVisible(), true);
  const leaf = tree.locator('[role=treeitem]').filter({has: page.getByText('دریل', {exact:true})}).last();
  assert.equal(await leaf.locator('[data-category-edit]').count(), 1);
  assert.equal(await leaf.locator('[data-category-content]').count(), 1);
  assert.equal(await leaf.locator('[data-catalog-status]').count(), 1);
  assert.equal(await leaf.locator('[data-admin-record]').evaluate(button => button.closest('[data-admin-page]').__adminRows[Number(button.dataset.adminRecord)].id), 'leaf');
  await page.getByRole('button', {name:'بستن همه'}).click();
  assert.equal(await tree.getByText('برقی', {exact:true}).isVisible(), false);
  await page.locator('[name=q]').fill('دریل');
  await page.getByRole('button', {name:'جستجو'}).click();
  await tree.getByText('دریل', {exact:true}).waitFor();
  assert.equal(await tree.getByText('دیگر', {exact:true}).count(), 0);
  await page.getByRole('button', {name:'بستن همه'}).click();
  assert.equal(await tree.getByText('دریل', {exact:true}).isVisible(), false);
  await page.getByRole('button', {name:'بازکردن همه'}).click();
  assert.equal(await tree.getByText('دریل', {exact:true}).isVisible(), true);
  assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
  await page.setViewportSize({width: 1440, height: 900});
  assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
});
