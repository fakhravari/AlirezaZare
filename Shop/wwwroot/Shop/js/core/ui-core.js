(function (window, document) {
  "use strict";

  const STORAGE_KEYS = Object.freeze({
    auth: "hn-demo-auth",
    cart: "hadi-norouzi-cart",
  });

  const storage = {
    get(key, fallback = null) {
      try {
        const value = window.localStorage.getItem(key);
        return value === null ? fallback : value;
      } catch {
        return fallback;
      }
    },
    set(key, value) {
      try {
        window.localStorage.setItem(key, value);
        if (key === "hn-favorites" || key === "hn-compare") {
          document.dispatchEvent(new Event("hn:lists-change"));
        }
        return true;
      } catch {
        return false;
      }
    },
    remove(key) {
      try {
        window.localStorage.removeItem(key);
      } catch {
        // The static demo remains usable when browser storage is unavailable.
      }
    },
    json(key, fallback = null) {
      try {
        const value = JSON.parse(this.get(key, "null"));
        return value ?? fallback;
      } catch {
        return fallback;
      }
    },
  };

  const ready = (callback) => {
    if (document.readyState === "loading")
      document.addEventListener("DOMContentLoaded", callback, { once: true });
    else callback();
  };

  const normalizeCart = (value) => {
    if (!Array.isArray(value)) return [];
    const normalizeImage = (image) =>
      String(image || "")
        .slice(0, 250)
        .replace(
          /^((?:Shop\/)?images\/watch-(?:blue|cream|silver|story|hero))\.(?:jpe?g|png)$/i,
          "$1.webp",
        );
    return value
      .filter((item) => item && typeof item === "object")
      .map((item) => ({
        id: String(item.id || "").slice(0, 80),
        name: String(item.name || "").slice(0, 120),
        price: Number(item.price),
        image: normalizeImage(item.image),
        quantity: Math.min(
          99,
          Math.max(1, Number.parseInt(item.quantity, 10) || 1),
        ),
      }))
      .filter(
        (item) =>
          item.id &&
          item.name &&
          Number.isFinite(item.price) &&
          item.price >= 0 &&
          /^(?:Shop\/)?images\/(?:[\w.-]+\/)*[\w.-]+$/i.test(item.image),
      );
  };

  const cart = {
    read() {
      try {
        const raw = JSON.parse(storage.get(STORAGE_KEYS.cart, "[]"));
        const normalized = normalizeCart(raw);
        if (JSON.stringify(raw) !== JSON.stringify(normalized)) {
          storage.set(STORAGE_KEYS.cart, JSON.stringify(normalized));
        }
        return normalized;
      } catch {
        return [];
      }
    },
    write(items) {
      const safeItems = normalizeCart(items);
      storage.set(STORAGE_KEYS.cart, JSON.stringify(safeItems));
      document.dispatchEvent(
        new CustomEvent("hn:cart-change", { detail: safeItems }),
      );
      return safeItems;
    },
    add(product, quantity = 1, trigger = null) {
      const items = this.read();
      const safeProduct = normalizeCart([{ ...product, quantity }])[0];
      if (!safeProduct) return items;
      const existing = items.find((item) => item.id === safeProduct.id);
      if (existing) {
        existing.quantity = Math.min(99, existing.quantity + safeProduct.quantity);
      } else {
        items.push(safeProduct);
      }
      const savedItems = this.write(items);
      document.dispatchEvent(
        new CustomEvent("hn:cart-added", {
          detail: {
            product: safeProduct,
            items: savedItems,
            trigger: trigger || document.activeElement,
          },
        }),
      );
      return savedItems;
    },
    update(id, quantity) {
      const items = this.read();
      const item = items.find((entry) => entry.id === String(id));
      if (!item) return items;
      const nextQuantity = Number.parseInt(quantity, 10);
      if (!Number.isFinite(nextQuantity) || nextQuantity < 1) {
        return this.remove(id);
      }
      item.quantity = Math.min(99, nextQuantity);
      return this.write(items);
    },
    remove(id) {
      return this.write(this.read().filter((item) => item.id !== String(id)));
    },
    totals() {
      return this.read().reduce(
        (summary, item) => ({
          count: summary.count + item.quantity,
          price: summary.price + item.price * item.quantity,
        }),
        { count: 0, price: 0 },
      );
    },
  };

  const auth = {
    isLoggedIn: () =>
      ["1", "true"].includes(storage.get(STORAGE_KEYS.auth, "")),
    login() {
      storage.set(STORAGE_KEYS.auth, "1");
      document.dispatchEvent(
        new CustomEvent("hn:auth-change", { detail: true }),
      );
    },
    logout() {
      storage.remove(STORAGE_KEYS.auth);
      document.dispatchEvent(
        new CustomEvent("hn:auth-change", { detail: false }),
      );
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

  const persianDigits = "\u06F0\u06F1\u06F2\u06F3\u06F4\u06F5\u06F6\u06F7\u06F8\u06F9";
  const arabicDigits = "\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669";
  const toEnglishDigits = (value) => String(value)
    .replace(/[\u06F0-\u06F9]/g, (digit) => String(persianDigits.indexOf(digit)))
    .replace(/[\u0660-\u0669]/g, (digit) => String(arabicDigits.indexOf(digit)));
  const formatNumber = (value) => Number(value).toLocaleString("fa-IR");
  const escapeHtml = (value) => String(value ?? "").replace(/[&<>"']/g, (character) =>
    ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[character]);

  const localizeVisibleDigits = (root = document.body) => {
    if (!root) return;
    const excluded = "SCRIPT, STYLE, NOSCRIPT, TEXTAREA, INPUT, SELECT, OPTION, CODE, PRE";
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
          if (addedNode.nodeType === Node.ELEMENT_NODE || addedNode.nodeType === Node.TEXT_NODE)
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
          behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches
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
    const items = cart.add(
      {
        id: button.dataset.id,
        name: button.dataset.name,
        price: button.dataset.price,
        image: button.dataset.image,
      },
      1,
      button,
    );
    if (!items.some((item) => item.id === String(button.dataset.id))) return;
    const originalMarkup = button.innerHTML;
    button.classList.add("added");
    button.innerHTML = '<i class="bi bi-check2"></i> به سبد اضافه شد';
    window.setTimeout(() => {
      if (!button.isConnected) return;
      button.classList.remove("added");
      button.innerHTML = originalMarkup;
    }, 1400);
  });
  window.addEventListener("storage", (event) => {
    if (event.key === STORAGE_KEYS.cart)
      document.dispatchEvent(
        new CustomEvent("hn:cart-change", { detail: cart.read() }),
      );
    if (event.key === STORAGE_KEYS.auth)
      document.dispatchEvent(
        new CustomEvent("hn:auth-change", { detail: auth.isLoggedIn() }),
      );
  });

  window.HNUI = Object.freeze({
    STORAGE_KEYS,
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
    localizeVisibleDigits,
  });
})(window, document);
