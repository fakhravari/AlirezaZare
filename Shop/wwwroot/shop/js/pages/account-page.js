window.HNUI.ready(() => window.HNUI.mount("account", document, () => {
    const ui = window.HNUI;
    const read = (key) => {
        const v = ui.storage.json(key, []);
        return Array.isArray(v) ? v.filter((x) => x && typeof x === "object") : [];
    };
    const esc = (v) => ui.escapeHtml(v);
    const addressKey = "hn-delivery-addresses";
    const couponPageSize = 5;
    const couponHistory = [
        {
            code: "BAZAR10",
            discountPercent: 10,
            active: true,
            expiresOn: "۲۹ اسفند ۱۴۰۵",
            uses: [
                { invoice: "HN-۱۴۰۵۰۵۹۲", date: "۳۱ مرداد ۱۴۰۵", dateTime: "2026-08-22" },
                { invoice: "HN-۱۴۰۵۰۵۴۰", date: "۸ مرداد ۱۴۰۵", dateTime: "2026-07-30" },
            ],
        },
        {
            code: "TOOLS15",
            discountPercent: 15,
            active: true,
            expiresOn: "۳۰ آبان ۱۴۰۵",
            uses: [
                { invoice: "HN-۱۴۰۵۰۶۲۱", date: "۱۷ شهریور ۱۴۰۵", dateTime: "2026-09-08" },
                { invoice: "HN-۱۴۰۵۰۴۹۸", date: "۳۰ تیر ۱۴۰۵", dateTime: "2026-07-21" },
            ],
        },
        {
            code: "WELCOME8",
            discountPercent: 8,
            active: true,
            expiresOn: "۳۰ آذر ۱۴۰۵",
            uses: [
                { invoice: "HN-۱۴۰۵۰۶۱۸", date: "۱۴ شهریور ۱۴۰۵", dateTime: "2026-09-05" },
            ],
        },
        {
            code: "NIMROOZ12",
            discountPercent: 12,
            active: false,
            expiresOn: "۳۱ مرداد ۱۴۰۵",
            uses: [
                { invoice: "HN-۱۴۰۵۰۵۸۵", date: "۲۶ مرداد ۱۴۰۵", dateTime: "2026-08-17" },
            ],
        },
        {
            code: "SUMMER8",
            discountPercent: 8,
            active: false,
            expiresOn: "۳۱ مرداد ۱۴۰۵",
            uses: [
                { invoice: "HN-۱۴۰۵۰۵۷۲", date: "۱۷ مرداد ۱۴۰۵", dateTime: "2026-08-08" },
            ],
        },
        {
            code: "AUTUMN5",
            discountPercent: 5,
            active: false,
            expiresOn: "۳۱ تیر ۱۴۰۵",
            uses: [
                { invoice: "HN-۱۴۰۵۰۴۶۱", date: "۱۷ تیر ۱۴۰۵", dateTime: "2026-07-08" },
            ],
        },
        {
            code: "ORDER7",
            discountPercent: 7,
            active: false,
            expiresOn: "۳۱ تیر ۱۴۰۵",
            uses: [
                { invoice: "HN-۱۴۰۵۰۴۲۲", date: "۱ تیر ۱۴۰۵", dateTime: "2026-06-22" },
            ],
        },
        {
            code: "AMAN15",
            discountPercent: 15,
            active: false,
            expiresOn: "۳۱ خرداد ۱۴۰۵",
            uses: [
                { invoice: "HN-۱۴۰۵۰۳۸۹", date: "۱۹ خرداد ۱۴۰۵", dateTime: "2026-06-09" },
            ],
        },
        {
            code: "BAHAR15",
            discountPercent: 15,
            active: false,
            expiresOn: "۳۱ تیر ۱۴۰۵",
            uses: [],
        },
        {
            code: "OLD20",
            discountPercent: 20,
            active: false,
            expiresOn: "۳۱ اردیبهشت ۱۴۰۵",
            uses: [],
        },
    ];
    const activeCoupons = couponHistory.filter((coupon) => coupon.active);
    const formatPersianDate = (value) => {
        const date = new Date(value);
        if (!Number.isFinite(date.getTime())) return "";
        return new Intl.DateTimeFormat("fa-IR-u-ca-persian", {
            day: "numeric",
            month: "short",
            year: "numeric",
        }).format(date);
    };
    const priceWatchKey = "hn-price-watches-demo";
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
        .querySelector('[name="phone"]')
        .closest("label")
        .after(form.querySelector('[name="postal"]').closest("label"));
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
                    (a) => {
                        const region = [a.province, a.city].filter(Boolean).join("، ");
                        return `<article class="flow-card address-account-card"><h3>${esc(a.title)} ${a.id === selected ? "<small>آدرس انتخابی</small>" : ""}</h3><strong>${esc(a.recipient)} · ${esc(a.phone)}</strong><p>${esc(region)}، ${esc(a.street)}</p><p>کد پستی: ${esc(a.postal)}</p><div class="account-actions"><button data-select="${esc(a.id)}" ${a.id === selected ? "disabled" : ""}>انتخاب برای تحویل</button><button data-edit="${esc(a.id)}">ویرایش</button><button data-delete="${esc(a.id)}">حذف آدرس</button></div></article>`;
                    },
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
                "country",
                "province",
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
        if (["title", "recipient", "phone", "country", "countryId", "province", "provinceId", "city", "cityId", "street", "postal"].some((key) => !address[key])) {
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
        renderCoupons();
    }
    function renderCoupons(moveFocus = false) {
        const appliedCode = ui.storage.get("hn-demo-coupon", "");
        const overviewHost = document.querySelector("#activeCouponsList");
        if (overviewHost) {
            overviewHost.innerHTML = activeCoupons
                .map(({ code, discountPercent }) =>
                    `<article class="overview-coupon-card"><div class="overview-coupon-copy"><bdi class="overview-coupon-code" dir="ltr">${esc(code)}</bdi><strong>${ui.formatNumber(discountPercent)}٪ تخفیف خرید</strong><small>${appliedCode === code ? "در سبد خرید اعمال شده" : "قابل استفاده در سبد خرید"}</small></div><a href="/Cart" aria-label="استفاده از کد ${esc(code)}"><span>استفاده</span><i class="bi bi-arrow-left" aria-hidden="true"></i></a></article>`,
                )
                .join("");
        }

        const profileHost = document.querySelector("#profileCouponsList");
        if (!profileHost) return;
        const pagination = document.querySelector("#profileCouponsPagination");
        const pageStatus = pagination?.querySelector("p");
        const pageNav = pagination?.querySelector("nav");
        const pageSize = couponPageSize;
        const pageCount = Math.max(1, Math.ceil(couponHistory.length / pageSize));
        const url = new URL(window.location.href);
        const requestedPage = Number(url.searchParams.get("couponPage"));
        const page = Number.isSafeInteger(requestedPage) && requestedPage > 0
            ? Math.min(requestedPage, pageCount)
            : 1;
        const start = (page - 1) * pageSize;

        profileHost.innerHTML = couponHistory.slice(start, start + pageSize).map((coupon) => {
            const uses = Array.isArray(coupon.uses) ? coupon.uses : [];
            const stateClass = coupon.active ? "is-active" : "is-inactive";
            const stateLabel = coupon.active ? "فعال" : "غیرفعال";
            const stateIcon = coupon.active ? "bi-check-circle-fill" : "bi-clock-history";
            const expiryLabel = coupon.active ? "قابل استفاده تا" : "منقضی‌شده در";
            const invoices = uses.length
                ? `<div class="profile-coupon-invoice-list">${uses.map((use) =>
                    `<div class="profile-coupon-invoice"><span class="profile-coupon-invoice-number"><i class="bi bi-receipt" aria-hidden="true"></i><bdi dir="ltr">${esc(use.invoice)}</bdi></span><time datetime="${esc(use.dateTime)}">${esc(use.date)}</time></div>`,
                ).join("")}</div>`
                : '<p class="profile-coupon-no-invoices">هنوز از این کد در سفارشی استفاده نشده است.</p>';
            return `<article class="profile-coupon-card ${stateClass}"><header class="profile-coupon-header"><div class="profile-coupon-identity"><bdi class="profile-coupon-code" dir="ltr">${esc(coupon.code)}</bdi><div><strong>${ui.formatNumber(coupon.discountPercent)}٪ تخفیف خرید</strong><small>${expiryLabel} ${esc(coupon.expiresOn)}</small></div></div><span class="profile-coupon-status ${stateClass}"><i class="bi ${stateIcon}" aria-hidden="true"></i>${stateLabel}</span></header><section class="profile-coupon-usage" aria-label="سوابق استفاده از کد ${esc(coupon.code)}"><div class="profile-coupon-usage-heading"><strong><i class="bi bi-receipt-cutoff" aria-hidden="true"></i>فاکتورهای استفاده‌شده</strong><span>تعداد استفاده: <b>${ui.formatNumber(uses.length)}</b></span></div>${invoices}</section></article>`;
        }).join("");

        if (!pagination || !pageStatus || !pageNav) return;
        pagination.hidden = pageCount <= 1;
        pageStatus.textContent = couponHistory.length
            ? `${ui.formatNumber(start + 1)} تا ${ui.formatNumber(Math.min(start + pageSize, couponHistory.length))} از ${ui.formatNumber(couponHistory.length)} کد`
            : "کدی ثبت نشده است";
        pageNav.replaceChildren();
        pageNav.hidden = pageCount <= 1;

        const addPageButton = (label, target, disabled = false) => {
            const button = document.createElement("button");
            button.type = "button";
            button.textContent = label;
            button.disabled = disabled;
            button.setAttribute("aria-label", target < page ? "صفحهٔ قبل" : target > page ? "صفحهٔ بعد" : `صفحه ${label}`);
            if (target === page && label === ui.formatNumber(page))
                button.setAttribute("aria-current", "page");
            button.addEventListener("click", () => {
                const next = new URL(window.location.href);
                if (target === 1) next.searchParams.delete("couponPage");
                else next.searchParams.set("couponPage", String(target));
                if (next.href !== window.location.href)
                    history.pushState(null, "", next);
                renderCoupons(true);
            });
            pageNav.append(button);
        };

        addPageButton("قبلی", page - 1, page === 1);
        for (let target = 1; target <= pageCount; target++)
            addPageButton(ui.formatNumber(target), target);
        addPageButton("بعدی", page + 1, page === pageCount);

        if (window.location.pathname.replace(/\/+$/, "") === "/profile/coupons") {
            const normalizedUrl = new URL(window.location.href);
            if (page === 1) normalizedUrl.searchParams.delete("couponPage");
            else normalizedUrl.searchParams.set("couponPage", String(page));
            if (normalizedUrl.href !== window.location.href)
                history.replaceState(null, "", normalizedUrl);
        }
        if (moveFocus)
            pageNav.querySelector('[aria-current="page"]')?.focus({ preventScroll: true });
    }
    window.addEventListener("popstate", () => renderCoupons());
    function renderPriceAlerts() {
        const host = document.querySelector("#priceAlertsList");
        if (!host) return;
        const watches = read(priceWatchKey);
        if (!watches.length) {
            host.innerHTML = '<div class="flow-card flow-empty empty-state-card"><i class="bi bi-bell-slash" aria-hidden="true"></i><h3>پیگیری قیمتی ثبت نشده</h3><p>در صفحهٔ محصول، «به من اطلاع بده» را بزنید تا پیگیری اینجا نمایش داده شود.</p><a class="account-section-action" href="/search"><i class="bi bi-search" aria-hidden="true"></i>جست‌وجوی محصولات</a></div>';
            return;
        }
        host.innerHTML = watches.map((watch, index) => {
            const product = window.HNCatalog?.find(watch.productId);
            const productName = product?.name || `محصول ${watch.productId || "نامشخص"}`;
            const variants = watch.scope === "variant" && watch.variants && typeof watch.variants === "object"
                ? Object.entries(watch.variants).map(([key, value]) => {
                    const feature = product?.variants?.[key];
                    const option = feature?.options?.find((item) => item.value === value);
                    return `${feature?.label || key}: ${option?.label || value}`;
                }).join("، ")
                : "";
            const hasVariants = Object.values(product?.variants || {}).some(
                (feature) => Array.isArray(feature?.options) && feature.options.length,
            );
            const scopeLabel = watch.scope === "variant"
                ? (variants || "ویژگی انتخاب‌شده")
                : hasVariants
                    ? "همهٔ قیمت‌های محصول"
                    : "بدون ویژگی";
            const scopeIcon = watch.scope === "variant"
                ? "sliders"
                : hasVariants
                    ? "tags"
                    : "box-seam";
            const registeredDate = formatPersianDate(watch.createdAt);
            const registeredPrice = typeof watch.priceAtRegistration === "number"
                ? watch.priceAtRegistration
                : NaN;
            const trackingDetails = [
                registeredDate ? { icon: "calendar3", label: "ثبت", value: registeredDate } : null,
                Number.isFinite(registeredPrice) && registeredPrice >= 0
                    ? { icon: "currency-exchange", label: "قیمت ثبت‌شده", value: ui.formatNumber(registeredPrice) + " تومان" }
                    : null,
                typeof watch.stockAtRegistration === "string" && watch.stockAtRegistration.trim()
                    ? { icon: "boxes", label: "موجودی ثبت‌شده", value: watch.stockAtRegistration.trim() }
                    : null,
            ].filter(Boolean);
            const href = product ? window.HNProductUrl?.(product) || "/search" : "/search";
            return `<article class="profile-price-alert-card"><a class="profile-price-alert-product" href="${esc(href)}"><span class="profile-price-alert-icon"><i class="bi bi-bell" aria-hidden="true"></i></span><span><strong>${esc(productName)}</strong><span class="profile-price-alert-meta"><span class="profile-price-alert-scope"><i class="bi bi-${scopeIcon}" aria-hidden="true"></i><span>${esc(scopeLabel)}</span></span><span class="profile-price-alert-details">${trackingDetails.map((detail) => `<small class="profile-price-alert-detail"><i class="bi bi-${detail.icon}" aria-hidden="true"></i><span class="profile-price-alert-detail-label">${esc(detail.label)}</span><span class="profile-price-alert-detail-value">${esc(detail.value)}</span></small>`).join("")}</span></span></span></a><button class="profile-price-alert-delete" type="button" data-delete-price-alert="${index}" aria-label="حذف پیگیری ${esc(productName)}"><i class="bi bi-trash3" aria-hidden="true"></i><span>حذف</span></button></article>`;
        }).join("");
        host.querySelectorAll("[data-delete-price-alert]").forEach((button) => {
            button.addEventListener("click", async () => {
                const current = read(priceWatchKey);
                const index = Number(button.dataset.deletePriceAlert);
                if (!Number.isInteger(index) || index < 0 || index >= current.length) return;
                const selectedWatch = JSON.stringify(current[index]);
                const productName = window.HNCatalog?.find(current[index].productId)?.name || "این محصول";
                if (typeof window.confirmFormSubmission !== "function") return;
                const confirmed = await window.confirmFormSubmission({
                    title: "حذف پیگیری قیمت؟",
                    message: `پیگیری «${productName}» از فهرست حذف می‌شود. ادامه می‌دهید؟`,
                    confirmText: "بله، حذف شود",
                });
                if (!confirmed) return;

                const latest = read(priceWatchKey);
                const latestIndex = latest.findIndex((watch) => JSON.stringify(watch) === selectedWatch);
                if (latestIndex < 0) {
                    renderPriceAlerts();
                    ui.setMessage(status, "این پیگیری قبلاً حذف شده است.", "info");
                    return;
                }
                latest.splice(latestIndex, 1);
                if (!save(priceWatchKey, latest)) {
                    ui.setMessage(status, "حذف پیگیری ذخیره نشد. دوباره تلاش کنید.", "error");
                    return;
                }
                renderPriceAlerts();
                ui.setMessage(status, "پیگیری قیمت حذف شد.", "success");
                requestAnimationFrame(() => {
                    const nextDelete = host.querySelector(`[data-delete-price-alert="${Math.min(index, latest.length - 1)}"]`);
                    (nextDelete || host.querySelector("a"))?.focus({ preventScroll: true });
                });
            });
        });
    }
    function navigate() {
        renderOverview();
        const routeSection = {
            orders: "orders",
            addresses: "addresses",
            favorites: "favorites",
            coupons: "coupons",
            pricealerts: "price-alerts",
        };
        const routeName = location.pathname
            .split("/")
            .filter(Boolean)
            .pop()
            ?.toLowerCase();
        const hashSection = location.hash.slice(1);
        const section =
            routeSection[routeName] ||
            (["orders", "addresses", "favorites", "coupons", "price-alerts"].includes(hashSection)
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
        renderPriceAlerts();
    });
    document.addEventListener("hn:lists-change", () => {
        renderOverview();
        renderPriceAlerts();
    });
    renderAddresses();
    renderOverview();
    renderPriceAlerts();
    navigate();
}));
