import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import vm from "node:vm";

test("movement mode changes ignore an older response", async () => {
  const source = await readFile(new URL("../../Admin/wwwroot/js/admin-movements.js", import.meta.url), "utf8");
  const requests = [];
  const element = () => ({ dataset: {}, classList: { toggle() {} }, addEventListener(type, fn) { this[type] = fn; }, setAttribute() {}, append(...items) { (this.children ||= []).push(...items); }, replaceChildren() { this.children = []; } });
  const stock = element(), audit = element(), rows = element(), filter = element(), prev = element(), next = element();
  stock.dataset.movementMode = "stock"; audit.dataset.movementMode = "audit";
  const map = { "[name=warehouseId]": element(), "[data-movement-filter]": filter, "[data-movement-rows]": rows, "[data-movement-status]": element(), "[data-movement-title]": element(), "[data-movement-head]": element(), "[data-movement-page-label]": element(), '[data-movement-page="-1"]': prev, '[data-movement-page="1"]': next };
  const page = { dataset: { api: "/movements" }, querySelector: key => map[key], querySelectorAll: key => key === "[data-movement-mode]" ? [stock, audit] : [prev, next] };
  const window = { AdminUI: { getJson(url) { if (url.includes("warehouses")) return Promise.resolve([]); return new Promise(resolve => requests.push(resolve)); } } };
  class EmptyFormData { *[Symbol.iterator]() {} }
  vm.runInNewContext(source, { window, document: { createElement: element }, WeakSet, WeakMap, Intl, URLSearchParams, FormData: EmptyFormData, setTimeout, Date, JSON });
  void window.AdminMovements.initialize({ querySelector: () => page });
  await new Promise(resolve => setImmediate(resolve));
  audit.click();
  requests[1]({ items: [{ occurredAtUtc: "2026-01-01T00:00:00Z", documentNumber: 1, action: "Created", warehouse: "انبار", actorUserKey: "مدیر" }], total: 1 });
  await new Promise(resolve => setImmediate(resolve));
  requests[0]([{ documentNumber: 2 }]);
  await new Promise(resolve => setImmediate(resolve));
  assert.equal(rows.children.length, 1);
  assert.equal(rows.children[0].children[2].textContent, "ایجاد");
  assert.equal(map["[data-movement-status]"].textContent, "");
});

test("summary renders opening, receipts, dispatches and closing separately", async () => {
  const source = await readFile(new URL("../../Admin/wwwroot/js/admin-movements.js", import.meta.url), "utf8");
  const element = () => ({ dataset: {}, classList: { toggle() {} }, addEventListener(type, fn) { this[type] = fn; }, setAttribute() {}, append(...items) { (this.children ||= []).push(...items); }, replaceChildren() { this.children = []; } });
  const summary = element(), rows = element(), previous = element(), next = element(), status = element();
  summary.dataset.movementMode = "summary";
  const map = { "[name=warehouseId]": element(), "[data-movement-filter]": element(), "[data-movement-rows]": rows, "[data-movement-status]": status, "[data-movement-title]": element(), "[data-movement-head]": element(), "[data-movement-page-label]": element(), '[data-movement-page="-1"]': previous, '[data-movement-page="1"]': next };
  const page = { dataset: { api: "/movements" }, querySelector: key => map[key], querySelectorAll: key => key === "[data-movement-mode]" ? [summary] : [previous, next] };
  const window = { AdminUI: { getJson(url) {
    if (url.includes("warehouses")) return Promise.resolve([]);
    if (url.includes("summary=true")) return Promise.resolve({ items: [{ warehouse: "انبار", productKey: "A", product: "کالا", salesPlan: "طرح", unit: "عدد", opening: 10, incoming: 3, outgoing: 2, closing: 11 }], total: 1 });
    return Promise.resolve([]);
  } } };
  class EmptyFormData { *[Symbol.iterator]() {} }
  vm.runInNewContext(source, { window, document: { createElement: element }, WeakSet, WeakMap, Intl, URLSearchParams, FormData: EmptyFormData, setTimeout, Date, JSON });
  await window.AdminMovements.initialize({ querySelector: () => page });
  summary.click();
  await new Promise(resolve => setImmediate(resolve));
  assert.equal(rows.children.length, 1);
  assert.deepEqual(rows.children[0].children.slice(-4).map(cell => cell.textContent), ["۱۰", "۳", "۲", "۱۱"]);
  assert.match(status.textContent, /۱ ردیف/);
});
