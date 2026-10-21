import test from 'node:test';
import assert from 'node:assert/strict';
import {runtime, plain} from '../helpers/runtime.mjs';

test('legacy records remain readable, migrate on write and retain a one-time backup', () => {
  const old = '[1,6]';
  const {ui, values} = runtime(undefined, {'hn-favorites': old});
  assert.deepEqual(plain(ui.storage.json('hn-favorites')), [1,6]);
  assert.equal(ui.storage.set('hn-favorites', '[1,6,11]'), true);
  assert.deepEqual(JSON.parse(values.get('hn-favorites')), {schemaVersion: 1, value: [1,6,11]});
  assert.equal(values.get('hn-migration-backup:hn-favorites'), old);
  ui.storage.set('hn-favorites', '[1]');
  assert.equal(values.get('hn-migration-backup:hn-favorites'), old);
});

test('future schema is read-only and cannot be removed or downgraded', () => {
  const future = JSON.stringify({schemaVersion: 2, value: [1]});
  const {ui, values} = runtime(undefined, {'hn-favorites': future});
  assert.deepEqual(plain(ui.storage.json('hn-favorites', [])), []);
  assert.equal(ui.storage.writable('hn-favorites'), false);
  assert.equal(ui.storage.set('hn-favorites', '[]'), false);
  assert.equal(ui.storage.remove('hn-favorites'), false);
  assert.equal(values.get('hn-favorites'), future);
});

test('product feedback uses the same versioned migration contract', () => {
  const key = 'hn-product-reviews-1';
  const old = '[{"name":"guest","text":"review"}]';
  const {ui, values} = runtime(undefined, {[key]: old});
  assert.equal(ui.storage.json(key)[0].text, 'review');
  assert.equal(ui.storage.set(key, old), true);
  assert.equal(JSON.parse(values.get(key)).schemaVersion, 1);
  assert.equal(values.get('hn-migration-backup:' + key), old);
});

test('malformed JSON and invalid shapes use fallbacks and can be repaired with backup', () => {
  for (const bad of ['{broken', '{}', 'null', '12']) {
    const {ui, values} = runtime(undefined, {'hn-compare': bad});
    assert.deepEqual(plain(ui.storage.json('hn-compare', [])), []);
    assert.equal(ui.storage.set('hn-compare', '[1]'), true);
    assert.equal(values.get('hn-migration-backup:hn-compare'), bad);
  }
});

test('storage unavailable returns failure and emits no success event', () => {
  const {window, ui, events} = runtime();
  window.localStorage.setItem = () => { throw new Error('quota'); };
  assert.equal(ui.storage.set('hn-favorites', '[1]'), false);
  assert.equal(events.length, 0);
  window.localStorage.getItem = () => { throw new Error('blocked'); };
  assert.deepEqual(plain(ui.storage.json('hn-favorites', [])), []);
  assert.equal(ui.storage.writable('hn-favorites'), false);
});
