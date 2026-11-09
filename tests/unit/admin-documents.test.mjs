import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import vm from "node:vm";

test("returning to documents reloads warehouse definitions", async () => {
  const source = await readFile(new URL("../../Shop.Admin/wwwroot/js/admin-documents.js", import.meta.url), "utf8");
  let version = 0, current;
  const node = () => ({ dataset: {}, classList: { toggle() {} }, children: [], value: "", addEventListener() {}, replaceChildren(...items) { this.children = items; }, append(...items) { this.children.push(...items); }, querySelector() { return node(); } });
  function fixture() {
    const filterWarehouses = node(), page = { isConnected: true, dataset: { api: "/documents", documentFeature: "receipts", canCreate: "true" } };
    const pagination = node(), controls = { "[data-document-warehouses]": filterWarehouses, "[data-document-create]": node(), "[data-document-filter]": node(), "[data-document-reset]": node(), "[data-document-status]": node(), "[data-document-rows]": node(), "[data-document-pagination]": pagination };
    page.querySelector = selector => controls[selector]; page.querySelectorAll = () => [];
    const global = { "[data-document-warehouse]": node(), "[data-document-types]": node(), "[data-document-types-help]": node(), "[data-document-plans]": node(), "[data-document-parties]": node(), "[data-reference-label]": node(), "[data-document-reference]": node(), "[data-document-line-add]": node(), "[data-document-form]": node() };
    return { page, global, filterWarehouses };
  }
  const window = { bootstrap: { Modal: { getOrCreateInstance: () => ({}) } }, AdminUI: { getJson(url) { if (url.endsWith("/lookups")) { version++; return Promise.resolve({ warehouses: [{ id: version, name: `انبار ${version}`, code: "W" }], types: [], parties: [], plans: [], ownerLinks: [], inventoryItems: [] }); } return Promise.resolve({ items: [] }); } } };
  const document = { createElement: node, getElementById: () => ({}), querySelector: selector => current.global[selector] || null };
  class EmptyFormData { *[Symbol.iterator]() {} }
  vm.runInNewContext(source, { window, document, Intl, WeakSet, URLSearchParams, FormData: EmptyFormData, Date, Number, Set });
  current = fixture(); const first = current;
  window.AdminDocuments.initialize({ querySelector: () => first.page });
  await new Promise(resolve => setImmediate(resolve));
  assert.match(first.filterWarehouses.children[1].textContent, /انبار 1/);
  first.page.isConnected = false;
  current = fixture(); const second = current;
  window.AdminDocuments.initialize({ querySelector: () => second.page });
  await new Promise(resolve => setImmediate(resolve));
  assert.equal(version, 2);
  assert.match(second.filterWarehouses.children[1].textContent, /انبار 2/);
});


test("a late document detail cannot overwrite a more recently opened editor", async () => {
  let source = await readFile(new URL("../../Shop.Admin/wwwroot/js/admin-documents.js", import.meta.url), "utf8");
  source = source.replace("Object.freeze({ initialize })", "Object.freeze({ initialize, openEditor, setLookups(value) { lookups = value; }, setModal(value) { modal = value; } })");
  const pending = new Map(), messages = [];
  const field = () => ({ value: "", append() {}, replaceChildren() {} });
  const elements = Object.fromEntries(["id", "status", "documentDate", "externalReference", "warehouseId", "documentTypeId", "partyId", "salesPlanId", "reason"].map(key => [key, field()]));
  const form = { elements, reset() {}, querySelector: () => ({ replaceChildren() {} }), querySelectorAll: () => [] };
  const document = { querySelector: () => form, getElementById: () => ({}), createElement: () => field() };
  const window = { AdminUI: { getJson(url) { return new Promise((resolve, reject) => pending.set(url, { resolve, reject })); }, notify(message) { messages.push(message); } } };
  vm.runInNewContext(source, { window, document, Intl, WeakSet, URLSearchParams, Date, Number, Set });
  window.AdminDocuments.setLookups({ ownerLinks: [], plans: [], warehouses: [] });
  let shows = 0; window.AdminDocuments.setModal({ show() { shows++; } });
  const page = { isConnected: true, dataset: { api: "/documents" } };
  const first = window.AdminDocuments.openEditor(page, 1);
  const second = window.AdminDocuments.openEditor(page, 2);
  pending.get("/documents/2").resolve({ id: 2, warehouseId: 1, lines: [], date: "2026-11-09", status: "D" });
  await second;
  pending.get("/documents/1").resolve({ id: 1, warehouseId: 1, lines: [], date: "2026-11-09", status: "D" });
  await first;
  assert.equal(form.elements.id.value, "2");
  assert.equal(shows, 1);
  assert.deepEqual(messages, []);
});
