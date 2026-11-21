import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('category creation and editing use the real options contract and preserve the parent', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  await page.route('https://example.test/', route => route.fulfill({contentType:'text/html', body:''}));
  await page.goto('https://example.test/');
  await page.setContent(`<div data-admin-page="catalog" data-admin-section="categories" data-admin-api="/api" data-can-upload-image="true">
    <button data-category-create>افزودن</button><form data-admin-filter><input name="q"></form><div data-admin-status></div>
    <div data-admin-results role="tree"></div><div data-admin-empty></div><div data-admin-pagination></div></div>
    <div id="category-editor-modal"><form data-category-editor><input name="originalKey" type="hidden"><input name="key"><input name="nameFa"><input name="nameEn"><select name="parentId"></select><button type="submit">ذخیره</button></form></div>`);
  await page.addScriptTag({path:'Admin/wwwroot/js/admin-ui.js'});
  await page.evaluate(() => {
    window.saved = [];
    window.bootstrap = {Modal:{getOrCreateInstance:() => ({show(){ window.shown = (window.shown || 0) + 1; }}), getInstance:() => ({hide(){}})}};
    window.AdminUI = {...AdminUI, notify(message, kind){ if (kind === 'danger') window.error = message; }, getJson: async (url, options = {}) => {
      url = String(url);
      if (options.method) { window.saved.push({url, method:options.method, body:JSON.parse(options.body)}); return {message:'ok'}; }
      if (url.includes('/manage/options')) return [
        {id:1, categoryKey:'root', nameFa:'ابزار', nameEn:'Tools', idParentCategory:null},
        {id:2, categoryKey:'child', nameFa:'برقی', nameEn:'Power', idParentCategory:1},
        {id:3, categoryKey:'leaf', nameFa:'دریل', nameEn:'Drill', idParentCategory:2}
      ];
      return {items:[{id:'child', name:'برقی', parentId:null, isActive:true}], total:1, hasNext:false};
    }};
  });
  await page.addScriptTag({path:'Admin/wwwroot/js/admin-data.js'});
  await page.evaluate(() => AdminPages.initialize(document));
  await page.locator('[data-category-edit]').waitFor();
  await page.locator('[data-category-edit]').click();
  await page.waitForFunction(() => window.shown === 1);
  const form = page.locator('[data-category-editor]');
  assert.equal(await form.locator('[name=key]').inputValue(), 'child');
  assert.equal(await form.locator('[name=key]').isEditable(), false);
  assert.equal(await form.locator('[name=parentId]').inputValue(), '1');
  assert.equal(await form.locator('[name=parentId]').isDisabled(), true);
  assert.equal(await form.locator('[name=parentId] option[value="3"]').textContent(), 'ابزار / برقی / دریل');
  await form.locator('[name=nameFa]').fill('ابزار برقی');
  await form.locator('button[type=submit]').click();
  await page.waitForFunction(() => window.saved.length === 1);
  assert.deepEqual(await page.evaluate(() => window.saved[0]), {url:'/api/admin/store/catalog/categories/manage/child', method:'PUT', body:{key:'child', nameFa:'ابزار برقی', nameEn:'Power', parentId:1}});
  await page.locator('[data-category-create]').click();
  await page.waitForFunction(() => window.shown === 2);
  assert.equal(await form.locator('[name=parentId]').isEnabled(), true);
  assert.equal(await form.locator('[name=parentId] option[value="3"]').textContent(), 'ابزار / برقی / دریل');
  assert.equal(await page.evaluate(() => window.error ?? null), null);
});
