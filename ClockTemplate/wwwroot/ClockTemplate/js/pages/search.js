(() => {
const products = window.HNProducts;
const $ = (selector) => document.querySelector(selector);
const $$ = (selector) => [...document.querySelectorAll(selector)];
const state = {
  brand: new Set(),
  type: new Set(),
  movement: new Set(),
  strap: new Set(),
  maxPrice: 60,
  query: "",
  sort: "recommended",
  visible: 9,
};
const filterFields = {
  brand: { fa: "برند", en: "Brand", slug: "brand" },
  type: { fa: "گروه محصول", en: "Product group", slug: "product-group" },
  movement: { fa: "نوع تغذیه", en: "Power type", slug: "power-type" },
  strap: { fa: "محیط کاربرد", en: "Application environment", slug: "application-environment" },
  price: { fa: "بازه قیمت", en: "Price range", slug: "price-range" },
};
const hasActiveFilters = () =>
  ["brand", "type", "movement", "strap"].some((key) => state[key].size) ||
  state.maxPrice < 60 ||
  Boolean(state.query);
const brandPersianLabels = {
  Arva: "آروا",
  Benning: "بننینگ",
  Bosch: "بوش",
  "DeWalt": "دیوالت",
  Fluke: "فلوک",
  GOOT: "گوت",
  Hakko: "هاکو",
  Knipex: "کنیپکس",
  Kyoritsu: "کیوریتسو",
  Makita: "ماکیتا",
  Mastech: "مستک",
  Philips: "فیلیپس",
  Quick: "کوئیک",
  Ronix: "رونیکس",
  Stanley: "استنلی",
  Tosan: "توسن",
  "UNI-T": "یونی‌تی",
  Victor: "ویکتور",
  Weller: "ولر",
  Wiha: "ویها",
  YIHUA: "ییهوا",
};
const brandSlug = (brand) => brand.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "");
const brandFilterOptions = Object.fromEntries(
  [...new Set(products.map((product) => product.brand))].map((brand) => [brandSlug(brand), brand]),
);
const brandDisplayName = (brand) => brandPersianLabels[brand] || brand;
const createFilterOptions = (key) =>
  Object.fromEntries(
    [...new Set(products.map((product) => product[key]))].map((label, index) => [
      `option${index + 1}`,
      label,
    ]),
  );
const filterOptions = {
  brand: brandFilterOptions,
  type: createFilterOptions("type"),
  movement: createFilterOptions("movement"),
  strap: createFilterOptions("strap"),
};
const filterConfig = Object.fromEntries(
  Object.entries(filterOptions).map(([key, options]) => [key, Object.values(options)]),
);
let restoringUrl = false;
function syncUrl() {
  if (restoringUrl) return;
  const params = new URLSearchParams();
  Object.entries(filterOptions).forEach(([key, options]) =>
    Object.entries(options).forEach(([id, label]) => {
      if (state[key].has(label)) params.append(filterFields[key].slug, id);
    }),
  );
  if (state.maxPrice < 60) params.set(filterFields.price.slug, String(state.maxPrice));
  if (state.query) params.set("q", state.query);
  if (state.sort !== "recommended") params.set("sort", state.sort);
  const query = params.toString();
  history.replaceState(
    null,
    "",
    `${location.pathname}${query ? `?${query}` : ""}${location.hash}`,
  );
}
function applyUrlState() {
  restoringUrl = true;
  const params = new URLSearchParams(location.search);
  Object.keys(filterConfig).forEach((key) => {
    state[key].clear();
    const values = [...new Set([
      ...params.getAll(filterFields[key].slug),
      ...(filterFields[key].slug === key ? [] : params.getAll(key)),
    ])];
    values.forEach((value) => {
      // Accept slug, legacy English brand values, and Persian labels in shared links.
      const legacyOption = /^option\d+$/.test(value) ? createFilterOptions(key)[value] : undefined;
      const slugValue = Object.hasOwn(filterOptions[key], value)
        ? filterOptions[key][value]
        : legacyOption || value;
      const label = key === "brand"
        ? (brandPersianLabels[slugValue] ? slugValue : Object.keys(brandPersianLabels).find((brand) => brandPersianLabels[brand] === value) || slugValue)
        : slugValue;
      if (filterConfig[key].includes(label)) state[key].add(label);
    });
  });
  const price = Number(params.get(filterFields.price.slug) ?? params.get("maxPrice"));
  state.maxPrice =
    Number.isFinite(price) && price >= 10 && price <= 60 ? price : 60;
  state.query = params.get("q")?.trim() || "";
  state.sort = ["recommended", "newest", "low", "high"].includes(
    params.get("sort"),
  )
    ? params.get("sort")
    : "recommended";
  $$(".filter-option input").forEach(
    (input) => (input.checked = state[input.dataset.filter].has(input.value)),
  );
  $("#maxPrice").value = state.maxPrice;
  $("#productSearch").value = state.query;
  $("#sortProducts").value = state.sort;
  restoringUrl = false;
}
const fa = (n) => window.HNUI.formatNumber(n);
function buildFilters() {
  Object.entries(filterConfig).forEach(([key, values]) => {
    const searchBox = ["brand", "type"].includes(key)
      ? `<label class="filter-search"><i class="bi bi-search" aria-hidden="true"></i><input type="search" data-filter-search="${key}" placeholder="جستجوی ${key === "brand" ? "برند" : "گروه محصول"}" aria-label="جستجو در ${key === "brand" ? "برندها" : "گروه‌های محصول"}"></label>`
      : "";
    $(`#${key}Filters`).innerHTML = searchBox + values
      .map(
        (value) =>
          `<label class="filter-option"><input type="checkbox" value="${value}" data-filter="${key}"><span>${key === "brand" ? brandDisplayName(value) : value}</span></label>`,
      )
      .join("");
  });
}
function getFiltered() {
  let list = products.filter(
    (p) =>
      (!state.brand.size || state.brand.has(p.brand)) &&
      (!state.type.size || state.type.has(p.type)) &&
      (!state.movement.size || state.movement.has(p.movement)) &&
      (!state.strap.size || state.strap.has(p.strap)) &&
      p.price <= state.maxPrice &&
      (!state.query ||
        `${p.name} ${p.brand} ${p.type} ${p.movement}`.includes(state.query)),
  );
  if (state.sort === "low") list.sort((a, b) => a.price - b.price);
  if (state.sort === "high") list.sort((a, b) => b.price - a.price);
  if (state.sort === "newest") list.sort((a, b) => a.newest - b.newest);
  return list;
}
function card(p) {
  return `<article class="search-product product-card"><div class="search-product-image product-image"><a href="Product?id=${p.id}"><img src="${p.image}" alt="${p.name}" loading="lazy" decoding="async"></a><button class="favorite" type="button" data-product-id="${p.id}" aria-label="افزودن به علاقه‌مندی‌ها" aria-pressed="false"><i class="bi bi-heart"></i></button><button class="compare-trigger" type="button" data-product-id="${p.id}" aria-label="افزودن به مقایسه" aria-pressed="false" title="افزودن به مقایسه"><i class="bi bi-arrow-left-right" aria-hidden="true"></i></button>${p.badge ? `<span class="badge-best">${p.badge}</span>` : ""}</div><div class="search-product-body product-info"><div class="search-product-heading"><small>${p.brand} · HN-${String(p.id).padStart(3, "0")}</small><h3><a href="Product?id=${p.id}">${p.name}</a></h3></div><p class="search-product-price">${fa(p.price * 1000000)} <small>تومان</small></p></div><button class="search-add-cart add-cart" type="button" data-id="${p.id}" data-name="${p.name}" data-price="${p.price * 1000000}" data-image="${p.image}"><i class="bi bi-bag-plus"></i> افزودن به سبد خرید</button></article>`;
}
function applyDiscountDisplay(root) {
  root.querySelectorAll(".search-product").forEach((productCard) => {
    const productId = Number(productCard.querySelector(".search-add-cart")?.dataset.id);
    const product = products.find((item) => item.id === productId);
    const discount = Number(product?.discountPercent) || 0;
    if (!product || discount <= 0 || discount >= 100) return;

    const currentPrice = Math.round(product.price * 1000000);
    const previousPrice = Math.round(currentPrice / (1 - discount / 100));
    const price = productCard.querySelector(".search-product-price");
    if (price) {
      price.classList.add("has-discount");
      price.innerHTML = '<span class="product-price-deal"><del class="price-old">' + fa(previousPrice) + '</del><b class="product-price-discount">٪' + fa(discount) + '</b></span><strong class="product-price-current">' + fa(currentPrice) + ' <small>تومان</small></strong>';
    }
    const image = productCard.querySelector(".search-product-image");
    if (image && !image.querySelector(".product-discount-badge")) {
      image.insertAdjacentHTML("beforeend", '<span class="product-discount-badge">٪' + fa(discount) + ' تخفیف</span>');
    }
  });
}
function scrollToResultsStart() {
  const results = document.querySelector(".results-area");
  if (!results) return;
  const headerOffset = document.querySelector("#mainNav")?.offsetHeight || 0;
  const targetTop = window.scrollY + results.getBoundingClientRect().top - headerOffset - 16;
  if (window.scrollY <= targetTop + 24) return;
  window.scrollTo({
    top: Math.max(0, targetTop),
    behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches
      ? "auto"
      : "smooth",
  });
}
function render(reset = false, returnToStart = false) {
  if (reset) state.visible = 9;
  const list = getFiltered();
  const shown = list.slice(0, state.visible);
  const finished = list.length > 0 && state.visible >= list.length;
  $("#productResults").innerHTML = shown.map(card).join("");
  applyDiscountDisplay($("#productResults"));
  const comparedProducts = new Set(
    window.HNUI?.storage.json("hn-compare", []).map(String) || [],
  );
  $("#productResults").querySelectorAll(".compare-trigger[data-product-id]").forEach((button) => {
    const active = comparedProducts.has(String(button.dataset.productId));
    button.classList.toggle("active", active);
    button.setAttribute("aria-pressed", String(active));
    button.setAttribute("aria-label", active ? "حذف از مقایسه" : "افزودن به مقایسه");
    button.title = active ? "حذف از مقایسه" : "افزودن به مقایسه";
  });
  window.HNUI?.enhance($("#productResults"));
  $("#resultCount").textContent = fa(list.length);
  $("#filterResultCount").textContent = fa(list.length);
  const heroResultCount = $("#heroResultCount");
  if (heroResultCount) heroResultCount.textContent = fa(list.length);
  $("#endResultCount").textContent = fa(list.length);
  $("#emptyResults").hidden = list.length > 0;
  $("#productResults").hidden = list.length === 0;
  $("#loadMoreState").classList.toggle("loading", false);
  $("#loadMoreSentinel").hidden = finished || list.length === 0;
  $("#resultsEnd").hidden = !finished;
  $("#clearFilters").hidden = !hasActiveFilters();
  renderChips();
  syncUrl();
  if (returnToStart) requestAnimationFrame(scrollToResultsStart);
}
function renderChips() {
  const chips = [];
  ["brand", "type", "movement", "strap"].forEach((key) =>
    state[key].forEach((value) =>
      chips.push(
        `<button class="filter-chip" data-key="${key}" data-value="${value}">${key === "brand" ? brandDisplayName(value) : value} ×</button>`,
      ),
    ),
  );
  if (state.maxPrice < 60)
    chips.push(
      `<button class="filter-chip" data-key="price">تا ${fa(state.maxPrice)} میلیون ×</button>`,
    );
  $("#activeChips").innerHTML = chips.join("");
  const count = chips.length;
  $("#activeFilterCount").textContent = fa(count);
  $$(".filter-chip").forEach(
    (chip) =>
      (chip.onclick = () => {
        if (chip.dataset.key === "price") {
          state.maxPrice = 60;
          $("#maxPrice").value = 60;
        } else {
          state[chip.dataset.key].delete(chip.dataset.value);
          const box = document.querySelector(
            `input[data-filter="${chip.dataset.key}"][value="${chip.dataset.value}"]`,
          );
          if (box) box.checked = false;
        }
        updatePrice();
        render(true, true);
      }),
  );
}
function clearAll() {
  ["brand", "type", "movement", "strap"].forEach((key) => state[key].clear());
  state.maxPrice = 60;
  state.query = "";
  $$(".filter-option input").forEach((input) => (input.checked = false));
  $("#maxPrice").value = 60;
  $("#productSearch").value = "";
  $$('[data-filter-search]').forEach((input) => {
    input.value = "";
    input.closest(".filter-options").querySelectorAll(".filter-option").forEach((option) => option.classList.remove("filter-option-hidden"));
  });
  updatePrice();
  render(true, true);
}
function updatePrice() {
  state.maxPrice = Number($("#maxPrice").value);
  $("#maxPriceLabel").textContent = `${fa(state.maxPrice)} میلیون`;
}
buildFilters();
$$("[data-filter-search]").forEach((input) => {
  input.addEventListener("input", () => {
    const query = input.value.trim().toLocaleLowerCase();
    input.closest(".filter-options").querySelectorAll(".filter-option").forEach((option) => {
      option.classList.toggle("filter-option-hidden", !option.textContent.toLocaleLowerCase().includes(query));
    });
  });
});
$$(".filter-group>button").forEach(
  (button) =>
    (button.onclick = () => {
      const open = button.parentElement.classList.toggle("open");
      button.setAttribute("aria-expanded", String(open));
    }),
);
$$(".filter-option input").forEach(
  (input) =>
    (input.onchange = () => {
      const set = state[input.dataset.filter];
      input.checked ? set.add(input.value) : set.delete(input.value);
      render(true, true);
    }),
);
$("#maxPrice").oninput = () => {
  updatePrice();
  render(true);
};
$("#maxPrice").onchange = () => scrollToResultsStart();
$("#productSearch").oninput = (e) => {
  state.query = e.target.value.trim();
  render(true);
};
$("#sortProducts").onchange = (e) => {
  state.sort = e.target.value;
  render(true, true);
};
$("#clearFilters").onclick = clearAll;
$("#emptyResults button").onclick = clearAll;
function readCart() {
  return window.HNUI?.cart.read() || [];
}
function updateCartCount() {
  const cart = readCart(),
    count = cart.reduce((sum, item) => sum + (Number(item.quantity) || 0), 0),
    total = cart.reduce(
      (sum, item) =>
        sum + (Number(item.price) || 0) * (Number(item.quantity) || 0),
      0,
    );
  const badge = $("#searchCartCount");
  if (badge) badge.textContent = fa(count);
  const summary = $("#searchCartSummary"),
    totalBox = $("#searchCartTotal"),
    items = $("#searchCartItems"),
    footer = $("#searchCartFooter");
  if (summary) summary.textContent = `${fa(count)} محصول`;
  if (totalBox) totalBox.textContent = `${fa(total)} تومان`;
  if (items)
    items.innerHTML = cart.length
      ? cart
          .map(
            (item) =>
              `<article class="search-cart-item"><img src="${item.image || "ClockTemplate/images/products/tool-hammer-drill.png"}" alt=""><span><strong>${item.name || "محصول"}</strong><small>${fa(item.price)} تومان</small></span><b>${fa(item.quantity)} عدد</b></article>`,
          )
          .join("")
      : '<div class="search-cart-empty"><i class="bi bi-bag"></i><strong>سبد خرید شما خالی است</strong></div>';
  if (footer) footer.style.display = cart.length ? "block" : "none";
}
const panel = $("#filterPanel"),
  backdrop = $("#filterBackdrop"),
  filterToggle = $("#mobileFilterToggle");
const closeFilters = () => {
  const wasOpen = panel.classList.contains("open");
  panel.classList.remove("open");
  backdrop.classList.remove("open");
  filterToggle.setAttribute("aria-expanded", "false");
  if (wasOpen) {
    HNUI.scrollLock.unlock(true);
    filterToggle.focus();
  }
};
filterToggle.onclick = () => {
  panel.classList.add("open");
  backdrop.classList.add("open");
  filterToggle.setAttribute("aria-expanded", "true");
  HNUI.scrollLock.lock();
  setTimeout(() => $("#closeFilters").focus(), 40);
};
$("#closeFilters").onclick = closeFilters;
backdrop.onclick = closeFilters;
document.addEventListener('keydown', (event) => { if (event.key === 'Escape') closeFilters(); HNUI.dialog.trap(event, panel.classList.contains('open') ? panel : null); });
applyUrlState();
updatePrice();
render();
updateCartCount();
document.addEventListener("hn:cart-change", updateCartCount);
window.addEventListener("popstate", () => {
  applyUrlState();
  updatePrice();
  render(true);
});
let loading = false;
const observer = new IntersectionObserver(
  (entries) => {
    if (!entries[0].isIntersecting || loading) return;
    const list = getFiltered();
    if (state.visible >= list.length) return;
    loading = true;
    $("#loadMoreState").classList.add("loading");
    setTimeout(() => {
      state.visible += 6;
      loading = false;
      render();
    }, 450);
  },
  { rootMargin: "300px" },
);
observer.observe($("#loadMoreSentinel"));

})();


