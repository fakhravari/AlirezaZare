(() => {
  const buttons = [...document.querySelectorAll("[data-journal-filter]")];
  const posts = [...document.querySelectorAll(".journal-post")];
  const nav = document.getElementById("journal-pagination");
  const heading = document.getElementById("archive-title");
  const count = document.getElementById("journal-count");
  if (!nav || !heading || !count) return;
  const perPage = 12;
  let filter = "all";
  let page = 1;
  const number = (value) => window.HNUI.formatNumber(value);
  function readLocation() {
    const params = new URLSearchParams(window.location.search);
    filter = params.get("category") || "all";
    if (!buttons.some((button) => button.dataset.journalFilter === filter))
      filter = "all";
    const requestedPage = Number(params.get("page"));
    page =
      Number.isSafeInteger(requestedPage) && requestedPage > 0
        ? requestedPage
        : 1;
  }
  function saveLocation(replace = false) {
    const url = new URL(window.location.href);
    if (page > 1) url.searchParams.set("page", page);
    else url.searchParams.delete("page");
    if (filter !== "all") url.searchParams.set("category", filter);
    else url.searchParams.delete("category");
    if (url.href !== window.location.href)
      window.history[replace ? "replaceState" : "pushState"](null, "", url);
  }
  function render() {
    const matches = posts.filter(
      (post) =>
        filter === "all" ||
        post.dataset.branch === filter ||
        post.dataset.topic === filter,
    );
    const pages = Math.max(1, Math.ceil(matches.length / perPage));
    page = Math.max(1, Math.min(page, pages));
    buttons.forEach((button) => {
      const active = button.dataset.journalFilter === filter;
      button.classList.toggle("is-active", active);
      button.setAttribute("aria-pressed", String(active));
      if (active)
        heading.textContent =
          filter === "all" ? "همه نوشته‌های مجله" : button.textContent.trim();
    });
    const start = (page - 1) * perPage;
    const visible = new Set(matches.slice(start, start + perPage));
    posts.forEach((post) => {
      post.hidden = !visible.has(post);
    });
    count.textContent = matches.length
      ? `${number(start + 1)} تا ${number(Math.min(start + perPage, matches.length))} از ${number(matches.length)} مطلب`
      : "مطلبی در این دسته وجود ندارد";
    nav.replaceChildren();
    nav.hidden = pages <= 1;
    const add = (label, target, disabled, current = false) => {
      const button = document.createElement("button");
      button.type = "button";
      button.textContent = label;
      button.disabled = disabled;
      button.setAttribute(
        "aria-label",
        current
          ? `صفحه ${number(target)}، صفحه فعلی`
          : label === "قبلی" || label === "بعدی"
            ? `صفحه ${label}`
            : `صفحه ${number(target)}`,
      );
      if (current) button.setAttribute("aria-current", "page");
      button.addEventListener("click", () => {
        page = target;
        render();
        saveLocation();
        heading.focus({ preventScroll: true });
        heading.scrollIntoView({ block: "start", behavior: "auto" });
      });
      nav.append(button);
    };
    add("قبلی", page - 1, page === 1);
    const visibleCount = 5;
    const firstPage =
      pages <= visibleCount
        ? 1
        : page <= 3
          ? 1
          : page >= pages - 2
            ? pages - visibleCount + 1
            : page - 2;
    const visiblePages = [
      ...new Set([
        ...Array.from(
          { length: Math.min(visibleCount, pages) },
          (_, index) => firstPage + index,
        ),
        pages,
      ]),
    ].sort((a, b) => a - b);
    let previous = 0;
    visiblePages.forEach((value) => {
      if (previous && value - previous === 2)
        add(number(previous + 1), previous + 1, false);
      else if (previous && value - previous > 2) {
        const gap = document.createElement("span");
        gap.textContent = "…";
        gap.setAttribute("aria-hidden", "true");
        nav.append(gap);
      }
      add(number(value), value, false, value === page);
      previous = value;
    });
    add("بعدی", page + 1, page === pages);
  }
  buttons.forEach((button) =>
    button.addEventListener("click", () => {
      filter = button.dataset.journalFilter;
      page = 1;
      render();
      saveLocation();
    }),
  );
  window.addEventListener("popstate", () => {
    readLocation();
    render();
    saveLocation(true);
  });
  readLocation();
  render();
  saveLocation(true);
})();
