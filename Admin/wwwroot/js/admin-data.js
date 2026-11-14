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
        target.classList.toggle("text-danger", isError); target.classList.toggle("text-muted", !isError); target.classList.add("small");
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
        container.hidden = current <= 1 && !hasNext;
        const label = container.querySelector("[data-admin-page-label]");
        const previous = container.querySelector('[data-admin-page-step="-1"]');
        const next = container.querySelector('[data-admin-page-step="1"]');
        if (label) label.textContent = `صفحهٔ ${ui.formatNumber(current)}`;
        if (previous) previous.disabled = current <= 1;
        if (next) next.disabled = !hasNext;
        page.dataset.adminPageNumber = String(current);
    }

    function setIconAction(button, label, icon) {
        button.title = label;
        button.setAttribute("aria-label", label);
        ui.withIcon(button, icon);
        button.querySelector("i")?.classList.remove("me-1");
        const accessibleLabel = document.createElement("span");
        accessibleLabel.className = "visually-hidden";
        accessibleLabel.textContent = label;
        button.append(accessibleLabel);
    }

    function renderRows(page, rows, columns) {
        const body = page.querySelector("[data-admin-results]");
        const empty = page.querySelector("[data-admin-empty]");
        if (!body) return;
        ui.destroyDataTable(page.querySelector("[data-admin-datatable]"));
        page.__adminRows = rows;
        body.replaceChildren();
        rows.forEach((item, index) => {
            const row = document.createElement("tr");
            for (const [columnIndex, value] of columns(item).entries()) {
                const cell = document.createElement("td");
                if (page.dataset.adminSection === "payment-institutions" && columnIndex === 2 && item.logoUrl) {
                    const image = document.createElement("img"); image.className = "admin-brand-logo"; image.src = item.logoUrl; image.alt = ""; image.width = 44; image.height = 44; image.loading = "lazy"; image.addEventListener("error", () => image.remove(), { once: true }); cell.append(image);
                } else if (page.dataset.adminSection === "brands" && columnIndex === 1) {
                    const brand = document.createElement("span");
                    brand.className = "admin-brand-name-with-logo";
                    if (item.logoUrl) {
                        const image = document.createElement("img");
                        image.className = "admin-brand-logo";
                        image.src = item.logoUrl;
                        image.alt = "";
                        image.loading = "lazy";
                        image.addEventListener("error", () => image.remove(), { once: true });
                        brand.append(image);
                    } else {
                        const placeholder = document.createElement("span");
                        placeholder.className = "admin-brand-logo-placeholder";
                        placeholder.setAttribute("aria-hidden", "true");
                        placeholder.innerHTML = '<i class="fa fa-image"></i>';
                        brand.append(placeholder);
                    }
                    const name = document.createElement("span");
                    name.textContent = value == null || value === "" ? "—" : String(value);
                    brand.append(name);
                    cell.append(brand);
                } else {
                    cell.textContent = value == null || value === "" ? "—" : String(value);
                }
                row.append(cell);
            }
            const actions = document.createElement("td");
            actions.className = "admin-row-actions";
            if (page.dataset.adminSection !== "payment-institutions") {
                const details = document.createElement("button");
                details.type = "button";
                details.className = "btn btn-xs btn-default";
                details.dataset.adminRecord = String(index);
                details.innerHTML = '<i class="fa fa-eye" aria-hidden="true"></i><span class="visually-hidden">نمایش جزئیات</span>';
                details.setAttribute("aria-label", "نمایش جزئیات رکورد");
                actions.append(details);
            }
            if (page.dataset.adminPage === "product-drafts") {
                const publish = document.createElement("button");
                publish.type = "button"; publish.className = "btn btn-xs btn-primary";
                publish.dataset.productPublish = String(index); setIconAction(publish, "انتشار کالا", "fa-paper-plane");
                publish.disabled = !item.hasImage; actions.append(publish);
            }
            if (page.dataset.adminPage === "customers") {
                const edit = document.createElement("button");
                edit.type = "button"; edit.className = "btn btn-xs btn-default ms-1";
                edit.dataset.customerEdit = String(index); edit.textContent = "ویرایش";
                actions.append(edit);
                const remove = document.createElement("button");
                remove.type = "button"; remove.className = "btn btn-xs btn-danger ms-1";
                remove.dataset.customerDelete = String(index); remove.textContent = "حذف";
                actions.append(remove);
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
            if (page.dataset.adminSection === "categories" && page.dataset.canUploadImage === "true") {
                const edit = document.createElement("button"); edit.type = "button"; edit.className = "btn btn-xs btn-default ms-1";
                edit.dataset.categoryEdit = String(index); edit.textContent = "ویرایش دسته"; actions.append(edit);
            }
            if (page.dataset.adminSection === "products" && page.dataset.canUploadImage === "true") {
                const edit = document.createElement("button");
                edit.type = "button"; edit.className = "btn btn-xs btn-default";
                edit.dataset.productEdit = String(index); setIconAction(edit, "ویرایش کالا", "fa-pen-to-square");
                actions.append(edit);
                const unpublish = document.createElement("button");
                unpublish.type = "button"; unpublish.className = "btn btn-xs btn-default";
                unpublish.dataset.productUnpublish = String(index); setIconAction(unpublish, "خروج از انتشار", "fa-eye-slash");
                actions.append(unpublish);
                const upload = document.createElement("button");
                upload.type = "button"; upload.className = "btn btn-xs btn-default";
                upload.dataset.productImage = String(index); setIconAction(upload, "مدیریت تصویر محصول", "fa-image");
                actions.append(upload);
                const file = document.createElement("button");
                file.type = "button"; file.className = "btn btn-xs btn-default";
                file.dataset.productFile = String(index); setIconAction(file, "مدیریت فایل‌های محصول", "fa-paperclip");
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
            if (page.dataset.adminSection === "payment-institutions" && page.dataset.canUploadImage === "true") {
                const upload = document.createElement("button"); upload.type = "button"; upload.className = "btn btn-xs btn-default ms-1"; upload.dataset.paymentInstitutionImage = String(index); upload.textContent = item.logoPath ? "تغییر لوگو" : "بارگذاری لوگو"; actions.append(upload);
            }
            if (page.dataset.adminSection === "attributes" && page.dataset.canUploadImage === "true") {
                const edit = document.createElement("button");
                edit.type = "button"; edit.className = "btn btn-xs btn-default ms-1";
                edit.dataset.attributeEdit = String(index); edit.textContent = "ویرایش ویژگی";
                actions.append(edit);
            }
            if (["categories", "brands", "attributes"].includes(page.dataset.adminSection) && page.dataset.canUploadImage === "true") {
                const statusButton = document.createElement("button");
                statusButton.type = "button"; statusButton.className = "btn btn-xs btn-default ms-1";
                statusButton.dataset.catalogStatus = String(index);
                statusButton.textContent = item.isActive ? "خروج از انتشار" : "انتشار";
                actions.append(statusButton);
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
        ui.refreshDataTable(page.querySelector("[data-admin-datatable]"));
    }

    function queryValue(form, name) {
        const value = new FormData(form).get(name);
        return typeof value === "string" ? value.trim() : value;
    }

    async function loadMediaLibrary(root) {
        const statusNode = root.querySelector("[data-media-library-status]");
        const container = root.querySelector("[data-media-library-items]");
        if (!container) return;
        if (root.dataset.loading === "true") { root.dataset.reload = "true"; return; }
        root.dataset.loading = "true";
        if (statusNode) statusNode.textContent = "در حال دریافت رسانه‌های موجود…";
        try {
            const allItems = await ui.getJson("/api/admin/store/media/library");
            const kindFilter = root.dataset.mediaLibraryKind;
            const items = kindFilter ? allItems.filter(item => item.kind === kindFilter) : allItems;
            container.replaceChildren();
            const labels = { slider: "اسلایدر", testimonial: "دیدگاه", trust: "اعتماد", identity: "هویت فروشگاه", payment: "پرداخت" };
            for (const item of items) {
                const card = document.createElement("article"); card.className = "admin-media-card";
                const label = labels[item.kind] || item.kind;
                const preview = document.createElement("img"); preview.className = "admin-media-preview"; preview.src = item.thumbnailUrl || item.url || ""; preview.alt = `پیش‌نمایش ${label}`; preview.loading = "lazy";
                const details = document.createElement("div"); details.className = "admin-media-details";
                const title = document.createElement("strong"); title.textContent = label;
                const path = document.createElement("code"); path.dir = "ltr"; path.className = "admin-media-path"; path.textContent = item.path;
                const meta = document.createElement("small"); meta.className = "text-muted"; meta.textContent = `${ui.formatNumber(Math.ceil(item.sizeBytes / 1024))} کیلوبایت · ${new Date(item.modifiedUtc).toLocaleDateString("fa-IR")}`;
                details.append(title, path, meta);
                const actions = document.createElement("div"); actions.className = "admin-media-actions";
                const copy = document.createElement("button"); copy.type = "button"; copy.className = "btn btn-default btn-sm"; copy.dataset.mediaCopy = item.url || ""; copy.textContent = "کپی نشانی";
                const remove = document.createElement("button"); remove.type = "button"; remove.className = "btn btn-danger btn-sm"; remove.dataset.mediaDelete = item.path; remove.dataset.mediaKind = item.kind; remove.textContent = "حذف"; remove.disabled = !item.canDelete;
                if (!item.canDelete) remove.title = "این فایل با بارگذاری پنل ساخته نشده و حذف آن از اینجا مجاز نیست.";
                actions.append(copy, remove); card.append(preview, details, actions); container.append(card);
            }
            if (statusNode) statusNode.textContent = items.length ? `${ui.formatNumber(items.length)} رسانهٔ عمومی` : "رسانه‌ای در این پوشه‌ها ثبت نشده است.";
        } catch (error) {
            if (statusNode) statusNode.textContent = error.message || "دریافت کتابخانهٔ رسانه ناموفق بود.";
        } finally {
            root.dataset.loading = "false";
            if (root.dataset.reload === "true") { delete root.dataset.reload; void loadMediaLibrary(root); }
        }
    }

    document.addEventListener("click", async event => {
        const refresh = event.target.closest("[data-media-library-refresh]");
        if (refresh) { const root = refresh.closest("[data-media-library]"); if (root) void loadMediaLibrary(root); return; }
        const copy = event.target.closest("[data-media-copy]");
        if (copy) {
            if (!copy.dataset.mediaCopy) return;
            try { await navigator.clipboard.writeText(copy.dataset.mediaCopy); copy.textContent = "کپی شد"; }
            catch { ui.notify("کپی نشانی در این مرورگر در دسترس نیست.", "danger"); }
            return;
        }
        const remove = event.target.closest("[data-media-delete]");
        if (remove) {
            if (!window.confirm("این رسانه و تصویر کوچک آن برای همیشه از فضای فایل حذف شوند؟")) return;
            const root = remove.closest("[data-media-library]"), form = root?.querySelector("[data-media-library-token]");
            if (!root || !form) return;
            const body = new FormData(form); body.set("kind", remove.dataset.mediaKind); body.set("path", remove.dataset.mediaDelete);
            remove.disabled = true;
            try { const result = await ui.getJson("/api/admin/store/media/delete", { method: "POST", body }); ui.notify(result.message, "success"); await loadMediaLibrary(root); }
            catch (error) { ui.notify(error.message || "حذف رسانه ناموفق بود.", "danger"); remove.disabled = false; }
        }
    });

    async function loadAllCategories(signal) {
        const items = [];
        const size = 200;
        let currentPage = 1;
        let total = 0;
        do {
            const url = new URL("/api/admin/store/catalog/categories", window.location.origin);
            url.searchParams.set("page", String(currentPage));
            url.searchParams.set("pageSize", String(size));
            const result = await ui.getJson(url, { signal });
            items.push(...(result.items || []));
            total = result.total || 0;
            currentPage++;
        } while (items.length < total);
        return items;
    }

    function renderCategoryTree(page, allRows, search = "") {
        const body = page.querySelector("[data-admin-results]");
        const empty = page.querySelector("[data-admin-empty]");
        if (!body) return;

        const byId = new Map(allRows.map(item => [String(item.id), item]));
        const normalizedSearch = search.toLocaleLowerCase();
        const matches = new Set(allRows.filter(item => !normalizedSearch ||
            [item.id, item.name, item.englishName, item.slug].some(value => String(value || "").toLocaleLowerCase().includes(normalizedSearch)))
            .map(item => String(item.id)));
        const included = new Set(matches);
        if (normalizedSearch) {
            for (const id of matches) {
                let parentId = byId.get(id)?.parentId;
                const visited = new Set([id]);
                while (parentId && byId.has(String(parentId)) && !visited.has(String(parentId))) {
                    included.add(String(parentId));
                    visited.add(String(parentId));
                    parentId = byId.get(String(parentId)).parentId;
                }
            }
        } else {
            for (const item of allRows) included.add(String(item.id));
        }

        const childrenByParent = new Map();
        for (const item of allRows) {
            const id = String(item.id);
            if (!included.has(id)) continue;
            const parentId = included.has(String(item.parentId)) ? String(item.parentId) : "";
            if (!childrenByParent.has(parentId)) childrenByParent.set(parentId, []);
            childrenByParent.get(parentId).push(item);
        }
        if (!page.__adminCategoryExpanded) {
            page.__adminCategoryExpanded = new Set((childrenByParent.get("") || [])
                .filter(item => (childrenByParent.get(String(item.id)) || []).length)
                .map(item => String(item.id)));
        }
        if (normalizedSearch) {
            for (const id of matches) {
                let parentId = byId.get(id)?.parentId;
                while (parentId && byId.has(String(parentId))) {
                    page.__adminCategoryExpanded.add(String(parentId));
                    parentId = byId.get(String(parentId)).parentId;
                }
            }
        }

        const flattened = [];
        const seen = new Set();
        const visit = (item, depth) => {
            const id = String(item.id);
            if (seen.has(id)) return;
            seen.add(id);
            const children = childrenByParent.get(id) || [];
            flattened.push({ item, depth, hasChildren: children.length > 0 });
            if (children.length && page.__adminCategoryExpanded.has(id))
                children.forEach(child => visit(child, depth + 1));
        };
        (childrenByParent.get("") || []).forEach(item => visit(item, 0));
        for (const item of allRows) if (included.has(String(item.id)) && !seen.has(String(item.id))) visit(item, 0);

        page.__adminRows = flattened.map(entry => entry.item);
        ui.destroyDataTable(page.querySelector("[data-admin-datatable]"));
        body.replaceChildren();
        flattened.forEach(({ item, depth, hasChildren }, index) => {
            const row = document.createElement("tr");
            row.className = "admin-category-row";
            row.setAttribute("aria-level", String(depth + 1));
            const id = String(item.id);
            if (hasChildren) row.setAttribute("aria-expanded", String(page.__adminCategoryExpanded.has(id)));

            const idCell = document.createElement("td"); idCell.textContent = item.id;
            const nameCell = document.createElement("td");
            const label = document.createElement("div"); label.className = "admin-category-tree-label";
            label.style.paddingInlineStart = `${depth * 1.35}rem`;
            if (hasChildren) {
                const toggle = document.createElement("button");
                toggle.type = "button"; toggle.className = "admin-category-toggle";
                toggle.setAttribute("aria-expanded", String(page.__adminCategoryExpanded.has(id)));
                toggle.setAttribute("aria-label", `${page.__adminCategoryExpanded.has(id) ? "بستن" : "بازکردن"} زیرشاخه‌های ${item.name}`);
                toggle.append(Object.assign(document.createElement("span"), { className: "admin-category-chevron", textContent: "›" }));
                toggle.addEventListener("click", () => {
                    if (page.__adminCategoryExpanded.has(id)) page.__adminCategoryExpanded.delete(id);
                    else page.__adminCategoryExpanded.add(id);
                    renderCategoryTree(page, page.__adminCategoryData || allRows, queryValue(page.querySelector("[data-admin-filter]"), "q"));
                });
                label.append(toggle);
            } else {
                label.append(Object.assign(document.createElement("span"), { className: "admin-category-toggle-spacer" }));
            }
            label.append(Object.assign(document.createElement("span"), { className: "admin-category-name", textContent: item.name || "—" }));
            nameCell.append(label);

            const cells = [idCell, nameCell, item.englishName, item.parentId, item.slug, item.isActive ? "منتشر" : "پیش‌نویس"];
            for (const value of cells.slice(2)) {
                const cell = document.createElement("td");
                cell.textContent = value == null || value === "" ? "—" : String(value);
                row.append(cell);
            }
            row.prepend(idCell, nameCell);

            const actions = document.createElement("td"); actions.className = "admin-row-actions";
            const details = document.createElement("button"); details.type = "button"; details.className = "btn btn-xs btn-default";
            details.dataset.adminRecord = String(index); details.setAttribute("aria-label", "نمایش جزئیات رکورد");
            details.innerHTML = '<i class="fa fa-eye" aria-hidden="true"></i><span class="visually-hidden">نمایش جزئیات</span>';
            actions.append(details);
            if (page.dataset.canEditCategory === "true") {
                const content = document.createElement("button"); content.type = "button"; content.className = "btn btn-xs btn-default ms-1";
                content.dataset.categoryContent = String(index); content.textContent = "محتوای دسته"; ui.withIcon(content, "fa-file-lines"); actions.append(content);
            }
            if (page.dataset.canUploadImage === "true") {
                const edit = document.createElement("button"); edit.type = "button"; edit.className = "btn btn-xs btn-default ms-1";
                edit.dataset.categoryEdit = String(index); edit.textContent = "ویرایش دسته"; ui.withIcon(edit, "fa-pen-to-square"); actions.append(edit);
                const statusButton = document.createElement("button"); statusButton.type = "button"; statusButton.className = "btn btn-xs btn-default ms-1";
                statusButton.dataset.catalogStatus = String(index); statusButton.textContent = item.isActive ? "خروج از انتشار" : "انتشار"; ui.withIcon(statusButton, item.isActive ? "fa-eye-slash" : "fa-paper-plane"); actions.append(statusButton);
            }
            row.append(actions);
            body.append(row);
        });
        empty?.classList.toggle("d-none", flattened.length > 0);
        const pagination = page.querySelector("[data-admin-pagination]");
        if (pagination) pagination.hidden = true;
        const count = page.querySelector("[data-admin-count]");
        if (count) count.textContent = `نمایش ${ui.formatNumber(flattened.length)} از ${ui.formatNumber(allRows.length)} دسته`;
        ui.refreshDataTable(page.querySelector("[data-admin-datatable]"));
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
            if (page.dataset.adminSection === "categories") {
                const allRows = await loadAllCategories(controller.signal);
                if (!page.isConnected || requests.get(page) !== controller) return;
                page.__adminCategoryData = allRows;
                renderCategoryTree(page, allRows, query);
                page.dataset.adminPageNumber = "1";
                status(page, "درخت دسته‌ها از دیتابیس دریافت شد.");
                if (announce) ui.notify("فهرست به‌روز شد.", "success");
                return;
            }
            const result = await ui.getJson(api, { signal: controller.signal });
            if (!page.isConnected || requests.get(page) !== controller) return;
            const section = page.dataset.adminSection;
            const rows = result.items || [];
            const columns = item => section === "products"
                ? [item.code, item.name, item.brandName, item.categoryName]
                : section === "categories"
                    ? [item.id, item.name, item.englishName, item.parentId, item.slug, item.isActive ? "منتشر" : "پیش‌نویس"]
                    : section === "brands"
                        ? [item.id, item.name, item.englishName, item.countryCode, item.isActive ? "منتشر" : "پیش‌نویس"]
                        : section === "payment-institutions"
                            ? [item.nameFa, item.code, item.logoUrl ? "ثبت شده" : "—", item.logoPath]
                        : [item.key, item.label || item.name, item.sourceKey || item.source, item.filterKey, item.isFilterable ? "بله" : "خیر", item.isHighlight ? "بله" : "خیر", item.isActive ? "منتشر" : "پیش‌نویس"];
            renderRows(page, rows, columns);
            updateCount(page, result.total ?? 0, rows.length);
            updatePagination(page, result.page ?? pageNumber, (result.page ?? pageNumber) * (result.pageSize ?? pageSize) < (result.total ?? 0));
            status(page, "داده‌ها از دیتابیس دریافت شد.");
            if (announce) ui.notify("فهرست به‌روز شد.", "success");
        } catch (error) {
            if (error.name === "AbortError") return;
            if (!page.isConnected || requests.get(page) !== controller) return;
            if (page.dataset.adminSection === "categories") renderCategoryTree(page, [], query);
            else renderRows(page, [], () => []);
            status(page, error.message || "دریافت داده ناموفق بود.", true);
            ui.notify(error.message || "دریافت داده ناموفق بود.", "danger");
        } finally { finishRequest(page, controller); }
    }

    async function loadProductDrafts(page, pageNumber = Number(page.dataset.adminPageNumber || "1"), announce = false) {
        const controller = beginRequest(page);
        try {
            const api = new URL("/api/admin/store/catalog/products/drafts", window.location.origin);
            api.searchParams.set("page", String(pageNumber));
            api.searchParams.set("pageSize", String(pageSize));
            const query = queryValue(page.querySelector("[data-admin-filter]"), "q");
            if (query) api.searchParams.set("q", query);
            const result = await ui.getJson(api, { signal: controller.signal });
            if (!page.isConnected || requests.get(page) !== controller) return;
            const rows = result.items || [];
            renderRows(page, rows, item => [item.code, item.name, item.brandName, item.categoryName, ui.formatNumber(item.priceAmount), item.hasImage ? "ثبت شده" : "ثبت نشده"]);
            updateCount(page, result.total ?? 0, rows.length);
            updatePagination(page, result.page ?? pageNumber, (result.page ?? pageNumber) * (result.pageSize ?? pageSize) < (result.total ?? 0));
            status(page, rows.length ? "پیش‌نویس‌ها از دیتابیس دریافت شدند." : "پیش‌نویسی با این فیلتر پیدا نشد.");
            if (announce) ui.notify("فهرست پیش‌نویس‌ها به‌روز شد.", "success");
        } catch (error) {
            if (error.name !== "AbortError" && page.isConnected && requests.get(page) === controller) status(page, error.message || "دریافت پیش‌نویس‌ها ناموفق بود.", true);
        } finally { finishRequest(page, controller); }
    }

    function refreshProductDrafts(pageNumber = 1) { const page = document.querySelector('[data-admin-page="product-drafts"]'); if (page) void loadProductDrafts(page, pageNumber); }

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
                all.forEach(item => options.push(new Option(`${item.companyName} / ${item.name} (${item.code})`, String(item.id))));
                warehouseSelect.replaceChildren(...options);
                if (all.some(item => String(item.id) === selected)) warehouseSelect.value = selected;
            }
            renderRows(page, rows, item => [`${item.companyName} / ${item.name} (${item.code})`, item.isSalesWarehouse ? "بله" : "خیر", item.isActive ? "فعال" : "غیرفعال"]);
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
                (warehouses || []).forEach(item => options.push(new Option(`${item.companyName} / ${item.name} (${item.code})`, String(item.id))));
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
                `${item.companyName} / ${item.warehouseName} (${item.warehouseCode})`, item.storeProductKey,
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
            const body = page.querySelector("[data-faq-rows]"); ui.destroyDataTable(page.querySelector("[data-admin-datatable]")); body.replaceChildren();
            rows.forEach((item, index) => {
                const row = document.createElement("tr");
                for (const value of [item.question, ui.formatNumber(item.sortOrder), item.isPublished ? "منتشرشده" : "منتشرنشده"]) {
                    const cell = document.createElement("td"); cell.textContent = value; row.append(cell);
                }
                const cell = document.createElement("td"), edit = document.createElement("button"), remove = document.createElement("button");
                edit.type = "button"; edit.className = "btn btn-sm btn-default me-1"; edit.dataset.faqEdit = String(index); edit.textContent = "ویرایش";
                remove.type = "button"; remove.className = "btn btn-sm btn-danger"; remove.dataset.faqDelete = String(index); remove.textContent = "حذف";
                ui.withIcon(edit, "fa-pen-to-square"); ui.withIcon(remove, "fa-trash-can");
                cell.append(edit, remove); row.append(cell); body.append(row);
            });
            ui.refreshDataTable(page.querySelector("[data-admin-datatable]"));
            status(page, rows.length ? "" : "پرسشی ثبت نشده است.");
        } catch (error) {
            if (error.name !== "AbortError" && page.isConnected && requests.get(page) === controller) status(page, error.message || "دریافت پرسش‌ها ناموفق بود.", true);
        } finally { finishRequest(page, controller); }
    }

    async function loadMagazine() {
        const page = document.querySelector("[data-magazine-admin]"); if (!page) return;
        const statusNode = page.querySelector("[data-mag-status]"), body = page.querySelector("[data-mag-rows]");
        try {
            const rows = await ui.getJson(page.dataset.api); ui.destroyDataTable(page.querySelector("[data-admin-datatable]")); body.replaceChildren();
            rows.forEach(item => {
                const tr = document.createElement("tr");
                for (const text of [item.title, item.articleKey, ui.formatNumber(item.count), item.isPublished ? "منتشرشده" : "پیش‌نویس"]) { const td = document.createElement("td"); td.textContent = text; tr.append(td); }
                const td = document.createElement("td");
                for (const [label, action, icon] of [["ویرایش", "edit", "fa-pen-to-square"], ["تصویر", "image", "fa-image"], ["حذف", "delete", "fa-trash-can"]]) { const button = document.createElement("button"); button.type = "button"; button.className = `btn btn-sm ${action === "delete" ? "btn-danger" : "btn-default"} me-1`; button.dataset.magAction = action; button.dataset.id = item.id; button.dataset.articleKey = item.articleKey; button.dataset.articleTitle = item.title; button.textContent = label; ui.withIcon(button, icon); td.append(button); }
                tr.append(td); body.append(tr);
            });
            ui.refreshDataTable(page.querySelector("[data-admin-datatable]"));
            statusNode.textContent = rows.length ? "" : "مقاله‌ای ثبت نشده است.";
        } catch (error) { statusNode.textContent = error.message || "دریافت مقاله‌ها ناموفق بود."; }
    }
    async function loadServicePages(selectedKey = "") {
        const root = document.querySelector("[data-service-pages]"); if (!root) return;
        const select = root.querySelector("[data-service-page-select]"), statusNode = root.querySelector("[data-service-page-status]"), form = root.querySelector("[data-service-page-editor]");
        try {
            const rows = await ui.getJson(root.dataset.api);
            select.replaceChildren(new Option("صفحه را انتخاب کنید", ""), ...rows.map(item => new Option(`${item.title} (${item.routePath})`, item.pageKey)));
            select.value = selectedKey;
            form.hidden = !selectedKey; statusNode.textContent = selectedKey ? "" : "برای ویرایش یک صفحه را انتخاب کنید.";
        } catch (error) { statusNode.textContent = error.message || "دریافت صفحه‌های خدماتی ناموفق بود."; }
    }
    function renumberEditorSections(container, selector, actionsSelector) {
        const cards = [...container.querySelectorAll(selector)];
        cards.forEach((card, index) => {
            const title = card.querySelector("legend"); if (title) title.textContent = `بخش ${index + 1}`;
            const actions = card.querySelector(actionsSelector); actions?.setAttribute("aria-label", `عملیات بخش ${index + 1}`);
            const moveButtons = [...(actions?.querySelectorAll("button") || [])];
            for (const button of moveButtons) {
                const direction = button.dataset.serviceSectionMove || button.dataset.magSectionMove;
                if (direction === "up") button.disabled = index === 0;
                if (direction === "down") button.disabled = index === cards.length - 1;
            }
        });
    }
    function renderServiceSections(container, sections) {
        container.replaceChildren();
        sections.forEach((section, index) => {
            const card = document.createElement("fieldset"); card.className = "admin-content-section-card p-3 mb-3"; card.dataset.serviceSection = "";
            const legend = document.createElement("legend"); legend.className = "float-none w-auto mb-3 fs-6 fw-semibold"; legend.textContent = `بخش ${index + 1}`;
            const key = document.createElement("input"); key.type = "hidden"; key.value = section.sectionKey || `section-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`; key.dataset.sectionKey = "";
            const headingId = `service-section-heading-${index + 1}`;
            const bodyId = `service-section-body-${index + 1}`;
            const titleLabel = document.createElement("label"); titleLabel.className = "form-label mb-1"; titleLabel.htmlFor = headingId; titleLabel.textContent = "عنوان بخش";
            const heading = document.createElement("input"); heading.id = headingId; heading.className = "form-control mb-3"; heading.maxLength = 300; heading.value = section.heading || ""; heading.dataset.sectionHeading = ""; heading.required = true;
            const bodyLabel = document.createElement("label"); bodyLabel.className = "form-label mb-1"; bodyLabel.htmlFor = bodyId; bodyLabel.textContent = "متن بخش";
            const body = document.createElement("textarea"); body.id = bodyId; body.className = "form-control mb-3"; body.rows = 5; body.maxLength = 20000; body.value = section.body || ""; body.dataset.sectionBody = ""; body.required = true;
            const remove = document.createElement("button"); remove.type = "button"; remove.className = "btn btn-danger btn-sm align-self-start"; remove.dataset.serviceSectionRemove = ""; remove.textContent = "حذف بخش";
            const moveUp = document.createElement("button"); moveUp.type = "button"; moveUp.className = "btn btn-default btn-sm me-1"; moveUp.dataset.serviceSectionMove = "up"; moveUp.textContent = "بالا";
            const moveDown = document.createElement("button"); moveDown.type = "button"; moveDown.className = "btn btn-default btn-sm me-1"; moveDown.dataset.serviceSectionMove = "down"; moveDown.textContent = "پایین";
            const actions = document.createElement("div"); actions.className = "d-flex flex-wrap gap-2 border-top pt-3"; actions.dataset.sectionActions = ""; actions.setAttribute("aria-label", `عملیات بخش ${index + 1}`); actions.append(moveUp, moveDown, remove);
            card.append(legend, key, titleLabel, heading, bodyLabel, body, actions); container.append(card);
        });
        renumberEditorSections(container, "[data-service-section]", "[data-section-actions]");
    }
    function renderMagazineSections(container, sections) {
        container.replaceChildren();
        sections.forEach((section, index) => {
            const card = document.createElement("fieldset"); card.className = "admin-content-section-card p-3 mb-3"; card.dataset.magSection = "";
            const legend = document.createElement("legend"); legend.className = "float-none w-auto mb-3 fs-6 fw-semibold"; legend.textContent = `بخش ${index + 1}`;
            const headingId = `mag-section-heading-${index + 1}`, bodyId = `mag-section-body-${index + 1}`;
            const headingLabel = document.createElement("label"); headingLabel.className = "form-label mb-1"; headingLabel.htmlFor = headingId; headingLabel.textContent = "عنوان بخش";
            const heading = document.createElement("input"); heading.id = headingId; heading.className = "form-control mb-3"; heading.maxLength = 300; heading.value = section.heading || ""; heading.required = true; heading.dataset.magSectionHeading = "";
            const bodyLabel = document.createElement("label"); bodyLabel.className = "form-label mb-1"; bodyLabel.htmlFor = bodyId; bodyLabel.textContent = "متن بخش";
            const body = document.createElement("textarea"); body.id = bodyId; body.className = "form-control mb-3"; body.rows = 5; body.value = section.body || ""; body.required = true; body.dataset.magSectionBody = "";
            const actions = document.createElement("div"); actions.className = "d-flex flex-wrap gap-2 border-top pt-3 admin-content-section-card__actions";
            const up = document.createElement("button"); up.type = "button"; up.className = "btn btn-default btn-sm"; up.dataset.magSectionMove = "up"; up.textContent = "بالا";
            const down = document.createElement("button"); down.type = "button"; down.className = "btn btn-default btn-sm"; down.dataset.magSectionMove = "down"; down.textContent = "پایین";
            const remove = document.createElement("button"); remove.type = "button"; remove.className = "btn btn-danger btn-sm"; remove.dataset.magSectionRemove = ""; remove.textContent = "حذف بخش";
            actions.append(up, down, remove); card.append(legend, headingLabel, heading, bodyLabel, body, actions); container.append(card);
        });
        renumberEditorSections(container, "[data-mag-section]", ".admin-content-section-card__actions");
    }
    document.addEventListener("click", event => {
        const magazineAdd = event.target.closest("[data-mag-section-add]");
        if (magazineAdd) {
            const form = magazineAdd.closest("[data-mag-editor]"), container = form.querySelector("[data-mag-sections]"), sections = [...container.querySelectorAll("[data-mag-section]")].map(row => ({ heading: row.querySelector("[data-mag-section-heading]").value, body: row.querySelector("[data-mag-section-body]").value }));
            sections.push({ heading: "", body: "" }); renderMagazineSections(container, sections); container.lastElementChild?.querySelector("[data-mag-section-heading]")?.focus(); return;
        }
        const magazineRemove = event.target.closest("[data-mag-section-remove]");
        if (magazineRemove) { const container = magazineRemove.closest("[data-mag-sections]"); magazineRemove.closest("[data-mag-section]")?.remove(); renumberEditorSections(container, "[data-mag-section]", ".admin-content-section-card__actions"); return; }
        const magazineMove = event.target.closest("[data-mag-section-move]");
        if (magazineMove) {
            const card = magazineMove.closest("[data-mag-section]"), parent = card.parentElement;
            if (magazineMove.dataset.magSectionMove === "up" && card.previousElementSibling) parent.insertBefore(card, card.previousElementSibling);
            if (magazineMove.dataset.magSectionMove === "down" && card.nextElementSibling) parent.insertBefore(card.nextElementSibling, card);
            renumberEditorSections(parent, "[data-mag-section]", ".admin-content-section-card__actions");
            return;
        }
        const add = event.target.closest("[data-service-section-add]");
        if (add) {
            const form = add.closest("[data-service-page-editor]"), container = form.querySelector("[data-service-sections]"), existing = [...container.querySelectorAll("[data-service-section]")].map(row => ({ sectionKey: row.querySelector("[data-section-key]").value, heading: row.querySelector("[data-section-heading]").value, body: row.querySelector("[data-section-body]").value }));
            existing.push({ sectionKey: `section-${Date.now()}`, heading: "", body: "" }); renderServiceSections(container, existing); return;
        }
        const remove = event.target.closest("[data-service-section-remove]");
        if (remove) { const container = remove.closest("[data-service-sections]"); remove.closest("[data-service-section]")?.remove(); renumberEditorSections(container, "[data-service-section]", "[data-section-actions]"); }
        const move = event.target.closest("[data-service-section-move]");
        if (move) {
            const card = move.closest("[data-service-section]"), parent = card.parentElement;
            if (move.dataset.serviceSectionMove === "up" && card.previousElementSibling) parent.insertBefore(card, card.previousElementSibling);
            if (move.dataset.serviceSectionMove === "down" && card.nextElementSibling) parent.insertBefore(card.nextElementSibling, card);
            renumberEditorSections(parent, "[data-service-section]", "[data-section-actions]");
        }
    });
    document.addEventListener("change", async event => {
        const select = event.target.closest("[data-service-page-select]"); if (!select) return;
        const root = select.closest("[data-service-pages]"), form = root.querySelector("[data-service-page-editor]"), statusNode = root.querySelector("[data-service-page-status]"), key = select.value;
        if (!key) { form.hidden = true; statusNode.textContent = "برای ویرایش یک صفحه را انتخاب کنید."; return; }
        select.disabled = true; form.hidden = true; statusNode.textContent = "در حال دریافت متن صفحه…";
        try {
            const page = await ui.getJson(`${root.dataset.api}/${encodeURIComponent(key)}`);
            form.elements.title.value = page.title; form.elements.description.value = page.description || "";
            renderServiceSections(form.querySelector("[data-service-sections]"), page.sections);
            form.elements.isPublished.checked = page.isPublished; form.hidden = false; statusNode.textContent = "";
        } catch (error) { form.hidden = true; statusNode.textContent = error.message || "دریافت متن صفحه ناموفق بود."; }
        finally { select.disabled = false; }
    });
    document.addEventListener("submit", event => {
        const form = event.target.closest("[data-service-page-editor]"); if (!form) return;
        event.preventDefault(); const root = form.closest("[data-service-pages]"), select = root.querySelector("[data-service-page-select]"), button = form.querySelector('[type="submit"]');
        const sectionRows = [...form.querySelectorAll("[data-service-section]")];
        const sections = sectionRows.map(row => ({ sectionKey: row.querySelector("[data-section-key]").value, heading: row.querySelector("[data-section-heading]").value, body: row.querySelector("[data-section-body]").value }));
        if (!sections.length) { ui.notify("هر صفحه باید دست‌کم یک بخش داشته باشد.", "danger"); return; }
        const pageKey = select.value;
        const body = { title: form.elements.title.value, description: form.elements.description.value, isPublished: form.elements.isPublished.checked, sections };
        const token = form.querySelector('[name="__RequestVerificationToken"]')?.value || ""; button.disabled = true;
        void ui.getJson(`${root.dataset.api}/${encodeURIComponent(pageKey)}`, { method: "PUT", headers: { "Content-Type": "application/json", RequestVerificationToken: token }, body: JSON.stringify(body) })
            .then(result => { ui.notify(result.message, "success"); return loadServicePages(pageKey).then(() => select.dispatchEvent(new Event("change", { bubbles: true }))); })
            .catch(error => ui.notify(error.message || "ذخیرهٔ صفحه ناموفق بود.", "danger")).finally(() => { button.disabled = false; });
    });
    document.addEventListener("click", async event => {
        const button = event.target.closest("[data-mag-create], [data-mag-action]"); if (!button) return;
        const form = document.querySelector("[data-mag-editor]");
        if (button.dataset.magAction === "image") {
            const mediaForm = document.querySelector("[data-article-image-form]");
            if (!mediaForm) return;
            mediaForm.reset(); mediaForm.elements.articleKey.value = button.dataset.articleKey || "";
            mediaForm.querySelector("[data-article-image-name]").textContent = button.dataset.articleTitle || "";
            window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("article-image-modal")).show();
            return;
        }
        if (button.hasAttribute("data-mag-create")) { form.reset(); form.elements.id.value = ""; renderMagazineSections(form.querySelector("[data-mag-sections]"), []); window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("mag-editor-modal")).show(); return; }
        if (button.dataset.magAction === "delete") {
            if (!window.confirm("مقاله حذف شود؟")) return;
            const token = document.querySelector("[data-mag-editor] [name=__RequestVerificationToken]")?.value || "";
            try { const result = await ui.getJson(`/api/admin/store/magazine/${button.dataset.id}`, { method: "DELETE", headers: { RequestVerificationToken: token } }); ui.notify(result.message, "success"); await loadMagazine(); }
            catch (error) { ui.notify(error.message || "حذف مقاله ناموفق بود.", "danger"); } return;
        }
        try {
            const item = await ui.getJson(`/api/admin/store/magazine/${button.dataset.id}`); form.reset(); form.elements.id.value = button.dataset.id;
            for (const name of ["articleKey","title","category","summary","author","kicker","branch","topic","area","heroImagePath","contentImagePath","readTimeMinutes","readTimeText","lead","conclusion"]) form.elements[name].value = item[name] || "";
            renderMagazineSections(form.querySelector("[data-mag-sections]"), item.sections || []); form.elements.isPublished.checked = item.isPublished;
            window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("mag-editor-modal")).show();
        } catch (error) { ui.notify(error.message || "دریافت مقاله ناموفق بود.", "danger"); }
    });
    document.addEventListener("submit", event => {
        const form = event.target.closest("[data-mag-editor]"); if (!form) return;
        event.preventDefault(); const button = form.querySelector('[type="submit"]'); if (button.disabled) return;
        const sectionRows = [...form.querySelectorAll("[data-mag-section]")];
        const sections = sectionRows.map(row => ({ heading: row.querySelector("[data-mag-section-heading]").value.trim(), body: row.querySelector("[data-mag-section-body]").value.trim() }));
        if (sections.some(section => !section.heading || !section.body)) { ui.notify("عنوان و متن همهٔ بخش‌های مقاله را تکمیل کنید.", "danger"); return; }
        const body = {}; for (const name of ["articleKey","title","category","summary","author","kicker","branch","topic","area","heroImagePath","contentImagePath","readTimeText","lead","conclusion"]) body[name] = form.elements[name].value;
        body.readTimeMinutes = form.elements.readTimeMinutes.value ? Number(form.elements.readTimeMinutes.value) : null; body.isPublished = form.elements.isPublished.checked; body.sections = sections;
        button.disabled = true; const id = form.elements.id.value, token = form.querySelector('[name="__RequestVerificationToken"]')?.value || "";
        void ui.getJson(`/api/admin/store/magazine${id ? `/${id}` : ""}`, { method: id ? "PUT" : "POST", headers: { "Content-Type": "application/json", RequestVerificationToken: token }, body: JSON.stringify(body) })
            .then(result => { ui.notify(result.message, "success"); window.bootstrap?.Modal?.getInstance(document.getElementById("mag-editor-modal"))?.hide(); return loadMagazine(); })
            .catch(error => ui.notify(error.message || "ذخیره مقاله ناموفق بود.", "danger")).finally(() => { button.disabled = false; });
    });
    document.addEventListener("DOMContentLoaded", () => void loadMagazine());

    function refresh(page, announce = true) {
        if (page.dataset.adminPage === "catalog") return loadCatalog(page, Number(page.dataset.adminPageNumber || "1"), announce);
        if (page.dataset.adminPage === "warehouses") return loadWarehouses(page, announce);
        if (page.dataset.adminPage === "stock") return loadStock(page, Number(page.dataset.adminPageNumber || "1"), announce);
        if (page.dataset.adminPage === "customers") return loadCustomers(page, Number(page.dataset.adminPageNumber || "1"), announce);
        if (page.dataset.adminPage === "orders") return loadOrders(page, Number(page.dataset.adminPageNumber || "1"), announce);
    }

    function initialize(root = document) {
        root.querySelectorAll("[data-media-library]").forEach(page => {
            if (page.dataset.mediaLibraryInitialized === "true") return;
            page.dataset.mediaLibraryInitialized = "true";
            void loadMediaLibrary(page);
        });
        const pages = root.querySelectorAll("[data-admin-page]");
        pages.forEach(page => {
            if (page.dataset.adminInitialized === "true") return;
            page.dataset.adminInitialized = "true";
            if (page.dataset.adminPage === "catalog") void loadCatalog(page, 1, false);
            else if (page.dataset.adminPage === "warehouses") void loadWarehouses(page, false);
            else if (page.dataset.adminPage === "stock") void loadStock(page, 1, false);
            else if (page.dataset.adminPage === "orders") void loadOrders(page, 1, false);
            else if (page.dataset.adminPage === "customers") void loadCustomers(page, 1, false);
            else if (page.dataset.adminPage === "faq") { void loadFaq(page); void loadMagazine(); void loadServicePages(); }
            else if (page.dataset.adminPage === "product-drafts") void loadProductDrafts(page);
        });
    }

    document.addEventListener("click", event => {
        const categoryEdit = event.target.closest("[data-category-edit], [data-category-create]");
        if (categoryEdit) {
            const page = categoryEdit.closest('[data-admin-section="categories"]'), form = document.querySelector("[data-category-editor]");
            const item = categoryEdit.hasAttribute("data-category-edit") ? page?.__adminRows?.[Number(categoryEdit.dataset.categoryEdit)] : null;
            if (!page || !form || categoryEdit.disabled || (categoryEdit.hasAttribute("data-category-edit") && !item)) return;
            categoryEdit.disabled = true;
            void ui.getJson("/api/admin/store/catalog/categories/manage/options").then(nodes => {
                if (!page.isConnected || !form.isConnected) return;
                const category = item ? nodes.find(node => node.categoryKey === item.id) : null;
                if (item && !category) throw new Error("دسته دیگر در دسترس نیست؛ فهرست را به‌روز کنید.");
                form.reset(); form.elements.originalKey.value = category?.categoryKey || "";
                form.elements.key.value = category?.categoryKey || ""; form.elements.key.readOnly = !!category;
                form.elements.nameFa.value = category?.nameFa || ""; form.elements.nameEn.value = category?.nameEn || "";
                form.elements.parentId.replaceChildren(new Option("بدون والد", ""), ...nodes.map(node => new Option(JSON.parse(node.pathJson).join(" / "), String(node.id))));
                form.elements.parentId.value = category?.idParentCategory == null ? "" : String(category.idParentCategory);
                form.elements.parentId.disabled = !!category;
                window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("category-editor-modal")).show();
            }).catch(error => ui.notify(error.message || "دریافت دسته‌ها ناموفق بود.", "danger"))
                .finally(() => { categoryEdit.disabled = false; });
            return;
        }
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
        const unpublish = event.target.closest("[data-product-unpublish]");
        if (unpublish) {
            const page = unpublish.closest('[data-admin-section="products"]');
            const item = page?.__adminRows?.[Number(unpublish.dataset.productUnpublish)];
            if (!item || unpublish.disabled || !window.confirm(`کالای ${item.name} از انتشار خارج شود؟`)) return;
            const token = document.querySelector('[data-product-create-form] [name="__RequestVerificationToken"]')?.value || "";
            unpublish.disabled = true;
            void ui.getJson(`/api/admin/store/catalog/products/manage/${encodeURIComponent(item.id)}/unpublish`, { method: "POST", headers: { RequestVerificationToken: token } })
                .then(result => { ui.notify(result.message, "success"); refreshProductDrafts(); void loadCatalog(page, 1); })
                .catch(error => { ui.notify(error.message || "خروج از انتشار ناموفق بود.", "danger"); unpublish.disabled = false; });
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
            form.elements.email.value = item.email;
            form.elements.firstName.value = item.firstName; form.elements.lastName.value = item.lastName;
            form.elements.phone.value = item.phone || "";
            form.__expected = { expectedEmail: item.email, expectedFirstName: item.firstName, expectedLastName: item.lastName, expectedPhone: item.phone };
            document.getElementById("customer-editor-title").textContent = "ویرایش مشتری";
            window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("customer-editor-modal")).show();
            return;
        }
        const customerCreate = event.target.closest("[data-customer-create]");
        if (customerCreate) {
            const form = document.querySelector("[data-customer-editor]");
            if (!form) return;
            form.reset(); form.elements.id.value = ""; form.__expected = null;
            document.getElementById("customer-editor-title").textContent = "ایجاد مشتری";
            window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("customer-editor-modal")).show();
            return;
        }
        const customerDelete = event.target.closest("[data-customer-delete]");
        if (customerDelete) {
            const page = customerDelete.closest('[data-admin-page="customers"]');
            const item = page?.__adminRows?.[Number(customerDelete.dataset.customerDelete)];
            if (!item || !window.confirm(`پروفایل ${item.email} حذف شود؟ اگر سفارش یا سبد داشته باشد، سرور حذف را رد می‌کند.`)) return;
            const form = document.querySelector("[data-customer-editor]"), token = form?.querySelector('[name="__RequestVerificationToken"]')?.value || "";
            customerDelete.disabled = true;
            void ui.getJson(`${page.dataset.adminApi}/${encodeURIComponent(item.id)}`, { method: "DELETE", headers: { RequestVerificationToken: token } })
                .then(result => { ui.notify(result.message, "success"); return loadCustomers(page, Number(page.dataset.adminPageNumber || 1)); })
                .catch(error => { ui.notify(error.message || "حذف مشتری ناموفق بود.", "danger"); customerDelete.disabled = false; });
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
        const faqDelete = event.target.closest("[data-faq-delete]");
        if (faqDelete) {
            const page = faqDelete.closest('[data-admin-page="faq"]'), item = page?.__faqRows?.[Number(faqDelete.dataset.faqDelete)];
            if (!page || !item || !window.confirm("این پرسش متداول حذف شود؟")) return;
            const token = document.querySelector("[data-faq-editor] [name=__RequestVerificationToken]")?.value || ""; faqDelete.disabled = true;
            void ui.getJson(`${page.dataset.adminApi}/${encodeURIComponent(item.id)}`, { method: "DELETE", headers: { RequestVerificationToken: token } })
                .then(result => { ui.notify(result.message, "success"); return loadFaq(page); })
                .catch(error => { faqDelete.disabled = false; ui.notify(error.message || "حذف پرسش ناموفق بود.", "danger"); });
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
        const attributeAction = event.target.closest("[data-attribute-edit], [data-attribute-create]");
        if (attributeAction) {
            const page = attributeAction.closest('[data-admin-section="attributes"]');
            const form = document.querySelector("[data-attribute-editor]");
            const item = attributeAction.hasAttribute("data-attribute-edit") ? page?.__adminRows?.[Number(attributeAction.dataset.attributeEdit)] : null;
            if (!page || !form || attributeAction.disabled || (attributeAction.hasAttribute("data-attribute-edit") && !item)) return;
            attributeAction.disabled = true;
            const load = item
                ? ui.getJson(`/api/admin/store/catalog/attributes/manage/${encodeURIComponent(item.key)}`)
                : Promise.resolve(null);
            void load.then(data => {
                if (!page.isConnected || !form.isConnected) return;
                const attribute = data || {};
                form.reset();
                form.elements.originalKey.value = attribute.key || "";
                form.elements.key.value = attribute.key || "";
                form.elements.key.readOnly = !!item;
                form.elements.label.value = attribute.label || "";
                form.elements.icon.value = attribute.icon || "";
                form.elements.sourceKey.value = attribute.sourceKey || "";
                form.elements.filterKey.value = attribute.filterKey || "";
                form.elements.isFilterable.checked = !!attribute.isFilterable;
                form.elements.isHighlight.checked = !!attribute.isHighlight;
                window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("attribute-editor-modal")).show();
            }).catch(error => ui.notify(error.message || "دریافت ویژگی ناموفق بود.", "danger"))
                .finally(() => { attributeAction.disabled = false; });
            return;
        }
        const catalogStatus = event.target.closest("[data-catalog-status]");
        if (catalogStatus) {
            const page = catalogStatus.closest('[data-admin-page="catalog"]');
            const section = page?.dataset.adminSection;
            const item = page?.__adminRows?.[Number(catalogStatus.dataset.catalogStatus)];
            if (!page || !item || !["categories", "brands", "attributes"].includes(section) || catalogStatus.disabled) return;
            const nextActive = !item.isActive;
            const typeName = section === "categories" ? "دسته" : section === "brands" ? "برند" : "ویژگی";
            if (!nextActive && !window.confirm(`${typeName} «${item.name || item.label}» از کاتالوگ خارج شود؟`)) return;
            const editorSelector = section === "categories" ? "[data-category-editor]" : section === "brands" ? "[data-brand-editor]" : "[data-attribute-editor]";
            const token = document.querySelector(`${editorSelector} [name="__RequestVerificationToken"]`)?.value || "";
            const key = section === "attributes" ? item.key : item.id;
            catalogStatus.disabled = true;
            void ui.getJson(`/api/admin/store/catalog/${section}/manage/${encodeURIComponent(key)}/status`, {
                method: "POST",
                headers: { "Content-Type": "application/json", RequestVerificationToken: token },
                body: JSON.stringify({ isActive: nextActive })
            }).then(result => {
                ui.notify(result.message, "success");
                return loadCatalog(page, Number(page.dataset.adminPageNumber || 1));
            }).catch(error => {
                ui.notify(error.message || "تغییر وضعیت انتشار ناموفق بود.", "danger");
                catalogStatus.disabled = false;
            });
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
        const institutionButton = event.target.closest("[data-payment-institution-image]");
        if (institutionButton) {
            const page = institutionButton.closest('[data-admin-section="payment-institutions"]');
            const item = page?.__adminRows?.[Number(institutionButton.dataset.paymentInstitutionImage)];
            const form = document.querySelector("[data-payment-institution-image-form]");
            if (!item || !form) return;
            form.reset(); form.elements.paymentInstitutionCode.value = item.code;
            form.querySelector("[data-payment-institution-name]").textContent = item.nameFa;
            window.bootstrap?.Modal?.getOrCreateInstance(document.getElementById("payment-institution-image-modal")).show();
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
        const treeButton = event.target.closest("[data-category-tree-expand], [data-category-tree-collapse]");
        if (treeButton) {
            const page = treeButton.closest('[data-admin-section="categories"]');
            const rows = page?.__adminCategoryData;
            if (!page || !rows) return;
            if (treeButton.hasAttribute("data-category-tree-expand")) {
                const parentIds = new Set(rows.map(item => String(item.parentId || "")).filter(Boolean));
                page.__adminCategoryExpanded = new Set(rows.filter(item => parentIds.has(String(item.id))).map(item => String(item.id)));
            } else {
                page.__adminCategoryExpanded = new Set();
            }
            renderCategoryTree(page, rows, queryValue(page.querySelector("[data-admin-filter]"), "q"));
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
                        "اقلام": items.map(line => `${line.name}، ${ui.formatNumber(line.quantity)} عدد، مبلغ واحد ${ui.formatNumber(line.unitAmount)} تومان، طرح ${line.salesPlan}${line.priceReferences?.length ? `، نسخه‌های قیمت: ${line.priceReferences.map(ref => `#${ref.priceVersionId} ${ref.component}=${ui.formatNumber(ref.calculatedUnitAmount)} تومان`).join("، ")}` : "، مرجع نسخهٔ قیمت ثبت نشده"}`).join(" ؛ ") || "جزئیات اقلام ثبت نشده است",
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
                document.querySelector("[data-minimum-product]").textContent = `${item.productName}${item.variantName ? ` — ${item.variantName}` : ""} · ${item.companyName} / ${item.warehouseName} (${item.warehouseCode}) · ${item.salesPlanName}`;
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
            else if (page?.dataset.adminPage === "product-drafts") void loadProductDrafts(page, nextPage);
        }
    });

    document.addEventListener("submit", event => {
        const categoryEditor = event.target.closest("[data-category-editor]");
        if (categoryEditor) {
            event.preventDefault();
            const save = categoryEditor.querySelector('[type="submit"]'); if (save.disabled) return;
            const original = categoryEditor.elements.originalKey.value, token = categoryEditor.querySelector('[name="__RequestVerificationToken"]')?.value || "";
            const parentId = categoryEditor.elements.parentId.value;
            const body = { key: categoryEditor.elements.key.value, nameFa: categoryEditor.elements.nameFa.value, nameEn: categoryEditor.elements.nameEn.value, parentId: parentId ? Number(parentId) : null };
            save.disabled = true;
            void ui.getJson(`/api/admin/store/catalog/categories/manage${original ? `/${encodeURIComponent(original)}` : ""}`, { method: original ? "PUT" : "POST", headers: { "Content-Type": "application/json", RequestVerificationToken: token }, body: JSON.stringify(body) })
                .then(result => { ui.notify(result.message, "success"); window.bootstrap?.Modal?.getInstance(document.getElementById("category-editor-modal"))?.hide(); const page = document.querySelector('[data-admin-section="categories"]'); if (page) return loadCatalog(page, Number(page.dataset.adminPageNumber || 1)); })
                .catch(error => ui.notify(error.message || "ذخیرهٔ دسته ناموفق بود.", "danger"))
                .finally(() => { save.disabled = false; });
            return;
        }
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
            const id = customerForm.elements.id.value;
            if (button.disabled || id && !customerForm.__expected) return;
            const token = customerForm.querySelector('[name="__RequestVerificationToken"]')?.value || "";
            const body = { ...(customerForm.__expected || {}), email: customerForm.elements.email.value, firstName: customerForm.elements.firstName.value, lastName: customerForm.elements.lastName.value, phone: customerForm.elements.phone.value };
            button.disabled = true;
            void ui.getJson(`/api/admin/store/customers${id ? `/${encodeURIComponent(id)}` : ""}`, { method: id ? "PUT" : "POST", headers: { "Content-Type": "application/json", RequestVerificationToken: token }, body: JSON.stringify(body) })
                .then(result => { ui.notify(result.message, "success"); window.bootstrap?.Modal?.getInstance(document.getElementById("customer-editor-modal"))?.hide(); const page = document.querySelector('[data-admin-page="customers"]'); if (page) return loadCustomers(page, id ? Number(page.dataset.adminPageNumber || 1) : 1); })
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
        const attributeEditor = event.target.closest("[data-attribute-editor]");
        if (attributeEditor) {
            event.preventDefault();
            const save = attributeEditor.querySelector('[type="submit"]');
            if (save.disabled) return;
            const original = attributeEditor.elements.originalKey.value;
            const token = attributeEditor.querySelector('[name="__RequestVerificationToken"]')?.value || "";
            const body = {
                key: attributeEditor.elements.key.value,
                label: attributeEditor.elements.label.value,
                icon: attributeEditor.elements.icon.value,
                sourceKey: attributeEditor.elements.sourceKey.value,
                filterKey: attributeEditor.elements.filterKey.value,
                isFilterable: attributeEditor.elements.isFilterable.checked,
                isHighlight: attributeEditor.elements.isHighlight.checked
            };
            save.disabled = true;
            void ui.getJson(`/api/admin/store/catalog/attributes/manage${original ? `/${encodeURIComponent(original)}` : ""}`, {
                method: original ? "PUT" : "POST",
                headers: { "Content-Type": "application/json", RequestVerificationToken: token },
                body: JSON.stringify(body)
            }).then(result => {
                ui.notify(result.message, "success");
                window.bootstrap?.Modal?.getInstance(document.getElementById("attribute-editor-modal"))?.hide();
                const page = document.querySelector('[data-admin-section="attributes"]');
                if (page) return loadCatalog(page, Number(page.dataset.adminPageNumber || 1));
            }).catch(error => ui.notify(error.message || "ذخیرهٔ ویژگی ناموفق بود.", "danger"))
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
        const institutionForm = event.target.closest("[data-payment-institution-image-form]");
        if (institutionForm) {
            event.preventDefault();
            const save = institutionForm.querySelector('[type="submit"]'); save.disabled = true;
            void ui.getJson("/api/admin/store/media", { method: "POST", body: new FormData(institutionForm) })
                .then(result => { ui.notify(result.message, "success"); window.bootstrap?.Modal?.getInstance(document.getElementById("payment-institution-image-modal"))?.hide(); const page = document.querySelector('[data-admin-section="payment-institutions"]'); if (page) return loadCatalog(page, 1); })
                .catch(error => ui.notify(error.message || "بارگذاری لوگو ناموفق بود.", "danger"))
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
            if (mediaForm.elements.kind.value === "country" && !mediaForm.elements.countryCode.value) {
                result.textContent = "کشور را انتخاب کنید."; return;
            }
            if (mediaForm.elements.kind.value === "article" && !mediaForm.elements.articleKey.value) {
                result.textContent = "مقاله را انتخاب کنید."; return;
            }
            result.textContent = "در حال بارگذاری…"; save.disabled = true;
            void ui.getJson("/api/admin/store/media", { method: "POST", body: new FormData(mediaForm) })
                .then(data => {
                    const display = (url, path) => url || path;
                    const fileUrl = display(data.url, data.path);
                    const thumbnailUrl = display(data.thumbnailUrl, data.thumbnail);
                    result.textContent = data.thumbnail ? `نشانی فایل: ${fileUrl} — تصویر کوچک: ${thumbnailUrl}` : `نشانی فایل: ${fileUrl}`;
                    ui.notify(data.message, "success");
                    if (mediaForm.elements.kind.value === "country")
                        document.dispatchEvent(new CustomEvent("admin:media-uploaded", { detail: { kind: "country", countryCode: mediaForm.elements.countryCode.value, path: data.path } }));
                    if (mediaForm.hasAttribute("data-article-image-form")) {
                        window.bootstrap?.Modal?.getInstance(document.getElementById("article-image-modal"))?.hide();
                        void loadMagazine();
                    }
                    const library = mediaForm.closest("[data-media-library]");
                    if (library) void loadMediaLibrary(library);
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
        else if (page?.dataset.adminPage === "product-drafts") void loadProductDrafts(page, 1);
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
        else if (page?.dataset.adminPage === "product-drafts") void loadProductDrafts(page, 1);
        else if (page?.dataset.adminPage === "warehouses") void loadWarehouses(page);
        else if (page?.dataset.adminPage === "stock") void loadStock(page, 1);
    });

    window.AdminPages = Object.freeze({ initialize });
})();
