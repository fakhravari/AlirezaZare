window.HNUI.ready(() => window.HNUI.mount("checkout", document, () => {
    const ui = window.HNUI;
    const addressKey = "hn-delivery-addresses";
    const selectedKey = "hn-selected-address";
    const paymentMethodKey = "hn-payment-method-v2";
    const paymentMethods = Object.freeze({ zarinpal: "Ø²Ø±ÛŒÙ†â€ŒÙ¾Ø§Ù„", });
    const select = document.querySelector("#addressSelect");
    const preview = document.querySelector("#addressPreview");
    const checkoutPreview = document.querySelector("#checkoutAddressPreview");
    const payment = document.querySelector("#finalPayment");
    const cartCheckoutLink = () => document.querySelector("#cartView .cart-checkout-link");
    let addresses = [];
    let selectedPaymentMethod = ui.storage.get(paymentMethodKey, "zarinpal");
    let salesPlansValid = false;
    let validationAttempt = 0;
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
        try {
            const response = await window.HNUI.api.request("store/sales-plans/validate-cart", {
                method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ lines }),
            });
            if (attempt !== validationAttempt) return;
            if (response.ok) {
                const result = await response.json();
                salesPlansValid = result.valid === true && Array.isArray(result.lines) &&
                    result.lines.length === currentItems.length && result.lines.every((line, index) =>
                        line.productId === Number(currentItems[index].id) &&
                        line.salesPlanId === currentItems[index].salesPlanId &&
                        Number(line.quote?.variantId || 0) === Number(currentItems[index].variantId || 0) &&
                        Number(line.quote?.unitAmount) === Number(currentItems[index].price));
            } else salesPlansValid = false;
        } catch { salesPlansValid = false; }
        if (attempt !== validationAttempt) return;
        syncPayment();
        const message = document.querySelector("#paymentStatus");
        if (!salesPlansValid && message) ui.setMessage(message, "Ù‚ÛŒÙ…Øª ÛŒØ§ Ù…ÙˆØ¬ÙˆØ¯ÛŒ ÛŒÚ©ÛŒ Ø§Ø² Ú©Ø§Ù„Ø§Ù‡Ø§ Ø¨Ø§ Ø§Ø·Ù„Ø§Ø¹Ø§Øª ÙØ¹Ù„ÛŒ Ù…Ø·Ø§Ø¨Ù‚Øª Ù†Ø¯Ø§Ø±Ø¯Ø› Ø¨Ù‡ Ø³Ø¨Ø¯ Ø¨Ø§Ø²Ú¯Ø±Ø¯ÛŒØ¯ØŒ Ú©Ø§Ù„Ø§ Ø±Ø§ Ø§Ø² Ø³Ø¨Ø¯ Ø­Ø°Ù Ùˆ Ø¯ÙˆØ¨Ø§Ø±Ù‡ Ø§Ø² ØµÙØ­Ù‡Ù” Ù…Ø­ØµÙˆÙ„ Ø§Ø¶Ø§ÙÙ‡ Ú©Ù†ÛŒØ¯.", "warning");
    };
    if (!Object.hasOwn(paymentMethods, selectedPaymentMethod)) {
        selectedPaymentMethod = "zarinpal";
    }
    ui.storage.set(paymentMethodKey, selectedPaymentMethod);

    addresses = readAddresses();

    let selected = ui.storage.get(selectedKey, "");
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
        const region = [address.province, address.city].filter(Boolean).join("ØŒ ");
        street.textContent = `${region}ØŒ ${address.street}`;
        postalCode.textContent = `Ú©Ø¯ Ù¾Ø³ØªÛŒ: ${address.postal}`;
        phone.textContent = `ØªÙ„ÙÙ†: ${address.phone}`;
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
                    ? "Ø§Ø¨ØªØ¯Ø§ Ø¢Ø¯Ø±Ø³ ØªØ­ÙˆÛŒÙ„ Ø±Ø§ Ø§Ù†ØªØ®Ø§Ø¨ Ú©Ù†ÛŒØ¯"
                    : "Ø§Ø¨ØªØ¯Ø§ Ù†Ø­ÙˆÙ‡ Ù¾Ø±Ø¯Ø§Ø®Øª Ø±Ø§ Ø§Ù†ØªØ®Ø§Ø¨ Ú©Ù†ÛŒØ¯";
        }
        if (!payment) return;
        payment.disabled =
            !ui.cart.read().length || !selectedAddress() || !selectedPaymentMethod || !salesPlansValid;
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
                logo.src = "/content/payments/zarinpal.svg";
                logo.alt = "Ù†Ø´Ø§Ù† Ø²Ø±ÛŒÙ†â€ŒÙ¾Ø§Ù„";
                logo.width = 48;
                logo.height = 48;
                logo.decoding = "async";
                const label = document.createElement("span");
                label.textContent = `Ù¾Ø±Ø¯Ø§Ø®Øª Ø§Ø² Ø·Ø±ÛŒÙ‚ ${paymentMethods[selectedPaymentMethod]}`;
                checkoutMethod.append(logo, label);
            } else {
                checkoutMethod.textContent = "Ø±ÙˆØ´ Ù¾Ø±Ø¯Ø§Ø®ØªÛŒ Ø§Ù†ØªØ®Ø§Ø¨ Ù†Ø´Ø¯Ù‡ Ø§Ø³ØªØ› Ø¨Ù‡ Ø³Ø¨Ø¯ Ø®Ø±ÛŒØ¯ Ø¨Ø§Ø²Ú¯Ø±Ø¯ÛŒØ¯.";
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
                    '<div class="checkout-address-missing"><i class="bi bi-geo-alt"></i><div><strong>Ø¢Ø¯Ø±Ø³ÛŒ Ø¨Ø±Ø§ÛŒ ØªØ­ÙˆÛŒÙ„ Ø§Ù†ØªØ®Ø§Ø¨ Ù†Ø´Ø¯Ù‡ Ø§Ø³Øª</strong><p>Ø¨Ø±Ø§ÛŒ Ø«Ø¨Øª ÛŒØ§ Ø§Ù†ØªØ®Ø§Ø¨ Ø¢Ø¯Ø±Ø³ Ø¨Ù‡ Ø³Ø¨Ø¯ Ø®Ø±ÛŒØ¯ Ø¨Ø§Ø²Ú¯Ø±Ø¯ÛŒØ¯.</p><a class="ui-inline-action checkout-address-action" href="Cart"><i class="bi bi-geo-alt" aria-hidden="true"></i><span>Ù…Ø¯ÛŒØ±ÛŒØª Ø¢Ø¯Ø±Ø³ Ø¯Ø± Ø³Ø¨Ø¯ Ø®Ø±ÛŒØ¯</span><i class="bi bi-arrow-left" aria-hidden="true"></i></a></div></div>';
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
                addresses.length ? "Ø§Ù†ØªØ®Ø§Ø¨ Ø¢Ø¯Ø±Ø³ ØªØ­ÙˆÛŒÙ„" : "Ù‡Ù†ÙˆØ² Ø¢Ø¯Ø±Ø³ÛŒ Ø«Ø¨Øª Ù†Ø´Ø¯Ù‡",
                "",
            ),
        );
        addresses.forEach((address) =>
            select.add(
                new Option(
                    `${address.title} â€” ${[address.province, address.city].filter(Boolean).join("ØŒ ")}ØŒ ${address.street}`,
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
            setFormStatus("Ù„Ø·ÙØ§Ù‹ Ù…ÙˆØ§Ø±Ø¯ Ù…Ø´Ø®Øµâ€ŒØ´Ø¯Ù‡ Ø±Ø§ Ú©Ø§Ù…Ù„ Ùˆ Ø§ØµÙ„Ø§Ø­ Ú©Ù†ÛŒØ¯.", true),
        );
        form
            .querySelector('[name="phone"]')
            .closest("label")
            .after(form.querySelector('[name="postal"]').closest("label"));
        select.addEventListener("change", () => {
            selected = select.value;
            ui.storage.set(selectedKey, selected);
            if (ui.auth.isLoggedIn() && selected) void ui.api.request("account/checkout-preference", { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ addressId: Number(selected), paymentMethod: selectedPaymentMethod }) });
            renderPreview();
        });
        document.querySelector("#newAddress").addEventListener("click", () => {
            form.reset();
            window.HNAddressFields.set();
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
                setFormStatus("Ù„Ø·ÙØ§Ù‹ Ù‡Ù…Ù‡ Ù…Ø´Ø®ØµØ§Øª Ø¢Ø¯Ø±Ø³ Ø±Ø§ Ú©Ø§Ù…Ù„ Ú©Ù†ÛŒØ¯.", true);
                return;
            }
            address.id = crypto.randomUUID();
            if (ui.auth.isLoggedIn()) {
                try {
                    const response = await ui.api.request("account/addresses", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ title: address.title, recipient: address.recipient, phone: address.phone, country: address.country, countryId: address.countryId, province: address.province, provinceId: address.provinceId, city: address.city, cityId: address.cityId, street: address.street, postal: address.postal }) });
                    const result = await response.json().catch(() => ({}));
                    if (!response.ok || !result.id) throw new Error("Ø°Ø®ÛŒØ±Ù‡ Ù†Ø´Ø§Ù†ÛŒ Ø¯Ø± Ø­Ø³Ø§Ø¨ Ø§Ù†Ø¬Ø§Ù… Ù†Ø´Ø¯.");
                    address.id = String(result.id);
                } catch (error) { setFormStatus(error.message || "Ø°Ø®ÛŒØ±Ù‡ Ù†Ø´Ø§Ù†ÛŒ Ø§Ù†Ø¬Ø§Ù… Ù†Ø´Ø¯.", true); return; }
            }
            addresses.push(address);
            if (!ui.storage.set(addressKey, JSON.stringify(addresses))) {
                addresses.pop();
                setFormStatus(
                    "Ø°Ø®ÛŒØ±Ù‡ Ø¢Ø¯Ø±Ø³ Ù…Ù…Ú©Ù† Ù†Ø´Ø¯. Ø¯Ø³ØªØ±Ø³ÛŒ Ù…Ø±ÙˆØ±Ú¯Ø± Ø¨Ù‡ ÙØ¶Ø§ÛŒ Ø°Ø®ÛŒØ±Ù‡â€ŒØ³Ø§Ø²ÛŒ Ø±Ø§ Ø¨Ø±Ø±Ø³ÛŒ Ú©Ù†ÛŒØ¯.",
                    true,
                );
                return;
            }
            const previousSelection = selected;
            selected = address.id;
            ui.storage.set(selectedKey, selected);
            if (ui.auth.isLoggedIn()) {
                let preferenceSaved = false;
                try {
                    const preferenceResponse = await ui.api.request("account/checkout-preference", { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ addressId: Number(selected), paymentMethod: selectedPaymentMethod }) });
                    preferenceSaved = preferenceResponse.ok;
                } catch { /* Address is saved; leave checkout selection unchanged until API succeeds. */ }
                if (!preferenceSaved) {
                    selected = previousSelection;
                    ui.storage.set(selectedKey, selected);
                    setFormStatus("آدرس ذخیره شد، اما ثبت انتخاب آن برای تسویه‌حساب انجام نشد. دوباره آدرس را انتخاب کنید.", true);
                    renderAddresses();
                    return;
                }
            }
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
                    ? "Ø¨Ø±Ø§ÛŒ Ø§Ø¯Ø§Ù…Ù‡ØŒ Ø§Ø¨ØªØ¯Ø§ ÛŒÚ© Ø¢Ø¯Ø±Ø³ ØªØ­ÙˆÛŒÙ„ Ø«Ø¨Øª ÛŒØ§ Ø§Ù†ØªØ®Ø§Ø¨ Ú©Ù†ÛŒØ¯."
                    : "Ø¨Ø±Ø§ÛŒ Ø§Ø¯Ø§Ù…Ù‡ØŒ Ø§Ù†ØªØ®Ø§Ø¨ Ù†Ø­ÙˆÙ‡ Ù¾Ø±Ø¯Ø§Ø®Øª Ø§Ù„Ø²Ø§Ù…ÛŒ Ø§Ø³Øª.",
                "warning",
            );
            target.scrollIntoView({
                behavior: "smooth",
                block: "center",
            });
        });
    }

    document.querySelectorAll('[name="paymentMethod"]').forEach((radio) => {
        radio.addEventListener("change", () => {
            if (!radio.checked || !Object.hasOwn(paymentMethods, radio.value)) return;
            selectedPaymentMethod = radio.value;
            ui.storage.set(paymentMethodKey, selectedPaymentMethod);
            if (ui.auth.isLoggedIn() && selected) void ui.api.request("account/checkout-preference", { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ addressId: Number(selected), paymentMethod: selectedPaymentMethod }) });
            ui.setMessage(
                document.querySelector("#paymentMethodStatus"),
                `${paymentMethods[selectedPaymentMethod]} Ø¨Ù‡â€ŒØ¹Ù†ÙˆØ§Ù† Ø±ÙˆØ´ Ù¾Ø±Ø¯Ø§Ø®Øª Ø§Ù†ØªØ®Ø§Ø¨ Ø´Ø¯.`,
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
            payment.disabled = true;
            try {
                const lines = ui.cart.read().map((item) => ({ productId: Number(String(item.id).replace(/:.*$/, "")), salesPlanId: item.salesPlanId || "regular", quantity: item.quantity, variantId: item.variantId || null, options: item.options || {} }));
                const response = await ui.api.request("account/checkout", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ addressId: Number(selected), paymentMethod: selectedPaymentMethod, lines }) });
                const result = await response.json().catch(() => ({}));
                if (!response.ok) throw new Error(result.code === "price-or-stock-changed" ? "قیمت یا موجودی تغییر کرده است؛ سبد را دوباره بررسی کنید." : "ثبت سفارش انجام نشد. نشانی و سبد خرید را بررسی کنید.");
                ui.cart.write([]);
                ui.setMessage(document.querySelector("#paymentStatus"), `سفارش ${result.number} ثبت شد و در انتظار پرداخت است. مبلغ: ${ui.formatNumber(result.totalAmount)} تومان`, "success");
                payment.textContent = `سفارش ${result.number} ثبت شد`;
            } catch (error) {
                ui.setMessage(document.querySelector("#paymentStatus"), error.message || "ثبت سفارش انجام نشد.", "error");
                syncPayment();
            }
        });
    }
    document.addEventListener("hn:cart-change", () => {
        syncPayment();
        if (payment) validateSalesPlanCart();
    });

    window.addEventListener("storage", (event) => {
        if (event.key === addressKey || event.key === selectedKey) {
            if (event.key === addressKey) addresses = readAddresses();
            selected = ui.storage.get(selectedKey, "");
            renderAddresses();
            return;
        }
        if (event.key !== paymentMethodKey) return;
        const nextMethod = ui.storage.get(paymentMethodKey, "zarinpal");
        selectedPaymentMethod = Object.hasOwn(paymentMethods, nextMethod)
            ? nextMethod
            : "zarinpal";
        renderPaymentMethod();
        syncPayment();
    });
    renderAddresses();
    if (ui.auth.isLoggedIn()) void ui.api.request("account/state").then(async (response) => {
        if (!response.ok) return;
        const account = await response.json();
        if (Array.isArray(account.addresses)) addresses = account.addresses;
        selected = String(account.checkout?.selectedAddressId || selected || "");
        if (account.checkout?.paymentMethod) selectedPaymentMethod = account.checkout.paymentMethod;
        ui.storage.set(addressKey, JSON.stringify(addresses)); ui.storage.set(selectedKey, selected); ui.storage.set(paymentMethodKey, selectedPaymentMethod);
        if (selected && addresses.some((address) => String(address.id) === selected))
            await ui.api.request("account/checkout-preference", { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ addressId: Number(selected), paymentMethod: selectedPaymentMethod }) });
        renderAddresses(); renderPaymentMethod();
    }).catch(() => {});
    document.addEventListener("hn:auth-change", (event) => {
        if (!event.detail) return;
        void ui.api.request("account/state").then(async (response) => {
            if (!response.ok) return;
            const account = await response.json();
            addresses = Array.isArray(account.addresses) ? account.addresses : [];
            selected = String(account.checkout?.selectedAddressId || ui.storage.get(selectedKey, ""));
            if (account.checkout?.paymentMethod) selectedPaymentMethod = account.checkout.paymentMethod;
            ui.storage.set(addressKey, JSON.stringify(addresses)); ui.storage.set(selectedKey, selected); ui.storage.set(paymentMethodKey, selectedPaymentMethod);
            if (selected && addresses.some((address) => String(address.id) === selected))
                await ui.api.request("account/checkout-preference", { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ addressId: Number(selected), paymentMethod: selectedPaymentMethod }) });
            renderAddresses(); renderPaymentMethod();
        }).catch(() => {});
    });
    if (payment) validateSalesPlanCart();
    renderPaymentMethod();
}));
