(() => {
    "use strict";
    const initialized = new WeakSet();
    const api = "/api/admin/store/paid-orders-dispatch";
    const format = new Intl.NumberFormat("fa-IR");
    function initialize(root) {
        const page = root.querySelector?.("[data-paid-orders-dispatch]");
        if (!page || initialized.has(page)) return;
        initialized.add(page);
        const canDispatch = page.dataset.canDispatch === "true";
        const warehouseQueue = page.dataset.warehouseQueue === "true";
        const form = page.querySelector("[data-dispatch-form]");
        const rows = page.querySelector("[data-dispatch-rows]");
        const status = page.querySelector("[data-dispatch-status]");
        const all = page.querySelector("[data-dispatch-all]");
        const submit = page.querySelector("[data-dispatch-submit]");
        const clearSelection = page.querySelector("[data-dispatch-clear]");
        const pageSize = 50, selectedIds = new Set();
        const checkboxes = () => {
            const table = page.querySelector("[data-admin-datatable]");
            const instance = window.AdminUI.getDataTable(table);
            const rowNodes = instance ? instance.rows().nodes().toArray() : [...rows.rows];
            return rowNodes.flatMap(row => [...row.querySelectorAll('input[type="checkbox"]:not(:disabled)')]);
        };
        let requestVersion = 0, currentPage = 1, totalPages = 1;
        let saving = false;
        const selected = () => [...selectedIds];
        const count = () => {
            const boxes = checkboxes(), checked = boxes.filter(input => input.checked).length;
            all.checked = boxes.length > 0 && checked === boxes.length;
            all.indeterminate = checked > 0 && checked < boxes.length;
            const selectionLimitMessage = selectedIds.size > 200
                ? " · سقف هر حواله ۲۰۰ سفارش است"
                : "";
            page.querySelector("[data-dispatch-count]").textContent =
                `${format.format(selectedIds.size)} سفارش انتخاب‌شده در همهٔ صفحه‌ها${selectionLimitMessage}`;
            submit.disabled = saving || selectedIds.size === 0 || selectedIds.size > 200;
            clearSelection.disabled = selectedIds.size === 0;
        };
        async function load(pageNumber = currentPage) {
            const version = ++requestVersion;
            status.textContent = "در حال دریافت سفارش‌ها…";
            try {
                const url = new URL(api, window.location.origin);
                if (warehouseQueue) url.searchParams.set("warehouseQueue", "true");
                url.searchParams.set("page", String(pageNumber)); url.searchParams.set("pageSize", String(pageSize));
                const result = await window.AdminUI.getJson(url);
                if (version !== requestVersion || !page.isConnected) return;
                totalPages = Math.max(1, Math.ceil((result.total || 0) / pageSize));
                if (pageNumber > totalPages) return void await load(totalPages);
                currentPage = result.page || pageNumber;
                window.AdminUI.destroyDataTable(page.querySelector("[data-admin-datatable]"));
                rows.replaceChildren();
                all.checked = false;
                for (const item of result.items) {
                    const row = document.createElement("tr");
                    const cell = document.createElement("td");
                    const checkbox = document.createElement("input");
                    checkbox.type = "checkbox"; checkbox.value = item.id; checkbox.disabled = !canDispatch; checkbox.checked = selectedIds.has(item.id);
                    if (checkbox.disabled) checkbox.checked = false;
                    checkbox.setAttribute("aria-label", `انتخاب سفارش ${item.orderNumber}`);
                    checkbox.addEventListener("change", () => {
                        if (checkbox.checked) selectedIds.add(item.id);
                        else selectedIds.delete(item.id);
                        count();
                    });
                    cell.append(checkbox);
                    row.append(cell);
                    const paidAt = /(?:Z|[+-]\d\d:\d\d)$/i.test(item.paidAtUtc) ? item.paidAtUtc : `${item.paidAtUtc}Z`;
                    for (const value of [item.orderNumber, item.customerName, format.format(item.itemCount), format.format(item.totalAmount), item.isSample ? "—" : new Date(paidAt).toLocaleString("fa-IR")]) {
                        const column = document.createElement("td"); column.textContent = value; row.append(column);
                    }
                    if (item.isSample) { const label = document.createElement("span"); label.className = "badge bg-secondary ms-2"; label.textContent = "نمونه، بدون پرداخت واقعی"; row.cells[1].append(" ", label); }
                    rows.append(row);
                }
                window.AdminUI.refreshDataTable(page.querySelector("[data-admin-datatable]"));
                const pager = page.querySelector("[data-dispatch-pagination]");
                window.AdminUI.renderNumberedPagination(pager, currentPage, totalPages,
                    target => { void load(target); }, "صفحه‌بندی حوالهٔ سفارش‌ها");
                status.textContent = result.items.length ? "" : "سفارش آمادهٔ حواله وجود ندارد.";
                count();
            } catch (error) { if (version === requestVersion && page.isConnected) status.textContent = error.message; }
        }
        all.addEventListener("change", () => {
            checkboxes().forEach(input => {
                input.checked = all.checked;
                if (all.checked) selectedIds.add(Number(input.value));
                else selectedIds.delete(Number(input.value));
            });
            count();
        });
        clearSelection.addEventListener("click", () => { selectedIds.clear(); void load(currentPage); });
        page.querySelector("[data-dispatch-refresh]").addEventListener("click", () => void load());
        form.addEventListener("submit", async event => {
            event.preventDefault();
            if (saving || !canDispatch) return;
            const orderIds = selected();
            if (!orderIds.length) { status.textContent = "ابتدا سفارش‌ها را انتخاب کنید."; return; }
            saving = true;
            count();
            try {
                const result = await window.AdminUI.getJson(api, { method: "POST", headers: { "Content-Type": "application/json", RequestVerificationToken: form.querySelector('[name="__RequestVerificationToken"]').value }, body: JSON.stringify({ orderIds }) });
                selectedIds.clear(); await load(1);
                status.textContent = `حواله شماره ${format.format(result.documentNumber)} صادر شد.`;
            } catch (error) { status.textContent = error.message; }
            finally { saving = false; count(); }
        });
        all.disabled = !canDispatch;
        void load();
    }
    window.AdminPaidOrdersDispatch = Object.freeze({ initialize });
})();
