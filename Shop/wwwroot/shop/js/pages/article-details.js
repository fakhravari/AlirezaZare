window.HNUI.mount("pages-article-details", document, () => {
  const page = document.querySelector("[data-article-slug]");
  if (!page) return;
  const formatNumber = (value) => window.HNUI.formatNumber(value);
  const body = page.querySelector(".article-body");
  document
    .getElementById("articlePrint")
    .addEventListener("click", () => window.print());
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
