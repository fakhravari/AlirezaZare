(() => {
    "use strict";
    const ui = window.AdminUI;
    if (!ui) return;
    const initialized = new WeakSet();
    const formRequests = new WeakMap();
    const variantRows = new WeakMap();
    const variantNames = new WeakMap();
    const fmt = new Intl.NumberFormat("fa-IR");
    const conversionFmt = new Intl.NumberFormat("fa-IR", { maximumFractionDigits: 9 });
    const configs = {
        "stock-owners": { fields: [["code", "کد", "text", true], ["displayName", "نام", "text", true], ["idStockOwnerType", "نوع", "select", true, [["1", "طرح فروش"], ["2", "مالک"]]]], columns: [["code", "کد"], ["displayName", "نام"], ["idStockOwnerType", "نوع"], ["isActive", "وضعیت", "active"]] },
        companies: { fields: [["code", "کد شرکت", "text", true], ["name", "نام شرکت", "text", true], ["phone", "تلفن", "text"], ["address", "آدرس", "textarea"], ["description", "توضیحات", "textarea"]], columns: [["code", "کد"], ["name", "شرکت"], ["phone", "تلفن"], ["address", "آدرس"], ["description", "توضیحات"], ["isActive", "وضعیت", "active"]] },
        warehouses: { fields: [["companyId", "شرکت", "companies", true], ["code", "کد انبار", "text", true], ["name", "نام انبار", "text", true], ["isSalesWarehouse", "انبار فروش است", "checkbox"], ["ownerSalesPlanId", "مالک پیش‌فرض انبار", "plans"], ["isSiteWarehouse", "منبع موجودی سایت", "checkbox"]], columns: [["warehouseLabel", "شرکت / انبار"], ["isSalesWarehouse", "انبار فروش", "affects"], ["isSiteWarehouse", "منبع موجودی سایت", "siteWarehouse"], ["ownerSalesPlan", "مالک پیش‌فرض"], ["isActive", "وضعیت", "active"]] },
        references: { fields: [["name", "نام مرجع", "text", true], ["partyType", "نوع مرجع", "select", true, [["Person", "شخص"], ["Organization", "سازمان"], ["Warehouse", "انبار"], ["Other", "سایر"]]], ["warehouseId", "انبار مرتبط", "warehouses"], ["externalPartyKey", "شناسهٔ بیرونی", "text"]], columns: [["name", "نام"], ["partyType", "نوع"], ["idWarehouse", "شناسهٔ انبار"], ["isActive", "وضعیت", "active"]] },
        units: { fields: [["parentId", "گروه والد", "unit-groups", true], ["nameFa", "نام فارسی", "text", true], ["nameEn", "نام انگلیسی", "text"], ["isActive", "واحد فعال است", "checkbox"]], columns: [["parentName", "گروه والد"], ["nameFa", "نام فارسی"], ["nameEn", "نام انگلیسی"], ["isActive", "وضعیت", "active"]] },
        "document-types": { fields: [["code", "کد نوع سند", "text", true], ["name", "نام سند", "text", true], ["direction", "جهت سند", "select", true, [["R", "رسید"], ["I", "حواله"]]], ["affectsStock", "بر موجودی اثر دارد", "checkbox" ]], columns: [["code", "کد"], ["name", "نوع سند"], ["direction", "جهت", "direction"], ["affectsStock", "اثر موجودی", "affects"], ["isActive", "وضعیت", "active"]] }
        ,"product-units": { fields: [["productId", "کالا", "products", true], ["unitId", "واحد", "units", true], ["conversionFactor", "ضریب تبدیل به واحد اصلی", "number", true], ["isBaseUnit", "واحد اصلی کالا", "checkbox"]], columns: [["productName", "کالا"], ["unitName", "واحد"], ["conversionFactor", "ضریب تبدیل"], ["isBaseUnit", "اصلی", "base"], ["isActive", "وضعیت", "active"]] },
        variants: { fields: [["productId", "کالا", "products", true], ["variantNameId", "نام تنوع", "variant-names", true], ["attributes", "ویژگی‌های تنوع", "attributes"], ["isDefault", "تنوع پیش‌فرض", "checkbox"]], columns: [["productName", "کالا"], ["name", "تنوع"], ["attributes", "ویژگی‌ها", "attributes"], ["createdAtUtc", "تاریخ ثبت", "date"], ["isDefault", "پیش‌فرض", "default"], ["isActive", "وضعیت", "active"]] }
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
        const productFilter = page.querySelector("[data-variant-product-filter]")?.value || "";
        const statusFilter = page.querySelector("[data-variant-status-filter]")?.value || "";
        const visibleRows = rows.filter(item =>
            (page.dataset.definitionSection !== "references" || !referenceType || item.partyType === referenceType) &&
            (page.dataset.definitionSection !== "variants" || ((!productFilter || String(item.idProduct) === productFilter) &&
                (!statusFilter || item.isActive === (statusFilter === "active")))));
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
                if (format === "date") value = value ? ui.formatDateTime(value) : "—";
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
                if (page.dataset.definitionSection === "variants" && (item.isDefault || !rows.some(other => other.idProduct === item.idProduct && other.id !== item.id && other.isActive))) {
                    deactivate.disabled = true;
                    deactivate.title = item.isDefault
                        ? "تنوع پیش‌فرض قابل غیرفعال‌سازی نیست؛ ابتدا تنوع فعال دیگری را پیش‌فرض کنید."
                        : "کالای دارای تنوع باید حداقل یک تنوع فعال داشته باشد.";
                    deactivate.setAttribute("aria-label", deactivate.title);
                }
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
        setStatus(page, visibleRows.length ? "" : (referenceType ? "مرجعی با این نوع پیدا نشد." : (productFilter || statusFilter) ? "تنوعی با این فیلترها پیدا نشد." : "موردی ثبت نشده است."));
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
        tree.setAttribute("role", "tree");
        const groupsByParent = new Map();
        const unitsByParent = new Map();
        for (const group of groups) {
            const key = group.idParent == null ? "root" : String(group.idParent);
            if (!groupsByParent.has(key)) groupsByParent.set(key, []);
            groupsByParent.get(key).push(group);
        }
        for (const unit of units) {
            const key = unit.idParent == null ? "root" : String(unit.idParent);
            if (!unitsByParent.has(key)) unitsByParent.set(key, []);
            unitsByParent.get(key).push(unit);
        }
        const groupById = new Map(groups.map(group => [String(group.id), group]));
        const updateGroupAvailability = group => {
            const node = [...tree.querySelectorAll("[data-unit-group-id]")].find(item => item.dataset.unitGroupId === String(group.id));
            if (!node) return;
            const activeMemberCount = (unitsByParent.get(String(group.id)) || []).filter(unit => unit.isActive).length;
            const status = node.querySelector(":scope > .admin-permission-row [data-unit-group-status]");
            const warning = node.querySelector(":scope > .admin-permission-row [data-group-blocked]");
            const label = node.querySelector(":scope > .admin-permission-row [data-unit-group-label]");
            const reason = activeMemberCount ? `برای غیرفعال‌کردن گروه، ${fmt.format(activeMemberCount)} واحد فعال را به گروه دیگری منتقل یا غیرفعال کنید.` : "";
            const canToggle = group.isActive
                ? page.dataset.canDeactivate === "true" && activeMemberCount === 0
                : page.dataset.canEdit === "true";
            status.disabled = !canToggle;
            status.dataset.blocked = String(Boolean(group.isActive && activeMemberCount));
            status.title = reason;
            status.setAttribute("aria-label", `${group.isActive ? "غیرفعال‌کردن" : "فعال‌کردن"} ${group.name}`);
            label.textContent = `${group.name}${group.isActive ? "" : " · غیرفعال"} (${fmt.format((groupsByParent.get(String(group.id)) || []).length)} زیرگروه · ${fmt.format((unitsByParent.get(String(group.id)) || []).length)} واحد)`;
            if (group.isActive) status.dataset.adminConfirm = "گروه واحد غیرفعال شود؟ گروه دارای واحد فعال قابل غیرفعال‌سازی نیست.";
            else delete status.dataset.adminConfirm;
            if (warning) { warning.textContent = reason; warning.hidden = !reason; }
        };
        const makeRow = (parent, label, active, toggleLabel, toggleUrl, toggleBody, canToggle, editHandler = null, disabledReason = "", displayLabel = () => label, onStatusChange = null, statusDataAttribute = "", deleteOptions = null) => {
            const row = element("div", null, "admin-permission-row");
            const children = parent.querySelector(":scope > ul[role=group]");
            if (children) {
                const expand = element("button", null, "admin-permission-toggle");
                expand.type = "button"; expand.setAttribute("aria-expanded", "false"); expand.setAttribute("aria-label", `بازکردن ${label}`);
                expand.append(element("span", "›", "admin-permission-chevron"));
                expand.addEventListener("click", () => {
                    const open = expand.getAttribute("aria-expanded") !== "true";
                    expand.setAttribute("aria-expanded", String(open));
                    expand.setAttribute("aria-label", `${open ? "بستن" : "بازکردن"} ${label}`);
                    parent.setAttribute("aria-expanded", String(open));
                    children.hidden = !open;
                });
                row.append(expand);
            } else row.append(element("span", null, "admin-permission-toggle-spacer"));
            const status = element("button", null, "admin-unit-tree-status");
            status.type = "button"; status.setAttribute("role", "checkbox"); status.setAttribute("aria-checked", String(Boolean(active)));
            let isActive = Boolean(active);
            const toggleAllowed = () => typeof canToggle === "function" ? canToggle(isActive) : canToggle;
            if (statusDataAttribute) status.setAttribute(statusDataAttribute, "");
            status.setAttribute("aria-label", `${active ? "غیرفعال‌کردن" : "فعال‌کردن"} ${label}`);
            status.disabled = !toggleAllowed();
            if (disabledReason) status.title = disabledReason;
            if (isActive) status.dataset.adminConfirm = toggleLabel;
            status.addEventListener("click", async () => {
                if (!toggleAllowed()) return;
                status.disabled = true;
                try {
                    const url = typeof toggleUrl === "function" ? toggleUrl(isActive) : toggleUrl;
                    const result = await request(page, url, isActive ? "POST" : "PUT", isActive ? {} : toggleBody);
                    isActive = !isActive;
                    onStatusChange?.(isActive);
                    status.setAttribute("aria-checked", String(isActive));
                    status.setAttribute("aria-label", `${isActive ? "غیرفعال‌کردن" : "فعال‌کردن"} ${displayLabel()}`);
                    if (isActive) status.dataset.adminConfirm = toggleLabel;
                    else delete status.dataset.adminConfirm;
                    choice.textContent = displayLabel();
                    ui.notify(result.message, "success");
                } catch (error) { ui.notify(error.message, "danger"); }
                finally { if (status.isConnected) status.disabled = !toggleAllowed(); }
            });
            const choice = element("span", displayLabel(), "admin-permission-name flex-grow-1");
            if (statusDataAttribute) choice.dataset.unitGroupLabel = "";
            row.append(status, choice);
            if (disabledReason) {
                const note = element("small", disabledReason, "text-warning-emphasis");
                note.dataset.groupBlocked = "";
                row.append(note);
            }
            if (editHandler) {
                const edit = actionButton("ویرایش", "fa-pen-to-square", "btn btn-sm btn-default");
                edit.addEventListener("click", editHandler);
                row.append(edit);
            }
            if (deleteOptions?.canDelete && page.dataset.canDelete === "true") {
                const remove = actionButton("حذف", "fa-trash", "btn btn-sm btn-outline-danger");
                remove.dataset.adminConfirm = deleteOptions.confirmMessage;
                remove.addEventListener("click", async () => {
                    if (remove.disabled) return;
                    remove.disabled = true;
                    try {
                        const result = await request(page, deleteOptions.url, "DELETE");
                        deleteOptions.onDelete?.();
                        parent.remove();
                        ui.notify(result.message, "success");
                    } catch (error) { ui.notify(error.message, "danger"); }
                    finally { remove.disabled = false; }
                });
                row.append(remove);
            }
            const childList = parent.querySelector(":scope > ul[role=group]");
            if (childList) parent.insertBefore(row, childList);
            else parent.append(row);
        };
        const appendGroup = (group, container) => {
            const childGroups = groupsByParent.get(String(group.id)) || [];
            const members = unitsByParent.get(String(group.id)) || [];
            const activeMemberCount = members.filter(unit => unit.isActive).length;
            const blockedMessage = activeMemberCount
                ? `برای غیرفعال‌کردن گروه، ${fmt.format(activeMemberCount)} واحد فعال را به گروه دیگری منتقل یا غیرفعال کنید.`
                : "";
            const node = element("li", null, "admin-permission-node"); node.setAttribute("role", "treeitem"); node.setAttribute("aria-expanded", "false");
            const children = element("ul", null, "admin-permission-list"); children.setAttribute("role", "group"); children.hidden = true;
            node.append(children);
            node.dataset.unitGroupId = String(group.id);
            const groupLabel = () => `${group.name}${group.isActive ? "" : " · غیرفعال"} (${fmt.format(childGroups.length)} زیرگروه · ${fmt.format(members.length)} واحد)`;
            makeRow(node, group.name, group.isActive,
                "گروه واحد غیرفعال شود؟ گروه دارای واحد فعال قابل غیرفعال‌سازی نیست.", isActive => `${page.dataset.api}/unit-groups/${group.id}${isActive ? "/deactivate" : ""}`,
                { name: group.name, nameEn: group.nameEn, sortOrder: group.sortOrder, isActive: true },
                active => active
                    ? page.dataset.canDeactivate === "true" && members.filter(unit => unit.isActive).length === 0
                    : page.dataset.canEdit === "true",
                page.dataset.canEdit === "true" ? () => {
                    const form = page.querySelector("[data-unit-group-form]");
                    form.elements.id.value = group.id; form.elements.name.value = group.name; form.elements.nameEn.value = group.nameEn || "";
                    form.elements.sortOrder.value = group.sortOrder;
                    form.querySelector('[type="submit"]').textContent = "ذخیرهٔ گروه"; form.querySelector("[data-unit-group-cancel]").hidden = false;
                    form.elements.name.focus();
                } : null, blockedMessage, groupLabel,
                isActive => { group.isActive = isActive; updateGroupAvailability(group); }, "data-unit-group-status",
                { canDelete: group.canDelete, url: `${page.dataset.api}/unit-groups/${group.id}`, confirmMessage: `گروه «${group.name}» برای همیشه حذف شود؟`, onDelete: () => { const index = groups.indexOf(group); if (index >= 0) groups.splice(index, 1); } });
            updateGroupAvailability(group);
            for (const childGroup of childGroups) appendGroup(childGroup, children);
            for (const unit of members) appendUnit(unit, children);
            if (!childGroups.length && !members.length) children.append(element("li", "زیرگروه یا واحدی ثبت نشده است.", "admin-permission-node text-muted"));
            node.append(children); container.append(node);
        };
        const appendUnit = (unit, container) => {
            const node = element("li", null, "admin-permission-node"); node.setAttribute("role", "treeitem");
            const unitLabel = () => `${unit.nameFa}${unit.nameEn ? ` / ${unit.nameEn}` : ""}${unit.isActive ? "" : " · غیرفعال"}`;
            makeRow(node, unit.nameFa, unit.isActive,
                "این واحد غیرفعال شود؟ واحد مورد استفادهٔ کالا غیرفعال نمی‌شود.", isActive => `${page.dataset.api}/units/${unit.id}${isActive ? "/deactivate" : ""}`,
                { nameFa: unit.nameFa, nameEn: unit.nameEn, parentId: unit.idParent, isActive: true },
                active => active ? page.dataset.canDeactivate === "true" : page.dataset.canEdit === "true",
                page.dataset.canEdit === "true" ? () => void openForm(page, unit, configs.units).catch(error => ui.notify(error.message || "بازکردن فرم ناموفق بود.", "danger")) : null,
                "", unitLabel, isActive => {
                    unit.isActive = isActive;
                    const parentGroup = groupById.get(String(unit.idParent));
                    if (parentGroup) updateGroupAvailability(parentGroup);
                }, "", { canDelete: unit.canDelete, url: `${page.dataset.api}/units/${unit.id}`, confirmMessage: `واحد «${unit.nameFa}» برای همیشه حذف شود؟`, onDelete: () => {
                    const members = unitsByParent.get(String(unit.idParent)) || [];
                    const index = members.indexOf(unit); if (index >= 0) members.splice(index, 1);
                    const allIndex = units.indexOf(unit); if (allIndex >= 0) units.splice(allIndex, 1);
                    const parentGroup = groupById.get(String(unit.idParent));
                    if (parentGroup) updateGroupAvailability(parentGroup);
                } });
            container.append(node);
        };
        const roots = element("ul", null, "admin-permission-list"); roots.setAttribute("role", "group");
        for (const group of groupsByParent.get("root") || []) appendGroup(group, roots);
        const ungrouped = units.filter(unit => unit.idParent == null);
        if (ungrouped.length) {
            const section = element("li", null, "admin-permission-node"); section.setAttribute("role", "treeitem");
            const children = element("ul", null, "admin-permission-list"); children.setAttribute("role", "group");
            section.append(children);
            makeRow(section, "واحدهای بدون گروه والد", true, "", "", {}, false);
            for (const unit of ungrouped) appendUnit(unit, children);
            roots.append(section);
        }
        tree.append(roots);
        setStatus(page, groups.length || units.length ? "" : "گروهی تعریف نشده است.");
    }
    function addField(host, field, data, lookups, variantTree = []) {
        const [key, label, type = "text", required = false, choices = []] = field;
        const wrap = element("div", null, "mb-3");
        if (type === "attributes") {
            const caption = element("label", label, "form-label");
            caption.id = "definition-attributes-label";
            const list = element("div", null, "d-grid gap-2");
            list.dataset.variantAttributes = "";
            list.setAttribute("aria-labelledby", caption.id);
            for (const attribute of Array.isArray(data[key]) ? data[key] : []) addVariantAttributeRow(list, variantTree, attribute);
            const add = actionButton("افزودن ویژگی", "fa-plus", "btn btn-sm btn-outline-primary mt-2");
            add.disabled = variantTree.length === 0;
            add.onclick = () => addVariantAttributeRow(list, variantTree);
            wrap.append(caption, list, add);
            host.append(wrap);
            return;
        }
        if (type === "variant-names") {
            const control = element("select", null, "form-select");
            control.name = key; control.id = `definition-${key}`; control.required = required;
            control.append(new Option("انتخاب نام تنوع", ""));
            for (const item of lookups.variantNames || []) if (item.isActive || String(item.id) === String(data[key] ?? ""))
                control.add(new Option(variantNamePath(lookups.variantNames, item.id), String(item.id)));
            control.value = data[key] == null ? "" : String(data[key]);
            const caption = element("label", label, "form-label"); caption.htmlFor = control.id; wrap.append(caption, control);
            host.append(wrap); return;
        }
        if (type === "checkbox") {
            const check = element("input", null, "form-check-input"); check.type = "checkbox"; ui.initStatusSwitch(check); check.name = key; check.checked = Boolean(data[key]); check.id = `definition-${key}`;
            const caption = element("label", label, "form-check-label"); caption.htmlFor = check.id; wrap.className = "form-check mb-3"; wrap.append(check, caption);
        } else {
            const control = ["select", "warehouses", "products", "units", "companies", "plans", "unit-groups"].includes(type) ? element("select", null, "form-select") : type === "textarea" ? element("textarea", null, "form-control") : element("input", null, "form-control");
            if (type === "product-fixed") {
                const product = (lookups.products || []).find(item => String(item.id) === String(data[key] ?? ""));
                control.id = `definition-${key}`;
                const caption = element("label", label, "form-label"); caption.htmlFor = control.id;
                control.removeAttribute("name"); control.type = "text"; control.readOnly = true; control.value = product?.name || data.productName || "کالای ثبت‌شده";
                const id = element("input"); id.type = "hidden"; id.name = key; id.value = data[key] ?? "";
                wrap.append(caption, control, id); host.append(wrap); return;
            }
            control.name = key; control.id = `definition-${key}`; control.required = required;
            if (type === "select") {
                const empty = element("option", "انتخاب کنید"); empty.value = ""; control.append(empty);
                for (const [value, name] of choices) { const option = element("option", name); option.value = value; control.append(option); }
            }
            if (["warehouses", "products", "units", "companies", "plans", "unit-groups"].includes(type)) {
                const choices = type === "warehouses" ? lookups.warehouses : (lookups[type] || []);
                const empty = element("option", type === "warehouses" ? "بدون انبار مرتبط" : "انتخاب کنید"); empty.value = ""; control.append(empty);
                for (const row of choices.filter(row => row.isActive !== false || String(row.id) === String(data[key] ?? ""))) { const option = element("option", `${row.name || row.displayName}${row.isActive === false ? " (غیرفعال)" : ""}`); option.value = row.id; control.append(option); }
            }
            if (type === "text") control.type = "text";
            if (type === "textarea") control.rows = 4;
            if (type === "number") { control.type = "number"; control.min = key === "conversionFactor" ? "0.000000001" : "1"; control.step = key === "conversionFactor" ? "any" : "1"; if (key === "conversionFactor") control.max = "999999999.999999999"; }
            if (type === "select" || ["warehouses", "products", "units", "companies", "plans", "unit-groups"].includes(type)) control.value = data[key] == null ? "" : String(data[key]);
            else control.value = data[key] ?? (key === "conversionFactor" ? "1" : "");
            const caption = element("label", label, "form-label"); caption.htmlFor = control.id; wrap.append(caption, control);
        }
        host.append(wrap);
    }
    function addVariantAttributeRow(list, tree, data = {}) {
        if (list.childElementCount >= 12) { ui.notify("برای هر تنوع حداکثر ۱۲ ویژگی ثبت می‌شود.", "danger"); return; }
        const row = element("div", null, "border rounded p-2");
        row.dataset.variantAttribute = "";
        const grid = element("div", null, "row g-2");
        const attributeSelect = element("select", null, "form-select form-select-sm");
        attributeSelect.dataset.attributeField = "attributeId"; attributeSelect.setAttribute("aria-label", "ویژگی تنوع");
        const valueSelect = element("select", null, "form-select form-select-sm");
        valueSelect.dataset.attributeField = "valueId"; valueSelect.setAttribute("aria-label", "مقدار ویژگی");
        const fillValues = selectedId => {
            valueSelect.replaceChildren();
            const placeholder = element("option", tree.length ? "انتخاب مقدار" : "ابتدا کالایی با درخت تنوع انتخاب کنید"); placeholder.value = ""; valueSelect.append(placeholder);
            const attribute = tree.find(item => String(item.id) === String(attributeSelect.value));
            for (const item of attribute?.values || []) { const option = element("option", item.label || item.key); option.value = item.id; valueSelect.append(option); }
            valueSelect.value = selectedId == null ? "" : String(selectedId);
            valueSelect.disabled = !attribute;
        };
        const emptyAttribute = element("option", tree.length ? "انتخاب ویژگی" : "درخت تنوعی برای این کالا ثبت نشده است"); emptyAttribute.value = ""; attributeSelect.append(emptyAttribute);
        for (const item of tree) { const option = element("option", item.label); option.value = item.id; attributeSelect.append(option); }
        attributeSelect.value = data.attributeId == null ? "" : String(data.attributeId);
        attributeSelect.addEventListener("change", () => fillValues(null));
        fillValues(data.valueId);
        for (const [control, label] of [[attributeSelect, "ویژگی"], [valueSelect, "مقدار"]]) {
            const column = element("div", null, "col-12 col-md-6");
            const caption = element("label", label, "form-label small"); caption.htmlFor = `variant-attribute-${label}-${Math.random().toString(36).slice(2)}`;
            control.id = caption.htmlFor; column.append(caption, control); grid.append(column);
        }
        const remove = actionButton("حذف ویژگی", "fa-trash", "btn btn-sm btn-outline-danger mt-2");
        remove.addEventListener("click", () => row.remove());
        row.append(grid, remove); list.append(row);
    }
    async function openForm(page, data, config) {
        const sequence = (formRequests.get(page) || 0) + 1;
        formRequests.set(page, sequence);
        const form = document.querySelector("[data-definition-form]"), host = form.querySelector("[data-definition-fields]");
        if (data && page.dataset.definitionSection === "warehouses") data = { ...data, companyId: data.idCompany, ownerSalesPlanId: data.defaultOwnerSalesPlanId };
        if (data && page.dataset.definitionSection === "references") data = { ...data, warehouseId: data.idWarehouse };
        if (data && page.dataset.definitionSection === "product-units") data = { ...data, productId: data.idProduct, unitId: data.idUnit };
        if (data && page.dataset.definitionSection === "units") data = { ...data, parentId: data.idParent };
        if (data && page.dataset.definitionSection === "variants") data = { ...data, productId: data.idProduct, variantNameId: data.idVariantName };
        let lookups = { warehouses: [] };
        if (["references", "product-units", "variants", "warehouses"].includes(page.dataset.definitionSection)) lookups = await request(page, `${page.dataset.api}/lookups`);
        if (page.dataset.definitionSection === "variants") lookups.variantNames = variantNames.get(page) || [];
        let variantTree = [];
        if (page.dataset.definitionSection === "variants" && data?.productId)
            variantTree = await request(page, `${page.dataset.api}/variant-options/${encodeURIComponent(data.productId)}${data.id ? `?variantId=${encodeURIComponent(data.id)}` : ""}`);
        if (page.dataset.definitionSection === "units") lookups["unit-groups"] = await request(page, page.dataset.api + "/unit-groups");
        if (!page.isConnected || formRequests.get(page) !== sequence) return;
        host.replaceChildren(); form.reset(); form.elements.id.value = data?.id || "";
        for (const originalField of config.fields) {
            const field = page.dataset.definitionSection === "variants" && data?.id && originalField[0] === "productId"
                ? [originalField[0], originalField[1], "product-fixed", false] : originalField;
            if (field[0] === "isSiteWarehouse" && page.dataset.fullAccess !== "true") continue;
            addField(host, field, data || (page.dataset.definitionSection === "units" ? { isActive: true } : {}), lookups, variantTree);
        }
        if (page.dataset.definitionSection === "variants" && !data?.id) {
            const product = form.elements.namedItem("productId"), list = host.querySelector("[data-variant-attributes]"), add = list?.parentElement?.querySelector("button");
            let treeRequest = 0;
            product?.addEventListener("change", async () => {
                const requestId = ++treeRequest;
                list?.replaceChildren(); variantTree = [];
                if (add) add.disabled = true;
                try {
                    if (product.value) {
                        const result = await request(page, `${page.dataset.api}/variant-options/${encodeURIComponent(product.value)}`);
                        if (requestId === treeRequest && String(product.value) === String(form.elements.namedItem("productId")?.value)) variantTree = result;
                    }
                }
                catch (error) { ui.notify(error.message || "دریافت درخت تنوع ناموفق بود.", "danger"); }
                if (requestId !== treeRequest) return;
                if (add) add.disabled = variantTree.length === 0;
                if (list && add) add.onclick = () => addVariantAttributeRow(list, variantTree);
            });
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
        const rows = page.querySelector("[data-mapping-rows]"), warehouseFilter = page.querySelector("[data-mapping-warehouse-filter]");
        const selectedWarehouse = warehouseFilter.value;
        warehouseFilter.replaceChildren(element("option", "همهٔ انبارها"));
        warehouseFilter.options[0].value = "";
        for (const item of data.warehouses) {
            const companyName = item.companyName || data.companies.find(companyItem => companyItem.id === item.idCompany)?.name || "—";
            const option = element("option", `${companyName} / ${item.name} (${item.code})`);
            option.value = item.id;
            warehouseFilter.append(option);
        }
        warehouseFilter.value = data.warehouses.some(item => String(item.id) === selectedWarehouse) ? selectedWarehouse : "";
        const renderRows = () => {
            rows.replaceChildren();
            for (const link of data.links) {
                if (warehouseFilter.value && String(link.idWarehouse) !== warehouseFilter.value) continue;
                const warehouseRow = data.warehouses.find(item => item.id === link.idWarehouse);
                const planRow = data.plans.find(item => item.id === link.idStockOwner);
                if (!warehouseRow || !planRow) continue;
                const companyRow = data.companies.find(item => item.id === warehouseRow.idCompany);
                const row = element("tr");
                row.append(element("td", `${companyRow?.name || "—"} / ${warehouseRow.name} (${warehouseRow.code})`), element("td", planRow.displayName), element("td", link.isDefault ? "پیش‌فرض" : ""));
                const actions = element("td"), remove = actionButton("حذف اتصال", "fa-link-slash", "btn btn-sm btn-outline-danger");
                remove.dataset.adminConfirm = "اتصال این مالک به انبار حذف شود؟";
                remove.addEventListener("click", async () => {
                    if (remove.disabled) return;
                    remove.disabled = true;
                    try { const result = await request(page, `${page.dataset.api}/sales-plan-mapping/${link.idWarehouse}/${link.idStockOwner}`, "DELETE", {}); ui.notify(result.message, "success"); await loadMapping(page); }
                    catch (error) { ui.notify(error.message, "danger"); }
                    finally { remove.disabled = false; }
                });
                if (!link.isDefault) { const makeDefault = actionButton("پیش‌فرض‌کردن", "fa-star", "btn btn-sm btn-outline-primary me-2"); makeDefault.addEventListener("click", async () => { try { const result = await request(page, `${page.dataset.api}/sales-plan-mapping/${link.idWarehouse}/${link.idStockOwner}/default`, "PUT", {}); ui.notify(result.message, "success"); await loadMapping(page); } catch (error) { ui.notify(error.message, "danger"); } }); actions.append(makeDefault); }
                actions.append(remove); row.append(actions);
                rows.append(row);
            }
        };
        warehouseFilter.onchange = renderRows;
        renderRows();
        if (data.siteWarehouseId) { const selected = data.warehouses.find(item => item.id === data.siteWarehouseId); if (selected) { company.value = String(selected.idCompany); fillWarehouses(); warehouse.value = String(selected.id); } }
    }
    function variantNamePath(nodes, id) {
        const byId = new Map(nodes.map(node => [String(node.id), node]));
        const parts = [], seen = new Set();
        let node = byId.get(String(id));
        while (node && !seen.has(String(node.id))) {
            seen.add(String(node.id)); parts.unshift(node.name);
            node = node.idParent == null ? null : byId.get(String(node.idParent));
        }
        return parts.join(" / ");
    }
    async function loadVariantNames(page) {
        const nodes = await request(page, `${page.dataset.api}/variant-names`);
        variantNames.set(page, nodes);
        const form = page.querySelector("[data-variant-name-form]"), tree = page.querySelector("[data-variant-name-tree]");
        if (!form || !tree) return;
        const parent = form.elements.idParent, selected = parent.value;
        parent.replaceChildren(new Option("ریشه", ""));
        for (const node of nodes) if (node.isActive) parent.add(new Option(variantNamePath(nodes, node.id), node.id));
        parent.value = nodes.some(node => String(node.id) === selected) ? selected : "";
        window.jQuery?.(parent).trigger("change.select2");
        tree.replaceChildren();
        const appendBranch = (parentId, host) => {
            for (const node of nodes.filter(item => item.idParent === parentId)) {
                const li = element("li", null, "list-group-item");
                const heading = element("div", null, "d-flex align-items-center justify-content-between gap-2");
                heading.append(element("span", node.name));
                if (page.dataset.canEdit === "true") {
                    const edit = actionButton("ویرایش", "fa-pen-to-square", "btn btn-sm btn-default");
                    edit.addEventListener("click", () => { form.elements.id.value = node.id; form.elements.name.value = node.name; parent.value = node.idParent ?? ""; window.jQuery?.(parent).trigger("change.select2"); form.querySelector("[data-variant-name-cancel]").hidden = false; form.elements.name.focus(); });
                    heading.append(edit);
                }
                li.append(heading);
                const children = element("ul", null, "list-group mt-2");
                appendBranch(node.id, children);
                if (children.childElementCount) li.append(children);
                host.append(li);
            }
        };
        appendBranch(null, tree);
    }
    async function load(page) {
        const section = page.dataset.definitionSection;
        if (section === "sales-plan-mapping") { await loadMapping(page); setStatus(page, ""); return; }
        const config = configs[section];
        if (section === "units") {
            const [groups, units] = await Promise.all([
                request(page, `${page.dataset.api}/unit-groups`),
                request(page, `${page.dataset.api}/units`)
            ]);
            renderUnitTree(page, groups, units);
            return;
        }
        const rows = await request(page, `${page.dataset.api}/${section}`);
        if (section === "variants") {
            variantRows.set(page, rows);
            const product = page.querySelector("[data-variant-product-filter]");
            if (product) {
                const selected = product.value;
                const products = new Map(rows.map(item => [String(item.idProduct), item.productName]));
                product.replaceChildren(new Option("همهٔ کالاها", ""));
                for (const [id, name] of [...products].sort((a, b) => a[1].localeCompare(b[1], "fa"))) product.add(new Option(name, id));
                if (products.has(selected)) product.value = selected;
                window.jQuery?.(product).trigger("change.select2");
            }
        }
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
                mapping.addEventListener("submit", async event => { event.preventDefault(); if (mapping.dataset.saving === "true") return; const submit = mapping.querySelector('[type="submit"]'); mapping.dataset.saving = "true"; submit.disabled = true; try { const result = await request(page, `${page.dataset.api}/sales-plan-mapping`, "PUT", { companyId: Number(mapping.elements.companyId.value), warehouseId: Number(mapping.elements.warehouseId.value), salesPlanId: Number(mapping.elements.salesPlanId.value), isDefault: mapping.elements.isDefault.checked }); ui.notify(result.message, "success"); await loadMapping(page); } catch (error) { ui.notify(error.message, "danger"); } finally { mapping.dataset.saving = "false"; submit.disabled = false; } });
            } else {
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
                            const result = await request(page, `${page.dataset.api}/unit-groups${id ? `/${id}` : ""}`, id ? "PUT" : "POST", { name, nameEn: groupForm.elements.nameEn.value.trim() || null, sortOrder: Number(groupForm.elements.sortOrder.value) || 0, isActive: true });
                            ui.notify(result.message, "success"); resetGroupForm(); await load(page);
                        } catch (error) { ui.notify(error.message, "danger"); }
                        finally { groupForm.dataset.saving = "false"; button.disabled = false; }
                    });
                }
                if (section === "variants") {
                    await loadVariantNames(page);
                    const nameForm = page.querySelector("[data-variant-name-form]");
                    if (nameForm) {
                        nameForm.hidden = page.dataset.canCreate !== "true" && page.dataset.canEdit !== "true";
                        const reset = () => { nameForm.reset(); nameForm.elements.id.value = ""; nameForm.querySelector("[data-variant-name-cancel]").hidden = true; window.jQuery?.(nameForm.elements.idParent).trigger("change.select2"); };
                        nameForm.querySelector("[data-variant-name-cancel]").addEventListener("click", reset);
                        nameForm.addEventListener("submit", async event => {
                            event.preventDefault();
                            const id = nameForm.elements.id.value, name = nameForm.elements.name.value.trim();
                            if (!name || (id ? page.dataset.canEdit !== "true" : page.dataset.canCreate !== "true")) return;
                            const button = nameForm.querySelector('[type="submit"]'); button.disabled = true;
                            try {
                                const result = await request(page, `${page.dataset.api}/variant-names${id ? `/${id}` : ""}`, id ? "PUT" : "POST", { name, idParent: Number(nameForm.elements.idParent.value) || null });
                                ui.notify(result.message, "success"); reset(); await loadVariantNames(page); await load(page);
                            } catch (error) { ui.notify(error.message || "ذخیرهٔ نام تنوع ناموفق بود.", "danger"); }
                            finally { button.disabled = false; }
                        });
                    }
                }
                const config = configs[section];
                page.querySelector("[data-reference-type-filter]")?.addEventListener("change", () => void load(page));
                for (const filter of page.querySelectorAll("[data-variant-product-filter], [data-variant-status-filter]"))
                    filter.addEventListener("change", () => renderTable(page, variantRows.get(page) || [], config));
                const createButton = page.querySelector("[data-definition-create]");
                if (createButton) {
                    createButton.hidden = page.dataset.canCreate !== "true";
                    createButton.addEventListener("click", () => void openForm(page, null, config).catch(error => ui.notify(error.message || "بازکردن فرم ناموفق بود.", "danger")));
                }
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
                        body[key] = type === "checkbox" ? Boolean(form.elements.namedItem(key)?.checked) : (key === "idStockOwnerType" || ["number", "products", "product-fixed", "units", "companies", "plans", "unit-groups", "variant-names"].includes(type) ? Number(values.get(key)) || null : values.get(key));
                    }
                    if (section === "variants") {
                        const rows = [...form.querySelectorAll("[data-variant-attribute]")];
                        const incomplete = rows.find(row => !row.querySelector('[data-attribute-field="attributeId"]')?.value || !row.querySelector('[data-attribute-field="valueId"]')?.value);
                        if (incomplete) { ui.notify("برای هر ردیف، ویژگی و مقدار آن را از درخت تنوع انتخاب کنید.", "danger"); incomplete.querySelector("select")?.focus(); return; }
                        body.attributes = rows.map(row => Object.fromEntries([...row.querySelectorAll("[data-attribute-field]")].map(input => [input.dataset.attributeField, Number(input.value) || null])));
                    }
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
