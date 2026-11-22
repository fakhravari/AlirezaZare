window.HNUI.mount("pages-orders-pagination", document, () => {
  const section = document.getElementById("orders");
  if (!section) return;
  let cards = [...section.querySelectorAll(".profile-order-card")];
  const pageSize = 5;
  const number = (value) => window.HNUI.formatNumber(value);
  const footer = document.createElement("div");
  footer.className = "orders-pagination";
  const summary = document.createElement("p");
  summary.setAttribute("role", "status");
  const nav = document.createElement("nav");
  nav.className = "site-pagination";
  nav.setAttribute("aria-label", "صفحه‌بندی سفارش‌ها");
  footer.append(summary, nav);
  section.append(footer);
  function render(moveFocus = false) {
    const pageCount = Math.max(1, Math.ceil(cards.length / pageSize));
    const url = new URL(window.location.href);
    const requested = Number(url.searchParams.get("page"));
    const page =
      Number.isSafeInteger(requested) && requested > 0
        ? Math.min(requested, pageCount)
        : 1;
    const start = (page - 1) * pageSize;
    cards.forEach((card, index) => {
      card.hidden = index < start || index >= start + pageSize;
    });
    summary.textContent = cards.length
      ? `${number(start + 1)} تا ${number(Math.min(start + pageSize, cards.length))} از ${number(cards.length)} سفارش`
      : "هنوز سفارشی ثبت نشده است";
    window.HNUI.renderPagination(nav, page, pageCount, (target) => {
      const next = new URL(window.location.href);
      if (target === 1) next.searchParams.delete("page");
      else next.searchParams.set("page", target);
      if (next.href !== window.location.href)
        history.pushState(null, "", next);
      render(true);
    }, "صفحه‌بندی سفارش‌ها");
    if (cards.length && (/\/orders\/?$/i.test(url.pathname) || url.hash === "#orders")) {
      if (page === 1) url.searchParams.delete("page");
      else url.searchParams.set("page", page);
      if (url.href !== window.location.href)
        history.replaceState(null, "", url);
    }
    if (moveFocus) {
      const heading = document.getElementById("ordersTitle");
      heading.focus({ preventScroll: true });
      heading.scrollIntoView({ block: "start", behavior: "auto" });
    }
  }
  function refresh() {
    cards = [...section.querySelectorAll(".profile-order-card")];
    footer.hidden = !cards.length;
    document.getElementById("ordersTotal").textContent =
      `${number(cards.length)} سفارش`;
    if (!cards.length) {
      section.querySelector(".account-demo-note")?.setAttribute("hidden", "hidden");
      section.querySelector(".profile-order-list").innerHTML =
      '<div class="compare-empty empty-state-card"><div class="compare-empty-art" aria-hidden="true"><span><i class="bi bi-receipt"></i></span><i class="bi bi-plus-lg"></i></div><span class="compare-empty-kicker">تاریخچهٔ خرید شما</span><h2>هنوز سفارشی ثبت نکرده‌اید</h2><p>بعد از ثبت سفارش، خلاصهٔ فاکتور و وضعیت ارسال را در این بخش می‌بینید.</p><a class="flow-button" href="/search"><i class="bi bi-search" aria-hidden="true"></i> مشاهدهٔ محصولات <i class="bi bi-arrow-left" aria-hidden="true"></i></a><small class="compare-empty-hint"><i class="bi bi-truck" aria-hidden="true"></i> سفارش‌های ثبت‌شده و جزئیات پیگیری آن‌ها اینجا در دسترس خواهند بود.</small></div>';
    }
    render();
  }
  document.addEventListener("hn:orders-change", refresh);
  window.addEventListener("popstate", () => render());
  refresh();
});
