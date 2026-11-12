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
        source: "منبع", isFilterable: "قابل فیلتر", isHighlight: "نمایش برجسته", price: "قیمت",
        discountPercent: "درصد تخفیف", stockQuantity: "موجودی", warehouseId: "شناسهٔ انبار",
        warehouseCode: "کد انبار", warehouseName: "انبار", companyName: "شرکت", isSalesWarehouse: "انبار فروش",
        isActive: "وضعیت", storeProductKey: "شناسهٔ محصول فروشگاه", productName: "کالا", variantName: "تنوع",
        salesPlanName: "طرح فروش", unitName: "واحد اصلی", quantity: "موجودی", minimumQuantity: "حداقل موجودی", isLowStock: "وضعیت حداقل"
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
        const list = document.createElement("dl");
        list.className = "row g-0";
        for (const [key, value] of Object.entries(record || {})) {
            const term = document.createElement("dt");
            term.className = "col-sm-4 col-lg-3 p-2 border-bottom text-secondary fw-normal";
            term.textContent = labels[key] || key;
            const description = document.createElement("dd");
            description.className = "col-sm-8 col-lg-9 p-2 border-bottom mb-0";
            if (typeof value === "boolean") description.textContent = value ? "بله" : "خیر";
            else if (value == null || value === "") description.textContent = "—";
            else if (typeof value === "number") description.textContent = numberFormat.format(value);
            else if (typeof value === "object") description.textContent = JSON.stringify(value);
            else description.textContent = String(value);
            list.append(term, description);
        }
        body.append(list);
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
