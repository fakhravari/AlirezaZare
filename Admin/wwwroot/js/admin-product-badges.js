(() => {
    "use strict";
    const initialized = new WeakSet();
    function initialize(container = document) {
    const root = container.matches?.("[data-product-badges]") ? container : container.querySelector("[data-product-badges]"), ui = window.AdminUI;
    if (!root || !ui || initialized.has(root)) return;
    initialized.add(root);
    const form = root.querySelector("[data-badge-form]"), search = root.querySelector("[data-badge-search]");
    const token = root.querySelector('input[name="__RequestVerificationToken"]').value;
    const api = "/api/admin/store/product-badges";
    let badges = [], page = 1, total = 0, query = "", badgeFilter = "", request = 0;
    const options = method => ({ method, headers: { "Content-Type": "application/json", RequestVerificationToken: token } });
    const button = (label, action) => { const b = document.createElement("button"); b.type = "button"; b.className = "btn btn-default btn-sm me-1"; b.textContent = label; b.addEventListener("click", action); return b; };
    const cell = (row, value) => { const td = document.createElement("td"); td.textContent = value; row.append(td); return td; };
    const badgeTextColor = color => {
        const channels = [1, 3, 5].map(index => parseInt(color.slice(index, index + 2), 16) / 255).map(value => value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4);
        return 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2] > 0.179 ? "#000000" : "#ffffff";
    };
    const error = e => { const message = e.message || "عملیات ناموفق بود."; root.querySelector("[data-badge-status]").textContent = message; ui.notify(message, "danger"); };
    async function loadBadges() {
        badges = await ui.getJson(api);
        const filter = search.elements.badgeFilter;
        const selectedFilter = filter.value;
        filter.replaceChildren(new Option("همهٔ نشان‌ها", ""), new Option("بدون نشان", "none"), ...badges.map(badge => new Option(badge.title, String(badge.id))));
        filter.value = selectedFilter;
        if (filter.value !== selectedFilter) { filter.value = ""; badgeFilter = ""; page = 1; }
        const body = root.querySelector("[data-badge-rows]"); body.replaceChildren();
        for (const badge of badges) {
            const row = body.insertRow(); cell(row, badge.title);
            const colorCell = cell(row, ""), preview = document.createElement("span");
            const color = /^#[0-9a-f]{6}$/i.test(badge.backgroundColor || "") ? badge.backgroundColor : "#102a43";
            const textColor = /^#[0-9a-f]{6}$/i.test(badge.textColor || "") ? badge.textColor : badgeTextColor(color);
            const borderColor = /^#[0-9a-f]{6}$/i.test(badge.borderColor || "") ? badge.borderColor : badgeTextColor(color);
            preview.className = "badge"; preview.style.backgroundColor = color; preview.style.color = textColor; preview.style.border = `1px solid ${borderColor}`; preview.textContent = badge.title; colorCell.append(preview);
            cell(row, String(badge.productCount));
            const actions = cell(row, "");
            actions.append(button("ویرایش", () => { form.elements.id.value = badge.id; form.elements.title.value = badge.title; form.elements.backgroundColor.value = color; form.elements.textColor.value = textColor; form.elements.borderColor.value = borderColor; form.elements.title.focus(); }));
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
        if (badgeFilter === "none") url.searchParams.set("withoutBadge", "true");
        else if (badgeFilter) url.searchParams.set("badgeId", badgeFilter);
        const result = await ui.getJson(url); if (serial !== request) return;
        total = result.total; const body = root.querySelector("[data-badge-products]"); body.replaceChildren();
        for (const product of result.items) {
            const row = body.insertRow(); cell(row, product.code); cell(row, product.name);
            const select = document.createElement("select"); select.className = "form-select"; select.setAttribute("aria-label", `نشان ${product.name}`);
            const placeholder = new Option("انتخاب نشان", ""); placeholder.disabled = true; select.add(placeholder);
            for (const badge of badges) select.add(new Option(badge.title, String(badge.id)));
            select.value = product.idBadge == null ? "" : String(product.idBadge); const td = cell(row, ""); td.append(select);
            const actions = cell(row, "");
            const save = button("ذخیره", async () => {
                if (!select.value) return;
                const opts = options("PUT"); opts.body = JSON.stringify({ badgeId: Number(select.value) });
                try { await ui.getJson(`${api}/products/${product.id}`, opts); await refresh(); ui.notify("نشان کالا ذخیره شد.", "success"); } catch (e) { error(e); }
            });
            save.disabled = !select.value;
            select.addEventListener("change", () => { save.disabled = !select.value; });
            actions.append(save);
            if (product.idBadge != null) actions.append(button("حذف نشان", async () => {
                if (!window.confirm(`نشان کالای «${product.name}» حذف شود؟`)) return;
                const opts = options("PUT"); opts.body = JSON.stringify({ badgeId: null });
                try { await ui.getJson(`${api}/products/${product.id}`, opts); await refresh(); ui.notify("نشان از کالا حذف شد.", "success"); } catch (e) { error(e); }
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
        const id = form.elements.id.value, opts = options(id ? "PUT" : "POST"); opts.body = JSON.stringify({ title: form.elements.title.value, backgroundColor: form.elements.backgroundColor.value, textColor: form.elements.textColor.value, borderColor: form.elements.borderColor.value });
        try { await ui.getJson(id ? `${api}/${id}` : api, opts); form.reset(); form.elements.id.value = ""; await refresh(); ui.notify("نشان ذخیره شد.", "success"); } catch (e) { error(e); }
    });
    root.querySelector("[data-badge-reset]").addEventListener("click", () => { form.reset(); form.elements.id.value = ""; });
    search.addEventListener("submit", event => { event.preventDefault(); query = search.elements.q.value.trim(); badgeFilter = search.elements.badgeFilter.value; page = 1; void loadProducts().catch(error); });
    root.querySelector("[data-badge-prev]").addEventListener("click", () => { if (page > 1) { page--; void loadProducts().catch(error); } });
    root.querySelector("[data-badge-next]").addEventListener("click", () => { if (page * 30 < total) { page++; void loadProducts().catch(error); } });
    void refresh().catch(error);
    }
    window.AdminProductBadges = Object.freeze({ initialize });
})();
