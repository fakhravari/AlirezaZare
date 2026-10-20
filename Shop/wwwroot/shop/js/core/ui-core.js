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
    const migrateLegacyItem = (item) => {
      const product = (window.HNProducts || []).find(
        (candidate) =>
          String(candidate.id) ===
          String(item.id || "").replace(/^(\d+):.*$/, "$1"),
      );
      const originalName = String(item.name || "").trim();
      if (!product || !originalName.startsWith(`${product.name} (`))
        return item;

      const options = item.options || {};
      if ((options.variants || []).length || (options.accessories || []).length)
        return item;

      const legacyOptions = originalName.slice(product.name.length + 2);
      const remaining = (legacyOptions.endsWith(")")
        ? legacyOptions.slice(0, -1)
        : legacyOptions)
        .split("،")
        .map((part) => part.trim())
        .filter(Boolean);
      const variants = [];
      Object.entries(product.variants || {}).forEach(([key, feature]) => {
        const prefix = `${feature.label}:`;
        const index = remaining.findIndex((part) => part.startsWith(prefix));
        if (index < 0) return;
        const value = remaining[index].slice(prefix.length).trim();
        const option = (feature.options || []).find((entry) => entry.label === value);
        variants.push({
          key,
          label: feature.label,
          value,
          productValue: option?.value || "",
        });
        remaining.splice(index, 1);
      });
      const accessories = [];
      (product.accessories || []).forEach((accessory) => {
        const index = remaining.indexOf(accessory.name);
        if (index < 0) return;
        accessories.push({
          name: accessory.name,
          price: accessory.price,
          required: Boolean(accessory.required),
        });
        remaining.splice(index, 1);
      });
      // Keep any older custom selection visible as an accessory chip instead of
      // leaving it appended to the product title or silently dropping it.
      remaining.forEach((name) => accessories.push({ name, price: 0 }));
      return {
        ...item,
        name: product.name,
        options: { variants, accessories },
      };
    };
    const normalizeOptions = (options, product) => {
      const cleanText = (text, limit) => String(text || "").trim().slice(0, limit);
      const variants = Array.isArray(options?.variants)
        ? options.variants
            .filter((option) => option && typeof option === "object")
            .map((option) => {
              const key = cleanText(option.key, 50);
              const value = cleanText(option.value, 80);
              const productValue = cleanText(option.productValue, 80);
              const productOption = product?.variants?.[key]?.options?.find(
                (candidate) =>
                  candidate.value === productValue || candidate.label === value,
              );
              const savedDelta = Number(option.priceDelta);
              return {
                key,
                label: cleanText(option.label, 50),
                value,
                productValue,
                priceDelta: Number.isFinite(savedDelta)
                  ? savedDelta
                  : Number(productOption?.priceDelta) || 0,
              };
            })
            .filter((option) => option.label && option.value)
            .slice(0, 12)
        : [];
      const selectedVariantKeys = new Set(variants.map((option) => option.key));
      Object.entries(product?.variants || {}).forEach(([key, feature]) => {
        if (selectedVariantKeys.has(key)) return;
        const option = feature.options?.[0];
        if (!option) return;
        variants.push({
          key,
          label: cleanText(feature.label, 50),
          value: cleanText(option.label, 80),
          productValue: cleanText(option.value, 80),
          priceDelta: Number(option.priceDelta) || 0,
        });
      });
      const accessories = Array.isArray(options?.accessories)
        ? options.accessories
            .filter((accessory) => accessory && typeof accessory === "object")
            .map((accessory) => {
              const name = cleanText(accessory.name, 100);
              const productAccessory = (product?.accessories || []).find(
                (candidate) => candidate.name === name,
              );
              const required = Boolean(accessory.required || productAccessory?.required);
              return {
                name,
                price: Math.max(0, Number(productAccessory?.price ?? accessory.price) || 0),
                required,
                selected: required || accessory.selected !== false,
              };
            })
            .filter((accessory) => accessory.name)
            .slice(0, 12)
        : [];
      (product?.accessories || []).forEach((accessory) => {
        const existing = accessories.find((item) => item.name === accessory.name);
        if (existing) {
          existing.required = Boolean(accessory.required);
          if (existing.required) existing.selected = true;
          existing.price = Math.max(0, Number(accessory.price) || 0);
          return;
        }
        if (accessories.length >= 12) return;
        accessories.push({
          name: cleanText(accessory.name, 100),
          price: Math.max(0, Number(accessory.price) || 0),
          required: Boolean(accessory.required),
          selected: Boolean(accessory.required),
        });
      });
      return { variants: variants.slice(0, 12), accessories };
    };
    const normalizeImage = (image) =>
      String(image || "")
        .slice(0, 250)
        .replace(
          /^((?:Shop\/)?images\/watch-(?:blue|cream|silver|story|hero))\.(?:jpe?g|png)$/i,
          "$1.webp",
        );
    const normalizedItems = value
      .filter((item) => item && typeof item === "object")
      .map((rawItem) => {
        const item = migrateLegacyItem(rawItem);
        const product = (window.HNProducts || []).find(
          (candidate) => String(candidate.id) === String(item.id || "").replace(/^(\d+):.*$/, "$1"),
        );
        const options = normalizeOptions(item.options, product);
        const requiredAccessories = (product?.accessories || []).filter(
          (accessory) => accessory.required,
        );
        const selectedAccessoryNames = new Set(
          (Array.isArray(item.options?.accessories) ? item.options.accessories : [])
            .filter((accessory) => accessory && accessory.selected !== false)
            .map((accessory) => accessory.name),
        );
        const missingRequiredAccessories = requiredAccessories.filter(
          (accessory) => !selectedAccessoryNames.has(accessory.name),
        );
        const missingVariantPriceDelta = options.variants
          .filter((option) => !Array.isArray(item.options?.variants) ||
            !item.options.variants.some((selected) => selected?.key === option.key))
          .reduce((sum, option) => sum + (Number(option.priceDelta) || 0), 0);
        return {
          // Older product-detail rows included variant/accessory keys after the product ID.
          id: String(item.id || "")
            .slice(0, 80)
            .replace(/^(\d+):.*$/, "$1"),
          name: String(item.name || "").slice(0, 120),
          price:
            Number(item.price) +
            missingVariantPriceDelta * 1000000 +
            missingRequiredAccessories.reduce(
              (sum, accessory) =>
                sum + (Number(accessory.price) || 0) * 1000000,
              0,
            ),
          image: normalizeImage(item.image),
          options,
          quantity: Math.min(
            productCartLimit(product),
            Math.max(1, Number.parseInt(item.quantity, 10) || 1),
          ),
        };
      })
      .filter(
        (item) =>
          item.id &&
          item.name &&
          Number.isFinite(item.price) &&
          item.price >= 0 &&
          /^(?:Shop\/)?images\/(?:[\w.-]+\/)*[\w.-]+$/i.test(item.image),
      );
    const uniqueItems = new Map();
    normalizedItems.forEach((item) => {
      const existing = uniqueItems.get(item.id);
      if (!existing) {
        uniqueItems.set(item.id, item);
        return;
      }
      if (
        existing.name === item.name &&
        existing.price === item.price &&
        existing.image === item.image &&
        JSON.stringify(existing.options) === JSON.stringify(item.options)
      ) {
        existing.quantity = Math.min(
          productCartLimit((window.HNProducts || []).find(
            (product) => String(product.id) === existing.id,
          )),
          existing.quantity + item.quantity,
        );
        return;
      }
      // Different options for one product code replace its previous configuration.
      uniqueItems.set(item.id, item);
    });
    return [...uniqueItems.values()];
  };

  const productForCartItem = (item) =>
    (window.HNProducts || []).find(
      (candidate) => String(candidate.id) === String(item?.id || ""),
    );
  const productCartLimit = (product) => {
    const rawLimit = Number(product?.maxCartQuantity);
    return Number.isFinite(rawLimit) && rawLimit > 0
      ? Math.min(99, Math.floor(rawLimit))
      : 99;
  };
  const availableStock = (item) => {
    const product = productForCartItem(item);
    if (!product) return null;
    const rawStock =
      product.stockQuantity ??
      product.inventoryCount ??
      product.quantityInStock ??
      product.availableQuantity ??
      product.stock;
    let stock =
      typeof rawStock === "number" ||
      (typeof rawStock === "string" && rawStock.trim())
        ? Number(rawStock)
        : Number.NaN;
    if (!Number.isFinite(stock) || stock < 0) return null;

    const variantKeys = Object.keys(product.variants || {});
    if (variantKeys.length && product.variantStock) {
      const selected = new Map(
        (item.options?.variants || []).map((option) => [option.key, option.productValue]),
      );
      const hasCombination = variantKeys.every((key) => selected.get(key));
      if (hasCombination) {
        const combinationKey = variantKeys
          .map((key) => `${key}=${selected.get(key)}`)
          .join("|");
        if (Object.prototype.hasOwnProperty.call(product.variantStock, combinationKey))
          stock = Number(product.variantStock[combinationKey]);
      }
    }
    return Number.isFinite(stock) && stock >= 0 ? Math.floor(stock) : null;
  };

  const enforceCartLimits = (items) =>
    items.flatMap((item) => {
      const cartLimit = productCartLimit(productForCartItem(item));
      const stock = availableStock(item);
      if (stock === null)
        return [{ ...item, quantity: Math.min(item.quantity, cartLimit) }];
      if (!stock) return [];
      return [{
        ...item,
        quantity: Math.min(item.quantity, stock, cartLimit),
      }];
    });

  const cart = {
    renderOptions(item) {
      const variants = Array.isArray(item.options?.variants)
        ? item.options.variants
        : [];
      const accessories = Array.isArray(item.options?.accessories)
        ? item.options.accessories.filter((accessory) => accessory.selected !== false)
        : [];
      if (!variants.length && !accessories.length) return "";
      const money = (amount) => `${formatNumber(amount)} تومان`;
      const variantsMarkup = variants.length
        ? `<div class="cart-item-option-group"><small class="cart-item-option-label">ویژگی‌ها</small><div class="cart-item-option-list cart-item-option-list--variants">${variants
            .map((option) => `<span class="cart-item-option-chip"><small>${escapeHtml(option.label)}</small><strong>${escapeHtml(option.value)}</strong>${Number(option.priceDelta) ? `<small class="cart-item-option-price">${Number(option.priceDelta) > 0 ? "+" : "−"} ${money(Math.abs(Number(option.priceDelta)) * 1000000)}</small>` : ""}</span>`)
            .join("")}</div></div>`
        : "";
      const accessoriesMarkup = accessories.length
        ? `<div class="cart-item-option-group"><small class="cart-item-option-label">متعلقات و خدمات</small><div class="cart-item-option-list cart-item-option-list--accessories">${accessories
            .map((accessory) => `<span class="cart-item-option-chip cart-item-option-chip--accessory"><i class="bi bi-plus-circle" aria-hidden="true"></i><strong>${escapeHtml(accessory.name)}</strong><small class="cart-item-option-status ${accessory.required ? "is-required" : "is-optional"}">${accessory.required ? "الزامی" : "اختیاری"}</small>${Number(accessory.price) > 0 ? `<small>+ ${money(Number(accessory.price) * 1000000)}</small>` : "<small>رایگان</small>"}</span>`)
            .join("")}</div></div>`
        : "";
      return `<div class="cart-item-options" aria-label="ویژگی‌ها و متعلقات انتخاب‌شده">${variantsMarkup}${accessoriesMarkup}</div>`;
    },
    read() {
      try {
        const raw = JSON.parse(storage.get(STORAGE_KEYS.cart, "[]"));
        const normalized = enforceCartLimits(normalizeCart(raw));
        if (JSON.stringify(raw) !== JSON.stringify(normalized)) {
          storage.set(STORAGE_KEYS.cart, JSON.stringify(normalized));
        }
        return normalized;
      } catch {
        return [];
      }
    },
    write(items) {
      const safeItems = enforceCartLimits(normalizeCart(items));
      storage.set(STORAGE_KEYS.cart, JSON.stringify(safeItems));
      document.dispatchEvent(
        new CustomEvent("hn:cart-change", { detail: safeItems }),
      );
      return safeItems;
    },
    add(product, quantity = 1, trigger = null) {
      if (!auth.requireLogin(() => cart.add(product, quantity, trigger)))
        return null;
      const items = this.read();
      const requestedQuantity = Math.max(1, Number.parseInt(quantity, 10) || 1);
      const safeProduct = normalizeCart([{ ...product, quantity }])[0];
      if (!safeProduct) return items;
      const existing = items.find((item) => item.id === safeProduct.id);
      const sameConfiguration =
        existing &&
        existing.name === safeProduct.name &&
        existing.price === safeProduct.price &&
        existing.image === safeProduct.image &&
        JSON.stringify(existing.options) === JSON.stringify(safeProduct.options);
      const stock = availableStock(safeProduct);
      const cartLimit = productCartLimit(productForCartItem(safeProduct));
      const quantityLimit = stock === null ? cartLimit : Math.min(stock, cartLimit);
      const currentQuantity = sameConfiguration ? existing.quantity : 0;
      const remaining = Math.max(0, quantityLimit - currentQuantity);
      const requested = requestedQuantity;
      safeProduct.quantity = Math.min(requested, remaining);
      if (safeProduct.quantity < requested) {
        document.dispatchEvent(
          new CustomEvent("hn:cart-stock-limit", {
            detail: {
              product: safeProduct,
              stock,
              maxCartQuantity: cartLimit,
              limit: quantityLimit,
              requested,
              remaining,
              reason: stock !== null && stock <= cartLimit ? "stock" : "cart",
            },
          }),
        );
      }
      if (!safeProduct.quantity) return items;
      if (existing) {
        if (sameConfiguration) {
          existing.quantity = Math.min(
            quantityLimit,
            existing.quantity + safeProduct.quantity,
          );
        } else {
          Object.assign(existing, safeProduct);
        }
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
      const stock = availableStock(item);
      const cartLimit = productCartLimit(productForCartItem(item));
      const quantityLimit = stock === null ? cartLimit : Math.min(stock, cartLimit);
      const limitedQuantity = Math.min(nextQuantity, quantityLimit);
      if (limitedQuantity < nextQuantity) {
        document.dispatchEvent(
          new CustomEvent("hn:cart-stock-limit", {
            detail: {
              product: item,
              stock,
              maxCartQuantity: cartLimit,
              limit: quantityLimit,
              requested: nextQuantity,
              remaining: quantityLimit,
              reason: stock !== null && stock <= cartLimit ? "stock" : "cart",
            },
          }),
        );
      }
      if (!limitedQuantity) return this.remove(id);
      item.quantity = limitedQuantity;
      return this.write(items);
    },
    quantityLimit(item) {
      const stock = availableStock(item);
      const cartLimit = productCartLimit(productForCartItem(item));
      return stock === null ? cartLimit : Math.min(stock, cartLimit);
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
    requireLogin(resume) {
      if (auth.isLoggedIn()) return true;
      document.dispatchEvent(
        new CustomEvent("hn:auth-required", {
          detail: { resume: typeof resume === "function" ? resume : null },
        }),
      );
      return false;
    },
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
    setMessage,
    setMessageType,
    showToast,
    localizeVisibleDigits,
  });
})(window, document);
