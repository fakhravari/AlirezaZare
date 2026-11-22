(() => {
    "use strict";
    const ui = window.AdminUI, initialized = new WeakSet(), requests = new WeakMap(), number = new Intl.NumberFormat("fa-IR"), pageSize = 50;
    if (!ui) return;
    const cell = (value) => { const node = document.createElement("td"); node.textContent = value ?? "—"; return node; };
    const token = (page) => page.querySelector('[name="__RequestVerificationToken"]')?.value || "";
    const status = (page, message, error = false) => {
        const node = page.querySelector("[data-price-status]"); node.textContent = message;
        node.classList.toggle("text-danger", error); node.classList.toggle("text-muted", !error); node.classList.add("small"); node.classList.toggle("d-none", !message);
    };
    async function request(page, url, method = "GET", body) {
        return ui.getJson(url, { method, headers: { ...(body ? { "Content-Type": "application/json" } : {}), ...(method !== "GET" ? { RequestVerificationToken: token(page) } : {}) }, ...(body ? { body: JSON.stringify(body) } : {}) });
    }
    function filterUrl(page, path, selector) {
        const url = new URL(`${page.dataset.api}${path}`, window.location.origin);
        const form = page.querySelector(selector);
        if (!form) return url;
        const from = form.elements.from?.value || "", to = form.elements.to?.value || "";
        if (from && to && from > to) throw new Error("تاریخ شروع نباید بعد از تاریخ پایان باشد.");
        for (const name of ["q", "from", "to"]) {
            const value = form.elements[name]?.value.trim();
            if (value) url.searchParams.set(name, value);
        }
        for (const option of form.querySelectorAll('[name="productIds"] option:checked')) url.searchParams.append("productIds", option.value);
        return url;
    }
    function bindFilter(form, reload) {
        if (!form) return;
        let timer;
        form.addEventListener("submit", event => { event.preventDefault(); clearTimeout(timer); reload(); });
        form.addEventListener("input", event => {
            if (event.target.name !== "q") return;
            clearTimeout(timer); timer = setTimeout(reload, 300);
        });
        form.addEventListener("reset", () => { clearTimeout(timer); setTimeout(reload, 0); });
    }
    function updatePager(page, current, total) {
        const pager = page.querySelector("[data-offer-pagination]");
        if (!pager) return;
        const pages = Math.max(1, Math.ceil(total / pageSize));
        ui.renderNumberedPagination(pager, current, pages, target => void loadOffers(page, target), "صفحه‌بندی پیشنهادها");
    }
    function updateSaveAll(page) {
        const button = page.querySelector("[data-price-save-all]");
        if (button) button.disabled = page.dataset.canEdit !== "true" || page._variantPriceBulkSaving || page._variantPriceSaving.size > 0 || page._variantPriceDrafts.size === 0;
    }
    function pricePayload(amount, discount, active) {
        if (!amount.value.trim()) throw new Error("برای فروش این تنوع، قیمت آن را در این طرح فروش ثبت کنید.");
        const priceAmount = ui.priceValue(amount), discountPercent = Number(discount.value || 0);
        if (!Number.isSafeInteger(priceAmount) || priceAmount < 0 || !Number.isFinite(discountPercent) || discountPercent < 0 || discountPercent >= 100 || !discount.validity.valid) throw new Error("قیمت صحیح و تخفیف تا دو رقم اعشار و کمتر از ۱۰۰ وارد کنید.");
        return { priceAmount, discountPercent, isEnabled: active };
    }
    async function saveAll(page) {
        if (page._variantPriceBulkSaving || page._variantPriceSaving.size || !page._variantPriceDrafts.size) return;
        const message = page.querySelector("[data-price-save-all-status]"), entries = [];
        for (const [key, draft] of page._variantPriceDrafts) {
            const amount = document.createElement("input"), discount = document.createElement("input");
            amount.type = "text"; ui.setPriceValue(amount, draft.amount);
            discount.type = "number"; discount.min = "0"; discount.max = "99.99"; discount.step = "0.01"; discount.value = draft.discount;
            try { entries.push({ key, item: draft.item, payload: pricePayload(amount, discount, draft.active) }); }
            catch (error) { message.textContent = `${draft.item.productName}، ${draft.item.variantName}، ${draft.item.salesPlanName}: ${error.message}`; return; }
        }
        page._variantPriceBulkSaving = true; render(page);
        let saved = 0; const failures = [];
        for (const entry of entries) {
            message.textContent = `در حال ذخیرهٔ ${number.format(saved + failures.length + 1)} از ${number.format(entries.length)}…`;
            try {
                await request(page, `${page.dataset.api}/${entry.item.variantId}/${encodeURIComponent(entry.item.salesPlanId)}`, "PUT", entry.payload);
                page._variantPriceDrafts.delete(entry.key); saved++;
            } catch (error) { failures.push(`${entry.item.productName}، ${entry.item.variantName}، ${entry.item.salesPlanName}: ${error.message || "ذخیره ناموفق بود."}`); }
        }
        await load(page);
        page._variantPriceBulkSaving = false; render(page);
        message.textContent = `${number.format(saved)} ردیف ذخیره شد.${failures.length ? ` ${number.format(failures.length)} ردیف ذخیره نشد: ${failures.join("؛ ")}` : ""}`;
    }
    function render(page) {
        const rows = page._variantPriceRows;
        const body = page.querySelector("[data-price-rows]"), table = body.closest("table"); ui.destroyDataTable(table); body.replaceChildren();
        for (const item of rows) {
            const key = `${item.variantId}\u0000${item.salesPlanId}`;
            const draft = page._variantPriceDrafts.get(key);
            const row = document.createElement("tr");
            row.append(cell(item.productName), cell(item.variantName), cell(item.salesPlanName));
            const amountCell = document.createElement("td"), amount = document.createElement("input");
            amount.type = "text"; amount.max = "9007199254740991"; amount.className = "form-control form-control-sm";
            amount.setAttribute("aria-label", `قیمت (تومان) ${item.productName}، ${item.variantName}، ${item.salesPlanName}`);
            ui.setPriceValue(amount, draft?.amount ?? (item.priceAmount == null ? "" : String(item.priceAmount))); amount.disabled = page.dataset.canEdit !== "true"; amountCell.append(amount); row.append(amountCell);
            const discountCell = document.createElement("td"), discount = document.createElement("input");
            discount.type = "number"; discount.min = "0"; discount.max = "99.99"; discount.step = "0.01"; discount.className = "form-control form-control-sm";
            discount.setAttribute("aria-label", `درصد تخفیف ${item.productName}، ${item.variantName}، ${item.salesPlanName}`);
            discount.value = draft?.discount ?? (item.discountPercent == null ? "0" : String(item.discountPercent)); discount.disabled = page.dataset.canEdit !== "true"; discountCell.append(discount); row.append(discountCell);
            const activeCell = document.createElement("td"), activeLabel = document.createElement("label"), active = document.createElement("input"), activeText = document.createElement("span");
            active.type = "checkbox"; ui.initStatusSwitch(active); active.checked = draft?.active ?? (item.isEnabled !== false); active.disabled = page.dataset.canEdit !== "true"; activeText.textContent = " فعال"; activeLabel.append(active, activeText); activeCell.append(activeLabel); row.append(activeCell);
            const rememberDraft = () => { page._variantPriceDrafts.set(key, { item, amount: amount.value, discount: discount.value, active: active.checked }); updateSaveAll(page); };
            amount.addEventListener("input", rememberDraft); discount.addEventListener("input", rememberDraft); active.addEventListener("change", rememberDraft);
            const actions = document.createElement("td"), save = document.createElement("button"); actions.className = "admin-pricing-row-actions"; save.type = "button"; save.className = "btn btn-sm btn-primary"; save.textContent = "ذخیره"; save.dataset.adminConfirm = "قیمت و وضعیت طرح برای این تنوع ذخیره شود؟"; save.disabled = page.dataset.canEdit !== "true";
            ui.withIcon(save, "fa-floppy-disk");
            const setBusy = busy => { if (busy) page._variantPriceSaving.add(key); else page._variantPriceSaving.delete(key); const disabled = busy || page._variantPriceBulkSaving || page.dataset.canEdit !== "true"; save.disabled = disabled; amount.disabled = disabled; discount.disabled = disabled; active.disabled = disabled; updateSaveAll(page); };
            save.addEventListener("click", async () => {
                if (save.disabled) return;
                setBusy(true);
                try {
                    await request(page, `${page.dataset.api}/${item.variantId}/${encodeURIComponent(item.salesPlanId)}`, "PUT", pricePayload(amount, discount, active.checked));
                    page._variantPriceDrafts.delete(key); ui.notify("قیمت طرح ذخیره شد.", "success"); await load(page);
                } catch (error) { ui.notify(error.message || "ذخیرهٔ قیمت ناموفق بود.", "danger"); }
                finally { setBusy(false); render(page); }
            });
            actions.append(save);
            if (page._variantPriceSaving.has(key) || page._variantPriceBulkSaving) { const disabled = true; save.disabled = disabled; amount.disabled = disabled; discount.disabled = disabled; active.disabled = disabled; }
            row.append(actions); body.append(row);
        }
        page.querySelector("[data-price-count]").textContent = `${number.format(rows.length)} ردیف`;
        updateSaveAll(page);
        ui.refreshDataTable(table);
        if (!rows.length) status(page, "موردی با این جست‌وجو پیدا نشد."); else status(page, "");
    }
    async function load(page) {
        const serial = (requests.get(page) || 0) + 1; requests.set(page, serial);
        status(page, "در حال دریافت قیمت‌ها…");
        try {
            const url = filterUrl(page, "", "[data-price-filter]");
            url.searchParams.set("all", "true");
            const result = await request(page, url);
            if (requests.get(page) !== serial || !page.isConnected) return;
            page._variantPriceRows = result.items || []; render(page);
        }
        catch (error) { if (requests.get(page) === serial) status(page, error.message || "دریافت قیمت‌ها ناموفق بود.", true); }
    }
    async function loadOffers(page, pageNumber = page._offerPage || 1) {
        const serial = (page._offerSerial || 0) + 1; page._offerSerial = serial;
        const message = page.querySelector("[data-offer-status]"); message.textContent = "در حال دریافت پیشنهادها…";
        try {
            const url = filterUrl(page, "/product-offers", "[data-offer-filter]");
            url.searchParams.set("page", String(pageNumber)); url.searchParams.set("pageSize", String(pageSize));
            const result = await request(page, url);
            if (!page.isConnected || page._offerSerial !== serial) return;
            const lastPage = Math.max(1, Math.ceil((result.total || 0) / pageSize));
            if (pageNumber > lastPage) return void await loadOffers(page, lastPage);
            page._offerPage = result.page || pageNumber; page._offerTotal = result.total || 0;
            page._offers = result.items || []; renderOffers(page); updatePager(page, page._offerPage, page._offerTotal);
        } catch (error) { if (page.isConnected && page._offerSerial === serial) message.textContent = error.message || "دریافت پیشنهادها ناموفق بود."; }
    }
    async function loadHistory(page) {
        const serial = (page._historySerial || 0) + 1; page._historySerial = serial;
        const message = page.querySelector("[data-price-history-status]"), body = page.querySelector("[data-price-history-rows]"), table = body.closest("table");
        ui.destroyDataTable(table); message.textContent = "در حال دریافت تاریخچه…"; body.replaceChildren();
        try {
            const url = filterUrl(page, "/price-history", "[data-history-filter]"); url.searchParams.set("take", "200");
            const rows = await request(page, url);
            if (!page.isConnected || serial !== page._historySerial) return;
            for (const item of rows) {
                const row = document.createElement("tr");
                const target = ({ option: "ویژگی", accessory: "متعلقه", variant: "تنوع", product: "کالا" })[item.targetType] || item.targetType;
                row.append(cell(`#${item.id}`), cell(item.productName || `#${item.idProduct}`), cell(item.salesPlanName || `#${item.idSalesPlan}`), cell(`${target}${item.targetId ? ` #${item.targetId}` : ""}`), cell(number.format(item.priceAmount)), cell(item.isActive ? "فعال" : "سابقه"), cell(window.AdminUI.formatDateTime(item.createdAtUtc)), cell(item.idPreviousVersion ? `#${item.idPreviousVersion}` : "—"), cell(item.idChangedBy ? `#${item.idChangedBy}` : "—"), cell(item.changeReason || "—"));
                body.append(row);
            }
            ui.refreshDataTable(table);
            message.textContent = rows.length ? `${number.format(rows.length)} نسخهٔ اخیر` : "هنوز نسخه‌ای ثبت نشده است.";
        } catch (error) { if (serial === page._historySerial) message.textContent = error.message || "دریافت تاریخچه ناموفق بود."; }
    }
    function renderOffers(page) {
        const rows = page._offers;
        const body = page.querySelector("[data-offer-rows]"), table = body.closest("table"); ui.destroyDataTable(table); body.replaceChildren();
        for (const item of rows) {
            const key = `${item.productId}\u0000${item.salesPlanId}`, draft = page._offerDrafts.get(key);
            const row = document.createElement("tr"); row.append(cell(`${item.productName} (${item.productCode})`), cell(item.salesPlanName));
            const makeNumber = (label, value, min, max, step) => {
                const td = document.createElement("td"), input = document.createElement("input");
                input.type = label === "قیمت" ? "text" : "number"; input.className = "form-control form-control-sm"; input.min = min; input.max = max; input.step = step;
                input.setAttribute("aria-label", `${label}${label === "قیمت" ? " (تومان)" : ""} ${item.productName}، ${item.salesPlanName}`); if (label === "قیمت") ui.setPriceValue(input, value); else input.value = value; td.append(input); row.append(td); return input;
            };
            const amount = makeNumber("قیمت", draft?.amount ?? (item.priceAmount == null ? "" : String(item.priceAmount)), "0", "9007199254740991", "1");
            const discount = makeNumber("تخفیف", draft?.discount ?? String(item.discountPercent ?? 0), "0", "99.99", "0.01");
            const limit = makeNumber("سقف خرید", draft?.limit ?? String(item.maxPurchaseQuantity ?? 99), "1", "99", "1");
            const activeCell = document.createElement("td"), activeLabel = document.createElement("label"), active = document.createElement("input");
            active.type = "checkbox"; ui.initStatusSwitch(active); active.checked = draft?.active ?? (item.isEnabled ?? true); activeLabel.append(active, document.createTextNode(" فعال")); activeCell.append(activeLabel); row.append(activeCell);
            const remember = () => page._offerDrafts.set(key, { amount: amount.value, discount: discount.value, limit: limit.value, active: active.checked });
            for (const input of [amount, discount, limit]) input.addEventListener("input", remember); active.addEventListener("change", remember);
            const actions = document.createElement("td"), save = document.createElement("button"); actions.className = "admin-pricing-row-actions"; save.type = "button"; save.className = "btn btn-sm btn-primary"; save.textContent = "ذخیره"; save.dataset.adminConfirm = "قیمت پیشنهاد و محدودیت خرید ذخیره شوند؟"; actions.append(save);
            ui.withIcon(save, "fa-floppy-disk");
            if (item.priceAmount != null) {
                const accessories = document.createElement("button"); accessories.type = "button"; accessories.className = "btn btn-sm btn-default ms-1"; accessories.textContent = "متعلقات";
                ui.withIcon(accessories, "fa-puzzle-piece");
                accessories.addEventListener("click", () => void openAccessories(page, item)); actions.append(accessories);
                const specifications = document.createElement("button"); specifications.type = "button"; specifications.className = "btn btn-sm btn-default ms-1"; specifications.textContent = "مشخصات";
                ui.withIcon(specifications, "fa-circle-info");
                specifications.addEventListener("click", () => void openSpecifications(page, item)); actions.append(specifications);
            }
            row.append(actions);
            const setBusy = busy => { if (busy) page._offerSaving.add(key); else page._offerSaving.delete(key); for (const input of [amount, discount, limit, active, save]) input.disabled = busy || page.dataset.canEdit !== "true"; };
            setBusy(page._offerSaving.has(key));
            save.addEventListener("click", async () => {
                if (save.disabled) return;
                const priceAmount = ui.priceValue(amount), discountPercent = Number(discount.value), maxPurchaseQuantity = Number(limit.value);
                if (!amount.value.trim() || !Number.isSafeInteger(priceAmount) || priceAmount < 0 || !discount.value.trim() || !Number.isFinite(discountPercent) || discountPercent < 0 || discountPercent >= 100 || Math.abs(discountPercent * 100 - Math.round(discountPercent * 100)) > 0.000001 || !Number.isInteger(maxPurchaseQuantity) || maxPurchaseQuantity < 1 || maxPurchaseQuantity > 99) { ui.notify("قیمت، تخفیف یا سقف خرید معتبر نیست.", "danger"); return; }
                setBusy(true);
                try { const result = await request(page, `${page.dataset.api}/product-offers/${item.productId}/${encodeURIComponent(item.salesPlanId)}`, "PUT", { priceAmount, discountPercent, maxPurchaseQuantity, isEnabled: active.checked }); page._offerDrafts.delete(key); ui.notify(result.message, "success"); await loadOffers(page); }
                catch (error) { ui.notify(error.message || "ذخیرهٔ پیشنهاد ناموفق بود.", "danger"); }
                finally { setBusy(false); if (page.isConnected) renderOffers(page); }
            });
            body.append(row);
        }
        const first = page._offerTotal ? (page._offerPage - 1) * pageSize + 1 : 0;
        const last = rows.length ? first + rows.length - 1 : 0;
        page.querySelector("[data-offer-count]").textContent =
            `نمایش ${number.format(first)} تا ${number.format(last)} از ${number.format(page._offerTotal)} ردیف`;
        ui.refreshDataTable(table);
        page.querySelector("[data-offer-status]").textContent = rows.length ? "" : "موردی پیدا نشد.";
    }
    async function openAccessories(page, offer) {
        const modal = document.getElementById("offer-accessories-modal"); if (!modal) return;
        const serial = (page._accessoriesSerial || 0) + 1; page._accessoriesSerial = serial;
        const message = modal.querySelector("[data-offer-accessories-status]"), body = modal.querySelector("[data-offer-accessories-rows]");
        ui.destroyDataTable(body.closest("table")); body.replaceChildren(); message.textContent = "در حال دریافت متعلقات…";
        modal.querySelector("[data-offer-accessories-name]").textContent = `${offer.productName} — ${offer.salesPlanName}`;
        window.bootstrap?.Modal?.getOrCreateInstance(modal).show();
        const url = `${page.dataset.api}/product-offers/${offer.productId}/${encodeURIComponent(offer.salesPlanId)}/accessories`;
        try {
            const items = await request(page, url); if (!page.isConnected || page._accessoriesSerial !== serial) return;
            message.textContent = items.length ? "" : "متعلقهٔ فعالی برای کالا ثبت نشده است.";
            for (const item of items) {
                const row = document.createElement("tr"); row.append(cell(`${item.name}${item.isRequired ? " (اجباری)" : ""}`), cell(number.format(item.basePriceAmount)));
                const priceCell = document.createElement("td"), price = document.createElement("input"); price.type = "text"; price.max = "9007199254740991"; price.className = "form-control form-control-sm"; price.setAttribute("aria-label", `قیمت (تومان) ${item.name} در ${offer.salesPlanName}`); ui.setPriceValue(price, item.priceAmount ?? item.basePriceAmount); priceCell.append(price); row.append(priceCell);
                const allowedCell = document.createElement("td"), allowed = document.createElement("input"); allowed.type = "checkbox"; ui.initStatusSwitch(allowed); allowed.checked = item.isRequired || item.isAllowed; allowed.disabled = item.isRequired || page.dataset.canEdit !== "true"; allowed.setAttribute("aria-label", `مجاز بودن ${item.name}`); allowedCell.append(allowed); row.append(allowedCell);
                const actions = document.createElement("td"), save = document.createElement("button"); save.type = "button"; save.className = "btn btn-sm btn-primary"; save.textContent = "ذخیره"; save.dataset.adminConfirm = `قیمت و وضعیت متعلقهٔ ${item.name} ذخیره شود؟`; save.disabled = page.dataset.canEdit !== "true"; actions.append(save); row.append(actions);
                ui.withIcon(save, "fa-floppy-disk");
                save.addEventListener("click", async () => {
                    if (save.disabled) return;
                    const priceAmount = ui.priceValue(price);
                    if (!price.value.trim() || !Number.isSafeInteger(priceAmount) || priceAmount < 0) { ui.notify("قیمت متعلقه باید مبلغ صحیح و غیرمنفی باشد.", "danger"); return; }
                    save.disabled = true; price.disabled = true; allowed.disabled = true;
                    try { const result = await request(page, `${url}/${item.accessoryId}`, "PUT", { priceAmount, isAllowed: allowed.checked }); ui.notify(result.message, "success"); }
                    catch (error) { ui.notify(error.message || "ذخیرهٔ متعلقه ناموفق بود.", "danger"); }
                    finally { save.disabled = page.dataset.canEdit !== "true"; price.disabled = page.dataset.canEdit !== "true"; allowed.disabled = item.isRequired || page.dataset.canEdit !== "true"; }
                });
                price.disabled = page.dataset.canEdit !== "true"; body.append(row);
            }
            ui.refreshDataTable(body.closest("table"));
        } catch (error) { if (page.isConnected && page._accessoriesSerial === serial) message.textContent = error.message || "دریافت متعلقات ناموفق بود."; }
    }
    async function openSpecifications(page, offer) {
        const modal = document.getElementById("offer-specifications-modal"), form = modal?.querySelector("[data-offer-specifications-form]"); if (!form) return;
        const serial = (page._specificationsSerial || 0) + 1; page._specificationsSerial = serial;
        const message = form.querySelector("[data-offer-specifications-status]"), save = form.querySelector('[type="submit"]');
        form.elements.specifications.value = ""; form.querySelector("[data-offer-specifications-name]").textContent = `${offer.productName} — ${offer.salesPlanName}`;
        message.textContent = "در حال دریافت مشخصات…"; save.disabled = true; form._offerUrl = null;
        window.bootstrap?.Modal?.getOrCreateInstance(modal).show();
        const url = `${page.dataset.api}/product-offers/${offer.productId}/${encodeURIComponent(offer.salesPlanId)}/specifications`;
        try {
            const rows = await request(page, url); if (!page.isConnected || page._specificationsSerial !== serial) return;
            form.elements.specifications.value = rows.map(row => `${row.key} | ${row.value}`).join("\n"); form._offerUrl = url;
            message.textContent = ""; save.disabled = page.dataset.canEdit !== "true";
        } catch (error) { if (page.isConnected && page._specificationsSerial === serial) message.textContent = error.message || "دریافت مشخصات ناموفق بود."; }
    }
    async function loadPlanSettings(page, selectedId) {
        const form = page.querySelector("[data-plan-settings]");
        if (!form) return;
        const message = form.querySelector("[data-plan-status]");
        const serial = (page._planSerial || 0) + 1; page._planSerial = serial;
        try {
            const plans = await request(page, filterUrl(page, "/plans", "[data-plan-filter]"));
            if (!page.isConnected || serial !== page._planSerial) return;
            page._planSettings = plans;
            form.elements.id.replaceChildren(...plans.map(plan => new Option(`${plan.displayName} (${plan.code})`, String(plan.id))));
            if (selectedId && plans.some(plan => String(plan.id) === selectedId)) form.elements.id.value = selectedId;
            showPlanSettings(page);
            message.textContent = plans.length ? "" : "طرح فروشی یافت نشد.";
            form.querySelector('[type="submit"]').disabled = !plans.length;
        } catch (error) {
            if (!page.isConnected || serial !== page._planSerial) return;
            message.textContent = error.message || "دریافت طرح‌ها ناموفق بود.";
            form.querySelector('[type="submit"]').disabled = true;
        }
    }
    function showPlanSettings(page) {
        const form = page.querySelector("[data-plan-settings]");
        const plan = page._planSettings?.find(item => String(item.id) === form.elements.id.value);
        const publication = form.querySelector("[data-plan-publication]");
        if (publication) {
            publication.hidden = !plan;
            publication.textContent = plan?.isRegular ? "فروش عادی" : plan?.isActive ? "منتشرشده" : "غیرفعال";
            publication.dataset.active = String(!!plan?.isActive);
        }
        const utcValue = value => value ? new Date(value).toISOString().slice(0, 19) : "";
        window.AdminPersianDatePicker?.setValue(form.elements.startsAtUtc, utcValue(plan?.startsAtUtc));
        window.AdminPersianDatePicker?.setValue(form.elements.endsAtUtc, utcValue(plan?.endsAtUtc));
        form.elements.label.value = plan?.label || "";
        form.elements.showCountdown.checked = !!plan?.showCountdown;
        form.elements.code.value = plan?.code || "";
        form.elements.displayName.value = plan?.displayName || "";
        form.elements.planType.value = plan?.planType || "";
        for (const name of ["startsAtUtc", "endsAtUtc", "showCountdown"]) form.elements[name].disabled = !plan || plan.isRegular;
        for (const name of ["code", "displayName", "planType"]) form.elements[name].disabled = !plan || plan.isRegular;
        for (const selector of ["[data-plan-details-save]", "[data-plan-delete]"]) form.querySelector(selector).disabled = !plan || plan.isRegular;
        form.querySelector("[data-plan-publish]").disabled = !plan || plan.isRegular || plan.isActive;
        form.querySelector("[data-plan-unpublish]").disabled = !plan || plan.isRegular || !plan.isActive;
    }
    function initialize(root) {
        const page = root.querySelector?.("[data-store-variant-prices]"); if (!page || initialized.has(page)) return; initialized.add(page);
        page._variantPriceRows = [];
        page._variantPriceDrafts = new Map();
        page._variantPriceSaving = new Set();
        page._offers = []; page._offerDrafts = new Map(); page._offerSaving = new Set(); page._offerPage = 1; page._offerTotal = 0;
        const offerFilter = page.querySelector("[data-offer-filter]");
        if (offerFilter) {
            bindFilter(offerFilter, () => void loadOffers(page, 1));
            page.querySelector("[data-offer-refresh]").addEventListener("click", () => void loadOffers(page));
            void loadOffers(page);
            const specificationForm = document.querySelector("[data-offer-specifications-form]");
            specificationForm?.addEventListener("submit", async event => {
                event.preventDefault();
                const save = specificationForm.querySelector('[type="submit"]'); if (save.disabled || !specificationForm._offerUrl) return;
                const specs = specificationForm.elements.specifications.value.split(/\r?\n/).map(line => line.trim()).filter(Boolean).map(line => { const index = line.indexOf("|"); return index < 0 ? [line] : [line.slice(0, index).trim(), line.slice(index + 1).trim()]; });
                if (specs.some(parts => parts.length !== 2 || !parts[0] || !parts[1])) { ui.notify("هر خط باید شامل عنوان و مقدار با جداکنندهٔ | باشد.", "danger"); return; }
                save.disabled = true;
                try { const result = await request(page, specificationForm._offerUrl, "PUT", specs.map(([key, value]) => ({ key, value }))); ui.notify(result.message, "success"); window.bootstrap?.Modal?.getInstance(document.getElementById("offer-specifications-modal"))?.hide(); }
                catch (error) { ui.notify(error.message || "ذخیرهٔ مشخصات ناموفق بود.", "danger"); }
                finally { if (specificationForm.isConnected) save.disabled = page.dataset.canEdit !== "true"; }
            });
        }
        const planForm = page.querySelector("[data-plan-settings]");
        if (planForm) {
            const refreshPlans = selected => loadPlanSettings(page, selected);
            bindFilter(page.querySelector("[data-plan-filter]"), () => void refreshPlans());
            page.querySelector("[data-plan-create]").addEventListener("submit", async event => {
                event.preventDefault(); const form = event.currentTarget, button = form.querySelector('[type="submit"]'); button.disabled = true;
                try { const result = await request(page, `${page.dataset.api}/plans`, "POST", Object.fromEntries(new FormData(form))); ui.notify(result.message, "success"); form.reset(); await refreshPlans(String(result.id)); }
                catch (error) { ui.notify(error.message || "ساخت طرح ناموفق بود.", "danger"); } finally { button.disabled = false; }
            });
            planForm.elements.id.addEventListener("change", () => showPlanSettings(page));
            planForm.querySelector("[data-plan-details-save]").addEventListener("click", async event => {
                const button = event.currentTarget, id = planForm.elements.id.value; button.disabled = true;
                try { const result = await request(page, `${page.dataset.api}/plans/${encodeURIComponent(id)}/details`, "PUT", { code: planForm.elements.code.value, displayName: planForm.elements.displayName.value, planType: planForm.elements.planType.value }); ui.notify(result.message, "success"); await refreshPlans(id); }
                catch (error) { ui.notify(error.message || "ذخیرهٔ مشخصات ناموفق بود.", "danger"); } finally { button.disabled = false; }
            });
            for (const [selector, active] of [["[data-plan-publish]", true], ["[data-plan-unpublish]", false]]) planForm.querySelector(selector).addEventListener("click", async event => {
                const button = event.currentTarget, id = planForm.elements.id.value; button.disabled = true;
                try { const result = await request(page, `${page.dataset.api}/plans/${encodeURIComponent(id)}/publication`, "PUT", { isActive: active }); ui.notify(result.message, "success"); await refreshPlans(id); }
                catch (error) { ui.notify(error.message || "تغییر وضعیت طرح ناموفق بود.", "danger"); } finally { button.disabled = false; }
            });
            planForm.querySelector("[data-plan-delete]").addEventListener("click", async event => {
                const button = event.currentTarget, id = planForm.elements.id.value; if (!id) return; button.disabled = true;
                try { const result = await request(page, `${page.dataset.api}/plans/${encodeURIComponent(id)}`, "DELETE"); ui.notify(result.message, "success"); await refreshPlans(); }
                catch (error) { ui.notify(error.message || "حذف طرح ناموفق بود.", "danger"); } finally { button.disabled = false; }
            });
            planForm.addEventListener("submit", async event => {
                event.preventDefault();
                const button = planForm.querySelector('[type="submit"]');
                if (button.disabled) return;
                const id = planForm.elements.id.value;
                const toUtc = value => value ? `${value}Z` : null;
                const body = { startsAtUtc: toUtc(planForm.elements.startsAtUtc.value), endsAtUtc: toUtc(planForm.elements.endsAtUtc.value), label: planForm.elements.label.value, showCountdown: planForm.elements.showCountdown.checked };
                button.disabled = true;
                try { const result = await request(page, `${page.dataset.api}/plans/${encodeURIComponent(id)}`, "PUT", body); ui.notify(result.message, "success"); await loadPlanSettings(page, id); }
                catch (error) { ui.notify(error.message || "ذخیرهٔ تنظیمات طرح ناموفق بود.", "danger"); }
                finally { if (page.isConnected) button.disabled = false; }
            });
            void loadPlanSettings(page);
        }
        const priceFilter = page.querySelector("[data-price-filter]");
        if (priceFilter) {
            bindFilter(priceFilter, () => void load(page));
            page.querySelector("[data-price-save-all]")?.addEventListener("click", () => void saveAll(page));
            page.querySelector("[data-price-refresh]")?.addEventListener("click", () => void load(page));
            void load(page);
        }
        if (page.querySelector("[data-price-history-status]")) {
            bindFilter(page.querySelector("[data-history-filter]"), () => void loadHistory(page));
            page.querySelector("[data-price-history-refresh]")?.addEventListener("click", () => void loadHistory(page));
            void loadHistory(page);
        }
    }
    window.AdminVariantPrices = Object.freeze({ initialize });
})();
