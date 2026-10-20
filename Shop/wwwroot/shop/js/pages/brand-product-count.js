(() => {
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
})();
