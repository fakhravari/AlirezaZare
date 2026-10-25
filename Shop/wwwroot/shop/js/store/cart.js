(function (window) {
  "use strict";
  if (window.HNCartModel) return;
  window.HNCartModel = Object.freeze({ create({ document, emit, storage, cartKey, requireLogin, formatNumber, escapeHtml }) {
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
                label: cleanText(product?.variants?.[key]?.label || option.label, 50),
                value: productOption?.label || value,
                productValue: productOption?.value || productValue,
                priceDelta: product
                  ? Number(productOption?.priceDelta) || 0
                  : Number.isFinite(savedDelta) ? savedDelta : 0,
              };
            })
            .filter((option) => option.label && option.value && (!product ||
              product.variants?.[option.key]?.options?.some((candidate) => candidate.value === option.productValue || candidate.label === option.value)))
            .filter((option, index, all) => all.findIndex((candidate) => candidate.key === option.key) === index)
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
                (candidate) => accessory.id ? candidate.id === accessory.id : candidate.name === name,
              );
              const required = product ? Boolean(productAccessory?.required) : Boolean(accessory.required);
              return {
                id: cleanText(productAccessory?.id || accessory.id, 80),
                name: cleanText(productAccessory?.name || name, 100),
                price: Math.max(0, Number(productAccessory?.price ?? accessory.price) || 0),
                required,
                selected: required || accessory.selected !== false,
              };
            })
            .filter((accessory) => accessory.name && (!product || (product.accessories || []).some((candidate) => candidate.id === accessory.id)))
            .filter((accessory, index, all) => all.findIndex((candidate) => (candidate.id || candidate.name) === (accessory.id || accessory.name)) === index)
            .slice(0, 12)
        : [];
      (product?.accessories || []).forEach((accessory) => {
        const existing = accessories.find((item) => item.id === accessory.id);
        if (existing) {
          existing.required = Boolean(accessory.required);
          if (existing.required) existing.selected = true;
          existing.price = Math.max(0, Number(accessory.price) || 0);
          return;
        }
        if (accessories.length >= 12) return;
        accessories.push({
          id: cleanText(accessory.id, 80),
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
        const configuredPrice = product ? Math.round((Number(product.price) +
          options.variants.reduce((sum, option) => sum + Number(option.priceDelta || 0), 0) +
          options.accessories.filter((accessory) => accessory.selected).reduce((sum, accessory) => sum + Number(accessory.price || 0), 0)
        ) * 1000000) : Number(item.price);
        return {
          // Older product-detail rows included variant/accessory keys after the product ID.
          id: String(item.id || "")
            .slice(0, 80)
            .replace(/^(\d+):.*$/, "$1"),
          name: String(product?.name || item.name || "").slice(0, 120),
          price: configuredPrice,
          image: normalizeImage(product?.image || item.image),
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
          /^(?:(?:Shop\/)?images\/|\/?content\/products\/)(?:[\w.-]+\/)*[\w.-]+$/i.test(item.image),
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
    renderControls(item, index = null) {
      const attribute = (action) => index === null
        ? `data-action="${action}"`
        : `data-${action}="${index}"`;
      return `<div class="hn-cart-controls" role="group" aria-label="تعداد ${escapeHtml(item.name)}"><div class="hn-cart-quantity"><button type="button" ${attribute("plus")} aria-label="افزایش تعداد" ${item.quantity >= this.quantityLimit(item) ? "disabled" : ""}><i class="bi bi-plus" aria-hidden="true"></i></button><span class="cart-quantity-value">${formatNumber(item.quantity)}</span><button type="button" ${attribute("minus")} aria-label="کاهش تعداد" ${item.quantity <= 1 ? "disabled" : ""}><i class="bi bi-dash" aria-hidden="true"></i></button></div><button type="button" class="hn-cart-remove" ${attribute("remove")} aria-label="حذف ${escapeHtml(item.name)} از سبد"><i class="bi bi-trash3" aria-hidden="true"></i></button></div>`;
    },
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
            .map((option) => `<span class="cart-item-option-chip"><small class="cart-item-option-name">${escapeHtml(option.label)}</small><strong>${escapeHtml(option.value)}</strong>${Number(option.priceDelta) ? `<small class="cart-item-option-price">${Number(option.priceDelta) > 0 ? "+" : "−"} ${money(Math.abs(Number(option.priceDelta)) * 1000000)}</small>` : ""}</span>`)
            .join("")}</div></div>`
        : "";
      const accessoriesMarkup = accessories.length
        ? `<div class="cart-item-option-group"><small class="cart-item-option-label">متعلقات و خدمات</small><div class="cart-item-option-list cart-item-option-list--accessories">${accessories
            .map((accessory) => `<span class="cart-item-option-chip cart-item-option-chip--accessory"><small class="cart-item-option-name">${escapeHtml(accessory.name)}</small><small class="cart-item-option-status ${accessory.required ? "is-required" : "is-optional"}">${accessory.required ? "الزامی" : "اختیاری"}</small><small class="cart-item-option-price">${Number(accessory.price) > 0 ? `+ ${money(Number(accessory.price) * 1000000)}` : "رایگان"}</small></span>`)
            .join("")}</div></div>`
        : "";
      return `<div class="cart-item-options" aria-label="ویژگی‌ها و متعلقات انتخاب‌شده">${variantsMarkup}${accessoriesMarkup}</div>`;
    },
    read() {
      try {
        const raw = JSON.parse(storage.get(cartKey, "[]"));
        const normalized = enforceCartLimits(normalizeCart(raw));
        if (JSON.stringify(raw) !== JSON.stringify(normalized)) {
          storage.set(cartKey, JSON.stringify(normalized));
        }
        return normalized;
      } catch {
        return [];
      }
    },
    write(items) {
      if (!storage.writable(cartKey)) return this.read();
      const safeItems = enforceCartLimits(normalizeCart(items));
      if (!storage.set(cartKey, JSON.stringify(safeItems))) return this.read();
      emit("hn:cart-change", safeItems);
      return safeItems;
    },
    add(product, quantity = 1, trigger = null) {
      if (!requireLogin(() => cart.add(product, quantity, trigger)))
        return null;
      const items = this.read();
      if (!storage.writable(cartKey)) return items;
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
        emit("hn:cart-stock-limit", {
              product: safeProduct,
              stock,
              maxCartQuantity: cartLimit,
              limit: quantityLimit,
              requested,
              remaining,
              reason: stock !== null && stock <= cartLimit ? "stock" : "cart",
            });
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
      const saved = savedItems.find((item) => item.id === safeProduct.id);
      if (!saved || saved.quantity !== (sameConfiguration ? existing.quantity : safeProduct.quantity) || JSON.stringify(saved.options) !== JSON.stringify(safeProduct.options)) return savedItems;
      emit("hn:cart-added", {
            product: safeProduct,
            items: savedItems,
            trigger: trigger || document.activeElement,
          });
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
        emit("hn:cart-stock-limit", {
              product: item,
              stock,
              maxCartQuantity: cartLimit,
              limit: quantityLimit,
              requested: nextQuantity,
              remaining: quantityLimit,
              reason: stock !== null && stock <= cartLimit ? "stock" : "cart",
            });
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


    return Object.freeze(cart);
  }});
})(window);
