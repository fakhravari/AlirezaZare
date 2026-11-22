import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import vm from "node:vm";

test("order pagination follows asynchronously loaded and replaced order cards", async () => {
  const listeners = new Map();
  const heading = { textContent: "", focus() {}, scrollIntoView() {} };
  const total = { textContent: "" };
  const list = { innerHTML: "" };
  let cards = [];
  let targetPage;
  const section = {
    append() {},
    querySelectorAll(selector) { return selector === ".profile-order-card" ? cards : []; },
    querySelector(selector) { return selector === ".profile-order-list" ? list : null; },
  };
  const document = {
    getElementById(id) { return ({ orders: section, ordersTitle: heading, ordersTotal: total })[id]; },
    createElement() { return { hidden: false, setAttribute() {}, append() {}, textContent: "" }; },
    addEventListener(name, handler) { listeners.set(name, handler); },
  };
  const location = { href: "https://example.test/Profile/Orders?page=2" };
  const history = { pushState(_state, _title, value) { location.href = value; }, replaceState(_state, _title, value) { location.href = value; } };
  const window = {
    location,
    addEventListener() {},
    HNUI: {
      mount(_name, _document, callback) { callback(); },
      formatNumber: String,
      renderPagination(_nav, _page, _count, callback) { targetPage = callback; },
    },
  };
  const source = await readFile("Shop/wwwroot/shop/js/pages/orders-pagination.js", "utf8");
  vm.runInNewContext(source, { window, document, history, URL });
  assert.equal(new URL(location.href).searchParams.get("page"), "2");
  cards = Array.from({ length: 7 }, () => ({ hidden: false }));
  listeners.get("hn:orders-change")();
  assert.equal(total.textContent, "7 سفارش");
  assert.equal(cards.filter(card => !card.hidden).length, 2);
  targetPage(1);
  assert.equal(cards.filter(card => !card.hidden).length, 5);
  cards = [{ hidden: false }];
  listeners.get("hn:orders-change")();
  assert.equal(total.textContent, "1 سفارش");
  assert.equal(cards[0].hidden, false);
});
