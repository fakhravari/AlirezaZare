(() => {
    "use strict";
    const ui = window.AdminUI, initialized = new WeakSet(), number = new Intl.NumberFormat("fa-IR");
    if (!ui) return;
    const el = (tag, text) => { const item = document.createElement(tag); if (text != null) item.textContent = text; return item; };
    async function get(url) { return ui.getJson(url, { cache: "no-store" }); }
    function status(page, message, error = false) { const node = page.querySelector("[data-movement-status]"); node.textContent = message; node.classList.toggle("is-error", error); node.classList.toggle("d-none", !message); }
    function render(page, rows) {
        const body = page.querySelector("[data-movement-rows]"); body.replaceChildren();
        for (const item of rows) {
            const row = document.createElement("tr");
            const product = item.variantNameSnapshot ? `${item.productNameSnapshot} — ${item.variantNameSnapshot}` : item.productNameSnapshot;
            const quantity = `${number.format(item.quantity)} ${item.unitNameSnapshot} × ${number.format(item.conversionFactor)} = ${number.format(item.baseQuantity)}`;
            const delta = item.affectsStock ? number.format(item.quantityDelta) : "بدون اثر بر موجودی";
            [item.documentDate, number.format(item.documentNumber), item.documentType, item.warehouse, item.salesPlan, product, quantity, delta, item.status === "F" ? "قطعی" : "موقت", item.reason].forEach(value => row.append(el("td", value || "—")));
            body.append(row);
        }
    }
    async function load(page) {
        const query = new URLSearchParams(new FormData(page.querySelector("[data-movement-filter]"))), current = Number(page.dataset.page || 1); query.set("page", current);
        try {
            const rows = await get(`${page.dataset.api}?${query}`); render(page, rows); status(page, rows.length ? "" : "گردشی برای این فیلتر پیدا نشد.");
            page.querySelector("[data-movement-page-label]").textContent = `صفحهٔ ${number.format(current)}`;
            page.querySelector('[data-movement-page="-1"]').disabled = current <= 1;
            page.querySelector('[data-movement-page="1"]').disabled = rows.length < 100;
        } catch (error) { status(page, error.message || "دریافت گردش‌ها ناموفق بود.", true); }
    }
    async function initialize(root) {
        const page = root.querySelector?.("[data-admin-movements]"); if (!page || initialized.has(page)) return; initialized.add(page);
        try {
            const warehouses = await get("/api/admin/warehouse/warehouses"), select = page.querySelector("[name=warehouseId]");
            for (const warehouse of warehouses) { const option = el("option", `${warehouse.name} (${warehouse.code})`); option.value = warehouse.id; select.append(option); }
            page.querySelector("[data-movement-filter]").addEventListener("submit", event => { event.preventDefault(); page.dataset.page = "1"; void load(page); });
            page.querySelector("[data-movement-filter]").addEventListener("reset", () => { page.dataset.page = "1"; setTimeout(() => void load(page), 0); });
            page.querySelectorAll("[data-movement-page]").forEach(button => button.addEventListener("click", () => { page.dataset.page = String(Math.max(1, Number(page.dataset.page || 1) + Number(button.dataset.movementPage))); void load(page); }));
            await load(page);
        } catch (error) { status(page, error.message || "بارگذاری فیلترها ناموفق بود.", true); }
    }
    window.AdminMovements = Object.freeze({ initialize });
})();
