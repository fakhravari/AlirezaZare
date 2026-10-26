import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import {root, source, runtime, plain} from '../helpers/runtime.mjs';

const baseline = JSON.parse(fs.readFileSync(path.join(root, 'tests/fixtures/catalog-baseline.json'), 'utf8'));

test('catalog extraction preserves product identity, media, stock, price and canonical URL', () => {
  const {window} = runtime();
  for (const expected of baseline) {
    const product = window.HNCatalog.find(expected.id);
    for (const [key, value] of Object.entries(expected)) {
      assert.equal(key === 'url' ? window.HNProductUrl(product) : product[key], value, `${expected.id}: ${key}`);
    }
  }
});

test('reordering products cannot change IDs, URLs or relationships', () => {
  const data = source();
  data.products.reverse();
  const first = runtime().window;
  const reordered = runtime(data).window;
  for (const {id} of baseline) assert.deepEqual(plain(reordered.HNCatalog.find(id)), plain(first.HNCatalog.find(id)));
  assert.deepEqual(plain(first.HNCatalog.collections), plain(reordered.HNCatalog.collections));
});

test('category URLs carry one category slug and category filters include descendants', () => {
  const {window} = runtime();
  const powerToolsUrl = new URL(window.HNCatalog.categoryUrl(['ابزار و تجهیزات', 'ابزار برقی']), 'http://localhost');
  const powerTools = window.HNCatalog.categoryForSlug('power-tools');
  const hammerDrill = window.HNCatalog.find(1);
  const cordlessDrill = window.HNCatalog.find(2);
  const multimeter = window.HNCatalog.find(6);

  assert.deepEqual(powerToolsUrl.searchParams.getAll('product-group'), ['power-tools']);
  assert.equal(powerToolsUrl.searchParams.has('q'), false);
  assert.equal(powerTools.fa, 'ابزار برقی');
  assert.equal(window.HNCatalog.categoryMatchesPath('power-tools', hammerDrill.categoryPath), true);
  assert.equal(window.HNCatalog.categoryMatchesPath('power-tools', multimeter.categoryPath), false);
  assert.equal(window.HNCatalog.categoryMatchesPath('drill-drivers', hammerDrill.categoryPath), true);
  assert.equal(window.HNCatalog.categoryMatchesPath('drill-drivers', multimeter.categoryPath), false);
  assert.equal(window.HNCatalog.categoryContains('drill-drivers', cordlessDrill.categoryId), true);
  assert.equal(window.HNCatalog.categoryContains('cordless-drill', cordlessDrill.categoryId), true);
  assert.equal(window.HNCatalog.categoryContains('cordless-drill', hammerDrill.categoryId), false);
  assert.equal(window.HNCatalog.categoryMatchesPath('tools-and-equipment', hammerDrill.categoryPath), true);
  assert.deepEqual(
    new URL(window.HNCatalog.categoryUrl(hammerDrill.categoryPath), 'http://localhost')
      .searchParams.getAll('product-group'),
    ['hammer-drills'],
  );
  const voltageProtectorCategories = Object.values(window.HNCatalog.categories)
    .filter(category => category.fa === 'محافظ ولتاژ');
  assert.equal(voltageProtectorCategories.length, 2);
  assert.notEqual(
    window.HNCatalog.categoryUrl(voltageProtectorCategories[0].path),
    window.HNCatalog.categoryUrl(voltageProtectorCategories[1].path),
  );
});

test('all products use leaf categories and category paths follow parent links', () => {
  const {window} = runtime();
  const categories = Object.values(window.HNCatalog.categories);
  const parentsWithChildren = new Set(categories.map(category => category.parentId).filter(Boolean));
  const slugs = categories.map(category => category.slug);

  assert.equal(window.HNCatalog.schemaVersion, 2);
  assert.equal(new Set(slugs).size, categories.length);
  for (const category of categories) {
    assert.ok(category.en && category.slug, category.id);
    const resolved = window.HNCatalog.categoryForSlug(category.slug);
    assert.deepEqual(resolved.path, category.path, category.id);
  }
  for (const product of window.HNProducts) {
    assert.ok(!parentsWithChildren.has(product.categoryId), `Product ${product.id} must reference a leaf category`);
    assert.deepEqual(product.categoryPath, window.HNCatalog.categoryForSlug(product.categorySlug).path);
  }
});

test('broken relations, duplicate IDs and unknown schema versions fail explicitly', () => {
  for (const mutate of [
    d => d.products[0].id = d.products[1].id,
    d => d.products[0].brandId = 'unknown',
    d => d.products[0].categoryId = 'unknown',
    d => d.products[0].colorIds = ['unknown'],
    d => d.products[0].mediaId = 'unknown',
    d => d.products[0].detailId = 'unknown',
    d => d.schemaVersion = 99,
  ]) { const data = source(); mutate(data); assert.throws(() => runtime(data)); }
});

test('resolved catalog is immutable and unknown IDs do not create product URLs', () => {
  const {window} = runtime();
  const category = window.HNCatalog.categoryForSlug('hammer-drills');
  assert.throws(() => { category.parentId = category.id; });
  assert.throws(() => category.path.push('changed'));
  assert.equal(window.HNCatalog.categoryContains('power-tools', category.id), true);
  assert.throws(() => window.HNCatalog.find(1).variants.battery.options.push({}));
  assert.throws(() => window.HNProducts.push({}));
  assert.equal(window.HNProductUrl({id: 999}), '/search');
  assert.equal(window.HNCatalog.find(999), null);
});

test('product specifications override shared values in highlighted attributes', () => {
  const data = source();
  const product = data.products[0];
  data.details[product.detailId].specs = [['توان آزمایشی', 'shared']];
  data.details[product.detailId].specGroups = [{title: 'Shared', specs: [['توان آزمایشی', 'shared group']]}];
  product.specs = [['توان آزمایشی', 'product']];
  product.specGroups = [];
  data.attributeDefinitions.testPower = {
    label: 'توان آزمایشی', source: 'spec:توان آزمایشی', is_highlight: true,
  };
  const value = catalog => runtime(catalog).window.HNCatalog.find(product.id)
    .attributeValues.find(attribute => attribute.key === 'testPower').value;
  assert.equal(value(data), 'product');
  product.specGroups = [{title: 'Product', specs: [['توان آزمایشی', 'product group']]}];
  assert.equal(value(data), 'product group');
});

test('every referenced media file exists with exact lowercase casing', () => {
  const data = source();
  const files = new Set();
  for (const media of Object.values(data.media)) {
    files.add(media.main).add(media.card).add(media.thumbnail);
    for (const image of media.images) files.add(image.src).add(image.thumbnail);
    for (const video of media.videos) files.add(video);
  }
  for (const brand of Object.values(data.brands)) if (brand.logo) files.add(brand.logo);
  for (const country of Object.values(data.countries)) files.add(country.flag);
  for (const file of files) {
    assert.equal(file, file.toLowerCase());
    assert.ok(!file.includes('..') && !file.includes('\\'));
    const absolute = path.join(root, 'Shop/wwwroot', file);
    assert.ok(fs.existsSync(absolute), file);
    assert.ok(fs.readdirSync(path.dirname(absolute)).includes(path.basename(file)), file);
  }
  const codes = data.products.map(p => p.code);
  assert.equal(new Set(codes).size, codes.length);
  for (const ids of Object.values(data.collections)) for (const id of ids) assert.ok(data.products.some(p => p.id === id));
});
