import { test } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync } from 'node:fs';
import { chromium } from '@playwright/test';

test('feature filters select, restore, combine and clear catalog values', { skip: !existsSync(chromium.executablePath()) }, async t => {
  const browser = await chromium.launch({ headless: true });
  t.after(() => browser.close());
  const page = await browser.newPage();
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  const group = (id, slug) => `<div class="filter-group" data-filter-slug="${slug}"><button type="button"></button><div class="filter-options" id="${id}Filters"></div></div>`;
  const html = `<div class="search-main"></div><div id="filterPanel">${group('brand', 'brand')}${group('type', 'product-group')}${group('color', 'color')}${group('salesPlan', 'sales-plan')}<div class="filter-group" data-filter-slug="price-range"><button type="button"></button></div></div>
    <div id="filterBackdrop"></div><button id="mobileFilterToggle"></button><button id="closeFilters"></button>
    <div class="price-range"></div><input id="minPrice" value="0"><input id="maxPrice" value="60"><span id="minPriceLabel"></span><span id="maxPriceLabel"></span>
    <input id="productSearch"><input id="discountFilter" type="checkbox"><div id="productResults"></div>
    <span id="resultCount"></span><span id="filterResultCount"></span><span id="endResultCount"></span>
    <div id="emptyResults"><button></button></div><div id="loadMoreState"></div><div id="loadMoreSentinel"></div><div id="resultsEnd"></div>
    <button id="clearFilters"></button><div id="activeChips"></div><span id="activeFilterCount"></span><span id="clearFilterCount"></span><div id="searchBreadcrumb"><ol></ol></div>`;
  await page.route('https://example.test/**', route => route.fulfill({ status: 200, contentType: 'text/html', body: html }));
  await page.goto('https://example.test/Search?feature-10=100');
  await page.evaluate(() => {
    window.HNProducts = [
      { id: 1, name: 'A', brand: 'B', type: 'Group', categoryId: 'c', categorySlug: 'c', colors: [], catalogFeatures: [{ featureId: 10, featureName: 'Power', valueId: 100, valueName: '18V', kind: 2 }, { featureId: 11, featureName: 'Color', valueId: 110, valueName: 'Blue', kind: 2 }, { featureId: 12, featureName: 'Capacity', valueId: 120, valueName: '2Ah', kind: 1 }] },
      { id: 2, name: 'B', brand: 'B', type: 'Group', categoryId: 'c', categorySlug: 'c', colors: [], catalogFeatures: [{ featureId: 10, featureName: 'Power', valueId: 101, valueName: '12V', kind: 2 }] },
    ];
    window.HNCatalog = { categories: { c: { id: 'c', slug: 'c', fa: 'Group', en: 'Group', path: ['Group'] } },
      attributeDefinitions: { brand: { is_filterable: true }, category: { is_filterable: true }, color: { is_filterable: true } },
      categoryContains: () => true, categoryForSlug: () => ({ id: 'c', slug: 'c', fa: 'Group', en: 'Group', path: ['Group'] }),
      categoryForLabel: () => null, categoryFacets: () => ({ brand: ['B'], type: ['c'], color: [] }), categoryUrl: () => '/Search', searchText: p => p.name };
    window.HNProductCard = { availableStock: () => 1, priceDetails: () => ({ discount: 0 }), currentPrice: () => 1000000, render: p => `<article data-id="${p.id}">${p.name}</article>` };
    window.HNUI = { ready: fn => fn(), mount: (_name, _root, fn) => fn(), formatNumber: String, normalizeSearchText: value => value.toLowerCase(), storage: { json: () => [] }, enhance: () => {}, cart: { read: () => [] }, dialog: { trap: () => {} }, scrollLock: { lock: () => {}, unlock: () => {} } };
  });
  await page.addScriptTag({ path: 'Shop/wwwroot/shop/js/pages/search.js' });
  assert.deepEqual(errors, []);
  assert.equal(await page.locator('[data-filter-slug="feature-12"]').count(), 0);
  assert.equal(await page.locator('#productResults article').count(), 1);
  assert.equal(await page.locator('input[data-catalog-feature="10"][value="100"]').isChecked(), true);
  await page.locator('[data-filter-slug="feature-10"] > button').click();
  await page.locator('input[data-catalog-feature="10"][value="101"]').check();
  assert.equal(await page.locator('#productResults article').count(), 2);
  assert.deepEqual(new URL(page.url()).searchParams.getAll('feature-10'), ['100', '101']);
  await page.locator('[data-filter-slug="feature-11"] > button').click();
  await page.locator('input[data-catalog-feature="11"][value="110"]').check();
  assert.equal(await page.locator('#productResults article').count(), 1);
  await page.locator('#clearFilters').click();
  assert.equal(await page.locator('#productResults article').count(), 2);
  assert.equal(new URL(page.url()).searchParams.has('feature-10'), false);
  assert.deepEqual(errors, []);
});
