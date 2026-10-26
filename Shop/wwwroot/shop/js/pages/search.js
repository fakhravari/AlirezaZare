window.HNUI.ready(() => window.HNUI.mount("search", document.querySelector(".search-main"), () => {
  const products = window.HNProducts;
  const $ = (selector) => document.querySelector(selector);
  const $$ = (selector) => [...document.querySelectorAll(selector)];
  const state = {
    brand: new Set(),
    type: new Set(),
    movement: new Set(),
    strap: new Set(),
    color: new Set(),
    availability: "all",
    discountOnly: false,
    minPrice: 0,
    maxPrice: 60,
    query: "",
    sort: "recommended",
    visible: 9,
  };
  const attributeDefinitions = window.HNCatalog?.attributeDefinitions || {};
  const filterFields = {
    brand: { fa: "برند", en: "Brand", slug: "brand", attributeId: "brand" },
    type: { fa: "گروه محصول", en: "Product group", slug: "product-group", attributeId: "category" },
    movement: { fa: "منبع تغذیه", en: "Power source", slug: "power-source", attributeId: "powerSource" },
    strap: {
      fa: "مورد استفاده",
      en: "Application environment",
      slug: "application-environment",
      attributeId: "useCase",
    },
    color: { fa: "رنگ", en: "Color", slug: "color", attributeId: "color" },
    price: { fa: "بازه قیمت", en: "Price range", slug: "price-range", attributeId: "price" },
  };
  const isFilterable = (key) => attributeDefinitions[filterFields[key]?.attributeId]?.is_filterable === true;
  const hasActiveFilters = () =>
    ["brand", "type", "movement", "strap", "color"].some((key) => state[key].size) ||
    state.availability !== "all" ||
    state.discountOnly ||
    state.minPrice > 0 ||
    state.maxPrice < 60 ||
    Boolean(state.query);
  const brandPersianLabels = {
    Arva: "آروا",
    Benning: "بننینگ",
    Bosch: "بوش",
    DeWalt: "دیوالت",
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
  const brandSlug = (brand) =>
    brand
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, "-")
      .replace(/^-|-$/g, "");
  const brandDisplayName = (brand) => brandPersianLabels[brand] || brand;
  const filterValueMetadata = {
    type: {
      "ابزار دستی برق‌کاری": {
        en: "Electrician hand tools",
        slug: "electrician-hand-tools",
      },
      "ایستگاه هویه": { en: "Soldering station", slug: "soldering-station" },
      "پروژکتور کارگاهی": {
        en: "Workshop floodlight",
        slug: "workshop-floodlight",
      },
      "تستر مدار": { en: "Circuit tester", slug: "circuit-tester" },
      "تستر و فازمتر": { en: "Electrical tester", slug: "electrical-tester" },
      "تستر ولتاژ": { en: "Voltage tester", slug: "voltage-tester" },
      "چراغ بازرسی": { en: "Inspection light", slug: "inspection-light" },
      "چراغ کارگاهی": { en: "Workshop light", slug: "workshop-light" },
      "دریل چکشی": { en: "Hammer drill", slug: "hammer-drill" },
      "دریل و پیچ‌گوشتی": { en: "Drill driver", slug: "drill-driver" },
      "کلمپ‌متر": { en: "Clamp meter", slug: "clamp-meter" },
      فازمتر: { en: "Voltage detector", slug: "voltage-detector" },
      "مجموعه ابزار برق‌کاری": {
        en: "Electrician tool kit",
        slug: "electrician-tool-kit",
      },
      "مولتی‌متر دیجیتال": {
        en: "Digital multimeter",
        slug: "digital-multimeter",
      },
      "هویه قلمی": { en: "Soldering iron", slug: "soldering-iron" },
      "هیتر و هویه": {
        en: "Hot air rework station",
        slug: "hot-air-rework-station",
      },
    },
    movement: {
      "باتری شارژی": {
        en: "Rechargeable battery",
        slug: "rechargeable-battery",
      },
      باتری‌خور: { en: "Battery powered", slug: "battery-powered" },
      "برق شهری": { en: "Mains powered", slug: "mains-powered" },
      دستی: { en: "Manual", slug: "manual" },
    },
    strap: {
      "ایمنی و بازرسی": {
        en: "Safety and inspection",
        slug: "safety-inspection",
      },
      "بازرسی و تعمیر": {
        en: "Inspection and repair",
        slug: "inspection-repair",
      },
      "برق صنعتی": {
        en: "Industrial electrical",
        slug: "industrial-electrical",
      },
      "تأسیسات برقی": {
        en: "Electrical installation",
        slug: "electrical-installation",
      },
      "تست اولیه برق": {
        en: "Initial electrical testing",
        slug: "initial-electrical-testing",
      },
      "تست و سرویس": { en: "Testing and service", slug: "testing-service" },
      "تست و عیب‌یابی": {
        en: "Testing and troubleshooting",
        slug: "testing-troubleshooting",
      },
      "تعمیر برد": { en: "Circuit board repair", slug: "circuit-board-repair" },
      "تعمیر خودرو": { en: "Automotive repair", slug: "automotive-repair" },
      "تعمیر و نگهداری": {
        en: "Maintenance and repair",
        slug: "maintenance-repair",
      },
      "تعمیرات الکترونیک": {
        en: "Electronics repair",
        slug: "electronics-repair",
      },
      "تعمیرگاه و کارگاه": {
        en: "Repair shop and workshop",
        slug: "repair-shop-workshop",
      },
      "سیم‌کشی و نصب": {
        en: "Wiring and installation",
        slug: "wiring-installation",
      },
      "عیب‌یابی برق": {
        en: "Electrical troubleshooting",
        slug: "electrical-troubleshooting",
      },
      "کارگاه ساختمانی": {
        en: "Construction workshop",
        slug: "construction-workshop",
      },
      "کارگاه و سرویس": { en: "Workshop service", slug: "workshop-service" },
      "کارگاه و نصب": {
        en: "Workshop installation",
        slug: "workshop-installation",
      },
      "محیط کارگاهی": {
        en: "Workshop environment",
        slug: "workshop-environment",
      },
      "مونتاژ الکترونیک": {
        en: "Electronics assembly",
        slug: "electronics-assembly",
      },
      "مونتاژ و تعمیر": { en: "Assembly and repair", slug: "assembly-repair" },
      "نصب و برق‌کاری": {
        en: "Electrical installation work",
        slug: "electrical-installation-work",
      },
      "نصب و تعمیر": {
        en: "Installation and repair",
        slug: "installation-repair",
      },
      "نصب و کارگاه": {
        en: "Installation workshop",
        slug: "installation-workshop",
      },
      "نصب و مونتاژ": {
        en: "Installation and assembly",
        slug: "installation-assembly",
      },
    },
  };
  const filterValueMeta = (key, value) => {
    if (key === "brand") return {fa: brandDisplayName(value), en: value, slug: brandSlug(value)};
    if (key === "color") return window.HNProductColors?.[value] || {fa: value, en: value, slug: value, hex: "#64748b"};
    if (key === "type") {
      const category = window.HNCatalog?.categoryForSlug?.(value) || window.HNCatalog?.categoryForLabel?.(value);
      if (category) return {fa: category.fa, en: category.en, slug: category.slug};
    }
    return {fa: value, ...(filterValueMetadata[key]?.[value] || {en: value, slug: `option-${value}`})};
  };
  const createFilterOptions = (key) =>
    Object.fromEntries(
      [...new Set(products.map((product) => product[key]))].map((label) => [
        filterValueMeta(key, label).slug,
        label,
      ]),
    );
  const createCategoryFilterOptions = () => Object.fromEntries(
    Object.entries(window.HNCatalog?.categories || {})
      .filter(([id, category]) => products.some((product) =>
        window.HNCatalog?.categoryContains?.(category.slug || id, product.categoryId)))
      .map(([id, category]) => [category.slug || id, category.slug || id]),
  );
  const createLegacyFilterOptions = (key) =>
    Object.fromEntries(
      [...new Set(products.map((product) => product[key]))].map(
        (label, index) => [`option${index + 1}`, label],
      ),
    );
  const filterOptions = Object.fromEntries(Object.entries({
    brand: createFilterOptions("brand"),
    type: createCategoryFilterOptions(),
    movement: createFilterOptions("movement"),
    strap: createFilterOptions("strap"),
    color: Object.fromEntries(
      [
        ...new Set(
          products.flatMap((product) =>
            (product.colors || []).map((color) => color.slug),
          ),
        ),
      ].map((slug) => [slug, slug]),
    ),
  }).filter(([key]) => isFilterable(key)));
  const filterConfig = Object.fromEntries(
    Object.entries(filterOptions).map(([key, options]) => [
      key,
      Object.values(options),
    ]),
  );
  let restoringUrl = false;
  function syncUrl() {
    if (restoringUrl) return;
    const params = new URLSearchParams();
    Object.entries(filterOptions).forEach(([key, options]) => {
      if (key === "type") return;
      Object.entries(options).forEach(([id, label]) => {
        if (state[key].has(label)) params.append(filterFields[key].slug, id);
      });
    });
    state.type.forEach((value) => {
      const category = window.HNCatalog?.categoryForSlug?.(value) || window.HNCatalog?.categoryForLabel?.(value);
      params.append(filterFields.type.slug, category?.slug || filterValueMeta("type", value).slug);
    });
    if (state.minPrice > 0) params.set("price-min", String(state.minPrice));
    if (state.minPrice > 0 || state.maxPrice < 60)
      params.set(filterFields.price.slug, String(state.maxPrice));
    if (state.availability === "in-stock") params.set("available", "1");
    if (state.availability === "out-of-stock") params.set("available", "0");
    if (state.discountOnly) params.set("discounted", "1");
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
    state.type.clear();
    const params = new URLSearchParams(location.search);
    Object.keys(filterConfig).forEach((key) => {
      if (key !== "type") state[key].clear();
      const values = [
        ...new Set([
          ...params.getAll(filterFields[key].slug),
          ...(filterFields[key].slug === key ? [] : params.getAll(key)),
        ]),
      ];
      values.forEach((value) => {
        // Accept category slugs and legacy IDs, plus filter slugs, old options, and Persian labels.
        if (key === "type") {
          const legacyOption = /^option\d+$/.test(value)
            ? createLegacyFilterOptions(key)[value]
            : Object.entries(filterValueMetadata.type).find(([, metadata]) => metadata.slug === value)?.[0] || value;
          const productCategory = products.find((product) => product.type === legacyOption);
          const catalogCategory = window.HNCatalog?.categoryForSlug?.(value) ||
            window.HNCatalog?.categories?.[value] ||
            window.HNCatalog?.categoryForLabel?.(legacyOption) ||
            (productCategory && window.HNCatalog?.categoryForSlug?.(productCategory.categorySlug));
          if (catalogCategory) state.type.add(catalogCategory.slug || catalogCategory.id);
          return;
        }
        const legacyOption = /^option\d+$/.test(value)
          ? createLegacyFilterOptions(key)[value]
          : undefined;
        const slugValue = Object.hasOwn(filterOptions[key], value)
          ? filterOptions[key][value]
          : legacyOption || value;
        const label =
          key === "brand"
            ? brandPersianLabels[slugValue]
              ? slugValue
              : Object.keys(brandPersianLabels).find(
                  (brand) => brandPersianLabels[brand] === value,
                ) || slugValue
            : slugValue;
        if (filterConfig[key].includes(label)) state[key].add(label);
      });
    });
    const price = Number(
      params.get(filterFields.price.slug) ?? params.get("maxPrice"),
    );
    state.maxPrice =
      Number.isFinite(price) && price >= 1 && price <= 60 ? price : 60;
    const minPrice = Number(params.get("price-min") ?? params.get("minPrice"));
    state.minPrice = Number.isFinite(minPrice) && minPrice >= 0 && minPrice < state.maxPrice
      ? minPrice
      : 0;
    state.query = params.get("q")?.trim() || "";
    const availability = (
      params.get("available") ?? params.get("onlyAvailable") ?? ""
    ).toLowerCase();
    state.availability = ["0", "false", "out-of-stock", "unavailable"].includes(
      availability,
    )
      ? "out-of-stock"
      : ["1", "true", "in-stock", "available"].includes(availability)
        ? "in-stock"
        : "all";
    state.discountOnly = ["1", "true", "yes"].includes(
      (params.get("discounted") ?? params.get("discount") ?? "").toLowerCase(),
    );
    state.sort = ["recommended", "newest", "low", "high"].includes(
      params.get("sort"),
    )
      ? params.get("sort")
      : "recommended";
    $$(".filter-option input[data-filter]").forEach((input) => {
      input.checked = state[input.dataset.filter].has(input.value);
    });
    $("#availabilityFilter").value = state.availability;
    $("#discountFilter").checked = state.discountOnly;
    $("#minPrice").value = state.minPrice;
    $("#maxPrice").value = state.maxPrice;
    $("#productSearch").value = state.query;
    $("#sortProducts").value = state.sort;
    restoringUrl = false;
  }
  const fa = (n) => window.HNUI.formatNumber(n);
  const isProductAvailable = (product) => {
    if (typeof product.inStock === "boolean") return product.inStock;
    const inventory =
      product.stockQuantity ??
      product.inventoryCount ??
      product.quantityInStock ??
      product.availableQuantity ??
      product.stock;
    const quantity = Number(inventory);
    return Number.isFinite(quantity) && quantity >= 0 ? quantity > 0 : true;
  };
  const isProductDiscounted = (product) => {
    const discount = Number(product.discountPercent) || 0;
    return discount > 0 && discount < 100;
  };
  const normalizeFilterSearch = (value) =>
    String(value ?? "")
      .normalize("NFKC")
      .toLocaleLowerCase("fa")
      .replace(/[يى]/g, "ی")
      .replace(/ك/g, "ک")
      .replace(/[\u200c\u200f\u202a-\u202e]/g, "")
      .replace(/\s+/g, " ")
      .trim();
  function buildFilters() {
    Object.entries(filterConfig).forEach(([key, values]) => {
      const filterName = key === "brand" ? "برند" : "گروه محصول";
      const searchBox = ["brand", "type"].includes(key)
        ? `<label class="filter-search"><i class="bi bi-search" aria-hidden="true"></i><input type="search" data-filter-search="${key}" placeholder="جستجوی فارسی یا انگلیسی ${filterName}" aria-label="جستجوی فارسی یا انگلیسی در ${key === "brand" ? "برندها" : "گروه‌های محصول"}"></label>`
        : "";
      $(`#${key}Filters`).innerHTML =
        searchBox +
        values
          .map((value) =>
            (() => {
              const metadata = filterValueMeta(key, value);
              const colorOption =
                key === "color"
                  ? `<i class="filter-color-swatch" style="--filter-color: ${metadata.hex}" aria-hidden="true"></i>`
                  : "";
              return `<label class="filter-option${key === "color" ? " filter-color-option" : ""}"><input type="checkbox" value="${value}" data-filter="${key}" data-value-fa="${metadata.fa}" data-value-en="${metadata.en}" data-value-slug="${metadata.slug}"><span class="filter-option-label">${colorOption}${key === "color" ? `<span class="filter-color-name" title="${metadata.fa} ${metadata.hex}">${metadata.fa}</span>` : metadata.fa}</span></label>`;
            })(),
          )
          .join("");
    });
  }
  function syncCategoryFilters() {
    const facets = window.HNCatalog.categoryFacets(state.type);
    for (const key of ['brand', 'type', 'movement', 'strap', 'color']) {
      const container = $(`#${key}Filters`);
      const allowed = new Set(facets[key]);
      if (key !== 'type') {
        for (const value of state[key]) if (!allowed.has(value)) state[key].delete(value);
      }
      const enabled = isFilterable(key);
      container.closest('.filter-group').hidden = !enabled || !allowed.size;
      container.closest('.filter-group').style.display = enabled && allowed.size ? '' : 'none';
      container.querySelectorAll('input[data-filter]').forEach(input => {
        const visible = allowed.has(input.value);
        input.closest('.filter-option').hidden = !visible;
        input.closest('.filter-option').style.display = visible ? '' : 'none';
        input.disabled = !visible;
        input.checked = state[key].has(input.value);
      });
    }
  }
  function getFiltered() {
    const selectedTypes = [...state.type];
    let list = products.filter(
      (p) =>
        (!state.brand.size || state.brand.has(p.brand)) &&
        (!selectedTypes.length || selectedTypes.some((type) =>
          window.HNCatalog?.categoryContains?.(type, p.categoryId))) &&
        (!state.movement.size || state.movement.has(p.powerSource || p.movement)) &&
        (!state.strap.size || state.strap.has(p.useCase || p.strap)) &&
        (!state.color.size ||
          (p.colors || []).some((color) => state.color.has(color.slug))) &&
        (state.availability === "all" ||
          (state.availability === "in-stock"
            ? isProductAvailable(p)
            : !isProductAvailable(p))) &&
        (!state.discountOnly || isProductDiscounted(p)) &&
        p.price >= state.minPrice &&
        p.price <= state.maxPrice &&
        (!state.query ||
          `${p.name} ${p.brand} ${p.type} ${p.powerSource || p.movement}`.includes(state.query)),
    );
    if (state.sort === "low") list.sort((a, b) => a.price - b.price);
    if (state.sort === "high") list.sort((a, b) => b.price - a.price);
    if (state.sort === "newest") list.sort((a, b) => a.newest - b.newest);
    return list;
  }
  const card = (product) => window.HNProductCard.render(product, {variant: "search"});
  function scrollToResultsStart() {
    const results = document.querySelector(".results-area");
    if (!results) return;
    const headerOffset = document.querySelector("#mainNav")?.offsetHeight || 0;
    const targetTop =
      window.scrollY + results.getBoundingClientRect().top - headerOffset - 16;
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
    syncCategoryFilters();
    const list = getFiltered();
    const shown = list.slice(0, state.visible);
    const finished = list.length > 0 && state.visible >= list.length;
    $("#productResults").innerHTML = shown.map(card).join("");
    const comparedProducts = new Set(
      window.HNUI?.storage.json("hn-compare", []).map(String) || [],
    );
    $("#productResults")
      .querySelectorAll(".compare-trigger[data-product-id]")
      .forEach((button) => {
        const active = comparedProducts.has(String(button.dataset.productId));
        button.classList.toggle("active", active);
        button.setAttribute("aria-pressed", String(active));
        button.setAttribute(
          "aria-label",
          active ? "حذف از مقایسه" : "افزودن به مقایسه",
        );
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
    const breadcrumb = $("#searchBreadcrumb ol");
    if (breadcrumb) {
      const selectedTypes = [...state.type];
      const selectedPaths = selectedTypes.map((type) => {
        const category = window.HNCatalog?.categoryForSlug?.(type);
        return category?.path || products.find((product) => product.categorySlug === type)?.categoryPath || ["ابزار و تجهیزات", type];
      });
      const sharedPathLength = selectedPaths.length
        ? selectedPaths[0].findIndex((part, index) => selectedPaths.some((path) => path[index] !== part))
        : 1;
      const categoryPath = selectedTypes.length === 1
        ? selectedPaths[0]
        : selectedTypes.length > 1
          ? [...selectedPaths[0].slice(0, sharedPathLength < 0 ? selectedPaths[0].length : sharedPathLength), "گروه‌های منتخب"]
          : ["ابزار و تجهیزات"];
      breadcrumb.replaceChildren();
      const appendItem = (className) => {
        const item = document.createElement("li");
        if (className) item.className = className;
        breadcrumb.append(item);
        return item;
      };
      const home = appendItem();
      const homeLink = document.createElement("a");
      homeLink.href = "/";
      homeLink.innerHTML = '<i class="bi bi-house-door" aria-hidden="true"></i><span>خانه</span>';
      home.append(homeLink);
      categoryPath.forEach((part, index) => {
        const item = appendItem();
        const separator = document.createElement("i");
        separator.className = "bi bi-chevron-left";
        separator.setAttribute("aria-hidden", "true");
        item.append(separator);
        if (part === "گروه‌های منتخب") {
          item.append(document.createTextNode(part));
          return;
        }
        const link = document.createElement("a");
        link.href = window.HNCatalog?.categoryUrl(categoryPath.slice(0, index + 1)) || `/Search?q=${encodeURIComponent(part)}`;
        link.textContent = part;
        item.append(link);
      });
      const current = appendItem("cart-breadcrumb-current");
      const currentSeparator = document.createElement("i");
      currentSeparator.className = "bi bi-chevron-left";
      currentSeparator.setAttribute("aria-hidden", "true");
      const currentLabel = document.createElement("span");
      currentLabel.setAttribute("aria-current", "page");
      currentLabel.textContent = selectedTypes.length ? "نتایج جستجو" : "جستجو و انتخاب ابزار";
      current.append(currentSeparator, currentLabel);
    }
    syncUrl();
    if (returnToStart) requestAnimationFrame(scrollToResultsStart);
  }
  function renderChips() {
    const chips = [];
    ["brand", "type", "movement", "strap", "color"].forEach((key) =>
      state[key].forEach((value) =>
        chips.push(
          (() => {
            const metadata = filterValueMeta(key, value);
            const swatch =
              key === "color"
                ? `<i class="filter-color-swatch" style="--filter-color: ${metadata.hex}" aria-hidden="true"></i>`
                : "";
            const label =
              key === "brand" ? brandDisplayName(value) : ["color", "type"].includes(key) ? metadata.fa : value;
            return `<button class="filter-chip${key === "color" ? " filter-chip-color" : ""}" data-key="${key}" data-value="${value}">${swatch}${label} ×</button>`;
          })(),
        ),
      ),
    );
    if (state.minPrice > 0 || state.maxPrice < 60)
      chips.push(
        `<button class="filter-chip" data-key="price">${fa(state.minPrice)} تا ${fa(state.maxPrice)} میلیون تومان ×</button>`,
      );
    if (state.availability !== "all")
      chips.push(
        `<button class="filter-chip" data-key="available">${state.availability === "in-stock" ? "کالاهای موجود" : "کالاهای ناموجود"} ×</button>`,
      );
    if (state.discountOnly)
      chips.push(
        '<button class="filter-chip" data-key="discounted">کالاهای تخفیف‌دار ×</button>',
      );
    $("#activeChips").innerHTML = chips.join("");
    const count = chips.length;
    $("#activeFilterCount").textContent = fa(count);
    $$(".filter-chip").forEach(
      (chip) =>
        (chip.onclick = () => {
          if (chip.dataset.key === "price") {
            state.minPrice = 0;
            state.maxPrice = 60;
            $("#minPrice").value = 0;
            $("#maxPrice").value = 60;
          } else if (chip.dataset.key === "available") {
            state.availability = "all";
            $("#availabilityFilter").value = "all";
          } else if (chip.dataset.key === "discounted") {
            state.discountOnly = false;
            $("#discountFilter").checked = false;
          } else {
            state[chip.dataset.key].delete(chip.dataset.value);
            if (chip.dataset.key === "type") {
              $$("input[data-filter=\"type\"]").forEach((input) => {
                if (input.value === chip.dataset.value) input.checked = false;
              });
            } else {
              const box = document.querySelector(
                `input[data-filter="${chip.dataset.key}"][value="${chip.dataset.value}"]`,
              );
              if (box) box.checked = false;
            }
          }
          updatePrice();
          render(true, true);
        }),
    );
  }
  function clearAll() {
    ["brand", "type", "movement", "strap", "color"].forEach((key) => state[key].clear());
    state.availability = "all";
    state.discountOnly = false;
    state.minPrice = 0;
    state.maxPrice = 60;
    state.query = "";
    $$(".filter-option input").forEach((input) => (input.checked = false));
    $("#availabilityFilter").value = "all";
    $("#minPrice").value = 0;
    $("#maxPrice").value = 60;
    $("#productSearch").value = "";
    $$("[data-filter-search]").forEach((input) => {
      input.value = "";
      input
        .closest(".filter-options")
        .querySelectorAll(".filter-option")
        .forEach((option) => option.classList.remove("filter-option-hidden"));
    });
    updatePrice();
    render(true, true);
  }
  function updatePrice() {
    state.minPrice = Number($("#minPrice").value);
    state.maxPrice = Number($("#maxPrice").value);
    const range = $(".price-range");
    range.style.setProperty("--range-start", `${(state.minPrice / 60) * 100}%`);
    range.style.setProperty("--range-end", `${(state.maxPrice / 60) * 100}%`);
    $("#minPriceLabel").textContent = fa(state.minPrice);
    $("#maxPriceLabel").textContent = fa(state.maxPrice);
  }
  buildFilters();
  $$("[data-filter-search]").forEach((input) => {
    input.addEventListener("input", () => {
      const query = normalizeFilterSearch(input.value);
      input
        .closest(".filter-options")
        .querySelectorAll(".filter-option")
        .forEach((option) => {
          const optionInput = option.querySelector("input[data-filter]");
          const searchableText = normalizeFilterSearch(
            [
              option.textContent,
              optionInput?.dataset.valueFa,
              optionInput?.dataset.valueEn,
              optionInput?.dataset.valueSlug,
            ].join(" "),
          );
          option.classList.toggle(
            "filter-option-hidden",
            !searchableText.includes(query),
          );
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
  $$(".filter-option input[data-filter]").forEach(
    (input) =>
      (input.onchange = () => {
        const set = state[input.dataset.filter];
        const value = input.value;
        input.checked ? set.add(value) : set.delete(value);
        render(true, true);
      }),
  );
  $("#availabilityFilter").onchange = (event) => {
    state.availability = event.target.value;
    render(true, true);
  };
  $("#discountFilter").onchange = (event) => {
    state.discountOnly = event.target.checked;
    render(true, true);
  };
  $("#minPrice").oninput = () => {
    if (Number($("#minPrice").value) >= state.maxPrice)
      $("#minPrice").value = Math.max(0, state.maxPrice - 1);
    updatePrice();
    render(true);
  };
  $("#maxPrice").oninput = () => {
    if (Number($("#maxPrice").value) <= state.minPrice)
      $("#maxPrice").value = Math.min(60, state.minPrice + 1);
    updatePrice();
    render(true);
  };
  $("#minPrice").onchange = () => scrollToResultsStart();
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
  function cartOptionsMarkup(item) {
    const escape = window.HNUI?.escapeHtml || ((value) => String(value || ""));
    const variants = Array.isArray(item.options?.variants)
      ? item.options.variants
      : [];
    const accessories = Array.isArray(item.options?.accessories)
      ? item.options.accessories
      : [];
    if (!variants.length && !accessories.length) return "";
    const variantMarkup = variants.length
      ? `<div class="cart-item-option-group"><small class="cart-item-option-label">ویژگی‌ها</small><div class="cart-item-option-list cart-item-option-list--variants">${variants
          .map(
            (option) =>
              `<span class="cart-item-option-chip"><small>${escape(option.label)}</small><strong>${escape(option.value)}</strong>${Number(option.priceDelta) ? `<small class="cart-item-option-price">${Number(option.priceDelta) > 0 ? "+" : "−"} ${fa(Math.abs(Number(option.priceDelta)) * 1000000)} تومان</small>` : ""}</span>`,
          )
          .join("")}</div></div>`
      : "";
    const accessoryMarkup = accessories.length
      ? `<div class="cart-item-option-group"><small class="cart-item-option-label">متعلقات و خدمات</small><div class="cart-item-option-list cart-item-option-list--accessories">${accessories
          .map(
            (accessory) =>
              `<span class="cart-item-option-chip cart-item-option-chip--accessory"><i class="bi bi-plus-circle" aria-hidden="true"></i><strong>${escape(accessory.name)}</strong><small class="cart-item-option-status ${accessory.required ? "is-required" : "is-optional"}">${accessory.required ? "الزامی" : "اختیاری"}</small>${Number(accessory.price) > 0 ? `<small>+ ${fa(Number(accessory.price) * 1000000)} تومان</small>` : "<small>رایگان</small>"}</span>`,
          )
          .join("")}</div></div>`
      : "";
    return `<div class="cart-item-options" aria-label="ویژگی‌ها و متعلقات انتخاب‌شده">${variantMarkup}${accessoryMarkup}</div>`;
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
                `<article class="search-cart-item"><img src="${window.HNUI.escapeHtml(item.image || "content/products/images/card/cordless-hammer-drill-hn-001-row-001-card-800.jpg")}" alt=""><span><strong>${window.HNUI.escapeHtml(item.name || "محصول")}</strong>${cartOptionsMarkup(item)}<small>${fa(item.price)} تومان</small></span><b>${fa(item.quantity)} عدد</b></article>`,
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
  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") closeFilters();
    HNUI.dialog.trap(event, panel.classList.contains("open") ? panel : null);
  });
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
}));
