(() => {
    "use strict";

    // Admin dialogs stay open when the backdrop is clicked; Escape and explicit
    // dismiss buttons continue to work. Set this before page scripts create instances.
    if (window.bootstrap?.Modal?.Default) window.bootstrap.Modal.Default.backdrop = "static";

    function initStatusSwitch(input) {
        input.dataset.adminSwitch = "";
        input.setAttribute("role", "switch");
        return input;
    }

    function arrangeModalFields(modal) {
        const body = modal.querySelector(".modal-body");
        if (!body || body.dataset.adminFieldsArranged || body.querySelector(":scope > .row")) return;

        const children = [...body.children];
        for (const child of children) {
            if (!child.matches(".mb-3") || !child.querySelector(":scope > label.form-label")) continue;
            child.classList.add("admin-modal-field");
            child.classList.remove("mb-3");
            if (child.querySelector(":scope > textarea, :scope > input[type=file]")) child.classList.add("admin-modal-field-wide");
        }
        for (let index = 0; index < children.length; index++) {
            const label = children[index];
            if (!label.matches("label.form-label")) continue;
            const control = children[index + 1];
            if (!control?.matches("input:not([type=hidden]), select, textarea")) continue;

            const field = document.createElement("div");
            field.className = "admin-modal-field";
            if (control.matches("textarea, input[type=file]")) field.classList.add("admin-modal-field-wide");
            label.before(field);
            field.append(label, control);
            index++;
        }
        const fieldCount = body.querySelectorAll(":scope > .admin-modal-field").length;
        if (fieldCount >= 2) body.classList.add(fieldCount >= 6 ? "admin-modal-grid-three" : "admin-modal-grid-two");
        if (fieldCount >= 5) modal.querySelector(".modal-dialog")?.classList.add("admin-modal-wide");
        body.dataset.adminFieldsArranged = "true";
    }

    document.addEventListener("show.bs.modal", event => {
        if (event.target instanceof HTMLElement) arrangeModalFields(event.target);
    });

    function validationLabel(control) {
        const label = control.labels?.[0]?.textContent || control.getAttribute("aria-label") || control.placeholder || control.name;
        return (label || "این فیلد").replace(/[：:：*]+$/u, "").replace(/\s+/gu, " ").trim();
    }

    function priceRaw(value) {
        return String(value ?? "").replace(/[۰-۹٠-٩]/g, digit => String(digit.charCodeAt(0) - (digit.charCodeAt(0) >= 1776 ? 1776 : 1632))).replace(/[,٬\s]/g, "");
    }

    function formatPriceInput(input) {
        if (!(input instanceof HTMLInputElement)) return;
        const before = input.value, position = input.selectionStart;
        const raw = priceRaw(before);
        if (!/^\d*$/.test(raw)) return;
        input.value = raw.replace(/\B(?=(\d{3})+(?!\d))/g, ",");
        if (position != null && document.activeElement === input) {
            const digitsBefore = priceRaw(before.slice(0, position)).length;
            let cursor = 0, seen = 0;
            while (cursor < input.value.length && seen < digitsBefore) if (/\d/.test(input.value[cursor++])) seen++;
            input.setSelectionRange(cursor, cursor);
        }
    }

    function initPriceInput(input) {
        input.type = "text";
        input.inputMode = "numeric";
        input.dir = "ltr";
        input.dataset.adminPrice = "";
        formatPriceInput(input);
        setPersianValidationMessage(input);
    }

    function priceValue(input) {
        const raw = priceRaw(input.value);
        return /^\d+$/.test(raw) ? Number(raw) : NaN;
    }

    function setPriceValue(input, value) {
        input.value = value == null ? "" : String(value);
        initPriceInput(input);
    }

    function setPersianValidationMessage(control) {
        if (!(control instanceof HTMLInputElement || control instanceof HTMLSelectElement || control instanceof HTMLTextAreaElement)) return;
        control.setCustomValidity("");
        if (control instanceof HTMLInputElement && control.hasAttribute("data-admin-price") && control.value.trim()) {
            const amount = priceValue(control);
            if (!Number.isSafeInteger(amount) || amount < 0 || (control.max && amount > Number(control.max))) {
                control.setCustomValidity(`${validationLabel(control)} باید عدد صحیح و غیرمنفی در محدودهٔ مجاز باشد.`);
                return;
            }
        }
        if (control.validity.valid) return;

        const label = validationLabel(control);
        const validity = control.validity;
        let message = "";
        if (validity.valueMissing) message = `${label} را وارد یا انتخاب کنید.`;
        else if (validity.typeMismatch && control.type === "email") message = `قالب ${label} معتبر نیست.`;
        else if (validity.typeMismatch && control.type === "url") message = `نشانی ${label} معتبر نیست.`;
        else if (validity.patternMismatch) message = `قالب ${label} با الگوی خواسته‌شده مطابقت ندارد.`;
        else if (validity.tooShort) message = `${label} باید دست‌کم ${control.minLength} نویسه باشد.`;
        else if (validity.tooLong) message = `${label} نباید بیشتر از ${control.maxLength} نویسه باشد.`;
        else if (validity.rangeUnderflow) message = `مقدار ${label} نباید کمتر از ${control.min} باشد.`;
        else if (validity.rangeOverflow) message = `مقدار ${label} نباید بیشتر از ${control.max} باشد.`;
        else if (validity.stepMismatch) message = `مقدار ${label} با گام مجاز سازگار نیست.`;
        else if (validity.badInput) message = `مقدار واردشده برای ${label} معتبر نیست.`;

        if (message) control.setCustomValidity(message);
    }

    document.addEventListener("input", event => {
        if (event.target instanceof HTMLInputElement && event.target.hasAttribute("data-admin-price")) formatPriceInput(event.target);
        setPersianValidationMessage(event.target);
    }, true);
    document.addEventListener("change", event => setPersianValidationMessage(event.target));
    document.addEventListener("invalid", event => setPersianValidationMessage(event.target), true);

    document.addEventListener("click", event => {
        if (!(event.target instanceof Element)) return;
        const collapseButton = event.target.closest("[data-admin-panel-collapse]");
        if (collapseButton) {
            const panel = collapseButton.closest(".panel");
            const body = document.getElementById(collapseButton.getAttribute("aria-controls"));
            if (body && panel?.contains(body)) {
                body.hidden = !body.hidden;
                const expanded = !body.hidden;
                collapseButton.setAttribute("aria-expanded", String(expanded));
                const label = expanded ? "جمع‌کردن پنل" : "بازکردن پنل";
                collapseButton.setAttribute("aria-label", label);
                collapseButton.title = label;
                const icon = collapseButton.querySelector("i");
                if (icon) icon.className = `fa ${expanded ? "fa-minus" : "fa-plus"}`;
            }
        }

        if (event.target.closest("[data-admin-page-reload]")) window.location.reload();
    });

    function createElement(tag, text, className) {
        const node = document.createElement(tag);
        if (text != null) node.textContent = text;
        if (className) node.className = className;
        return node;
    }

    function setStatus(target, message, error = false, options = {}) {
        if (!target) return;
        target.textContent = message;
        target.classList.toggle("text-danger", error);
        target.classList.toggle("text-muted", !error);
        if (options.compact) target.classList.add("small");
        if (options.hideEmpty) target.classList.toggle("d-none", !message);
    }

    const numberFormat = new Intl.NumberFormat("fa-IR");
    const labels = {
        id: "شناسه", code: "کد", name: "نام", englishName: "نام لاتین", brandName: "برند", logoPath: "مسیر لوگو", logoUrl: "نشانی لوگو",
        categoryName: "دسته‌بندی", parentId: "شناسهٔ والد", slug: "نشانی", countryCode: "کد کشور",
        source: "منبع", isFilterable: "قابل فیلتر", isHighlight: "نمایش برجسته", price: "قیمت (تومان)",
        priceAmount: "قیمت (تومان)", totalAmount: "مبلغ کل (تومان)", unitAmount: "مبلغ واحد (تومان)", discountPercent: "درصد تخفیف", stockQuantity: "موجودی", warehouseId: "شناسهٔ انبار",
        warehouseCode: "کد انبار", warehouseName: "انبار", companyName: "شرکت", isSalesWarehouse: "انبار فروش",
        isActive: "وضعیت", storeProductKey: "شناسهٔ محصول فروشگاه", productName: "کالا", variantName: "تنوع",
        salesPlanName: "طرح فروش", unitName: "واحد اصلی", secondaryUnits: "واحدهای فرعی", conversionFactor: "ضریب تبدیل به واحد پایه", quantity: "موجودی", minimumQuantity: "حداقل موجودی", isLowStock: "وضعیت حداقل",
        maxPurchaseQuantity: "حداکثر تعداد خرید", createdAtUtc: "زمان ایجاد", updatedAtUtc: "آخرین ویرایش",
        isStoreEnabled: "انتشار در فروشگاه", isWarehouseEnabled: "فعال در انبار", hasImage: "تصویر محصول", idBrand: "شناسهٔ برند",
        idCategory: "شناسهٔ دسته", idBaseUnit: "شناسهٔ واحد پایه", productKey: "کلید محصول",
        badge: "نشان کالا", variants: "تنوع‌ها", attributes: "ویژگی‌های تنوع",
        stockByWarehouseAndPlan: "موجودی به تفکیک انبار و طرح", benefits: "مزیت‌های کالا", specifications: "مشخصات فنی",
        feature: "عنوان مشخصه", value: "مقدار", documents: "فایل‌های کالا", media: "رسانهٔ کالا",
        mainPath: "مسیر تصویر اصلی", cardPath: "مسیر تصویر کارت", thumbnailPath: "مسیر تصویر بندانگشتی",
        imageList: "تصاویر تکمیلی", videoList: "ویدئوها", detail: "توضیح", icon: "آیکن", sortOrder: "ترتیب",
        isDefault: "تنوع پیش‌فرض", title: "عنوان", path: "مسیر فایل", src: "مسیر تصویر", thumbnail: "مسیر بندانگشتی",
        accessories: "متعلقات", description: "توضیح", isRequired: "اجباری", displayDiscountPercent: "درصد تخفیف نمایشی",
        unitHistory: "واحدهای غیرفعال", isBaseUnit: "واحد پایه", attributeDefinitions: "ویژگی‌ها", label: "عنوان", key: "کلید", values: "مقادیر", displayValue: "مقدار نمایشی", valueKey: "کلید مقدار",
        salesOffers: "پیشنهادهای فروش", isEnabled: "فعال", currentPlanPrices: "قیمت‌های جاری", priceHistory: "سابقهٔ قیمت", priceVersionAmount: "مبلغ ثبت‌شده پیش از تخفیف (تومان)", accessoryName: "متعلقه", idChangedBy: "شناسهٔ کاربر", changeReason: "دلیل تغییر"
    };

    function notify(message, kind = "info") {
        const region = document.getElementById("admin-toast-region");
        if (!region || !window.bootstrap?.Toast) return;
        const toast = document.createElement("div");
        const themeColor = kind === "danger" ? "danger" : kind === "success" ? "success" : "primary";
        toast.className = `toast text-bg-${themeColor}`;
        toast.setAttribute("role", kind === "danger" ? "alert" : "status");
        toast.setAttribute("aria-live", kind === "danger" ? "assertive" : "polite");
        toast.setAttribute("aria-atomic", "true");

        const row = document.createElement("div");
        row.className = "d-flex align-items-center gap-2 p-2";
        const icon = document.createElement("i");
        icon.className = `fa ms-2 ${kind === "danger" ? "fa-circle-exclamation" : kind === "success" ? "fa-circle-check" : "fa-circle-info"}`;
        icon.setAttribute("aria-hidden", "true");
        const text = document.createElement("div");
        text.className = "toast-body";
        text.textContent = message;
        const close = document.createElement("button");
        close.type = "button";
        close.className = "btn-close btn-close-white ms-auto";
        close.dataset.bsDismiss = "toast";
        close.setAttribute("aria-label", "بستن پیام");
        row.append(icon, text, close);
        toast.append(row);
        region.append(toast);
        toast.addEventListener("hidden.bs.toast", () => toast.remove(), { once: true });
        window.bootstrap.Toast.getOrCreateInstance(toast, { delay: 4500 }).show();
    }

    function withIcon(button, iconName) {
        if (!(button instanceof HTMLElement) || button.querySelector("i.fa, i.fas, i.far, svg")) return button;
        const icon = document.createElement("i");
        icon.className = `fa ${iconName} me-1`;
        icon.setAttribute("aria-hidden", "true");
        button.prepend(icon);
        return button;
    }

    const actionIcons = [
        [/جزئیات|نمایش/u, "fa-eye"], [/ویرایش/u, "fa-pen-to-square"], [/گذرواژه|رمز/u, "fa-key"],
        [/ذخیره|ثبت/u, "fa-floppy-disk"], [/غیرفعال‌سازی|غیرفعال/u, "fa-toggle-off"], [/حذف/u, "fa-trash-can"], [/فعال‌سازی/u, "fa-toggle-on"],
        [/انتشار/u, "fa-paper-plane"], [/تصویر|عکس|نشان|لوگو/u, "fa-image"], [/فایل|پیوست/u, "fa-paperclip"],
        [/رهگیری/u, "fa-truck-fast"], [/بستن|خوانده/u, "fa-check"], [/بازگشایی/u, "fa-rotate-left"],
        [/حداقل/u, "fa-sliders"], [/تنظیم/u, "fa-gear"], [/افزودن/u, "fa-plus"], [/کپی/u, "fa-copy"],
        [/متعلقه/u, "fa-puzzle-piece"], [/مشخصات/u, "fa-circle-info"], [/لینک/u, "fa-link"], [/برگشت/u, "fa-rotate-left"]
    ];

    function normalizeActionCell(cell) {
        if (!(cell instanceof HTMLTableCellElement) || !cell.closest("tr") || cell.closest("[data-mds-dtp], .mds-bs-dtp-container")) return;
        const buttons = [...cell.querySelectorAll("button")];
        if (!buttons.length) return;
        cell.classList.add("admin-row-actions");

        for (const button of buttons) {
            const icon = button.querySelector("i.fa, i.fas, i.far, svg");
            const alreadyStyled = button.dataset.adminActionStyled === "true";
            const visibleText = alreadyStyled ? button.textContent.trim() : [...button.childNodes]
                    .filter(node => node.nodeType === Node.TEXT_NODE || (node.nodeType === Node.ELEMENT_NODE && !node.matches("i, svg, .visually-hidden")))
                    .map(node => node.textContent.trim()).filter(Boolean).join(" ");
            const accessibleLabel = (button.getAttribute("aria-label") || button.title || visibleText || "عملیات").replace(/\s+/gu, " ").trim();
            const shortLabels = [
                ["adminRecord", "جزئیات"], ["productEdit", "ویرایش"], ["productUnpublish", "لغو انتشار"],
                ["productBenefits", "مزیت‌ها"], ["productAccessories", "متعلقات"],
                ["productImage", "تصویر"], ["productFile", "فایل"], ["productPublish", "انتشار"],
                ["customerEdit", "ویرایش"], ["customerDelete", "حذف"], ["adminMinimum", "حداقل"],
                ["orderTracking", "رهگیری"]
            ];
            const dataLabel = shortLabels.find(([key]) => button.hasAttribute(`data-${key.replace(/[A-Z]/g, letter => `-${letter.toLowerCase()}`)}`))?.[1];
            const caption = (alreadyStyled ? button.textContent : dataLabel || visibleText || accessibleLabel).replace(/\s+/gu, " ").trim() || "عملیات";
            if (alreadyStyled && icon && button.querySelector(":scope > span")?.textContent.trim() === caption) continue;
            const variant = button.classList.contains("btn-primary") ? "btn-primary"
                : button.classList.contains("btn-success") || button.classList.contains("btn-outline-success") ? "btn-outline-success"
                    : button.classList.contains("btn-danger") || button.classList.contains("btn-outline-danger") ? "btn-outline-danger"
                        : button.classList.contains("btn-warning") ? "btn-warning" : "btn-default";
            button.className = `btn admin-action-btn ${variant}`;
            button.title = accessibleLabel;
            button.setAttribute("aria-label", accessibleLabel);

            const dataIcon = button.hasAttribute("data-product-unpublish") ? "fa-eye-slash"
                : button.hasAttribute("data-product-publish") ? "fa-paper-plane" : null;
            const iconName = dataIcon || actionIcons.find(([pattern]) => pattern.test(caption))?.[1] || "fa-arrow-pointer";
            const actionIcon = icon || document.createElement("i");
            actionIcon.className = icon ? icon.className : `fa ${iconName}`;
            actionIcon.classList.remove("me-1", "ms-1", "me-2", "ms-2");
            actionIcon.setAttribute("aria-hidden", "true");
            const captionNode = document.createElement("span");
            captionNode.textContent = caption;
            button.replaceChildren(actionIcon, captionNode);
            button.dataset.adminActionStyled = "true";
        }
    }

    function normalizeActionRows(root) {
        if (root instanceof HTMLTableCellElement) normalizeActionCell(root);
        else root.closest?.("td") && normalizeActionCell(root.closest("td"));
        root.querySelectorAll?.("td").forEach(normalizeActionCell);
    }

    normalizeActionRows(document);
    new MutationObserver(records => {
        for (const record of records) for (const node of record.addedNodes)
            if (node instanceof Element) normalizeActionRows(node);
    }).observe(document.body, { childList: true, subtree: true });

    function ensureConfirmModal() {
        let modal = document.getElementById("admin-action-confirm-modal");
        if (modal) return modal;

        modal = document.createElement("div");
        modal.className = "modal fade admin-action-confirm-modal";
        modal.id = "admin-action-confirm-modal";
        modal.tabIndex = -1;
        modal.setAttribute("aria-labelledby", "admin-action-confirm-title");
        modal.setAttribute("aria-hidden", "true");
        modal.innerHTML = `<div class="modal-dialog modal-dialog-centered modal-sm"><div class="modal-content"><div class="modal-header"><h2 class="modal-title fs-5" id="admin-action-confirm-title">تأیید عملیات</h2><button type="button" class="btn-close ms-0" data-bs-dismiss="modal" aria-label="بستن"></button></div><div class="modal-body"><p class="mb-0" data-admin-confirm-message></p></div><div class="modal-footer"><button type="button" class="btn btn-default" data-bs-dismiss="modal">انصراف</button><button type="button" class="btn btn-primary" data-admin-confirm-accept>تأیید و ادامه</button></div></div></div>`;
        document.body.append(modal);
        return modal;
    }

    function confirmAction(message, title = "تأیید عملیات") {
        const modal = ensureConfirmModal();
        if (!window.bootstrap?.Modal) return Promise.resolve(window.confirm(message || "آیا از انجام این تغییر مطمئن هستید؟"));

        return new Promise(resolve => {
            const heading = modal.querySelector("#admin-action-confirm-title");
            const content = modal.querySelector("[data-admin-confirm-message]");
            const accept = modal.querySelector("[data-admin-confirm-accept]");
            const instance = window.bootstrap.Modal.getOrCreateInstance(modal, { backdrop: "static" });
            let approved = false;
            heading.textContent = title;
            content.textContent = message || "آیا از انجام این تغییر مطمئن هستید؟";
            const finish = () => {
                accept.removeEventListener("click", approve);
                modal.removeEventListener("hidden.bs.modal", finish);
                resolve(approved);
            };
            const approve = () => { approved = true; instance.hide(); };
            accept.addEventListener("click", approve, { once: true });
            modal.addEventListener("hidden.bs.modal", finish, { once: true });
            instance.show();
        });
    }

    const pendingConfirmations = new WeakSet();
    const approvedButtons = new WeakSet();
    const approvedForms = new WeakSet();
    document.addEventListener("click", event => {
        if (!(event.target instanceof Element)) return;
        const button = event.target.closest("button[data-admin-confirm]");
        if (!button || approvedButtons.has(button) || pendingConfirmations.has(button)) return;
        if (button.type === "submit" && button.form?.hasAttribute("data-admin-confirm")) return;

        event.preventDefault();
        event.stopImmediatePropagation();
        pendingConfirmations.add(button);
        void confirmAction(button.dataset.adminConfirm).then(approved => {
            if (!approved || !button.isConnected || button.disabled) return;
            approvedButtons.add(button);
            button.click();
            approvedButtons.delete(button);
        }).finally(() => pendingConfirmations.delete(button));
    }, true);

    document.addEventListener("submit", event => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || !form.hasAttribute("data-admin-confirm") || approvedForms.has(form) || pendingConfirmations.has(form)) return;

        event.preventDefault();
        event.stopImmediatePropagation();
        pendingConfirmations.add(form);
        const submitter = event.submitter;
        void confirmAction(form.dataset.adminConfirm).then(approved => {
            if (!approved || !form.isConnected) return;
            approvedForms.add(form);
            if (submitter) form.requestSubmit(submitter);
            else form.requestSubmit();
            approvedForms.delete(form);
        }).finally(() => pendingConfirmations.delete(form));
    }, true);

    async function getJson(url, options = {}) {
        const { headers: suppliedHeaders, ...requestOptions } = options;
        const response = await fetch(url, {
            cache: "no-store",
            credentials: "same-origin",
            headers: { Accept: "application/json", "X-Requested-With": "XMLHttpRequest", ...suppliedHeaders },
            ...requestOptions
        });
        let body;
        try { body = await response.json(); } catch { body = null; }
        if (!response.ok) {
            const validation = body?.errors && Object.values(body.errors).flat().find(message => typeof message === "string" && message.trim());
            throw new Error(body?.detail || validation || body?.title || `پاسخ سرور ${response.status} بود.`);
        }
        return body;
    }

    function formatDateTime(value) {
        if (value == null || value === "") return "—";
        const raw = typeof value === "string" ? value.trim() : value;
        const dateOnly = typeof raw === "string" && /^\d{4}-\d{2}-\d{2}$/.test(raw);
        const normalized = typeof raw === "string" && /^\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}/.test(raw)
            ? raw.replace(" ", "T").replace(/(?<!Z|[+-]\d{2}:\d{2})$/i, "Z") : raw;
        const date = new Date(normalized);
        if (!Number.isFinite(date.getTime())) return "—";
        return new Intl.DateTimeFormat("fa-IR-u-ca-persian", {
            dateStyle: "short", ...(dateOnly ? {} : { timeStyle: "short" }),
            timeZone: dateOnly ? "UTC" : "Asia/Tehran"
        }).format(date);
    }

    function renderRecord(body, record, options = {}) {
        body.replaceChildren();
        const entries = Object.entries(record || {});
        if (!entries.length) {
            const empty = document.createElement("p");
            empty.className = "admin-record-empty";
            empty.textContent = "اطلاعاتی برای این رکورد ثبت نشده است.";
            body.append(empty);
            return;
        }

        const labelFor = key => labels[key] || String(key).replace(/([a-z\d])([A-Z])/g, "$1 $2").replace(/^./, char => char.toLocaleUpperCase("fa-IR"));
        const makeValue = (value, key = "") => {
            const container = document.createElement("div");
            container.className = "admin-record-value";
            if (typeof value === "string" && /(?:date|time|at(?:utc)?|utc)$/i.test(key) && /^\d{4}-\d{2}-\d{2}/.test(value)) {
                container.textContent = formatDateTime(value); return container;
            }
            if (options.layout === "product" && value && typeof value === "object" && (!Array.isArray(value) || value.length)) {
                const items = Array.isArray(value) ? value : [value];
                if (items.every(item => item && typeof item === "object" && !Array.isArray(item))) {
                    const keys = [...new Set(items.flatMap(item => Object.keys(item)))];
                    if (!keys.length) { container.textContent = "—"; return container; }
                    const wrapper = document.createElement("div"); wrapper.className = "admin-record-table-scroll";
                    const table = document.createElement("table"); table.className = "admin-record-table admin-record-table-nested";
                    const head = document.createElement("thead"); const header = document.createElement("tr");
                    for (const key of keys) { const cell = document.createElement("th"); cell.scope = "col"; cell.textContent = labelFor(key); header.append(cell); }
                    head.append(header); table.append(head);
                    const rows = document.createElement("tbody");
                    for (const item of items) {
                        const row = document.createElement("tr");
                        for (const key of keys) { const cell = document.createElement("td"); cell.append(makeValue(item[key], key)); row.append(cell); }
                        rows.append(row);
                    }
                    table.append(rows); wrapper.append(table); container.append(wrapper); return container;
                }
            }
            if (value == null || value === "") container.textContent = "—";
            else if (typeof value === "boolean") container.textContent = value ? "بله" : "خیر";
            else if (typeof value === "number") container.textContent = numberFormat.format(value);
            else if (Array.isArray(value)) {
                if (!value.length) container.textContent = "موردی ثبت نشده است";
                else {
                    const list = document.createElement("ul");
                    list.className = "admin-record-list";
                    for (const item of value) {
                        const row = document.createElement("li");
                        if (item && typeof item === "object") {
                            const nested = document.createElement("dl");
                            nested.className = "admin-record-nested-grid";
                            for (const [key, child] of Object.entries(item)) {
                                const term = document.createElement("dt"); term.textContent = labelFor(key);
                                const description = document.createElement("dd"); description.append(makeValue(child, key));
                                nested.append(term, description);
                            }
                            row.append(nested);
                        } else row.append(makeValue(item));
                        list.append(row);
                    }
                    container.append(list);
                }
            } else if (typeof value === "object") {
                const nested = document.createElement("dl");
                nested.className = "admin-record-nested-grid";
                for (const [key, child] of Object.entries(value)) {
                    const term = document.createElement("dt"); term.textContent = labelFor(key);
                    const description = document.createElement("dd"); description.append(makeValue(child, key));
                    nested.append(term, description);
                }
                container.append(nested);
            } else container.textContent = String(value);
            return container;
        };
        const makeProductMedia = media => {
            const gallery = document.createElement("div");
            gallery.className = "admin-record-media-gallery";
            if (!media || typeof media !== "object") {
                gallery.textContent = "رسانه‌ای برای این کالا ثبت نشده است.";
                return gallery;
            }
            const cdnBase = document.querySelector('meta[name="media-cdn-base-url"]')?.content || "";
            const mediaUrl = path => {
                if (typeof path !== "string" || !path.trim()) return null;
                const raw = path.trim();
                const relative = raw.replace(/^[~/]+/, "");
                const candidate = /^content\//i.test(relative) && cdnBase ? `${cdnBase.replace(/\/$/, "")}/${relative}` : raw;
                try {
                    const url = new URL(candidate, window.location.origin);
                    return ["http:", "https:"].includes(url.protocol) ? url.href : null;
                } catch { return null; }
            };
            const primaryMedia = document.createElement("div"); primaryMedia.className = "admin-record-media-primary";
            const additionalMedia = document.createElement("div"); additionalMedia.className = "admin-record-media-additional";
            const appendMedia = (host, path, label, video = false) => {
                if (!path) return;
                const card = document.createElement("figure");
                card.className = "admin-record-media-card";
                const caption = document.createElement("figcaption"); caption.textContent = label;
                const url = mediaUrl(path);
                if (url) {
                    const preview = document.createElement(video ? "video" : "img");
                    if (video) { preview.controls = true; preview.preload = "none"; preview.setAttribute("aria-label", label); }
                    else { preview.alt = label; preview.loading = "lazy"; }
                    preview.src = url;
                    preview.addEventListener("error", () => {
                        const message = document.createElement("span"); message.textContent = "نمایش فایل ممکن نیست.";
                        preview.replaceWith(message);
                    }, { once: true });
                    card.append(preview);
                } else {
                    const message = document.createElement("span"); message.textContent = "نشانی فایل معتبر نیست.";
                    card.append(message);
                }
                card.append(caption); host.append(card);
            };
            appendMedia(primaryMedia, media.mainPath, "تصویر اصلی");
            appendMedia(primaryMedia, media.cardPath, "تصویر کارت");
            appendMedia(primaryMedia, media.thumbnailPath, "تصویر بندانگشتی");
            if (Array.isArray(media.imageList)) media.imageList.forEach((image, index) => {
                const path = typeof image === "string" ? image : image?.src || image?.Src || image?.path || image?.Path;
                appendMedia(additionalMedia, path, `تصویر تکمیلی ${numberFormat.format(index + 1)}`);
            });
            if (Array.isArray(media.videoList)) media.videoList.forEach((video, index) => {
                const path = typeof video === "string" ? video : video?.src || video?.Src || video?.path || video?.Path;
                appendMedia(additionalMedia, path, `ویدئو ${numberFormat.format(index + 1)}`, true);
            });
            if (primaryMedia.childElementCount) gallery.append(primaryMedia);
            if (additionalMedia.childElementCount) gallery.append(additionalMedia);
            if ((media.imageList && !Array.isArray(media.imageList)) || (media.videoList && !Array.isArray(media.videoList))) {
                const message = document.createElement("p"); message.textContent = "فهرست رسانه‌های تکمیلی قابل خواندن نیست."; gallery.append(message);
            }
            if (!gallery.children.length) gallery.textContent = "رسانه‌ای برای این کالا ثبت نشده است.";
            return gallery;
        };
        const groups = options.groups || [
            { title: "شناسه و مشخصات", keys: /^(id|code|key|productKey|storeProductKey|name|englishName|brandName|categoryName|parentId|slug|warehouseCode|warehouseName|companyName|productName|variantName|salesPlanName|unitName)$/i },
            { title: "قیمت و مقدار", keys: /(price|amount|quantity|stock|minimum|discount|maximum|maxPurchase)/i },
            { title: "وضعیت و زمان", keys: /^(is|has|status|createdAt|updatedAt|deletedAt|publishedAt|.*Utc$)/i }
        ];
        const consumed = new Set();
        const appendSection = (sectionTitle, sectionEntries, compact = false) => {
            if (!sectionEntries.length) return;
            const section = document.createElement(options.accordion ? "details" : "section"); section.className = "admin-record-section";
            if (options.accordion) section.open = true;
            if (compact) section.classList.add("admin-record-section-compact");
            if (options.layout === "product" && sectionEntries.some(([, value]) => value && typeof value === "object")) section.classList.add("admin-record-section-wide");
            const sectionHeading = document.createElement(options.accordion ? "summary" : "h3"); sectionHeading.className = "admin-record-section-title"; sectionHeading.textContent = sectionTitle;
            const productLayout = options.layout === "product";
            const grid = document.createElement(productLayout ? "table" : "dl");
            grid.className = productLayout ? "admin-record-table" : "admin-record-grid";
            const rows = productLayout ? document.createElement("tbody") : grid;
            const headerRow = productLayout ? document.createElement("tr") : null;
            const valueRow = productLayout ? document.createElement("tr") : null;
            for (const [key, value] of sectionEntries) {
                const field = productLayout ? null : document.createElement("div"); if (field) field.className = "admin-record-field";
                const term = document.createElement(productLayout ? "th" : "dt"); term.className = "admin-record-label"; term.textContent = labelFor(key);
                if (productLayout) term.scope = "col";
                const description = document.createElement(productLayout ? "td" : "dd"); description.className = "admin-record-data";
                if (options.layout === "product" && key === "media") {
                    description.append(makeProductMedia(value));
                } else if ((key === "logoPath" || key === "logoUrl") && typeof value === "string" && value.length > 0) {
                    const input = document.createElement("input");
                    input.className = "form-control form-control-sm admin-record-copy-value";
                    input.type = "text";
                    input.value = value;
                    input.readOnly = true;
                    input.setAttribute("aria-label", labelFor(key));
                    input.addEventListener("focus", () => input.select());
                    input.addEventListener("click", () => input.select());
                    description.append(input);
                    if (key === "logoUrl") {
                        const preview = document.createElement("img");
                        preview.className = "admin-record-logo-preview";
                        preview.src = value;
                        preview.alt = "پیش‌نمایش لوگوی برند";
                        preview.loading = "lazy";
                        preview.addEventListener("error", () => {
                            const unavailable = document.createElement("span");
                            unavailable.className = "admin-record-logo-unavailable";
                            unavailable.textContent = "پیش‌نمایش لوگو در دسترس نیست.";
                            preview.replaceWith(unavailable);
                        }, { once: true });
                        description.append(preview);
                    }
                } else description.append(makeValue(value, key));
                if (productLayout) { headerRow.append(term); valueRow.append(description); }
                else { field.append(term, description); rows.append(field); }
                consumed.add(key);
            }
            if (productLayout) {
                const head = document.createElement("thead"); head.append(headerRow);
                rows.append(valueRow); grid.append(head, rows);
                const wrapper = document.createElement("div"); wrapper.className = "admin-record-table-scroll";
                wrapper.append(grid); section.append(sectionHeading, wrapper);
            } else section.append(sectionHeading, grid);
            body.append(section);
        };
        for (const group of groups) {
            const sectionEntries = Array.isArray(group.keys)
                ? group.keys.filter(key => Object.hasOwn(record, key) && !consumed.has(key)).map(key => [key, record[key]])
                : entries.filter(([key]) => !consumed.has(key) && group.keys.test(key));
            appendSection(group.title, sectionEntries, group.compact);
        }
        appendSection("اطلاعات تکمیلی", entries.filter(([key]) => !consumed.has(key)));
    }

    function showRecord(title, record, options = {}) {
        const modal = document.getElementById("admin-record-modal");
        const body = modal?.querySelector("[data-admin-record-body]");
        const heading = modal?.querySelector("#admin-record-title");
        if (!modal || !body || !window.bootstrap?.Modal) {
            notify("ابزار نمایش جزئیات در دسترس نیست.", "danger");
            return;
        }
        modal.classList.toggle("admin-record-product", options.layout === "product");
        heading.textContent = title;
        renderRecord(body, record, options);
        window.bootstrap.Modal.getOrCreateInstance(modal).show();
    }

    const persianTableLanguage = {
        emptyTable: "داده‌ای برای نمایش وجود ندارد",
        info: "نمایش _START_ تا _END_ از _TOTAL_ ردیف",
        infoEmpty: "نمایش ۰ تا ۰ از ۰ ردیف",
        infoFiltered: "(فیلترشده از _MAX_ ردیف)",
        lengthMenu: "نمایش _MENU_ ردیف",
        loadingRecords: "در حال بارگذاری…",
        processing: "در حال پردازش…",
        search: "جست‌وجو:",
        zeroRecords: "ردیفی با این جست‌وجو پیدا نشد",
        paginate: { first: "نخست", last: "پایان", next: "بعدی", previous: "قبلی" },
        aria: {
            orderable: ": برای مرتب‌سازی کلیک کنید",
            orderableReverse: ": برای مرتب‌سازی معکوس کلیک کنید",
            orderableRemove: ": برای برداشتن مرتب‌سازی کلیک کنید",
            paginate: { first: "نخست", last: "پایان", next: "بعدی", previous: "قبلی", number: "" }
        }
    };

    const dataTableInstances = new WeakMap();

    function destroyDataTable(table) {
        const instance = table && dataTableInstances.get(table);
        if (!instance) return;
        instance.destroy();
        dataTableInstances.delete(table);
    }

    function getDataTable(table) {
        return table ? dataTableInstances.get(table) || null : null;
    }

    function decorateTableActions(table) {
        const icons = [
            [/ویرایش|تنظیم/u, "fa-pen"],
            [/حذف/u, "fa-trash"],
            [/خروج از انتشار/u, "fa-eye-slash"],
            [/انتشار/u, "fa-paper-plane"],
            [/تصویر|نشان/u, "fa-image"],
            [/فایل/u, "fa-file"],
            [/رهگیری/u, "fa-truck"],
            [/گذرواژه/u, "fa-key"],
            [/کپی/u, "fa-copy"],
            [/ذخیره|ثبت/u, "fa-floppy-disk"],
            [/افزودن|ایجاد/u, "fa-plus"],
            [/جست‌وجو|جستجو/u, "fa-magnifying-glass"],
            [/نمایش|جزئیات/u, "fa-eye"],
            [/انصراف/u, "fa-xmark"]
        ];
        table.querySelectorAll("tbody button.btn").forEach(button => {
            if (button.querySelector("i.fa, i.fas, i.far, svg")) return;
            const text = button.getAttribute("aria-label") || button.textContent || "";
            const match = icons.find(([pattern]) => pattern.test(text));
            if (!match) return;
            const icon = document.createElement("i");
            icon.className = `fa ${match[1]}`;
            icon.setAttribute("aria-hidden", "true");
            button.prepend(icon);
        });
    }

    function refreshDataTable(table) {
        if (!table || !table.matches("[data-admin-datatable]")) return;
        decorateTableActions(table);
        if (!window.DataTable) return;
        destroyDataTable(table);
        if (table.dataset.adminTableMode === "tree") return;
        if ([...(table.tBodies[0]?.rows || [])].some(row => [...row.cells].some(cell => cell.colSpan > 1))) return;
        const rowCount = table.tBodies[0]?.rows.length || 0;
        const header = table.tHead?.rows[table.tHead.rows.length - 1];
        const expectedColumns = header ? [...header.cells].reduce((total, cell) => total + cell.colSpan, 0) : 0;
        const malformedRow = [...(table.tBodies[0]?.rows || [])].find(row => [...row.cells].reduce((total, cell) => total + cell.colSpan, 0) !== expectedColumns);
        if (expectedColumns && malformedRow) {
            console.error(`جدول مدیریت راه‌اندازی نشد: تعداد ستون‌های ردیف با سرستون برابر نیست (${table.id || "بدون شناسه"}).`);
            return;
        }
        const externallyPaged = table.dataset.adminTableMode === "server";
        const pageLength = Number(table.dataset.adminPageLength) || 25;
        const pageLengths = [...new Set([10, pageLength, 25, 50, 100])].sort((a, b) => a - b);
        const hasActions = [...(table.tHead?.rows[0]?.cells || [])].some(cell => /عملیات|اقدام/u.test(cell.textContent || ""));
        const localizePaging = () => {
            const controls = table.closest(".dt-container")?.querySelectorAll(".dt-paging-button .page-link:not(.first):not(.previous):not(.next):not(.last)") || [];
            controls.forEach(control => { control.textContent = control.textContent.replace(/\d/g, digit => "۰۱۲۳۴۵۶۷۸۹"[Number(digit)]); });
        };
        table.dir = "rtl";
        const instance = new window.DataTable(table, {
            language: persianTableLanguage,
            paging: !externallyPaged && rowCount > pageLength,
            pageLength,
            lengthChange: !externallyPaged && rowCount > pageLength,
            lengthMenu: [pageLengths, pageLengths.map(length => new Intl.NumberFormat("fa-IR").format(length))],
            searching: !externallyPaged && table.dataset.adminTableSearch !== "false",
            ordering: !externallyPaged,
            order: [],
            autoWidth: false,
            deferRender: true,
            info: !externallyPaged,
            infoCallback: (settings, start, end, max, total) => `نمایش ${new Intl.NumberFormat("fa-IR").format(start)} تا ${new Intl.NumberFormat("fa-IR").format(end)} از ${new Intl.NumberFormat("fa-IR").format(total)} ردیف`,
            columnDefs: hasActions ? [{ targets: -1, orderable: false, searchable: false }] : []
        });
        instance.on("draw", localizePaging);
        dataTableInstances.set(table, instance);
        const controlRow = table.closest(".dt-container")?.querySelector(".dt-search")?.closest(".dt-layout-end")?.parentElement;
        const searchCell = controlRow?.querySelector(":scope > .dt-layout-end");
        if (searchCell) {
            controlRow.classList.add("admin-datatable-control-row");
            const startCell = controlRow.querySelector(":scope > .dt-layout-start");
            if (startCell && searchCell !== controlRow.firstElementChild) controlRow.insertBefore(searchCell, controlRow.firstElementChild);
            [...controlRow.children].forEach(cell => cell.classList.remove("ms-auto", "me-auto"));
            searchCell.classList.add("admin-datatable-search-cell");
            if (startCell && !startCell.querySelector("*") && !startCell.textContent.trim()) startCell.hidden = true;
        }
        localizePaging();
    }

    function renderNumberedPagination(container, currentPage, totalPages, onPage, label = "صفحه‌بندی") {
        if (!container) return;
        const pageCount = Math.max(1, Number(totalPages) || 1);
        const page = Math.min(pageCount, Math.max(1, Number(currentPage) || 1));
        const format = new Intl.NumberFormat("fa-IR");
        container.replaceChildren();
        container.hidden = pageCount <= 1;
        container.setAttribute("role", "navigation");
        container.setAttribute("aria-label", label);

        const addButton = (text, target, { disabled = false, current = false, title } = {}) => {
            const button = document.createElement("button");
            button.type = "button";
            button.className = `btn btn-sm ${current ? "btn-primary" : "btn-default"}`;
            button.textContent = text;
            button.disabled = disabled;
            if (current) button.setAttribute("aria-current", "page");
            if (title) button.setAttribute("aria-label", title);
            button.addEventListener("click", () => {
                if (!disabled && target !== page) onPage(target);
            });
            container.append(button);
        };
        const addGap = () => {
            const gap = document.createElement("span");
            gap.className = "admin-pagination__gap";
            gap.textContent = "…";
            gap.setAttribute("aria-hidden", "true");
            container.append(gap);
        };

        addButton("قبلی", page - 1, { disabled: page <= 1, title: "صفحهٔ قبل" });
        const visiblePages = [...new Set([1, page - 1, page, page + 1, pageCount])]
            .filter(value => value >= 1 && value <= pageCount)
            .sort((first, second) => first - second);
        let previous = 0;
        for (const target of visiblePages) {
            if (previous && target - previous > 1) addGap();
            addButton(format.format(target), target, { current: target === page, title: `صفحهٔ ${format.format(target)}` });
            previous = target;
        }
        addButton("بعدی", page + 1, { disabled: page >= pageCount, title: "صفحهٔ بعد" });
    }

    // Filter forms retain their existing submit handlers and paging/validation logic.
    document.addEventListener("change", event => {
        const control = event.target;
        if (!(control instanceof HTMLSelectElement) &&
            !(control instanceof HTMLInputElement && (control.matches('[type="checkbox"], [type="radio"], [type="date"], [type="datetime-local"]') || control.hasAttribute("data-admin-persian-date-value")))) return;
        const form = control.form;
        if (!form?.matches(".admin-filter-bar, .admin-sales-plans__filter") || !form.matches(":valid")) return;
        form.requestSubmit();
    });

    window.AdminUI = Object.freeze({
        createElement,
        setStatus,
        getJson,
        confirm: confirmAction,
        notify,
        showRecord,
        renderRecord,
        getDataTable,
        destroyDataTable,
        refreshDataTable,
        withIcon,
        renderNumberedPagination,
        initStatusSwitch,
        initPriceInput,
        setPriceValue,
        priceValue,
        formatDateTime,
        formatNumber: value => numberFormat.format(value ?? 0)
    });
})();
