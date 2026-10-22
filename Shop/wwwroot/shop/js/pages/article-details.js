window.HNUI.mount("pages-article-details", document, () => {
  const page = document.querySelector("[data-article-slug]");
  if (!page) return;
  const formatNumber = (value) => window.HNUI.formatNumber(value);
  const body = page.querySelector(".article-body");
  document
    .getElementById("articlePrint")
    .addEventListener("click", () => window.print());
  const feedback = page.querySelector("[data-article-feedback]");
  if (feedback) {
    const slug = page.dataset.articleSlug;
    const key = `hn-article-feedback-v1-${slug}`;
    const metrics = { likes: Number(page.dataset.articleLikes) || 0 };
    const seed = [
      { name: "کاربر نمونه", text: "راهنمای خلاصه و کاربردی بود؛ نکته‌های انتخاب ابزار کمک‌کننده است.", rating: 5, sample: true },
      { name: "خوانندهٔ مجله", text: "مقایسهٔ نیاز و مشخصات دستگاه، انتخاب را ساده‌تر کرد.", rating: 4, sample: true },
      { name: "بازدیدکننده", text: "بخش نکات ایمنی و بررسی مدل را مفید دیدم.", rating: 5, sample: true },
    ];
    const readSaved = () => {
      try {
        const parsed = JSON.parse(window.HNUI.storage.get(key, "[]"));
        return Array.isArray(parsed) ? parsed.filter((item) => item && typeof item.name === "string" && typeof item.text === "string" && Number(item.rating) >= 1 && Number(item.rating) <= 5).slice(0, 50) : [];
      } catch { return []; }
    };
    let saved = readSaved();
    const list = feedback.querySelector("[data-feedback-list]");
    const renderFeedback = () => {
      const entries = [...saved.map((item) => ({ ...item, sample: false })).reverse(), ...seed];
      list.replaceChildren();
      entries.forEach((item) => {
        const card = document.createElement("article");
        card.className = "article-feedback-card";
        const heading = document.createElement("div");
        const name = document.createElement("strong"); name.textContent = item.name;
        const badge = document.createElement("small"); badge.textContent = item.sample ? "نمونهٔ نمایشی" : "دیدگاه شما";
        heading.append(name, badge);
        const stars = document.createElement("span"); stars.className = "article-feedback-stars"; stars.textContent = `★ ${item.rating} از ۵`;
        const copy = document.createElement("p"); copy.textContent = item.text;
        card.append(heading, stars, copy); list.append(card);
      });
      const all = [...saved, ...seed];
      const average = all.reduce((sum, item) => sum + Number(item.rating), 0) / all.length;
      feedback.querySelector("[data-feedback-average]").textContent = average.toLocaleString("fa-IR", { maximumFractionDigits: 1 });
      feedback.querySelector("[data-feedback-count]").textContent = `${all.length.toLocaleString("fa-IR")} نظر`;
      const commentStat = document.querySelector("[data-article-comment-count]");
      const ratingStat = document.querySelector("[data-article-rating]");
      commentStat?.querySelector("[data-article-comment-value]")?.replaceChildren(document.createTextNode(all.length.toLocaleString("fa-IR")));
      ratingStat?.querySelector("[data-article-rating-value]")?.replaceChildren(document.createTextNode(average.toLocaleString("fa-IR", { maximumFractionDigits: 1 })));
      commentStat?.setAttribute("aria-label", `${all.length.toLocaleString("fa-IR")} نظر`);
      ratingStat?.setAttribute("aria-label", `امتیاز ${average.toLocaleString("fa-IR", { maximumFractionDigits: 1 })} از ۵`);
    };
    let likes = Number(metrics.likes) || 0;
    let liked = window.HNUI.storage.get(`${key}-liked`, "false") === "true";
    const likeButton = feedback.querySelector("[data-article-like]");
    const paintLikes = () => {
      const count = Math.max(0, likes + (liked ? 1 : 0));
      likeButton.setAttribute("aria-pressed", String(liked));
      likeButton.querySelector("b").textContent = count.toLocaleString("fa-IR");
      likeButton.querySelector("span").textContent = liked ? "پسندیدید" : "پسندیدن";
      const likeStat = document.querySelector("[data-article-like-count]");
      likeStat?.querySelector("[data-article-like-value]")?.replaceChildren(document.createTextNode(count.toLocaleString("fa-IR")));
      likeStat?.setAttribute("aria-label", `${count.toLocaleString("fa-IR")} پسند`);
    };
    likeButton.addEventListener("click", () => { liked = !liked; window.HNUI.storage.set(`${key}-liked`, String(liked)); paintLikes(); });
    feedback.querySelector("[data-feedback-form]").addEventListener("submit", (event) => {
      event.preventDefault();
      const form = event.currentTarget;
      const data = new FormData(form);
      saved.push({ name: String(data.get("name")).trim().slice(0, 50), text: String(data.get("text")).trim().slice(0, 1000), rating: Math.max(1, Math.min(5, Number(data.get("rating")) || 5)), createdAt: new Date().toISOString() });
      saved = saved.slice(-50);
      const stored = window.HNUI.storage.set(key, JSON.stringify(saved));
      form.reset(); renderFeedback();
      feedback.querySelector("[data-feedback-status]").textContent = stored ? "دیدگاه در همین مرورگر ذخیره شد." : "دیدگاه نمایش داده شد، اما ذخیرهٔ مرورگر در دسترس نیست.";
    });
    renderFeedback(); paintLikes();
  }
  const checks = [...page.querySelectorAll(".article-checklist input")];
  checks.forEach((input) =>
    input.addEventListener("change", () => {
      window.HNUI.setMessage(
        document.getElementById("articleChecklistStatus"),
        `${formatNumber(checks.filter((item) => item.checked).length)} از ${formatNumber(checks.length)} مورد بررسی شده`,
        "info",
      );
    }),
  );
  const headings = [...body.querySelectorAll(".article-section h2[id]")];
  const links = [...page.querySelectorAll(".article-toc a")];
  const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
  const relatedViewport = page.querySelector("#articleRelatedViewport");
  if (relatedViewport) {
    const relatedCards = [...relatedViewport.querySelectorAll(".article-related-card")];
    const relatedButtons = [...page.querySelectorAll("[data-related-scroll]")];
    const updateRelatedButtons = () => {
      const bounds = relatedViewport.getBoundingClientRect();
      const visible = relatedCards
        .map((card, index) => ({ index, rect: card.getBoundingClientRect() }))
        .filter(({ rect }) => rect.right > bounds.left + 2 && rect.left < bounds.right - 2)
        .map(({ index }) => index);
      const first = visible.length ? Math.min(...visible) : 0;
      const last = visible.length ? Math.max(...visible) : relatedCards.length - 1;
      relatedButtons.forEach((button) => {
        button.disabled = button.dataset.relatedScroll === "previous"
          ? first <= 0
          : last >= relatedCards.length - 1;
      });
    };
    relatedButtons.forEach((button) => {
      button.addEventListener("click", () => {
        const visible = relatedCards
          .map((card, index) => ({ card, index, rect: card.getBoundingClientRect() }))
          .filter(({ rect }) => rect.right > relatedViewport.getBoundingClientRect().left + 2
            && rect.left < relatedViewport.getBoundingClientRect().right - 2);
        const targetIndex = button.dataset.relatedScroll === "previous"
          ? Math.max(0, Math.min(...visible.map(({ index }) => index)) - 1)
          : Math.min(relatedCards.length - 1, Math.max(...visible.map(({ index }) => index)) + 1);
        relatedCards[targetIndex]?.scrollIntoView({
          behavior: reducedMotion.matches ? "auto" : "smooth",
          block: "nearest",
          inline: "start",
        });
      });
    });
    relatedViewport.addEventListener("scroll", updateRelatedButtons, { passive: true });
    window.addEventListener("resize", updateRelatedButtons);
    updateRelatedButtons();
  }
  links.forEach((link) =>
    link.addEventListener("click", (event) => {
      if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey)
        return;
      const target = document.getElementById(
        decodeURIComponent(link.hash.slice(1)),
      );
      if (!target) return;
      event.preventDefault();
      const url = new URL(window.location.href);
      url.hash = link.hash;
      if (url.href !== window.location.href) history.pushState(null, "", url);
      target.setAttribute("tabindex", "-1");
      target.focus({ preventScroll: true });
      target.scrollIntoView({
        behavior: reducedMotion.matches ? "auto" : "smooth",
        block: "start",
      });
    }),
  );
  let scheduled = false;
  function updateProgress() {
    scheduled = false;
    const rect = body.getBoundingClientRect();
    const progress = Math.max(
      0,
      Math.min(1, -rect.top / Math.max(1, rect.height - window.innerHeight)),
    );
    document.getElementById("readingProgress").style.transform =
      `scaleX(${progress})`;
    const active = headings
      .filter((item) => item.getBoundingClientRect().top <= 180)
      .at(-1);
    links.forEach((link) => {
      if (active && decodeURIComponent(link.hash.slice(1)) === active.id)
        link.setAttribute("aria-current", "location");
      else link.removeAttribute("aria-current");
    });
  }
  function schedule() {
    if (!scheduled) {
      scheduled = true;
      requestAnimationFrame(updateProgress);
    }
  }
  window.addEventListener("scroll", schedule, { passive: true });
  window.addEventListener("resize", schedule);
  window.addEventListener("load", schedule);
  updateProgress();
});
