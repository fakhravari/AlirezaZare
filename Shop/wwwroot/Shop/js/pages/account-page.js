document.addEventListener("DOMContentLoaded", () => {
  const ui = window.HNUI;
  const read = (key) => {
    const v = ui.storage.json(key, []);
    return Array.isArray(v) ? v.filter((x) => x && typeof x === "object") : [];
  };
  const esc = (v) => ui.escapeHtml(v);
  const addressKey = "hn-delivery-addresses";
  const dialog = document.querySelector("#addressDialog"),
    form = document.querySelector("#addressForm");
  form
    .querySelector(".address-form-grid")
    .append(form.querySelector('[name="postal"]').closest("label"));
  let editing = null;
  const status = document.querySelector("#accountStatus");
  const save = (key, value) => {
    const ok = ui.storage.set(key, JSON.stringify(value));
    if (!ok)
      status.textContent =
        "ذخیره اطلاعات ممکن نشد؛ تنظیمات مرورگر را بررسی کنید.";
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
      : '<div class="flow-card flow-empty"><i class="bi bi-geo-alt"></i><h3>هنوز آدرسی ندارید</h3><p>آدرس جدید را اضافه کنید تا در پیش‌فاکتور هم قابل انتخاب باشد.</p></div>';
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
            status.textContent = "آدرس حذف شد.";
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
    document.querySelector("#addressFormStatus").textContent = "";
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
  document.querySelector("#closeAddress").onclick = () => dialog.close();
  dialog.addEventListener("close", () => {
    document.documentElement.classList.remove("address-modal-open");
    document.querySelector("#newAddress").focus();
  });
  form.onsubmit = (e) => {
    e.preventDefault();
    const address = Object.fromEntries(
      [...new FormData(form)].map(([k, v]) => [k, v.trim()]),
    );
    if (Object.values(address).some((v) => !v)) {
      document.querySelector("#addressFormStatus").textContent =
        "همه فیلدها را کامل کنید.";
      return;
    }
    address.id = editing || crypto.randomUUID();
    const addresses = read(addressKey);
    const next = editing
      ? addresses.map((a) => (a.id === editing ? address : a))
      : [...addresses, address];
    if (!save(addressKey, next)) {
      document.querySelector("#addressFormStatus").textContent =
        "ذخیره آدرس ممکن نشد.";
      return;
    }
    ui.storage.set("hn-selected-address", address.id);
    renderAddresses();
    dialog.close();
    status.textContent = "آدرس ذخیره و برای تحویل انتخاب شد.";
  };
  function renderOverview() {
    const values = {
      statOrders: document.querySelectorAll(".profile-order-card").length,
      statAddresses: read(addressKey).length,
      statFavorites: read("hn-favorites").length,
    };
    Object.entries(values).forEach(([id, value]) => {
      const element = document.querySelector(`#${id}`);
      if (element) element.textContent = ui.formatNumber(value);
    });
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
  window.addEventListener("storage", () => {
    renderAddresses();
    renderOverview();
  });
  renderAddresses();
  renderOverview();
  navigate();
});
