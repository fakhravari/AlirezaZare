import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import vm from "node:vm";

test("price drafts survive search and cannot be saved twice while pending", async () => {
  const source = await readFile(new URL("../../Admin/wwwroot/js/admin-variant-prices.js", import.meta.url), "utf8");
  class Element {
    constructor() { this.children = []; this.dataset = {}; this.classList = { toggle() {} }; this.validity = { valid: true }; this.value = ""; }
    append(...children) { this.children.push(...children); }
    replaceChildren() { this.children = []; }
    addEventListener(type, fn) { this[type] = fn; }
    setAttribute() {}
  }
  const filter = new Element(), search = new Element(), body = new Element(), count = new Element(), status = new Element(), refresh = new Element();
  filter.querySelector = () => search;
  const nodes = { "[data-price-filter]": filter, "[data-price-filter] input": search, "[data-price-rows]": body, "[data-price-count]": count, "[data-price-status]": status, "[data-price-refresh]": refresh };
  const page = { dataset: { api: "/prices", canEdit: "true" }, querySelector: selector => nodes[selector] || null };
  const calls = [];
  const window = { AdminUI: { notify() {}, getJson(url, options) { calls.push(options.method); return options.method === "GET" ? Promise.resolve([{ variantId: 1, productName: "کالا", variantName: "آبی", salesPlanName: "عادی", salesPlanId: "regular", priceAmount: 10, discountPercent: 0 }]) : new Promise(() => {}); } } };
  vm.runInNewContext(source, { window, document: { createElement: () => new Element() }, WeakSet, Map, Set, Intl, Number, encodeURIComponent });
  window.AdminVariantPrices.initialize({ querySelector: () => page });
  await new Promise(resolve => setImmediate(resolve));
  const amount = body.children[0].children[3].children[0];
  amount.value = "25"; amount.input();
  search.value = "ناموجود"; filter.input();
  search.value = "کالا"; filter.input();
  const row = body.children[0];
  assert.equal(row.children[3].children[0].value, "25");
  const save = row.children[6].children[0];
  void save.click();
  await save.click();
  assert.deepEqual(calls, ["GET", "PUT"]);
  assert.equal(save.disabled, true);
  let prevented = false;
  filter.submit({ preventDefault() { prevented = true; } });
  assert.equal(prevented, true);
});
