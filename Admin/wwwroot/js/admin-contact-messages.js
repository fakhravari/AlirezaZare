(() => {
    "use strict";
    const ui = window.AdminUI, initialized = new WeakSet(), number = new Intl.NumberFormat("fa-IR");
    if (!ui) return;
    let pageNumber = 1, serial = 0;
    const labels = { order: "پیگیری سفارش", consultation: "مشاوره پیش از خرید", "after-sales": "خدمات پس از فروش", business: "همکاری سازمانی", other: "سایر درخواست‌ها" };
    const states = { new: "جدید", read: "خوانده‌شده", closed: "بسته‌شده" };
    const el = (tag, value, cls) => { const node = document.createElement(tag); if (value != null) node.textContent = value; if (cls) node.className = cls; return node; };
    function message(page, value, error = false) { const target = page.querySelector("[data-contact-status]"); target.textContent = value; target.classList.toggle("text-danger", error); target.classList.toggle("text-muted", !error); }
    function token() { return document.querySelector('[data-admin-contact-messages] input[name="__RequestVerificationToken"]')?.value || ""; }
    async function load(page) {
        const requestId = ++serial, query = new URLSearchParams(new FormData(page.querySelector("[data-contact-filter]"))); query.set("page", String(pageNumber));
        message(page, "در حال دریافت پیام‌ها…");
        try {
            const result = await ui.getJson(`${page.dataset.api}?${query}`); if (requestId !== serial) return;
            const body = page.querySelector("[data-contact-rows]"); body.replaceChildren();
            for (const item of result.items || []) {
                const row = document.createElement("tr");
                [new Date(item.createdAtUtc).toLocaleString("fa-IR"), item.name, item.phone, labels[item.subjectKey] || item.subjectKey, item.message.length > 100 ? `${item.message.slice(0, 100)}…` : item.message, states[item.status] || item.status].forEach(value => row.append(el("td", value)));
                const actions = el("td"), detail = el("button", "نمایش", "btn btn-sm btn-default me-1"); detail.type = "button";
                detail.addEventListener("click", () => { page.querySelector("[data-contact-sender]").textContent = `${item.name} · ${item.phone} · ${labels[item.subjectKey] || item.subjectKey}`; page.querySelector("[data-contact-body]").textContent = item.message; window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("contact-message-modal")).show(); });
                const next = item.status === "new" ? "read" : item.status === "read" ? "closed" : "new";
                const update = el("button", next === "read" ? "خوانده شد" : next === "closed" ? "بستن پیام" : "بازگشایی", "btn btn-sm btn-primary"); update.type = "button";
                update.addEventListener("click", async () => { update.disabled = true; try { const result = await ui.getJson(`${page.dataset.api}/${item.id}/status`, { method: "PUT", headers: { "Content-Type": "application/json", RequestVerificationToken: token() }, body: JSON.stringify({ status: next }) }); ui.notify(result.message, "success"); await load(page); } catch (error) { ui.notify(error.message || "ذخیرهٔ وضعیت ناموفق بود.", "danger"); update.disabled = false; } });
                actions.append(detail, update); row.append(actions); body.append(row);
            }
            page.querySelector("[data-contact-count]").textContent = `${number.format(result.total || 0)} پیام`;
            page.querySelector("[data-contact-empty]").classList.toggle("d-none", Boolean(result.items?.length));
            message(page, result.items?.length ? "" : "پیامی با این فیلتر پیدا نشد.");
            page.querySelector("[data-contact-page]").textContent = `صفحهٔ ${number.format(pageNumber)}`;
            page.querySelector('[data-contact-step="-1"]').disabled = pageNumber <= 1;
            page.querySelector('[data-contact-step="1"]').disabled = pageNumber * result.pageSize >= result.total;
            page.querySelector("[data-contact-pagination]").hidden = result.total <= result.pageSize;
        } catch (error) { if (requestId === serial) message(page, error.message || "دریافت پیام‌ها ناموفق بود.", true); }
    }
    function initialize(root) {
        const page = root.querySelector?.("[data-admin-contact-messages]"); if (!page || initialized.has(page)) return; initialized.add(page);
        page.querySelector("[data-contact-filter]").addEventListener("submit", event => { event.preventDefault(); pageNumber = 1; void load(page); });
        page.querySelectorAll("[data-contact-step]").forEach(button => button.addEventListener("click", () => { pageNumber = Math.max(1, pageNumber + Number(button.dataset.contactStep)); void load(page); }));
        page.querySelector("[data-contact-refresh]").addEventListener("click", () => void load(page)); void load(page);
    }
    window.AdminContactMessages = Object.freeze({ initialize });
})();
