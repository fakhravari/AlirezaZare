import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import vm from "node:vm";

test("feedback keeps the latest filter result when requests finish out of order", async () => {
  const source = await readFile(new URL("../../Admin/wwwroot/js/admin-feedback.js", import.meta.url), "utf8");
  const requests = [];
  const listeners = {};
  const element = () => ({ textContent: "", classList: { toggle() {}, add() {}, remove() {} }, append(...children) { (this.children ||= []).push(...children); }, replaceChildren() { this.children = []; }, addEventListener() {}, dataset: {} });
  const rows = element(), status = element(), label = element(), previous = element(), next = element();
  const filter = { addEventListener(type, handler) { listeners[type] = handler; } };
  const form = { addEventListener() {} };
  const map = { "[data-feedback-filter]": filter, "[data-feedback-status]": status, "[data-feedback-rows]": rows, "[data-feedback-page-label]": label, '[data-feedback-page="-1"]': previous, '[data-feedback-page="1"]': next };
  const page = { dataset: { api: "/feedback" }, querySelector: selector => map[selector], querySelectorAll: () => [] };
  const window = { AdminUI: { getJson: () => new Promise(resolve => requests.push(resolve)) } };
  const document = { createElement: element, querySelector: selector => selector === "[data-feedback-form]" ? form : null };
  class EmptyFormData { *[Symbol.iterator]() {} }
  vm.runInNewContext(source, { window, document, Intl, URLSearchParams, FormData: EmptyFormData, WeakSet, setTimeout });
  window.AdminFeedback.initialize({ querySelector: () => page });
  listeners.submit({ preventDefault() {} });
  const result = name => ({ items: [{ productName: name, storeProductKey: "1", displayName: "کاربر", feedbackText: "متن", status: "pending", isSample: true }], total: 1, pageSize: 50 });
  requests[1](result("جدید"));
  await new Promise(resolve => setImmediate(resolve));
  requests[0](result("قدیمی"));
  await new Promise(resolve => setImmediate(resolve));
  assert.match(rows.children[0].children[1].textContent, /جدید/);
  assert.equal(rows.children.length, 1);
});
