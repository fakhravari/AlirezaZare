(() => {
    "use strict";

    // Color Admin persists layout state by default. This panel keeps no browser-side state.
    if (window.App && typeof window.App.settings === "function") {
        window.App.settings({ disableLocalStorage: true, disableSidebarScrollMemory: true });
    }

    const content = document.getElementById("content");
    const sidebar = document.getElementById("sidebar");
    if (!content) return;

    const numberFormat = new Intl.NumberFormat("fa-IR");
    let requestNumber = 0;

    function showStatus(page, message, isError = false) {
        const status = page.querySelector("[data-admin-status]");
        if (!status) return;
        status.textContent = message;
        status.classList.toggle("is-error", isError);
    }

    async function readJson(url) {
        const response = await fetch(url, {
            method: "GET",
            cache: "no-store",
            headers: { Accept: "application/json", "X-Requested-With": "XMLHttpRequest" }
        });
        let body;
        try { body = await response.json(); }
        catch { body = null; }
        if (!response.ok) {
            throw new Error(body?.title || body?.detail || `پاسخ سرور ${response.status} بود.`);
        }
        return body;
    }

    function appendCell(row, value, className = "") {
        const cell = document.createElement("td");
        if (className) cell.className = className;
        cell.textContent = value == null || value === "" ? "—" : String(value);
        row.append(cell);
    }

    function updateTable(page, rows, valuesForRow) {
        const body = page.querySelector("[data-admin-results]");
        const empty = page.querySelector("[data-admin-empty]");
        if (!body) return;
        body.replaceChildren();
        for (const item of rows) {
            const row = document.createElement("tr");
            for (const value of valuesForRow(item)) appendCell(row, value);
            body.append(row);
        }
        empty?.classList.toggle("d-none", rows.length !== 0);
    }

    function updateCount(page, total, current = total) {
        const count = page.querySelector("[data-admin-count]");
        if (count) count.textContent = `نمایش ${numberFormat.format(current)} از ${numberFormat.format(total)} مورد`;
    }

    function updatePagination(page, currentPage, hasNext) {
        const container = page.querySelector("[data-admin-pagination]");
        if (!container) return;
        container.hidden = false;
        const label = container.querySelector("[data-admin-page-label]");
        if (label) label.textContent = `صفحهٔ ${numberFormat.format(currentPage)}`;
        const previous = container.querySelector('[data-admin-page-step="-1"]');
        const next = container.querySelector('[data-admin-page-step="1"]');
        if (previous) previous.disabled = currentPage <= 1;
        if (next) next.disabled = !hasNext;
    }

    function catalogValues(section, item) {
        switch (section) {
            case "products": return [item.code, item.name, item.brandName, item.categoryName, numberFormat.format(item.price), numberFormat.format(item.stockQuantity)];
            case "categories": return [item.id, item.name, item.englishName, item.parentId, item.slug];
            case "brands": return [item.id, item.name, item.englishName, item.countryCode];
            default: return [item.key, item.name, item.source, item.isFilterable ? "بله" : "خیر", item.isHighlight ? "بله" : "خیر"];
        }
    }

    async function loadCatalog(page, pageNumber = 1) {
        const form = page.querySelector("[data-admin-filter]");
        const api = new URL(page.dataset.adminApi, window.location.origin);
        api.searchParams.set("page", String(pageNumber));
        api.searchParams.set("pageSize", "50");
        const query = form?.elements.namedItem("q")?.value?.trim();
        if (query) api.searchParams.set("q", query);
        const id = ++requestNumber;
        showStatus(page, "در حال خواندن داده از دیتابیس…");
        try {
            const result = await readJson(api);
            if (id !== requestNumber || !page.isConnected) return;
            updateTable(page, result.items || [], item => catalogValues(page.dataset.adminSection, item));
            updateCount(page, result.total ?? 0, result.items?.length ?? 0);
            updatePagination(page, result.page ?? pageNumber, (result.page ?? pageNumber) * (result.pageSize ?? 50) < (result.total ?? 0));
            showStatus(page, "داده‌ها از دیتابیس دریافت شد.");
        } catch (error) {
            if (id !== requestNumber || !page.isConnected) return;
            updateTable(page, [], () => []);
            showStatus(page, error.message || "دریافت داده ناموفق بود.", true);
        }
    }

    async function loadWarehouses(page) {
        const result = await readJson(page.dataset.adminApi);
        const items = Array.isArray(result) ? result : [];
        updateTable(page, items, item => [
            item.code,
            item.name,
            item.companyName,
            item.isSalesWarehouse ? "بله" : "خیر",
            item.isActive ? "فعال" : "غیرفعال"
        ]);
        updateCount(page, items.length);
        showStatus(page, "داده‌ها از دیتابیس دریافت شد.");
    }

    async function loadWarehouseStock(page, pageNumber = Number(page.dataset.adminPageNumber || "1")) {
        const form = page.querySelector("[data-admin-filter]");
        const warehouseSelect = page.querySelector("[data-admin-warehouse-options]");
        const api = new URL(page.dataset.adminApi, window.location.origin);
        api.searchParams.set("page", String(pageNumber));
        api.searchParams.set("pageSize", "50");
        if (form) {
            const data = new FormData(form);
            for (const key of ["warehouseId", "search", "lowStockOnly"]) {
                const value = data.get(key);
                if (value) api.searchParams.set(key, String(value));
            }
        }
        const id = ++requestNumber;
        showStatus(page, "در حال خواندن موجودی از دیتابیس…");
        try {
            const [items, warehouses] = await Promise.all([
                readJson(api),
                warehouseSelect ? readJson(page.dataset.adminWarehousesApi) : Promise.resolve([])
            ]);
            if (id !== requestNumber || !page.isConnected) return;
            if (warehouseSelect) {
                const selected = warehouseSelect.value;
                const options = [new Option("همهٔ انبارها", "")];
                for (const warehouse of warehouses || []) options.push(new Option(warehouse.name, String(warehouse.id)));
                warehouseSelect.replaceChildren(...options);
                warehouseSelect.value = selected;
            }
            updateTable(page, items || [], item => [
                `${item.warehouseName} (${item.warehouseCode})`,
                item.storeProductKey,
                item.variantName ? `${item.productName} ـ ${item.variantName}` : item.productName,
                item.salesPlanName,
                numberFormat.format(item.quantity),
                numberFormat.format(item.minimumQuantity),
                item.isLowStock ? "کمتر از حداقل" : "مناسب"
            ]);
            page.dataset.adminPageNumber = String(pageNumber);
            updateCount(page, items?.length ?? 0);
            updatePagination(page, pageNumber, (items?.length ?? 0) === 50);
            showStatus(page, "موجودی از دیتابیس دریافت شد.");
        } catch (error) {
            if (id !== requestNumber || !page.isConnected) return;
            updateTable(page, [], () => []);
            showStatus(page, error.message || "دریافت موجودی ناموفق بود.", true);
        }
    }

    function initializePage() {
        const page = content.querySelector("[data-admin-page]");
        if (!page) return;
        if (page.dataset.adminPage === "catalog") void loadCatalog(page);
        else if (page.dataset.adminPage === "warehouses") void loadWarehouses(page).catch(error => showStatus(page, error.message, true));
        else if (page.dataset.adminPage === "stock") void loadWarehouseStock(page);
    }

    async function navigate(href, pushState = true) {
        const target = new URL(href, window.location.href);
        if (target.origin !== window.location.origin) return;
        const id = ++requestNumber;
        content.setAttribute("aria-busy", "true");
        try {
            const response = await fetch(target, {
                method: "GET",
                cache: "no-store",
                headers: { Accept: "text/html", "X-Requested-With": "XMLHttpRequest" }
            });
            const html = await response.text();
            if (id !== requestNumber) return;
            if (!response.ok) throw new Error(`بارگذاری صفحه با خطای ${response.status} روبه‌رو شد.`);
            const nextDocument = new DOMParser().parseFromString(html, "text/html");
            const nextContent = nextDocument.getElementById("content");
            if (!nextContent) throw new Error("ساختار صفحهٔ دریافت‌شده معتبر نیست.");
            content.innerHTML = nextContent.innerHTML;
            document.title = nextDocument.title;
            if (pushState) window.history.pushState({ admin: true }, "", target.href);
            updateActiveNavigation(target);
            content.removeAttribute("aria-busy");
            content.focus({ preventScroll: true });
            const heading = content.querySelector("h1");
            if (heading) {
                heading.tabIndex = -1;
                heading.focus({ preventScroll: true });
            }
            initializePage();
        } catch (error) {
            if (id !== requestNumber) return;
            content.removeAttribute("aria-busy");
            content.innerHTML = `<div class="alert alert-danger" role="alert">${escapeHtml(error.message || "بارگذاری صفحه ناموفق بود.")}</div>`;
        }
    }

    function updateActiveNavigation(target) {
        if (!sidebar) return;
        sidebar.querySelectorAll(".menu-item.active").forEach(item => item.classList.remove("active", "expand"));
        sidebar.querySelectorAll(".menu-link[aria-expanded='true']").forEach(link => link.setAttribute("aria-expanded", "false"));
        const targetUrl = new URL(target.href);
        for (const link of sidebar.querySelectorAll("a[data-admin-nav]")) {
            const linkUrl = new URL(link.href);
            if (linkUrl.pathname.toLowerCase() === targetUrl.pathname.toLowerCase() && linkUrl.search === targetUrl.search) {
                const item = link.closest(".menu-item");
                item?.classList.add("active");
                const group = item?.parentElement?.closest(".menu-item.has-sub");
                if (group) {
                    group.classList.add("active", "expand");
                    group.querySelector(":scope > .menu-link")?.setAttribute("aria-expanded", "true");
                }
                break;
            }
        }
    }

    function escapeHtml(value) {
        const node = document.createElement("span");
        node.textContent = value;
        return node.innerHTML;
    }

    document.addEventListener("click", event => {
        const refresh = event.target.closest("[data-admin-refresh]");
        if (refresh) {
            const page = refresh.closest("[data-admin-page]");
            if (page?.dataset.adminPage === "catalog") void loadCatalog(page, Number(page.dataset.adminPageNumber || "1"));
            else if (page?.dataset.adminPage === "warehouses") void loadWarehouses(page).catch(error => showStatus(page, error.message, true));
            else if (page?.dataset.adminPage === "stock") void loadWarehouseStock(page);
            return;
        }

        const step = event.target.closest("[data-admin-page-step]");
        if (step && !step.disabled) {
            const page = step.closest("[data-admin-page]");
            const delta = Number(step.dataset.adminPageStep);
            const pageNumber = Number(page?.dataset.adminPageNumber || "1") + delta;
            if (page?.dataset.adminPage === "catalog") void loadCatalog(page, pageNumber);
            else if (page?.dataset.adminPage === "stock") void loadWarehouseStock(page, pageNumber);
            return;
        }

        const link = event.target.closest("a[data-admin-nav]");
        if (!link || event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
        event.preventDefault();
        void navigate(link.href);
    });

    document.addEventListener("submit", event => {
        const form = event.target.closest("[data-admin-filter]");
        if (!form) return;
        event.preventDefault();
        const page = form.closest("[data-admin-page]");
        if (page?.dataset.adminPage === "catalog") void loadCatalog(page, 1);
        else if (page?.dataset.adminPage === "stock") void loadWarehouseStock(page, 1);
    });

    window.addEventListener("popstate", () => void navigate(window.location.href, false));
    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", initializePage, { once: true });
    else initializePage();
})();
