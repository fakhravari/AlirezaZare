window.HNUI.mount("pages-brand-product-count", document, () => {
  const countElements = document.querySelectorAll("[data-brand-product-count]");
  if (!countElements.length) return;

  const products = Array.isArray(window.HNProducts) ? window.HNProducts : [];
  const normalizeBrand = (value) =>
    String(value || "")
      .toLocaleLowerCase()
      .replace(/[^a-z0-9]/g, "");

  countElements.forEach((element) => {
    const brandSlug = normalizeBrand(element.dataset.brandSlug);
    const count = products.filter(
      (product) => normalizeBrand(product.brand) === brandSlug,
    ).length;
    const localizedCount = new Intl.NumberFormat("fa-IR").format(count);

    element.textContent = `${localizedCount} محصول`;
    element.setAttribute("aria-label", `${localizedCount} محصول مرتبط`);
  });
});

window.HNUI.mount("pages-brand-readability", document.querySelector(".brand-detail-page"), (root) => {
  const paragraphs = Array.from(root.querySelectorAll(".brand-detail-content p"))
    .filter((element) => element.childNodes.length === 1 && element.firstChild.nodeType === Node.TEXT_NODE);
  const mobile = window.matchMedia("(max-width: 767px)");
  let lastLayout = "";
  let scheduled = false;

  const updateAlignment = () => {
    scheduled = false;
    const layout = `${root.clientWidth}:${mobile.matches}`;
    if (layout === lastLayout) return;
    lastLayout = layout;

    // Try full-width prose; short or narrow paragraphs keep their natural spacing.
    paragraphs.forEach((paragraph) => {
      const wordCount = paragraph.textContent.match(/\S+/g)?.length || 0;
      paragraph.dataset.brandTextAlign = mobile.matches && paragraph.clientWidth >= 220 && wordCount >= 8
        ? "justify" : "natural";
    });

    const stretched = paragraphs.filter((paragraph) => {
      if (paragraph.dataset.brandTextAlign !== "justify") return false;
      const maximumGap = Math.max(6, parseFloat(getComputedStyle(paragraph).fontSize) * 0.7);
      const range = document.createRange();
      let previous;
      for (const word of paragraph.textContent.matchAll(/\S+/g)) {
        range.setStart(paragraph.firstChild, word.index);
        range.setEnd(paragraph.firstChild, word.index + word[0].length);
        const rectangles = range.getClientRects();
        const rectangle = range.getBoundingClientRect();
        // Mixed Persian/Latin words can have several fragments on the same line.
        if (Array.from(rectangles).some((part) => Math.abs(part.top - rectangle.top) >= 2)) return true;
        if (previous && Math.abs(previous.top - rectangle.top) < 2) {
          const gap = Math.max(previous.left - rectangle.right, rectangle.left - previous.right, 0);
          if (gap > maximumGap) return true;
        }
        previous = rectangle;
      }
      return false;
    });
    stretched.forEach((paragraph) => { paragraph.dataset.brandTextAlign = "natural"; });
  };

  const scheduleAlignment = () => {
    if (scheduled) return;
    scheduled = true;
    requestAnimationFrame(updateAlignment);
  };
  window.addEventListener("resize", scheduleAlignment, { passive: true });
  scheduleAlignment();
  document.fonts.ready.then(() => { lastLayout = ""; scheduleAlignment(); });
});
