(() => {
  const section = document.getElementById("orders");
  if (!section) return;
  const cards = [...section.querySelectorAll(".profile-order-card")];
  const pageSize = 5;
  const pageCount = Math.max(1, Math.ceil(cards.length / pageSize));
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
  document.getElementById("ordersTotal").textContent =
    `${number(cards.length)} سفارش`;

  function render(moveFocus = false) {
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
    nav.replaceChildren();
    nav.hidden = pageCount <= 1;
    function add(label, target, disabled = false) {
      const button = document.createElement("button");
      button.type = "button";
      button.textContent = label;
      button.disabled = disabled;
      button.setAttribute("aria-label", `صفحه ${label}`);
      if (target === page && label === number(page))
        button.setAttribute("aria-current", "page");
      button.addEventListener("click", () => {
        const next = new URL(window.location.href);
        if (target === 1) next.searchParams.delete("page");
        else next.searchParams.set("page", target);
        if (next.href !== window.location.href)
          history.pushState(null, "", next);
        render(true);
      });
      nav.append(button);
    }
    add("قبلی", page - 1, page === 1);
    let previous = 0;
    for (const target of [...new Set([1, page - 1, page, page + 1, pageCount])]
      .filter((value) => value >= 1 && value <= pageCount)
      .sort((a, b) => a - b)) {
      if (previous && target - previous > 1) {
        const gap = document.createElement("span");
        gap.textContent = "…";
        gap.setAttribute("aria-hidden", "true");
        nav.append(gap);
      }
      add(number(target), target);
      previous = target;
    }
    add("بعدی", page + 1, page === pageCount);
    if (/\/orders\/?$/i.test(url.pathname) || url.hash === "#orders") {
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
  window.addEventListener("popstate", () => render());
  render();
})();
