(function (window, document) {
  "use strict";

  const destinations = {
    featured: {
      root: document.querySelector("#homeSalesTime"),
      content: document.querySelector("#homeSalesTimeContent"),
    },
    collection: {
      root: document.querySelector("#homeSalesPlans"),
      content: document.querySelector("#homeSalesPlansContent"),
    },
  };
  if (Object.values(destinations).some(({ root, content }) => !root || !content) || !window.HNCatalog || !window.HNProductCard) return;

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
    if (plan.isDefault || plan.id === "regular" || !plan.isEnabled) return false;
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
  const sections = {featured: [], collection: []};
  plans.forEach((plan) => {
    const offers = (sales.offers || []).filter((offer) =>
      offer.salesPlanId === plan.id && offer.isEnabled && Number(offer.stockQuantity) > 0 && products.has(Number(offer.productId)));
    if (!offers.length) return;
    const placement = plan.type === "digi-time" ? "featured" : "collection";
    const endTime = plan.endsAtUtc ? Date.parse(plan.endsAtUtc) : NaN;
    const countdown = Number.isFinite(endTime)
      ? `<div class="home-sales-plan-countdown"><span><i class="bi bi-hourglass-split" aria-hidden="true"></i> فرصت باقی‌مانده</span><time data-sales-plan-countdown="${escape(plan.endsAtUtc)}"></time></div>`
      : "";
    sections[placement].push(`<section class="home-sales-plan" data-sales-plan="${escape(plan.id)}" aria-labelledby="homeSalesPlan-${escape(plan.id)}-title"><header class="category-showcase-head"><div><span class="eyebrow dark">پیشنهاد ویژه</span><h2 id="homeSalesPlan-${escape(plan.id)}-title">${escape(plan.name)}</h2>${countdown}${plan.label ? `<p>${escape(plan.label)}</p>` : ""}</div><a href="/Search?sales-plan=${encodeURIComponent(plan.id)}">مشاهدهٔ همهٔ کالاهای این طرح <i class="bi bi-arrow-left" aria-hidden="true"></i></a></header><div class="row g-3 home-sales-plan-grid" aria-label="کالاهای طرح ${escape(plan.name)}">${offers.map((offer) => {
      const product = products.get(Number(offer.productId));
      return `<div class="col-6 col-sm-4 col-md-3 col-xl-2">${window.HNProductCard.render(product, {
        salesPlanId: plan.id,
        priceAmount: Number(offer.priceAmount),
        discountPercent: Number(offer.discountPercent),
        stockQuantity: Number(offer.stockQuantity),
        salesPlanName: plan.name,
        salesPlanCode: plan.code,
        salesPlanLabel: plan.label,
        maxPurchaseQuantity: Number(offer.maxPurchaseQuantity),
        offer: true,
      })}</div>`;
    }).join("")}</div></section>`);
  });

  Object.entries(destinations).forEach(([placement, {root, content}]) => {
    const markup = sections[placement].join("");
    if (!markup) return;
    content.innerHTML = markup;
    root.hidden = false;
    const updateCountdowns = () => content.querySelectorAll("[data-sales-plan-countdown]").forEach((timer) => {
      const remaining = Math.max(0, Date.parse(timer.dataset.salesPlanCountdown) - Date.now());
      if (!remaining) {
        timer.textContent = "پایان‌یافته";
        timer.closest(".home-sales-plan-countdown")?.classList.add("is-expired");
        return;
      }
      const totalSeconds = Math.floor(remaining / 1000);
      const days = Math.floor(totalSeconds / 86400);
      const hours = Math.floor((totalSeconds % 86400) / 3600);
      const minutes = Math.floor((totalSeconds % 3600) / 60);
      const seconds = totalSeconds % 60;
      const pad = (value) => number(value).padStart(2, "۰");
      const clock = `${pad(hours)}:${pad(minutes)}:${pad(seconds)}`;
      timer.innerHTML = `${days ? `<span class="sales-plan-countdown-days" dir="rtl">${number(days)} روز</span>` : ""}<bdi dir="ltr">${clock}</bdi>`;
      timer.setAttribute("aria-label", `${days ? `${number(days)} روز و ` : ""}${number(hours)} ساعت و ${number(minutes)} دقیقه و ${number(seconds)} ثانیه`);
    });
    updateCountdowns();
    if (content.querySelector("[data-sales-plan-countdown]")) window.setInterval(updateCountdowns, 1000);
    window.HNUI.enhance?.(root);
  });
})(window, document);
