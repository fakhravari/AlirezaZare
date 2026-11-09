import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import vm from "node:vm";

test("a stale stock request cannot overwrite current plan options", async () => {
  const source = await readFile(new URL("../../Shop.Admin/wwwroot/js/admin-data.js", import.meta.url), "utf8");
  const calls = [], listeners = {};
  const node = () => ({ dataset: {}, value: "", classList: { toggle() {} }, replaceChildren(...children) { this.children = children; }, setAttribute() {}, removeAttribute() {} });
  const plans = node(), form = node(), status = node(), rows = node();
  const map = { "[data-admin-filter]": form, "[data-admin-status]": status, "[data-admin-plan-options]": plans, "[data-admin-results]": rows };
  const page = { dataset: { adminPage: "stock", adminApi: "/stock" }, isConnected: true, setAttribute() {}, removeAttribute() {}, querySelector: key => map[key] || null };
  const refresh = { closest: () => page };
  const document = { addEventListener(type, fn) { (listeners[type] ||= []).push(fn); }, querySelector() { return null; } };
  const window = { location: { origin: "http://localhost" }, AdminUI: { formatNumber: String, notify() {}, getJson(url) { return new Promise(resolve => calls.push({ url: String(url), resolve })); } } };
  class EmptyFormData { get() { return ""; } }
  class FakeOption { constructor(text, value) { this.text = text; this.value = value; } }
  vm.runInNewContext(source, { window, document, URL, AbortController, WeakMap, Promise, FormData: EmptyFormData, Option: FakeOption });
  window.AdminPages.initialize({ querySelectorAll: () => [page] });
  listeners.click[0]({ target: { closest: key => key === "[data-admin-refresh]" ? refresh : null } });
  assert.equal(calls.length, 4);
  calls[2].resolve([]); calls[3].resolve([{ id: "new", name: "جدید" }]);
  await new Promise(resolve => setImmediate(resolve));
  calls[0].resolve([]); calls[1].resolve([{ id: "old", name: "قدیمی" }]);
  await new Promise(resolve => setImmediate(resolve));
  assert.deepEqual(plans.children.map(option => option.value), ["", "new"]);
});
