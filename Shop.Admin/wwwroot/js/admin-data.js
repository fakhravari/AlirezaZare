(() => {
    "use strict";

    const ui = window.AdminUI;
    if (!ui) return;
    const requests = new WeakMap();
    const pageSize = 50;

    function beginRequest(page) {
        requests.get(page)?.abort();
        const controller = new AbortController();
        requests.set(page, controller);
        page.setAttribute("aria-busy", "true");
        return controller;
    }

    function finishRequest(page, controller) {
        if (requests.get(page) === controller) {
            requests.delete(page);
            page.removeAttribute("aria-busy");
        }
    }

    function status(page, message, isError = false) {
        const target = page.querySelector("[data-admin-status]");
        if (!target) return;
        target.textContent = message;
        target.classList.toggle("is-error", isError);
        target.classList.toggle("d-none", !message);
    }

    function updateCount(page, total, visible, exact = true) {
        const count = page.querySelector("[data-admin-count]");
        if (count) count.textContent = exact
            ? `نمایش ${ui.formatNumber(visible)} از ${ui.formatNumber(total)} مورد`
            : `${ui.formatNumber(visible)} ردیف در این صفحه`;
    }

    function updatePagination(page, current, hasNext) {
        const container = page.querySelector("[data-admin-pagination]");
        if (!container) return;
        container.hidden = false;
        const label = container.querySelector("[data-admin-page-label]");
        const previous = container.querySelector('[data-admin-page-step="-1"]');
        const next = container.querySelector('[data-admin-page-step="1"]');
        if (label) label.textContent = `صفحهٔ ${ui.formatNumber(current)}`;
        if (previous) previous.disabled = current <= 1;
        if (next) next.disabled = !hasNext;
        page.dataset.adminPageNumber = String(current);
    }

    function renderRows(page, rows, columns) {
        const body = page.querySelector("[data-admin-results]");
        const empty = page.querySelector("[data-admin-empty]");
        if (!body) return;
        page.__adminRows = rows;
        body.replaceChildren();
        rows.forEach((item, index) => {
            const row = document.createElement("tr");
            for (const value of columns(item)) {
                const cell = document.createElement("td");
                cell.textContent = value == null || value === "" ? "—" : String(value);
                row.append(cell);
            }
            const actions = document.createElement("td");
            actions.className = "admin-row-actions";
            const details = document.createElement("button");
            details.type = "button";
            details.className = "btn btn-xs btn-default";
            details.dataset.adminRecord = String(index);
            details.innerHTML = '<i class="fa fa-eye" aria-hidden="true"></i><span class="visually-hidden">نمایش جزئیات</span>';
            details.setAttribute("aria-label", "نمایش جزئیات رکورد");
            actions.append(details);
            if (page.dataset.adminPage === "product-drafts") {
                const publish = document.createElement("button");
                publish.type = "button"; publish.className = "btn btn-xs btn-primary ms-1";
                publish.dataset.productPublish = String(index); publish.textContent = "انتشار";
                publish.disabled = !item.hasImage; actions.append(publish);
            }
            if (page.dataset.adminPage === "customers") {
                const edit = document.createElement("button");
                edit.type = "button"; edit.className = "btn btn-xs btn-default ms-1";
                edit.dataset.customerEdit = String(index); edit.textContent = "ویرایش";
                actions.append(edit);
            }
            if (page.dataset.adminPage === "orders" && !item.isSample) {
                if (["warehouse-confirmed", "in-transit", "shipped-to-post", "delivered"].includes(item.status)) {
                    const tracking = document.createElement("button"); tracking.type = "button"; tracking.className = "btn btn-xs btn-default ms-1";
                    tracking.dataset.orderTracking = String(index); tracking.textContent = "رهگیری"; actions.append(tracking);
                }
                const next = item.status === "warehouse-confirmed" ? [["in-transit", "ثبت ارسال"], ["shipped-to-post", "تحویل به پست"]]
                    : ["in-transit", "shipped-to-post"].includes(item.status) ? [["delivered", "ثبت تحویل"]] : [];
                for (const [value, label] of next) {
                    const button = document.createElement("button");
                    button.type = "button"; button.className = "btn btn-xs btn-default ms-1";
                    button.dataset.orderStatus = value; button.dataset.orderIndex = String(index); button.textContent = label;
                    actions.append(button);
                }
            }
            if (page.dataset.adminSection === "categories" && page.dataset.canEditCategory === "true") {
                const edit = document.createElement("button");
                edit.type = "button"; edit.className = "btn btn-xs btn-default ms-1";
                edit.dataset.categoryContent = String(index); edit.textContent = "محتوای دسته";
                actions.append(edit);
            }
            if (page.dataset.adminSection === "products" && page.dataset.canUploadImage === "true") {
                const edit = document.createElement("button");
                edit.type = "button"; edit.className = "btn btn-xs btn-default ms-1";
                edit.dataset.productEdit = String(index); edit.textContent = "ویرایش کالا";
                actions.append(edit);
                const upload = document.createElement("button");
                upload.type = "button"; upload.className = "btn btn-xs btn-default ms-1";
                upload.dataset.productImage = String(index); upload.textContent = "تصویر محصول";
                actions.append(upload);
                const file = document.createElement("button");
                file.type = "button"; file.className = "btn btn-xs btn-default ms-1";
                file.dataset.productFile = String(index); file.textContent = "فایل محصول";
                actions.append(file);
            }
            if (page.dataset.adminSection === "brands" && page.dataset.canUploadImage === "true") {
                const edit = document.createElement("button");
                edit.type = "button"; edit.className = "btn btn-xs btn-default ms-1";
                edit.dataset.brandEdit = String(index); edit.textContent = "ویرایش برند";
                actions.append(edit);
                const upload = document.createElement("button");
                upload.type = "button"; upload.className = "btn btn-xs btn-default ms-1";
                upload.dataset.brandImage = String(index); upload.textContent = "نشان برند";
                actions.append(upload);
            }
            if (page.dataset.adminPage === "stock" && page.dataset.canMinimum === "true" && page.__adminRows[index]?.isRegularSalesPlan) {
                const minimum = document.createElement("button");
                minimum.type = "button"; minimum.className = "btn btn-xs btn-default ms-1";
                minimum.dataset.adminMinimum = String(index); minimum.textContent = "تنظیم حداقل";
                actions.append(minimum);
            }
            row.append(actions);
            body.append(row);
        });
        empty?.classList.toggle("d-none", rows.length > 0);
    }

    function queryValue(form, name) {
        const value = new FormData(form).get(name);
        return typeof value === "string" ? value.trim() : value;
    }

    async function loadCatalog(page, pageNumber = 1, announce = false) {
        const form = page.querySelector("[data-admin-filter]");
        const api = new URL(page.dataset.adminApi, window.location.origin);
        api.searchParams.set("page", String(pageNumber));
        api.searchParams.set("pageSize", String(pageSize));
        const query = queryValue(form, "q");
        if (query) api.searchParams.set("q", query);
        const controller = beginRequest(page);
        status(page, "در حال خواندن داده از دیتابیس…");
        try {
            const result = await ui.getJson(api, { signal: controller.signal });
            if (!page.isConnected || requests.get(page) !== controller) return;
            const section = page.dataset.adminSection;
            const rows = result.items || [];
            const columns = item => section === "products"
                ? [item.code, item.name, item.brandName, item.categoryName, ui.formatNumber(item.price), ui.formatNumber(item.stockQuantity)]
                : section === "categories"
                    ? [item.id, item.name, item.englishName, item.parentId, item.slug]
                    : section === "brands"
                        ? [item.id, item.name, item.englishName, item.countryCode]
                        : [item.key, item.name, item.source, item.isFilterable ? "بله" : "خیر", item.isHighlight ? "بله" : "خیر"];
            renderRows(page, rows, columns);
            updateCount(page, result.total ?? 0, rows.length);
            updatePagination(page, result.page ?? pageNumber, (result.page ?? pageNumber) * (result.pageSize ?? pageSize) < (result.total ?? 0));
            status(page, "داده‌ها از دیتابیس دریافت شد.");
            if (announce) ui.notify("فهرست به‌روز شد.", "success");
        } catch (error) {
            if (error.name === "AbortError") return;
            if (!page.isConnected || requests.get(page) !== controller) return;
            renderRows(page, [], () => []);
            status(page, error.message || "دریافت داده ناموفق بود.", true);
            ui.notify(error.message || "دریافت داده ناموفق بود.", "danger");
        } finally { finishRequest(page, controller); }
    }

    async function loadProductDrafts(page) {
        const controller = beginRequest(page);
        try {
            const rows = await ui.getJson("/api/admin/store/catalog/products/drafts", { signal: controller.signal });
            if (!page.isConnected || requests.get(page) !== controller) return;
            renderRows(page, rows, item => [item.code, item.name, item.brandName, item.categoryName, ui.formatNumber(item.priceAmount), item.hasImage ? "ثبت شده" : "ثبت نشده"]);
            status(page, "");
        } catch (error) {
            if (error.name !== "AbortError" && page.isConnected && requests.get(page) === controller) status(page, error.message || "دریافت پیش‌نویس‌ها ناموفق بود.", true);
        } finally { finishRequest(page, controller); }
    }

    function refreshProductDrafts() { const page = document.querySelector('[data-admin-page="product-drafts"]'); if (page) void loadProductDrafts(page); }

    async function loadPlans(signal) {
        const data = await ui.getJson("/api/admin/warehouse/stock/plans", { signal });
        return Array.isArray(data) ? data : [];
    }

    async function loadWarehouses(page, announce = false) {
        const form = page.querySelector("[data-admin-filter]");
        const controller = beginRequest(page);
        status(page, "در حال دریافت فهرست انبارها…");
        try {
            const result = await ui.getJson(page.dataset.adminApi, { signal: controller.signal });
            if (!page.isConnected || requests.get(page) !== controller) return;
            const all = Array.isArray(result) ? result : [];
            const query = queryValue(form, "q").toLocaleLowerCase("fa-IR");
            const rows = query ? all.filter(item => [item.code, item.name, item.companyName].some(value => String(value || "").toLocaleLowerCase("fa-IR").includes(query))) : all;
            const warehouseSelect = page.querySelector("[data-admin-warehouse-options]");
            if (warehouseSelect) {
                const selected = warehouseSelect.value;
                const options = [new Option("همهٔ انبارها", "")];
                all.forEach(item => options.push(new Option(item.name, String(item.id))));
                warehouseSelect.replaceChildren(...options);
                if (all.some(item => String(item.id) === selected)) warehouseSelect.value = selected;
            }
            renderRows(page, rows, item => [item.code, item.name, item.companyName, item.isSalesWarehouse ? "بله" : "خیر", item.isActive ? "فعال" : "غیرفعال"]);
            updateCount(page, all.length, rows.length);
            status(page, "فهرست انبارها از دیتابیس دریافت شد.");
            if (announce) ui.notify("فهرست انبارها به‌روز شد.", "success");
        } catch (error) {
            if (error.name === "AbortError") return;
            if (!page.isConnected || requests.get(page) !== controller) return;
            renderRows(page, [], () => []);
            status(page, error.message || "دریافت انبارها ناموفق بود.", true);
            ui.notify(error.message || "دریافت انبارها ناموفق بود.", "danger");
        } finally { finishRequest(page, controller); }
    }

    async function loadStock(page, pageNumber = Number(page.dataset.adminPageNumber || "1"), announce = false) {
        const form = page.querySelector("[data-admin-filter]");
        const api = new URL(page.dataset.adminApi, window.location.origin);
        api.searchParams.set("page", String(pageNumber));
        api.searchParams.set("pageSize", String(pageSize));
        for (const key of ["warehouseId", "salesPlanKey", "search", "lowStockOnly"]) {
            const value = queryValue(form, key);
            if (value) api.searchParams.set(key, String(value));
        }
        const controller = beginRequest(page);
        status(page, "در حال خواندن موجودی از دیتابیس…");
        try {
            const tasks = [ui.getJson(api, { signal: controller.signal })];
            const warehouseSelect = page.querySelector("[data-admin-warehouse-options]");
            const planSelect = page.querySelector("[data-admin-plan-options]");
            if (warehouseSelect && !warehouseSelect.dataset.loaded) tasks.push(ui.getJson(page.dataset.adminWarehousesApi, { signal: controller.signal }));
            else tasks.push(Promise.resolve(null));
            if (planSelect && !planSelect.dataset.loaded) tasks.push(loadPlans(controller.signal));
            else tasks.push(Promise.resolve(null));
            const [items, warehouses, plans] = await Promise.all(tasks);
            if (!page.isConnected || requests.get(page) !== controller) return;
            if (warehouseSelect && warehouses) {
                const selected = warehouseSelect.value;
                const options = [new Option("همهٔ انبارها", "")];
                (warehouses || []).forEach(item => options.push(new Option(item.name, String(item.id))));
                warehouseSelect.replaceChildren(...options);
                warehouseSelect.value = selected;
                warehouseSelect.dataset.loaded = "true";
            }
            if (planSelect && plans) {
                const selected = planSelect.value;
                const options = [new Option("همهٔ طرح‌ها", "")];
                plans.forEach(plan => options.push(new Option(plan.name, plan.id)));
                planSelect.replaceChildren(...options);
                if (plans.some(plan => String(plan.id) === selected)) planSelect.value = selected;
                planSelect.dataset.loaded = "true";
            }
            const rows = Array.isArray(items) ? items : [];
            renderRows(page, rows, item => [
                `${item.warehouseName} (${item.warehouseCode})`, item.storeProductKey,
                item.variantName ? `${item.productName} ـ ${item.variantName}` : item.productName,
                item.salesPlanName, item.unitName, ui.formatNumber(item.quantity), ui.formatNumber(item.minimumQuantity),
                item.isLowStock ? "کمتر از حداقل" : "مناسب"
            ]);
            updateCount(page, 0, rows.length, false);
            updatePagination(page, pageNumber, rows.length === pageSize);
            status(page, "موجودی از دیتابیس دریافت شد.");
            if (announce) ui.notify("موجودی به‌روز شد.", "success");
        } catch (error) {
            if (error.name === "AbortError") return;
            if (!page.isConnected || requests.get(page) !== controller) return;
            renderRows(page, [], () => []);
            status(page, error.message || "دریافت موجودی ناموفق بود.", true);
            ui.notify(error.message || "دریافت موجودی ناموفق بود.", "danger");
        } finally { finishRequest(page, controller); }
    }

    async function loadOrders(page, pageNumber = 1, announce = false) {
        const api = new URL(page.dataset.adminApi, window.location.origin);
        api.searchParams.set("page", String(pageNumber));
        api.searchParams.set("pageSize", String(pageSize));
        const query = queryValue(page.querySelector("[data-admin-filter]"), "q");
        if (query) api.searchParams.set("q", query);
        const controller = beginRequest(page);
        status(page, "در حال خواندن سفارش‌ها…");
        try {
            const result = await ui.getJson(api, { signal: controller.signal });
            if (!page.isConnected || requests.get(page) !== controller) return;
            const rows = result.items || [];
            const labels = { "pending-payment": "در انتظار پرداخت", "simulated-payment-confirmed": "پرداخت آزمایشی تأیید شده", "payment-confirmed": "پرداخت تأیید شده", "warehouse-confirmed": "تأیید انبار", "in-transit": "در حال ارسال", "shipped-to-post": "تحویل به پست", delivered: "تحویل‌شده", cancelled: "لغوشده" };
            renderRows(page, rows, item => [item.isSample ? `${item.orderNumber} (نمونه)` : item.orderNumber, item.customerName, labels[item.status] || item.status,
                item.paymentMethod, ui.formatNumber(item.itemCount), ui.formatNumber(item.totalAmount),
                new Date(/[zZ]|[+-]\d\d:\d\d$/.test(item.orderedAtUtc) ? item.orderedAtUtc : `${item.orderedAtUtc}Z`).toLocaleString("fa-IR")]);
            updateCount(page, 0, rows.length, false);
            updatePagination(page, result.page, result.hasNext);
            status(page, "سفارش‌ها از دیتابیس دریافت شدند.");
            if (announce) ui.notify("فهرست سفارش‌ها به‌روز شد.", "success");
        } catch (error) {
            if (error.name === "AbortError" || !page.isConnected || requests.get(page) !== controller) return;
            renderRows(page, [], () => []);
            status(page, error.message || "دریافت سفارش‌ها ناموفق بود.", true);
            ui.notify(error.message || "دریافت سفارش‌ها ناموفق بود.", "danger");
        } finally { finishRequest(page, controller); }
    }

    async function loadCustomers(page, pageNumber = 1, announce = false) {
        const api = new URL(page.dataset.adminApi, window.location.origin);
        api.searchParams.set("page", String(pageNumber));
        api.searchParams.set("pageSize", String(pageSize));
        const query = queryValue(page.querySelector("[data-admin-filter]"), "q");
        if (query) api.searchParams.set("q", query);
        const controller = beginRequest(page);
        status(page, "در حال دریافت مشتریان…");
        try {
            const result = await ui.getJson(api, { signal: controller.signal });
            if (!page.isConnected || requests.get(page) !== controller) return;
            const rows = result.items || [];
            renderRows(page, rows, item => [`${item.firstName} ${item.lastName}`.trim(), item.email, item.phone, ui.formatNumber(item.orderCount)]);
            updateCount(page, 0, rows.length, false);
            updatePagination(page, result.page, result.hasNext);
            status(page, "مشتریان از دیتابیس دریافت شدند.");
            if (announce) ui.notify("فهرست مشتریان به‌روز شد.", "success");
        } catch (error) {
            if (error.name === "AbortError" || !page.isConnected || requests.get(page) !== controller) return;
            renderRows(page, [], () => []);
            status(page, error.message || "دریافت مشتریان ناموفق بود.", true);
        } finally { finishRequest(page, controller); }
    }

    async function loadFaq(page) {
        const controller = beginRequest(page);
        status(page, "در حال دریافت پرسش‌ها…");
        try {
            const rows = await ui.getJson(page.dataset.adminApi, { signal: controller.signal });
            if (!page.isConnected || requests.get(page) !== controller) return;
            page.__faqRows = rows;
            const body = page.querySelector("[data-faq-rows]"); body.replaceChildren();
            rows.forEach((item, index) => {
                const row = document.createElement("tr");
                for (const value of [item.question, ui.formatNumber(item.sortOrder), item.isPublished ? "منتشرشده" : "منتشرنشده"]) {
                    const cell = document.createElement("td"); cell.textContent = value; row.append(cell);
                }
                const cell = document.createElement("td"), edit = document.createElement("button");
                edit.type = "button"; edit.className = "btn btn-sm btn-default"; edit.dataset.faqEdit = String(index); edit.textContent = "ویرایش";
                cell.append(edit); row.append(cell); body.append(row);
            });
            status(page, rows.length ? "" : "پرسشی ثبت نشده است.");
        } catch (error) {
            if (error.name !== "AbortError" && page.isConnected && requests.get(page) === controller) status(page, error.message || "دریافت پرسش‌ها ناموفق بود.", true);
        } finally { finishRequest(page, controller); }
    }

    function refresh(page, announce = true) {
        if (page.dataset.adminPage === "catalog") return loadCatalog(page, Number(page.dataset.adminPageNumber || "1"), announce);
        if (page.dataset.adminPage === "warehouses") return loadWarehouses(page, announce);
        if (page.dataset.adminPage === "stock") return loadStock(page, Number(page.dataset.adminPageNumber || "1"), announce);
        if (page.dataset.adminPage === "customers") return loadCustomers(page, Number(page.dataset.adminPageNumber || "1"), announce);
        if (page.dataset.adminPage === "orders") return loadOrders(page, Number(page.dataset.adminPageNumber || "1"), announce);
    }

    function initialize(root = document) {
        const pages = root.querySelectorAll("[data-admin-page]");
        pages.forEach(page => {
            if (page.dataset.adminInitialized === "true") return;
            page.dataset.adminInitialized = "true";
            if (page.dataset.adminPage === "catalog") void loadCatalog(page, 1, false);
            else if (page.dataset.adminPage === "warehouses") void loadWarehouses(page, false);
            else if (page.dataset.adminPage === "stock") void loadStock(page, 1, false);
            else if (page.dataset.adminPage === "orders") void loadOrders(page, 1, false);
            else if (page.dataset.adminPage === "customers") void loadCustomers(page, 1, false);
            else if (page.dataset.adminPage === "faq") void loadFaq(page);
            else if (page.dataset.adminPage === "product-drafts") void loadProductDrafts(page);
        });
    }

    document.addEventListener("change", event => {
        if (!event.target.matches('[data-media-upload-form] [name="kind"]')) return;
        const form = event.target.closest("[data-media-upload-form]");
        const kind = event.target.value;
        for (const [name, field] of [["country", "countryCode"], ["article", "articleKey"]]) {
            form.querySelector(`[data-media-${name}-container]`).hidden = kind !== name;
            form.elements[field].disabled = kind !== name;
            form.elements[field].required = kind === name;
            if (kind !== name) form.elements[field].value = "";
        }
        if (kind !== "country" && kind !== "article") return;
        const select = form.elements[kind === "country" ? "countryCode" : "articleKey"];
        if (select.options.length > 1) return;
        select.disabled = true;
        void ui.getJson(kind === "country" ? "/api/admin/store/catalog/brands/countries" : "/api/admin/store/media/articles")
            .then(items => {
                select.replaceChildren(new Option(kind === "country" ? "کشور را انتخاب کنید" : "مقاله را انتخاب کنید", ""), ...items.map(item => new Option(kind === "country" ? item.nameFa : item.title, kind === "country" ? item.countryCode : item.articleKey)));
            })
            .catch(error => { ui.notify(error.message || "دریافت فهرست ناموفق بود.", "danger"); })
            .finally(() => { select.disabled = form.elements.kind.value !== kind; });
    });

    document.addEventListener("click", event => {
        const trackingButton = event.target.closest("[data-order-tracking]");
        if (trackingButton) {
            const page = trackingButton.closest('[data-admin-page="orders"]');
            const item = page?.__adminRows?.[Number(trackingButton.dataset.orderTracking)];
            const form = document.querySelector("[data-order-tracking-form]");
            if (!item || !form || trackingButton.disabled) return;
            trackingButton.disabled = true;
            void ui.getJson(`/api/admin/store/orders/${encodeURIComponent(item.id)}`).then(order => {
                if (!page.isConnected || !form.isConnected) return;
                form.reset(); form.elements.id.value = order.id;
                form.elements.carrier.value = order.shippingCarrier || ""; form.elements.trackingCode.value = order.shippingTrackingCode || "";
                form.__expected = { expectedCarrier: order.shippingCarrier, expectedTrackingCode: order.shippingTrackingCode };
                form.querySelector("[data-order-tracking-number]").textContent = `سفارش ${order.orderNumber}`;
                window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("order-tracking-modal")).show();
            }).catch(error => ui.notify(error.message || "دریافت اطلاعات سفارش ناموفق بود.", "danger"))
                .finally(() => { trackingButton.disabled = false; });
            return;
        }
        const productCreate = event.target.closest("[data-product-create]");
        if (productCreate) {
            const form = document.querySelector("[data-product-create-form]");
            if (!form || productCreate.disabled) return;
            productCreate.disabled = true;
            void ui.getJson("/api/admin/store/catalog/products/create-options").then(data => {
                form.reset();
                for (const [field, items] of [["brandId", data.brands], ["categoryId", data.categories], ["unitId", data.units]])
                    form.elements[field].replaceChildren(new Option("انتخاب کنید", ""), ...items.map(item => new Option(item.name, String(item.id))));
                window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("product-create-modal")).show();
            }).catch(error => ui.notify(error.message || "دریافت اطلاعات ناموفق بود.", "danger"))
                .finally(() => { productCreate.disabled = false; });
            return;
        }
        const publish = event.target.closest("[data-product-publish]");
        if (publish) {
            const page = publish.closest('[data-admin-page="product-drafts"]');
            const item = page?.__adminRows?.[Number(publish.dataset.productPublish)];
            if (!item || publish.disabled || !window.confirm(`کالای ${item.name} منتشر شود؟`)) return;
            const token = document.querySelector('[data-product-create-form] [name="__RequestVerificationToken"]')?.value || "";
            publish.disabled = true;
            void ui.getJson(`/api/admin/store/catalog/products/manage/${encodeURIComponent(item.id)}/publish`, { method: "POST", headers: { RequestVerificationToken: token } })
                .then(result => { ui.notify(result.message, "success"); refreshProductDrafts(); const catalog = document.querySelector('[data-admin-page="catalog"][data-admin-section="products"]'); if (catalog) void loadCatalog(catalog, 1); })
                .catch(error => { ui.notify(error.message || "انتشار ناموفق بود.", "danger"); publish.disabled = false; });
            return;
        }
        const customerEdit = event.target.closest("[data-customer-edit]");
        if (customerEdit) {
            const page = customerEdit.closest('[data-admin-page="customers"]');
            const item = page?.__adminRows?.[Number(customerEdit.dataset.customerEdit)];
            const form = document.querySelector("[data-customer-editor]");
            if (!item || !form) return;
            form.reset(); form.elements.id.value = item.id;
            form.elements.firstName.value = item.firstName; form.elements.lastName.value = item.lastName;
            form.elements.phone.value = item.phone || "";
            form.__expected = { expectedFirstName: item.firstName, expectedLastName: item.lastName, expectedPhone: item.phone };
            window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("customer-editor-modal")).show();
            return;
        }
        const faqAction = event.target.closest("[data-faq-create], [data-faq-edit]");
        if (faqAction) {
            const page = faqAction.closest('[data-admin-page="faq"]'), form = document.querySelector("[data-faq-editor]");
            const item = faqAction.hasAttribute("data-faq-edit") ? page?.__faqRows?.[Number(faqAction.dataset.faqEdit)] : null;
            if (!page || !form || (faqAction.hasAttribute("data-faq-edit") && !item)) return;
            form.reset(); form.elements.id.value = item?.id || ""; form.elements.question.value = item?.question || "";
            form.elements.answer.value = item?.answer || ""; form.elements.linkText.value = item?.linkText || "";
            form.elements.linkUrl.value = item?.linkUrl || ""; form.elements.sortOrder.value = item?.sortOrder ?? 0;
            form.elements.isPublished.checked = item?.isPublished ?? false;
            window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("faq-editor-modal")).show();
            return;
        }
        const productEdit = event.target.closest("[data-product-edit]");
        if (productEdit) {
            const page = productEdit.closest('[data-admin-section="products"]');
            const item = page?.__adminRows?.[Number(productEdit.dataset.productEdit)];
            const form = document.querySelector("[data-product-editor]");
            if (!item || !form || productEdit.disabled) return;
            productEdit.disabled = true;
            void ui.getJson(`/api/admin/store/catalog/products/manage/${encodeURIComponent(item.id)}`).then(data => {
                if (!page.isConnected) return;
                const product = data.product;
                form.reset(); form.elements.id.value = product.id;
                form.querySelector("[data-product-editor-code]").textContent = `کد کالا: ${product.code}`;
                form.elements.name.value = product.name; form.elements.englishName.value = product.englishName || product.name;
                form.elements.brandId.replaceChildren(new Option("انتخاب برند", ""), ...data.brands.map(brand => new Option(brand.name, String(brand.id))));
                form.elements.categoryId.replaceChildren(new Option("انتخاب دسته", ""), ...data.categories.map(category => new Option(category.name, String(category.id))));
                form.elements.brandId.value = String(product.idBrand || ""); form.elements.categoryId.value = String(product.idCategory || "");
                form.elements.priceAmount.value = product.priceAmount ?? ""; form.elements.discountPercent.value = product.discountPercent ?? 0;
                form.elements.maxPurchaseQuantity.value = product.maxPurchaseQuantity ?? 99;
                window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("product-editor-modal")).show();
            }).catch(error => ui.notify(error.message || "دریافت اطلاعات کالا ناموفق بود.", "danger"))
                .finally(() => { productEdit.disabled = false; });
            return;
        }
        const brandEdit = event.target.closest("[data-brand-edit], [data-brand-create]");
        if (brandEdit) {
            const page = brandEdit.closest('[data-admin-section="brands"]');
            const form = document.querySelector("[data-brand-editor]");
            const item = brandEdit.hasAttribute("data-brand-edit") ? page?.__adminRows?.[Number(brandEdit.dataset.brandEdit)] : null;
            if (!page || !form || brandEdit.disabled) return;
            brandEdit.disabled = true;
            void ui.getJson("/api/admin/store/catalog/brands/countries").then(countries => {
                if (!page.isConnected) return;
                form.reset(); form.elements.originalKey.value = item?.id || "";
                form.elements.key.value = item?.id || ""; form.elements.key.readOnly = !!item;
                form.elements.nameFa.value = item?.name || ""; form.elements.nameEn.value = item?.englishName || "";
                form.elements.countryCode.replaceChildren(new Option("بدون کشور", ""), ...countries.map(country => new Option(country.nameFa, country.countryCode)));
                form.elements.countryCode.value = item?.countryCode || "";
                window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("brand-editor-modal")).show();
            }).catch(error => ui.notify(error.message || "دریافت کشورها ناموفق بود.", "danger"))
                .finally(() => { brandEdit.disabled = false; });
            return;
        }
        const shippingButton = event.target.closest("[data-order-status]");
        if (shippingButton) {
            const page = shippingButton.closest('[data-admin-page="orders"]');
            const item = page?.__adminRows?.[Number(shippingButton.dataset.orderIndex)];
            if (!item || shippingButton.disabled || !window.confirm(`وضعیت سفارش ${item.orderNumber} ثبت شود؟`)) return;
            shippingButton.disabled = true;
            const token = page.querySelector('[name="__RequestVerificationToken"]')?.value || "";
            void ui.getJson(`${page.dataset.adminApi}/${encodeURIComponent(item.id)}/shipping-status`, {
                method: "PUT", headers: { "Content-Type": "application/json", RequestVerificationToken: token },
                body: JSON.stringify({ expectedStatus: item.status, status: shippingButton.dataset.orderStatus })
            }).then(result => { ui.notify(result.message, "success"); if (page.isConnected) return loadOrders(page, Number(page.dataset.adminPageNumber || 1)); })
                .catch(error => ui.notify(error.message || "ثبت وضعیت ارسال ناموفق بود.", "danger"))
                .finally(() => { shippingButton.disabled = false; });
            return;
        }
        const brandButton = event.target.closest("[data-brand-image]");
        if (brandButton) {
            const page = brandButton.closest('[data-admin-section="brands"]');
            const item = page?.__adminRows?.[Number(brandButton.dataset.brandImage)];
            const form = document.querySelector("[data-brand-image-form]");
            if (!item || !form) return;
            form.reset(); form.elements.brandKey.value = item.id;
            form.querySelector("[data-brand-image-name]").textContent = item.name;
            window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("brand-image-modal")).show();
            return;
        }
        const fileButton = event.target.closest("[data-product-file]");
        if (fileButton) {
            const page = fileButton.closest('[data-admin-section="products"]');
            const item = page?.__adminRows?.[Number(fileButton.dataset.productFile)];
            const form = document.querySelector("[data-product-file-form]");
            if (!item || !form) return;
            form.reset(); form.elements.productId.value = item.id;
            form.querySelector("[data-product-file-name]").textContent = item.name;
            window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("product-file-modal")).show();
            return;
        }
        const imageButton = event.target.closest("[data-product-image]");
        if (imageButton) {
            const page = imageButton.closest('[data-admin-section="products"]');
            const item = page?.__adminRows?.[Number(imageButton.dataset.productImage)];
            const form = document.querySelector("[data-product-image-form]");
            if (!item || !form) return;
            form.reset(); form.elements.productId.value = item.id;
            form.querySelector("[data-product-image-name]").textContent = item.name;
            window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("product-image-modal")).show();
            return;
        }
        const categoryButton = event.target.closest("[data-category-content]");
        if (categoryButton) {
            const page = categoryButton.closest('[data-admin-section="categories"]');
            const item = page?.__adminRows?.[Number(categoryButton.dataset.categoryContent)];
            const form = document.querySelector("[data-category-content-form]");
            if (!item || !form) return;
            categoryButton.disabled = true;
            void ui.getJson(`/api/admin/store/category-content/${encodeURIComponent(item.id)}`)
                .then(data => {
                    form.elements.key.value = item.id; form.elements.intro.value = data.intro || "";
                    form.elements.highlights.value = JSON.parse(data.highlights || "[]").join("\n");
                    form.elements.specs.value = JSON.parse(data.specs || "[]").map(pair => pair.join(" | ")).join("\n");
                    form.querySelector("[data-category-content-name]").textContent = item.name;
                    window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("category-content-modal")).show();
                }).catch(error => ui.notify(error.message || "دریافت محتوای دسته ناموفق بود.", "danger"))
                .finally(() => { categoryButton.disabled = false; });
            return;
        }
        const refreshButton = event.target.closest("[data-admin-refresh]");
        if (refreshButton) {
            const page = refreshButton.closest("[data-admin-page]");
            if (page) void refresh(page, true);
            return;
        }
        const detailButton = event.target.closest("[data-admin-record]");
        if (detailButton) {
            const page = detailButton.closest("[data-admin-page]");
            const item = page?.__adminRows?.[Number(detailButton.dataset.adminRecord)];
            if (item && page.dataset.adminPage === "orders") {
                detailButton.disabled = true;
                void ui.getJson(`/api/admin/store/orders/${encodeURIComponent(item.id)}`).then(order => {
                    const address = JSON.parse(order.addressSnapshotJson || "{}");
                    const items = JSON.parse(order.itemsJson || "[]");
                    const history = JSON.parse(order.shippingHistoryJson || "[]");
                    ui.showRecord("جزئیات سفارش", {
                        "شماره سفارش": order.orderNumber, "دادهٔ نمونه": order.isSample ? "بله" : "خیر", "مشتری": order.customerName,
                        "وضعیت": order.status === "simulated-payment-confirmed" ? "پرداخت آزمایشی تأیید شده" : order.status === "pending-payment" ? "در انتظار پرداخت" : order.status,
                        "مبلغ تومان": order.totalAmount,
                        "شناسه حواله": order.idWarehouseDocument || "ثبت نشده",
                        "شرکت حمل": order.shippingCarrier || "ثبت نشده", "کد رهگیری": order.shippingTrackingCode || "ثبت نشده",
                        "نشانی ثبت‌شده": Object.values(address).filter(value => typeof value === "string" && value).join("، "),
                        "اقلام": items.map(line => `${line.name}، ${ui.formatNumber(line.quantity)} عدد، مبلغ واحد ${ui.formatNumber(line.unitAmount)} تومان، طرح ${line.salesPlan}`).join(" ؛ ") || "جزئیات اقلام ثبت نشده است",
                        "تاریخچهٔ ارسال": history.map(entry => `${entry.PreviousStatus} ← ${entry.NewStatus}، مدیر ${entry.ActorUserKey}، ${entry.ChangedAtUtc} UTC`).join(" ؛ ") || "تغییری ثبت نشده است"
                    });
                }).catch(error => ui.notify(error.message || "دریافت جزئیات سفارش ناموفق بود.", "danger"))
                    .finally(() => { detailButton.disabled = false; });
            } else if (item && page.dataset.adminPage === "customers") {
                detailButton.disabled = true;
                void ui.getJson(`/api/admin/store/customers/${encodeURIComponent(item.id)}`).then(customer => {
                    if (!page.isConnected) return;
                    const addresses = JSON.parse(customer.addressesJson || "[]");
                    const orders = JSON.parse(customer.ordersJson || "[]");
                    const feedback = JSON.parse(customer.feedbackJson || "[]");
                    ui.showRecord("جزئیات مشتری", {
                        "نام": `${customer.firstName} ${customer.lastName}`.trim(), "ایمیل": customer.email,
                        "تلفن": customer.phone, "تعداد سفارش": customer.orderCount,
                        "نشانی‌ها": addresses.map(a => [a.Title, a.Recipient, a.Phone, a.Country, a.Province, a.City, a.Street, a.PostalCode, a.IsDefault ? "پیش‌فرض" : ""].filter(Boolean).join("، ")).join(" ؛ ") || "نشانی ثبت نشده است",
                        "۵۰ سفارش اخیر": orders.map(o => `${o.OrderNumber}${o.IsSample ? " (نمونه)" : ""}، ${o.Status}، ${ui.formatNumber(o.TotalAmount)} تومان`).join(" ؛ ") || "سفارشی ثبت نشده است",
                        "تعداد علاقه‌مندی‌ها": customer.favoriteCount, "تعداد پیگیری قیمت": customer.priceAlertCount,
                        "تعداد تعامل‌های مجله": customer.articleInteractionCount,
                        "۵۰ بازخورد اخیر": feedback.map(f => `${f.ProductName}، ${f.FeedbackType}، ${f.Status}${f.IsSample ? " (نمونه)" : ""}: ${f.FeedbackText}`).join(" ؛ ") || "بازخوردی ثبت نشده است"
                    });
                }).catch(error => ui.notify(error.message || "دریافت جزئیات مشتری ناموفق بود.", "danger"))
                    .finally(() => { detailButton.disabled = false; });
            } else if (item) ui.showRecord("جزئیات رکورد", item);
            return;
        }
        const minimumButton = event.target.closest("[data-admin-minimum]");
        if (minimumButton) {
            const page = minimumButton.closest("[data-admin-page]"), item = page?.__adminRows?.[Number(minimumButton.dataset.adminMinimum)];
            const form = document.querySelector("[data-minimum-form]");
            if (item && form) {
                form.elements.warehouseId.value = item.idWarehouse;
                form.elements.inventoryItemId.value = item.idInventoryItem;
                form.elements.minimumQuantity.value = item.minimumQuantity;
                document.querySelector("[data-minimum-product]").textContent = `${item.productName}${item.variantName ? ` — ${item.variantName}` : ""} · ${item.warehouseName} · ${item.salesPlanName}`;
                window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("admin-minimum-modal")).show();
            }
            return;
        }
        const stepButton = event.target.closest("[data-admin-page-step]");
        if (stepButton && !stepButton.disabled) {
            const page = stepButton.closest("[data-admin-page]");
            const nextPage = Number(page?.dataset.adminPageNumber || "1") + Number(stepButton.dataset.adminPageStep);
            if (page?.dataset.adminPage === "catalog") void loadCatalog(page, nextPage);
            else if (page?.dataset.adminPage === "stock") void loadStock(page, nextPage);
            else if (page?.dataset.adminPage === "orders") void loadOrders(page, nextPage);
            else if (page?.dataset.adminPage === "customers") void loadCustomers(page, nextPage);
        }
    });

    document.addEventListener("submit", event => {
        const trackingForm = event.target.closest("[data-order-tracking-form]");
        if (trackingForm) {
            event.preventDefault();
            const save = trackingForm.querySelector('[type="submit"]'); if (save.disabled || !trackingForm.__expected) return;
            const token = trackingForm.querySelector('[name="__RequestVerificationToken"]')?.value || "";
            const body = { ...trackingForm.__expected, carrier: trackingForm.elements.carrier.value, trackingCode: trackingForm.elements.trackingCode.value };
            save.disabled = true;
            void ui.getJson(`/api/admin/store/orders/${encodeURIComponent(trackingForm.elements.id.value)}/tracking`, { method: "PUT", headers: { "Content-Type": "application/json", RequestVerificationToken: token }, body: JSON.stringify(body) })
                .then(result => { ui.notify(result.message, "success"); window.bootstrap?.Modal?.getInstance(document.getElementById("order-tracking-modal"))?.hide(); })
                .catch(error => ui.notify(error.message || "ثبت رهگیری ناموفق بود.", "danger"))
                .finally(() => { save.disabled = false; });
            return;
        }
        const productCreateForm = event.target.closest("[data-product-create-form]");
        if (productCreateForm) {
            event.preventDefault();
            const save = productCreateForm.querySelector('[type="submit"]'); if (save.disabled) return;
            const fields = productCreateForm.elements;
            const body = { key: fields.key.value, code: fields.code.value, name: fields.name.value, englishName: fields.englishName.value, brandId: Number(fields.brandId.value), categoryId: Number(fields.categoryId.value), unitId: Number(fields.unitId.value), priceAmount: Number(fields.priceAmount.value), discountPercent: Number(fields.discountPercent.value), maxPurchaseQuantity: Number(fields.maxPurchaseQuantity.value) };
            if (!Number.isSafeInteger(body.priceAmount)) { ui.notify("قیمت صحیح و معتبر وارد کنید.", "danger"); return; }
            const token = productCreateForm.querySelector('[name="__RequestVerificationToken"]')?.value || ""; save.disabled = true;
            void ui.getJson("/api/admin/store/catalog/products/manage", { method: "POST", headers: { "Content-Type": "application/json", RequestVerificationToken: token }, body: JSON.stringify(body) })
                .then(result => { ui.notify(result.message, "success"); window.bootstrap?.Modal?.getInstance(document.getElementById("product-create-modal"))?.hide(); refreshProductDrafts(); })
                .catch(error => ui.notify(error.message || "ساخت پیش‌نویس ناموفق بود.", "danger"))
                .finally(() => { save.disabled = false; });
            return;
        }
        const customerForm = event.target.closest("[data-customer-editor]");
        if (customerForm) {
            event.preventDefault();
            const button = customerForm.querySelector('[type="submit"]');
            if (button.disabled || !customerForm.__expected) return;
            const token = customerForm.querySelector('[name="__RequestVerificationToken"]')?.value || "";
            const body = { ...customerForm.__expected, firstName: customerForm.elements.firstName.value, lastName: customerForm.elements.lastName.value, phone: customerForm.elements.phone.value };
            button.disabled = true;
            void ui.getJson(`/api/admin/store/customers/${encodeURIComponent(customerForm.elements.id.value)}`, { method: "PUT", headers: { "Content-Type": "application/json", RequestVerificationToken: token }, body: JSON.stringify(body) })
                .then(result => { ui.notify(result.message, "success"); window.bootstrap?.Modal?.getInstance(document.getElementById("customer-editor-modal"))?.hide(); const page = document.querySelector('[data-admin-page="customers"]'); if (page) return loadCustomers(page, Number(page.dataset.adminPageNumber || 1)); })
                .catch(error => ui.notify(error.message || "ذخیرهٔ مشتری ناموفق بود.", "danger"))
                .finally(() => { button.disabled = false; });
            return;
        }
        const faqForm = event.target.closest("[data-faq-editor]");
        if (faqForm) {
            event.preventDefault();
            const button = faqForm.querySelector('[type="submit"]'); if (button.disabled) return;
            const id = faqForm.elements.id.value, token = faqForm.querySelector('[name="__RequestVerificationToken"]')?.value || "";
            const body = { question: faqForm.elements.question.value, answer: faqForm.elements.answer.value, linkText: faqForm.elements.linkText.value, linkUrl: faqForm.elements.linkUrl.value, sortOrder: Number(faqForm.elements.sortOrder.value), isPublished: faqForm.elements.isPublished.checked };
            button.disabled = true;
            void ui.getJson(`/api/admin/store/faq${id ? `/${encodeURIComponent(id)}` : ""}`, { method: id ? "PUT" : "POST", headers: { "Content-Type": "application/json", RequestVerificationToken: token }, body: JSON.stringify(body) })
                .then(result => { ui.notify(result.message, "success"); window.bootstrap?.Modal?.getInstance(document.getElementById("faq-editor-modal"))?.hide(); const page = document.querySelector('[data-admin-page="faq"]'); if (page) return loadFaq(page); })
                .catch(error => ui.notify(error.message || "ذخیرهٔ پرسش ناموفق بود.", "danger"))
                .finally(() => { button.disabled = false; });
            return;
        }
        const productEditor = event.target.closest("[data-product-editor]");
        if (productEditor) {
            event.preventDefault();
            const save = productEditor.querySelector('[type="submit"]');
            if (save.disabled) return;
            const token = productEditor.querySelector('[name="__RequestVerificationToken"]')?.value || "";
            const body = { name: productEditor.elements.name.value, englishName: productEditor.elements.englishName.value, brandId: Number(productEditor.elements.brandId.value), categoryId: Number(productEditor.elements.categoryId.value), priceAmount: Number(productEditor.elements.priceAmount.value), discountPercent: Number(productEditor.elements.discountPercent.value), maxPurchaseQuantity: Number(productEditor.elements.maxPurchaseQuantity.value) };
            if (!Number.isSafeInteger(body.priceAmount)) { ui.notify("قیمت صحیح و معتبر وارد کنید.", "danger"); return; }
            save.disabled = true;
            void ui.getJson(`/api/admin/store/catalog/products/manage/${encodeURIComponent(productEditor.elements.id.value)}`, { method: "PUT", headers: { "Content-Type": "application/json", RequestVerificationToken: token }, body: JSON.stringify(body) })
                .then(result => { ui.notify(result.message, "success"); window.bootstrap?.Modal?.getInstance(document.getElementById("product-editor-modal"))?.hide(); refreshProductDrafts(); const page = document.querySelector('[data-admin-page="catalog"][data-admin-section="products"]'); if (page) return loadCatalog(page, Number(page.dataset.adminPageNumber || 1)); })
                .catch(error => ui.notify(error.message || "ذخیرهٔ کالا ناموفق بود.", "danger"))
                .finally(() => { save.disabled = false; });
            return;
        }
        const brandEditor = event.target.closest("[data-brand-editor]");
        if (brandEditor) {
            event.preventDefault();
            const save = brandEditor.querySelector('[type="submit"]');
            if (save.disabled) return;
            const original = brandEditor.elements.originalKey.value;
            const token = brandEditor.querySelector('[name="__RequestVerificationToken"]')?.value || "";
            const body = { key: brandEditor.elements.key.value, nameFa: brandEditor.elements.nameFa.value, nameEn: brandEditor.elements.nameEn.value, countryCode: brandEditor.elements.countryCode.value };
            save.disabled = true;
            void ui.getJson(`/api/admin/store/catalog/brands/manage${original ? `/${encodeURIComponent(original)}` : ""}`, { method: original ? "PUT" : "POST", headers: { "Content-Type": "application/json", RequestVerificationToken: token }, body: JSON.stringify(body) })
                .then(result => { ui.notify(result.message, "success"); window.bootstrap?.Modal?.getInstance(document.getElementById("brand-editor-modal"))?.hide(); const page = document.querySelector('[data-admin-section="brands"]'); if (page) return loadCatalog(page, Number(page.dataset.adminPageNumber || 1)); })
                .catch(error => ui.notify(error.message || "ذخیرهٔ برند ناموفق بود.", "danger"))
                .finally(() => { save.disabled = false; });
            return;
        }
        const brandForm = event.target.closest("[data-brand-image-form]");
        if (brandForm) {
            event.preventDefault();
            const save = brandForm.querySelector('[type="submit"]'); save.disabled = true;
            void ui.getJson("/api/admin/store/media", { method: "POST", body: new FormData(brandForm) })
                .then(result => { ui.notify(result.message, "success"); window.bootstrap?.Modal?.getInstance(document.getElementById("brand-image-modal"))?.hide(); })
                .catch(error => ui.notify(error.message || "بارگذاری نشان ناموفق بود.", "danger"))
                .finally(() => { save.disabled = false; });
            return;
        }
        const fileForm = event.target.closest("[data-product-file-form]");
        if (fileForm) {
            event.preventDefault();
            if (fileForm.elements.kind.value === "product-document" && !fileForm.elements.title.value.trim()) {
                ui.notify("عنوان فایل PDF را وارد کنید.", "danger"); return;
            }
            const save = fileForm.querySelector('[type="submit"]'); save.disabled = true;
            void ui.getJson("/api/admin/store/media", { method: "POST", body: new FormData(fileForm) })
                .then(result => { ui.notify(result.message, "success"); window.bootstrap?.Modal?.getInstance(document.getElementById("product-file-modal"))?.hide(); })
                .catch(error => ui.notify(error.message || "بارگذاری فایل ناموفق بود.", "danger"))
                .finally(() => { save.disabled = false; });
            return;
        }
        const mediaForm = event.target.closest("[data-media-upload-form]");
        if (mediaForm) {
            event.preventDefault();
            const result = mediaForm.querySelector("[data-media-upload-result]");
            const save = mediaForm.querySelector('[type="submit"]');
            if (mediaForm.elements.kind.value === "country" && (!mediaForm.elements.countryCode.value || mediaForm.elements.countryCode.disabled)) {
                result.textContent = "کشور را انتخاب کنید."; return;
            }
            if (mediaForm.elements.kind.value === "article" && (!mediaForm.elements.articleKey.value || mediaForm.elements.articleKey.disabled)) {
                result.textContent = "مقاله را انتخاب کنید."; return;
            }
            result.textContent = "در حال بارگذاری…"; save.disabled = true;
            void ui.getJson("/api/admin/store/media", { method: "POST", body: new FormData(mediaForm) })
                .then(data => {
                    let base;
                    try {
                        const configured = new URL(mediaForm.dataset.publicSiteUrl);
                        if (["http:", "https:"].includes(configured.protocol) && !configured.username && !configured.password && configured.pathname === "/" && !configured.search && !configured.hash) base = configured.origin;
                    } catch { /* Show the stored relative paths if the public origin is not configured. */ }
                    const display = path => base && typeof path === "string" && path.startsWith("content/") ? `${base}/${path}` : path;
                    result.textContent = data.thumbnail ? `نشانی فایل: ${display(data.path)} — تصویر کوچک: ${display(data.thumbnail)}` : `نشانی فایل: ${display(data.path)}`;
                    ui.notify(data.message, "success");
                })
                .catch(error => { result.textContent = error.message || "بارگذاری ناموفق بود."; ui.notify(result.textContent, "danger"); })
                .finally(() => { save.disabled = false; });
            return;
        }
        const imageForm = event.target.closest("[data-product-image-form]");
        if (imageForm) {
            event.preventDefault();
            const token = imageForm.querySelector('[name="__RequestVerificationToken"]')?.value || "";
            const save = imageForm.querySelector('[type="submit"]'); save.disabled = true;
            void ui.getJson(`/api/admin/store/product-images/${encodeURIComponent(imageForm.elements.productId.value)}`, { method: "POST", headers: { RequestVerificationToken: token }, body: new FormData(imageForm) })
                .then(result => { ui.notify(result.message, "success"); window.bootstrap?.Modal?.getInstance(document.getElementById("product-image-modal"))?.hide(); refreshProductDrafts(); })
                .catch(error => ui.notify(error.message || "بارگذاری تصویر ناموفق بود.", "danger"))
                .finally(() => { save.disabled = false; });
            return;
        }
        const categoryForm = event.target.closest("[data-category-content-form]");
        if (categoryForm) {
            event.preventDefault();
            const lines = value => value.split(/\r?\n/).map(line => line.trim()).filter(Boolean);
            const specs = lines(categoryForm.elements.specs.value).map(line => line.split("|").map(part => part.trim()));
            if (specs.some(pair => pair.length !== 2 || pair.some(value => !value))) {
                ui.notify("هر مشخصه باید یک عنوان و مقدار با جداکنندهٔ | داشته باشد.", "danger"); return;
            }
            const token = categoryForm.querySelector('[name="__RequestVerificationToken"]')?.value || "";
            const body = { intro: categoryForm.elements.intro.value, highlights: JSON.stringify(lines(categoryForm.elements.highlights.value)), specs: JSON.stringify(specs) };
            const save = categoryForm.querySelector('[type="submit"]'); save.disabled = true;
            void ui.getJson(`/api/admin/store/category-content/${encodeURIComponent(categoryForm.elements.key.value)}`, { method: "PUT", headers: { "Content-Type": "application/json", RequestVerificationToken: token }, body: JSON.stringify(body) })
                .then(result => { ui.notify(result.message, "success"); window.bootstrap?.Modal?.getInstance(document.getElementById("category-content-modal"))?.hide(); })
                .catch(error => ui.notify(error.message || "ذخیرهٔ محتوای دسته ناموفق بود.", "danger"))
                .finally(() => { save.disabled = false; });
            return;
        }
        const minimumForm = event.target.closest("[data-minimum-form]");
        if (minimumForm) {
            event.preventDefault();
            if (minimumForm.dataset.saving === "true") return;
            const body = { warehouseId: Number(minimumForm.elements.warehouseId.value), inventoryItemId: Number(minimumForm.elements.inventoryItemId.value), minimumQuantity: Number(minimumForm.elements.minimumQuantity.value) };
            const token = minimumForm.querySelector('[name="__RequestVerificationToken"]')?.value || "";
            const save = minimumForm.querySelector('[type="submit"]');
            minimumForm.dataset.saving = "true"; save.disabled = true;
            void ui.getJson("/api/admin/warehouse/stock/minimum", { method: "PUT", headers: { "Content-Type": "application/json", RequestVerificationToken: token }, body: JSON.stringify(body) })
                .then(result => { ui.notify(result.message, "success"); window.bootstrap?.Modal?.getInstance(document.getElementById("admin-minimum-modal"))?.hide(); const page = minimumForm.closest("main")?.querySelector('[data-admin-page="stock"]') || document.querySelector('[data-admin-page="stock"]'); if (page) void loadStock(page, Number(page.dataset.adminPageNumber || 1)); })
                .catch(error => ui.notify(error.message || "ذخیرهٔ حداقل ناموفق بود.", "danger"))
                .finally(() => { minimumForm.dataset.saving = "false"; save.disabled = false; });
            return;
        }
        const form = event.target.closest("[data-admin-filter]");
        if (!form) return;
        event.preventDefault();
        const page = form.closest("[data-admin-page]");
        if (page?.dataset.adminPage === "catalog") void loadCatalog(page, 1);
        else if (page?.dataset.adminPage === "warehouses") void loadWarehouses(page);
        else if (page?.dataset.adminPage === "stock") void loadStock(page, 1);
        else if (page?.dataset.adminPage === "orders") void loadOrders(page, 1);
        else if (page?.dataset.adminPage === "customers") void loadCustomers(page, 1);
    });

    document.addEventListener("click", event => {
        const reset = event.target.closest("[data-admin-reset-filter]");
        if (!reset) return;
        const page = reset.closest("[data-admin-page]");
        const form = page?.querySelector("[data-admin-filter]");
        form?.reset();
        if (page?.dataset.adminPage === "catalog") void loadCatalog(page, 1);
        else if (page?.dataset.adminPage === "warehouses") void loadWarehouses(page);
        else if (page?.dataset.adminPage === "stock") void loadStock(page, 1);
    });

    window.AdminPages = Object.freeze({ initialize });
})();
