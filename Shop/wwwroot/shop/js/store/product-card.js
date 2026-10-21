(function (window) {
  "use strict";
  if (window.HNProductCard) return;
  const esc = (value) => window.HNUI.escapeHtml(value);
  const fa = (value) => window.HNUI.formatNumber(value);
  const price = (product) => {
    const current = Math.round(Number(product.price) * 1000000);
    const percent = Number(product.discountPercent) || 0;
    const discounted = percent > 0 && percent < 100;
    return `<strong class="product-price-current">${fa(current)} <small>تومان</small></strong>${discounted
      ? `<span class="product-price-deal"><del class="price-old">${fa(Math.round(current / (1 - percent / 100)))} تومان</del><b class="product-price-discount">٪${fa(percent)}</b></span>` : ""}`;
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
    const url = window.HNProductUrl(product);
    const title = `<h3 class="${headingClass}"><a class="${slider ? "showcase-product-detail" : ""}" href="${esc(url)}">${esc(product.name)}</a></h3>`;
    const badge = options.badge ?? product.badge;
    const actionLabel = search ? '<span class="add-cart-label search-page-card-action-label">افزودن</span><span class="search-page-card-action-compact">افزودن</span>' : '<span class="add-cart-label">افزودن به سبد</span>';
    const action = favorite
      ? `<button class="list-remove-button" type="button" data-unlike="${esc(product.id)}" aria-label="حذف ${esc(product.name)} از علاقه‌مندی‌ها"><i class="bi bi-trash3" aria-hidden="true"></i><span>حذف از علاقه‌مندی‌ها</span></button>`
      : `<button class="add-cart${search ? " search-add-cart search-page-card-action" : ""}" type="button" data-id="${esc(product.id)}" data-name="${esc(product.name)}" data-price="${Math.round(product.price * 1000000)}" data-image="${esc(product.image)}"><i class="bi bi-bag-plus" aria-hidden="true"></i>${actionLabel}</button>`;
    const html = `<article class="${classes}" data-product-id="${esc(product.id)}" data-product-card="v1"><div class="product-image${search ? " search-page-card-media" : ""}"><a href="${esc(url)}" aria-label="مشاهده ${esc(product.name)}"><img src="${esc(options.image || product.image)}" alt="${esc(product.name)}" width="800" height="800" loading="lazy" decoding="async"></a><div class="product-card-actions" role="group" aria-label="عملیات محصول"><button class="favorite" type="button" data-product-id="${esc(product.id)}" aria-label="افزودن به علاقه‌مندی‌ها" aria-pressed="false"><i class="bi bi-heart" aria-hidden="true"></i></button><button class="compare-trigger" type="button" data-product-id="${esc(product.id)}" aria-label="افزودن به مقایسه" aria-pressed="false"><i class="bi bi-arrow-left-right" aria-hidden="true"></i></button></div>${badge ? `<span class="${options.offer ? "badge-offer" : badge === "جدید" ? "badge-new" : "badge-best"}">${esc(badge)}</span>` : ""}</div><div class="${bodyClass}">${slider ? title : `<div${search ? ' class="search-page-card-heading"' : ""}>${title}</div>`}<p class="${slider ? "product-card-sale-price" : search ? "search-product-price search-page-card-price" : favorite ? "favorite-item-price" : "product-card-price"} ${Number(product.discountPercent) > 0 ? "has-discount" : "is-regular"}">${price(product)}</p>${slider ? action : ""}</div>${slider ? "" : action}</article>`;
    return slider ? `<div class="showcase-slide">${html}</div>` : html;
  };
  window.HNProductCard = Object.freeze({ render, price });
})(window);
