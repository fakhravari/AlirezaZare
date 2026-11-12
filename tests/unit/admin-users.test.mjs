import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import vm from "node:vm";

test("user form rejects blank names and prevents duplicate saves", async () => {
  const source = await readFile(new URL("../../Admin/wwwroot/js/admin-users.js", import.meta.url), "utf8");
  const handlers = {}, messages = [], calls = [];
  const submit = { disabled: false };
  const field = value => ({ value, focus() {} });
  const form = {
    dataset: {}, elements: { id: field(""), userName: field("  "), displayName: field("مدیر"), password: { ...field("valid-password"), required: true }, isActive: { checked: true } },
    addEventListener(type, fn) { handlers.user = fn; }, querySelector: () => submit, querySelectorAll: () => []
  };
  const noop = { addEventListener() {}, classList: { add() {} } };
  const page = { dataset: { api: "/users" }, querySelector(selector) { if (selector === "[data-users-status]") return { textContent: "", classList: { add() {}, remove() {} } }; if (selector === "[data-user-form]") return form; return noop; } };
  const window = { AdminUI: { notify(message) { messages.push(message); }, getJson(url, options) { calls.push(options.method); return new Promise(() => {}); } }, bootstrap: { Modal: { getOrCreateInstance: () => ({ hide() {} }) } } };
  const document = { querySelector: selector => selector === "[data-user-form]" ? form : null, getElementById: () => ({}) };
  vm.runInNewContext(source, { window, document, WeakSet, Intl });
  window.AdminUsers.initialize({ querySelector: () => page });
  await handlers.user({ preventDefault() {}, currentTarget: form });
  assert.equal(messages[0], "نام کاربری را وارد کنید.");
  assert.deepEqual(calls, ["GET"]);
  form.elements.userName.value = "operator";
  void handlers.user({ preventDefault() {}, currentTarget: form });
  await handlers.user({ preventDefault() {}, currentTarget: form });
  assert.deepEqual(calls, ["GET", "POST"]);
  assert.equal(submit.disabled, true);
});


test("user actions wait for their data and a detached page ignores a late response", async () => {
  const source = await readFile(new URL("../../Admin/wwwroot/js/admin-users.js", import.meta.url), "utf8");
  const handlers = {}, messages = [];
  let resolve;
  const pending = new Promise(done => { resolve = done; });
  const node = selector => ({ addEventListener(type, fn) { handlers[selector] = fn; }, classList: { remove() {}, add() {} }, textContent: "" });
  const nodes = new Map();
  const page = { isConnected: true, dataset: { api: "/users" }, querySelector(selector) { if (!nodes.has(selector)) nodes.set(selector, node(selector)); return nodes.get(selector); } };
  const form = { addEventListener() {} };
  const document = { querySelector: () => form, getElementById: () => ({}) };
  const window = { AdminUI: { notify(message) { messages.push(message); }, getJson: () => pending }, bootstrap: { Modal: { getOrCreateInstance: () => ({}) } } };
  vm.runInNewContext(source, { window, document, WeakSet, Intl });
  window.AdminUsers.initialize({ querySelector: () => page });
  handlers["[data-user-create]"]();
  await handlers["[data-role-save]"]({ currentTarget: { disabled: false } });
  assert.equal(messages.length, 2);
  page.isConnected = false;
  resolve({ users: [], roles: [], permissions: [], warehouses: [] });
  await new Promise(done => setImmediate(done));
  assert.equal(nodes.has("[data-users-rows]"), false);
});
