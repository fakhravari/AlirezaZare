window.HNUI.mount("pages-magazine", document, () => {
  const buttons = [...document.querySelectorAll("[data-journal-filter]")];
  const posts = [...document.querySelectorAll(".journal-post")];
  const nav = document.getElementById("journal-pagination");
  const heading = document.getElementById("archive-title");
  const count = document.getElementById("journal-count");
  if (!nav || !heading || !count) return;
  const perPage = 15;
  let filter = "all";
  let page = 1;
  const number = (value) => window.HNUI.formatNumber(value);
  function scrollToResultsHeading() {
    const target = heading.closest(".journal-results-heading") || heading;
    const fixedHeader = document.getElementById("mainNav");
    const headerBottom = fixedHeader
      ? Math.max(0, fixedHeader.getBoundingClientRect().bottom)
      : 0;
    const top = Math.max(
      0,
      window.scrollY + target.getBoundingClientRect().top - headerBottom - 16,
    );
    window.scrollTo({ top, behavior: "auto" });
  }
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
    const matches = posts.filter((post) => {
      if (filter === "all") return true;
      if (filter.startsWith("area-"))
        return post.dataset.area === filter.slice("area-".length);
      return post.dataset.branch === filter || post.dataset.topic === filter;
    });
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
    window.HNUI.renderPagination(nav, page, pages, (target) => {
      page = target;
      render();
      saveLocation();
      heading.focus({ preventScroll: true });
      scrollToResultsHeading();
    }, "صفحه‌بندی مجله");
  }
  function focusResultsHeading() {
    heading.focus({ preventScroll: true });
    scrollToResultsHeading();
  }
  buttons.forEach((button) =>
    button.addEventListener("click", () => {
      filter = button.dataset.journalFilter;
      page = 1;
      render();
      saveLocation();
      focusResultsHeading();
    }),
  );
  window.addEventListener("popstate", () => {
    readLocation();
    render();
    saveLocation(true);
    focusResultsHeading();
  });
  readLocation();
  render();
  saveLocation(true);
});
