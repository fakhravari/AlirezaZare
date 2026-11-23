import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import vm from "node:vm";

test("invoice displays variant and accessory prices and the pre-discount subtotal", async () => {
  const source = (await readFile("Shop/wwwroot/shop/js/pages/account-page.js", "utf8")).replaceAll("\r\n", "\n");
  const start = source.indexOf('document.addEventListener("click", (event) => {\n            const button = event.target.closest("[data-open-order-invoice]");');
  const end = source.indexOf('    invoiceDialog?.querySelector("[data-close-order-invoice]")', start);
  assert.ok(start >= 0 && end > start, "invoice click handler is present");
  const elements = new Map();
  const element = () => ({ textContent: "", hidden: false, children: [], setAttribute() {}, replaceChildren() { this.children = []; }, append(...children) { this.children.push(...children); } });
  const invoiceDialog = { open: false, querySelector(selector) { if (!elements.has(selector)) elements.set(selector, element()); return elements.get(selector); }, showModal() { this.open = true; } };
  const order = {
    dataset: { orderNumber: "1", orderDate: "today", orderStatus: "pending", orderDelivery: "pending", orderCount: "2", orderAmount: "180" },
    _orderDetails: { subtotalAmount: 200, totalAmount: 180, couponCode: "C", couponDiscountAmount: 20,
      items: [{ name: "محصول", variantName: "قرمز", salesPlanName: "طرح ویژه", quantity: 2, unitAmount: 100, priceReferences: [
        { component: "variant", name: "قرمز", priceVersionId: 10, calculatedUnitAmount: 80, priceBeforeDiscountAmount: 100, discountPercent: 20 },
        { component: "accessory", name: "کیف", priceVersionId: 11, calculatedUnitAmount: 20 },
      ] }] },
  };
  let openInvoice;
  const document = { addEventListener(_name, handler) { openInvoice = () => handler({ target: { closest: () => ({ closest: () => order }) } }); }, createElement: element };
  const ui = { formatNumber: String, formatDateTime: String };
  const window = {};
  vm.runInNewContext(await readFile("Shop/wwwroot/shop/js/store/order-price-components.js", "utf8"), { document, window });
  vm.runInNewContext(source.slice(start, end), { document, window, invoiceDialog, ui, formatToman: amount => `${amount} تومان` });
  openInvoice();
  const plan = elements.get("[data-invoice-lines]").children[0];
  assert.equal(plan.children[0].textContent, "طرح فروش: طرح ویژه");
  const row = plan.children[1].children[1].children[0];
  assert.equal(row.children[0].children[0].textContent, "محصول");
  assert.equal(row.children[0].children[1].textContent, "تنوع انتخاب‌شده: قرمز");
  const components = row.children[0].children[2].children;
  assert.equal(components[0].children[0].children[0].textContent, "قیمت تنوع");
  assert.equal(components[0].children[0].children[1].textContent, "80 تومان");
  assert.equal(components[0].children[1].textContent, "مبلغ واحد: 80 تومان × 2 = 160 تومان");
  assert.equal(components[0].children[2].children[0].textContent, "پیش از تخفیف: 100 تومان");
  assert.equal(components[0].children[2].children[1].textContent, "تخفیف 20٪: 20 تومان");
  assert.equal(components[1].children[0].children[0].textContent, "متعلقه: کیف");
  assert.equal(components[1].children[0].children[1].textContent, "20 تومان");
  assert.equal(components[1].children[1].textContent, "مبلغ واحد: 20 تومان × 2 = 40 تومان");
  assert.equal(components[1].children.length, 2);
  assert.equal(row.children[1].textContent, "2");
  assert.equal(row.children[2].textContent, "100 تومان");
  assert.equal(row.children[3].textContent, "200 تومان");
  assert.equal(elements.get("[data-invoice-amount]").textContent, "200 تومان");
  assert.equal(elements.get("[data-invoice-subtotal]").textContent, "200 تومان");
  assert.equal(elements.get("[data-invoice-total]").textContent, "180 تومان");
  assert.equal(elements.get("[data-invoice-coupon-row]").hidden, false);
  assert.equal(elements.get("[data-invoice-coupon-code]").textContent, "C");
  assert.equal(elements.get("[data-invoice-discount]").textContent, "− 20 تومان");
  assert.equal(elements.get("[data-invoice-amount-mismatch]").hidden, true);
  order._orderDetails.couponCode = null;
  order._orderDetails.couponDiscountAmount = 0;
  openInvoice();
  assert.equal(elements.get("[data-invoice-coupon-code]").textContent, "استفاده نشده");
  assert.equal(elements.get("[data-invoice-discount]").textContent, "0 تومان");
  assert.equal(invoiceDialog.open, true);
  order._orderDetails.subtotalAmount = 250;
  openInvoice();
  assert.equal(elements.get("[data-invoice-amount-mismatch]").hidden, false);
  order._orderDetails.subtotalAmount = 200;
  openInvoice();
  assert.equal(elements.get("[data-invoice-amount-mismatch]").hidden, true);
  order._orderDetails.items.push({ name: "محصول دوم", salesPlanId: "regular", salesPlanName: "فروش عادی", quantity: 1, unitAmount: 50 });
  order._orderDetails.items.push({ name: "محصول سوم", salesPlanId: "regular", salesPlanName: "فروش عادی", quantity: 1, unitAmount: 50 });
  openInvoice();
  const groups = elements.get("[data-invoice-lines]").children;
  assert.equal(groups.length, 2);
  assert.equal(groups[1].children[0].textContent, "طرح فروش: فروش عادی");
  assert.equal(groups[1].children[1].children[1].children.length, 2);
});

test("order components use the saved accessory amount", async () => {
  const account = await readFile("Shop/Controllers/AccountController.cs", "utf8");
  const admin = await readFile("Admin/Areas/Store/Controllers/Api/StoreOrdersAdminApiController.cs", "utf8");
  const query = await readFile("Shop/Features/Store/Database/Seeds/order-invoice-details-query.sql", "utf8");
  assert.match(account, /PriceAmount = reference\.UnitAmountSnapshot/);
  assert.match(account, /calculatedUnitAmount = reference\.PriceAmount/);
  assert.match(admin, /PriceAmount = r\.UnitAmountSnapshot/);
  assert.match(admin, /calculatedUnitAmount = r\.PriceAmount/);
  assert.match(query, /reference\.AccessoryNameSnapshot[\s\S]*?reference\.UnitAmountSnapshot/);
});
