window.HNUI.ready(() => window.HNUI.mount("checkout", document, () => {
    const ui = window.HNUI;
    const mediaCdnBaseUrl = document.querySelector('meta[name="media-cdn-base-url"]')?.content || "";
    const addressKey = "hn-delivery-addresses";
    const paymentMethods = Object.freeze({ zarinpal: "زرین‌پال", });
    const select = document.querySelector("#addressSelect");
    const preview = document.querySelector("#addressPreview");
    const checkoutPreview = document.querySelector("#checkoutAddressPreview");
    const payment = document.querySelector("#finalPayment");
    const cartCheckoutLink = () => document.querySelector("#cartView .cart-checkout-link");
    let addresses = [];
    const checkoutPage = document.querySelector(".invoice-page");
    let selectedPaymentMethod = checkoutPage?.dataset.paymentMethod || "zarinpal";
    let checkoutPending = false;
    let savedOrder = null;
    const checkoutKeyStorage = "hn-pending-checkout-key";
    const purchaseErrorText = (code) => ({
        "insufficient-stock": "موجودی کافی از این کالا باقی نمانده است؛ تعداد یا تنوع را در سبد اصلاح کنید.",
        "inventory-variant-required": "برای یکی از کالاها تنوع انبار را انتخاب کنید.",
        "inventory-variant-or-stock-changed": "موجودی تنوع یکی از کالاها تغییر کرده است؛ سبد را بازبینی کنید.",
        "product-variant-unavailable": "تنوع انتخاب‌شده دیگر قابل فروش نیست؛ کالا را از سبد حذف و دوباره انتخاب کنید.",
        "purchase-limit": "تعداد یکی از کالاها از سقف مجاز خرید بیشتر است.",
        "offer-disabled": "پیشنهاد فروش یکی از کالاها غیرفعال شده است؛ طرح فروش را تغییر دهید.",
        "inactive-plan": "طرح فروش یکی از کالاها فعال نیست یا مهلتش تمام شده است.",
        "product-not-in-plan": "یکی از کالاها در طرح انتخاب‌شده عرضه نمی‌شود.",
        "unknown-plan": "طرح فروش یکی از کالاها معتبر نیست؛ سبد را بازبینی کنید.",
        "invalid-product-options": "گزینه‌ها یا متعلقات یکی از کالاها معتبر نیست؛ سبد را اصلاح کنید.",
        "price-or-stock-changed": "قیمت یا موجودی یکی از کالاها تغییر کرده است؛ سبد را بازبینی کنید.",
        "product-or-plan-unavailable": "یکی از کالاها یا طرح فروش دیگر در دسترس نیست؛ سبد را اصلاح کنید.",
        "product-unavailable": "یکی از کالاها دیگر در فروشگاه فعال نیست.",
        "product-options-unavailable": "تنظیمات خرید یکی از کالاها در دسترس نیست؛ بعداً دوباره تلاش کنید.",
        "invalid-cart": "ساختار سبد معتبر نیست؛ کالاها را حذف و دوباره اضافه کنید.",
    })[code] || "وضعیت کالا از فروشگاه تأیید نشد؛ سبد را بازبینی و دوباره تلاش کنید.";
    const checkoutErrorText = (code) => ({
        ...Object.fromEntries(["insufficient-stock", "inventory-variant-required", "inventory-variant-or-stock-changed", "product-variant-unavailable", "purchase-limit", "offer-disabled", "inactive-plan", "product-not-in-plan", "unknown-plan", "invalid-product-options", "price-or-stock-changed"].map((key) => [key, purchaseErrorText(key)])),
        "invalid-cart": "ساختار سبد معتبر نیست؛ کالاها را حذف و دوباره اضافه کنید.",
        "product-or-plan-unavailable": "یکی از کالاها یا طرح فروش دیگر در دسترس نیست؛ سبد را اصلاح کنید.",
        "account-cart-empty-or-invalid": "سبد حساب خالی یا نامعتبر است؛ صفحهٔ سبد را تازه کنید.",
        "invalid-account-cart": "اطلاعات سبد حساب معتبر نیست؛ سبد را دوباره ذخیره کنید.",
        "invalid-account-cart-options": "انتخاب‌های سبد حساب معتبر نیست؛ گزینه‌های کالا را دوباره انتخاب کنید.",
        "product-unavailable": "یکی از کالاها دیگر در فروشگاه فعال نیست.",
        "product-options-unavailable": "تنظیمات خرید یکی از کالاها در دسترس نیست؛ بعداً دوباره تلاش کنید.",
        "invalid-coupon": "کد تخفیف نامعتبر است؛ فقط حروف انگلیسی و عدد وارد کنید.",
        "coupon-unavailable": "کد تخفیف منقضی، غیرفعال یا ناشناخته است.",
        "coupon-limit-reached": "ظرفیت استفاده از این کد تخفیف تکمیل شده است.",
        "coupon-no-eligible-items": "کد تخفیف روی کالاهای فعلی سبد قابل اعمال نیست.",
        "order-amount-out-of-range": "مبلغ سفارش از سقف مجاز بیشتر است؛ تعداد کالاها را کاهش دهید.",
        "address-not-owned": "نشانی انتخاب‌شده در حساب شما ثبت نشده است؛ نشانی دیگری انتخاب کنید.",
        "payment-method-unavailable": "روش پرداخت در حال حاضر در دسترس نیست؛ بعداً دوباره تلاش کنید.",
        "invalid-checkout": "نشانی یا روش پرداخت معتبر نیست؛ اطلاعات تسویه‌حساب را بازبینی کنید.",
    })[code] || "ثبت فاکتور انجام نشد؛ اطلاعات سبد، نشانی و شرایط خرید را بررسی کنید.";
    const readAddresses = () => {
        try {
            const value = JSON.parse(ui.storage.get(addressKey, "[]"));
            return Array.isArray(value)
                ? value.filter(
                    (address) =>
                        address &&
                        [
                            "id",
                            "title",
                            "recipient",
                            "phone",
                            "province",
                            "street",
                            "postal",
                        ].every(
                            (key) => typeof address[key] === "string" && address[key].trim(),
                        ),
                )
                : [];
        } catch {
            return [];
        }
    };
    if (!Object.hasOwn(paymentMethods, selectedPaymentMethod)) {
        selectedPaymentMethod = "zarinpal";
    }
    addresses = readAddresses();

    let selected = checkoutPage?.dataset.addressId || "";
    const selectedAddress = () =>
        addresses.find((address) => address.id === selected);

    function addressMarkup(address) {
        if (!address) return "";
        const wrapper = document.createElement("div");
        wrapper.className = "selected-address-card";
        const heading = document.createElement("div");
        heading.className = "selected-address-card-heading";
        const title = document.createElement("strong");
        const street = document.createElement("p");
        const postal = document.createElement("small");
        const postalCode = document.createElement("span");
        const phone = document.createElement("span");
        postal.className = "selected-address-contact";
        title.textContent = address.recipient;
        const region = [address.province, address.city].filter(Boolean).join("، ");
        street.textContent = `${region}، ${address.street}`;
        postalCode.textContent = `کد پستی: ${address.postal}`;
        phone.textContent = `تلفن: ${address.phone}`;
        postal.append(postalCode, phone);
        heading.append(title);
        wrapper.append(heading, street, postal);
        return wrapper;
    }

    function syncPayment() {
        const link = cartCheckoutLink();
        if (link) {
            const ready =
                Boolean(selectedAddress()) &&
                Boolean(selectedPaymentMethod) &&
                ui.cart.read().length > 0;
            link.classList.toggle("is-disabled", !ready);
            link.setAttribute("aria-disabled", String(!ready));
            link.title = ready
                ? ""
                : !selectedAddress()
                    ? "ابتدا آدرس تحویل را انتخاب کنید"
                    : "ابتدا نحوه پرداخت را انتخاب کنید";
        }
        if (!payment) return;
        payment.disabled = checkoutPending || !savedOrder || savedOrder.status !== "pending-payment";
    }

    function renderPaymentMethod() {
        document.querySelectorAll('[name="paymentMethod"]').forEach((radio) => {
            radio.checked = radio.value === selectedPaymentMethod;
        });
        const checkoutMethod = document.querySelector("#checkoutPaymentMethod");
        if (checkoutMethod) {
            checkoutMethod.replaceChildren();
            if (selectedPaymentMethod) {
                const logo = document.createElement("img");
                logo.className = "checkout-payment-logo";
                logo.src = `${mediaCdnBaseUrl}content/payments/zarinpal.svg`;
                logo.alt = "نشان زرین‌پال";
                logo.width = 48;
                logo.height = 48;
                logo.decoding = "async";
                const label = document.createElement("span");
                label.textContent = `پرداخت از طریق ${paymentMethods[selectedPaymentMethod]}`;
                checkoutMethod.append(logo, label);
            } else {
                checkoutMethod.textContent = "روش پرداختی انتخاب نشده است؛ به سبد خرید بازگردید.";
            }
        }
    }

    function renderPreview() {
        const address = selectedAddress();
        const addressSection = document.querySelector(".cart-address-section");
        addressSection?.classList.toggle("is-selected", Boolean(address));
        if (preview) {
            preview.replaceChildren();
            preview.hidden = !address;
            if (address) preview.append(addressMarkup(address));
        }
        if (checkoutPreview && !checkoutPage) {
            checkoutPreview.replaceChildren();
            if (address) checkoutPreview.append(addressMarkup(address));
            else {
                checkoutPreview.innerHTML =
                    '<div class="checkout-address-missing"><i class="bi bi-geo-alt"></i><div><strong>آدرسی برای تحویل انتخاب نشده است</strong><p>برای ثبت یا انتخاب آدرس به سبد خرید بازگردید.</p><a class="ui-inline-action checkout-address-action" href="Cart"><i class="bi bi-geo-alt" aria-hidden="true"></i><span>مدیریت آدرس در سبد خرید</span><i class="bi bi-arrow-left" aria-hidden="true"></i></a></div></div>';
            }
        }
        syncPayment();
    }

    function renderAddresses() {
        if (!select) {
            renderPreview();
            return;
        }
        select.replaceChildren(
            new Option(
                addresses.length ? "انتخاب آدرس تحویل" : "هنوز آدرسی ثبت نشده",
                "",
            ),
        );
        addresses.forEach((address) =>
            select.add(
                new Option(
                    `${address.title} — ${[address.province, address.city].filter(Boolean).join("، ")}، ${address.street}`,
                    address.id,
                ),
            ),
        );
        select.value = selectedAddress() ? selected : "";
        renderPreview();
    }

    if (select) {
        const dialog = document.querySelector("#addressDialog");
        const form = document.querySelector("#addressForm");
        const formStatus = document.querySelector("#addressFormStatus");
        const setFormStatus = (message, isError = false) => {
            formStatus.querySelector(".address-form-status-message").textContent = message;
            formStatus.querySelector("i").className = isError
                ? "bi bi-exclamation-circle-fill"
                : "bi bi-check-circle-fill";
            formStatus.hidden = !message;
            formStatus.classList.toggle("is-error", isError);
            ui.setMessageType(formStatus, message ? (isError ? "error" : "success") : "");
        };
        form.addEventListener("hn:form-invalid", () =>
            setFormStatus("لطفاً موارد مشخص‌شده را کامل و اصلاح کنید.", true),
        );
        form
            .querySelector('[name="phone"]')
            .closest("label")
            .after(form.querySelector('[name="postal"]').closest("label"));
        select.addEventListener("change", async () => {
            const next = select.value;
            selected = next;
            renderPreview();
        });
        document.querySelector("#newAddress").addEventListener("click", async () => {
            if (!ui.auth.isLoggedIn()) { ui.auth.requireLogin(() => document.querySelector("#newAddress")?.click()); return; }
            let fields;
            try { fields = await window.HNAddressFieldsReady; }
            catch { ui.setMessage(document.querySelector("#addressStatus"), "دریافت اطلاعات جغرافیایی ناموفق بود؛ صفحه را دوباره بارگذاری کنید.", "error"); return; }
            if (!fields) return;
            form.reset();
            fields.set();
            setFormStatus("");
            dialog.showModal();
            document.documentElement.classList.add("address-modal-open");
        });
        document
            .querySelector("#closeAddress")
            .addEventListener("click", () => dialog.close());
        dialog.addEventListener("close", () => {
            document.documentElement.classList.remove("address-modal-open");
            document.querySelector("#newAddress").focus();
        });
        form.addEventListener("submit", async (event) => {
            event.preventDefault();
            const address = Object.fromEntries(
                [...new FormData(form)].map(([key, value]) => [key, value.trim()]),
            );
            if (["title", "recipient", "phone", "country", "countryId", "province", "provinceId", "city", "cityId", "street", "postal"].some((key) => !address[key])) {
                setFormStatus("لطفاً همه مشخصات آدرس را کامل کنید.", true);
                return;
            }
            {
                try {
                    const response = await ui.api.request("account/addresses", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ title: address.title, recipient: address.recipient, phone: address.phone, countryId: Number(address.countryId), provinceId: Number(address.provinceId), cityId: Number(address.cityId), street: address.street, postal: address.postal }) });
                    const result = await response.json().catch(() => ({}));
                    if (!response.ok || !result.id) throw new Error("ذخیره نشانی در حساب انجام نشد.");
                    address.id = String(result.id);
                } catch (error) { setFormStatus(error.message || "ذخیره نشانی انجام نشد.", true); return; }
            }
            addresses.push(address);
            ui.storage.hydrate(addressKey, JSON.stringify(addresses));
            const nextSelection = address.id;
            selected = nextSelection;
            renderAddresses();
            dialog.close();
        });
        document.addEventListener("click", async (event) => {
            const link = event.target.closest("#cartView .cart-checkout-link");
            if (!link) return;
            event.preventDefault();
            if (checkoutPending) return;
            if (link.getAttribute("aria-disabled") !== "true") {
                if (!ui.auth.isLoggedIn()) { ui.auth.requireLogin(() => link.click()); return; }
                checkoutPending = true;
                link.setAttribute("aria-disabled", "true");
                try {
                    const confirmed = window.confirmFormSubmission
                        ? await window.confirmFormSubmission({
                            title: "تأیید ثبت سفارش",
                            message: "پس از ثبت سفارش، اقلام و نشانی این سفارش قابل ویرایش نیستند. آیا از اطلاعات سفارش اطمینان دارید؟",
                            confirmText: "تأیید و ادامه",
                        })
                        : window.confirm("پس از ثبت سفارش، اقلام و نشانی این سفارش قابل ویرایش نیستند. آیا ادامه می‌دهید؟");
                    if (!confirmed) {
                        checkoutPending = false;
                        syncPayment();
                        return;
                    }
                    let checkoutKey = sessionStorage.getItem(checkoutKeyStorage);
                    if (!checkoutKey) {
                        checkoutKey = crypto.randomUUID();
                        sessionStorage.setItem(checkoutKeyStorage, checkoutKey);
                    }
                    const items = ui.cart.read().map((item) => ({
                        productId: Number(String(item.id).replace(/:.*$/, "")),
                        salesPlanId: item.salesPlanId || "regular", quantity: item.quantity,
                        variantId: item.variantId || null,
                        accessoryIds: (item.options?.accessories || []).filter((accessory) => accessory.selected !== false).map((accessory) => Number(accessory.productAccessoryId)).filter((id) => Number.isSafeInteger(id) && id > 0),
                    }));
                    const saveResponse = await ui.api.request("account/cart", { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ items }) });
                    if (!saveResponse.ok) {
                        const error = await saveResponse.json().catch(() => ({}));
                        throw new Error(checkoutErrorText(error.code));
                    }
                    const couponCode = String(ui.storage.get("hn-coupon-code", ui.storage.get("hn-demo-coupon", ""))).trim().toUpperCase();
                    const response = await ui.api.request("account/checkout", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ addressId: Number(selected), paymentMethod: selectedPaymentMethod, couponCode: couponCode || null, checkoutKey }) });
                    const result = await response.json().catch(() => ({}));
                    if (!response.ok || !result.id) throw new Error(checkoutErrorText(result.code));
                    ui.storage.remove("hn-coupon-code");
                    ui.storage.remove("hn-demo-coupon");
                    ui.cart.write([]);
                    sessionStorage.removeItem(checkoutKeyStorage);
                    window.location.assign(`${link.href.split("?")[0]}?orderId=${encodeURIComponent(result.id)}`);
                } catch (error) {
                    checkoutPending = false;
                    syncPayment();
                    ui.setMessage(document.querySelector("#paymentMethodStatus"), `ثبت سفارش انجام نشد: ${error.message || "ارتباط با فروشگاه برقرار نشد."}`, "error");
                }
                return;
            }
            const needsAddress = !selectedAddress();
            const target = needsAddress
                ? document.querySelector(".cart-address-section")
                : document.querySelector(".cart-payment-section");
            const status = needsAddress
                ? document.querySelector("#addressStatus")
                : document.querySelector("#paymentMethodStatus");
            ui.setMessage(
                status,
                needsAddress
                    ? "برای ادامه، ابتدا یک آدرس تحویل ثبت یا انتخاب کنید."
                    : "برای ادامه، انتخاب نحوه پرداخت الزامی است.",
                "warning",
            );
            target.scrollIntoView({
                behavior: "smooth",
                block: "center",
            });
        });
    }

    document.querySelectorAll('[name="paymentMethod"]').forEach((radio) => {
        radio.addEventListener("change", async () => {
            if (!radio.checked || !Object.hasOwn(paymentMethods, radio.value)) return;
            const next = radio.value;
            selectedPaymentMethod = next;
            ui.setMessage(
                document.querySelector("#paymentMethodStatus"),
                `${paymentMethods[selectedPaymentMethod]} به‌عنوان روش پرداخت انتخاب شد.`,
                "success",
            );
            renderPaymentMethod();
            syncPayment();
        });
    });

    if (payment) {
        payment.addEventListener("click", async () => {
            syncPayment();
            if (payment.disabled) return;
            checkoutPending = true;
            payment.disabled = true;
            try {
                const url = await ui.payment.start(Number(savedOrder.id));
                window.location.assign(url);
            } catch (error) {
                checkoutPending = false;
                syncPayment();
                ui.setMessage(document.querySelector("#paymentStatus"), error.message || "درگاه پرداخت در دسترس نیست؛ سفارش در انتظار پرداخت است.", "error");
            }
        });
        const loadOrder = async () => {
            const orderId = Number(new URLSearchParams(window.location.search).get("orderId"));
            if (!Number.isSafeInteger(orderId) || orderId < 1) {
                ui.setMessage(document.querySelector("#paymentStatus"), "سفارش مشخص نشده است؛ سفارش‌های خود را در حساب کاربری مشاهده کنید.", "error");
                return;
            }
            try {
                const response = await ui.api.request("account/state");
                if (!response.ok) throw new Error("دریافت سفارش انجام نشد؛ وارد حساب کاربری شوید.");
                const account = await response.json();
                savedOrder = (account.orders || []).find((order) => Number(order.id) === orderId) || null;
                if (!savedOrder) throw new Error("سفارش در حساب شما پیدا نشد.");
                const amount = (value) => `${ui.formatNumber(Number(value) || 0)} تومان`;
                const items = document.querySelector("#savedOrderItems");
                items.replaceChildren();
                const itemList = document.createElement("div");
                itemList.className = "cart-items-list";
                for (const item of savedOrder.items || []) {
                    const row = document.createElement("article");
                    row.className = "flow-row cart-product-row";
                    const header = document.createElement("div");
                    header.className = "cart-product-header";
                    const identity = document.createElement("div");
                    identity.className = "cart-product-identity";
                    if (item.image) {
                        const image = document.createElement("img");
                        image.src = item.image;
                        image.alt = item.name || "";
                        image.loading = "lazy";
                        identity.append(image);
                    }
                    const copy = document.createElement("div");
                    copy.className = "cart-product-copy";
                    const name = document.createElement("strong");
                    name.textContent = item.name || "کالا";
                    const unitPrice = document.createElement("p");
                    unitPrice.className = "cart-product-unit-price";
                    const unitLabel = document.createElement("span");
                    unitLabel.textContent = "مبلغ واحد";
                    const unitAmount = document.createElement("strong");
                    unitAmount.textContent = amount(item.unitAmount);
                    unitPrice.append(unitLabel, unitAmount);
                    copy.append(name);
                    if (item.variantName) {
                        const variant = document.createElement("p"); variant.className = "checkout-order-meta"; variant.textContent = `تنوع انتخاب‌شده: ${item.variantName}`; copy.append(variant);
                    }
                    if (item.salesPlanName || item.salesPlanId) {
                        const plan = document.createElement("p"); plan.className = "checkout-order-meta"; plan.textContent = `طرح فروش: ${item.salesPlanName || item.salesPlanId}`; copy.append(plan);
                    }
                    copy.append(unitPrice);
                    identity.append(copy);
                    header.append(identity);

                    const toolbar = document.createElement("div");
                    toolbar.className = "cart-product-toolbar";
                    const lineTotal = document.createElement("p");
                    lineTotal.className = "cart-product-line-total";
                    const lineLabel = document.createElement("span");
                    lineLabel.textContent = "جمع ردیف";
                    const lineAmount = document.createElement("strong");
                    lineAmount.textContent = amount(Number(item.unitAmount) * Number(item.quantity));
                    lineTotal.append(lineLabel, lineAmount);
                    const quantity = document.createElement("span");
                    quantity.className = "invoice-item-quantity";
                    quantity.textContent = `تعداد: ${ui.formatNumber(item.quantity)}`;
                    toolbar.append(lineTotal, quantity);

                    const details = document.createElement("div");
                    details.className = "cart-product-copy cart-product-details";
                    const components = window.HNOrderPriceComponents.render(item, ui);
                    if (components) details.append(components);
                    row.append(header, toolbar, details);
                    itemList.append(row);
                }
                if (itemList.childElementCount) items.append(itemList);
                else items.innerHTML = "<p>اقلام سفارش در دسترس نیست.</p>";
                const summary = document.querySelector("#savedOrderSummary");
                summary.replaceChildren();
                const summaryHeading = document.createElement("div");
                summaryHeading.className = "cart-summary-heading";
                summaryHeading.innerHTML = '<span class="cart-summary-icon"><i class="bi bi-receipt" aria-hidden="true"></i></span><h2>خلاصهٔ سفارش</h2>';
                const summaryLines = document.createElement("div");
                summaryLines.className = "cart-summary-lines";
                for (const [label, value] of [["شماره سفارش", savedOrder.number], ["جمع اقلام پس از تخفیف طرح فروش", amount(savedOrder.subtotalAmount)], ["کد تخفیف", savedOrder.couponCode || "استفاده نشده"], ["مبلغ تخفیف کد", amount(savedOrder.couponDiscountAmount)], ["مبلغ قابل پرداخت", amount(savedOrder.totalAmount)]]) {
                    const row = document.createElement("div");
                    row.className = `flow-total${label === "مبلغ قابل پرداخت" ? " flow-payable" : ""}`;
                    const title = document.createElement("span"); title.textContent = label;
                    const valueNode = document.createElement("strong"); valueNode.textContent = value;
                    row.append(title, valueNode); summaryLines.append(row);
                }
                summary.append(summaryHeading, summaryLines);
                const lineAmounts = (savedOrder.items || []).map(item => Number(item.unitAmount) * Number(item.quantity));
                const itemSubtotal = lineAmounts.reduce((sum, value) => sum + value, 0);
                const recordedSubtotal = Number(savedOrder.subtotalAmount);
                if (lineAmounts.length && lineAmounts.every(Number.isSafeInteger) && Number.isSafeInteger(itemSubtotal) && Number.isSafeInteger(recordedSubtotal) && itemSubtotal !== recordedSubtotal) {
                    const warning = document.createElement("p"); warning.className = "invoice-demo-note";
                    warning.textContent = "جمع جزئیات این سفارش با مبلغ ثبت‌شده اختلاف دارد. برای بررسی با پشتیبانی تماس بگیرید.";
                    summary.append(warning);
                }
                let address = null;
                try { address = JSON.parse(savedOrder.addressSnapshotJson || "null"); } catch { /* Older orders may not have a readable snapshot. */ }
                checkoutPreview.replaceChildren();
                if (address) checkoutPreview.append(addressMarkup(address));
                else checkoutPreview.textContent = "نشانی ثبت‌شده در دسترس نیست.";
                selectedPaymentMethod = savedOrder.paymentMethod;
                renderPaymentMethod();
                if (savedOrder.status !== "pending-payment") ui.setMessage(document.querySelector("#paymentStatus"), "این سفارش در وضعیت انتظار پرداخت نیست.", "warning");
                syncPayment();
            } catch (error) {
                ui.setMessage(document.querySelector("#paymentStatus"), error.message || "دریافت سفارش انجام نشد.", "error");
            }
        };
        void loadOrder();
    }
    document.addEventListener("hn:cart-change", () => {
        syncPayment();
    });

    renderAddresses();
    if (ui.auth.isLoggedIn()) void ui.api.request("account/state").then(async (response) => {
        if (!response.ok) return;
        const account = await response.json();
        const pendingKey = select && sessionStorage.getItem(checkoutKeyStorage);
        const pendingOrder = pendingKey && (account.orders || []).find((order) => order.checkoutKey === `checkout-${pendingKey}`);
        if (pendingOrder) {
            sessionStorage.removeItem(checkoutKeyStorage);
            window.location.assign(`Checkout?orderId=${encodeURIComponent(pendingOrder.id)}`);
            return;
        }
        if (Array.isArray(account.addresses)) addresses = account.addresses;
        if (!selected) selected = String(account.checkout?.selectedAddressId || "");
        ui.storage.hydrate(addressKey, JSON.stringify(addresses));
        renderAddresses(); renderPaymentMethod();
    }).catch(() => {});
    document.addEventListener("hn:auth-change", (event) => {
        if (!event.detail) return;
        void ui.api.request("account/state").then(async (response) => {
            if (!response.ok) return;
            const account = await response.json();
            addresses = Array.isArray(account.addresses) ? account.addresses : [];
            if (!selected) selected = String(account.checkout?.selectedAddressId || "");
            ui.storage.hydrate(addressKey, JSON.stringify(addresses));
            renderAddresses(); renderPaymentMethod();
        }).catch(() => {});
    });
    renderPaymentMethod();
}));
