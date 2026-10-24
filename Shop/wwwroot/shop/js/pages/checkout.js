window.HNUI.ready(() => window.HNUI.mount("checkout", document, () => {
  const ui = window.HNUI;
  const addressKey = "hn-delivery-addresses";
  const selectedKey = "hn-selected-address";
  const paymentMethodKey = "hn-payment-method-v2";
  const paymentMethods = Object.freeze({
    zarinpal: "زرین‌پال",
  });
  const select = document.querySelector("#addressSelect");
  const preview = document.querySelector("#addressPreview");
  const checkoutPreview = document.querySelector("#checkoutAddressPreview");
  const payment = document.querySelector("#finalPayment");
  const cartCheckoutLink = document.querySelector(
    "#cartView .cart-checkout-link",
  );
  let addresses = [];
  let selectedPaymentMethod = ui.storage.get(paymentMethodKey, "zarinpal");
  if (!Object.hasOwn(paymentMethods, selectedPaymentMethod)) {
    selectedPaymentMethod = "zarinpal";
  }
  ui.storage.set(paymentMethodKey, selectedPaymentMethod);

  try {
    const value = JSON.parse(ui.storage.get(addressKey, "[]"));
    if (Array.isArray(value)) {
      addresses = value.filter(
        (address) =>
          address &&
          [
            "id",
            "title",
            "recipient",
            "phone",
            "city",
            "street",
            "postal",
          ].every(
            (key) => typeof address[key] === "string" && address[key].trim(),
          ),
      );
    }
  } catch {
    addresses = [];
  }

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
    const badge = document.createElement("span");
    const badgeIcon = document.createElement("i");
    const street = document.createElement("p");
    const postal = document.createElement("small");
    badge.className = "selected-address-card-badge";
    badgeIcon.className = "bi bi-check2-circle";
    badgeIcon.setAttribute("aria-hidden", "true");
    badge.append(badgeIcon, document.createTextNode("آدرس انتخابی"));
    title.textContent = `${address.recipient} · ${address.phone}`;
    street.textContent = `${address.province ? `${address.province}، ` : ""}${address.city}، ${address.street}`;
    postal.textContent = `کد پستی: ${address.postal}`;
    heading.append(title, badge);
    wrapper.append(heading, street, postal);
    return wrapper;
  }

  function syncPayment() {
    if (cartCheckoutLink) {
      const ready =
        Boolean(selectedAddress()) &&
        Boolean(selectedPaymentMethod) &&
        ui.cart.read().length > 0;
      cartCheckoutLink.classList.toggle("is-disabled", !ready);
      cartCheckoutLink.setAttribute("aria-disabled", String(!ready));
      cartCheckoutLink.title = ready
        ? ""
        : !selectedAddress()
          ? "ابتدا آدرس تحویل را انتخاب کنید"
          : "ابتدا نحوه پرداخت را انتخاب کنید";
    }
    if (!payment) return;
    payment.disabled =
      !ui.cart.read().length || !selectedAddress() || !selectedPaymentMethod;
    ui.setMessage(document.querySelector("#paymentStatus"), "");
  }

  function renderPaymentMethod() {
    document.querySelectorAll('[name="paymentMethod"]').forEach((radio) => {
      radio.checked = radio.value === selectedPaymentMethod;
    });
    const checkoutMethod = document.querySelector("#checkoutPaymentMethod");
    if (checkoutMethod) {
      checkoutMethod.textContent = selectedPaymentMethod
        ? `پرداخت از طریق ${paymentMethods[selectedPaymentMethod]}`
        : "روش پرداختی انتخاب نشده است؛ به سبد خرید بازگردید.";
    }
  }

  function renderPreview() {
    const address = selectedAddress();
    const addressSection = document.querySelector(".cart-address-section");
    addressSection?.classList.toggle("is-selected", Boolean(address));
    const selectedBadge = addressSection?.querySelector(
      ".address-selected-badge",
    );
    if (selectedBadge)
      selectedBadge.setAttribute("aria-hidden", String(!address));
    if (preview) {
      preview.replaceChildren();
      if (address) preview.append(addressMarkup(address));
      else {
        preview.textContent = addresses.length
          ? "آدرس تحویل را از فهرست انتخاب کنید."
          : "برای ثبت اولین آدرس، دکمه + را بزنید.";
      }
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
          `${address.title} — ${address.city}، ${address.street}`,
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
      .querySelector(".address-form-grid")
      .append(form.querySelector('[name="postal"]').closest("label"));
    select.addEventListener("change", () => {
      selected = select.value;
      ui.storage.set(selectedKey, selected);
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
    form.addEventListener("submit", (event) => {
      event.preventDefault();
      const address = Object.fromEntries(
        [...new FormData(form)].map(([key, value]) => [key, value.trim()]),
      );
      if (Object.values(address).some((value) => !value)) {
        setFormStatus("لطفاً همه مشخصات آدرس را کامل کنید.", true);
        return;
      }
      address.id = crypto.randomUUID();
      addresses.push(address);
      if (!ui.storage.set(addressKey, JSON.stringify(addresses))) {
        addresses.pop();
        setFormStatus(
          "ذخیره آدرس ممکن نشد. دسترسی مرورگر به فضای ذخیره‌سازی را بررسی کنید.",
          true,
        );
        return;
      }
      selected = address.id;
      ui.storage.set(selectedKey, selected);
      renderAddresses();
      dialog.close();
    });
    cartCheckoutLink?.addEventListener("click", (event) => {
      if (cartCheckoutLink.getAttribute("aria-disabled") !== "true") return;
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
    radio.addEventListener("change", () => {
      if (!radio.checked || !Object.hasOwn(paymentMethods, radio.value)) return;
      selectedPaymentMethod = radio.value;
      ui.storage.set(paymentMethodKey, selectedPaymentMethod);
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
    payment.addEventListener("click", () => {
      syncPayment();
      if (payment.disabled) return;
      ui.setMessage(
        document.querySelector("#paymentStatus"),
        "درگاه پرداخت هنوز فعال نیست؛ سفارشی ثبت یا پرداخت نشده است. برای نهایی کردن خرید با 09369455457 تماس بگیرید.",
        "warning",
      );
    });
    document.addEventListener("hn:cart-change", syncPayment);
  }

  window.addEventListener("storage", () => window.location.reload());
  renderAddresses();
  renderPaymentMethod();
}));
