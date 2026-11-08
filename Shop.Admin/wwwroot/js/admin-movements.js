(() => {
    "use strict";
    const ui = window.AdminUI, initialized = new WeakSet(), number = new Intl.NumberFormat("fa-IR");
    if (!ui) return;
    const auditLabels = { Created: "ایجاد", Updated: "ویرایش", Deleted: "حذف" };
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
    function renderAudit(page, rows) {
        const body = page.querySelector("[data-movement-rows]"); body.replaceChildren();
        for (const item of rows) {
            const row = document.createElement("tr");
            [new Date(item.occurredAtUtc).toLocaleString("fa-IR"), number.format(item.documentNumber),
                auditLabels[item.action] || item.action, item.warehouse, item.actorUserKey, item.reason || "—"].forEach(value => row.append(el("td", value)));
            const detailsCell = el("td"), details = document.createElement("details"), summary = el("summary", "نمایش قبل و بعد");
            details.append(summary);
            for (const [label, json] of [["قبل", item.beforeJson], ["بعد", item.afterJson]]) {
                if (!json) continue;
                const heading = el("strong", label), pre = document.createElement("pre");
                pre.className = "small text-wrap mt-2";
                try { pre.textContent = JSON.stringify(JSON.parse(json), null, 2); } catch { pre.textContent = json; }
                details.append(heading, pre);
            }
            detailsCell.append(details); row.append(detailsCell); body.append(row);
        }
    }
    async function load(page) {
        const query = new URLSearchParams(new FormData(page.querySelector("[data-movement-filter]"))), current = Number(page.dataset.page || 1); query.set("page", current);
        if (page.dataset.mode === "audit") query.set("audit", "true");
        try {
            const result = await get(`${page.dataset.api}?${query}`);
            const rows = page.dataset.mode === "audit" ? result.items || [] : result;
            if (page.dataset.mode === "audit") renderAudit(page, rows); else render(page, rows);
            status(page, rows.length ? "" : page.dataset.mode === "audit" ? "تغییری برای این فیلتر ثبت نشده است." : "گردشی برای این فیلتر پیدا نشد.");
            page.querySelector("[data-movement-page-label]").textContent = `صفحهٔ ${number.format(current)}`;
            page.querySelector('[data-movement-page="-1"]').disabled = current <= 1;
            page.querySelector('[data-movement-page="1"]').disabled = rows.length < 100 || (page.dataset.mode === "audit" && current * 100 >= result.total);
        } catch (error) { status(page, error.message || "دریافت گردش‌ها ناموفق بود.", true); }
    }
    async function initialize(root) {
        const page = root.querySelector?.("[data-admin-movements]"); if (!page || initialized.has(page)) return; initialized.add(page);
        try {
            const warehouses = await get("/api/admin/warehouse/warehouses"), select = page.querySelector("[name=warehouseId]");
            for (const warehouse of warehouses) { const option = el("option", `${warehouse.name} (${warehouse.code})`); option.value = warehouse.id; select.append(option); }
            page.dataset.mode = "stock";
            page.querySelectorAll("[data-movement-mode]").forEach(button => button.addEventListener("click", () => {
                page.dataset.mode = button.dataset.movementMode;
                page.dataset.page = "1";
                page.querySelector("[data-movement-title]").textContent = page.dataset.mode === "audit" ? "تاریخچهٔ تغییر سندها" : "گردش اسناد و موجودی";
                page.querySelector("[data-movement-head]").innerHTML = page.dataset.mode === "audit"
                    ? "<tr><th>زمان</th><th>شمارهٔ سند</th><th>عملیات</th><th>انبار</th><th>کاربر سیستم</th><th>دلیل</th><th>جزئیات</th></tr>"
                    : "<tr><th>تاریخ</th><th>شماره</th><th>نوع سند</th><th>انبار</th><th>طرح</th><th>کالا / تنوع</th><th>مقدار سند</th><th>مقدار گردش</th><th>وضعیت</th><th>دلیل</th></tr>";
                page.querySelectorAll("[data-movement-mode]").forEach(item => {
                    const selected = item === button;
                    item.classList.toggle("btn-primary", selected);
                    item.classList.toggle("btn-default", !selected);
                    item.setAttribute("aria-pressed", String(selected));
                });
                void load(page);
            }));
            page.querySelector("[data-movement-filter]").addEventListener("submit", event => { event.preventDefault(); page.dataset.page = "1"; void load(page); });
            page.querySelector("[data-movement-filter]").addEventListener("reset", () => { page.dataset.page = "1"; setTimeout(() => void load(page), 0); });
            page.querySelectorAll("[data-movement-page]").forEach(button => button.addEventListener("click", () => { page.dataset.page = String(Math.max(1, Number(page.dataset.page || 1) + Number(button.dataset.movementPage))); void load(page); }));
            await load(page);
        } catch (error) { status(page, error.message || "بارگذاری فیلترها ناموفق بود.", true); }
    }
    window.AdminMovements = Object.freeze({ initialize });
})();
