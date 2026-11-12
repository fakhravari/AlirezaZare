(() => {
    "use strict";
    const initialized = new WeakSet();
    const api = "/api/admin/store/paid-orders-dispatch";
    const format = new Intl.NumberFormat("fa-IR");
    function initialize(root) {
        const page = root.querySelector?.("[data-paid-orders-dispatch]");
        if (!page || initialized.has(page)) return;
        initialized.add(page);
        const form = page.querySelector("[data-dispatch-form]");
        const rows = page.querySelector("[data-dispatch-rows]");
        const status = page.querySelector("[data-dispatch-status]");
        const all = page.querySelector("[data-dispatch-all]");
        const submit = page.querySelector("[data-dispatch-submit]");
        const checkboxes = () => {
            const table = page.querySelector("[data-admin-datatable]");
            const instance = window.AdminUI.getDataTable(table);
            const rowNodes = instance ? instance.rows().nodes().toArray() : [...rows.rows];
            return rowNodes.flatMap(row => [...row.querySelectorAll('input[type="checkbox"]')]);
        };
        let requestVersion = 0;
        let saving = false;
        const selected = () => checkboxes().filter(input => input.checked).map(input => Number(input.value));
        const count = () => {
            const boxes = checkboxes(), checked = boxes.filter(input => input.checked).length;
            all.checked = boxes.length > 0 && checked === boxes.length;
            all.indeterminate = checked > 0 && checked < boxes.length;
            page.querySelector("[data-dispatch-count]").textContent = `${format.format(checked)} سفارش انتخاب شده`;
        };
        async function load() {
            const version = ++requestVersion;
            status.textContent = "در حال دریافت سفارش‌ها…";
            try {
                const result = await window.AdminUI.getJson(api);
                if (version !== requestVersion || !page.isConnected) return;
                window.AdminUI.destroyDataTable(page.querySelector("[data-admin-datatable]"));
                rows.replaceChildren();
                all.checked = false;
                for (const item of result.items) {
                    const row = document.createElement("tr");
                    const cell = document.createElement("td");
                    const checkbox = document.createElement("input");
                    checkbox.type = "checkbox"; checkbox.value = item.id;
                    checkbox.setAttribute("aria-label", `انتخاب سفارش ${item.orderNumber}`);
                    checkbox.addEventListener("change", count); cell.append(checkbox); row.append(cell);
                    const paidAt = /(?:Z|[+-]\d\d:\d\d)$/i.test(item.paidAtUtc) ? item.paidAtUtc : `${item.paidAtUtc}Z`;
                    for (const value of [item.orderNumber, item.customerName, format.format(item.itemCount), format.format(item.totalAmount), new Date(paidAt).toLocaleString("fa-IR")]) {
                        const column = document.createElement("td"); column.textContent = value; row.append(column);
                    }
                    rows.append(row);
                }
                window.AdminUI.refreshDataTable(page.querySelector("[data-admin-datatable]"));
                status.textContent = result.items.length ? "" : "سفارش آمادهٔ حواله وجود ندارد.";
                count();
            } catch (error) { if (version === requestVersion && page.isConnected) status.textContent = error.message; }
        }
        all.addEventListener("change", () => { checkboxes().forEach(input => { input.checked = all.checked; }); count(); });
        page.querySelector("[data-dispatch-refresh]").addEventListener("click", () => void load());
        form.addEventListener("submit", async event => {
            event.preventDefault();
            if (saving) return;
            const orderIds = selected();
            if (!orderIds.length) { status.textContent = "ابتدا سفارش‌ها را انتخاب کنید."; return; }
            if (!window.confirm(`برای ${format.format(orderIds.length)} سفارش یک حواله فروش قطعی صادر شود؟`)) return;
            saving = true;
            submit.disabled = true;
            try {
                const result = await window.AdminUI.getJson(api, { method: "POST", headers: { "Content-Type": "application/json", RequestVerificationToken: form.querySelector('[name="__RequestVerificationToken"]').value }, body: JSON.stringify({ orderIds }) });
                await load();
                status.textContent = `حواله شماره ${format.format(result.documentNumber)} صادر شد.`;
            } catch (error) { status.textContent = error.message; }
            finally { saving = false; submit.disabled = false; }
        });
        void load();
    }
    window.AdminPaidOrdersDispatch = Object.freeze({ initialize });
})();
