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
                row.append(cell(item.isActive ? "فعال" : "غیرفعال"));

                const imageCell = document.createElement("td");
                if (item.imageUrl) {
                    const image = document.createElement("img"); image.src = item.imageUrl; image.alt = `تصویر ${item.displayName}`;
                    image.loading = "lazy"; image.className = "home-testimonial-preview"; imageCell.append(image);
                } else imageCell.textContent = "بدون تصویر";
                row.append(imageCell);

                const reviewCell = document.createElement("td");
                reviewCell.className = "home-testimonial-review";
                const name = document.createElement("strong"); name.textContent = `${item.displayName} · ${new Intl.NumberFormat("fa-IR").format(item.rating || 0)} از ۵${item.isSample ? " · نمونه" : ""}`;
                const text = document.createElement("p"); text.className = "small text-muted mb-0 home-testimonial-text";
                const characters = Array.from(item.feedbackText || "");
                text.textContent = characters.length > 140 ? `${characters.slice(0, 140).join("")}…` : characters.join("");
                reviewCell.append(name, text);
                if (characters.length > 140) {
                    const details = document.createElement("details");
                    const summary = document.createElement("summary"); summary.textContent = "نمایش متن کامل";
                    const fullText = document.createElement("p"); fullText.className = "small mb-0 home-testimonial-text"; fullText.textContent = item.feedbackText;
                    details.append(summary, fullText); reviewCell.append(details);
                }
                row.append(reviewCell);
                row.append(cell(`${item.productName} (${item.productKey})`));
                row.append(cell(window.AdminUI.formatDateTime(item.createdAtUtc)));

                const actions = document.createElement("td");
                const actionGroup = document.createElement("div"); actionGroup.className = "home-testimonial-actions";
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
                        editor.elements.image.value = "";
                        editor.elements.removeImage.checked = false;
                        editor.querySelector("[data-home-testimonial-remove-image]").hidden = !item.imageUrl;
                        editor.querySelector("[data-home-testimonial-submit]").textContent = "ذخیره ویرایش";
                        editor.scrollIntoView({ behavior: "smooth", block: "start" });
                        editor.elements.productId.focus();
                    });
                    actionGroup.append(edit);
                }
                actions.append(actionGroup); row.append(actions); body.append(row);
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
            editor.addEventListener("reset", () => { editingId = null; editor.querySelector("[data-home-testimonial-submit]").textContent = "افزودن دیدگاه"; editor.querySelector("[data-home-testimonial-remove-image]").hidden = true; });
            editor.addEventListener("submit", async event => {
                event.preventDefault();
                const submit = editor.querySelector("[data-home-testimonial-submit]");
                submit.disabled = true;
                const payload = { productId: Number(editor.elements.productId.value), displayName: editor.elements.displayName.value,
                    feedbackText: editor.elements.feedbackText.value, rating: Number(editor.elements.rating.value),
                    sortOrder: Number(editor.elements.sortOrder.value), isActive: editor.elements.isActive.checked };
                const image = editor.elements.image.files?.[0];
                if (image && editor.elements.removeImage.checked) {
                    ui.notify("برای حذف یا جایگزینی تصویر، فقط یکی را انتخاب کنید.", "danger"); submit.disabled = false; return;
                }
                if (image && image.size > 10000000) {
                    ui.notify("حجم تصویر باید حداکثر ۱۰ مگابایت باشد.", "danger"); submit.disabled = false; return;
                }
                try {
                    const url = editingId === null ? endpoint : `${endpoint.split("?")[0]}/${editingId}/review?feature=testimonials`;
                    const result = await request(url, editingId === null ? "POST" : "PUT", JSON.stringify(payload), { "Content-Type": "application/json" });
                    if (editingId === null) editingId = result.id;
                    if (image) {
                        const data = new FormData(); data.append("file", image);
                        try { await request(`${endpoint.split("?")[0]}/${editingId}/image?feature=testimonials`, "POST", data); }
                        catch (error) {
                            submit.textContent = "ذخیره ویرایش";
                            ui.notify(`دیدگاه ذخیره شد اما تصویر بارگذاری نشد: ${error.message || "دوباره تلاش کنید."}`, "danger");
                            await load(); return;
                        }
                    }
                    if (editor.elements.removeImage.checked) {
                        try { await request(`${endpoint.split("?")[0]}/${editingId}/image?feature=testimonials`, "DELETE"); }
                        catch (error) {
                            submit.textContent = "ذخیره ویرایش";
                            ui.notify(`دیدگاه ذخیره شد اما تصویر حذف نشد: ${error.message || "دوباره تلاش کنید."}`, "danger");
                            await load(); return;
                        }
                    }
                    ui.notify(result.message, "success"); editor.reset(); await load();
                } catch (error) { ui.notify(error.message || "ذخیره دیدگاه ناموفق بود.", "danger"); }
                finally { submit.disabled = false; }
            });
        }
        void load();
    }

    window.AdminHomeTestimonials = Object.freeze({ initialize });
})();
