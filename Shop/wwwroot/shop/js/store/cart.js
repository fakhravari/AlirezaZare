(function (window) {
  "use strict";
  if (window.HNCartModel) return;
  window.HNCartModel = Object.freeze({ create({ document, emit, storage, cartKey, requireLogin, formatNumber, escapeHtml }) {
  const getSalesPlanData = () => {
    try {
      return JSON.parse(document.querySelector("#storeSalesPlans")?.textContent || "{}");
    } catch {
      return {};
    }
  };
  const sortBySalesPlan = (items, getId = (item) => item?.salesPlanId || "regular") => {
    const data = getSalesPlanData();
    const plans = Array.isArray(data.plans) ? data.plans : [];
    const campaignPlans = plans.filter((plan) => !plan.isDefault && plan.type !== "regular" && plan.id !== "regular");
    const typeOrder = [...new Set(campaignPlans.map((plan) => String(plan.type || plan.id)))];
    const ranks = new Map(campaignPlans.map((plan, index) => [String(plan.id), [1, typeOrder.indexOf(String(plan.type || plan.id)), index, String(plan.id)]]));
    plans.filter((plan) => plan.isDefault || plan.type === "regular" || plan.id === "regular")
      .forEach((plan, index) => ranks.set(String(plan.id), [0, index, index, String(plan.id)]));
    if (!ranks.has("regular")) ranks.set("regular", [0, 0, 0, "regular"]);
    const rankFor = (id) => ranks.get(String(id || "regular")) || [2, Number.MAX_SAFE_INTEGER, Number.MAX_SAFE_INTEGER, String(id)];
    return (Array.isArray(items) ? items : [])
      .map((item, index) => ({ item, index, rank: rankFor(getId(item)) }))
      .sort((left, right) => left.rank[0] - right.rank[0] || left.rank[1] - right.rank[1] ||
        left.rank[2] - right.rank[2] || left.rank[3].localeCompare(right.rank[3]) || left.index - right.index)
      .map(({ item }) => item);
  };
  const salesPlanName = (id, fallback = "") => {
    const data = getSalesPlanData();
    const plan = (Array.isArray(data.plans) ? data.plans : [])
      .find((candidate) => String(candidate.id) === String(id));
    return String(plan?.name || fallback || (String(id) === "regular" ? "فروش عادی" : "طرح فروش"));
  };
  const groupBySalesPlan = (items, getId = (item) => item?.salesPlanId || "regular", getName = (item) => item?.salesPlanSnapshot?.name || "") => {
    const groups = new Map();
    sortBySalesPlan(items, getId).forEach((item) => {
      const id = String(getId(item) || "regular");
      if (!groups.has(id)) groups.set(id, { id, name: salesPlanName(id, getName(item)), items: [] });
      groups.get(id).items.push(item);
    });
    return [...groups.values()];
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
      if ((options.priceOptions || options.variants || []).length || (options.accessories || []).length)
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
        options: { priceOptions: variants, accessories },
      };
    };
    const normalizeOptions = (options, product) => {
      const cleanText = (text, limit) => String(text || "").trim().slice(0, limit);
      const productPriceOptions = getSalesPlanData().priceOptions?.[Number(product?.id)] || [];
      const priceOptionMap = Object.fromEntries(productPriceOptions.map((feature) => [feature.key, {
        label: feature.label,
        options: (feature.values || []).map((value) => ({ value: value.key, label: value.label, priceDelta: Number(value.priceDelta) / 1000000 })),
      }]));
      const savedPriceOptions = Array.isArray(options?.priceOptions) ? options.priceOptions : options?.variants;
      const priceOptions = Array.isArray(savedPriceOptions)
        ? savedPriceOptions
            .filter((option) => option && typeof option === "object")
            .map((option) => {
              const key = cleanText(option.key, 50);
              const value = cleanText(option.value, 80);
              const productValue = cleanText(option.productValue, 80);
              const productOption = priceOptionMap[key]?.options?.find(
                (candidate) =>
                  candidate.value === productValue || candidate.label === value,
              );
              const savedDelta = Number(option.priceDelta);
              return {
                key,
                label: cleanText(priceOptionMap[key]?.label || option.label, 50),
                value: productOption?.label || value,
                productValue: productOption?.value || productValue,
                priceDelta: product
                  ? Number(productOption?.priceDelta) || 0
                  : Number.isFinite(savedDelta) ? savedDelta : 0,
              };
            })
            .filter((option) => option.label && option.value && (!product ||
              priceOptionMap[option.key]?.options?.some((candidate) => candidate.value === option.productValue || candidate.label === option.value)))
            .filter((option, index, all) => all.findIndex((candidate) => candidate.key === option.key) === index)
            .slice(0, 12)
        : [];
      const selectedVariantKeys = new Set(priceOptions.map((option) => option.key));
      Object.entries(priceOptionMap).forEach(([key, feature]) => {
        if (selectedVariantKeys.has(key)) return;
        const option = feature.options?.[0];
        if (!option) return;
        priceOptions.push({
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
      const inventoryVariant = options?.inventoryVariant && typeof options.inventoryVariant === "object"
        ? { id: Number(options.inventoryVariant.id) || null, name: cleanText(options.inventoryVariant.name, 120), attributes: Array.isArray(options.inventoryVariant.attributes) ? options.inventoryVariant.attributes.slice(0, 12) : [] }
        : null;
      return { priceOptions: priceOptions.slice(0, 12), accessories, inventoryVariant };
    };
    const normalizeImage = (image) =>
      String(image || "")
        .slice(0, 250)
        .replace(
          /^((?:Shop\/)?images\/watch-(?:blue|cream|silver|story|hero))\.(?:jpe?g|png)$/i,
          "$1.webp",
        );
    const isAllowedImage = (image) => {
      const cdnBase = document.querySelector('meta[name="media-cdn-base-url"]')?.content || "";
      const path = cdnBase && image.startsWith(cdnBase) ? image.slice(cdnBase.length) : image;
      return /^(?:(?:Shop\/)?images\/|\/?content\/products\/)(?:[\w.-]+\/)*[\w.-]+$/i.test(path);
    };
    const normalizedItems = value
      .filter((item) => item && typeof item === "object")
      .map((rawItem) => {
        const item = migrateLegacyItem(rawItem);
        const product = (window.HNProducts || []).find(
          (candidate) => String(candidate.id) === String(item.id || "").replace(/^(\d+):.*$/, "$1"),
        );
        const options = normalizeOptions(item.options, product);
        if (item.salesPlanSnapshot?.accessoryPriceAmounts && typeof item.salesPlanSnapshot.accessoryPriceAmounts === "object") {
          options.accessories = options.accessories.map((accessory) => {
            const amount = Number(item.salesPlanSnapshot.accessoryPriceAmounts[accessory.id]);
            return Number.isFinite(amount) && amount >= 0
              ? { ...accessory, price: amount / 1000000 }
              : accessory;
          });
        }
        const configuredPrice = item.salesPlanSnapshot?.unitAmount != null ? Number(item.salesPlanSnapshot.unitAmount) :
          item.salesPlanId && item.salesPlanId !== "regular" && Number.isFinite(Number(item.price)) ? Number(item.price) : product ? Math.round((Number(product.price) +
          options.priceOptions.reduce((sum, option) => sum + Number(option.priceDelta || 0), 0) +
          options.accessories.filter((accessory) => accessory.selected).reduce((sum, accessory) => sum + Number(accessory.price || 0), 0)
        ) * 1000000) : Number(item.price);
        return {
          // Older product-detail rows included variant/accessory keys after the product ID.
          id: String(item.id || "")
            .slice(0, 80)
            .replace(/^(\d+):.*$/, "$1"),
          variantId: Number.isSafeInteger(Number(item.variantId)) && Number(item.variantId) > 0 ? Number(item.variantId) : null,
          salesPlanId: String(item.salesPlanId || item.salesPlanSnapshot?.id || "regular").trim().slice(0, 64),
          salesPlanSnapshot: item.salesPlanSnapshot && typeof item.salesPlanSnapshot === "object" ? item.salesPlanSnapshot : null,
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
          /^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(item.salesPlanId) &&
          item.name &&
          Number.isFinite(item.price) &&
          item.price >= 0 &&
          isAllowedImage(item.image),
      );
    const uniqueItems = new Map();
    normalizedItems.forEach((item) => {
      const lineKey = `${item.id}:${item.variantId || "base"}:${item.salesPlanId}`;
      const existing = uniqueItems.get(lineKey);
      if (!existing) {
        uniqueItems.set(lineKey, item);
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
      // Within one plan, a product code keeps one configuration; other plans remain separate lines.
      uniqueItems.set(lineKey, item);
    });
    return sortBySalesPlan([...uniqueItems.values()]);
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
    const warehouseVariants = (getSalesPlanData().inventoryVariants || []).filter((variant) => Number(variant.productId) === Number(product.id));
    if (warehouseVariants.length) {
      if (!item.variantId) return null;
      const selectedVariant = warehouseVariants.find((variant) => Number(variant.variantId) === Number(item.variantId));
      if (!selectedVariant) return 0;
      const variantPlanStock = Number(selectedVariant.stockBySalesPlan?.[item.salesPlanId] ?? 0);
      return Number.isFinite(variantPlanStock) && variantPlanStock >= 0 ? Math.floor(variantPlanStock) : 0;
    }
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
    sortBySalesPlan,
    groupBySalesPlan,
    salesPlanName,
    renderControls(item, index = null) {
      const attribute = (action) => index === null
        ? `data-action="${action}"`
        : `data-${action}="${index}"`;
      return `<div class="hn-cart-controls" data-sales-plan-id="${escapeHtml(item.salesPlanId || "regular")}" role="group" aria-label="تعداد ${escapeHtml(item.name)}"><div class="hn-cart-quantity"><button type="button" ${attribute("plus")} aria-label="افزایش تعداد" ${item.quantity >= this.quantityLimit(item) ? "disabled" : ""}><i class="bi bi-plus" aria-hidden="true"></i></button><span class="cart-quantity-value">${formatNumber(item.quantity)}</span><button type="button" ${attribute("minus")} aria-label="کاهش تعداد" ${item.quantity <= 1 ? "disabled" : ""}><i class="bi bi-dash" aria-hidden="true"></i></button></div><button type="button" class="hn-cart-remove" ${attribute("remove")} aria-label="حذف ${escapeHtml(item.name)} از سبد"><i class="bi bi-trash3" aria-hidden="true"></i></button></div>`;
    },
    renderOptions(item) {
      const priceOptions = Array.isArray(item.options?.priceOptions)
        ? item.options.priceOptions
        : [];
      const accessories = Array.isArray(item.options?.accessories)
        ? item.options.accessories.filter((accessory) => accessory.selected !== false)
        : [];
      const inventoryVariant = item.options?.inventoryVariant;
      if (!priceOptions.length && !accessories.length && !inventoryVariant) return "";
      const money = (amount) => `${formatNumber(amount)} تومان`;
      const variantsMarkup = priceOptions.length
        ? `<div class="cart-item-option-group"><small class="cart-item-option-label">گزینه‌های قیمت</small><div class="cart-item-option-list cart-item-option-list--variants">${priceOptions
            .map((option) => `<span class="cart-item-option-chip"><small class="cart-item-option-name">${escapeHtml(option.label)}</small><strong>${escapeHtml(option.value)}</strong>${Number(option.priceDelta) ? `<small class="cart-item-option-price">${Number(option.priceDelta) > 0 ? "+" : "−"} ${money(Math.abs(Number(option.priceDelta)) * 1000000)}</small>` : ""}</span>`)
            .join("")}</div></div>`
        : "";
      const accessoriesMarkup = accessories.length
        ? `<div class="cart-item-option-group"><small class="cart-item-option-label">متعلقات و خدمات</small><div class="cart-item-option-list cart-item-option-list--accessories">${accessories
            .map((accessory) => `<span class="cart-item-option-chip cart-item-option-chip--accessory"><small class="cart-item-option-name">${escapeHtml(accessory.name)}</small><small class="cart-item-option-status ${accessory.required ? "is-required" : "is-optional"}">${accessory.required ? "الزامی" : "اختیاری"}</small><small class="cart-item-option-price">${Number(accessory.price) > 0 ? `+ ${money(Number(accessory.price) * 1000000)}` : "رایگان"}</small></span>`)
            .join("")}</div></div>`
        : "";
      const inventoryVariantMarkup = inventoryVariant?.name
        ? `<div class="cart-item-option-group"><small class="cart-item-option-label">تنوع موجودی انبار</small><div class="cart-item-option-list"><span class="cart-item-option-chip"><strong>${escapeHtml(inventoryVariant.name)}</strong></span></div></div>`
        : "";
      return `<div class="cart-item-options" aria-label="تنوع انبار، گزینه‌های قیمت و متعلقات انتخاب‌شده">${inventoryVariantMarkup}${variantsMarkup}${accessoriesMarkup}</div>`;
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
    restoreAccountLines(lines) {
      const current = this.read();
      const key = (item) => JSON.stringify([String(item.id), Number(item.variantId) || null, String(item.salesPlanId || "regular")]);
      const known = new Set(current.map(key));
      const restored = [];
      for (const line of Array.isArray(lines) ? lines : []) {
        const identity = key(line);
        if (known.has(identity)) continue;
        const product = (window.HNProducts || []).find((candidate) => String(candidate.id) === String(line.id));
        if (!product) continue;
        restored.push({ ...product, id: String(line.id), variantId: line.variantId ?? null, quantity: Number(line.quantity), salesPlanId: line.salesPlanId, options: line.options || {} });
        known.add(identity);
      }
      return restored.length ? this.write([...current, ...restored]) : current;
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
      const existing = items.find((item) => item.id === safeProduct.id && item.variantId === safeProduct.variantId && item.salesPlanId === safeProduct.salesPlanId);
      const sameConfiguration =
        existing &&
        existing.salesPlanId === safeProduct.salesPlanId &&
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
      const saved = savedItems.find((item) => item.id === safeProduct.id && item.variantId === safeProduct.variantId && item.salesPlanId === safeProduct.salesPlanId);
      if (!saved || saved.quantity !== (sameConfiguration ? existing.quantity : safeProduct.quantity) || JSON.stringify(saved.options) !== JSON.stringify(safeProduct.options)) return savedItems;
      emit("hn:cart-added", {
            product: safeProduct,
            items: savedItems,
            trigger: trigger || document.activeElement,
          });
      return savedItems;
    },
    update(id, quantity, salesPlanId = "regular", variantId = null) {
      const items = this.read();
      const item = items.find((entry) => entry.id === String(id) && entry.salesPlanId === String(salesPlanId || "regular") && entry.variantId === (variantId ?? null));
      if (!item) return items;
      const nextQuantity = Number.parseInt(quantity, 10);
      if (!Number.isFinite(nextQuantity) || nextQuantity < 1) {
        return this.remove(id, salesPlanId, variantId);
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
      if (!limitedQuantity) return this.remove(id, salesPlanId, variantId);
      item.quantity = limitedQuantity;
      return this.write(items);
    },
    quantityLimit(item) {
      const stock = availableStock(item);
      const cartLimit = productCartLimit(productForCartItem(item));
      return stock === null ? cartLimit : Math.min(stock, cartLimit);
    },
    remove(id, salesPlanId = "regular", variantId = null) {
      return this.write(this.read().filter((item) => item.id !== String(id) || item.salesPlanId !== String(salesPlanId || "regular") || item.variantId !== (variantId ?? null)));
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
