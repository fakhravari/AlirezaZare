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
  assert.throws(() => window.HNCatalog.find(1).variants.battery.options.push({}));
  assert.throws(() => window.HNProducts.push({}));
  assert.equal(window.HNProductUrl({id: 999}), '/search');
  assert.equal(window.HNCatalog.find(999), null);
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
