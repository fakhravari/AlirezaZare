(() => {
    "use strict";
    const initialized = new WeakSet(), ui = window.AdminUI, api = "/api/admin/store/geography";
    function initialize(root) {
        const page = root.querySelector("[data-geography]");
        if (!page || initialized.has(page)) return;
        initialized.add(page);
        const form = page.querySelector("[data-geo-form]"), status = page.querySelector("[data-geo-status]"), rows = page.querySelector("[data-geo-rows]"), search = page.querySelector("[data-geo-search]");
        let data = { countries: [], provinces: [], cities: [] }, kind = "countries", provinceCountryFilter = "", cityProvinceFilter = "", busy = true, available = false;
        const tabs = [...page.querySelectorAll("[data-geo-tab]")], submit = form.querySelector('[type="submit"]'), retry = page.querySelector("[data-geo-retry]");
        submit.disabled = true;
        const request = (url, method = "GET", body) => ui.getJson(url, { method, headers: { ...(body ? { "Content-Type": "application/json" } : {}), ...(method !== "GET" ? { RequestVerificationToken: form.querySelector('[name="__RequestVerificationToken"]').value } : {}) }, ...(body ? { body: JSON.stringify(body) } : {}) });
        const countrySearch = page.querySelector("[data-geo-country-search]"), provinceSearch = page.querySelector("[data-geo-province-search]");
        const normalize = value => value.toLocaleLowerCase().replace(/[يى]/g, "ی").replace(/ك/g, "ک").replace(/[\s‌]+/g, " ").trim();
        function filterOptions(select, input) {
            const query = normalize(input.value);
            for (const option of select.options) option.hidden = !!option.value && option.value !== select.value && !normalize(option.textContent).includes(query);
        }
        function provinces(selected = "") {
            const country = form.elements.countryId.value;
            form.elements.provinceId.replaceChildren(new Option("استان را انتخاب کنید", ""), ...data.provinces.filter(p => String(p.countryId) === country).map(p => new Option(p.nameFa, p.id)));
            form.elements.provinceId.value = String(selected || "");
            filterOptions(form.elements.provinceId, provinceSearch);
        }
        function reset() {
            form.reset(); form.elements.id.value = ""; countrySearch.value = ""; provinceSearch.value = "";
            form.elements.countryId.replaceChildren(new Option("کشور را انتخاب کنید", ""), ...data.countries.map(c => new Option(c.nameFa, c.id)));
            if (kind !== "countries") form.elements.countryId.value = provinceCountryFilter;
            if (kind !== "countries" && form.elements.countryId.value !== provinceCountryFilter) { provinceCountryFilter = ""; cityProvinceFilter = ""; }
            provinces(kind === "cities" ? cityProvinceFilter : "");
            if (kind === "cities" && form.elements.provinceId.value !== cityProvinceFilter) cityProvinceFilter = "";
            const country = kind !== "countries", province = kind === "cities";
            form.elements.nameFa.maxLength = form.elements.nameEn.maxLength = province ? 200 : 150;
            page.querySelector("[data-geo-country]").hidden = !country; page.querySelector("[data-geo-province]").hidden = !province; page.querySelector("[data-geo-code]").hidden = country;
            form.elements.countryId.disabled = !country; form.elements.countryId.required = country;
            form.elements.provinceId.disabled = !province; form.elements.provinceId.required = province;
            form.elements.code.disabled = country; form.elements.code.required = !country;
            countrySearch.disabled = !country; provinceSearch.disabled = !province;
            page.querySelector("[data-geo-flag]").hidden = country;
            page.querySelectorAll("[data-geo-coordinates]").forEach(element => { element.hidden = !country; });
            page.querySelector("[data-geo-center]").hidden = !province;
            form.elements.flagPath.disabled = country;
            form.elements.latitude.disabled = form.elements.longitude.disabled = !country;
            form.elements.isProvinceCenter.disabled = !province;
            filterOptions(form.elements.countryId, countrySearch);
        }
        function render() {
            const columns = {
                countries: [["شناسه", "id"], ["نام فارسی", "nameFa"], ["نام انگلیسی", "nameEn"], ["کد کشور", "code"], ["مسیر پرچم", "flagPath"]],
                provinces: [["شناسه", "id"], ["نام فارسی", "nameFa"], ["نام انگلیسی", "nameEn"], ["کشور", "parentName"], ["شناسه کشور", "countryId"], ["عرض جغرافیایی", "latitude"], ["طول جغرافیایی", "longitude"]],
                cities: [["شناسه", "id"], ["نام فارسی", "nameFa"], ["نام انگلیسی", "nameEn"], ["کشور / استان", "parentName"], ["شناسه کشور", "countryId"], ["شناسه استان", "provinceId"], ["CountryName خام منبع", "sourceCountryName"], ["عرض جغرافیایی", "latitude"], ["طول جغرافیایی", "longitude"], ["مرکز استان", "isProvinceCenter"]]
            }[kind];
            const headings = page.querySelector("thead tr");
            headings.replaceChildren(...[...columns.map(column => column[0]), "عملیات"].map(title => {
                const th = document.createElement("th"); th.scope = "col"; th.textContent = title; return th;
            }));
            rows.replaceChildren(); const query = normalize(search.value);
            const display = (item, key) => key === "isProvinceCenter" ? (item[key] == null ? "—" : item[key] ? "بله" : "خیر") : (item[key] == null || item[key] === "" ? "—" : String(item[key]));
            const items = data[kind].filter(x => (kind === "countries" || !provinceCountryFilter || String(x.countryId) === provinceCountryFilter) && (kind !== "cities" || !cityProvinceFilter || String(x.provinceId) === cityProvinceFilter) && columns.some(([, key]) => normalize(display(x, key)).includes(query)));
            for (const item of items) {
                const tr = document.createElement("tr");
                for (const [, key] of columns) { const td = document.createElement("td"); td.textContent = display(item, key); tr.append(td); }
                const td = document.createElement("td"), edit = document.createElement("button"), del = document.createElement("button");
                edit.type = del.type = "button"; edit.className = "btn btn-sm btn-default me-1"; del.className = "btn btn-sm btn-danger"; edit.textContent = "ویرایش"; del.textContent = "حذف";
                edit.setAttribute("aria-label", `ویرایش ${item.nameFa}`); del.setAttribute("aria-label", `حذف ${item.nameFa}`);
                edit.addEventListener("click", () => { if (busy || !available) return; reset(); form.elements.id.value = item.id; form.elements.nameFa.value = item.nameFa; form.elements.nameEn.value = item.nameEn; form.elements.code.value = item.code || ""; form.elements.flagPath.value = item.flagPath || ""; form.elements.latitude.value = item.latitude ?? ""; form.elements.longitude.value = item.longitude ?? ""; form.elements.isProvinceCenter.checked = !!item.isProvinceCenter; if (item.countryId) { form.elements.countryId.value = item.countryId; provinces(item.provinceId); form.elements.countryId.disabled = true; form.elements.provinceId.disabled = true; countrySearch.disabled = true; provinceSearch.disabled = true; } form.elements.nameFa.focus(); });
                del.addEventListener("click", async () => { if (busy || !available || !window.confirm(`آیا ${item.nameFa} حذف شود؟`)) return; busy = true; try { const result = await request(`${api}/${kind}/${item.id}`, "DELETE"); ui.notify(result.message, "success"); reset(); await refresh(); } catch (error) { await handleMutationError(error); } finally { busy = false; } });
                td.append(edit, del); tr.append(td); rows.append(tr);
            }
            status.textContent = `${new Intl.NumberFormat("fa-IR").format(items.length)} مورد`;
        }
        async function handleMutationError(error) {
            if (error instanceof TypeError || error?.name === "AbortError") {
                ui.notify("نتیجهٔ عملیات نامشخص است؛ پیش از تلاش دوباره فهرست را بررسی کنید.", "danger");
                reset();
                await refresh();
                return;
            }
            ui.notify(error.message, "danger");
        }
        async function refresh() {
            available = false; submit.disabled = true; retry.hidden = true; status.textContent = "در حال دریافت اطلاعات…";
            try { data = await request(api); available = true; reset(); render(); submit.disabled = false; }
            catch (error) { rows.replaceChildren(); status.textContent = `دریافت فهرست ناموفق بود: ${error.message}`; retry.hidden = false; }
        }
        tabs.forEach((tab, index) => {
            tab.addEventListener("click", () => { if (busy || !available) return; kind = tab.dataset.geoTab; provinceCountryFilter = ""; cityProvinceFilter = ""; tabs.forEach(t => { const active = t === tab; t.classList.toggle("active", active); t.setAttribute("aria-selected", String(active)); t.tabIndex = active ? 0 : -1; }); page.querySelector("[data-geo-panel]").setAttribute("aria-labelledby", tab.id); search.value = ""; reset(); render(); });
            tab.addEventListener("keydown", event => { let next; if (event.key === "ArrowRight") next = (index + tabs.length - 1) % tabs.length; else if (event.key === "ArrowLeft") next = (index + 1) % tabs.length; else if (event.key === "Home") next = 0; else if (event.key === "End") next = tabs.length - 1; else return; if (busy || !available) return; event.preventDefault(); tabs[next].focus(); tabs[next].click(); });
        });
        countrySearch.addEventListener("input", () => filterOptions(form.elements.countryId, countrySearch));
        provinceSearch.addEventListener("input", () => filterOptions(form.elements.provinceId, provinceSearch));
        form.elements.countryId.addEventListener("change", () => { provinceSearch.value = ""; provinces(); filterOptions(form.elements.countryId, countrySearch); if (kind !== "countries" && !busy && available) { provinceCountryFilter = form.elements.countryId.value; cityProvinceFilter = ""; render(); } });
        form.elements.provinceId.addEventListener("change", () => { if (kind === "cities" && !busy && available) { cityProvinceFilter = form.elements.provinceId.value; filterOptions(form.elements.provinceId, provinceSearch); render(); } });
        page.querySelector("[data-geo-cancel]").addEventListener("click", () => { if (!busy) reset(); }); search.addEventListener("input", () => { if (!busy && available) render(); });
        form.addEventListener("submit", async event => { event.preventDefault(); if (busy || !available || !form.reportValidity()) return; busy = true; const button = form.querySelector('[type="submit"]'); button.disabled = true;
            const id = form.elements.id.value, body = { nameFa: form.elements.nameFa.value, nameEn: form.elements.nameEn.value, code: form.elements.code.value, countryId: Number(form.elements.countryId.value) || null, provinceId: Number(form.elements.provinceId.value) || null, flagPath: form.elements.flagPath.value, latitude: form.elements.latitude.value === "" ? null : Number(form.elements.latitude.value), longitude: form.elements.longitude.value === "" ? null : Number(form.elements.longitude.value), isProvinceCenter: form.elements.isProvinceCenter.checked, updateDetails: true };
            try { const result = await request(`${api}/${kind}${id ? `/${id}` : ""}`, id ? "PUT" : "POST", body); ui.notify(result.message, "success"); reset(); await refresh(); } catch (error) { await handleMutationError(error); } finally { busy = false; button.disabled = !available; }
        });
        retry.addEventListener("click", async () => { if (busy) return; busy = true; await refresh(); busy = false; });
        reset(); refresh().finally(() => { busy = false; });
    }
    window.AdminGeography = Object.freeze({ initialize });
})();
