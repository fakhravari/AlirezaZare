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
const hasActiveFilters = () =>
  ["brand", "type", "movement", "strap"].some((key) => state[key].size) ||
  state.maxPrice < 60 ||
  Boolean(state.query);
// Stable URL identifiers are independent of the Persian display labels.
const filterOptions = {
  brand: {
    victorinox: "ویکتورینوکس",
    patek: "پتک فیلیپ",
    police: "پلیس",
    polo: "پولو",
    poloexchange: "پولو اکسچنج",
    beverlyhills: "بورلی هیلز",
  },
  type: { men: "مردانه", women: "زنانه", sport: "اسپرت", classic: "کلاسیک" },
  movement: { quartz: "کوارتز", automatic: "اتوماتیک", mechanical: "مکانیکی", solar: "سولار" },
  strap: { steel: "استیل", leather: "چرم طبیعی", silicone: "سیلیکونی", mesh: "بند مش" },
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
      if (state[key].has(label)) params.append(key, id);
    }),
  );
  if (state.maxPrice < 60) params.set("maxPrice", String(state.maxPrice));
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
    params.getAll(key).forEach((value) => {
      // Accept old shared links, then normalize them to English in syncUrl.
      const label = Object.hasOwn(filterOptions[key], value) ? filterOptions[key][value] : value;
      if (filterConfig[key].includes(label)) state[key].add(label);
    });
  });
  const price = Number(params.get("maxPrice"));
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
    $(`#${key}Filters`).innerHTML = values
      .map(
        (value) =>
          `<label class="filter-option"><input type="checkbox" value="${value}" data-filter="${key}"><span>${value}</span></label>`,
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
  return `<article class="search-product product-card"><div class="search-product-image product-image"><a href="Product?id=${p.id}"><img src="${p.image}" alt="${p.name}" loading="lazy" decoding="async"></a><button class="favorite" type="button" aria-label="افزودن به علاقه‌مندی‌ها" aria-pressed="false"><i class="bi bi-heart"></i></button>${p.badge ? `<span class="badge-best">${p.badge}</span>` : ""}</div><div class="search-product-body product-info"><div class="search-product-heading"><small>${p.brand} · HN-${String(p.id).padStart(3, "0")}</small><h3><a href="Product?id=${p.id}">${p.name}</a></h3></div><p class="search-product-price">${fa(p.price * 1000000)} <small>تومان</small></p></div><button class="search-add-cart add-cart" type="button" data-id="${p.id}" data-name="${p.name}" data-price="${p.price * 1000000}" data-image="${p.image}"><i class="bi bi-bag-plus"></i> افزودن به سبد خرید</button></article>`;
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
        `<button class="filter-chip" data-key="${key}" data-value="${value}">${value} ×</button>`,
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
  updatePrice();
  render(true, true);
}
function updatePrice() {
  state.maxPrice = Number($("#maxPrice").value);
  $("#maxPriceLabel").textContent = `${fa(state.maxPrice)} میلیون`;
}
buildFilters();
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
              `<article class="search-cart-item"><img src="${item.image || "ClockTemplate/images/products/watch-blue.webp"}" alt=""><span><strong>${item.name || "محصول"}</strong><small>${fa(item.price)} تومان</small></span><b>${fa(item.quantity)} عدد</b></article>`,
          )
          .join("")
      : '<div class="search-cart-empty"><i class="bi bi-bag"></i><strong>سبد خرید شما خالی است</strong></div>';
  if (footer) footer.style.display = cart.length ? "block" : "none";
}
let searchToastTimer;
document.addEventListener("click", (event) => {
  const favorite = event.target.closest(".favorite");
  if (favorite) {
    const active = favorite.classList.toggle("active");
    favorite.setAttribute("aria-pressed", String(active));
    favorite.setAttribute(
      "aria-label",
      active ? "حذف از علاقه‌مندی‌ها" : "افزودن به علاقه‌مندی‌ها",
    );
    favorite.querySelector("i").className = active
      ? "bi bi-heart-fill"
      : "bi bi-heart";
    const toast = $("#searchToast");
    toast.textContent = active
      ? "محصول به علاقه‌مندی‌ها اضافه شد."
      : "محصول از علاقه‌مندی‌ها حذف شد.";
    toast.classList.add("show");
    clearTimeout(searchToastTimer);
    searchToastTimer = setTimeout(() => toast.classList.remove("show"), 1800);
    return;
  }
});
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


