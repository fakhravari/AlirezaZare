(() => {
    "use strict";

    const ui = window.AdminUI;
    if (!ui) return;
    const initialized = new WeakSet();
    const format = new Intl.NumberFormat("fa-IR", { maximumFractionDigits: 3 });
    const formatFactor = new Intl.NumberFormat("fa-IR", { maximumFractionDigits: 9 });
    let lookups;
    let modal;
    let pageNumber = 1;
    let requestSerial = 0;
    let editorSerial = 0;

    const token = () => document.querySelector('[data-voucher-form] input[name="__RequestVerificationToken"]')?.value || "";
    async function send(url, method = "GET", body) {
        const response = await ui.getJson(url, {
            method,
            headers: body ? { "Content-Type": "application/json", RequestVerificationToken: token() } : {},
            ...(body ? { body: JSON.stringify(body) } : {})
        });
        return response;
    }
    const node = (tag, text, cls) => {
        const result = document.createElement(tag);
        if (text != null) result.textContent = text;
        if (cls) result.className = cls;
        return result;
    };
    const option = (select, value, label) => {
        const item = node("option", label);
        item.value = String(value);
        select.append(item);
    };
    const today = () => new Date(Date.now() - new Date().getTimezoneOffset() * 60000).toISOString().slice(0, 10);

    function status(page, message, error = false) {
        const target = page.querySelector("[data-voucher-status]");
        target.textContent = message;
        target.classList.toggle("text-danger", error); target.classList.toggle("text-muted", !error); target.classList.add("small");
        target.classList.toggle("d-none", !message);
    }

    async function loadLookups(page) {
        const result = await send(`${page.dataset.api}/lookups`);
        if (!page.isConnected) return false;
        lookups = result;
        const filterStatus = page.querySelector('[data-voucher-filter] select[name="status"]');
        const formStatus = document.querySelector('[data-voucher-form] select[name="status"]');
        const selectedStatus = filterStatus.value;
        filterStatus.replaceChildren(new Option("همهٔ وضعیت‌ها", ""), ...lookups.statuses.map(item => new Option(item.nameFa, item.code)));
        filterStatus.value = selectedStatus;
        formStatus.replaceChildren(...lookups.statuses.map(item => new Option(item.nameFa, item.code)));
        const filterWarehouse = page.querySelector("[data-voucher-warehouses]");
        const formWarehouse = document.querySelector("[data-voucher-warehouse]");
        filterWarehouse.replaceChildren();
        option(filterWarehouse, "", "همهٔ انبارهای مجاز");
        formWarehouse.replaceChildren();
        option(formWarehouse, "", "انتخاب انبار");
        for (const warehouse of lookups.warehouses) {
            const label = `${warehouse.companyName} / ${warehouse.name} (${warehouse.code})`;
            option(filterWarehouse, warehouse.id, label);
            option(formWarehouse, warehouse.id, label);
        }
        const types = filteredTypes(page);
        fillSelect(document.querySelector("[data-voucher-types]"), types,
            types.length ? "انتخاب نوع سند" : "نوع سند مناسب تعریف نشده است", item => item.name, item => item.id);
        document.querySelector("[data-voucher-types-help]").hidden = types.length > 0;
        fillSelect(document.querySelector("[data-voucher-plans]"), [], "ابتدا انبار را انتخاب کنید", item => item.displayName, item => item.id);
        formWarehouse.onchange = () => updateVoucherOwners(formWarehouse.form);
        fillSelect(document.querySelector("[data-voucher-parties]"), lookups.parties, "انتخاب مرجع", item => `${item.name} (${partyLabel(item.partyType)})`, item => item.id);
        return true;
    }

    function filteredTypes(page) {
        const feature = page.dataset.voucherFeature;
        let types = lookups.types.filter(type => !type.isSystemSalesDispatch);
        if (feature === "receipts") types = types.filter(type => type.direction === "R");
        if (feature === "dispatches") types = types.filter(type => type.direction === "I");
        if (feature === "returns") types = types.filter(type => type.isReturn);
        return types;
    }

    function fillSelect(select, items, emptyLabel, label, value) {
        select.replaceChildren();
        option(select, "", emptyLabel);
        for (const item of items) option(select, value(item), label(item));
    }

    function partyLabel(value) {
        return ({ Person: "شخص", Organization: "سازمان", Warehouse: "انبار", Other: "سایر" })[value] || value;
    }

    async function loadVouchers(page) {
        const serial = ++requestSerial;
        status(page, "در حال دریافت سندها…");
        const form = page.querySelector("[data-voucher-filter]");
        const query = new URLSearchParams(new FormData(form));
        query.set("page", String(pageNumber));
        query.set("pageSize", "50");
        try {
            const result = await send(`${page.dataset.api}?${query}`);
            if (serial !== requestSerial) return;
            renderVouchers(page, result.items || []);
            status(page, result.items?.length ? "" : "سندی با این فیلتر پیدا نشد.");
            const pagination = page.querySelector("[data-voucher-pagination]");
            const hasNext = (result.items?.length || 0) >= 50;
            const pages = Number.isFinite(result.total) ? Math.ceil(result.total / (result.pageSize || 50)) : pageNumber + (hasNext ? 1 : 0);
                ui.renderNumberedPagination(pagination, pageNumber, pages, target => { pageNumber = target; void loadVouchers(page); }, "صفحه‌بندی اسناد");
        } catch (error) { if (serial === requestSerial) status(page, error.message, true); }
    }

    function renderVouchers(page, items) {
        const body = page.querySelector("[data-voucher-rows]");
        ui.destroyDataTable(page.querySelector("[data-admin-datatable]"));
        body.replaceChildren();
        for (const item of items) {
            const row = document.createElement("tr");
            [format.format(item.voucherNumber), ui.formatDateTime(item.voucherDate), item.voucherType, item.warehouse, item.salesPlan, item.party].forEach(value => row.append(node("td", value)));
            const state = node("td");
            state.append(node("span", item.statusName || item.status, `badge ${item.status === "F" ? "bg-success" : "bg-warning"}`));
            row.append(state, node("td", item.affectsStock && item.status === "F" ? "دارد" : "ندارد"));
            const actions = node("td", null, "text-nowrap");
            const edit = node("button", "ویرایش", "btn btn-sm btn-default me-1");
            edit.type = "button";
            edit.addEventListener("click", () => void openEditor(page, item.id).catch(error => { status(page, error.message || "بازکردن سند ناموفق بود.", true); ui.notify(error.message || "بازکردن سند ناموفق بود.", "danger"); }));
            const remove = node("button", "حذف", "btn btn-sm btn-outline-danger");
            remove.type = "button";
            remove.disabled = item.status !== "D";
            if (remove.disabled) remove.title = "برای حذف، ابتدا وضعیت سند را به موقت تغییر دهید.";
            remove.dataset.adminConfirm = `سند شمارهٔ ${format.format(item.voucherNumber)} حذف شود؟`;
            remove.addEventListener("click", async () => {
                if (remove.disabled) return;
                remove.disabled = true;
                try {
                    await send(`${page.dataset.api}/${item.id}`, "DELETE", { delete: true });
                    ui.notify("سند حذف شد.", "success");
                    await loadVouchers(page);
                } catch (error) { ui.notify(error.message, "danger"); }
                finally { remove.disabled = false; }
            });
            if (page.dataset.canEdit === "true") actions.append(edit);
            if (page.dataset.canDelete === "true") {
                actions.append(remove);
                if (remove.disabled) actions.append(node("small", "برای حذف، ابتدا سند را موقت کنید.", "d-block text-muted mt-1"));
            }
            row.append(actions);
            body.append(row);
        }
        ui.refreshDataTable(page.querySelector("[data-admin-datatable]"));
    }

    function setSelect(select, value) { select.value = value == null ? "" : String(value); }

    function setVoucherDate(input, value) {
        const picker = window.AdminPersianDatePicker;
        picker?.initialize(input);
        if (!picker?.setValue(input, value)) input.value = value || "";
    }

    function updateVoucherOwners(form, selectedId = null) {
        const warehouseId = Number(form.elements.warehouseId.value);
        const allowed = new Set(lookups.ownerLinks.filter(link => link.idWarehouse === warehouseId).map(link => link.idStockOwner));
        const owners = lookups.plans.filter(plan => allowed.has(plan.id));
        const select = form.elements.salesPlanId;
        fillSelect(select, owners, "انتخاب مالک کالا", item => item.displayName, item => item.id);
        const warehouse = lookups.warehouses.find(item => item.id === warehouseId);
        const preferred = warehouse?.isSalesWarehouse ? owners.find(plan => plan.isRegular)?.id : warehouse?.defaultOwnerSalesPlanId;
        const selected = selectedId ?? preferred ?? (owners.length === 1 ? owners[0].id : null);
        setSelect(select, selected);
        form.querySelectorAll("[data-voucher-line-plan]").forEach(lineSelect => {
            const previous = lineSelect.value;
            fillSelect(lineSelect, owners, "انتخاب مالک کالا", item => item.displayName, item => item.id);
            setSelect(lineSelect, owners.some(owner => String(owner.id) === previous) ? previous : selected);
        });
    }

    async function openEditor(page, id = null) {
        const serial = ++editorSerial;
        const form = document.querySelector("[data-voucher-form]");
        form.reset();
        form.elements.id.value = "";
        form.elements.status.value = "D";
        setVoucherDate(form.elements.voucherDate, today());
        form.elements.voucherDate.max = today();
        form.elements.externalReference.value = "";
        form.querySelector("[data-voucher-lines]").replaceChildren();
        updateVoucherOwners(form);
        if (id) {
            try {
                const detail = await send(`${page.dataset.api}/${id}`);
                if (!page.isConnected || serial !== editorSerial) return;
                form.elements.id.value = String(detail.id);
                setSelect(form.elements.warehouseId, detail.warehouseId);
                setSelect(form.elements.voucherTypeId, detail.voucherTypeId);
                setSelect(form.elements.partyId, detail.partyId);
                updateVoucherOwners(form, detail.salesPlanId);
                setVoucherDate(form.elements.voucherDate, detail.voucherDate);
                form.elements.status.value = detail.status;
                form.elements.reason.value = detail.reason || "";
                form.elements.externalReference.value = detail.externalReference || "";
                for (const line of detail.lines) appendLine(form, line);
            } catch (error) { if (page.isConnected && serial === editorSerial) ui.notify(error.message, "danger"); return; }
        } else appendLine(form);
        document.getElementById("admin-voucher-title").textContent = id ? "ویرایش سند انبار" : "ثبت سند انبار";
        modal.show();
    }

    function appendLine(form, initial = null) {
        const body = form.querySelector("[data-voucher-lines]");
        if (body.rows.length >= 200) { ui.notify("هر سند حداکثر ۲۰۰ قلم دارد.", "danger"); return; }
        const row = document.createElement("tr");
        const itemCell = node("td");
        const itemSelect = node("select", null, "form-select");
        itemSelect.required = true;
        itemSelect.setAttribute("aria-label", "کالا یا تنوع قلم سند");
        itemSelect.dataset.voucherItem = "";
        option(itemSelect, "", "انتخاب کالا / تنوع");
        for (const item of lookups.inventoryItems) {
            const attributes = (item.variantAttributes || []).map(attribute => `${attribute.label}: ${attribute.value}`).join("، ");
            const variant = [item.variantName, attributes].filter(Boolean).join(" — ");
            option(itemSelect, item.id, variant ? `${item.productName} — ${variant}` : item.productName);
        }
        itemCell.append(itemSelect);
        const planCell = node("td");
        const planSelect = node("select", null, "form-select");
        planSelect.required = true;
        planSelect.dataset.voucherLinePlan = "";
        planSelect.setAttribute("aria-label", "طرح یا مالک قلم سند");
        const warehouseId = Number(form.elements.warehouseId.value);
        const allowed = new Set(lookups.ownerLinks.filter(link => link.idWarehouse === warehouseId).map(link => link.idStockOwner));
        fillSelect(planSelect, lookups.plans.filter(plan => allowed.has(plan.id)), "انتخاب مالک کالا", item => item.displayName, item => item.id);
        setSelect(planSelect, initial?.salesPlanId ?? form.elements.salesPlanId.value);
        planCell.append(planSelect);
        const unitCell = node("td");
        const unitSelect = node("select", null, "form-select");
        unitSelect.required = true;
        unitSelect.setAttribute("aria-label", "واحد قلم سند");
        unitSelect.dataset.voucherUnit = "";
        unitCell.append(unitSelect);
        const quantityCell = node("td");
        const quantity = node("input", null, "form-control");
        quantity.type = "number";
        quantity.min = "0.001";
        quantity.step = "0.001";
        quantity.max = "999999999999999.999";
        quantity.required = true;
        quantity.setAttribute("aria-label", "مقدار قلم سند");
        quantity.dataset.voucherQuantity = "";
        quantityCell.append(quantity);
        const factor = node("td", "—", "text-nowrap");
        factor.dataset.voucherFactor = "";
        const base = node("td", "—", "text-nowrap");
        base.dataset.voucherBase = "";
        const actionCell = node("td");
        const remove = node("button", "حذف", "btn btn-sm btn-outline-danger");
        remove.type = "button";
        remove.setAttribute("aria-label", "حذف قلم سند");
        remove.addEventListener("click", () => row.remove());
        actionCell.append(remove);
        row.append(itemCell, planCell, unitCell, quantityCell, factor, base, actionCell);

        const updateUnits = () => {
            unitSelect.replaceChildren();
            option(unitSelect, "", "انتخاب واحد");
            const selectedItem = lookups.inventoryItems.find(item => item.id === Number(itemSelect.value));
            for (const unit of selectedItem?.units || []) option(unitSelect, unit.id, unit.name);
            updateBase();
        };
        const updateBase = () => {
            const selectedItem = lookups.inventoryItems.find(item => item.id === Number(itemSelect.value));
            const selectedUnit = selectedItem?.units.find(unit => unit.id === Number(unitSelect.value));
            factor.textContent = selectedUnit ? formatFactor.format(selectedUnit.conversionFactor) : "—";
            const raw = Number(quantity.value || 0) * Number(selectedUnit?.conversionFactor || 0);
            base.textContent = selectedUnit && raw > 0 ? format.format(Math.trunc(raw * 1000) / 1000) : "—";
        };
        itemSelect.addEventListener("change", updateUnits);
        unitSelect.addEventListener("change", updateBase);
        quantity.addEventListener("input", updateBase);
        body.append(row);
        if (initial) {
            itemSelect.value = String(initial.inventoryItemId);
            quantity.value = initial.quantity;
            updateUnits();
            unitSelect.value = String(initial.unitId);
            updateBase();
        }
    }

    function initialize(root) {
        const page = root.querySelector?.("[data-admin-vouchers]");
        if (!page || initialized.has(page)) return;
        initialized.add(page);
        pageNumber = 1;
        requestSerial++;
        modal = window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("admin-voucher-modal"));
        const createButton = page.querySelector("[data-voucher-create]");
        createButton.hidden = page.dataset.canCreate !== "true";
        createButton.disabled = true;
        void (async () => {
            try {
                if (!await loadLookups(page)) return;
                createButton.disabled = false;
                createButton.addEventListener("click", () => void openEditor(page).catch(error => { status(page, error.message || "بازکردن فرم سند ناموفق بود.", true); ui.notify(error.message || "بازکردن فرم سند ناموفق بود.", "danger"); }));
                const referenceLabel = document.querySelector("[data-reference-label]");
                const referenceInput = document.querySelector("[data-voucher-reference]");
                if (page.dataset.voucherFeature === "returns") {
                    referenceLabel.textContent = "شمارهٔ سفارش مشتری";
                    referenceInput.required = true;
                }
                document.querySelector("[data-voucher-line-add]").addEventListener("click", () => appendLine(document.querySelector("[data-voucher-form]")));
                page.querySelector("[data-voucher-filter]").addEventListener("submit", event => { event.preventDefault(); pageNumber = 1; void loadVouchers(page); });
                page.querySelector("[data-voucher-reset]").addEventListener("click", () => { page.querySelector("[data-voucher-filter]").reset(); pageNumber = 1; void loadVouchers(page); });
                document.querySelector("[data-voucher-form]").addEventListener("submit", async event => {
                    event.preventDefault();
                    const form = event.currentTarget;
                    if (form.dataset.saving === "true") return;
                    const lines = [...form.querySelectorAll("[data-voucher-lines] tr")].map(row => ({
                        inventoryItemId: Number(row.querySelector("[data-voucher-item]").value),
                        unitId: Number(row.querySelector("[data-voucher-unit]").value),
                        salesPlanId: Number(row.querySelector("[data-voucher-line-plan]").value),
                        quantity: Number(row.querySelector("[data-voucher-quantity]").value)
                    }));
                    if (!lines.length || lines.some(line => !line.inventoryItemId || !line.unitId || !line.salesPlanId || !line.quantity)) {
                        ui.notify("برای هر قلم، کالا، واحد و مقدار را انتخاب کنید.", "danger");
                        return;
                    }
                    if (!form.elements.reason.value.trim()) {
                        ui.notify("دلیل سند را وارد کنید.", "danger");
                        form.elements.reason.focus();
                        return;
                    }
                    const body = {
                        warehouseId: Number(form.elements.warehouseId.value),
                        voucherTypeId: Number(form.elements.voucherTypeId.value),
                        partyId: Number(form.elements.partyId.value),
                        salesPlanId: Number(form.elements.salesPlanId.value),
                        voucherDate: form.elements.voucherDate.value,
                        status: form.elements.status.value,
                        reason: form.elements.reason.value,
                        externalReference: form.elements.externalReference.value || null,
                        lines
                    };
                    const id = Number(form.elements.id.value);
                    const submit = form.querySelector('[type="submit"]');
                    form.dataset.saving = "true";
                    submit.disabled = true;
                    try {
                        const result = await send(id ? `${page.dataset.api}/${id}` : page.dataset.api, id ? "PUT" : "POST", body);
                        ui.notify(result.message, "success");
                        modal.hide();
                        await loadVouchers(page);
                    } catch (error) { ui.notify(error.message, "danger"); }
                    finally { form.dataset.saving = "false"; submit.disabled = false; }
                });
                await loadVouchers(page);
            } catch (error) { status(page, error.message, true); }
        })();
    }

    window.AdminVouchers = Object.freeze({ initialize });
})();
