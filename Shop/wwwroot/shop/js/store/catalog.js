(function (window, document) {
  "use strict";
  if (window.HNCatalog) return;
  const element = document.querySelector("#storeCatalog");
  if (!element) throw new Error("The shared #storeCatalog data block is missing.");
  const data = JSON.parse(element.textContent);
  if (data.schemaVersion !== 1) throw new Error("Unsupported catalog schemaVersion.");
  const relation = (collection, id) => {
    if (!Object.prototype.hasOwnProperty.call(collection, id))
      throw new Error("Unknown catalog relation: " + id);
    return collection[id];
  };
  const freeze = (value) => {
    if (value && typeof value === "object" && !Object.isFrozen(value)) {
      Object.values(value).forEach(freeze);
      Object.freeze(value);
    }
    return value;
  };
  const mergeSpecs = (shared = [], specific = []) => {
    const merged = new Map();
    for (const pair of [...shared, ...specific]) {
      if (!Array.isArray(pair) || pair.length < 2) continue;
      const label = String(pair[0] ?? "").trim();
      const value = String(pair[1] ?? "").trim();
      if (label && value) merged.set(label.toLocaleLowerCase("fa"), [label, value]);
    }
    return [...merged.values()];
  };
  const normalizeSpecGroups = (groups = []) => groups
    .filter((group) => group && typeof group.title === "string" && Array.isArray(group.specs))
    .map((group) => ({title: group.title.trim(), specs: group.specs.filter((pair) =>
      Array.isArray(pair) && String(pair[0] ?? "").trim() && String(pair[1] ?? "").trim())
      .map(([label, value]) => [String(label).trim(), String(value).trim()])}))
    .filter((group) => group.title && group.specs.length);
  const attributeDefinitions = data.attributeDefinitions || {};
  const normalizeLabel = (value) => String(value ?? "").trim().toLocaleLowerCase("fa").replace(/[يى]/g, "ی").replace(/ك/g, "ک").replace(/[\u200c\u200f\s]+/g, " ");
  const unusableAttributeValue = (value) => /وابسته به|بر اساس مدل|بررسی .* پیش از|اقلام.*متفاوت/i.test(value);
  const ids = new Set();
  const products = data.products.map((product) => {
    if (!Number.isSafeInteger(product.id) || product.id < 1 || ids.has(product.id))
      throw new Error("Invalid/duplicate product id: " + product.id);
    ids.add(product.id);
    const brand = relation(data.brands, product.brandId);
    const category = relation(data.categories, product.categoryId);
    const media = relation(data.media, product.mediaId);
    if (brand.countryCode) relation(data.countries, brand.countryCode);
    const colors = product.colorIds.map((id) => relation(data.colors, id));
    const detail = (() => {
      const sharedDetail = relation(data.details, product.detailId);
      const sharedSpecGroups = [
        ...(sharedDetail.specs?.length ? [{title: "ویژگی‌های عمومی", specs: sharedDetail.specs}] : []),
        ...normalizeSpecGroups(sharedDetail.specGroups),
      ];
      const productSpecGroups = normalizeSpecGroups(product.specGroups);
      return {...sharedDetail, specs: mergeSpecs(sharedDetail.specs), sharedSpecGroups, productSpecGroups};
    })();
    const resolvedSpecs = mergeSpecs(
      detail.specs,
      [...(product.specs || []), ...detail.sharedSpecGroups.flatMap((group) => group.specs),
        ...detail.productSpecGroups.flatMap((group) => group.specs)],
    );
    const specValueByLabel = new Map(resolvedSpecs.map(([label, value]) => [normalizeLabel(label), value]));
    const attributeValues = Object.entries(attributeDefinitions).flatMap(([key, definition]) => {
      let value = "";
      if (definition.source === "spec") value = "";
      else if (definition.source?.startsWith("spec:")) value = specValueByLabel.get(normalizeLabel(definition.source.slice(5))) || "";
      else if (definition.source === "country") value = relation(data.countries, brand.countryCode).fa;
      else if (definition.source === "color") value = colors.map((color) => color.fa).filter(Boolean).join("، ");
      else value = String(({brand: brand.fa, type: category.fa})[definition.source] ?? product[definition.source] ?? "").trim();
      if (!value || unusableAttributeValue(value)) return [];
      return [{key, label: definition.label, icon: definition.icon, value,
        filterKey: definition.filterKey || "", is_filterable: definition.is_filterable === true,
        is_highlight: definition.is_highlight === true}];
    });
    return freeze({
      ...product, brand: brand.name, brandFa: brand.fa, brandLogo: brand.logo,
      brandCountryCode: brand.countryCode, type: category.fa, categoryPath: [...category.path],
      colors, attributeValues, detail, image: media.card,
      mainImage: media.main, thumbnailImage: media.thumbnail, images: media.images, videos: media.videos,
      // Compatibility aliases for persisted filters; new code uses explicit names.
      movement: product.powerSource, strap: product.useCase,
    });
  });
  const byId = new Map(products.map((product) => [String(product.id), product]));
  const productUrl = (productOrId) => {
    const product = byId.get(String(productOrId?.id ?? productOrId));
    return product ? "/products/" + product.slug + "-" + product.id : "/search";
  };
  window.HNCountries = freeze(data.countries);
  window.HNProductColors = freeze(data.colors);
  window.HNProducts = freeze(products);
  window.HNProductUrl = productUrl;
  window.HNCatalog = Object.freeze({schemaVersion: data.schemaVersion, products: window.HNProducts,
    brands: freeze(data.brands), categories: freeze(data.categories), attributeDefinitions: freeze(attributeDefinitions), collections: freeze(data.collections),
    find: (id) => byId.get(String(id)) || null, url: productUrl});
})(window, document);
