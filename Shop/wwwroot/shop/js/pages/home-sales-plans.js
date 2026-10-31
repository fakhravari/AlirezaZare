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
  const regularPlan = plans.find((plan) => plan.isDefault || plan.id === "regular");
  if (regularPlan) {
    const collection = catalog.collections?.selected || [];
    const regularOffers = new Map((sales.offers || [])
      .filter((offer) => offer.salesPlanId === regularPlan.id && offer.isEnabled)
      .map((offer) => [Number(offer.productId), offer]));
    const defaultProducts = collection.map((id) => products.get(Number(id))).filter(Boolean);
    if (defaultProducts.length) {
      const filters = `<nav class="home-sales-plan-filters" aria-label="دسته‌بندی کالاها"><button type="button" class="is-active" data-home-plan-filter="all" aria-pressed="true">همه</button><button type="button" data-home-plan-filter="power-tools" aria-pressed="false">ابزار برقی</button><button type="button" data-home-plan-filter="measurement" aria-pressed="false">اندازه‌گیری</button><button type="button" data-home-plan-filter="electronics" aria-pressed="false">لحیم‌کاری</button><button type="button" data-home-plan-filter="lighting" aria-pressed="false">روشنایی</button></nav>`;
      const cards = defaultProducts.map((product) => {
        const offer = regularOffers.get(Number(product.id));
        if (!offer) return "";
        const category = ({
          "ابزار برقی": "power-tools",
          "ابزار اندازه‌گیری": "measurement",
          "لحیم‌کاری و تعمیر برد": "electronics",
          "روشنایی و لامپ": "lighting",
        })[product.categoryPath?.[1]] || "all";
        return `<div class="col-6 col-md-4 col-lg-3 col-xxl-2 home-sales-plan-column" data-category="${category}">${window.HNProductCard.render(product, {
          priceAmount: Number(offer.priceAmount), discountPercent: Number(offer.discountPercent), stockQuantity: Number(offer.stockQuantity),
        })}</div>`;
      }).join("");
      const regularSection = `<section class="home-sales-plan home-sales-plan--regular" data-sales-plan="regular" aria-labelledby="homeSalesPlan-regular-title"><header class="category-showcase-head"><div><span class="eyebrow dark">منتخب فروشگاه ندا اولیائی</span><h2 id="homeSalesPlan-regular-title">فروشگاه تخصصی برق و ابزار <em>برای انتخاب دقیق‌تر</em></h2></div>${filters}</header><div class="row g-3 home-sales-plan-grid" aria-label="محصولات فروشگاه">${cards}</div><a class="home-sales-plan-all" href="/search">مشاهدهٔ همهٔ کالاها <i class="bi bi-arrow-left" aria-hidden="true"></i></a></section>`;
      content.innerHTML = regularSection;
    }
  }
  const sections = plans.filter((plan) => !plan.isDefault && plan.id !== "regular").map((plan) => {
    const offers = (sales.offers || []).filter((offer) =>
      offer.salesPlanId === plan.id && offer.isEnabled && Number(offer.stockQuantity) > 0 && products.has(Number(offer.productId)));
    if (!offers.length) return "";
    return `<section class="home-sales-plan" data-sales-plan="${escape(plan.id)}" aria-labelledby="homeSalesPlan-${escape(plan.id)}-title"><header class="category-showcase-head"><div><span class="eyebrow dark">پیشنهادهای فروشگاه</span><h2 id="homeSalesPlan-${escape(plan.id)}-title">${escape(plan.name)}</h2>${plan.label ? `<p>${escape(plan.label)}</p>` : ""}</div><a href="/search">مشاهدهٔ همهٔ کالاها <i class="bi bi-arrow-left" aria-hidden="true"></i></a></header><div class="row g-3 home-sales-plan-grid" aria-label="کالاهای طرح ${escape(plan.name)}">${offers.map((offer) => {
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
        badge: Number(offer.discountPercent) > 0 ? `${number(offer.discountPercent)}٪ تخفیف` : plan.label || plan.name,
      })}</div>`;
    }).join("")}</div></section>`;
  }).join("");
  if (!sections) return;

  content.innerHTML = sections;
  root.hidden = false;
  window.HNUI.enhance?.(root);
})(window, document);
