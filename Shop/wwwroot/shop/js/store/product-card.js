(function (window) {
  "use strict";
  if (window.HNProductCard) return;
  const esc = (value) => window.HNUI.escapeHtml(value);
  const fa = (value) => window.HNUI.formatNumber(value);
  const price = (product, options = {}) => {
    const current = Number.isFinite(Number(options.priceAmount))
      ? Math.round(Number(options.priceAmount) * (100 - (Number(options.discountPercent) || 0)) / 100)
      : Math.round(Number(product.price) * 1000000);
    const percent = Number(options.discountPercent ?? product.discountPercent) || 0;
    const discounted = percent > 0 && percent < 100;
    const listAmount = Number.isFinite(Number(options.priceAmount)) ? Number(options.priceAmount) : current / (1 - percent / 100);
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
    const baseUrl = window.HNProductUrl(product);
    const productUrl = options.salesPlanId
      ? `${baseUrl}${baseUrl.includes("?") ? "&" : "?"}salesPlanId=${encodeURIComponent(options.salesPlanId)}`
      : baseUrl;
    const title = `<h3 class="${headingClass}"><a class="${slider ? "showcase-product-detail" : ""}" href="${esc(productUrl)}">${esc(product.name)}</a></h3>`;
    const badge = options.badge ?? product.badge;
    const rawStock = options.stockQuantity ?? product.stockQuantity ?? product.inventoryCount ?? product.quantityInStock ?? product.availableQuantity ?? product.stock;
    const stock = rawStock === "" || rawStock === null || rawStock === undefined ? Number.NaN : Number(rawStock);
    const unavailable = Number.isFinite(stock) && stock >= 0 ? stock === 0 : product.inStock === false;
    const actionLabel = unavailable
      ? search ? '<span class="add-cart-label search-page-card-action-label">ناموجود</span><span class="search-page-card-action-compact">ناموجود</span>' : '<span class="add-cart-label">ناموجود</span>'
      : search ? '<span class="add-cart-label search-page-card-action-label">افزودن</span><span class="search-page-card-action-compact">افزودن</span>' : '<span class="add-cart-label">افزودن به سبد</span>';
    const action = favorite
      ? `<button class="list-remove-button" type="button" data-unlike="${esc(product.id)}" aria-label="حذف ${esc(product.name)} از علاقه‌مندی‌ها"><i class="bi bi-trash3" aria-hidden="true"></i><span>حذف از علاقه‌مندی‌ها</span></button>`
      : `<button class="add-cart${search ? " search-add-cart search-page-card-action" : ""}${unavailable ? " is-out-of-stock" : ""}" type="button" data-id="${esc(product.id)}" data-name="${esc(product.name)}" data-price="${Number.isFinite(Number(options.priceAmount)) ? Math.round(Number(options.priceAmount) * (100 - (Number(options.discountPercent) || 0)) / 100) : Math.round(product.price * 1000000)}" data-image="${esc(product.image)}"${options.salesPlanId ? ` data-sales-plan-id="${esc(options.salesPlanId)}"` : ""}${unavailable ? ' disabled aria-disabled="true" aria-label="ناموجود" title="موجودی این کالا تمام شده است"' : ""}><i class="bi ${unavailable ? "bi-bag-x" : "bi-bag-plus"}" aria-hidden="true"></i>${actionLabel}</button>`;
    const html = `<article class="${classes}" data-product-id="${esc(product.id)}" data-product-card="v1"><div class="product-image${search ? " search-page-card-media" : ""}"><a href="${esc(productUrl)}" aria-label="مشاهده ${esc(product.name)}"><img src="${esc(options.image || product.image)}" alt="${esc(product.name)}" width="800" height="800" loading="lazy" decoding="async"></a><div class="product-card-actions" role="group" aria-label="عملیات محصول"><button class="favorite" type="button" data-product-id="${esc(product.id)}" aria-label="افزودن به علاقه‌مندی‌ها" aria-pressed="false"><i class="bi bi-heart" aria-hidden="true"></i></button><button class="compare-trigger" type="button" data-product-id="${esc(product.id)}" aria-label="افزودن به مقایسه" aria-pressed="false"><i class="bi bi-arrow-left-right" aria-hidden="true"></i></button></div>${badge ? `<span class="${options.offer ? "badge-offer" : badge === "جدید" ? "badge-new" : "badge-best"}">${esc(badge)}</span>` : ""}</div><div class="${bodyClass}">${slider ? title : `<div${search ? ' class="search-page-card-heading"' : ""}>${title}</div>`}<p class="${slider ? "product-card-sale-price" : search ? "search-product-price search-page-card-price" : favorite ? "favorite-item-price" : "product-card-price"} ${(options.discountPercent ?? product.discountPercent) > 0 ? "has-discount" : "is-regular"}">${price(product, options)}</p>${slider ? action : ""}</div>${slider ? "" : action}</article>`;
    return slider ? `<div class="showcase-slide">${html}</div>` : html;
  };
  window.HNProductCard = Object.freeze({ render, price });
})(window);
