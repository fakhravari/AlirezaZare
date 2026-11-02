import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';
import {root} from '../helpers/runtime.mjs';
import path from 'node:path';

function checkoutCart() {
  let link;
  const makeLink = () => ({attributes: {}, classList: {toggle(name, value) { this[name] = value; }}, setAttribute(key, value) { this.attributes[key] = value; }});
  link = makeLink();
  const listeners = {};
  const address = {id:'home', title:'Home', recipient:'Test User', phone:'09123456789', province:'Test', street:'Street', postal:'1234567890'};
  const values = {'hn-delivery-addresses': JSON.stringify([address]), 'hn-selected-address':'home'};
  let items = [{id:'1', price:100, quantity:1, salesPlanId:'regular'}];
  const document = {querySelector: selector => selector === '#cartView .cart-checkout-link' ? link : null,
    querySelectorAll: () => [], addEventListener: (name, fn) => { listeners[name] = fn; }};
  const ui = {ready: fn => fn(), mount: (name, root, fn) => fn(),
    storage: {get: (key, fallback) => values[key] ?? fallback, set: (key, value) => {values[key] = value;}},
    cart: {read: () => items}, setMessage() {}};
  const window = {HNUI: ui, addEventListener() {}};
  vm.runInNewContext(fs.readFileSync(path.join(root, 'Shop/wwwroot/shop/js/pages/checkout.js'), 'utf8'), {window, document, console});
  return {getLink: () => link, replaceLink: () => {link = makeLink();}, listeners, setItems: value => {items = value;}};
}

test('selected address enables cart preview and regenerated link follows cart state', () => {
  const page = checkoutCart();
  assert.equal(page.getLink().attributes['aria-disabled'], 'false');
  page.replaceLink();
  page.listeners['hn:cart-change']();
  assert.equal(page.getLink().attributes['aria-disabled'], 'false');
  page.setItems([]);
  page.listeners['hn:cart-change']();
  assert.equal(page.getLink().attributes['aria-disabled'], 'true');
});
