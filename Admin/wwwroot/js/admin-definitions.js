(() => {
    "use strict";
    const ui = window.AdminUI;
    if (!ui) return;
    const initialized = new WeakSet();
    const formRequests = new WeakMap();
    const fmt = new Intl.NumberFormat("fa-IR");
    const conversionFmt = new Intl.NumberFormat("fa-IR", { maximumFractionDigits: 9 });
    const configs = {
        "stock-owners": { fields: [["code", "کد", "text", true], ["displayName", "نام", "text", true], ["idStockOwnerType", "نوع", "select", true, [["1", "طرح فروش"], ["2", "مالک"]]]], columns: [["code", "کد"], ["displayName", "نام"], ["idStockOwnerType", "نوع"], ["isActive", "وضعیت", "active"]] },
        companies: { fields: [["code", "کد شرکت", "text", true], ["name", "نام شرکت", "text", true], ["phone", "تلفن", "text"], ["address", "آدرس", "textarea"], ["description", "توضیحات", "textarea"]], columns: [["code", "کد"], ["name", "شرکت"], ["phone", "تلفن"], ["address", "آدرس"], ["description", "توضیحات"], ["isActive", "وضعیت", "active"]] },
        warehouses: { fields: [["companyId", "شرکت", "companies", true], ["code", "کد انبار", "text", true], ["warehouseTypeId", "نام انبار", "warehouse-types", true], ["isSalesWarehouse", "انبار فروش است", "checkbox"], ["ownerSalesPlanId", "طرح مالک انبار فروش", "plans"], ["isSiteWarehouse", "منبع موجودی سایت", "checkbox"]], columns: [["warehouseLabel", "شرکت / انبار"], ["isSalesWarehouse", "انبار فروش", "affects"], ["isSiteWarehouse", "منبع موجودی سایت", "siteWarehouse"], ["ownerSalesPlan", "طرح مالک"], ["isActive", "وضعیت", "active"]] },
        references: { fields: [["name", "نام مرجع", "text", true], ["partyType", "نوع مرجع", "select", true, [["Person", "شخص"], ["Organization", "سازمان"], ["Warehouse", "انبار"], ["Other", "سایر"]]], ["warehouseId", "انبار مرتبط", "warehouses"], ["externalPartyKey", "شناسهٔ بیرونی", "text"]], columns: [["name", "نام"], ["partyType", "نوع"], ["idWarehouse", "شناسهٔ انبار"], ["isActive", "وضعیت", "active"]] },
        units: { fields: [["unitGroupId", "گروه واحد", "unit-groups", true], ["nameFa", "نام فارسی", "text", true], ["nameEn", "نام انگلیسی", "text"], ["isActive", "واحد فعال است", "checkbox"]], columns: [["unitGroupName", "گروه"], ["nameFa", "نام فارسی"], ["nameEn", "نام انگلیسی"], ["isActive", "وضعیت", "active"]] },
        "document-types": { fields: [["code", "کد نوع سند", "text", true], ["name", "نام سند", "text", true], ["direction", "جهت سند", "select", true, [["R", "رسید"], ["I", "حواله"]]], ["affectsStock", "بر موجودی اثر دارد", "checkbox" ]], columns: [["code", "کد"], ["name", "نوع سند"], ["direction", "جهت", "direction"], ["affectsStock", "اثر موجودی", "affects"], ["isActive", "وضعیت", "active"]] }
        ,"product-units": { fields: [["productId", "کالا", "products", true], ["unitId", "واحد", "units", true], ["conversionFactor", "ضریب تبدیل به واحد اصلی", "number", true], ["isBaseUnit", "واحد اصلی کالا", "checkbox"]], columns: [["productName", "کالا"], ["unitName", "واحد"], ["conversionFactor", "ضریب تبدیل"], ["isBaseUnit", "اصلی", "base"], ["isActive", "وضعیت", "active"]] },
        variants: { fields: [["productId", "کالا", "products", true], ["name", "نام تنوع", "text", true], ["code", "کد تنوع", "text"], ["attributes", "ویژگی‌های تنوع", "attributes"], ["isDefault", "تنوع پیش‌فرض", "checkbox"]], columns: [["productName", "کالا"], ["name", "تنوع"], ["code", "کد"], ["attributes", "ویژگی‌ها", "attributes"], ["isDefault", "پیش‌فرض", "default"], ["isActive", "وضعیت", "active"]] }
    };
    const esc = value => value == null || value === "" ? "—" : String(value);
    function setStatus(page, message, error = false) {
        const target = page.querySelector("[data-definition-status]");
        if (!target) return;
        target.textContent = message;
        target.classList.toggle("text-danger", error); target.classList.toggle("text-muted", !error); target.classList.add("small");
        target.classList.toggle("d-none", !message);
    }
    function token(page) { return page.querySelector('[name="__RequestVerificationToken"]')?.value || document.querySelector('[name="__RequestVerificationToken"]')?.value || ""; }
    async function request(page, url, method = "GET", body) {
        return ui.getJson(url, { method, headers: { ...(body ? { "Content-Type": "application/json" } : {}), RequestVerificationToken: token(page) }, ...(body ? { body: JSON.stringify(body) } : {}) });
    }
    function element(tag, text, cls) { const node = document.createElement(tag); if (text != null) node.textContent = text; if (cls) node.className = cls; return node; }
    function actionButton(label, iconName, cls) {
        const button = element("button", null, cls);
        button.type = "button";
        const icon = element("i", null, `fa ${iconName}`);
        icon.setAttribute("aria-hidden", "true");
        button.append(icon, document.createTextNode(label));
        return button;
    }
    function active(value) { return value ? "فعال" : "غیرفعال"; }
    function renderTable(page, rows, config) {
        const head = page.querySelector("[data-definition-head]"), body = page.querySelector("[data-definition-rows]");
        const referenceType = page.querySelector("[data-reference-type-filter]")?.value;
        const visibleRows = page.dataset.definitionSection === "references" && referenceType
            ? rows.filter(item => item.partyType === referenceType)
            : rows;
        ui.destroyDataTable(page.querySelector("[data-admin-datatable]"));
        head.replaceChildren(); body.replaceChildren();
        const tr = document.createElement("tr");
        for (const [, label] of config.columns) tr.append(element("th", label));
        tr.append(element("th", "عملیات")); head.append(tr);
        for (const item of visibleRows) {
            const row = document.createElement("tr");
            for (const [key, , format] of config.columns) {
                let value = item[key];
                if (key === "warehouseLabel") value = `${item.companyName} / ${item.name} (${item.code})`;
                if (format === "active") value = active(value);
                if (format === "default") value = value ? "پیش‌فرض" : "خیر";
                if (format === "direction") value = value === "R" ? "رسید" : "حواله";
                if (format === "affects") value = value ? "دارد" : "ندارد";
                if (format === "base") value = value ? "واحد اصلی" : "واحد فرعی";
                if (format === "siteWarehouse") value = value ? "بله" : "خیر";
                if (format === "attributes") value = Array.isArray(value) ? value.map(attribute => `${attribute.label}: ${attribute.display}`).join("، ") : "—";
                if (key === "idStockOwnerType") value = value === 1 ? "طرح فروش" : "مالک";
                row.append(element("td", esc(value)));
            }
            const actions = element("td", null, "text-nowrap");
            if (item.isActive === false && page.dataset.definitionSection === "units" && page.dataset.canEdit === "true") {
                const activate = actionButton("فعال‌سازی", "fa-toggle-on", "btn btn-sm btn-outline-success");
                activate.addEventListener("click", async () => {
                    if (activate.disabled) return;
                    activate.disabled = true;
                    try { const result = await request(page, `${page.dataset.api}/units/${item.id}`, "PUT", { code: item.code, name: item.name, isActive: true }); ui.notify(result.message, "success"); await load(page); }
                    catch (error) { ui.notify(error.message, "danger"); }
                    finally { activate.disabled = false; }
                });
                actions.append(activate);
            }
            if (item.isActive === false && page.dataset.definitionSection === "warehouses" && page.dataset.canEdit === "true") {
                const activate = actionButton("فعال‌سازی و ویرایش", "fa-toggle-on", "btn btn-sm btn-outline-success");
                activate.addEventListener("click", () => void openForm(page, item, config).catch(error => ui.notify(error.message || "بازکردن فرم ناموفق بود.", "danger")));
                actions.append(activate);
            }
            if (item.isActive !== false) {
                if (page.dataset.canEdit === "true") {
                    const edit = actionButton("ویرایش", "fa-pen-to-square", "btn btn-sm btn-default me-1"); edit.addEventListener("click", () => void openForm(page, item, config).catch(error => ui.notify(error.message || "بازکردن فرم ناموفق بود.", "danger")));
                    actions.append(edit);
                }
                if (page.dataset.canDeactivate === "true" && page.dataset.definitionSection !== "stock-owners") {
                const deactivate = actionButton("غیرفعال‌سازی", "fa-toggle-off", "btn btn-sm btn-outline-danger");
                deactivate.dataset.adminConfirm = "این تعریف غیرفعال شود؟ سابقهٔ گردش‌ها حفظ می‌شود.";
                deactivate.addEventListener("click", async () => {
                    if (deactivate.disabled) return;
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
        ui.refreshDataTable(page.querySelector("[data-admin-datatable]"));
        setStatus(page, visibleRows.length ? "" : (referenceType ? "مرجعی با این نوع پیدا نشد." : "موردی ثبت نشده است."));
    }
    function renderProductUnitTree(page, rows, config) {
        const tree = page.querySelector("[data-product-unit-tree]");
        ui.destroyDataTable(page.querySelector("[data-admin-datatable]"));
        tree.replaceChildren();
        const products = new Map();
        for (const row of rows) {
            if (!products.has(row.idProduct)) products.set(row.idProduct, { name: row.productName, units: [] });
            products.get(row.idProduct).units.push(row);
        }
        for (const [productId, product] of products) {
            const branch = element("details", null, "list-group-item p-0 mb-2 border rounded");
            const summary = element("summary", null, "d-flex align-items-center justify-content-between gap-2 px-3 py-2 fw-semibold");
            summary.append(element("span", product.name || `کالا ${fmt.format(productId)}`));
            summary.append(element("span", `${fmt.format(product.units.length)} واحد`, "badge bg-light text-dark"));
            branch.append(summary);
            const children = element("ul", null, "list-group list-group-flush px-4 pb-2");
            for (const item of product.units) {
                const node = element("li", null, "list-group-item d-flex flex-wrap align-items-center justify-content-between gap-2");
                const description = element("div", null, "d-flex flex-column");
                description.append(element("span", item.unitName || "واحد نامشخص", "fw-semibold"));
                description.append(element("span", item.isBaseUnit
                    ? "واحد پایه · ضریب ۱"
                    : `ضریب ثبت‌شده: ${conversionFmt.format(Number(item.conversionFactor))}`, "small text-muted"));
                if (item.isActive === false) description.append(element("span", "غیرفعال", "small text-danger"));
                node.append(description);
                const actions = element("div", null, "d-flex flex-wrap gap-1");
                if (item.isActive !== false && page.dataset.canEdit === "true") {
                    const edit = actionButton("ویرایش", "fa-pen-to-square", "btn btn-sm btn-default");
                    edit.addEventListener("click", () => void openForm(page, item, config).catch(error => ui.notify(error.message || "بازکردن فرم ناموفق بود.", "danger")));
                    actions.append(edit);
                }
                if (item.isActive !== false && !item.isBaseUnit && page.dataset.canDeactivate === "true") {
                    const deactivate = actionButton("غیرفعال‌سازی", "fa-toggle-off", "btn btn-sm btn-outline-danger");
                    deactivate.dataset.adminConfirm = "این واحد تبدیل غیرفعال شود؟ سابقه حفظ می‌شود.";
                    deactivate.addEventListener("click", async () => {
                        if (deactivate.disabled) return;
                        deactivate.disabled = true;
                        try {
                            const result = await request(page, `${page.dataset.api}/product-units/${item.id}/deactivate`, "POST", {});
                            ui.notify(result.message, "success");
                            await load(page);
                        } catch (error) { ui.notify(error.message, "danger"); }
                        finally { deactivate.disabled = false; }
                    });
                    actions.append(deactivate);
                }
                if (actions.childElementCount) node.append(actions);
                children.append(node);
            }
            branch.append(children);
            tree.append(branch);
        }
        setStatus(page, rows.length ? "" : "واحد غیرفعالی در تاریخچه ثبت نشده است.");
    }
    function renderUnitTree(page, groups, units) {
        const tree = page.querySelector("[data-unit-tree]");
        tree.replaceChildren();
        for (const group of groups) {
            const members = units.filter(unit => unit.idUnitGroup === group.id);
            const branch = element("details", null, "list-group-item p-0 mb-2 border rounded");
            const summary = element("summary", null, "d-flex align-items-center justify-content-between gap-2 px-3 py-2 fw-semibold");
            summary.append(element("span", `${group.name}${group.isActive ? "" : " · غیرفعال"}`));
            summary.append(element("span", `${fmt.format(members.length)} واحد`, "badge bg-light text-dark"));
            branch.append(summary);
            const children = element("ul", null, "list-group list-group-flush px-4 pb-2");
            const groupActions = element("li", null, "list-group-item d-flex flex-wrap justify-content-end gap-2");
            if (page.dataset.canEdit === "true") {
                const edit = actionButton("ویرایش گروه", "fa-pen-to-square", "btn btn-sm btn-default");
                edit.addEventListener("click", () => {
                    const form = page.querySelector("[data-unit-group-form]");
                    form.elements.id.value = group.id; form.elements.name.value = group.name; form.elements.sortOrder.value = group.sortOrder;
                    form.querySelector('[type="submit"]').textContent = "ذخیرهٔ گروه"; form.querySelector("[data-unit-group-cancel]").hidden = false;
                    form.elements.name.focus();
                });
                groupActions.append(edit);
                if (group.isActive ? page.dataset.canDeactivate === "true" : page.dataset.canEdit === "true") {
                    const toggle = actionButton(group.isActive ? "غیرفعال‌سازی گروه" : "فعال‌سازی گروه", group.isActive ? "fa-toggle-off" : "fa-toggle-on", group.isActive ? "btn btn-sm btn-outline-danger" : "btn btn-sm btn-outline-success");
                    if (group.isActive) toggle.dataset.adminConfirm = "گروه واحد غیرفعال شود؟ گروه دارای واحد فعال قابل غیرفعال‌سازی نیست.";
                    toggle.addEventListener("click", async () => {
                        if (toggle.disabled) return; toggle.disabled = true;
                        try {
                            const result = group.isActive
                                ? await request(page, `${page.dataset.api}/unit-groups/${group.id}/deactivate`, "POST", {})
                                : await request(page, `${page.dataset.api}/unit-groups/${group.id}`, "PUT", { name: group.name, sortOrder: group.sortOrder, isActive: true });
                            ui.notify(result.message, "success"); await load(page);
                        } catch (error) { ui.notify(error.message, "danger"); } finally { toggle.disabled = false; }
                    });
                    groupActions.append(toggle);
                }
            }
            if (groupActions.childElementCount) children.append(groupActions);
            for (const unit of members) {
                const node = element("li", null, "list-group-item d-flex flex-wrap align-items-center justify-content-between gap-2");
                node.append(element("span", `${unit.nameFa}${unit.nameEn ? ` / ${unit.nameEn}` : ""}${unit.isActive ? "" : " · غیرفعال"}`, unit.isActive ? "" : "text-muted"));
                const actions = element("div", null, "d-flex flex-wrap gap-1");
                if (group.isActive && page.dataset.canEdit === "true") {
                    const editUnit = actionButton("ویرایش", "fa-pen-to-square", "btn btn-sm btn-default");
                    editUnit.addEventListener("click", () => void openForm(page, unit, configs.units).catch(error => ui.notify(error.message || "بازکردن فرم ناموفق بود.", "danger")));
                    actions.append(editUnit);
                }
                if (unit.isActive ? page.dataset.canDeactivate === "true" : page.dataset.canEdit === "true") {
                    const toggle = actionButton(unit.isActive ? "غیرفعال‌سازی" : "فعال‌سازی", unit.isActive ? "fa-toggle-off" : "fa-toggle-on", unit.isActive ? "btn btn-sm btn-outline-danger" : "btn btn-sm btn-outline-success");
                    if (unit.isActive) toggle.dataset.adminConfirm = "این واحد غیرفعال شود؟ واحد مورد استفادهٔ کالا غیرفعال نمی‌شود.";
                    toggle.addEventListener("click", async () => {
                        if (toggle.disabled) return; toggle.disabled = true;
                        try {
                            const result = unit.isActive
                                ? await request(page, `${page.dataset.api}/units/${unit.id}/deactivate`, "POST", {})
                                : await request(page, `${page.dataset.api}/units/${unit.id}`, "PUT", { nameFa: unit.nameFa, nameEn: unit.nameEn, unitGroupId: unit.idUnitGroup, isActive: true });
                            ui.notify(result.message, "success"); await load(page);
                        } catch (error) { ui.notify(error.message, "danger"); } finally { toggle.disabled = false; }
                    });
                    actions.append(toggle);
                }
                node.append(actions); children.append(node);
            }
            if (!members.length) children.append(element("li", "هنوز واحدی در این گروه ثبت نشده است.", "list-group-item text-muted"));
            branch.append(children); tree.append(branch);
        }
        setStatus(page, groups.length ? "" : "گروهی تعریف نشده است.");
    }
    function addField(host, field, data, lookups) {
        const [key, label, type = "text", required = false, choices = []] = field;
        const wrap = element("div", null, "mb-3");
        if (type === "attributes") {
            const caption = element("label", label, "form-label");
            caption.id = "definition-attributes-label";
            const list = element("div", null, "d-grid gap-2");
            list.dataset.variantAttributes = "";
            list.setAttribute("aria-labelledby", caption.id);
            for (const attribute of Array.isArray(data[key]) ? data[key] : []) addVariantAttributeRow(list, attribute);
            const add = actionButton("افزودن ویژگی", "fa-plus", "btn btn-sm btn-outline-primary mt-2");
            add.addEventListener("click", () => addVariantAttributeRow(list));
            wrap.append(caption, list, add);
            host.append(wrap);
            return;
        }
        if (type === "checkbox") {
            const check = element("input", null, "form-check-input"); check.type = "checkbox"; ui.initStatusSwitch(check); check.name = key; check.checked = Boolean(data[key]); check.id = `definition-${key}`;
            const caption = element("label", label, "form-check-label"); caption.htmlFor = check.id; wrap.className = "form-check mb-3"; wrap.append(check, caption);
        } else {
            const control = ["select", "warehouses", "products", "units", "companies", "plans", "warehouse-types", "unit-groups"].includes(type) ? element("select", null, "form-select") : type === "textarea" ? element("textarea", null, "form-control") : element("input", null, "form-control");
            control.name = key; control.id = `definition-${key}`; control.required = required;
            if (type === "select") {
                const empty = element("option", "انتخاب کنید"); empty.value = ""; control.append(empty);
                for (const [value, name] of choices) { const option = element("option", name); option.value = value; control.append(option); }
            }
            if (["warehouses", "products", "units", "companies", "plans", "warehouse-types", "unit-groups"].includes(type)) {
                const choices = type === "warehouses" ? lookups.warehouses : (lookups[type] || []);
                const empty = element("option", type === "warehouses" ? "بدون انبار مرتبط" : "انتخاب کنید"); empty.value = ""; control.append(empty);
                for (const row of choices.filter(row => row.isActive !== false || String(row.id) === String(data[key] ?? ""))) { const option = element("option", `${row.name || row.displayName}${row.isActive === false ? " (غیرفعال)" : ""}`); option.value = row.id; control.append(option); }
            }
            if (type === "text") control.type = "text";
            if (type === "textarea") control.rows = 4;
            if (type === "number") { control.type = "number"; control.min = key === "conversionFactor" ? "0.000000001" : "1"; control.step = key === "conversionFactor" ? "any" : "1"; if (key === "conversionFactor") control.max = "999999999.999999999"; }
            if (type === "select" || ["warehouses", "products", "units", "companies", "plans", "warehouse-types", "unit-groups"].includes(type)) control.value = data[key] == null ? "" : String(data[key]);
            else control.value = data[key] ?? (key === "conversionFactor" ? "1" : "");
            const caption = element("label", label, "form-label"); caption.htmlFor = control.id; wrap.append(caption, control);
        }
        host.append(wrap);
    }
    function addVariantAttributeRow(list, data = {}) {
        if (list.childElementCount >= 12) { ui.notify("برای هر تنوع حداکثر ۱۲ ویژگی ثبت می‌شود.", "danger"); return; }
        const row = element("div", null, "border rounded p-2");
        row.dataset.variantAttribute = "";
        const grid = element("div", null, "row g-2");
        const fields = [["key", "کلید انگلیسی", "color", 64], ["label", "نام ویژگی", "رنگ", 80], ["value", "مقدار یکتا", "black", 100], ["display", "نمایش", "مشکی", 100], ["swatch", "کد رنگ (اختیاری)", "#000000", 7]];
        for (const [key, label, placeholder, maxLength] of fields) {
            const column = element("div", null, "col-12 col-md-6");
            const input = element("input", null, "form-control form-control-sm");
            input.type = "text"; input.maxLength = maxLength; input.dataset.attributeField = key;
            input.value = data[key] ?? ""; input.placeholder = placeholder; input.setAttribute("aria-label", label);
            column.append(input); grid.append(column);
        }
        const remove = actionButton("حذف ویژگی", "fa-trash", "btn btn-sm btn-outline-danger mt-2");
        remove.addEventListener("click", () => row.remove());
        row.append(grid, remove); list.append(row);
    }
    async function openForm(page, data, config) {
        const sequence = (formRequests.get(page) || 0) + 1;
        formRequests.set(page, sequence);
        const form = document.querySelector("[data-definition-form]"), host = form.querySelector("[data-definition-fields]");
        if (data && page.dataset.definitionSection === "warehouses") data = { ...data, companyId: data.idCompany, warehouseTypeId: data.idWarehouseType, ownerSalesPlanId: data.idOwnerSalesPlanReference };
        if (data && page.dataset.definitionSection === "references") data = { ...data, warehouseId: data.idWarehouse };
        if (data && page.dataset.definitionSection === "product-units") data = { ...data, productId: data.idProduct, unitId: data.idUnit };
        if (data && page.dataset.definitionSection === "units") data = { ...data, unitGroupId: data.idUnitGroup };
        let lookups = { warehouses: [] };
        if (["references", "product-units", "variants", "warehouses"].includes(page.dataset.definitionSection)) lookups = await request(page, `${page.dataset.api}/lookups`);
        if (page.dataset.definitionSection === "warehouses") lookups["warehouse-types"] = await request(page, page.dataset.api + "/warehouse-types");
        if (page.dataset.definitionSection === "units") lookups["unit-groups"] = await request(page, page.dataset.api + "/unit-groups");
        if (!page.isConnected || formRequests.get(page) !== sequence) return;
        host.replaceChildren(); form.reset(); form.elements.id.value = data?.id || "";
        for (const field of config.fields) {
            if (field[0] === "isSiteWarehouse" && page.dataset.fullAccess !== "true") continue;
            addField(host, field, data || (page.dataset.definitionSection === "units" ? { isActive: true } : {}), lookups);
        }
        if (data?.isActive && page.dataset.definitionSection === "units" && page.dataset.canDeactivate !== "true")
            form.elements.isActive.disabled = true;
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
        const fillWarehouses = () => fill(warehouse, data.warehouses.filter(item => item.idCompany === Number(company.value)), item => `${item.companyName || data.companies.find(companyItem => companyItem.id === item.idCompany)?.name || "—"} / ${item.name} (${item.code})`);
        company.onchange = fillWarehouses; fillWarehouses();
        fill(form.elements.salesPlanId, data.plans, item => item.displayName);
        const rows = page.querySelector("[data-mapping-rows]");
        ui.destroyDataTable(page.querySelector("[data-admin-datatable]"));
        rows.replaceChildren();
        for (const link of data.links) {
            const warehouseRow = data.warehouses.find(item => item.id === link.idWarehouse);
            const planRow = data.plans.find(item => item.id === link.idStockOwner);
            if (!warehouseRow || !planRow) continue;
            const companyRow = data.companies.find(item => item.id === warehouseRow.idCompany);
            const row = element("tr");
            row.append(element("td", `${companyRow?.name || "—"} / ${warehouseRow.name} (${warehouseRow.code})`), element("td", planRow.displayName));
            const actions = element("td"), remove = actionButton("حذف اتصال", "fa-link-slash", "btn btn-sm btn-outline-danger");
            remove.dataset.adminConfirm = "اتصال این مالک به انبار حذف شود؟";
            remove.addEventListener("click", async () => {
                if (remove.disabled) return;
                remove.disabled = true;
                try { const result = await request(page, `${page.dataset.api}/sales-plan-mapping/${link.idWarehouse}/${link.idStockOwner}`, "DELETE", {}); ui.notify(result.message, "success"); await loadMapping(page); }
                catch (error) { ui.notify(error.message, "danger"); }
                finally { remove.disabled = false; }
            });
            actions.append(remove); row.append(actions);
            rows.append(row);
        }
        ui.refreshDataTable(page.querySelector("[data-admin-datatable]"));
        if (data.siteWarehouseId) { const selected = data.warehouses.find(item => item.id === data.siteWarehouseId); if (selected) { company.value = String(selected.idCompany); fillWarehouses(); warehouse.value = String(selected.id); } }
    }
    async function loadWarehouseTypes(page) {
        const types = await request(page, `${page.dataset.api}/warehouse-types`);
        const list = page.querySelector("[data-warehouse-types]");
        list.replaceChildren();
        for (const item of types) {
            const row = element("li", null, "list-group-item d-flex justify-content-between align-items-center gap-2");
            row.append(element("span", item.name));
            if (page.dataset.fullAccess === "true" && page.dataset.canEdit === "true") {
                const edit = actionButton("ویرایش", "fa-pen-to-square", "btn btn-sm btn-default");
                edit.setAttribute("aria-label", `ویرایش نام ${item.name}`);
                edit.addEventListener("click", () => {
                    const form = page.querySelector("[data-warehouse-type-form]");
                    form.hidden = false;
                    form.dataset.editingId = String(item.id);
                    form.dataset.adminConfirm = "نام انبار و همهٔ تخصیص‌های مرتبط تغییر کند؟";
                    form.elements.name.value = item.name;
                    form.querySelector('[type="submit"]').textContent = "ذخیرهٔ نام انبار";
                    form.querySelector("[data-warehouse-type-cancel]").hidden = false;
                    form.elements.name.focus();
                });
                row.append(edit);
            }
            list.append(row);
        }
        if (!types.length) list.append(element("li", "نام انباری ثبت نشده است.", "list-group-item text-muted"));
    }
    async function load(page) {
        const section = page.dataset.definitionSection;
        if (section === "sales-plan-mapping") { await loadMapping(page); setStatus(page, ""); return; }
        const config = configs[section];
        if (section === "warehouses") await loadWarehouseTypes(page);
        if (section === "units") {
            const [groups, units] = await Promise.all([
                request(page, `${page.dataset.api}/unit-groups`),
                request(page, `${page.dataset.api}/units`)
            ]);
            renderUnitTree(page, groups, units);
            return;
        }
        const rows = await request(page, `${page.dataset.api}/${section}`);
        if (section === "product-units") renderProductUnitTree(page, rows.filter(item => item.isActive === false), config);
        else renderTable(page, rows, config);
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
                if (section === "warehouses") {
                    const typeForm = page.querySelector("[data-warehouse-type-form]");
                    const resetTypeForm = () => {
                        typeForm.reset();
                        delete typeForm.dataset.editingId;
                        typeForm.dataset.adminConfirm = "نام انبار ذخیره شود؟";
                        typeForm.querySelector('[type="submit"]').textContent = "افزودن نام انبار";
                        typeForm.querySelector("[data-warehouse-type-cancel]").hidden = true;
                        typeForm.hidden = page.dataset.fullAccess !== "true" || page.dataset.canCreate !== "true";
                    };
                    resetTypeForm();
                    typeForm.querySelector("[data-warehouse-type-cancel]").addEventListener("click", resetTypeForm);
                    typeForm.addEventListener("submit", async event => {
                        event.preventDefault();
                        const button = typeForm.querySelector('[type="submit"]');
                        if (button.disabled) return;
                        const id = typeForm.dataset.editingId;
                        if (page.dataset.fullAccess !== "true" || (id ? page.dataset.canEdit !== "true" : page.dataset.canCreate !== "true")) return;
                        const name = typeForm.elements.name.value.trim();
                        if (!name) { ui.notify("نام انبار را وارد کنید.", "danger"); typeForm.elements.name.focus(); return; }
                        button.disabled = true;
                        try { const result = await request(page, page.dataset.api + `/warehouse-types${id ? `/${encodeURIComponent(id)}` : ""}`, id ? "PUT" : "POST", { name }); ui.notify(result.message, "success"); resetTypeForm(); await load(page); }
                        catch (error) { ui.notify(error.message, "danger"); }
                        finally { button.disabled = false; }
                    });
                }
                if (section === "units") {
                    const groupForm = page.querySelector("[data-unit-group-form]");
                    const resetGroupForm = () => {
                        groupForm.reset(); groupForm.elements.id.value = ""; groupForm.elements.sortOrder.value = "0";
                        groupForm.querySelector('[type="submit"]').textContent = "افزودن گروه";
                        groupForm.querySelector("[data-unit-group-cancel]").hidden = true;
                    };
                    groupForm.hidden = page.dataset.canCreate !== "true" && page.dataset.canEdit !== "true";
                    groupForm.querySelector("[data-unit-group-cancel]").addEventListener("click", resetGroupForm);
                    groupForm.addEventListener("submit", async event => {
                        event.preventDefault(); if (groupForm.dataset.saving === "true") return;
                        const id = groupForm.elements.id.value, name = groupForm.elements.name.value.trim();
                        if (!name) { ui.notify("نام گروه را وارد کنید.", "danger"); groupForm.elements.name.focus(); return; }
                        if (page.dataset.canCreate !== "true" && !id || id && page.dataset.canEdit !== "true") return;
                        const button = groupForm.querySelector('[type="submit"]'); groupForm.dataset.saving = "true"; button.disabled = true;
                        try {
                            const result = await request(page, `${page.dataset.api}/unit-groups${id ? `/${id}` : ""}`, id ? "PUT" : "POST", { name, sortOrder: Number(groupForm.elements.sortOrder.value) || 0, isActive: true });
                            ui.notify(result.message, "success"); resetGroupForm(); await load(page);
                        } catch (error) { ui.notify(error.message, "danger"); }
                        finally { groupForm.dataset.saving = "false"; button.disabled = false; }
                    });
                }
                const config = configs[section];
                page.querySelector("[data-reference-type-filter]")?.addEventListener("change", () => void load(page));
                const createButton = page.querySelector("[data-definition-create]");
                createButton.hidden = page.dataset.canCreate !== "true";
                createButton.addEventListener("click", () => void openForm(page, null, config).catch(error => ui.notify(error.message || "بازکردن فرم ناموفق بود.", "danger")));
                form.addEventListener("submit", async event => {
                    event.preventDefault();
                    if (form.dataset.saving === "true") return;
                    for (const [key, label, type, required] of config.fields) {
                        if (type === "attributes") continue;
                        const control = form.elements.namedItem(key);
                        if (required && type === "text" && control && !control.value.trim()) {
                            ui.notify(`${label} را وارد کنید.`, "danger"); control.focus(); return;
                        }
                    }
                    const values = new FormData(form), id = values.get("id"); const body = {};
                    for (const [key, , type] of config.fields) {
                        if (type === "attributes") continue;
                        body[key] = type === "checkbox" ? Boolean(form.elements.namedItem(key)?.checked) : (key === "idStockOwnerType" || ["number", "products", "units", "companies", "plans", "warehouse-types", "unit-groups"].includes(type) ? Number(values.get(key)) || null : values.get(key));
                    }
                    if (section === "variants") body.attributes = [...form.querySelectorAll("[data-variant-attribute]")].map(row => Object.fromEntries([...row.querySelectorAll("[data-attribute-field]")].map(input => [input.dataset.attributeField, input.value.trim()])));
                    if (section !== "units") body.isActive = true;
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
