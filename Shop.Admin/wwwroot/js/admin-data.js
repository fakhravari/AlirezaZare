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
            if (page.dataset.adminSection === "categories" && page.dataset.canEditCategory === "true") {
                const edit = document.createElement("button");
                edit.type = "button"; edit.className = "btn btn-xs btn-default ms-1";
                edit.dataset.categoryContent = String(index); edit.textContent = "محتوای دسته";
                actions.append(edit);
            }
            if (page.dataset.adminSection === "products" && page.dataset.canUploadImage === "true") {
                const upload = document.createElement("button");
                upload.type = "button"; upload.className = "btn btn-xs btn-default ms-1";
                upload.dataset.productImage = String(index); upload.textContent = "تصویر محصول";
                actions.append(upload);
                const file = document.createElement("button");
                file.type = "button"; file.className = "btn btn-xs btn-default ms-1";
                file.dataset.productFile = String(index); file.textContent = "فایل محصول";
                actions.append(file);
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

    async function loadPlans(select, signal) {
        const selected = select.value;
        const data = await ui.getJson("/api/store/sales-plans", { signal });
        const plans = Array.isArray(data?.plans) ? data.plans : [];
        const options = [new Option("همهٔ طرح‌ها", "")];
        plans.forEach(plan => options.push(new Option(plan.name, plan.id)));
        select.replaceChildren(...options);
        if (plans.some(plan => String(plan.id) === selected)) select.value = selected;
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
            if (planSelect && !planSelect.dataset.loaded) tasks.push(loadPlans(planSelect, controller.signal).then(() => true));
            else tasks.push(Promise.resolve(null));
            const [items, warehouses] = await Promise.all(tasks);
            if (!page.isConnected || requests.get(page) !== controller) return;
            if (warehouseSelect && warehouses) {
                const selected = warehouseSelect.value;
                const options = [new Option("همهٔ انبارها", "")];
                (warehouses || []).forEach(item => options.push(new Option(item.name, String(item.id))));
                warehouseSelect.replaceChildren(...options);
                warehouseSelect.value = selected;
                warehouseSelect.dataset.loaded = "true";
            }
            if (planSelect) planSelect.dataset.loaded = "true";
            const rows = Array.isArray(items) ? items : [];
            renderRows(page, rows, item => [
                `${item.warehouseName} (${item.warehouseCode})`, item.storeProductKey,
                item.variantName ? `${item.productName} ـ ${item.variantName}` : item.productName,
                item.salesPlanName, ui.formatNumber(item.quantity), ui.formatNumber(item.minimumQuantity),
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
            const labels = { "pending-payment": "در انتظار پرداخت", "simulated-payment-confirmed": "پرداخت آزمایشی تأیید شده" };
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

    function refresh(page, announce = true) {
        if (page.dataset.adminPage === "catalog") return loadCatalog(page, Number(page.dataset.adminPageNumber || "1"), announce);
        if (page.dataset.adminPage === "warehouses") return loadWarehouses(page, announce);
        if (page.dataset.adminPage === "stock") return loadStock(page, Number(page.dataset.adminPageNumber || "1"), announce);
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
        });
    }

    document.addEventListener("click", event => {
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
                    ui.showRecord("جزئیات سفارش", {
                        "شماره سفارش": order.orderNumber, "دادهٔ نمونه": order.isSample ? "بله" : "خیر", "مشتری": order.customerName,
                        "وضعیت": order.status === "simulated-payment-confirmed" ? "پرداخت آزمایشی تأیید شده" : order.status === "pending-payment" ? "در انتظار پرداخت" : order.status,
                        "مبلغ تومان": order.totalAmount,
                        "نشانی ثبت‌شده": Object.values(address).filter(value => typeof value === "string" && value).join("، "),
                        "اقلام": items.map(line => `${line.name}، ${ui.formatNumber(line.quantity)} عدد، مبلغ واحد ${ui.formatNumber(line.unitAmount)} تومان، طرح ${line.salesPlan}`).join(" ؛ ") || "جزئیات اقلام ثبت نشده است"
                    });
                }).catch(error => ui.notify(error.message || "دریافت جزئیات سفارش ناموفق بود.", "danger"))
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
        }
    });

    document.addEventListener("submit", event => {
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
            result.textContent = "در حال بارگذاری…"; save.disabled = true;
            void ui.getJson("/api/admin/store/media", { method: "POST", body: new FormData(mediaForm) })
                .then(data => { result.textContent = data.thumbnail ? `مسیر فایل: ${data.path} — تصویر کوچک: ${data.thumbnail}` : `مسیر فایل: ${data.path}`; ui.notify(data.message, "success"); })
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
                .then(result => { ui.notify(result.message, "success"); window.bootstrap?.Modal?.getInstance(document.getElementById("product-image-modal"))?.hide(); })
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
            const body = { warehouseId: Number(minimumForm.elements.warehouseId.value), inventoryItemId: Number(minimumForm.elements.inventoryItemId.value), minimumQuantity: Number(minimumForm.elements.minimumQuantity.value) };
            const token = minimumForm.querySelector('[name="__RequestVerificationToken"]')?.value || "";
            void ui.getJson("/api/admin/warehouse/stock/minimum", { method: "PUT", headers: { "Content-Type": "application/json", RequestVerificationToken: token }, body: JSON.stringify(body) })
                .then(result => { ui.notify(result.message, "success"); window.bootstrap?.Modal?.getInstance(document.getElementById("admin-minimum-modal"))?.hide(); const page = minimumForm.closest("main")?.querySelector('[data-admin-page="stock"]') || document.querySelector('[data-admin-page="stock"]'); if (page) void loadStock(page, Number(page.dataset.adminPageNumber || 1)); })
                .catch(error => ui.notify(error.message || "ذخیرهٔ حداقل ناموفق بود.", "danger"));
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
