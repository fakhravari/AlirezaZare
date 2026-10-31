(function (window, document) {
  "use strict";

  const root = document.querySelector("#homeSalesPlans");
  const content = document.querySelector("#homeSalesPlansContent");
  if (!root || !content || !window.HNCatalog || !window.HNProductCard) return;

  let catalog = {};
  let sales = {};
  try {
    catalog = JSON.parse(document.querySelector("#storeCatalog")?.textContent || "{}");
    sales = JSON.parse(document.querySelector("#storeSalesPlans")?.textContent || "{}");
  } catch {
    return;
  }

  const now = Date.now();
  const plans = (sales.plans || []).filter((plan) => {
    if (!plan.isEnabled) return false;
    if (plan.startsAtUtc && Date.parse(plan.startsAtUtc) > now) return false;
    if (plan.endsAtUtc && Date.parse(plan.endsAtUtc) <= now) return false;
    return (sales.offers || []).some((offer) => offer.salesPlanId === plan.id && offer.isEnabled);
  });
  if (!plans.length) return;

  const products = new Map((catalog.products || []).map((product) => {
    const id = Number(product.id);
    return [id, window.HNCatalog.find(id)];
  }).filter(([, product]) => Boolean(product)));
  const escape = window.HNUI.escapeHtml;
  const number = window.HNUI.formatNumber;
  const sections = plans.map((plan) => {
    const offers = (sales.offers || []).filter((offer) =>
      offer.salesPlanId === plan.id && offer.isEnabled && Number(offer.stockQuantity) > 0 && products.has(Number(offer.productId)));
    if (!offers.length) return "";
    const regular = plan.isDefault || plan.id === "regular";
    return `<section class="home-sales-plan${regular ? " home-sales-plan--regular" : ""}" data-sales-plan="${escape(plan.id)}" aria-labelledby="homeSalesPlan-${escape(plan.id)}-title"><header class="category-showcase-head"><div>${regular ? "" : '<span class="eyebrow dark">پیشنهادهای فروشگاه</span>'}<h2 id="homeSalesPlan-${escape(plan.id)}-title">${escape(regular ? "فروشگاه تخصصی برق و ابزار برای انتخاب دقیق‌تر" : plan.name)}</h2>${!regular && plan.label ? `<p>${escape(plan.label)}</p>` : ""}</div>${regular ? `<nav class="home-sales-plan-filters" aria-label="دسته‌بندی کالاها"><a href="/search">همهٔ کالاها</a><a href="/search?category=power-tools">ابزار برقی</a><a href="/search?category=measurement">اندازه‌گیری</a><a href="/search?category=electronics">لحیم‌کاری</a><a href="/search?category=lighting">روشنایی</a></nav>` : '<a href="/search">مشاهدهٔ همهٔ کالاها <i class="bi bi-arrow-left" aria-hidden="true"></i></a>'}</header><div class="row g-3 home-sales-plan-grid" aria-label="${regular ? "کالاهای فروش عادی" : `کالاهای طرح ${escape(plan.name)}`}">${offers.map((offer) => {
      const product = products.get(Number(offer.productId));
      return `<div class="col-6 col-sm-4 col-md-3 col-xl-2">${window.HNProductCard.render(product, {
        salesPlanId: plan.id,
        priceAmount: Number(offer.priceAmount),
        discountPercent: Number(offer.discountPercent),
        stockQuantity: Number(offer.stockQuantity),
        salesPlanName: plan.name,
        salesPlanCode: plan.code,
        salesPlanLabel: plan.label,
        offer: true,
        badge: regular ? "" : Number(offer.discountPercent) > 0 ? `${number(offer.discountPercent)}٪ تخفیف` : plan.label || plan.name,
      })}</div>`;
    }).join("")}</div></section>`;
  }).join("");
  if (!sections) return;

  content.innerHTML = sections;
  root.hidden = false;
  window.HNUI.enhance?.(root);
})(window, document);
