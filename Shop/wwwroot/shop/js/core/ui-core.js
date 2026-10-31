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
    document.dispatchEvent(new CustomEvent(type, { detail }));
  };

  const STORAGE_KEYS = Object.freeze({
    auth: "hn-demo-auth",
    cart: "hadi-norouzi-cart",
  });
  const storage = window.HNStorage.create({ document, emit });

  const ready = (callback) => {
    if (document.readyState === "loading")
      document.addEventListener("DOMContentLoaded", callback, { once: true });
    else callback();
  };

  const cart = window.HNCartModel.create({
    document, emit, storage, cartKey: STORAGE_KEYS.cart,
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
      storage.remove(STORAGE_KEYS.auth);
      emit("hn:auth-change", false);
    },
  };

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
  document.addEventListener("click", (event) => {
    const button = event.target.closest(".add-cart[data-id]");
    if (!button) return;
    const productId = String(button.dataset.id);
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
        salesPlanId: button.dataset.salesPlanId || "regular",
        salesPlanSnapshot: button.dataset.salesPlanId
          ? { id: button.dataset.salesPlanId, name: button.dataset.salesPlanName || "طرح فروش ویژه", code: button.dataset.salesPlanCode || "", label: button.dataset.salesPlanLabel || null, unitAmount: Number(button.dataset.price), stockQuantity: Number(button.dataset.stockQuantity), maxPurchaseQuantity: Number(button.dataset.maxPurchaseQuantity), currency: "IRT" }
          : null,
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
  window.addEventListener("storage", (event) => {
    if (event.key === STORAGE_KEYS.cart)
      emit("hn:cart-change", cart.read());
    if (event.key === STORAGE_KEYS.auth)
      emit("hn:auth-change", auth.isLoggedIn());
  });

  window.HNUI = Object.freeze({
    STORAGE_KEYS,
    EVENTS,
    emit,
    mount,
    storage,
    ready,
    cart,
    auth,
    scrollLock,
    dialog,
    enhance,
    toPersianDigits,
    toEnglishDigits,
    formatNumber,
    escapeHtml,
    setMessage,
    setMessageType,
    showToast,
    localizeVisibleDigits,
  });
})(window, document);
