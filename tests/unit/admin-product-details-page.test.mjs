import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync, readFileSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('product details page loads read-only open accordions and handles errors', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless: true});
  t.after(() => browser.close());
  const page = await browser.newPage({viewport: {width: 375, height: 812}});
  await page.setContent(`<div data-admin-page="product-details" data-product-id="42"><div data-product-details-status></div><div class="admin-record-product admin-product-details-record" data-product-details-record></div></div><div data-admin-page="product-details" data-product-id="404"><div data-product-details-status></div><div data-product-details-record></div></div>`);
  await page.addStyleTag({path: 'Admin/wwwroot/css/admin-ui.css'});
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-ui.js'});
  await page.evaluate(() => { window.AdminUI = {...window.AdminUI, getJson: async url => {
    if (url.includes('/404/')) throw Error('کالا پیدا نشد');
    return {name: 'کالای نمونه', variants: [{name:'آبی', attributes:[]}], media: null,
      accessories: [{name:'بیمه'}], currentPlanPrices: [{salesPlanName:'فروش عادی', priceAmount:100}], priceHistory: [], unitHistory: [], attributeDefinitions: [], salesOffers: []};
  }}; });
  await page.addScriptTag({path: 'Admin/wwwroot/js/admin-data.js'});
  await page.evaluate(() => window.AdminPages.initialize(document));
  const details = page.locator('[data-product-id="42"]');
  await details.getByText('کالای نمونه').waitFor();
  assert.ok(await details.locator('details').count() >= 8);
  assert.equal(await details.locator('details:not([open])').count(), 0);
  assert.equal(await details.getByText('بیمه').count(), 1);
  assert.equal(await details.getByText('فروش عادی').count(), 1);
  assert.equal(await details.locator('[data-product-details-record] button, [data-product-details-record] input, [data-product-details-record] form').count(), 0);
  await details.locator('summary').first().click();
  assert.equal(await details.locator('details').first().getAttribute('open'), null);
  assert.equal(await page.locator('[data-product-id="404"] [data-product-details-status]').innerText(), 'کالا پیدا نشد');
  assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
  await page.setViewportSize({width: 1440, height: 900});
  assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
});

test('product list uses a new-tab link and detail route uses existing catalog permission', () => {
  const script = readFileSync('Admin/wwwroot/js/admin-data.js', 'utf8');
  const auth = readFileSync('Admin/Security/AdminAuthentication.cs', 'utf8');
  assert.match(script, /details\.href = `\/Store\/Catalog\/ProductDetails\/\$\{encodeURIComponent\(item\.id\)\}`/);
  assert.match(script, /details\.target = "_blank"/);
  assert.match(script, /details\.rel = "noopener noreferrer"/);
  assert.match(auth, /string\.Equals\(action, "ProductDetails"[\s\S]*?action = "Products"/);
  assert.match(auth, /string\.Equals\(controller, "StoreCatalogAdminApi", StringComparison\.OrdinalIgnoreCase\)[\s\S]*?controller = "Catalog"/);
  const api = readFileSync('Data/Infrastructure/Store/StoreCatalogAdminData.cs', 'utf8');
  assert.match(api, /fullAccess \|\| allowedWarehouseIds\.Contains\(balanceDetails\.balance\.IdWarehouse\)/);
});
