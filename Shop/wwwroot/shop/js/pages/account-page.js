window.HNUI.ready(() => window.HNUI.mount("account", document, () => {
  const ui = window.HNUI;
  const read = (key) => {
    const v = ui.storage.json(key, []);
    return Array.isArray(v) ? v.filter((x) => x && typeof x === "object") : [];
  };
  const esc = (v) => ui.escapeHtml(v);
  const addressKey = "hn-delivery-addresses";
  const dialog = document.querySelector("#addressDialog"),
    form = document.querySelector("#addressForm");
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
  let editing = null;
  const status = document.querySelector("#accountStatus");
  const save = (key, value) => {
    const ok = ui.storage.set(key, JSON.stringify(value));
    if (!ok)
      ui.setMessage(
        status,
        "ذخیره اطلاعات ممکن نشد؛ تنظیمات مرورگر را بررسی کنید.",
        "error",
      );
    return ok;
  };
  function renderAddresses() {
    const addresses = read(addressKey),
      selected = ui.storage.get("hn-selected-address", "");
    const host = document.querySelector("#addressList");
    host.innerHTML = addresses.length
      ? addresses
          .map(
            (a) =>
              `<article class="flow-card address-account-card"><h3>${esc(a.title)} ${a.id === selected ? "<small>آدرس انتخابی</small>" : ""}</h3><strong>${esc(a.recipient)} · ${esc(a.phone)}</strong><p>${a.province ? esc(a.province) + "، " : ""}${esc(a.city)}، ${esc(a.street)}</p><p>کد پستی: ${esc(a.postal)}</p><div class="account-actions"><button data-select="${esc(a.id)}" ${a.id === selected ? "disabled" : ""}>انتخاب برای تحویل</button><button data-edit="${esc(a.id)}">ویرایش</button><button data-delete="${esc(a.id)}">حذف آدرس</button></div></article>`,
          )
          .join("")
      : '<div class="flow-card flow-empty empty-state-card"><i class="bi bi-geo-alt"></i><h3>هنوز آدرسی ندارید</h3><p>آدرس جدید را اضافه کنید تا در پیش‌فاکتور هم قابل انتخاب باشد.</p></div>';
    host.querySelectorAll("[data-select]").forEach(
      (b) =>
        (b.onclick = () => {
          ui.storage.set("hn-selected-address", b.dataset.select);
          renderAddresses();
        }),
    );
    host
      .querySelectorAll("[data-edit]")
      .forEach(
        (b) =>
          (b.onclick = () =>
            openAddress(addresses.find((a) => a.id === b.dataset.edit))),
      );
    host.querySelectorAll("[data-delete]").forEach(
      (b) =>
        (b.onclick = () => {
          if (
            save(
              addressKey,
              addresses.filter((a) => a.id !== b.dataset.delete),
            )
          ) {
            if (selected === b.dataset.delete)
              ui.storage.remove("hn-selected-address");
            renderAddresses();
            ui.setMessage(status, "آدرس حذف شد.", "success");
          }
        }),
    );
  }
  function openAddress(address) {
    form.reset();
    editing = address?.id || null;
    document.querySelector("#addressDialogTitle").textContent = editing
      ? "ویرایش آدرس"
      : "تعریف آدرس جدید";
    setFormStatus("");
    if (address)
      for (const field of [
        "title",
        "recipient",
        "phone",
        "city",
        "street",
        "postal",
      ])
        form.elements[field].value = address[field] || "";
    window.HNAddressFields.set(address);
    dialog.showModal();
    document.documentElement.classList.add("address-modal-open");
  }
  document.querySelector("#newAddress").onclick = () => openAddress();
  document
    .querySelectorAll("[data-open-address]")
    .forEach((button) => button.addEventListener("click", () => openAddress()));
  document.querySelector("#closeAddress").onclick = () => dialog.close();
  dialog.addEventListener("close", () => {
    document.documentElement.classList.remove("address-modal-open");
    document.querySelector("#newAddress").focus();
  });
  form.onsubmit = (e) => {
    e.preventDefault();
    if (!form.reportValidity()) return;
    const address = Object.fromEntries(
      [...new FormData(form)].map(([k, v]) => [k, v.trim()]),
    );
    if (Object.values(address).some((v) => !v)) {
      setFormStatus("همه فیلدها را کامل کنید.", true);
      return;
    }
    address.phone = ui.toEnglishDigits(address.phone).replace(/\D/g, "");
    address.postal = ui.toEnglishDigits(address.postal).replace(/\D/g, "");
    address.id = editing ||
      (window.crypto?.randomUUID?.() ??
        `address-${Date.now()}-${Math.random().toString(36).slice(2)}`);
    const addresses = read(addressKey);
    const next = editing
      ? addresses.map((a) => (a.id === editing ? address : a))
      : [...addresses, address];
    if (!save(addressKey, next)) {
      setFormStatus("ذخیره آدرس ممکن نشد. دوباره تلاش کنید.", true);
      return;
    }
    ui.storage.set("hn-selected-address", address.id);
    renderAddresses();
    renderOverview();
    dialog.close();
    ui.setMessage(status, "آدرس ذخیره و برای تحویل انتخاب شد.", "success");
  };
  function renderOverview() {
    const activeCoupons = [{ code: "HADI10", discountPercent: 10 }];
    const compareList = ui.storage.json("hn-compare", []);
    const compareCount = Array.isArray(compareList)
      ? new Set(
          compareList
            .filter((id) => id !== null && id !== undefined && String(id).trim())
            .map(String),
        ).size
      : 0;
    const values = {
      statOrders: document.querySelectorAll(".profile-order-card").length,
      statAddresses: read(addressKey).length,
      statFavorites: read("hn-favorites").length,
      statCoupons: activeCoupons.length,
      statCompare: compareCount,
    };
    Object.entries(values).forEach(([id, value]) => {
      const element = document.querySelector(`#${id}`);
      if (element) element.textContent = ui.formatNumber(value);
    });
    const couponHost = document.querySelector("#activeCouponsList");
    if (couponHost) {
      const appliedCode = ui.storage.get("hn-demo-coupon", "");
      couponHost.innerHTML = activeCoupons
        .map(
          ({ code, discountPercent }) =>
            `<article class="overview-coupon-card"><div class="overview-coupon-copy"><bdi class="overview-coupon-code" dir="ltr">${esc(code)}</bdi><strong>${ui.formatNumber(discountPercent)}٪ تخفیف خرید</strong><small>${appliedCode === code ? "در سبد خرید اعمال شده" : "قابل استفاده در سبد خرید"}</small></div><a href="/Cart" aria-label="استفاده از کد ${esc(code)}"><span>استفاده</span><i class="bi bi-arrow-left" aria-hidden="true"></i></a></article>`,
        )
        .join("");
    }
  }
  function navigate() {
    renderOverview();
    const routeSection = {
      orders: "orders",
      addresses: "addresses",
      favorites: "favorites",
    };
    const routeName = location.pathname
      .split("/")
      .filter(Boolean)
      .pop()
      ?.toLowerCase();
    const hashSection = location.hash.slice(1);
    const section =
      routeSection[routeName] ||
      (["orders", "addresses", "favorites"].includes(hashSection)
        ? hashSection
        : "overview");
    document
      .querySelectorAll(".account-section")
      .forEach((el) => (el.hidden = el.id !== section));
    document.querySelectorAll(".account-tabs a").forEach((a) => {
      const target = a.pathname.split("/").filter(Boolean).pop()?.toLowerCase();
      const active =
        section === "overview"
          ? target === "profile"
          : routeSection[target] === section;
      if (active) a.setAttribute("aria-current", "page");
      else a.removeAttribute("aria-current");
    });
  }
  window.addEventListener("hashchange", navigate);
  const invoiceDialog = document.getElementById("orderInvoiceDialog");
  const formatToman = (amount) => `${ui.formatNumber(Number(amount) || 0)} تومان`;
  document.querySelectorAll("[data-open-order-invoice]").forEach((button) => {
    button.addEventListener("click", () => {
      const order = button.closest(".profile-order-card");
      if (!order || !invoiceDialog) return;
      invoiceDialog.querySelector("[data-invoice-number]").textContent = order.dataset.orderNumber;
      invoiceDialog.querySelector("[data-invoice-date]").textContent = order.dataset.orderDate;
      invoiceDialog.querySelector("[data-invoice-status]").textContent = order.dataset.orderStatus;
      invoiceDialog.querySelector("[data-invoice-delivery]").textContent = order.dataset.orderDelivery;
      invoiceDialog.querySelector("[data-invoice-count]").textContent = ui.formatNumber(order.dataset.orderCount);
      invoiceDialog.querySelector("[data-invoice-amount]").textContent = formatToman(order.dataset.orderAmount);
      invoiceDialog.querySelector("[data-invoice-total]").textContent = formatToman(order.dataset.orderAmount);
      if (!invoiceDialog.open) invoiceDialog.showModal();
    });
  });
  invoiceDialog?.querySelector("[data-close-order-invoice]")?.addEventListener("click", () => invoiceDialog.close());
  invoiceDialog?.addEventListener("click", (event) => {
    if (event.target === invoiceDialog) invoiceDialog.close();
  });
  window.addEventListener("storage", () => {
    renderAddresses();
    renderOverview();
  });
  document.addEventListener("hn:lists-change", renderOverview);
  renderAddresses();
  renderOverview();
  navigate();
}));
