(() => {
    "use strict";
    const root = document.querySelector("[data-product-badges]"), ui = window.AdminUI;
    if (!root || !ui) return;
    const form = root.querySelector("[data-badge-form]"), search = root.querySelector("[data-badge-search]");
    const token = root.querySelector('input[name="__RequestVerificationToken"]').value;
    const api = "/api/admin/store/product-badges";
    let badges = [], page = 1, total = 0, query = "", request = 0;
    const options = method => ({ method, headers: { "Content-Type": "application/json", RequestVerificationToken: token } });
    const button = (label, action) => { const b = document.createElement("button"); b.type = "button"; b.className = "btn btn-default btn-sm me-1"; b.textContent = label; b.addEventListener("click", action); return b; };
    const cell = (row, value) => { const td = document.createElement("td"); td.textContent = value; row.append(td); return td; };
    const error = e => ui.notify(e.message || "عملیات ناموفق بود.", "danger");
    async function loadBadges() {
        badges = await ui.getJson(api);
        const body = root.querySelector("[data-badge-rows]"); body.replaceChildren();
        for (const badge of badges) {
            const row = body.insertRow(); cell(row, badge.title); cell(row, String(badge.productCount));
            const actions = cell(row, "");
            actions.append(button("ویرایش", () => { form.elements.id.value = badge.id; form.elements.title.value = badge.title; form.elements.title.focus(); }));
            const remove = button("حذف", async () => {
                if (!window.confirm(`نشان «${badge.title}» حذف شود؟`)) return;
                try { await ui.getJson(`${api}/${badge.id}`, options("DELETE")); await refresh(); ui.notify("نشان حذف شد.", "success"); } catch (e) { error(e); }
            });
            remove.disabled = badge.productCount > 0; remove.title = badge.productCount > 0 ? "ابتدا تخصیص نشان را از کالاها بردارید." : "حذف نشان"; actions.append(remove);
        }
    }
    async function loadProducts() {
        const serial = ++request, url = new URL(`${api}/products`, window.location.origin);
        url.searchParams.set("page", page); if (query) url.searchParams.set("q", query);
        const result = await ui.getJson(url); if (serial !== request) return;
        total = result.total; const body = root.querySelector("[data-badge-products]"); body.replaceChildren();
        for (const product of result.items) {
            const row = body.insertRow(); cell(row, product.code); cell(row, product.name);
            const select = document.createElement("select"); select.className = "form-select"; select.setAttribute("aria-label", `نشان ${product.name}`);
            select.add(new Option("بدون نشان", "")); for (const badge of badges) select.add(new Option(badge.title, String(badge.id)));
            select.value = product.idProductBadge == null ? "" : String(product.idProductBadge); const td = cell(row, ""); td.append(select);
            cell(row, "").append(button("ذخیره", async () => {
                const opts = options("PUT"); opts.body = JSON.stringify({ badgeId: select.value ? Number(select.value) : null });
                try { await ui.getJson(`${api}/products/${product.id}`, opts); await refresh(); ui.notify("نشان کالا ذخیره شد.", "success"); } catch (e) { error(e); }
            }));
        }
        root.querySelector("[data-badge-status]").textContent = `${total.toLocaleString("fa-IR")} کالا`;
        root.querySelector("[data-badge-page]").textContent = `صفحه ${page.toLocaleString("fa-IR")}`;
        root.querySelector("[data-badge-prev]").disabled = page <= 1;
        root.querySelector("[data-badge-next]").disabled = page * result.pageSize >= total;
    }
    async function refresh() { await loadBadges(); await loadProducts(); }
    form.addEventListener("submit", async event => {
        event.preventDefault(); if (!form.reportValidity()) return;
        const id = form.elements.id.value, opts = options(id ? "PUT" : "POST"); opts.body = JSON.stringify({ title: form.elements.title.value });
        try { await ui.getJson(id ? `${api}/${id}` : api, opts); form.reset(); form.elements.id.value = ""; await refresh(); ui.notify("نشان ذخیره شد.", "success"); } catch (e) { error(e); }
    });
    root.querySelector("[data-badge-reset]").addEventListener("click", () => { form.reset(); form.elements.id.value = ""; });
    search.addEventListener("submit", event => { event.preventDefault(); query = search.elements.q.value.trim(); page = 1; void loadProducts().catch(error); });
    root.querySelector("[data-badge-prev]").addEventListener("click", () => { if (page > 1) { page--; void loadProducts().catch(error); } });
    root.querySelector("[data-badge-next]").addEventListener("click", () => { if (page * 30 < total) { page++; void loadProducts().catch(error); } });
    void refresh().catch(error);
})();
