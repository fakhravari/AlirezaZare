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
  const categoryEntries = Object.entries(data.categories).map(([id, category]) => ({
    id, label: String(category.fa || "").trim(), path: [...(category.path || [])],
  }));
  const normalizeCategoryLabel = (value) => String(value ?? "").trim()
    .toLocaleLowerCase("fa").replace(/[يى]/g, "ی").replace(/ك/g, "ک")
    .replace(/[\u200c\u200f\s]+/g, " ");
  const categoryEntriesForPath = (path) => {
    const segments = (Array.isArray(path) ? path : [path]).map(normalizeCategoryLabel).filter(Boolean);
    if (!segments.length) return [];
    return categoryEntries.filter((category) => {
      const categoryPath = category.path.map(normalizeCategoryLabel);
      if (segments.length === 1) return categoryPath.includes(segments[0]);
      return segments.every((segment, index) => categoryPath[index] === segment);
    });
  };
  const categorySearchUrl = (entries, fallback) => {
    const types = [...new Set(entries.map((category) => category.label).filter(Boolean))];
    const params = new URLSearchParams();
    if (types.length) types.forEach((type) => params.append("product-group", type));
    else params.set("q", String(fallback || "ابزار و تجهیزات").trim());
    return "/Search?" + params.toString();
  };
  const categoryUrl = (path) => {
    const segments = Array.isArray(path) ? path : [path];
    const entries = categoryEntriesForPath(segments);
    return categorySearchUrl(entries, segments.at(-1));
  };
  const categoryUrlForLabels = (labels) => {
    const wanted = new Set((Array.isArray(labels) ? labels : [labels]).map(normalizeCategoryLabel));
    const entries = categoryEntries.filter((category) =>
      category.path.some((segment) => wanted.has(normalizeCategoryLabel(segment))),
    );
    return categorySearchUrl(entries, [...wanted].at(-1));
  };
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
      if (definition.source === "specs" || definition.source === "variants") return [];
      else if (definition.source?.startsWith("spec:")) value = specValueByLabel.get(normalizeLabel(definition.source.slice(5))) || "";
      else if (definition.source === "country") value = relation(data.countries, brand.countryCode).fa;
      else if (definition.source === "color") value = colors.map((color) => color.fa).filter(Boolean).join("، ");
      else value = String(({brand: brand.fa, type: category.fa})[definition.source] ?? product[definition.source] ?? "").trim();
      if (!value || unusableAttributeValue(value)) return [];
      return [{key, label: definition.label, icon: definition.icon, value,
        filterKey: definition.filterKey || "", is_filterable: definition.is_filterable === true,
        is_highlight: definition.is_highlight === true}];
    });
    const technicalDefinition = Object.values(attributeDefinitions).find((definition) => definition.source === "specs");
    const variantDefinition = Object.values(attributeDefinitions).find((definition) => definition.source === "variants");
    const seenLabels = new Set(attributeValues.map((attribute) => normalizeLabel(attribute.label)));
    const seenValues = new Set(attributeValues.map((attribute) => normalizeLabel(attribute.value)));
    const addDynamicAttribute = (key, label, value, definition) => {
      const normalizedLabel = normalizeLabel(label);
      const normalizedValue = normalizeLabel(value);
      if (!definition?.is_highlight || !normalizedLabel || !normalizedValue ||
          unusableAttributeValue(value) || seenLabels.has(normalizedLabel) || seenValues.has(normalizedValue)) return;
      seenLabels.add(normalizedLabel);
      seenValues.add(normalizedValue);
      attributeValues.push({key, label, icon: definition.icon, value, filterKey: "",
        is_filterable: definition.is_filterable === true, is_highlight: true});
    };
    if (variantDefinition?.is_highlight) {
      for (const [variantKey, variant] of Object.entries(product.variants || {})) {
        const options = (variant.options || []).map((option) => String(option.label || "").trim()).filter(Boolean);
        addDynamicAttribute(`variant-${variantKey}`, String(variant.label || "").trim(), options.join(" / "), variantDefinition);
      }
    }
    if (technicalDefinition?.is_highlight) {
      for (const [label, value] of resolvedSpecs)
        addDynamicAttribute(`spec-${normalizeLabel(label).replace(/[^\p{L}\p{N}]+/gu, "-")}`, label, value, technicalDefinition);
    }
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
    find: (id) => byId.get(String(id)) || null, url: productUrl,
    categoryUrl, categoryUrlForLabels});
})(window, document);
