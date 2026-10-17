(() => {
  const products = window.HNProducts;
  const $ = (selector) => document.querySelector(selector);
  const $$ = (selector) => [...document.querySelectorAll(selector)];
  const state = {
    brand: new Set(),
    type: new Set(),
    movement: new Set(),
    strap: new Set(),
    availability: "all",
    maxPrice: 60,
    query: "",
    sort: "recommended",
    visible: 9,
  };
  const filterFields = {
    brand: { fa: "برند", en: "Brand", slug: "brand" },
    type: { fa: "گروه محصول", en: "Product group", slug: "product-group" },
    movement: { fa: "منبع تغذیه", en: "Power source", slug: "power-source" },
    strap: {
      fa: "مورد استفاده",
      en: "Application environment",
      slug: "application-environment",
    },
    price: { fa: "بازه قیمت", en: "Price range", slug: "price-range" },
  };
  const hasActiveFilters = () =>
    ["brand", "type", "movement", "strap"].some((key) => state[key].size) ||
    state.availability !== "all" ||
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
  const filterValueMeta = (key, value) =>
    key === "brand"
      ? { fa: brandDisplayName(value), en: value, slug: brandSlug(value) }
      : {
          fa: value,
          ...(filterValueMetadata[key]?.[value] || {
            en: value,
            slug: `option-${value}`,
          }),
        };
  const createFilterOptions = (key) =>
    Object.fromEntries(
      [...new Set(products.map((product) => product[key]))].map((label) => [
        filterValueMeta(key, label).slug,
        label,
      ]),
    );
  const createLegacyFilterOptions = (key) =>
    Object.fromEntries(
      [...new Set(products.map((product) => product[key]))].map(
        (label, index) => [`option${index + 1}`, label],
      ),
    );
  const filterOptions = {
    brand: createFilterOptions("brand"),
    type: createFilterOptions("type"),
    movement: createFilterOptions("movement"),
    strap: createFilterOptions("strap"),
  };
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
    Object.entries(filterOptions).forEach(([key, options]) =>
      Object.entries(options).forEach(([id, label]) => {
        if (state[key].has(label)) params.append(filterFields[key].slug, id);
      }),
    );
    if (state.maxPrice < 60)
      params.set(filterFields.price.slug, String(state.maxPrice));
    if (state.availability === "in-stock") params.set("available", "1");
    if (state.availability === "out-of-stock") params.set("available", "0");
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
      const values = [
        ...new Set([
          ...params.getAll(filterFields[key].slug),
          ...(filterFields[key].slug === key ? [] : params.getAll(key)),
        ]),
      ];
      values.forEach((value) => {
        // Accept slug, legacy English brand values, and Persian labels in shared links.
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
      Number.isFinite(price) && price >= 10 && price <= 60 ? price : 60;
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
    state.sort = ["recommended", "newest", "low", "high"].includes(
      params.get("sort"),
    )
      ? params.get("sort")
      : "recommended";
    $$(".filter-option input[data-filter]").forEach(
      (input) => (input.checked = state[input.dataset.filter].has(input.value)),
    );
    $("#availabilityFilter").value = state.availability;
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
  function buildFilters() {
    Object.entries(filterConfig).forEach(([key, values]) => {
      const searchBox = ["brand", "type"].includes(key)
        ? `<label class="filter-search"><i class="bi bi-search" aria-hidden="true"></i><input type="search" data-filter-search="${key}" placeholder="جستجوی ${key === "brand" ? "برند" : "گروه محصول"}" aria-label="جستجو در ${key === "brand" ? "برندها" : "گروه‌های محصول"}"></label>`
        : "";
      $(`#${key}Filters`).innerHTML =
        searchBox +
        values
          .map((value) =>
            (() => {
              const metadata = filterValueMeta(key, value);
              return `<label class="filter-option"><input type="checkbox" value="${value}" data-filter="${key}" data-value-fa="${metadata.fa}" data-value-en="${metadata.en}" data-value-slug="${metadata.slug}"><span>${metadata.fa}</span></label>`;
            })(),
          )
          .join("");
    });
  }
  function getFiltered() {
    let list = products.filter(
      (p) =>
        (!state.brand.size || state.brand.has(p.brand)) &&
        (!state.type.size || state.type.has(p.type)) &&
        (!state.movement.size || state.movement.has(p.powerSource || p.movement)) &&
        (!state.strap.size || state.strap.has(p.useCase || p.strap)) &&
        (state.availability === "all" ||
          (state.availability === "in-stock"
            ? isProductAvailable(p)
            : !isProductAvailable(p))) &&
        p.price <= state.maxPrice &&
        (!state.query ||
          `${p.name} ${p.brand} ${p.type} ${p.powerSource || p.movement}`.includes(state.query)),
    );
    if (state.sort === "low") list.sort((a, b) => a.price - b.price);
    if (state.sort === "high") list.sort((a, b) => b.price - a.price);
    if (state.sort === "newest") list.sort((a, b) => a.newest - b.newest);
    return list;
  }
  function card(p) {
    return `<article class="search-product product-card search-page-card"><div class="product-image search-page-card-media"><a href="${window.HNProductUrl(p)}"><img src="${p.image}" alt="${p.name}" loading="lazy" decoding="async"></a><button class="favorite" type="button" data-product-id="${p.id}" aria-label="افزودن به علاقه‌مندی‌ها" aria-pressed="false"><i class="bi bi-heart"></i></button><button class="compare-trigger" type="button" data-product-id="${p.id}" aria-label="افزودن به مقایسه" aria-pressed="false" title="افزودن به مقایسه"><i class="bi bi-arrow-left-right" aria-hidden="true"></i></button>${p.badge ? `<span class="badge-best">${p.badge}</span>` : ""}</div><div class="product-info search-page-card-content"><div class="search-page-card-heading"><h3 class="search-page-card-title"><a href="${window.HNProductUrl(p)}">${p.name}</a></h3></div><p class="search-product-price search-page-card-price">${fa(p.price * 1000000)} <small>تومان</small></p></div><button class="search-add-cart add-cart search-page-card-action" type="button" data-id="${p.id}" data-name="${p.name}" data-price="${p.price * 1000000}" data-image="${p.image}"><i class="bi bi-bag-plus"></i><span class="search-page-card-action-label">افزودن</span><span class="search-page-card-action-compact">افزودن</span></button></article>`;
  }
  function applyDiscountDisplay(root) {
    root.querySelectorAll(".search-page-card").forEach((productCard) => {
      const productId = Number(
        productCard.querySelector(".search-page-card-action")?.dataset.id,
      );
      const product = products.find((item) => item.id === productId);
      const discount = Number(product?.discountPercent) || 0;
      if (!product || discount <= 0 || discount >= 100) return;

      const currentPrice = Math.round(product.price * 1000000);
      const previousPrice = Math.round(currentPrice / (1 - discount / 100));
      const price = productCard.querySelector(".search-page-card-price");
      if (price) {
        price.classList.add("has-discount");
        price.innerHTML =
          '<strong class="product-price-current">' +
          fa(currentPrice) +
          " <small>تومان</small></strong><span class=\"product-price-deal\"><del class=\"price-old\">" +
          fa(previousPrice) +
          '</del><b class="product-price-discount">٪' +
          fa(discount) +
          "</b></span>";
      }
    });
  }
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
    const list = getFiltered();
    const shown = list.slice(0, state.visible);
    const finished = list.length > 0 && state.visible >= list.length;
    $("#productResults").innerHTML = shown.map(card).join("");
    applyDiscountDisplay($("#productResults"));
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
      const categoryPath =
        selectedTypes.length === 1
          ? products.find((product) => product.type === selectedTypes[0])
              ?.categoryPath || ["ابزار و تجهیزات", selectedTypes[0]]
          : [
              "ابزار و تجهیزات",
              ...(selectedTypes.length ? ["گروه‌های منتخب"] : []),
            ];
      breadcrumb.innerHTML = `<li><a href="/"><i class="bi bi-house-door" aria-hidden="true"></i><span>خانه</span></a></li>${categoryPath.map((part) => `<li><i class="bi bi-chevron-left" aria-hidden="true"></i><span>${part}</span></li>`).join("")}<li class="cart-breadcrumb-current"><i class="bi bi-chevron-left" aria-hidden="true"></i><span aria-current="page">${selectedTypes.length ? "نتایج جستجو" : "جستجو و انتخاب ابزار"}</span></li>`;
    }
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
    if (state.availability !== "all")
      chips.push(
        `<button class="filter-chip" data-key="available">${state.availability === "in-stock" ? "کالاهای موجود" : "کالاهای ناموجود"} ×</button>`,
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
          } else if (chip.dataset.key === "available") {
            state.availability = "all";
            $("#availabilityFilter").value = "all";
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
    state.availability = "all";
    state.maxPrice = 60;
    state.query = "";
    $$(".filter-option input").forEach((input) => (input.checked = false));
    $("#availabilityFilter").value = "all";
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
    state.maxPrice = Number($("#maxPrice").value);
    $("#maxPriceLabel").textContent = `${fa(state.maxPrice)} میلیون`;
  }
  buildFilters();
  $$("[data-filter-search]").forEach((input) => {
    input.addEventListener("input", () => {
      const query = input.value.trim().toLocaleLowerCase();
      input
        .closest(".filter-options")
        .querySelectorAll(".filter-option")
        .forEach((option) => {
          option.classList.toggle(
            "filter-option-hidden",
            !option.textContent.toLocaleLowerCase().includes(query),
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
        input.checked ? set.add(input.value) : set.delete(input.value);
        render(true, true);
      }),
  );
  $("#availabilityFilter").onchange = (event) => {
    state.availability = event.target.value;
    render(true, true);
  };
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
      ? `<div class="cart-item-option-group"><small class="cart-item-option-label">ویژگی‌ها</small><div class="cart-item-option-list">${variants
          .map(
            (option) =>
              `<span class="cart-item-option-chip"><small>${escape(option.label)}</small><strong>${escape(option.value)}</strong></span>`,
          )
          .join("")}</div></div>`
      : "";
    const accessoryMarkup = accessories.length
      ? `<div class="cart-item-option-group"><small class="cart-item-option-label">متعلقات و خدمات</small><div class="cart-item-option-list">${accessories
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
                `<article class="search-cart-item"><img src="${window.HNUI.escapeHtml(item.image || "Shop/images/products/tool-hammer-drill.png")}" alt=""><span><strong>${window.HNUI.escapeHtml(item.name || "محصول")}</strong>${cartOptionsMarkup(item)}<small>${fa(item.price)} تومان</small></span><b>${fa(item.quantity)} عدد</b></article>`,
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
})();
