import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import {fileURLToPath} from 'node:url';

export const root = fileURLToPath(new URL('../../', import.meta.url));
export const source = () => JSON.parse(fs.readFileSync(path.join(root, 'Shop/wwwroot/content/data/catalog.json'), 'utf8'));
export const plain = value => JSON.parse(JSON.stringify(value));

export function runtime(data = source(), seed = {}, apiBasePath = '/api/') {
  const values = new Map(Object.entries(seed));
  const events = [];
  const listeners = [];
  const document = {
    readyState: 'loading', activeElement: null,
    querySelector: selector => selector === '#storeCatalog' ? {textContent: JSON.stringify(data)}
      : selector === 'meta[name="api-base-url"]' ? {content: apiBasePath, dataset: {timeoutMs: '15000'}} : null,
    addEventListener: (...args) => listeners.push(args),
    dispatchEvent: event => { events.push(event); return true; },
  };
  const window = {document, location: {href: 'https://shop.example/profile', origin: 'https://shop.example'},
    AbortController, URL, setTimeout, clearTimeout,
    addEventListener: (...args) => listeners.push(args),
    localStorage: {
      getItem: key => values.get(key) ?? null,
      setItem: (key, value) => values.set(key, String(value)),
      removeItem: key => values.delete(key),
    },
  };
  class CustomEvent { constructor(type, options = {}) { this.type = type; this.detail = options.detail; } }
  const context = vm.createContext({window, document, CustomEvent, URL, URLSearchParams, console, setTimeout, clearTimeout});
  const load = name => vm.runInContext(fs.readFileSync(path.join(root, 'Shop/wwwroot/shop/js', name), 'utf8'), context, {filename: name});
  load('store/catalog.js');
  load('core/storage.js');
  load('store/cart.js');
  load('core/ui-core.js');
  load('store/product-card.js');
  return {window, document, values, events, listeners, load, context, ui: window.HNUI};
}
