(() => {
    "use strict";

    const ui = window.AdminUI;
    const initialized = new WeakSet();

    function initialize(root) {
        const page = root.querySelector?.("[data-admin-home-testimonials]");
        if (!page || initialized.has(page) || !ui) return;
        initialized.add(page);
        let requestId = 0;
        let editingId = null;
        const editor = page.querySelector("[data-home-testimonial-editor]");
        let productsReady = Promise.resolve();
        const token = () => page.querySelector('[name="__RequestVerificationToken"]')?.value || "";
        const endpoint = page.dataset.api;
        const status = (message, error = false) => {
            ui.setStatus(page.querySelector("[data-home-testimonial-status]"), message, error);
        };
        const request = (url, method = "GET", body, headers = {}) => ui.getJson(url, {
            method, headers: { RequestVerificationToken: token(), ...headers }, ...(body ? { body } : {})
        });

        function render(items) {
            const body = page.querySelector("[data-home-testimonial-rows]");
            body.replaceChildren();
            for (const item of items) {
                const row = document.createElement("tr");
                const activeCell = document.createElement("td");
                const active = document.createElement("input");
                active.type = "checkbox"; active.className = "form-check-input"; ui.initStatusSwitch(active); active.checked = item.isActive;
                active.setAttribute("aria-label", `نمایش دیدگاه ${item.displayName} در صفحهٔ نخست`);
                activeCell.append(active); row.append(activeCell);

                const orderCell = document.createElement("td");
                const order = document.createElement("input"); order.type = "number"; order.min = "0"; order.max = "9999";
                order.value = String(item.sortOrder || 0); order.className = "form-control form-control-sm home-testimonial-order";
                order.setAttribute("aria-label", "ترتیب نمایش"); orderCell.append(order); row.append(orderCell);

                const imageCell = document.createElement("td");
                if (item.imageUrl) {
                    const image = document.createElement("img"); image.src = item.imageUrl; image.alt = `تصویر ${item.displayName}`;
                    image.loading = "lazy"; image.className = "home-testimonial-preview"; imageCell.append(image);
                } else imageCell.textContent = "بدون تصویر";
                row.append(imageCell);

                const reviewCell = document.createElement("td");
                const name = document.createElement("strong"); name.textContent = `${item.displayName} · ${new Intl.NumberFormat("fa-IR").format(item.rating || 0)} از ۵${item.isSample ? " · نمونه" : ""}`;
                const text = document.createElement("p"); text.className = "small text-muted mb-0 home-testimonial-text"; text.textContent = item.feedbackText;
                reviewCell.append(name, text); row.append(reviewCell);
                row.append(cell(`${item.productName} (${item.productKey})`));
                row.append(cell(window.AdminUI.formatDateTime(item.createdAtUtc)));

                const actions = document.createElement("td");
                const upload = document.createElement("input"); upload.type = "file"; upload.accept = "image/jpeg,image/png,image/webp";
                upload.className = "visually-hidden";
                const uploadButton = button(item.imageUrl ? "تعویض تصویر" : "بارگذاری تصویر", "btn btn-sm btn-default");
                uploadButton.dataset.adminConfirm = "تصویر این دیدگاه بارگذاری یا جایگزین شود؟";
                uploadButton.addEventListener("click", () => upload.click());
                upload.addEventListener("change", async () => {
                    if (!upload.files?.length) return;
                    const data = new FormData(); data.append("file", upload.files[0]);
                    try { await request(`${endpoint.split("?")[0]}/${item.id}/image?feature=testimonials`, "POST", data); await load(); }
                    catch (error) { ui.notify(error.message || "بارگذاری تصویر ناموفق بود.", "danger"); }
                });
                const save = button("ذخیره", "btn btn-sm btn-primary");
                save.dataset.adminConfirm = "تنظیم نمایش و ترتیب این دیدگاه ذخیره شود؟";
                save.addEventListener("click", async () => {
                    save.disabled = true;
                    try {
                        const result = await request(`${endpoint.split("?")[0]}/${item.id}?feature=testimonials`, "PUT",
                            JSON.stringify({ isActive: active.checked, sortOrder: Number(order.value) || 0 }), { "Content-Type": "application/json" });
                        ui.notify(result.message, "success"); await load();
                    } catch (error) { ui.notify(error.message || "ذخیره تنظیمات دیدگاه انجام نشد.", "danger"); save.disabled = false; }
                });
                actions.append(upload, uploadButton);
                if (editor) {
                    const edit = button("ویرایش", "btn btn-sm btn-default");
                    edit.addEventListener("click", async () => {
                        try { await productsReady; } catch { return; }
                        editingId = item.id;
                        editor.elements.productId.value = String(item.idProduct);
                        editor.elements.displayName.value = item.displayName;
                        editor.elements.feedbackText.value = item.feedbackText;
                        editor.elements.rating.value = String(item.rating || 1);
                        editor.elements.sortOrder.value = String(item.sortOrder || 0);
                        editor.elements.isActive.checked = item.isActive;
                        editor.querySelector("[data-home-testimonial-submit]").textContent = "ذخیره ویرایش";
                        editor.scrollIntoView({ behavior: "smooth", block: "start" });
                        editor.elements.productId.focus();
                    });
                    actions.append(edit);
                }
                if (item.imageUrl) {
                    const remove = button("حذف تصویر", "btn btn-sm btn-danger");
                    remove.dataset.adminConfirm = "تصویر این دیدگاه از FTP حذف شود؟";
                    remove.addEventListener("click", async () => {
                        try { const result = await request(`${endpoint.split("?")[0]}/${item.id}/image?feature=testimonials`, "DELETE"); ui.notify(result.message, "success"); await load(); }
                        catch (error) { ui.notify(error.message || "حذف تصویر ناموفق بود.", "danger"); }
                    });
                    actions.append(remove);
                }
                actions.append(save); row.append(actions); body.append(row);
            }
        }

        function cell(value) { const node = document.createElement("td"); node.textContent = value ?? ""; return node; }
        function button(value, className) { const node = document.createElement("button"); node.type = "button"; node.className = className; node.textContent = value; return node; }

        async function load() {
            const current = ++requestId;
            status("در حال دریافت دیدگاه‌ها…");
            try {
                const result = await request(endpoint);
                if (current !== requestId) return;
                render(result.items || []);
                status(result.items?.length ? "" : "دیدگاه تأییدشده‌ای پیدا نشد.");
            } catch (error) { if (current === requestId) status(error.message || "دریافت دیدگاه‌ها ناموفق بود.", true); }
        }

        if (editor) {
            const submit = editor.querySelector("[data-home-testimonial-submit]");
            submit.disabled = true;
            productsReady = request(`${endpoint.split("?")[0]}/products?feature=testimonials`).then(products => {
                for (const product of products) {
                    const option = document.createElement("option"); option.value = String(product.id);
                    option.textContent = `${product.name} (${product.productKey})${product.isStoreEnabled ? "" : " - غیرفعال در فروشگاه"}`;
                    editor.elements.productId.append(option);
                }
                submit.disabled = false;
            }).catch(error => { ui.notify(error.message || "دریافت محصولات ناموفق بود.", "danger"); throw error; });
            productsReady.catch(() => {});
            editor.addEventListener("reset", () => { editingId = null; editor.querySelector("[data-home-testimonial-submit]").textContent = "افزودن دیدگاه"; });
            editor.addEventListener("submit", async event => {
                event.preventDefault();
                const submit = editor.querySelector("[data-home-testimonial-submit]");
                submit.disabled = true;
                const payload = { productId: Number(editor.elements.productId.value), displayName: editor.elements.displayName.value,
                    feedbackText: editor.elements.feedbackText.value, rating: Number(editor.elements.rating.value),
                    sortOrder: Number(editor.elements.sortOrder.value), isActive: editor.elements.isActive.checked };
                try {
                    const url = editingId === null ? endpoint : `${endpoint.split("?")[0]}/${editingId}/review?feature=testimonials`;
                    const result = await request(url, editingId === null ? "POST" : "PUT", JSON.stringify(payload), { "Content-Type": "application/json" });
                    ui.notify(result.message, "success"); editor.reset(); await load();
                } catch (error) { ui.notify(error.message || "ذخیره دیدگاه ناموفق بود.", "danger"); }
                finally { submit.disabled = false; }
            });
        }
        void load();
    }

    window.AdminHomeTestimonials = Object.freeze({ initialize });
})();
