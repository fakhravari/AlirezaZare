(function (window) {
  "use strict";
  if (window.HNProductCard) return;
  const esc = (value) => window.HNUI.escapeHtml(value);
  const fa = (value) => window.HNUI.formatNumber(value);
  const badgeStyle = (color, textColor, borderColor) => {
    if (!/^#[0-9a-f]{6}$/i.test(color || "")) return "";
    const channels = [1, 3, 5].map(index => parseInt(color.slice(index, index + 2), 16) / 255).map(value => value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4);
    const fallback = 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2] > 0.179 ? "#000000" : "#ffffff";
    const text = /^#[0-9a-f]{6}$/i.test(textColor || "") ? textColor : fallback;
    const border = /^#[0-9a-f]{6}$/i.test(borderColor || "") ? borderColor : fallback;
    return ` style="background:${color}!important;color:${text}!important;border:1px solid ${border}!important"`;
  };
  let salesPlanText, salesPlanData;
  const getSalesPlanData = () => {
    const text = document.querySelector("#storeSalesPlans")?.textContent || "null";
    if (text !== salesPlanText) {
      salesPlanText = text;
      try { salesPlanData = JSON.parse(text); } catch { salesPlanData = null; }
    }
    return salesPlanData;
  };
  const selectVariant = (variants, planId) => {
    const eligible = variants.filter((variant) => {
      const amount = variant.priceBySalesPlan?.[planId];
      const finalAmount = variant.finalPriceBySalesPlan?.[planId];
      return !variant.disabledSalesPlanIds?.includes(planId) && amount != null && finalAmount != null && Number.isFinite(Number(amount)) && Number.isFinite(Number(finalAmount));
    }).sort((left, right) => Number(right.isDefault === true) - Number(left.isDefault === true) || Number(left.variantId) - Number(right.variantId));
    return eligible.find((variant) => Number(variant.stockBySalesPlan?.[planId] || 0) > 0) || eligible[0] || null;
  };
  const priceDetails = (product, options = {}) => {
    const data = getSalesPlanData();
    const planId = options.salesPlanId || options.id || data?.plans?.find((plan) => plan.isDefault)?.id;
    const offer = data?.offers?.find((item) => Number(item.productId) === Number(product.id) && item.salesPlanId === planId);
    const plan = data?.plans?.find((item) => item.id === planId);
    if (plan?.isEnabled === false || (plan?.startsAtUtc && Date.now() < Date.parse(plan.startsAtUtc)) || (plan?.endsAtUtc && Date.now() >= Date.parse(plan.endsAtUtc)) || offer?.isEnabled === false)
      return { current: null, list: null, discount: 0, adjustments: 0, priceUnavailable: true };
    const productVariants = data?.inventoryVariants?.filter((item) => Number(item.productId) === Number(product.id)) || [];
    const variant = selectVariant(productVariants, planId);
    const variantAmount = variant?.priceBySalesPlan?.[planId];
    const hasAmount = (amount) => amount !== null && amount !== undefined && Number.isFinite(Number(amount));
    if (productVariants.length && (!variant || !hasAmount(variantAmount))) return { current: null, list: null, discount: 0, adjustments: 0, priceUnavailable: true, variantDisabled: productVariants.every((item) => item.disabledSalesPlanIds?.includes(planId) === true) };
    if (Array.isArray(data?.offers) && !hasAmount(offer?.priceAmount) && !hasAmount(options.priceAmount) && !hasAmount(variantAmount)) return { current: null, list: null, discount: 0, adjustments: 0, priceUnavailable: true };
    const baseAmount = hasAmount(variantAmount) ? Number(variantAmount) : hasAmount(offer?.priceAmount) ? Number(offer.priceAmount) : hasAmount(options.priceAmount) ? Number(options.priceAmount) : null;
    const discount = hasAmount(variantAmount) ? Number(variant.discountBySalesPlan?.[planId]) || 0 : Number(offer?.discountPercent ?? options.discountPercent ?? product.discountPercent) || 0;
    const offeredFinal = hasAmount(variantAmount) ? variant?.finalPriceBySalesPlan?.[planId] : (offer?.finalPriceAmount ?? options.finalPriceAmount);
    const baseCurrent = baseAmount === null ? Number(product.finalPriceAmount ?? Math.round(Number(product.price) * 1000000)) : hasAmount(offeredFinal) ? Number(offeredFinal) : null;
    const accessoryAmount = (product.accessories || []).filter((item) => item.required).reduce((sum, item) => sum + (Number(offer?.accessoryPriceAmounts?.[item.id] ?? Number(item.price) * 1000000) || 0), 0);
    const adjustments = accessoryAmount;
    return { current: baseCurrent === null ? null : baseCurrent + adjustments, list: baseAmount, discount, adjustments };
  };
  const currentPrice = (product, options = {}) => priceDetails(product, options).current;
  const availableStock = (product, options = {}) => {
    const hasAmount = (amount) => amount !== null && amount !== undefined && Number.isFinite(Number(amount));
    const data = getSalesPlanData();
    if (!Array.isArray(data?.plans) || !Array.isArray(data?.offers) || !Array.isArray(data?.inventoryVariants)) return null;
    const planId = options.salesPlanId || data.plans.find((plan) => plan.isDefault)?.id;
    const plan = data.plans.find((item) => item.id === planId);
    if (!plan || plan.isEnabled === false || (plan.startsAtUtc && Date.now() < Date.parse(plan.startsAtUtc)) || (plan.endsAtUtc && Date.now() >= Date.parse(plan.endsAtUtc))) return 0;
    const offer = data.offers.find((item) => Number(item.productId) === Number(product.id) && item.salesPlanId === planId);
    if (!offer || !offer.isEnabled) return 0;
    let stock = Number(offer.stockQuantity);
    const variants = data.inventoryVariants.filter((item) => Number(item.productId) === Number(product.id));
    if (variants.length) {
      const variant = selectVariant(variants, planId);
      if (!variant || !hasAmount(variant.priceBySalesPlan?.[planId])) return 0;
      stock = Math.min(stock, Number(variant.stockBySalesPlan?.[planId] ?? 0));
    }
    return Number.isFinite(stock) ? Math.max(0, stock) : 0;
  };
  const price = (product, options = {}) => {
    const { current, list, discount: percent, adjustments, priceUnavailable, variantDisabled } = priceDetails(product, options);
    if (priceUnavailable || current == null) return `<span class="product-price-unavailable">${variantDisabled ? "تنوع قابل عرضه موجود نیست" : "قیمت تنوع ثبت نشده"}</span>`;
    const discounted = percent > 0 && percent < 100;
    const listAmount = list + adjustments;
    return `<strong class="product-price-current">${fa(current)} <small>تومان</small></strong>${discounted
      ? `<span class="product-price-deal"><del class="price-old">${fa(Math.round(listAmount))} تومان</del><b class="product-price-discount">٪${fa(percent)}</b></span>` : ""}`;
  };
  const render = (product, options = {}) => {
    const variant = options.variant || "grid";
    const slider = variant === "slider";
    const search = variant === "search";
    const favorite = variant === "favorite";
    const classes = slider ? "showcase-card" : search ? "product-card search-product search-page-card"
      : variant === "related" ? "product-card product-detail-related-card"
      : favorite ? "product-card flow-card favorite-item-card" : "product-card";
    const headingClass = search ? "search-page-card-title" : "";
    const bodyClass = slider ? "showcase-card-body" : "product-info" + (search ? " search-page-card-content" : "");
    const current = currentPrice(product, options);
    const productUrl = window.HNProductUrl(product, {salesPlanId: options.salesPlanId});
    const title = `<h3 class="${headingClass}"><a class="${slider ? "showcase-product-detail" : ""}" href="${esc(productUrl)}">${esc(product.name)}</a></h3>`;
    const rawStock = availableStock(product, options) ?? options.stockQuantity ?? product.stockQuantity ?? product.inventoryCount ?? product.quantityInStock ?? product.availableQuantity ?? product.stock;
    const planId = options.salesPlanId;
    const plan = planId ? salesPlanData?.plans?.find((item) => item.id === planId) : null;
    const regularPlan = !planId || planId === "regular" || plan?.isDefault === true;
    const badge = regularPlan ? (options.badge ?? product.badge) : null;
    const stock = rawStock === "" || rawStock === null || rawStock === undefined ? Number.NaN : Number(rawStock);
    const priceUnavailable = current == null || !Number.isFinite(Number(current));
    const variantDisabled = priceDetails(product, options).variantDisabled === true;
    const unavailable = priceUnavailable || (Number.isFinite(stock) && stock >= 0 ? stock < 1 : product.inStock === false);
    const actionLabel = variantDisabled
      ? '<span class="add-cart-label">ناموجود</span>'
      : priceUnavailable
      ? '<span class="add-cart-label">قیمت ناموجود</span>'
      : unavailable
      ? search ? '<span class="add-cart-label search-page-card-action-label">ناموجود</span><span class="search-page-card-action-compact">ناموجود</span>' : '<span class="add-cart-label">ناموجود</span>'
      : search ? '<span class="add-cart-label search-page-card-action-label">افزودن</span><span class="search-page-card-action-compact">افزودن</span>' : '<span class="add-cart-label">افزودن به سبد</span>';
    const action = favorite
      ? `<button class="list-remove-button" type="button" data-unlike="${esc(product.id)}" aria-label="حذف ${esc(product.name)} از علاقه‌مندی‌ها"><i class="bi bi-trash3" aria-hidden="true"></i><span>حذف از علاقه‌مندی‌ها</span></button>`
      : `<button class="add-cart${search ? " search-add-cart search-page-card-action" : ""}${unavailable ? " is-out-of-stock" : ""}" type="button" data-id="${esc(product.id)}" data-name="${esc(product.name)}" data-price="${current ?? ""}" data-image="${esc(product.image)}"${options.salesPlanId ? ` data-sales-plan-id="${esc(options.salesPlanId)}" data-stock-quantity="${Math.max(0, Number(options.stockQuantity) || 0)}" data-max-purchase-quantity="${Math.max(1, Number(options.maxPurchaseQuantity) || 99)}"` : ""}${options.salesPlanName ? ` data-sales-plan-name="${esc(options.salesPlanName)}"` : ""}${options.salesPlanCode ? ` data-sales-plan-code="${esc(options.salesPlanCode)}"` : ""}${options.salesPlanLabel ? ` data-sales-plan-label="${esc(options.salesPlanLabel)}"` : ""}${unavailable ? variantDisabled ? ' disabled aria-disabled="true" aria-label="ناموجود" title="تنوع قابل عرضه موجود نیست"' : priceUnavailable ? ' disabled aria-disabled="true" aria-label="قیمت ناموجود" title="قیمت این کالا ثبت نشده است"' : ' disabled aria-disabled="true" aria-label="ناموجود" title="موجودی این کالا تمام شده است"' : ""}><i class="bi ${unavailable ? "bi-bag-x" : "bi-bag-plus"}" aria-hidden="true"></i>${actionLabel}</button>`;
    const hasSalesPlanBadge = Boolean(options.salesPlanName && options.salesPlanId && !regularPlan);
    const imageBadge = hasSalesPlanBadge
      ? `<span class="product-sales-plan-badge product-sales-plan-badge--overlay"><i class="bi bi-lightning-charge-fill" aria-hidden="true"></i>${esc(options.salesPlanName)}</span>`
      : badge ? `<span class="${options.offer ? "badge-offer" : badge === "جدید" ? "badge-new" : "badge-best"}"${options.badge == null ? badgeStyle(product.badgeColor, product.badgeTextColor, product.badgeBorderColor) : ""}>${esc(badge)}</span>` : "";
    const html = `<article class="${classes}" data-product-id="${esc(product.id)}" data-product-card="v1"${options.salesPlanId ? ` data-sales-plan-id="${esc(options.salesPlanId)}" data-sales-plan-name="${esc(options.salesPlanName || "")}" data-sales-plan-code="${esc(options.salesPlanCode || "")}" data-sales-plan-label="${esc(options.salesPlanLabel || "")}"` : ""}><div class="product-image${search ? " search-page-card-media" : ""}"><a href="${esc(productUrl)}" aria-label="مشاهده ${esc(product.name)}"><img src="${esc(options.image || product.image)}" alt="${esc(product.name)}" width="800" height="800" loading="lazy" decoding="async"></a><div class="product-card-actions" role="group" aria-label="عملیات محصول"><button class="favorite" type="button" data-product-id="${esc(product.id)}" aria-label="افزودن به علاقه‌مندی‌ها" aria-pressed="false"><i class="bi bi-heart" aria-hidden="true"></i></button><button class="compare-trigger" type="button" data-product-id="${esc(product.id)}" aria-label="افزودن به مقایسه" aria-pressed="false"><i class="bi bi-arrow-left-right" aria-hidden="true"></i></button></div>${imageBadge}</div><div class="${bodyClass}">${slider ? title : `<div${search ? ' class="search-page-card-heading"' : ""}>${title}</div>`}<p class="${slider ? "product-card-sale-price" : search ? "search-product-price search-page-card-price" : favorite ? "favorite-item-price" : "product-card-price"} ${priceDetails(product, options).discount > 0 ? "has-discount" : "is-regular"}">${price(product, options)}</p>${slider ? action : ""}</div>${slider ? "" : action}</article>`;
    return slider ? `<div class="showcase-slide">${html}</div>` : html;
  };
  window.HNProductCard = Object.freeze({ render, price, priceDetails, currentPrice, availableStock, selectVariant });
})(window);
