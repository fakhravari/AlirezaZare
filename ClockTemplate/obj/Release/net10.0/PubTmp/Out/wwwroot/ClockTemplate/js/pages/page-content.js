document.addEventListener("DOMContentLoaded", () => {
  const share = document.querySelector(".article-share");
  if (share) {
    const url = encodeURIComponent(window.location.href);
    const title = encodeURIComponent(document.title);
    share.querySelector('[data-share="telegram"]')?.setAttribute("href", `https://t.me/share/url?url=${url}&text=${title}`);
    share.querySelector('[data-share="whatsapp"]')?.setAttribute("href", `https://wa.me/?text=${title}%20${url}`);
    const destinations = {
      linkedin: `https://www.linkedin.com/sharing/share-offsite/?url=${url}`,
      x: `https://twitter.com/intent/tweet?url=${url}&text=${title}`,
      facebook: `https://www.facebook.com/sharer/sharer.php?u=${url}`,
      email: `mailto:?subject=${title}&body=${title}%0A${url}`,
    };
    Object.entries(destinations).forEach(([name, href]) => share.querySelector(`[data-share="${name}"]`)?.setAttribute('href', href));
    const nativeShare = share.querySelector('[data-share="native"]');
    if (nativeShare && typeof navigator.share === 'function') {
      nativeShare.hidden = false;
      nativeShare.addEventListener('click', async () => {
        try { await navigator.share({ title: document.title, url: window.location.href }); }
        catch (error) { if (error.name !== 'AbortError') share.querySelector('.article-share-status').textContent = 'اشتراک‌گذاری انجام نشد؛ از کپی لینک استفاده کنید.'; }
      });
    }
    share.querySelector('[data-share="copy"]')?.addEventListener("click", async (event) => {
      const button = event.currentTarget;
      const status = share.querySelector(".article-share-status");
      try { await navigator.clipboard.writeText(window.location.href); }
      catch { status.textContent = 'کپی خودکار ممکن نشد؛ لینک را از نوار آدرس کپی کنید.'; return; }
      status.textContent = "لینک مطلب کپی شد.";
      button.classList.add("copied");
      setTimeout(() => {
        status.textContent = "";
        button.classList.remove("copied");
      }, 2200);
    });
  }

  const form = document.querySelector("#contactForm");
  // Contact validation and submit feedback live in contact.js.
  if (form && !form.matches("#contactForm")) {
    form.addEventListener("submit", (e) => {
      e.preventDefault();
      const button = form.querySelector('button[type="submit"]');
      const original = button.innerHTML;
      button.innerHTML = '<i class="bi bi-check2"></i> پیام شما ثبت شد';
      button.disabled = true;
      setTimeout(() => {
        button.innerHTML = original;
        button.disabled = false;
        form.reset();
      }, 2200);
    });
  }
  document.querySelectorAll(".blog-filter button").forEach((button) =>
    button.addEventListener("click", () => {
      document
        .querySelectorAll(".blog-filter button")
        .forEach((item) => item.classList.remove("active"));
      button.classList.add("active");
    }),
  );
});

document
  .querySelector(".blog-newsletter form")
  ?.addEventListener("submit", (event) => {
    event.preventDefault();
    const button = event.currentTarget.querySelector("button");
    button.innerHTML = '<i class="bi bi-check2"></i> عضو شدید';
    event.currentTarget.querySelector("input").value = "";
  });
document
  .querySelector(".blog-more button")
  ?.addEventListener("click", (event) => {
    event.currentTarget.innerHTML =
      '<i class="bi bi-check2"></i> همه مطالب نمایش داده شده‌اند';
    event.currentTarget.disabled = true;
  });

document.addEventListener("DOMContentLoaded", () => {
  const articlesSection = document.querySelector("#articles");
  const paginationHost = document.querySelector(".blog-more");
  if (!articlesSection || !paginationHost) return;
  const cards = [...articlesSection.querySelectorAll(".row.g-4>.col-md-6")];
  const filters = [...articlesSection.querySelectorAll(".blog-filter button")];
  const perPage = 6;
  let currentPage = 1;
  let activeFilter = "همه مطالب";
  paginationHost.className = "blog-pagination-wrap";
  paginationHost.innerHTML =
    '<nav class="blog-pagination site-pagination" aria-label="صفحه‌بندی مطالب"></nav><p class="blog-page-status" aria-live="polite"></p>';
  const nav = paginationHost.querySelector(".blog-pagination");
  const status = paginationHost.querySelector(".blog-page-status");
  const filteredCards = () =>
    activeFilter === "همه مطالب"
      ? cards
      : cards.filter(
          (card) =>
            card.querySelector(".article-meta span")?.textContent.trim() ===
            activeFilter,
        );
  const render = (scroll = false) => {
    const list = filteredCards();
    const totalPages = Math.max(1, Math.ceil(list.length / perPage));
    currentPage = Math.min(currentPage, totalPages);
    cards.forEach((card) => (card.hidden = true));
    list
      .slice((currentPage - 1) * perPage, currentPage * perPage)
      .forEach((card) => (card.hidden = false));
      nav.innerHTML = `<button type="button" class="page-step" data-page="${currentPage - 1}" ${currentPage === 1 ? "disabled" : ""} aria-label="صفحه قبل"><i class="bi bi-chevron-right"></i><span>قبلی</span></button>${Array.from({ length: totalPages }, (_, index) => `<button type="button" class="page-number${index + 1 === currentPage ? " active" : ""}" data-page="${index + 1}" ${index + 1 === currentPage ? 'aria-current="page"' : ""}>${window.HNUI.formatNumber(index + 1)}</button>`).join("")}<button type="button" class="page-step" data-page="${currentPage + 1}" ${currentPage === totalPages ? "disabled" : ""} aria-label="صفحه بعد"><span>بعدی</span><i class="bi bi-chevron-left"></i></button>`;
      status.textContent = `صفحه ${window.HNUI.formatNumber(currentPage)} از ${window.HNUI.formatNumber(totalPages)} — ${window.HNUI.formatNumber(list.length)} مطلب`;
    nav.querySelectorAll("button[data-page]").forEach((button) =>
      button.addEventListener("click", () => {
        if (button.disabled) return;
        currentPage = Number(button.dataset.page);
        render(true);
      }),
    );
    if (scroll)
      articlesSection.scrollIntoView({ behavior: "smooth", block: "start" });
  };
  filters.forEach((button) =>
    button.addEventListener("click", () => {
      activeFilter = button.textContent.trim();
      currentPage = 1;
      render();
    }),
  );
  render();
});
