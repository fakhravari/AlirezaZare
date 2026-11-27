import {test} from 'node:test';
import assert from 'node:assert/strict';
import {existsSync, readFileSync} from 'node:fs';
import {chromium} from '@playwright/test';

test('catalog exposes product specifications and priced variant values to search', {skip: !existsSync(chromium.executablePath())}, async t => {
  const browser = await chromium.launch({headless:true});
  t.after(() => browser.close());
  const page = await browser.newPage();
  const catalog = {schemaVersion:2, brands:{b:{name:'Brand', fa:'برند'}}, categories:{c:{fa:'دریل', en:'Drill', slug:'drill', parentId:null, path:['دریل']}}, countries:{},
    details:{c:{specs:[['کاربرد', 'پیچ کاری']]}}, media:{m:{}}, collections:{},
    attributeDefinitions:{color:{label:'رنگ', source:'color'}},
    catalogFeatureOptions:[{featureId:8, featureName:'رنگ', valueId:12, valueName:'آبی', valueKey:'blue', nameEn:'Blue', hexCode:'#0000FF', kind:2}],
    specificationGroups:{g:{name:'فنی'}}, specificationCategories:{s:{groupId:'g', name:'باتری'}}, specificationFeatures:{f:{categoryId:'s',label:'ولتاژ'}},
    products:[{id:1, name:'دریل', brandId:'b', categoryId:'c', detailId:'c', mediaId:'m',
      catalogFeatures:[{featureId:8, featureName:'رنگ', valueId:12, valueName:'آبی', valueKey:'blue', nameEn:'Blue', hexCode:'#0000FF', kind:2}],
      specificationValues:[{featureId:'f',value:'۱۸ ولت'}]}]};
  const plans = {inventoryVariants:[{productId:1, attributes:[{label:'رنگ', value:'blue', display:'آبی'}]}, {productId:2, attributes:[{label:'رنگ', display:'قرمز'}]}]};
  await page.setContent('<script type="application/json" id="storeCatalog"></script><script type="application/json" id="storeSalesPlans"></script>');
  await page.evaluate(({catalog,plans}) => { document.querySelector('#storeCatalog').textContent = JSON.stringify(catalog); document.querySelector('#storeSalesPlans').textContent = JSON.stringify(plans); }, {catalog,plans});
  await page.addScriptTag({path:'Shop/wwwroot/shop/js/store/catalog.js'});
  const terms = await page.evaluate(() => window.HNProducts[0].searchableAttributes);
  for (const term of ['کاربرد', 'پیچ کاری', 'ولتاژ', '۱۸ ولت', 'رنگ', 'آبی']) assert.ok(terms.includes(term));
  assert.deepEqual(await page.evaluate(() => window.HNProducts[0].colors[0]),
    {id:'blue', fa:'آبی', en:'Blue', hex:'#0000FF', slug:'blue'});
  assert.equal(await page.evaluate(() => window.HNProductColors.blue.hex), '#0000FF');
  const searchText = await page.evaluate(() => window.HNCatalog.searchText(window.HNProducts[0]));
  assert.ok(searchText.includes('۱۸ ولت'));
  assert.ok(searchText.includes('آبی'));
  assert.ok(!searchText.includes('قرمز'));
  assert.match(readFileSync('Shop/wwwroot/shop/js/pages/search.js', 'utf8'), /window\.HNCatalog\.searchText\(p\)/);
});
