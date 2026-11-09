(() => {
    "use strict";
    const ui = window.AdminUI, initialized = new WeakSet(), number = new Intl.NumberFormat("fa-IR");
    if (!ui) return;
    let pageNumber = 1;
    const el = (tag, text, cls) => { const node = document.createElement(tag); if (text != null) node.textContent = text; if (cls) node.className = cls; return node; };
    const statusText = value => ({ pending: "در انتظار بررسی", approved: "تأییدشده", rejected: "ردشده" })[value] || value;
    function setStatus(page, message, error = false) { const target = page.querySelector("[data-feedback-status]"); target.textContent = message; target.classList.toggle("is-error", error); target.classList.toggle("d-none", !message); }
    function token() { return document.querySelector('[data-feedback-form] [name="__RequestVerificationToken"]')?.value || ""; }
    async function request(url, method = "GET", body) { return ui.getJson(url, { method, headers: body ? { "Content-Type": "application/json", RequestVerificationToken: token() } : {}, ...(body ? { body: JSON.stringify(body) } : {}) }); }
    function openModal(item, page) {
        const form = document.querySelector("[data-feedback-form]"); form.elements.id.value = item.id;
        form.elements.status.value = item.status; form.elements.adminReply.value = item.adminReply || "";
        document.querySelector("[data-feedback-text]").textContent = `${item.displayName}${item.isSample ? " (نمونه)" : ""} · ${item.storeProductKey}\n${item.feedbackText}`;
        form.elements.status.disabled = page.dataset.canModerate !== "true";
        form.elements.adminReply.disabled = page.dataset.canModerate !== "true";
        form.querySelector("[data-feedback-save]").hidden = page.dataset.canModerate !== "true";
        window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("admin-feedback-modal")).show();
    }
    function render(page, items) {
        const body = page.querySelector("[data-feedback-rows]"); body.replaceChildren();
        for (const item of items) {
            const row = document.createElement("tr");
            [item.isSample ? "—" : new Date(item.createdAtUtc).toLocaleDateString("fa-IR"), item.storeProductKey, item.isSample ? `${item.displayName} (نمونه)` : item.displayName, item.feedbackText,
                item.rating == null ? "—" : number.format(item.rating), statusText(item.status), item.adminReply || "—"].forEach(value => row.append(el("td", value)));
            const action = el("td"); const button = el("button", "بررسی", "btn btn-sm btn-default"); button.type = "button"; button.addEventListener("click", () => openModal(item, page)); action.append(button); row.append(action); body.append(row);
        }
    }
    async function load(page) {
        const query = new URLSearchParams(new FormData(page.querySelector("[data-feedback-filter]"))); query.set("page", String(pageNumber));
        setStatus(page, "در حال دریافت بازخوردها…");
        try {
            const result = await request(`${page.dataset.api}?${query}`); render(page, result.items || []);
            setStatus(page, result.items?.length ? "" : "موردی با این فیلتر پیدا نشد.");
            page.querySelector("[data-feedback-page-label]").textContent = `صفحهٔ ${number.format(pageNumber)} · ${number.format(result.total || 0)} مورد`;
            page.querySelector('[data-feedback-page="-1"]').disabled = pageNumber <= 1;
            page.querySelector('[data-feedback-page="1"]').disabled = pageNumber * result.pageSize >= result.total;
        } catch (error) { setStatus(page, error.message || "دریافت بازخوردها ناموفق بود.", true); }
    }
    function initialize(root) {
        const page = root.querySelector?.("[data-admin-feedback]"); if (!page || initialized.has(page)) return; initialized.add(page); pageNumber = 1;
        page.querySelector("[data-feedback-filter]").addEventListener("submit", event => { event.preventDefault(); pageNumber = 1; void load(page); });
        page.querySelector("[data-feedback-filter]").addEventListener("reset", () => { pageNumber = 1; setTimeout(() => void load(page), 0); });
        page.querySelectorAll("[data-feedback-page]").forEach(button => button.addEventListener("click", () => { pageNumber = Math.max(1, pageNumber + Number(button.dataset.feedbackPage)); void load(page); }));
        document.querySelector("[data-feedback-form]").addEventListener("submit", async event => {
            event.preventDefault(); const form = event.currentTarget, id = form.elements.id.value;
            try {
                const result = await request(`${page.dataset.api}/${id}`, "PUT", { status: form.elements.status.value, adminReply: form.elements.adminReply.value || null });
                ui.notify(result.message, "success"); window.bootstrap?.Modal?.getInstance(document.getElementById("admin-feedback-modal"))?.hide(); await load(page);
            } catch (error) { ui.notify(error.message || "ذخیرهٔ بررسی ناموفق بود.", "danger"); }
        });
        void load(page);
    }
    window.AdminFeedback = Object.freeze({ initialize });
})();
