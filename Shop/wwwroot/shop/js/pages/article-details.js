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
      { name: "خوانندهٔ مجله", text: "راهنما خلاصه و کاربردی بود؛ نکته‌های انتخاب ابزار کمک‌کننده است.", rating: 5, reply: "خوشحالیم که راهنما برای انتخاب ابزار به شما کمک کرده است." },
      { name: "کاربر کارگاهی", text: "مقایسهٔ نیاز و مشخصات دستگاه، انتخاب را ساده‌تر کرد.", rating: 4, reply: "ممنون از بازخوردتان؛ بررسی نیاز کارگاه پیش از انتخاب، نکتهٔ مهمی است." },
      { name: "بازدیدکننده", text: "بخش نکات ایمنی مفید بود، اما توضیح بعضی مراحل می‌توانست کامل‌تر باشد.", rating: 3 },
      { name: "خواننده", text: "ترتیب مراحل روشن است و برای شروع کار راهنمای خوبی به حساب می‌آید.", rating: 5 },
      { name: "مخاطب مجله", text: "پیشنهاد بررسی مشخصات دستگاه پیش از خرید برایم مفید بود.", rating: 4, reply: "از همراهی شما سپاسگزاریم. مشخصات هر مدل را در صفحهٔ محصول می‌توانید بررسی کنید." },
      { name: "کاربر ابزار", text: "موضوع خوب توضیح داده شده، ولی چند نکتهٔ فنی برایم مبهم ماند.", rating: 2 },
      { name: "خوانندهٔ راهنما", text: "مطالب کوتاه و قابل استفاده بودند؛ مخصوصاً بخش نگهداری ابزار.", rating: 5 },
      { name: "بازدیدکنندهٔ سایت", text: "راهنمای مناسبی است و مثال‌های بیشتری می‌تواند آن را بهتر کند.", rating: 3 },
      { name: "کاربر مجله", text: "نکته‌های انتخاب با نیاز کارگاهی من هم‌خوانی داشت.", rating: 4 },
      { name: "خوانندهٔ سایت", text: "موضوع اصلی را پوشش می‌دهد و توضیحاتش روان است.", rating: 5 },
    ].map((item, index) => ({
      ...item,
      createdAt: new Date(Date.now() - (index + 1) * 86400000).toISOString(),
      ...(item.reply ? { replyAt: new Date(Date.now() - (index + 1) * 86400000 + 3600000).toISOString() } : {}),
    }));
    const pageSize = 5;
    let reviewPage = 1;
    const readSaved = () => {
      try {
        const parsed = JSON.parse(window.HNUI.storage.get(key, "[]"));
        return Array.isArray(parsed) ? parsed.filter((item) => item && typeof item.name === "string" && typeof item.text === "string" && Number(item.rating) >= 1 && Number(item.rating) <= 5).slice(0, 50) : [];
      } catch { return []; }
    };
    let saved = readSaved();
    const list = feedback.querySelector("[data-feedback-list]");
    const pagination = feedback.querySelector("[data-feedback-pagination]");
    const formatReviewTime = (value) => {
      const date = new Date(value);
      if (Number.isNaN(date.getTime())) return "زمان ثبت نامشخص";
      return new Intl.DateTimeFormat("fa-IR", {
        year: "numeric",
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      }).format(date);
    };
    const renderFeedback = () => {
      const entries = [...saved, ...seed].sort(
        (first, second) =>
          new Date(second.createdAt || 0).getTime() -
          new Date(first.createdAt || 0).getTime(),
      );
      const pageCount = Math.max(1, Math.ceil(entries.length / pageSize));
      reviewPage = Math.min(reviewPage, pageCount);
      const visibleEntries = entries.slice(
        (reviewPage - 1) * pageSize,
        reviewPage * pageSize,
      );
      list.replaceChildren();
      visibleEntries.forEach((item) => {
        const card = document.createElement("article");
        card.className = "article-feedback-card";
        card.tabIndex = -1;
        const heading = document.createElement("div");
        const name = document.createElement("strong");
        name.textContent = item.name;
        const stars = document.createElement("span");
        stars.className = "article-feedback-stars";
        stars.setAttribute("aria-label", `امتیاز ${formatNumber(Number(item.rating))} از ۵`);
        stars.textContent = `★ ${formatNumber(Number(item.rating))} از ۵`;
        const meta = document.createElement("span");
        meta.className = "article-feedback-meta";
        const separator = document.createElement("span");
        separator.className = "article-feedback-separator";
        separator.setAttribute("aria-hidden", "true");
        separator.textContent = "|";
        const registered = document.createElement("time");
        registered.className = "article-feedback-time";
        if (item.createdAt) registered.dateTime = item.createdAt;
        const registeredIcon = document.createElement("i");
        registeredIcon.className = "bi bi-calendar3";
        registeredIcon.setAttribute("aria-hidden", "true");
        registered.append(registeredIcon, document.createTextNode(formatReviewTime(item.createdAt)));
        meta.append(stars, separator, registered);
        heading.append(name, meta);
        const copy = document.createElement("p");
        copy.textContent = item.text;
        card.append(heading, registered, copy);
        if (item.reply) {
          const reply = document.createElement("aside");
          reply.className = "article-feedback-admin-reply";
          const replyHeading = document.createElement("strong");
          replyHeading.className = "article-feedback-admin-badge";
          replyHeading.innerHTML = '<i class="bi bi-patch-check-fill" aria-hidden="true"></i> پاسخ مدیر مجله';
          const replyTime = document.createElement("time");
          replyTime.className = "article-feedback-admin-time";
          const replyTimestamp = item.replyAt || item.createdAt;
          if (replyTimestamp) replyTime.dateTime = replyTimestamp;
          const replyIcon = document.createElement("i");
          replyIcon.className = "bi bi-clock";
          replyIcon.setAttribute("aria-hidden", "true");
          replyTime.append(replyIcon, document.createTextNode(formatReviewTime(replyTimestamp)));
          const replyText = document.createElement("p");
          replyText.textContent = item.reply;
          reply.append(replyHeading, replyTime, replyText);
          card.append(reply);
        }
        list.append(card);
      });
      pagination.replaceChildren();
      pagination.hidden = pageCount <= 1;
      if (pageCount > 1) {
        const addPageButton = (label, target, disabled, current = false) => {
          const button = document.createElement("button");
          button.type = "button";
          button.textContent = label;
          button.disabled = disabled;
          button.dataset.reviewPage = String(target);
          button.setAttribute(
            "aria-label",
            current ? `صفحه ${formatNumber(target)}، صفحهٔ فعلی` : `رفتن به صفحه ${formatNumber(target)}`,
          );
          if (current) button.setAttribute("aria-current", "page");
          button.addEventListener("click", () => {
            reviewPage = target;
            renderFeedback();
            const firstReview = list.querySelector(".article-feedback-card");
            firstReview?.focus({ preventScroll: true });
            firstReview?.scrollIntoView({
              behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "auto" : "smooth",
              block: "start",
            });
          });
          pagination.append(button);
        };
        addPageButton("قبلی", reviewPage - 1, reviewPage === 1);
        for (let target = 1; target <= pageCount; target++) {
          addPageButton(formatNumber(target), target, false, target === reviewPage);
        }
        addPageButton("بعدی", reviewPage + 1, reviewPage === pageCount);
      }
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
    feedback.querySelector("[data-feedback-form]").addEventListener("submit", async (event) => {
      event.preventDefault();
      const form = event.currentTarget;
      if (form.dataset.confirming === "true") return;
      const data = new FormData(form);
      const nameField = form.elements.namedItem("name");
      const fullName = String(data.get("name") || "").trim().replace(/\s+/g, " ");
      if (fullName.split(" ").filter(Boolean).length < 2) {
        nameField.setCustomValidity("نام و نام خانوادگی را وارد کنید.");
        nameField.reportValidity();
        nameField.addEventListener("input", () => nameField.setCustomValidity(""), { once: true });
        return;
      }
      nameField.setCustomValidity("");
      if (!form.reportValidity()) return;
      form.dataset.confirming = "true";
      const submitButton = form.querySelector('button[type="submit"]');
      if (submitButton) submitButton.disabled = true;
      const confirmed = await window.confirmFormSubmission?.({
        title: "دیدگاه شما ثبت شود؟",
        message: "پس از تأیید، دیدگاه در همین مرورگر ذخیره و به فهرست اضافه می‌شود.",
        confirmText: "بله، ثبت دیدگاه",
      });
      form.dataset.confirming = "false";
      if (submitButton) submitButton.disabled = false;
      if (!confirmed) return;
      saved.push({ name: fullName.slice(0, 80), text: String(data.get("text")).trim().slice(0, 1000), rating: Math.max(1, Math.min(5, Number(data.get("rating")) || 5)), createdAt: new Date().toISOString() });
      saved = saved.slice(-50);
      const stored = window.HNUI.storage.set(key, JSON.stringify(saved));
      reviewPage = 1;
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
