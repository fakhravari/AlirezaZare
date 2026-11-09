import test from 'node:test';
import assert from 'node:assert/strict';
import {runtime, plain} from '../helpers/runtime.mjs';

test('legacy account data is kept in memory for migration and removed from local storage', () => {
  const old = '[1,6]';
  const {ui, values} = runtime(undefined, {'hn-favorites': old, unrelated: 'keep'});
  assert.deepEqual(plain(ui.storage.json('hn-favorites')), [1, 6]);
  assert.equal(ui.storage.legacy('hn-favorites'), old);
  assert.equal(values.has('hn-favorites'), false);
  assert.equal(values.get('unrelated'), 'keep');
  ui.storage.clearLegacy('hn-favorites');
  assert.equal(ui.storage.legacy('hn-favorites'), null);
});

test('future and invalid legacy payloads are never interpreted as current data', () => {
  for (const raw of ['{broken', '{}', 'null', '12', JSON.stringify({schemaVersion: 2, value: [1]})]) {
    const {ui} = runtime(undefined, {'hn-compare': raw});
    assert.deepEqual(plain(ui.storage.json('hn-compare', [])), []);
    assert.equal(ui.storage.legacy('hn-compare'), raw);
    assert.equal(ui.storage.set('hn-compare', '[1]'), true);
    assert.deepEqual(plain(ui.storage.json('hn-compare')), [1]);
  }
});

test('versioned product feedback is decoded without persisting new data locally', () => {
  const key = 'hn-product-reviews-1';
  const raw = JSON.stringify({schemaVersion: 1, value: [{text: 'review'}]});
  const {ui, values} = runtime(undefined, {[key]: raw});
  assert.equal(ui.storage.json(key)[0].text, 'review');
  assert.equal(ui.storage.set(key, '[{"text":"new"}]'), true);
  assert.equal(values.has(key), false);
  assert.equal(ui.storage.json(key)[0].text, 'new');
});

test('unavailable local storage does not prevent in-memory state', () => {
  const {window, ui} = runtime();
  window.localStorage.setItem = () => { throw new Error('quota'); };
  assert.equal(ui.storage.set('hn-favorites', '[1]'), true);
  assert.deepEqual(plain(ui.storage.json('hn-favorites')), [1]);
});
