import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import vm from "node:vm";

test("order cards aggregate saved amounts by sales plan", async () => {
  const source = await readFile("Shop/wwwroot/shop/js/pages/account-page.js", "utf8");
  const start = source.indexOf("    const renderAccountOrders = (orders) => {");
  const end = source.indexOf("    const hydrateAccount = async", start);
  assert.ok(start >= 0 && end > start);
  const host = { children: [], replaceChildren() { this.children = []; }, append(card) { this.children.push(card); } };
  const total = {};
  const document = {
    querySelector(selector) { return selector === ".profile-order-list" ? host : total; },
    createElement() { return { dataset: {} }; },
    dispatchEvent() {},
  };
  const ui = { formatNumber: String, escapeHtml: value => String(value).replaceAll("<", "&lt;").replaceAll(">", "&gt;") };
  const render = vm.runInNewContext(`${source.slice(start, end)}\nrenderAccountOrders`, { document, ui, formatPersianDate: String, CustomEvent: class {} });
  render([{ id: 1, number: "A", status: "pending-payment", itemCount: 4, subtotalAmount: 500, couponDiscountAmount: 50, totalAmount: 450, items: [
    { salesPlanId: "regular", salesPlanName: "عادی", quantity: 2, unitAmount: 100 },
    { salesPlanId: "regular", salesPlanName: "عادی", quantity: 1, unitAmount: 50 },
    { salesPlanId: "special", salesPlanName: "<ویژه>", quantity: 1, unitAmount: 250 },
  ] }]);
  const html = host.children[0].innerHTML;
  assert.match(html, /جمع اقلام<\/dt><dd>500 تومان/);
  assert.match(html, /تخفیف کد<\/dt><dd>50 تومان/);
  assert.match(html, /مبلغ سفارش<\/dt><dd>450 تومان/);
  assert.match(html, /عادی<\/span><strong>250 تومان<\/strong><small>3 کالا/);
  assert.match(html, /&lt;ویژه&gt;<\/span><strong>250 تومان<\/strong><small>1 کالا/);
  render([{ number: "B", status: "pending-payment", itemCount: 1, totalAmount: 1, items: [{ quantity: 1 }] }]);
  assert.match(host.children[0].innerHTML, /جمع اقلام<\/dt><dd>—/);
  assert.match(host.children[0].innerHTML, /نامشخص<\/span><strong>—/);
});
