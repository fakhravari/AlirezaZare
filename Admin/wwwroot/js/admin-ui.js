(() => {
    "use strict";

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

    window.AdminUI = Object.freeze({
        getJson,
        notify,
        showRecord,
        formatNumber: value => numberFormat.format(value ?? 0)
    });
})();
