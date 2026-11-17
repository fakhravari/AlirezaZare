(() => {
    "use strict";

    const ui = window.AdminUI;
    const initialized = new WeakSet();
    const number = new Intl.NumberFormat("fa-IR");
    const money = value => `${number.format(Number(value || 0))} تومان`;
    const dateLabel = value => ui.formatDateTime(value);
    const reports = {
        sales: { title: "فروش و سفارش‌ها", columns: [["date", "تاریخ"], ["orderCount", "سفارش پرداخت‌شده"], ["revenue", "فروش خالص (تومان)"], ["discount", "تخفیف (تومان)"]] },
        products: { title: "عملکرد کالاها", columns: [["productName", "کالا"], ["quantity", "تعداد فروش"], ["revenue", "مبلغ فروش (تومان)"], ["orderCount", "تعداد سفارش"], ["stock", "موجودی مجاز"]] },
        inventory: { title: "موجودی و گردش انبار", columns: [["warehouseLabel", "شرکت / انبار"], ["productCode", "کد کالا"], ["productName", "نام کالا"], ["variantName", "تنوع"], ["planName", "طرح"], ["quantity", "موجودی"], ["minimumQuantity", "حداقل"], ["lastMovement", "آخرین گردش"]] },
        customers: { title: "مشتریان", columns: [["customerName", "مشتری"], ["email", "ایمیل"], ["orderCount", "سفارش"], ["revenue", "مبلغ خرید (تومان)"], ["firstPurchase", "اولین خرید"]] }
    };

    function initialize(root) {
        const page = root.querySelector?.("[data-admin-store-reports]");
        if (!page || initialized.has(page)) return;
        initialized.add(page);

        const filter = page.querySelector("[data-report-filter]");
        const fromInput = filter.elements.from;
        const toInput = filter.elements.to;
        const warehouseSelect = filter.elements.warehouseId;
        let type = "sales", pageNumber = 1, serial = 0;

        const setLastDays = days => {
            const today = new Date();
            const start = new Date(today.getFullYear(), today.getMonth(), today.getDate() - days + 1);
            const format = date => `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
            window.AdminPersianDatePicker.setValue(fromInput, format(start));
            window.AdminPersianDatePicker.setValue(toInput, format(today));
        };
        function queryString(extra = {}) {
            const query = new URLSearchParams({ type, from: fromInput.value, to: toInput.value, page: String(pageNumber), pageSize: "50" });
            if (warehouseSelect.value) query.set("warehouseId", warehouseSelect.value);
            for (const [key, value] of Object.entries(extra)) query.set(key, String(value));
            return query;
        }

        function renderSummary(summary = {}) {
            const host = page.querySelector("[data-report-summary]");
            host.replaceChildren();
            const cards = type === "sales" ? [
                ["فروش خالص", money(summary.revenue)], ["سفارش پرداخت‌شده", number.format(summary.orderCount || 0)],
                ["میانگین سفارش", money(summary.averageOrderAmount)], ["تخفیف ثبت‌شده", money(summary.discount)]
            ] : type === "products" ? [
                ["مبلغ فروش کالاها", money(summary.revenue)], ["تعداد کالای فروخته‌شده", number.format(summary.quantity || 0)],
                ["کالای فروخته‌شده", number.format(summary.productCount || 0)], ["تغییر فروش با دورهٔ قبل", `${summary.revenueChangePercent ?? 0}%`]
            ] : type === "inventory" ? [
                ["ردیف موجودی", number.format(summary.rowCount || 0)], ["کمتر از حداقل", number.format(summary.lowStockCount || 0)],
                ["ناموجود", number.format(summary.outOfStockCount || 0)], ["ورودی / خروجی دوره", `${number.format(summary.movementIn || 0)} / ${number.format(summary.movementOut || 0)}`]
            ] : [
                ["خریداران", number.format(summary.buyerCount || 0)], ["خریدار جدید", number.format(summary.newBuyerCount || 0)],
                ["خریدار تکراری", number.format(summary.repeatBuyerCount || 0)], ["مبلغ خرید", money(summary.revenue)]
            ];
            for (const [label, value] of cards) {
                const column = document.createElement("div"); column.className = "col-sm-6 col-xl-3";
                const card = document.createElement("div"); card.className = "border rounded p-3 h-100";
                const heading = document.createElement("div"); heading.className = "small text-muted mb-2"; heading.textContent = label;
                const amount = document.createElement("strong"); amount.className = "fs-5"; amount.textContent = value;
                card.append(heading, amount);
                if (summary.previousRevenue != null && ["sales", "products"].includes(type) && label !== "تغییر فروش با دورهٔ قبل") {
                    const prior = document.createElement("div"); prior.className = "small text-muted mt-2"; prior.textContent = `دورهٔ قبل: ${money(summary.previousRevenue)}`; card.append(prior);
                }
                column.append(card); host.append(column);
            }
        }

        function renderTable(result) {
            const headings = page.querySelector("[data-report-headings]");
            const body = page.querySelector("[data-report-rows]");
            const config = reports[type];
            headings.replaceChildren();
            for (const [, label] of config.columns) { const th = document.createElement("th"); th.scope = "col"; th.textContent = label; headings.append(th); }
            if (type !== "sales") { const th = document.createElement("th"); th.scope = "col"; th.textContent = "جزئیات"; headings.append(th); }
            body.replaceChildren();
            for (const item of result.items || []) {
                const row = document.createElement("tr");
                for (const [key] of config.columns) {
                    const cell = document.createElement("td");
                    const value = item[key];
                    if (["revenue", "discount", "stock", "quantity", "minimumQuantity", "orderCount"].includes(key)) cell.textContent = ["revenue", "discount"].includes(key) ? money(value) : number.format(value || 0);
                    else if (["date", "firstPurchase"].includes(key)) cell.textContent = dateLabel(value);
                    else if (key === "lastMovement") cell.textContent = value ? ui.formatDateTime(value) : "بدون گردش";
                    else cell.textContent = value || "—";
                    row.append(cell);
                }
                if (type !== "sales") {
                    const cell = document.createElement("td");
                    const button = document.createElement("button"); button.type = "button"; button.className = "btn btn-sm btn-default";
                    button.textContent = type === "products" ? "سفارش‌ها" : type === "customers" ? "خریدها" : "گردش‌ها";
                    button.dataset.reportDetail = type === "products" ? "product-orders" : type === "customers" ? "customer-orders" : "inventory-movements";
                    button.dataset.id = String(type === "products" ? item.productId : type === "customers" ? item.customerId : item.inventoryItemId);
                    if (item.warehouseId) button.dataset.warehouseId = String(item.warehouseId);
                    if (item.salesPlanReferenceId) button.dataset.salesPlanReferenceId = String(item.salesPlanReferenceId);
                    cell.append(button); row.append(cell);
                }
                body.append(row);
            }
            const pagination = page.querySelector("[data-report-pagination]");
            const pages = Math.max(1, Math.ceil((result.total || 0) / (result.pageSize || 50)));
            ui.renderNumberedPagination(pagination, pageNumber, pages,
                target => { pageNumber = target; void load(); }, `صفحه‌بندی گزارش ${config.title}`);
        }

        function updateExportLink() {
            page.querySelector("[data-report-export]").href = `${page.dataset.api}/export?${queryString({ pageSize: 5000 }).toString()}`;
        }

        async function load() {
            const requestId = ++serial;
            page.querySelector("[data-report-status]").textContent = "در حال دریافت گزارش…";
            page.querySelector("[data-report-rows]").replaceChildren();
            try {
                const result = await ui.getJson(`${page.dataset.api}?${queryString()}`);
                if (!page.isConnected || requestId !== serial) return;
                renderSummary(result.summary);
                renderTable(result);
                const dateRange = `${dateLabel(fromInput.value)} تا ${dateLabel(toInput.value)}`;
                const paymentWarning = result.summary?.ordersWithoutVerifiedPayment
                    ? ` ${number.format(result.summary.ordersWithoutVerifiedPayment)} سفارش با وضعیت فروش/ارسال، تأیید پرداخت معتبر ندارند و از مبلغ فروش کنار گذاشته شده‌اند.`
                    : "";
                page.querySelector("[data-report-note]").textContent = type === "inventory"
                    ? `موجودی لحظه‌ای است؛ گردش‌ها برای بازهٔ ${dateRange} محاسبه شده‌اند. ${result.warehouseAccess === false ? "برای این حساب انبار مجازی تعریف نشده است." : ""}`
                    : type === "sales" ? `فروش فقط سفارش‌های دارای پرداخت تأییدشده را شامل می‌شود؛ سفارش آزمایشی، پرداخت‌نشده و لغوشده در فروش محاسبه نمی‌شود. مقایسه با دورهٔ بلافاصله قبل و هم‌اندازه انجام شده است.${paymentWarning}`
                        : type === "products" ? "فروش کالاها از اقلام سفارش‌های دارای پرداخت تأییدشده محاسبه می‌شود؛ موجودی فقط انبارهای مجاز این حساب را نشان می‌دهد."
                            : "خریدار جدید یعنی نخستین خرید پرداخت‌شده در بازه؛ خریدار تکراری در همین بازه بیش از یک خرید پرداخت‌شده داشته است.";
                page.querySelector("[data-report-status]").textContent = `${number.format(result.total || 0)} ردیف`;
                updateExportLink();
            } catch (error) {
                if (requestId === serial) page.querySelector("[data-report-status]").textContent = error.message || "دریافت گزارش ناموفق بود.";
            }
        }

        async function showDetails(button) {
            const query = queryString({ detail: button.dataset.reportDetail, id: button.dataset.id });
            if (button.dataset.warehouseId) query.set("warehouseId", button.dataset.warehouseId);
            if (button.dataset.salesPlanReferenceId) query.set("salesPlanReferenceId", button.dataset.salesPlanReferenceId);
            try {
                const result = await ui.getJson(`${page.dataset.api}?${query}`);
                const modal = document.getElementById("admin-record-modal");
                const body = modal?.querySelector("[data-admin-record-body]");
                const heading = modal?.querySelector("#admin-record-title");
                if (!modal || !body || !window.bootstrap?.Modal) return;
                heading.textContent = result.title || "جزئیات گزارش";
                body.replaceChildren();
                const table = document.createElement("table"); table.className = "table table-striped align-middle";
                const thead = document.createElement("thead"), headRow = document.createElement("tr");
                for (const column of result.columns || []) { const th = document.createElement("th"); th.textContent = column.label; headRow.append(th); }
                thead.append(headRow); table.append(thead);
                const tbody = document.createElement("tbody");
                for (const item of result.items || []) {
                    const row = document.createElement("tr");
                    for (const column of result.columns || []) { const td = document.createElement("td"); td.textContent = column.key === "date" ? ui.formatDateTime(item[column.key]) : item[column.key] ?? "—"; row.append(td); }
                    tbody.append(row);
                }
                table.append(tbody); body.append(table);
                window.bootstrap.Modal.getOrCreateInstance(modal).show();
            } catch (error) { ui.notify(error.message || "دریافت جزئیات ناموفق بود.", "danger"); }
        }

        page.querySelector("[data-report-tabs]").addEventListener("click", event => {
            const tab = event.target.closest("[data-report-tab]");
            if (!tab) return;
            type = tab.dataset.reportTab; pageNumber = 1;
            page.querySelectorAll("[data-report-tab]").forEach(item => { const active = item === tab; item.classList.toggle("active", active); item.setAttribute("aria-pressed", String(active)); });
            page.querySelector("[data-report-warehouse-filter]").hidden = type !== "inventory";
            void load();
        });
        filter.addEventListener("submit", event => { event.preventDefault(); pageNumber = 1; void load(); });
        page.querySelector("[data-report-reset]").addEventListener("click", () => { setLastDays(30); pageNumber = 1; void load(); });
        page.querySelector("[data-report-rows]").addEventListener("click", event => { const button = event.target.closest("[data-report-detail]"); if (button) void showDetails(button); });
        setLastDays(30);
        void ui.getJson(`${page.dataset.api}?type=warehouses`).then(result => {
            for (const warehouse of result.items || []) warehouseSelect.add(new Option(warehouse.label, warehouse.id));
            if (result.defaultWarehouseId != null) warehouseSelect.value = String(result.defaultWarehouseId);
            else warehouseSelect.add(new Option("انبار فروش مجاز در دسترس نیست", "", true, true));
            void load();
        }).catch(error => {
            page.querySelector("[data-report-status]").textContent = error.message || "دریافت فهرست انبارها ناموفق بود.";
            void load();
        });
    }

    window.AdminStoreReports = Object.freeze({ initialize });
})();
