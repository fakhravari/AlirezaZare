(() => {
    "use strict";
    const ui = window.AdminUI, initialized = new WeakSet(), number = new Intl.NumberFormat("fa-IR");
    if (!ui) return;
    let pageNumber = 1, serial = 0, customers = [];
    const categoryLabels = { notice: "اطلاعیه", support: "پشتیبانی", orders: "سفارش", products: "محصولات" };
    const el = (tag, value, cls) => { const node = document.createElement(tag); if (value != null) node.textContent = value; if (cls) node.className = cls; return node; };
    const token = page => page.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    function status(page, value, error = false) { const node = page.querySelector("[data-account-message-load-status]"); node.textContent = value; node.classList.toggle("text-danger", error); node.classList.toggle("text-muted", !error); }
    async function load(page) {
        const id = ++serial, params = new URLSearchParams(new FormData(page.querySelector("[data-account-message-filter]"))); params.set("page", String(pageNumber)); status(page, "در حال دریافت پیام‌ها…");
        try {
            const data = await ui.getJson(`${page.dataset.api}?${params}`); if (id !== serial) return;
            const rows = page.querySelector("[data-account-message-rows]"); rows.replaceChildren();
            for (const item of data.items || []) {
                const row = document.createElement("tr");
                [new Date(item.createdAtUtc).toLocaleString("fa-IR"), `${item.firstName} ${item.lastName}`, item.email, item.title, categoryLabels[item.category] || item.category, item.isRead ? "خوانده‌شده" : "خوانده‌نشده", item.body].forEach(value => row.append(el("td", value)));
                rows.append(row);
            }
            page.querySelector("[data-account-message-count]").textContent = `${number.format(data.total || 0)} پیام`;
            status(page, data.items?.length ? "" : "پیامی ثبت نشده است.");
            page.querySelector("[data-account-message-page]").textContent = `صفحهٔ ${number.format(pageNumber)}`;
            page.querySelector('[data-account-message-step="-1"]').disabled = pageNumber <= 1;
            page.querySelector('[data-account-message-step="1"]').disabled = pageNumber * data.pageSize >= data.total;
            page.querySelector("[data-account-message-pagination]").hidden = data.total <= data.pageSize;
        } catch (error) { if (id === serial) status(page, error.message || "دریافت پیام‌ها ناموفق بود.", true); }
    }
    async function searchCustomers(page, query) {
        const list = page.querySelector("#account-message-customers");
        if (query.trim().length < 2) { list.replaceChildren(); customers = []; return; }
        try {
            customers = await ui.getJson(`${page.dataset.api}/customers?q=${encodeURIComponent(query.trim())}`);
            list.replaceChildren(...customers.map(customer => { const option = document.createElement("option"); option.value = `${customer.firstName} ${customer.lastName} · ${customer.email}`; return option; }));
        } catch { customers = []; }
    }
    function initialize(root) {
        const page = root.querySelector?.("[data-admin-account-messages]"); if (!page || initialized.has(page)) return; initialized.add(page);
        const filter = page.querySelector("[data-account-message-filter]"); filter.addEventListener("submit", event => { event.preventDefault(); pageNumber = 1; void load(page); });
        page.querySelector("[data-account-message-refresh]").addEventListener("click", () => void load(page));
        page.querySelectorAll("[data-account-message-step]").forEach(button => button.addEventListener("click", () => { pageNumber = Math.max(1, pageNumber + Number(button.dataset.accountMessageStep)); void load(page); }));
        const form = page.querySelector("[data-account-message-form]"), customerInput = page.querySelector("#account-message-customer");
        let timer; customerInput.addEventListener("input", () => { form.elements.profileId.value = ""; clearTimeout(timer); timer = setTimeout(() => void searchCustomers(page, customerInput.value), 250); });
        customerInput.addEventListener("change", () => { const match = customers.find(item => `${item.firstName} ${item.lastName} · ${item.email}` === customerInput.value); form.elements.profileId.value = match?.id || ""; });
        form.addEventListener("submit", async event => {
            event.preventDefault();
            if (!form.elements.profileId.value) { ui.notify("ابتدا مشتری را از پیشنهادهای جست‌وجو انتخاب کنید.", "danger"); return; }
            const submit = form.querySelector('[type="submit"]'); submit.disabled = true;
            try {
                const result = await ui.getJson(page.dataset.api, { method: "POST", headers: { "Content-Type": "application/json", RequestVerificationToken: token(page) }, body: JSON.stringify({ profileId: Number(form.elements.profileId.value), title: form.elements.title.value, category: form.elements.category.value, body: form.elements.body.value }) });
                ui.notify(result.message, "success"); form.reset(); form.elements.profileId.value = ""; pageNumber = 1; await load(page);
            } catch (error) { ui.notify(error.message || "ثبت پیام ناموفق بود.", "danger"); }
            finally { submit.disabled = false; }
        });
        void load(page);
    }
    window.AdminAccountMessages = Object.freeze({ initialize });
})();
