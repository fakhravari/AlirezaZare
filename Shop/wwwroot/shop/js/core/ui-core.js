(function (window, document) {
  "use strict";
  if (window.HNUI) return;

  // Each module mounts once per root, even when script tags or initialization repeat.
  const mounted = new WeakMap();
  const mount = (name, root, initialize) => {
    if (!root) return false;
    let names = mounted.get(root);
    if (!names) { names = new Set(); mounted.set(root, names); }
    if (names.has(name)) return false;
    names.add(name);
    try { initialize(root); } catch (error) { names.delete(name); throw error; }
    return true;
  };
  const EVENTS = Object.freeze({
    cartChange: "hn:cart-change", cartAdded: "hn:cart-added", cartStockLimit: "hn:cart-stock-limit",
    authChange: "hn:auth-change", authRequired: "hn:auth-required",
    listsChange: "hn:lists-change",
    accountDataChange: "hn:account-data-change",
  });
  const emit = (type, detail) => {
    if (!Object.values(EVENTS).includes(type)) throw new Error("Unknown UI event: " + type);
    if (type === EVENTS.cartChange && !Array.isArray(detail)) throw new TypeError("cart-change expects an array");
    if (type === EVENTS.authChange && typeof detail !== "boolean") throw new TypeError("auth-change expects a boolean");
    if (type === EVENTS.cartAdded && (!detail?.product?.id || !Array.isArray(detail.items)))
      throw new TypeError("cart-added expects {product, items, trigger}");
    if (type === EVENTS.cartStockLimit && (!detail?.product?.id || !Number.isFinite(detail.limit) || !Number.isFinite(detail.remaining)))
      throw new TypeError("cart-stock-limit expects {product, limit, remaining, requested, reason}");
    if (type === EVENTS.authRequired && (!detail || (detail.resume !== null && typeof detail.resume !== "function")))
      throw new TypeError("auth-required expects {resume: function|null}");
    if (type === EVENTS.accountDataChange && detail?.key !== "hn-price-watches-demo")
      throw new TypeError("account-data-change expects a supported account key");
    document.dispatchEvent(new CustomEvent(type, { detail }));
  };

  const STORAGE_KEYS = Object.freeze({
    auth: "hn-demo-auth",
  });
  const storage = window.HNStorage.create({ document, emit });

  const apiConfig = document.querySelector('meta[name="api-base-url"]');
  const apiBaseUrl = new URL(apiConfig?.content || "api/", window.location.href);
  if (apiBaseUrl.origin !== window.location.origin || !apiBaseUrl.pathname.endsWith("/"))
    throw new Error("API base URL must be a same-origin path ending in '/'.");
  const apiTimeoutMs = Number(apiConfig?.dataset?.timeoutMs) || 15000;
  const api = Object.freeze({
    baseUrl: apiBaseUrl.href,
    async request(path, options = {}) {
      let relativePath = String(path);
      while (relativePath.startsWith("/")) relativePath = relativePath.slice(1);
      const url = new URL(relativePath, apiBaseUrl);
      if (url.origin !== apiBaseUrl.origin || !url.pathname.startsWith(apiBaseUrl.pathname))
        throw new TypeError("API request path must stay under the configured API base path.");

      const controller = new window.AbortController();
      const externalSignal = options.signal;
      let timedOut = false;
      const abortFromCaller = () => controller.abort();
      if (externalSignal?.aborted) controller.abort();
      else externalSignal?.addEventListener("abort", abortFromCaller, { once: true });
      const timeout = window.setTimeout(() => {
        timedOut = true;
        controller.abort();
      }, apiTimeoutMs);

      try {
        return await window.fetch(url.href, {
          ...options,
          cache: "no-store",
          credentials: "same-origin",
          headers: { Accept: "application/json", ...options.headers },
          signal: controller.signal,
        });
      } catch (error) {
        if (timedOut) {
          const timeoutError = new Error("API request timed out.");
          timeoutError.name = "TimeoutError";
          throw timeoutError;
        }
        throw error;
      } finally {
        window.clearTimeout(timeout);
        externalSignal?.removeEventListener("abort", abortFromCaller);
      }
    },
  });

  const ready = (callback) => {
    if (document.readyState === "loading")
      document.addEventListener("DOMContentLoaded", callback, { once: true });
    else callback();
  };

  const payment = Object.freeze({
    async start(orderId) {
      if (!Number.isSafeInteger(orderId) || orderId < 1) throw new TypeError("Invalid order ID.");
      const tokenResponse = await api.request("payment/token");
      if (!tokenResponse.ok) throw new Error("دریافت مجوز پرداخت انجام نشد.");
      const { token } = await tokenResponse.json();
      if (typeof token !== "string" || !token) throw new Error("مجوز پرداخت معتبر نیست.");
      const response = await api.request(`payment/orders/${orderId}/start`, {
        method: "POST", headers: { RequestVerificationToken: token },
      });
      const result = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(result.message || "درگاه پرداخت در دسترس نیست؛ سفارش در انتظار پرداخت است.");
      const redirect = new URL(result.redirectUrl);
      if (redirect.protocol !== "https:" || redirect.username || redirect.password) throw new Error("نشانی درگاه معتبر نیست.");
      return redirect.href;
    },
  });

  document.addEventListener("click", (event) => {
    const link = event.target.closest?.("a.skip-link[href^='#']");
    if (!link) return;
    const target = document.getElementById(decodeURIComponent(link.hash.slice(1)));
    if (target) window.setTimeout(() => target.focus(), 0);
  });

  const cart = window.HNCartModel.create({
    document, emit,
    requireLogin: (resume) => auth.requireLogin(resume),
    formatNumber: (value) => formatNumber(value),
    escapeHtml: (value) => escapeHtml(value),
  });

  const auth = {
    isLoggedIn: () =>
      ["1", "true"].includes(storage.get(STORAGE_KEYS.auth, "")),
    requireLogin(resume) {
      if (auth.isLoggedIn()) return true;
      emit(EVENTS.authRequired, { resume: typeof resume === "function" ? resume : null });
      return false;
    },
    login() {
      storage.set(STORAGE_KEYS.auth, "1");
      emit("hn:auth-change", true);
    },
    logout() {
      storage.clear();
      cart.resetAccount();
      emit("hn:auth-change", false);
    },
  };

  document.addEventListener(EVENTS.cartChange, (event) => {
    if (!auth.isLoggedIn() || !cart.isAccountReady() || cart.isRestoringAccount() || !Array.isArray(event.detail)) return;
    const items = event.detail.map((item) => ({
      productId: Number(String(item.id).replace(/:.*$/, "")),
      variantId: item.variantId || null,
      quantity: Number(item.quantity),
      salesPlanId: String(item.salesPlanId || "regular"),
      options: item.options || {},
    })).filter((item) => Number.isSafeInteger(item.productId) && item.productId > 0);
    void api.request("account/cart", { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ items }) })
      .then(async (response) => {
        if (response.ok) return;
        const state = await api.request("account/state");
        if (state.ok) cart.restoreAccountLines((await state.json()).cart);
      }).catch(() => {});
  });
  document.addEventListener(EVENTS.listsChange, () => {
    if (!auth.isLoggedIn()) return;
    for (const kind of ["favorites", "compare"]) {
      const ids = storage.json(kind === "favorites" ? "hn-favorites" : "hn-compare", []);
      const productIds = (Array.isArray(ids) ? ids : []).map((id) => Number(id)).filter((id) => Number.isSafeInteger(id) && id > 0);
      const key = kind === "favorites" ? "hn-favorites" : "hn-compare";
      void api.request(`account/lists/${kind}`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ productIds }) })
        .then(async (response) => {
          if (response.ok) { storage.clearLegacy(key); return; }
          const state = await api.request("account/state");
          if (state.ok) {
            const account = await state.json();
            storage.hydrate(key, JSON.stringify((account[kind] || []).map((item) => String(item.id))));
          }
        })
        .catch(() => {});
    }
  });
  document.addEventListener(EVENTS.accountDataChange, (event) => {
    if (!auth.isLoggedIn()) return;
    const items = storage.json(event.detail.key, []);
    void api.request("account/price-alerts", { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ items: Array.isArray(items) ? items : [] }) })
      .then(async (response) => {
        if (response.ok) { storage.clearLegacy(event.detail.key); return; }
        const state = await api.request("account/state");
        if (state.ok) storage.hydrate(event.detail.key, JSON.stringify((await state.json()).priceAlerts || []));
      })
      .catch(() => {});
  });

  let lockCount = 0;
  const scrollLock = {
    lock() {
      lockCount += 1;
      document.documentElement.classList.add("ui-scroll-locked");
    },
    unlock(force = false) {
      lockCount = force ? 0 : Math.max(0, lockCount - 1);
      if (!lockCount)
        document.documentElement.classList.remove("ui-scroll-locked");
    },
  };

  const focusableSelector =
    'a[href],button:not([disabled]),input:not([disabled]),select:not([disabled]),textarea:not([disabled]),[tabindex]:not([tabindex="-1"])';
  const dialog = {
    activate(element, trigger) {
      if (!element) return;
      element.__uiTrigger = trigger || document.activeElement;
      element.setAttribute("aria-hidden", "false");
      scrollLock.lock();
      window.setTimeout(
        () => element.querySelector(focusableSelector)?.focus(),
        40,
      );
    },
    deactivate(element) {
      if (!element) return;
      element.setAttribute("aria-hidden", "true");
      scrollLock.unlock();
      const trigger = element.__uiTrigger;
      if (trigger && document.contains(trigger))
        window.setTimeout(() => trigger.focus(), 0);
      element.__uiTrigger = null;
    },
    trap(event, element) {
      if (event.key !== "Tab" || !element) return;
      const items = [...element.querySelectorAll(focusableSelector)].filter(
        (item) => item.offsetParent !== null,
      );
      if (!items.length) return;
      const first = items[0];
      const last = items[items.length - 1];
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    },
  };

  const enhance = (root = document) => {
    root.querySelectorAll("button").forEach((button) => {
      const hasText = button.textContent.trim().length > 0;
      if (!hasText && !button.hasAttribute("aria-label"))
        button.setAttribute("aria-label", "کنترل رابط کاربری");
    });
    root
      .querySelectorAll('[role="dialog"], aside[aria-modal="true"]')
      .forEach((element) => {
        if (!element.hasAttribute("aria-hidden"))
          element.setAttribute("aria-hidden", "true");
      });

    const badgeSelector =
      ".badge-best, .badge-new, .badge-offer, .rail-image > span, .search-product-image > span";
    let scopes =
      root === document
        ? [...document.querySelectorAll("section")]
        : [...root.querySelectorAll("section")];
    if (!scopes.length) scopes = [root];
    scopes.forEach((scope) => {
      scope.querySelectorAll(badgeSelector).forEach((badge, index) => {
        badge.classList.remove(
          "badge-tone-1",
          "badge-tone-2",
          "badge-tone-3",
          "badge-tone-4",
        );
        badge.classList.add(`badge-tone-${(index % 4) + 1}`);
      });
    });
  };

  const toPersianDigits = (value) =>
    String(value).replace(/[0-9]/g, (digit) => "۰۱۲۳۴۵۶۷۸۹"[digit]);

  const persianDigits =
    "\u06F0\u06F1\u06F2\u06F3\u06F4\u06F5\u06F6\u06F7\u06F8\u06F9";
  const arabicDigits =
    "\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669";
  const toEnglishDigits = (value) =>
    String(value)
      .replace(/[\u06F0-\u06F9]/g, (digit) =>
        String(persianDigits.indexOf(digit)),
      )
      .replace(/[\u0660-\u0669]/g, (digit) =>
        String(arabicDigits.indexOf(digit)),
      );
  const formatNumber = (value) => Number(value).toLocaleString("fa-IR");
  const normalizeSearchText = (value) => String(value ?? "")
    .normalize("NFKC")
    .toLocaleLowerCase("fa-IR")
    .replace(/[يى]/g, "ی")
    .replace(/ك/g, "ک")
    .replace(/[\u200c\u200f\u202a-\u202e]/g, "")
    .replace(/\s+/g, " ")
    .trim();
  const escapeHtml = (value) =>
    String(value ?? "").replace(
      /[&<>"']/g,
      (character) =>
        ({
          "&": "&amp;",
          "<": "&lt;",
          ">": "&gt;",
          '"': "&quot;",
          "'": "&#39;",
        })[character],
    );
  const setMessageType = (element, type = "") => {
    if (!element) return;
    const validTypes = ["success", "warning", "error", "info"];
    const normalizedType = validTypes.includes(type) ? type : "";
    validTypes.forEach((item) =>
      element.classList.toggle(`hn-message--${item}`, item === normalizedType),
    );
    if (normalizedType) {
      element.dataset.messageType = normalizedType;
      element.setAttribute(
        "role",
        normalizedType === "error" ? "alert" : "status",
      );
    } else {
      delete element.dataset.messageType;
    }
  };
  const setMessage = (element, message, type = "info") => {
    if (!element) return;
    element.textContent = message;
    setMessageType(element, message ? type : "");
  };
  const showToast = (message, type = "info") => {
    const searchToast = document.querySelector("#searchToast");
    const toast = searchToast || document.querySelector("#liveToast");
    if (!toast) return false;
    const body = searchToast ? toast : toast.querySelector(".toast-body");
    if (!body) return false;
    setMessageType(toast, type);
    const messageIcons = {
      success: "bi-check-circle-fill",
      warning: "bi-exclamation-triangle-fill",
      error: "bi-x-circle-fill",
      info: "bi-info-circle-fill",
    };
    const icon = document.createElement("i");
    icon.className = `bi ${messageIcons[type] || messageIcons.info} hn-toast-icon`;
    icon.setAttribute("aria-hidden", "true");
    const text = document.createElement("span");
    text.className = "hn-toast-text";
    text.textContent = message;
    body.replaceChildren(icon, text);
    if (searchToast) {
      window.clearTimeout(toast._hnMessageTimer);
      toast.classList.add("show");
      toast._hnMessageTimer = window.setTimeout(
        () => toast.classList.remove("show"),
        2200,
      );
    } else if (window.bootstrap?.Toast) {
      window.bootstrap.Toast.getOrCreateInstance(toast).show();
    } else {
      toast.classList.add("show");
      window.clearTimeout(toast._hnMessageTimer);
      toast._hnMessageTimer = window.setTimeout(
        () => toast.classList.remove("show"),
        4000,
      );
    }
    return true;
  };

  const localizeVisibleDigits = (root = document.body) => {
    if (!root) return;
    const excluded =
      "SCRIPT, STYLE, NOSCRIPT, TEXTAREA, INPUT, SELECT, OPTION, CODE, PRE";
    const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
    const textNodes = [];
    let node;
    while ((node = walker.nextNode())) {
      const parent = node.parentElement;
      if (!parent || parent.closest(excluded)) continue;
      textNodes.push(node);
    }
    textNodes.forEach((textNode) => {
      textNode.nodeValue = toPersianDigits(textNode.nodeValue);
    });
  };

  const observeVisibleDigits = () => {
    localizeVisibleDigits();
    const observer = new MutationObserver((mutations) => {
      mutations.forEach((mutation) => {
        mutation.addedNodes.forEach((addedNode) => {
          if (
            addedNode.nodeType === Node.ELEMENT_NODE ||
            addedNode.nodeType === Node.TEXT_NODE
          )
            localizeVisibleDigits(addedNode);
        });
      });
    });
    observer.observe(document.body, { childList: true, subtree: true });
  };

  document.addEventListener("DOMContentLoaded", () => {
    enhance();
    observeVisibleDigits();
    const backToTop = document.getElementById("backToTop");
    if (backToTop) {
      const updateBackToTop = () => {
        const visible = window.scrollY > 500;
        backToTop.classList.toggle("is-visible", visible);
        backToTop.setAttribute("aria-hidden", String(!visible));
        backToTop.tabIndex = visible ? 0 : -1;
      };
      backToTop.addEventListener("click", () => {
        window.scrollTo({
          top: 0,
          behavior: window.matchMedia("(prefers-reduced-motion: reduce)")
            .matches
            ? "auto"
            : "smooth",
        });
      });
      window.addEventListener("scroll", updateBackToTop, { passive: true });
      updateBackToTop();
    }
  });
  document.addEventListener("click", async (event) => {
    const button = event.target.closest(".add-cart[data-id]");
    if (!button || button._cartQuotePending) return;
    if (!auth.requireLogin(() => button.click())) return;
    const productId = String(button.dataset.id);
    const salesPlanId = button.dataset.salesPlanId || "regular";
    const product = (window.HNProducts || []).find((item) => String(item.id) === productId);
    if (!product) return;
    let salesPlanData = {};
    try { salesPlanData = JSON.parse(document.querySelector("#storeSalesPlans")?.textContent || "{}"); } catch { salesPlanData = {}; }
    const inventoryVariants = (salesPlanData.inventoryVariants || []).filter((variant) => Number(variant.productId) === Number(productId));
    const configuredPriceOptions = salesPlanData.priceOptions?.[Number(productId)] || [];
    if (inventoryVariants.length || configuredPriceOptions.length) {
      window.location.href = window.HNProductUrl?.(product) || `/products/${productId}`;
      return;
    }
    const priceOptions = {};
    let salesPlanSnapshot = null;
    if (salesPlanId !== "regular") {
      if (!auth.requireLogin(() => button.click())) return;
      const accessoryIds = (product.accessories || []).filter((item) => item.required).map((item) => item.id);
      button._cartQuotePending = true;
      button.disabled = true;
      try {
        const response = await api.request("store/sales-plans/quote", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ productId: Number(productId), salesPlanId, quantity: 1, variantId: null, priceOptions, accessoryIds }),
        });
        if (!response.ok) throw new Error("quote unavailable");
        const quote = await response.json();
        if (quote.productId !== Number(productId) || quote.salesPlanId !== salesPlanId || !Number.isFinite(Number(quote.unitAmount)))
          throw new Error("invalid quote");
        salesPlanSnapshot = {
          id: quote.salesPlanId, name: quote.planName, code: quote.salesPlanCode,
          label: button.dataset.salesPlanLabel || null, unitAmount: Number(quote.unitAmount),
          accessoryPriceAmounts: quote.accessoryAmounts, stockQuantity: Number(button.dataset.stockQuantity),
          maxPurchaseQuantity: Number(button.dataset.maxPurchaseQuantity), currency: quote.currency,
          quotedAtUtc: quote.quotedAtUtc,
        };
      } catch {
        showToast("دریافت قیمت طرح ممکن نشد؛ دوباره تلاش کنید یا جزئیات محصول را ببینید.", "warning");
        return;
      } finally {
        button._cartQuotePending = false;
        button.disabled = false;
      }
    }
    const quantityBefore = cart
      .read()
      .filter((item) => item.id === productId)
      .reduce((total, item) => total + item.quantity, 0);
    const items = cart.add(
      {
        id: productId,
        name: button.dataset.name,
        price: button.dataset.price,
        image: button.dataset.image,
        salesPlanId,
        salesPlanSnapshot,
      },
      1,
      button,
    );
    const quantityAfter = items
      ?.filter((item) => item.id === productId)
      .reduce((total, item) => total + item.quantity, 0);
    if (
      !Number.isFinite(quantityAfter) ||
      quantityAfter <= quantityBefore
    )
      return;
    window.clearTimeout(button._cartFeedbackTimer);
    button.querySelector(".add-cart-confirmation")?.remove();
    button.classList.add("added");
    const confirmation = document.createElement("span");
    confirmation.className = "add-cart-confirmation";
    confirmation.setAttribute("role", "status");
    confirmation.setAttribute("aria-live", "polite");
    const confirmationIcon = document.createElement("i");
    confirmationIcon.className = "bi bi-check2";
    confirmationIcon.setAttribute("aria-hidden", "true");
    const confirmationText = document.createElement("span");
    confirmationText.textContent = "به سبد خرید اضافه شد";
    confirmation.append(confirmationIcon, confirmationText);
    button.append(confirmation);
    button._cartFeedbackTimer = window.setTimeout(() => {
      if (!button.isConnected) return;
      confirmation.remove();
      button.classList.remove("added");
    }, 1400);
  });
  window.HNUI = Object.freeze({
    STORAGE_KEYS,
    EVENTS,
    emit,
    mount,
    storage,
    api,
    payment,
    ready,
    cart,
    auth,
    scrollLock,
    dialog,
    enhance,
    toPersianDigits,
    toEnglishDigits,
    formatNumber,
    normalizeSearchText,
    escapeHtml,
    setMessage,
    setMessageType,
    showToast,
    localizeVisibleDigits,
  });
})(window, document);
