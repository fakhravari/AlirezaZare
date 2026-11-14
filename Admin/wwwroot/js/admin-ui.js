(() => {
    "use strict";

    // Admin dialogs stay open when the backdrop is clicked; Escape and explicit
    // dismiss buttons continue to work. Set this before page scripts create instances.
    if (window.bootstrap?.Modal?.Default) window.bootstrap.Modal.Default.backdrop = "static";

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

    document.addEventListener("click", event => {
        if (!(event.target instanceof Element)) return;
        const collapseButton = event.target.closest("[data-admin-panel-collapse]");
        if (collapseButton) {
            const body = collapseButton.closest(".admin-page-widget")?.querySelector(".panel-body");
            if (body) {
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

    const numberFormat = new Intl.NumberFormat("fa-IR");
    const labels = {
        id: "شناسه", code: "کد", name: "نام", englishName: "نام لاتین", brandName: "برند",
        categoryName: "دسته‌بندی", parentId: "شناسهٔ والد", slug: "نشانی", countryCode: "کد کشور",
        source: "منبع", isFilterable: "قابل فیلتر", isHighlight: "نمایش برجسته", price: "قیمت (تومان)",
        priceAmount: "قیمت پایه (تومان)", discountPercent: "درصد تخفیف", stockQuantity: "موجودی", warehouseId: "شناسهٔ انبار",
        warehouseCode: "کد انبار", warehouseName: "انبار", companyName: "شرکت", isSalesWarehouse: "انبار فروش",
        isActive: "وضعیت", storeProductKey: "شناسهٔ محصول فروشگاه", productName: "کالا", variantName: "تنوع",
        salesPlanName: "طرح فروش", unitName: "واحد اصلی", quantity: "موجودی", minimumQuantity: "حداقل موجودی", isLowStock: "وضعیت حداقل",
        maxPurchaseQuantity: "حداکثر تعداد خرید", createdAtUtc: "زمان ایجاد", updatedAtUtc: "آخرین ویرایش", isCatalogActive: "انتشار در فروشگاه",
        isStoreEnabled: "فعال در فروشگاه", isWarehouseEnabled: "فعال در انبار", hasImage: "تصویر محصول", idBrand: "شناسهٔ برند",
        idCategory: "شناسهٔ دسته", idBaseUnit: "شناسهٔ واحد پایه", productKey: "کلید محصول"
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
        if (!(cell instanceof HTMLTableCellElement) || !cell.closest("tr")) return;
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

    function showRecord(title, record) {
        const modal = document.getElementById("admin-record-modal");
        const body = modal?.querySelector("[data-admin-record-body]");
        const heading = modal?.querySelector("#admin-record-title");
        if (!modal || !body || !window.bootstrap?.Modal) {
            notify("ابزار نمایش جزئیات در دسترس نیست.", "danger");
            return;
        }

        heading.textContent = title;
        body.replaceChildren();
        const entries = Object.entries(record || {});
        if (!entries.length) {
            const empty = document.createElement("p");
            empty.className = "admin-record-empty";
            empty.textContent = "اطلاعاتی برای این رکورد ثبت نشده است.";
            body.append(empty);
            window.bootstrap.Modal.getOrCreateInstance(modal).show();
            return;
        }

        const labelFor = key => labels[key] || String(key).replace(/([a-z\d])([A-Z])/g, "$1 $2").replace(/^./, char => char.toLocaleUpperCase("fa-IR"));
        const makeValue = value => {
            const container = document.createElement("div");
            container.className = "admin-record-value";
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
                                const description = document.createElement("dd"); description.append(makeValue(child));
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
                    const description = document.createElement("dd"); description.append(makeValue(child));
                    nested.append(term, description);
                }
                container.append(nested);
            } else container.textContent = String(value);
            return container;
        };
        const groups = [
            { title: "شناسه و مشخصات", keys: /^(id|code|key|productKey|storeProductKey|name|englishName|brandName|categoryName|parentId|slug|warehouseCode|warehouseName|companyName|productName|variantName|salesPlanName|unitName)$/i },
            { title: "قیمت و مقدار", keys: /(price|amount|quantity|stock|minimum|discount|maximum|maxPurchase)/i },
            { title: "وضعیت و زمان", keys: /^(is|has|status|createdAt|updatedAt|deletedAt|publishedAt|.*Utc$)/i }
        ];
        const consumed = new Set();
        const appendSection = (sectionTitle, sectionEntries) => {
            if (!sectionEntries.length) return;
            const section = document.createElement("section"); section.className = "admin-record-section";
            const sectionHeading = document.createElement("h3"); sectionHeading.className = "admin-record-section-title"; sectionHeading.textContent = sectionTitle;
            const grid = document.createElement("dl"); grid.className = "admin-record-grid";
            for (const [key, value] of sectionEntries) {
                const field = document.createElement("div"); field.className = "admin-record-field";
                const term = document.createElement("dt"); term.className = "admin-record-label"; term.textContent = labelFor(key);
                const description = document.createElement("dd"); description.className = "admin-record-data"; description.append(makeValue(value));
                field.append(term, description); grid.append(field); consumed.add(key);
            }
            section.append(sectionHeading, grid); body.append(section);
        };
        for (const group of groups) appendSection(group.title, entries.filter(([key]) => !consumed.has(key) && group.keys.test(key)));
        appendSection("اطلاعات تکمیلی", entries.filter(([key]) => !consumed.has(key)));
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
            lengthMenu: [[10, 25, 50, 100], ["۱۰", "۲۵", "۵۰", "۱۰۰"]],
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

    window.AdminUI = Object.freeze({
        getJson,
        notify,
        showRecord,
        getDataTable,
        destroyDataTable,
        refreshDataTable,
        withIcon,
        formatNumber: value => numberFormat.format(value ?? 0)
    });
})();
