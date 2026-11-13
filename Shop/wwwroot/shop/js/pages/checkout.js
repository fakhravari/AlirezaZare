window.HNUI.ready(() => window.HNUI.mount("checkout", document, () => {
    const ui = window.HNUI;
    const mediaCdnBaseUrl = document.querySelector('meta[name="media-cdn-base-url"]')?.content || "";
    const addressKey = "hn-delivery-addresses";
    const selectedKey = "hn-selected-address";
    const paymentMethodKey = "hn-payment-method-v2";
    const paymentMethods = Object.freeze({ zarinpal: "زرین‌پال", });
    const select = document.querySelector("#addressSelect");
    const preview = document.querySelector("#addressPreview");
    const checkoutPreview = document.querySelector("#checkoutAddressPreview");
    const payment = document.querySelector("#finalPayment");
    const cartCheckoutLink = () => document.querySelector("#cartView .cart-checkout-link");
    let addresses = [];
    let selectedPaymentMethod = "zarinpal";
    let salesPlansValid = false;
    let validationAttempt = 0;
    let checkoutPending = false;
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
    const validateSalesPlanCart = async () => {
        const attempt = ++validationAttempt;
        const currentItems = ui.cart.read();
        const lines = currentItems.map((item) => ({
            productId: Number(item.id), salesPlanId: item.salesPlanId || "regular", quantity: item.quantity,
            variantId: item.variantId || null,
            priceOptions: Object.fromEntries((item.options?.priceOptions || []).map((option) => [option.key, option.productValue || option.value])),
            accessoryIds: (item.options?.accessories || []).filter((item) => item.selected !== false).map((item) => item.id),
        }));
        if (!lines.length) {
            salesPlansValid = false;
            ui.setMessage(document.querySelector("#paymentStatus"), "");
            syncPayment();
            return;
        }
        salesPlansValid = false;
        syncPayment();
        let valid = false;
        try {
            const response = await window.HNUI.api.request("store/sales-plans/validate-cart", {
                method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ lines }),
            });
            if (attempt !== validationAttempt) return;
            if (response.ok) {
                const result = await response.json();
                valid = result.valid === true && Array.isArray(result.lines) &&
                    result.lines.length === currentItems.length && result.lines.every((line, index) =>
                        line.productId === Number(currentItems[index].id) &&
                        line.salesPlanId === currentItems[index].salesPlanId &&
                        Number(line.quote?.variantId || 0) === Number(currentItems[index].variantId || 0) &&
                        Number(line.quote?.unitAmount) === Number(currentItems[index].price));
            }
        } catch { /* A failed validation leaves this attempt invalid. */ }
        if (attempt !== validationAttempt) return;
        salesPlansValid = valid;
        syncPayment();
        const message = document.querySelector("#paymentStatus");
        if (!salesPlansValid && message) ui.setMessage(message, "قیمت یا موجودی یکی از کالاها با اطلاعات فعلی مطابقت ندارد؛ به سبد بازگردید، کالا را از سبد حذف و دوباره از صفحهٔ محصول اضافه کنید.", "warning");
    };
    if (!Object.hasOwn(paymentMethods, selectedPaymentMethod)) {
        selectedPaymentMethod = "zarinpal";
    }
    addresses = readAddresses();

    let selected = "";
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
        payment.disabled =
            checkoutPending || !ui.cart.read().length || !selectedAddress() || !selectedPaymentMethod || !salesPlansValid;
        if (salesPlansValid) ui.setMessage(document.querySelector("#paymentStatus"), "");
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
        if (checkoutPreview) {
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
            if (ui.auth.isLoggedIn() && next) {
                try {
                    const response = await ui.api.request("account/checkout-preference", { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ addressId: Number(next), paymentMethod: selectedPaymentMethod }) });
                    if (!response.ok) throw new Error();
                } catch {
                    select.value = selected;
                    ui.setMessage(document.querySelector("#addressStatus"), "ثبت انتخاب نشانی در حساب انجام نشد.", "error");
                    return;
                }
            }
            selected = next;
            ui.storage.hydrate(selectedKey, selected);
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
            {
                let preferenceSaved = false;
                try {
                    const preferenceResponse = await ui.api.request("account/checkout-preference", { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ addressId: Number(nextSelection), paymentMethod: selectedPaymentMethod }) });
                    preferenceSaved = preferenceResponse.ok;
                } catch { /* Address is saved; leave checkout selection unchanged until API succeeds. */ }
                if (!preferenceSaved) {
                    setFormStatus("آدرس ذخیره شد، اما ثبت انتخاب آن برای تسویه‌حساب انجام نشد. دوباره آدرس را انتخاب کنید.", true);
                    renderAddresses();
                    return;
                }
            }
            selected = nextSelection;
            ui.storage.hydrate(selectedKey, selected);
            renderAddresses();
            dialog.close();
        });
        document.addEventListener("click", (event) => {
            const link = event.target.closest("#cartView .cart-checkout-link");
            if (!link || link.getAttribute("aria-disabled") !== "true") return;
            event.preventDefault();
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
            if (ui.auth.isLoggedIn() && selected) {
                try {
                    const response = await ui.api.request("account/checkout-preference", { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ addressId: Number(selected), paymentMethod: next }) });
                    if (!response.ok) throw new Error();
                } catch {
                    renderPaymentMethod();
                    ui.setMessage(document.querySelector("#paymentMethodStatus"), "ثبت روش پرداخت در حساب انجام نشد.", "error");
                    return;
                }
            }
            selectedPaymentMethod = next;
            ui.storage.hydrate(paymentMethodKey, selectedPaymentMethod);
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
            if (!ui.auth.isLoggedIn()) { ui.auth.requireLogin(() => payment.click()); return; }
            checkoutPending = true;
            payment.disabled = true;
            try {
                const items = ui.cart.read().map((item) => ({
                    productId: Number(String(item.id).replace(/:.*$/, "")),
                    salesPlanId: item.salesPlanId || "regular",
                    quantity: item.quantity,
                    variantId: item.variantId || null,
                    optionValueIds: (item.options?.priceOptions || []).map((option) => Number(option.id)).filter((id) => Number.isSafeInteger(id) && id > 0),
                    accessoryIds: (item.options?.accessories || []).filter((accessory) => accessory.selected !== false).map((accessory) => Number(accessory.productAccessoryId)).filter((id) => Number.isSafeInteger(id) && id > 0),
                }));
                const saveResponse = await ui.api.request("account/cart", { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ items }) });
                if (!saveResponse.ok) throw new Error("ذخیرهٔ سبد خرید انجام نشد؛ سبد را دوباره بررسی کنید.");
                const couponCode = String(ui.storage.get("hn-coupon-code", ui.storage.get("hn-demo-coupon", ""))).trim().toUpperCase();
                const response = await ui.api.request("account/checkout", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ addressId: Number(selected), paymentMethod: selectedPaymentMethod, couponCode: couponCode || null }) });
                const result = await response.json().catch(() => ({}));
                if (!response.ok) {
                    if (result.code === "price-or-stock-changed") {
                        validationAttempt++;
                        salesPlansValid = false;
                    }
                    throw new Error(result.code === "price-or-stock-changed" ? "قیمت یا موجودی تغییر کرده است؛ سبد را دوباره بررسی کنید." : result.code === "order-amount-out-of-range" ? "مبلغ سفارش بیش از سقف مجاز است؛ تعداد کالاها را کاهش دهید." : result.code === "coupon-unavailable" ? "کد تخفیف دیگر معتبر یا فعال نیست." : result.code === "coupon-limit-reached" ? "ظرفیت استفاده از کد تخفیف تکمیل شده است." : result.code === "coupon-no-eligible-items" ? "کد تخفیف روی اقلام فعلی سبد قابل اعمال نیست." : "ثبت سفارش انجام نشد. نشانی و سبد خرید را بررسی کنید.");
                }
                ui.storage.remove("hn-coupon-code");
                ui.storage.remove("hn-demo-coupon");
                ui.cart.write([]);
                ui.setMessage(document.querySelector("#paymentStatus"), `سفارش ${result.number} ثبت شد و در انتظار پرداخت است. مبلغ: ${ui.formatNumber(result.totalAmount)} تومان`, "success");
                payment.textContent = `سفارش ${result.number} ثبت شد`;
                if (typeof result.simulatedPaymentUrl === "string" && /^\/payment\/simulated\/\d+$/.test(result.simulatedPaymentUrl)) {
                    window.location.assign(result.simulatedPaymentUrl);
                } else {
                    try {
                        const redirectUrl = await ui.payment.start(Number(result.id));
                        window.location.assign(redirectUrl);
                    } catch (paymentError) {
                        ui.setMessage(document.querySelector("#paymentStatus"), `سفارش ${result.number} ثبت شد و در انتظار پرداخت است. ${paymentError.message || "درگاه پرداخت در دسترس نیست."}`, "error");
                    }
                }
            } catch (error) {
                checkoutPending = false;
                syncPayment();
                ui.setMessage(document.querySelector("#paymentStatus"), error.message || "ثبت سفارش انجام نشد.", "error");
            }
        });
    }
    document.addEventListener("hn:cart-change", () => {
        syncPayment();
        if (payment) validateSalesPlanCart();
    });

    renderAddresses();
    if (ui.auth.isLoggedIn()) void ui.api.request("account/state").then(async (response) => {
        if (!response.ok) return;
        const account = await response.json();
        if (Array.isArray(account.addresses)) addresses = account.addresses;
        selected = String(account.checkout?.selectedAddressId || "");
        if (account.checkout?.paymentMethod) selectedPaymentMethod = account.checkout.paymentMethod;
        ui.storage.hydrate(addressKey, JSON.stringify(addresses)); ui.storage.hydrate(selectedKey, selected); ui.storage.hydrate(paymentMethodKey, selectedPaymentMethod);
        renderAddresses(); renderPaymentMethod();
    }).catch(() => {});
    document.addEventListener("hn:auth-change", (event) => {
        if (!event.detail) return;
        void ui.api.request("account/state").then(async (response) => {
            if (!response.ok) return;
            const account = await response.json();
            addresses = Array.isArray(account.addresses) ? account.addresses : [];
            selected = String(account.checkout?.selectedAddressId || "");
            if (account.checkout?.paymentMethod) selectedPaymentMethod = account.checkout.paymentMethod;
            ui.storage.hydrate(addressKey, JSON.stringify(addresses)); ui.storage.hydrate(selectedKey, selected); ui.storage.hydrate(paymentMethodKey, selectedPaymentMethod);
            renderAddresses(); renderPaymentMethod();
        }).catch(() => {});
    });
    if (payment) validateSalesPlanCart();
    renderPaymentMethod();
}));
