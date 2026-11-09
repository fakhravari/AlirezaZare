(() => {
    "use strict";
    const ui = window.AdminUI;
    if (!ui) return;
    const initialized = new WeakSet();
    const formRequests = new WeakMap();
    const fmt = new Intl.NumberFormat("fa-IR");
    const configs = {
        "stock-owners": { fields: [["code", "کد", "text", true], ["displayName", "نام", "text", true], ["idStockOwnerType", "نوع", "select", true, [["1", "طرح فروش"], ["2", "مالک"]]]], columns: [["code", "کد"], ["displayName", "نام"], ["idStockOwnerType", "نوع"], ["isActive", "وضعیت", "active"]] },
        companies: { fields: [["code", "کد شرکت", "text", true], ["name", "نام شرکت", "text", true]], columns: [["code", "کد"], ["name", "شرکت"], ["isActive", "وضعیت", "active"]] },
        warehouses: { fields: [["companyId", "شرکت", "companies", true], ["code", "کد انبار", "text", true], ["name", "نام انبار", "text", true], ["isSalesWarehouse", "انبار فروش است", "checkbox"], ["ownerSalesPlanId", "طرح مالک انبار فروش", "plans"], ["isSiteWarehouse", "منبع موجودی سایت", "checkbox"]], columns: [["code", "کد"], ["name", "انبار"], ["companyName", "شرکت"], ["isSalesWarehouse", "انبار فروش", "affects"], ["isSiteWarehouse", "منبع موجودی سایت", "siteWarehouse"], ["ownerSalesPlan", "طرح مالک"], ["isActive", "وضعیت", "active"]] },
        references: { fields: [["name", "نام مرجع", "text", true], ["partyType", "نوع مرجع", "select", true, [["Person", "شخص"], ["Organization", "سازمان"], ["Warehouse", "انبار"], ["Other", "سایر"]]], ["warehouseId", "انبار مرتبط", "warehouses"], ["externalPartyKey", "شناسهٔ بیرونی", "text"]], columns: [["name", "نام"], ["partyType", "نوع"], ["idWarehouse", "شناسهٔ انبار"], ["isActive", "وضعیت", "active"]] },
        units: { fields: [["code", "کد واحد", "text", true], ["name", "نام واحد", "text", true]], columns: [["code", "کد"], ["name", "واحد"], ["isActive", "وضعیت", "active"]] },
        "document-types": { fields: [["code", "کد نوع سند", "text", true], ["name", "نام سند", "text", true], ["direction", "جهت سند", "select", true, [["R", "رسید"], ["I", "حواله"]]], ["affectsStock", "بر موجودی اثر دارد", "checkbox" ]], columns: [["code", "کد"], ["name", "نوع سند"], ["direction", "جهت", "direction"], ["affectsStock", "اثر موجودی", "affects"], ["isActive", "وضعیت", "active"]] }
        ,"product-units": { fields: [["productId", "کالا", "products", true], ["unitId", "واحد", "units", true], ["conversionFactor", "ضریب تبدیل به واحد اصلی", "number", true], ["isBaseUnit", "واحد اصلی کالا", "checkbox"]], columns: [["productName", "کالا"], ["unitName", "واحد"], ["conversionFactor", "ضریب تبدیل"], ["isBaseUnit", "اصلی", "base"], ["isActive", "وضعیت", "active"]] },
        variants: { fields: [["productId", "کالا", "products", true], ["name", "نام تنوع", "text", true], ["code", "کد تنوع", "text"], ["attributesJson", "ویژگی‌های موجودی (JSON)", "textarea"]], columns: [["productName", "کالا"], ["name", "تنوع"], ["code", "کد"], ["isActive", "وضعیت", "active"]] }
    };
    const esc = value => value == null || value === "" ? "—" : String(value);
    function setStatus(page, message, error = false) {
        const target = page.querySelector("[data-definition-status]");
        if (!target) return;
        target.textContent = message;
        target.classList.toggle("is-error", error);
        target.classList.toggle("d-none", !message);
    }
    function token(page) { return page.querySelector('[name="__RequestVerificationToken"]')?.value || document.querySelector('[name="__RequestVerificationToken"]')?.value || ""; }
    async function request(page, url, method = "GET", body) {
        return ui.getJson(url, { method, headers: { ...(body ? { "Content-Type": "application/json" } : {}), RequestVerificationToken: token(page) }, ...(body ? { body: JSON.stringify(body) } : {}) });
    }
    function element(tag, text, cls) { const node = document.createElement(tag); if (text != null) node.textContent = text; if (cls) node.className = cls; return node; }
    function active(value) { return value ? "فعال" : "غیرفعال"; }
    function renderTable(page, rows, config) {
        const head = page.querySelector("[data-definition-head]"), body = page.querySelector("[data-definition-rows]");
        head.replaceChildren(); body.replaceChildren();
        const tr = document.createElement("tr");
        for (const [, label] of config.columns) tr.append(element("th", label));
        tr.append(element("th", "عملیات")); head.append(tr);
        for (const item of rows) {
            const row = document.createElement("tr");
            for (const [key, , format] of config.columns) {
                let value = item[key];
                if (format === "active") value = active(value);
                if (format === "direction") value = value === "R" ? "رسید" : "حواله";
                if (format === "affects") value = value ? "دارد" : "ندارد";
                if (format === "base") value = value ? "واحد اصلی" : "واحد فرعی";
                if (format === "siteWarehouse") value = value ? "بله" : "خیر";
                if (key === "idStockOwnerType") value = value === 1 ? "طرح فروش" : "مالک";
                row.append(element("td", esc(value)));
            }
            const actions = element("td", null, "text-nowrap");
            if (item.isActive === false && page.dataset.definitionSection === "units" && page.dataset.canEdit === "true") {
                const activate = element("button", "فعال‌سازی", "btn btn-sm btn-outline-success");
                activate.type = "button";
                activate.addEventListener("click", async () => {
                    if (activate.disabled) return;
                    activate.disabled = true;
                    try { const result = await request(page, `${page.dataset.api}/units/${item.id}`, "PUT", { code: item.code, name: item.name, isActive: true }); ui.notify(result.message, "success"); await load(page); }
                    catch (error) { ui.notify(error.message, "danger"); }
                    finally { activate.disabled = false; }
                });
                actions.append(activate);
            }
            if (item.isActive !== false) {
                if (page.dataset.canEdit === "true") {
                    const edit = element("button", "ویرایش", "btn btn-sm btn-default me-1"); edit.type = "button"; edit.addEventListener("click", () => void openForm(page, item, config).catch(error => ui.notify(error.message || "بازکردن فرم ناموفق بود.", "danger")));
                    actions.append(edit);
                }
                if (page.dataset.canDeactivate === "true" && page.dataset.definitionSection !== "stock-owners") {
                const deactivate = element("button", "غیرفعال‌سازی", "btn btn-sm btn-outline-danger"); deactivate.type = "button";
                deactivate.addEventListener("click", async () => {
                    if (deactivate.disabled || !window.confirm("این تعریف غیرفعال شود؟ سابقهٔ گردش‌ها حفظ می‌شود.")) return;
                    deactivate.disabled = true;
                    try { const result = await request(page, `${page.dataset.api}/${page.dataset.definitionSection}/${item.id}/deactivate`, "POST", {}); ui.notify(result.message, "success"); await load(page); }
                    catch (error) { ui.notify(error.message, "danger"); }
                    finally { deactivate.disabled = false; }
                });
                actions.append(deactivate);
                }
            }
            row.append(actions); body.append(row);
        }
        if (!rows.length) setStatus(page, "موردی ثبت نشده است.");
    }
    function addField(host, field, data, lookups) {
        const [key, label, type = "text", required = false, choices = []] = field;
        const wrap = element("div", null, "mb-3");
        if (type === "checkbox") {
            const check = element("input", null, "form-check-input"); check.type = "checkbox"; check.name = key; check.checked = Boolean(data[key]); check.id = `definition-${key}`;
            const caption = element("label", label, "form-check-label"); caption.htmlFor = check.id; wrap.className = "form-check mb-3"; wrap.append(check, caption);
        } else {
            const control = ["select", "warehouses", "products", "units", "companies", "plans"].includes(type) ? element("select", null, "form-select") : type === "textarea" ? element("textarea", null, "form-control") : element("input", null, "form-control");
            control.name = key; control.id = `definition-${key}`; control.required = required;
            if (type === "select") {
                const empty = element("option", "انتخاب کنید"); empty.value = ""; control.append(empty);
                for (const [value, name] of choices) { const option = element("option", name); option.value = value; control.append(option); }
            }
            if (["warehouses", "products", "units", "companies", "plans"].includes(type)) {
                const choices = type === "warehouses" ? lookups.warehouses : (lookups[type] || []);
                const empty = element("option", type === "warehouses" ? "بدون انبار مرتبط" : "انتخاب کنید"); empty.value = ""; control.append(empty);
                for (const row of choices) { const option = element("option", type === "warehouses" ? `${row.name} (${row.code})` : (row.name || row.displayName)); option.value = row.id; control.append(option); }
            }
            if (type === "text") control.type = "text";
            if (type === "textarea") { control.rows = 4; control.placeholder = '[{"key":"color","label":"رنگ","value":"yellow","display":"زرد"}]'; }
            if (type === "number") { control.type = "number"; control.min = key === "conversionFactor" ? "0.000000001" : "1"; control.step = key === "conversionFactor" ? "any" : "1"; if (key === "conversionFactor") control.max = "999999999.999999999"; }
            if (type === "select" || ["warehouses", "products", "units", "companies", "plans"].includes(type)) control.value = data[key] == null ? "" : String(data[key]);
            else control.value = data[key] ?? (key === "conversionFactor" ? "1" : "");
            const caption = element("label", label, "form-label"); caption.htmlFor = control.id; wrap.append(caption, control);
        }
        host.append(wrap);
    }
    async function openForm(page, data, config) {
        const sequence = (formRequests.get(page) || 0) + 1;
        formRequests.set(page, sequence);
        const form = document.querySelector("[data-definition-form]"), host = form.querySelector("[data-definition-fields]");
        if (data && page.dataset.definitionSection === "warehouses") data = { ...data, companyId: data.idCompany, ownerSalesPlanId: data.idOwnerSalesPlanReference };
        if (data && page.dataset.definitionSection === "references") data = { ...data, warehouseId: data.idWarehouse };
        if (data && page.dataset.definitionSection === "product-units") data = { ...data, productId: data.idProduct, unitId: data.idUnit };
        let lookups = { warehouses: [] };
        if (["references", "product-units", "variants", "warehouses"].includes(page.dataset.definitionSection)) lookups = await request(page, `${page.dataset.api}/lookups`);
        if (!page.isConnected || formRequests.get(page) !== sequence) return;
        host.replaceChildren(); form.reset(); form.elements.id.value = data?.id || "";
        for (const field of config.fields) {
            if (field[0] === "isSiteWarehouse" && page.dataset.fullAccess !== "true") continue;
            addField(host, field, data || {}, lookups);
        }
        if (page.dataset.definitionSection === "warehouses") {
            const sales = form.querySelector('[name="isSalesWarehouse"]'), plan = form.querySelector('[name="ownerSalesPlanId"]');
            const updatePlan = () => { plan.required = true; plan.disabled = false; };
            sales.addEventListener("change", updatePlan); updatePlan();
        }
        document.getElementById("admin-definition-title").textContent = data ? "ویرایش تعریف" : "افزودن تعریف";
        window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("admin-definition-modal")).show();
    }
    async function loadMapping(page) {
        const data = await request(page, `${page.dataset.api}/sales-plan-mapping`), form = page.querySelector("[data-mapping-form]");
        const fill = (select, rows, label) => { select.replaceChildren(); for (const row of rows) { const option = element("option", label(row)); option.value = row.id; select.append(option); } };
        fill(form.elements.companyId, data.companies, item => item.name);
        const company = form.elements.companyId, warehouse = form.elements.warehouseId;
        const fillWarehouses = () => fill(warehouse, data.warehouses.filter(item => item.idCompany === Number(company.value)), item => `${item.name} (${item.code})`);
        company.onchange = fillWarehouses; fillWarehouses();
        fill(form.elements.salesPlanId, data.plans, item => item.displayName);
        const rows = page.querySelector("[data-mapping-rows]");
        rows.replaceChildren();
        for (const link of data.links) {
            const warehouseRow = data.warehouses.find(item => item.id === link.idWarehouse);
            const planRow = data.plans.find(item => item.id === link.idStockOwner);
            if (!warehouseRow || !planRow) continue;
            const companyRow = data.companies.find(item => item.id === warehouseRow.idCompany);
            const row = element("tr");
            row.append(element("td", companyRow?.name || "—"), element("td", warehouseRow.name), element("td", planRow.displayName));
            const actions = element("td"), remove = element("button", "حذف اتصال", "btn btn-sm btn-outline-danger");
            remove.type = "button";
            remove.addEventListener("click", async () => {
                if (remove.disabled || !window.confirm("اتصال این مالک به انبار حذف شود؟")) return;
                remove.disabled = true;
                try { const result = await request(page, `${page.dataset.api}/sales-plan-mapping/${link.idWarehouse}/${link.idStockOwner}`, "DELETE", {}); ui.notify(result.message, "success"); await loadMapping(page); }
                catch (error) { ui.notify(error.message, "danger"); }
                finally { remove.disabled = false; }
            });
            actions.append(remove); row.append(actions);
            rows.append(row);
        }
        if (data.siteWarehouseId) { const selected = data.warehouses.find(item => item.id === data.siteWarehouseId); if (selected) { company.value = String(selected.idCompany); fillWarehouses(); warehouse.value = String(selected.id); } }
    }
    async function load(page) {
        const section = page.dataset.definitionSection;
        if (section === "sales-plan-mapping") { await loadMapping(page); setStatus(page, ""); return; }
        const config = configs[section];
        const rows = await request(page, `${page.dataset.api}/${section}`);
        renderTable(page, rows, config);
        if (rows.length) setStatus(page, "");
    }
    async function initialize(root) {
        const page = root.querySelector?.("[data-admin-definitions]");
        if (!page || initialized.has(page)) return;
        initialized.add(page);
        const section = page.dataset.definitionSection, form = document.querySelector("[data-definition-form]");
        try {
            if (section === "sales-plan-mapping") {
                const mapping = page.querySelector("[data-mapping-form]");
                mapping.addEventListener("submit", async event => { event.preventDefault(); if (mapping.dataset.saving === "true") return; const submit = mapping.querySelector('[type="submit"]'); mapping.dataset.saving = "true"; submit.disabled = true; try { const result = await request(page, `${page.dataset.api}/sales-plan-mapping`, "PUT", { companyId: Number(mapping.elements.companyId.value), warehouseId: Number(mapping.elements.warehouseId.value), salesPlanId: Number(mapping.elements.salesPlanId.value) }); ui.notify(result.message, "success"); await loadMapping(page); } catch (error) { ui.notify(error.message, "danger"); } finally { mapping.dataset.saving = "false"; submit.disabled = false; } });
            } else {
                const config = configs[section];
                const createButton = page.querySelector("[data-definition-create]");
                createButton.hidden = page.dataset.canCreate !== "true";
                createButton.addEventListener("click", () => void openForm(page, null, config).catch(error => ui.notify(error.message || "بازکردن فرم ناموفق بود.", "danger")));
                form.addEventListener("submit", async event => {
                    event.preventDefault();
                    if (form.dataset.saving === "true") return;
                    for (const [key, label, type, required] of config.fields) {
                        const control = form.elements.namedItem(key);
                        if (required && type === "text" && control && !control.value.trim()) {
                            ui.notify(`${label} را وارد کنید.`, "danger"); control.focus(); return;
                        }
                    }
                    const values = new FormData(form), id = values.get("id"); const body = {};
                    for (const [key, , type] of config.fields) body[key] = type === "checkbox" ? values.get(key) === "on" : (key === "idStockOwnerType" || ["number", "products", "units", "warehouses", "companies", "plans"].includes(type) ? Number(values.get(key)) || null : values.get(key));
                    body.isActive = true;
                    const submit = form.querySelector('[type="submit"]');
                    form.dataset.saving = "true"; submit.disabled = true;
                    try { const result = await request(page, `${page.dataset.api}/${section}${id ? `/${id}` : ""}`, id ? "PUT" : "POST", body); ui.notify(result.message, "success"); window.bootstrap?.Modal?.getInstance(document.getElementById("admin-definition-modal"))?.hide(); await load(page); }
                    catch (error) { ui.notify(error.message, "danger"); }
                    finally { form.dataset.saving = "false"; submit.disabled = false; }
                });
            }
            await load(page);
        } catch (error) { setStatus(page, error.message || "بارگذاری ناموفق بود.", true); }
    }
    window.AdminDefinitions = Object.freeze({ initialize });
})();
