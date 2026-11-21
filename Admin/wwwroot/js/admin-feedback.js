(() => {
    "use strict";
    const ui = window.AdminUI, initialized = new WeakSet(), number = new Intl.NumberFormat("fa-IR");
    if (!ui) return;
    let pageNumber = 1, requestSerial = 0;
    const el = ui.createElement;
    const statusText = value => ({ pending: "در انتظار بررسی", approved: "تأییدشده", rejected: "ردشده" })[value] || value;
    function setStatus(page, message, error = false) { ui.setStatus(page.querySelector("[data-feedback-status]"), message, error, { compact: true, hideEmpty: true }); }
    function token() { return document.querySelector('[data-feedback-form] [name="__RequestVerificationToken"]')?.value || ""; }
    async function request(url, method = "GET", body) { return ui.getJson(url, { method, headers: body ? { "Content-Type": "application/json", RequestVerificationToken: token() } : {}, ...(body ? { body: JSON.stringify(body) } : {}) }); }
    function openModal(item, page) {
        const form = document.querySelector("[data-feedback-form]"); form.elements.id.value = item.id;
        form.elements.status.value = item.status; window.AdminRichEditor?.setValue(form.elements.adminReply, item.adminReply || "");
        document.querySelector("[data-feedback-text]").textContent = `${item.displayName}${item.isSample ? " (نمونه)" : ""} · ${item.productName} (${item.storeProductKey})\n${item.feedbackText}`;
        form.elements.status.disabled = page.dataset.canModerate !== "true";
        form.elements.adminReply.disabled = page.dataset.canModerate !== "true";
        form.querySelector("[data-feedback-save]").hidden = page.dataset.canModerate !== "true";
        window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("admin-feedback-modal")).show();
    }
    function render(page, items) {
        const body = page.querySelector("[data-feedback-rows]"); ui.destroyDataTable(page.querySelector("[data-admin-datatable]")); body.replaceChildren();
        for (const item of items) {
            const row = document.createElement("tr");
            [item.isSample ? "—" : window.AdminUI.formatDateTime(item.createdAtUtc), `${item.productName} (${item.storeProductKey})`, item.isSample ? `${item.displayName} (نمونه)` : item.displayName, item.feedbackText,
                item.rating == null ? "—" : number.format(item.rating), statusText(item.status)].forEach(value => row.append(el("td", value)));
            const reply = el("td");
            reply.innerHTML = item.adminReply || "—";
            row.append(reply);
            const action = el("td"); const button = el("button", "بررسی", "btn btn-sm btn-default"); button.type = "button"; button.addEventListener("click", () => openModal(item, page)); action.append(button); row.append(action); body.append(row);
        }
        ui.refreshDataTable(page.querySelector("[data-admin-datatable]"));
    }
    async function load(page) {
        const serial = ++requestSerial;
        const query = new URLSearchParams(new FormData(page.querySelector("[data-feedback-filter]"))); query.set("page", String(pageNumber));
        setStatus(page, "در حال دریافت بازخوردها…");
        try {
            const result = await request(`${page.dataset.api}?${query}`); if (serial !== requestSerial) return; render(page, result.items || []);
            setStatus(page, result.items?.length ? "" : "موردی با این فیلتر پیدا نشد.");
            ui.renderNumberedPagination(page.querySelector("[data-feedback-pagination]"), pageNumber,
                Math.ceil((result.total || 0) / (result.pageSize || 50)), target => { pageNumber = target; void load(page); }, "صفحه‌بندی بازخوردها");
        } catch (error) { if (serial === requestSerial) setStatus(page, error.message || "دریافت بازخوردها ناموفق بود.", true); }
    }
    function initialize(root) {
        const page = root.querySelector?.("[data-admin-feedback]"); if (!page || initialized.has(page)) return; initialized.add(page); pageNumber = 1;
        page.querySelector("[data-feedback-filter]").addEventListener("submit", event => { event.preventDefault(); pageNumber = 1; void load(page); });
        page.querySelector("[data-feedback-filter]").addEventListener("reset", () => { pageNumber = 1; setTimeout(() => void load(page), 0); });
        document.querySelector("[data-feedback-form]").addEventListener("submit", async event => {
            event.preventDefault(); const form = event.currentTarget, id = form.elements.id.value;
            if (form.dataset.saving === "true") return;
            const submit = form.querySelector('[type="submit"]');
            form.dataset.saving = "true"; submit.disabled = true;
            try {
                const adminReply = window.AdminRichEditor?.getValue(form.elements.adminReply) ?? form.elements.adminReply.value;
                const result = await request(`${page.dataset.api}/${id}`, "PUT", { status: form.elements.status.value, adminReply: adminReply || null });
                ui.notify(result.message, "success"); window.bootstrap?.Modal?.getInstance(document.getElementById("admin-feedback-modal"))?.hide(); await load(page);
            } catch (error) { ui.notify(error.message || "ذخیرهٔ بررسی ناموفق بود.", "danger"); }
            finally { form.dataset.saving = "false"; submit.disabled = false; }
        });
        void load(page);
    }
    window.AdminFeedback = Object.freeze({ initialize });
})();
