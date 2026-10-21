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
  const ids = new Set();
  const products = data.products.map((product) => {
    if (!Number.isSafeInteger(product.id) || product.id < 1 || ids.has(product.id))
      throw new Error("Invalid/duplicate product id: " + product.id);
    ids.add(product.id);
    const brand = relation(data.brands, product.brandId);
    const category = relation(data.categories, product.categoryId);
    const media = relation(data.media, product.mediaId);
    if (brand.countryCode) relation(data.countries, brand.countryCode);
    return freeze({
      ...product, brand: brand.name, brandFa: brand.fa, brandLogo: brand.logo,
      brandCountryCode: brand.countryCode, type: category.fa, categoryPath: [...category.path],
      colors: product.colorIds.map((id) => relation(data.colors, id)),
      detail: relation(data.details, product.detailId), image: media.card,
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
    brands: freeze(data.brands), categories: freeze(data.categories), collections: freeze(data.collections),
    find: (id) => byId.get(String(id)) || null, url: productUrl});
})(window, document);
