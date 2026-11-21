import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync, readFileSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('catalog exposes product specifications and priced variant values to search', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless:true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  const catalog = {schemaVersion:2, brands:{b:{name:'Brand', fa:'برند'}}, categories:{c:{fa:'دریل', en:'Drill', slug:'drill', parentId:null, path:['دریل']}}, colors:{}, countries:{},
    details:{c:{specs:[['کاربرد', 'پیچ کاری']]}}, media:{m:{}}, collections:{},
    specificationGroups:{g:{name:'فنی'}}, specificationCategories:{s:{groupId:'g', name:'باتری'}}, specificationFeatures:{f:{categoryId:'s',label:'ولتاژ'}},
    products:[{id:1, name:'دریل', brandId:'b', categoryId:'c', detailId:'c', mediaId:'m', colorIds:[], specificationValues:[{featureId:'f',value:'۱۸ ولت'}]}]};
  const plans = {inventoryVariants:[{productId:1, attributes:[{label:'رنگ', value:'blue', display:'آبی'}]}, {productId:2, attributes:[{label:'رنگ', display:'قرمز'}]}]};
  await page.setContent('<script type="application/json" id="storeCatalog"></script><script type="application/json" id="storeSalesPlans"></script>');
  await page.evaluate(({catalog,plans}) => { document.querySelector('#storeCatalog').textContent = JSON.stringify(catalog); document.querySelector('#storeSalesPlans').textContent = JSON.stringify(plans); }, {catalog,plans});
  await page.addScriptTag({path:'Shop/wwwroot/shop/js/store/catalog.js'});
  const terms = await page.evaluate(() => window.HNProducts[0].searchableAttributes);
  assert.deepEqual(terms, ['کاربرد', 'پیچ کاری', 'ولتاژ', '۱۸ ولت', 'رنگ', 'آبی']);
  const searchText = await page.evaluate(() => window.HNCatalog.searchText(window.HNProducts[0]));
  assert.ok(searchText.includes('۱۸ ولت'));
  assert.ok(searchText.includes('آبی'));
  assert.ok(!searchText.includes('قرمز'));
  assert.match(readFileSync('Shop/wwwroot/shop/js/pages/search.js', 'utf8'), /window\.HNCatalog\.searchText\(p\)/);
});
