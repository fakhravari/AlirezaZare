$(function () {
  const $window = $(window);
  const $document = $(document);
  const $html = $("html");
  const $body = $("body");
  const $nav = $("#mainNav");
  const mobileBreakpoint = 991;
  const prefersReducedMotion = window.matchMedia(
    "(prefers-reduced-motion: reduce)",
  ).matches;
  const faNumber = (number) => window.HNUI.formatNumber(number);
  const escapeHtml = (value) => window.HNUI.escapeHtml(value);
  document
    .querySelectorAll(".home-header-search, .mobile-menu-search")
    .forEach((headerSearchForm) => {
      const searchInput = headerSearchForm.querySelector(
        ".header-search-input",
      );
      const suggestions = headerSearchForm.querySelector(
        ".header-search-suggestions, .mobile-menu-search-results",
      );
      if (!searchInput || !suggestions) return;
      const isDrawerSearch = headerSearchForm.classList.contains(
        "mobile-menu-search",
      );
      const isolateDrawerSearchMarkup = (markup) => {
        if (!isDrawerSearch) return markup;
        return [
          ["header-search-quick-section", "mobile-menu-search-quick-section"],
          ["header-search-quick-heading", "mobile-menu-search-quick-heading"],
          ["header-search-quick-chips", "mobile-menu-search-quick-chips"],
          ["header-search-quick-chip", "mobile-menu-search-quick-chip"],
          ["header-search-quick-empty", "mobile-menu-search-quick-empty"],
          ["header-search-suggestions-head", "mobile-menu-search-results-head"],
          ["header-search-suggestion-image", "mobile-menu-search-suggestion-image"],
          ["header-search-suggestion-copy", "mobile-menu-search-suggestion-copy"],
          ["header-search-suggestion", "mobile-menu-search-suggestion"],
          ["header-search-all", "mobile-menu-search-all"],
          ["header-search-empty", "mobile-menu-search-empty"],
        ]
          .sort((a, b) => b[0].length - a[0].length)
          .reduce(
            (html, [source, target]) => html.replaceAll(source, target),
            markup
              .replaceAll("recentSearchesTitle", "mobileMenuRecentSearchesTitle")
              .replaceAll("popularSearchesTitle", "mobileMenuPopularSearchesTitle"),
          );
      };
    const normalizeSearchText = (value) =>
      String(value || "")
        .toLocaleLowerCase("fa-IR")
        .replace(/[يى]/g, "ی")
        .replace(/ك/g, "ک")
        .replace(/\s+/g, " ")
        .trim();
    const recentSearchKey = "hn-header-recent-searches";
    const popularSearches = [
      "دریل شارژی",
      "مولتی‌متر دیجیتال",
      "ست مته و سری",
      "ایستگاه هویه",
      "چراغ کارگاهی",
    ];
    const readRecentSearches = () => {
      const searches = window.HNUI.storage.json(recentSearchKey, []);
      return Array.isArray(searches)
        ? searches.filter((item) => typeof item === "string").slice(0, 6)
        : [];
    };
    const rememberSearch = (value) => {
      const query = String(value || "")
        .trim()
        .slice(0, 80);
      if (Array.from(query).length < 2) return;
      const recent = readRecentSearches().filter(
        (item) => normalizeSearchText(item) !== normalizeSearchText(query),
      );
      window.HNUI.storage.set(
        recentSearchKey,
        JSON.stringify([query, ...recent].slice(0, 6)),
      );
    };
    const quickSearchLink = (query, icon) =>
      `<a class="header-search-quick-chip" data-search-query="${escapeHtml(query)}" href="${searchUrl(query)}"><i class="bi ${icon}" aria-hidden="true"></i><span>${escapeHtml(query)}</span></a>`;
    const renderQuickSearchPanel = () => {
      const recent = readRecentSearches();
      suggestions.innerHTML = `<section class="header-search-quick-section" aria-labelledby="recentSearchesTitle"><div class="header-search-quick-heading"><strong id="recentSearchesTitle"><i class="bi bi-clock-history" aria-hidden="true"></i> جستجوهای اخیر</strong>${recent.length ? '<button type="button" data-clear-search-history>پاک‌کردن</button>' : ""}</div>${recent.length ? `<div class="header-search-quick-chips">${recent.map((query) => quickSearchLink(query, "bi-clock-history")).join("")}</div>` : '<p class="header-search-quick-empty">جستجوهای اخیر اینجا نمایش داده می‌شوند.</p>'}</section><section class="header-search-quick-section" aria-labelledby="popularSearchesTitle"><div class="header-search-quick-heading"><strong id="popularSearchesTitle"><i class="bi bi-graph-up-arrow" aria-hidden="true"></i> جستجوهای پرتکرار</strong></div><div class="header-search-quick-chips">${popularSearches.map((query) => quickSearchLink(query, "bi-arrow-up-right")).join("")}</div></section>`;
      suggestions.innerHTML = isolateDrawerSearchMarkup(
        suggestions.innerHTML,
      );
      openSuggestions();
    };
    const closeSuggestions = () => {
      suggestions.hidden = true;
      searchInput.setAttribute("aria-expanded", "false");
    };
    const openSuggestions = () => {
      suggestions.hidden = false;
      searchInput.setAttribute("aria-expanded", "true");
    };
    const searchUrl = (query) => `Search?q=${encodeURIComponent(query)}`;
    const renderSuggestions = () => {
      const query = normalizeSearchText(searchInput.value);
      if (Array.from(query).length < 3) {
        renderQuickSearchPanel();
        return;
      }

      const products = Array.isArray(window.HNProducts)
        ? window.HNProducts
        : [];
      const matches = products
        .map((product) => {
          const productName = String(product.name || "").replace(/\s+\d+$/, "");
          const fields = [
            productName,
            product.brand,
            product.type,
            String(product.englishName || "").replace(/\s+\d+$/, ""),
          ];
          const normalizedFields = fields.map(normalizeSearchText);
          const matchIndex = normalizedFields.findIndex((field) =>
            field.includes(query),
          );
          return { product, matchIndex };
        })
        .filter(({ matchIndex }) => matchIndex >= 0)
        .sort(
          (a, b) =>
            a.matchIndex - b.matchIndex ||
            (b.product.newest || 0) - (a.product.newest || 0),
        )
        .filter(
          (() => {
            const seenNames = new Set();
            return ({ product }) => {
              const productName = normalizeSearchText(
                String(product.name || "").replace(/\s+\d+$/, ""),
              );
              if (seenNames.has(productName)) return false;
              seenNames.add(productName);
              return true;
            };
          })(),
        )
        .slice(0, 5)
        .map(({ product }) => product);

      const resultMarkup = matches.length
        ? `<div class="header-search-suggestions-head"><span><i class="bi bi-stars" aria-hidden="true"></i> پیشنهاد برای جستجوی شما</span><small>${faNumber(matches.length)} محصول</small></div><ul>${matches
            .map(
              (product) =>
                `<li><a class="header-search-suggestion" role="option" href="Product?id=${encodeURIComponent(product.id)}"><span class="header-search-suggestion-image"><img src="${escapeHtml(product.image)}" alt="" loading="lazy" decoding="async"></span><span class="header-search-suggestion-copy"><strong>${escapeHtml(product.name)}</strong><small>${escapeHtml(product.brand)} <bdi>· ${escapeHtml(product.type)}</bdi></small><b>${faNumber(product.price * 1000000)} <small>تومان</small></b></span><i class="bi bi-arrow-left" aria-hidden="true"></i></a></li>`,
            )
            .join(
              "",
            )}</ul><a class="header-search-all" href="${searchUrl(searchInput.value.trim())}">مشاهده همه نتایج <i class="bi bi-arrow-left" aria-hidden="true"></i></a>`
        : `<div class="header-search-empty"><i class="bi bi-search" aria-hidden="true"></i><span>محصولی با این عبارت پیدا نشد.</span><a href="${searchUrl(searchInput.value.trim())}">جستجو در همه محصولات <i class="bi bi-arrow-left" aria-hidden="true"></i></a></div>`;

      suggestions.innerHTML = resultMarkup;
      suggestions.innerHTML = isolateDrawerSearchMarkup(
        suggestions.innerHTML,
      );
      suggestions
        .querySelectorAll('[role="option"]')
        .forEach((option) => option.removeAttribute("role"));
      openSuggestions();
    };

    searchInput.addEventListener("input", renderSuggestions);
    headerSearchForm.addEventListener("submit", () =>
      rememberSearch(searchInput.value),
    );
    suggestions.addEventListener("click", (event) => {
      const clearHistory = event.target.closest("[data-clear-search-history]");
      if (clearHistory) {
        window.HNUI.storage.set(recentSearchKey, "[]");
        renderQuickSearchPanel();
        return;
      }
      const link = event.target.closest("a");
      if (link) rememberSearch(link.dataset.searchQuery || searchInput.value);
    });
    searchInput.addEventListener("focus", () => {
      if (Array.from(normalizeSearchText(searchInput.value)).length >= 3)
        renderSuggestions();
      else renderQuickSearchPanel();
    });
    searchInput.addEventListener("keydown", (event) => {
      if (event.key === "Escape" && !suggestions.hidden) {
        event.preventDefault();
        closeSuggestions();
      } else if (event.key === "ArrowDown" && !suggestions.hidden) {
        const firstSuggestion = suggestions.querySelector(
          ".header-search-suggestion, .header-search-quick-chip, .mobile-menu-search-suggestion, .mobile-menu-search-quick-chip",
        );
        if (firstSuggestion) {
          event.preventDefault();
          firstSuggestion.focus();
        }
      }
    });
      document.addEventListener("click", (event) => {
        if (!headerSearchForm.contains(event.target)) closeSuggestions();
      });
    });
  const storedListCount = (key) => {
    const list = window.HNUI.storage.json(key, []);
    return Array.isArray(list) ? new Set(list.map(String)).size : 0;
  };
  const readFavoriteList = () => {
    const list = window.HNUI.storage.json("hn-favorites", []);
    return Array.isArray(list) ? [...new Set(list.map(String))] : [];
  };
  const pulseListControl = (element) => {
    if (!element) return;
    window.clearTimeout(element._listReceivedTimer);
    element.classList.remove("list-received");
    void element.offsetWidth;
    element.classList.add("list-received");
    element._listReceivedTimer = window.setTimeout(() => {
      element.classList.remove("list-received");
      element._listReceivedTimer = null;
    }, 2100);
  };
  const flyProductToControl = (trigger, product, target) => {
    if (prefersReducedMotion || !target) return;
    const origin =
      trigger instanceof Element ? trigger : document.activeElement;
    if (!(origin instanceof Element)) return;
    const source =
      $(origin)
        .closest(
          ".product-card,.showcase-card,.rail-card,.featured-main,.featured-side",
        )
        .find("img")
        .first()[0] ||
      (origin.matches(".quick-add-cart") && $("#quickViewImage")[0]) ||
      (origin.id === "addProduct" && $("#productView .flow-photo")[0]);
    const image =
      source?.currentSrc || source?.getAttribute("src") || product?.image;
    if (!image) return;
    const a = source?.getBoundingClientRect() || origin.getBoundingClientRect();
    const b = target.getBoundingClientRect();
    const startX = a.left + a.width / 2 - 31;
    const startY = a.top + a.height / 2 - 31;
    const endX = b.left + b.width / 2 - startX - 31;
    const endY = b.top + b.height / 2 - startY - 31;
    const fly = document.createElement("img");
    fly.className = "fly-product";
    fly.alt = "";
    fly.setAttribute("aria-hidden", "true");
    fly.src = image;
    fly.style.left = `${startX}px`;
    fly.style.top = `${startY}px`;
    document.body.append(fly);
    const lift = Math.min(128, Math.max(76, Math.abs(endX) * 0.12));
    const p1 = { x: endX * 0.18, y: -lift };
    const p2 = { x: endX * 0.82, y: endY - lift * 0.8 };
    const offsets = [0, 0.12, 0.28, 0.46, 0.64, 0.82, 1];
    const flightFrames = offsets.map((t) => {
      const u = 1 - t;
      const x = 3 * u * u * t * p1.x + 3 * u * t * t * p2.x + t * t * t * endX;
      const y = 3 * u * u * t * p1.y + 3 * u * t * t * p2.y + t * t * t * endY;
      const scale =
        t < 0.12
          ? 0.78 + (t / 0.12) * 0.22
          : 1 - 0.82 * Math.max(0, (t - 0.12) / 0.88);
      const frame = {
        transform: `translate3d(${x}px, ${y}px, 0) scale(${scale}) rotate(${t * 24}deg)`,
        opacity: t === 0 ? 0 : t > 0.82 ? (1 - t) / 0.18 : 1,
        filter: `drop-shadow(0 0 ${Math.round(12 * (1 - t))}px rgba(245, 190, 46, ${0.65 * (1 - t)}))`,
        offset: t,
      };
      return frame;
    });
    const flight = fly.animate(flightFrames, {
      duration: 760,
      easing: "cubic-bezier(.2,.75,.2,1)",
      fill: "forwards",
    });
    flight.onfinish = () => fly.remove();
    flight.oncancel = () => fly.remove();
  };
  const syncFavoriteButtons = () => {
    const selected = new Set(readFavoriteList());
    $(".favorite[data-product-id]").each(function () {
      const active = selected.has(String(this.dataset.productId));
      $(this)
        .toggleClass("active", active)
        .attr("aria-pressed", String(active));
      $(this).attr(
        "aria-label",
        active ? "حذف از علاقه‌مندی‌ها" : "افزودن به علاقه‌مندی‌ها",
      );
      $(this)
        .find("i")
        .attr("class", active ? "bi bi-heart-fill" : "bi bi-heart");
    });
  };
  const cacheProductFromCard = (card, id) => {
    const cartButton = card?.querySelector(".add-cart[data-id]");
    if (!cartButton || !id) return;
    const cache = window.HNUI.storage.json("hn-product-cache", {});
    const name =
      cartButton.dataset.name ||
      card.querySelector("h3")?.textContent.trim() ||
      "محصول منتخب";
    cache[String(id)] = {
      id: String(id),
      name,
      brand:
        card.querySelector(".product-info small, small")?.textContent.trim() ||
        "",
      price: Number(cartButton.dataset.price) / 1000000 || 0,
      image:
        cartButton.dataset.image ||
        card
          .querySelector(".product-image img, .rail-image img")
          ?.getAttribute("src") ||
        "Shop/images/products/tool-hammer-drill.png",
      type: "مشخصات محصول",
      movement: "مطابق مشخصات کالا",
      strap: "مطابق اطلاعات سازنده",
    };
    window.HNUI.storage.set("hn-product-cache", JSON.stringify(cache));
  };
  const readCompareList = () => {
    const list = window.HNUI.storage.json("hn-compare", []);
    return Array.isArray(list) ? [...new Set(list.map(String))] : [];
  };
  const syncCompareButtons = () => {
    const selected = new Set(readCompareList());
    $(".compare-trigger[data-product-id]").each(function () {
      const active = selected.has(String(this.dataset.productId));
      $(this)
        .toggleClass("active", active)
        .attr("aria-pressed", String(active))
        .attr("aria-label", active ? "حذف از مقایسه" : "افزودن به مقایسه")
        .attr("title", active ? "حذف از مقایسه" : "افزودن به مقایسه")
        .find("i")
        .attr("class", "bi bi-arrow-left-right");
    });
  };
  const renderNavCounters = () => {
    const favoriteCount = storedListCount("hn-favorites");
    const compareCount = storedListCount("hn-compare");
    $("#favoritesCount").text(faNumber(favoriteCount));
    $("#compareCount").text(faNumber(compareCount));
    renderFavoritePanel();
    renderComparePanel();
    syncFavoriteButtons();
  };
  const renderFavoritePanel = () => {
    const ids = readFavoriteList();
    const products = new Map();
    (window.HNProducts || []).forEach((product) => {
      products.set(String(product.id), {
        name: product.name,
        brand: product.brand,
        type: product.type,
        price: Number(product.price) * 1000000,
        image: product.image,
      });
    });
    const cache = window.HNUI.storage.json("hn-product-cache", {});
    Object.entries(cache || {}).forEach(([id, product]) => {
      if (!products.has(String(id))) products.set(String(id), product);
    });
    $("#favoritesPanelCount").text(`${faNumber(ids.length)} محصول`);
    if (!ids.length) {
      $("#favoritesPanelItems").html(
        '<div class="compare-panel-empty header-panel-empty"><span class="header-panel-empty-icon"><i class="bi bi-heart" aria-hidden="true"></i></span><strong>هنوز محصولی در علاقه‌مندی‌ها نیست</strong><p>برای ذخیره، آیکن قلب روی کارت محصول را بزنید.</p></div>',
      );
      return;
    }
    $("#favoritesPanelItems").html(
      ids
        .map((id) => {
          const product = products.get(id) || {
            name: "محصول منتخب",
            price: 0,
            image: "Shop/images/products/tool-hammer-drill.png",
          };
          return `<article class="compare-panel-item" data-favorite-id="${escapeHtml(id)}"><img src="${escapeHtml(product.image || "Shop/images/products/tool-hammer-drill.png")}" alt="" loading="lazy"><span><strong>${escapeHtml(product.name || "محصول منتخب")}</strong><small>${faNumber(Number(product.price) * (Number(product.price) < 1000000 ? 1000000 : 1))} تومان</small></span><button class="cart-remove" type="button" data-favorite-remove="${escapeHtml(id)}" aria-label="حذف ${escapeHtml(product.name || "محصول")} از علاقه‌مندی‌ها"><i class="bi bi-trash3" aria-hidden="true"></i></button></article>`;
        })
        .join(""),
    );
  };
  const renderComparePanel = () => {
    const ids = readCompareList();
    const products = new Map();
    (window.HNProducts || []).forEach((product) => {
      products.set(String(product.id), {
        name: product.name,
        price: Number(product.price) * 1000000,
        image: product.image,
      });
    });
    document.querySelectorAll(".add-cart[data-id]").forEach((button) => {
      const card = button.closest(".product-card, .showcase-card, .rail-card");
      const image = card?.querySelector(".product-image img, .rail-image img");
      products.set(String(button.dataset.id), {
        name:
          button.dataset.name ||
          card?.querySelector("h3")?.textContent.trim() ||
          "محصول منتخب",
        price: Number(button.dataset.price) || 0,
        image:
          button.dataset.image ||
          image?.getAttribute("src") ||
          "Shop/images/products/tool-hammer-drill.png",
      });
    });
    $("#comparePanelCount").text(`${faNumber(ids.length)} محصول`);
    if (!ids.length) {
      $("#comparePanelItems").html(
        '<div class="compare-panel-empty header-panel-empty"><span class="header-panel-empty-icon"><i class="bi bi-arrow-left-right" aria-hidden="true"></i></span><strong>هنوز محصولی برای مقایسه نیست</strong><p>برای مقایسه، آیکن مقایسه روی کارت محصول را بزنید.</p></div>',
      );
      return;
    }
    $("#comparePanelItems").html(
      ids
        .map((id) => {
          const product = products.get(id) || {
            name: "محصول منتخب",
            price: 0,
            image: "Shop/images/products/tool-hammer-drill.png",
          };
          return `<article class="compare-panel-item" data-compare-id="${escapeHtml(id)}"><img src="${escapeHtml(product.image)}" alt="" loading="lazy"><span><strong>${escapeHtml(product.name)}</strong><small>${faNumber(product.price)} تومان</small></span><button class="cart-remove" type="button" data-compare-remove="${escapeHtml(id)}" aria-label="حذف ${escapeHtml(product.name)} از مقایسه"><i class="bi bi-trash3" aria-hidden="true"></i></button></article>`;
        })
        .join(""),
    );
  };
  renderNavCounters();
  document.addEventListener("hn:lists-change", renderNavCounters);
  window.addEventListener("storage", renderNavCounters);
  document.addEventListener("hn:lists-change", syncCompareButtons);
  window.addEventListener("storage", syncCompareButtons);

  const heroSwiperElement = document.querySelector("#heroSwiper");
  if (heroSwiperElement && window.Swiper) {
    const playActiveHeroVideo = (swiper) => {
      heroSwiperElement.querySelectorAll(".hero-video").forEach((video) => {
        if (!video.closest(".swiper-slide-active")) {
          video.pause();
          video.currentTime = 0;
        }
      });
      const activeVideo =
        swiper.slides[swiper.activeIndex]?.querySelector(".hero-video");
      if (activeVideo) {
        const playPromise = activeVideo.play();
        if (playPromise && typeof playPromise.catch === "function")
          playPromise.catch(() => {});
      }
    };
    const heroSwiper = new Swiper(heroSwiperElement, {
      effect: "fade",
      fadeEffect: { crossFade: true },
      speed: 700,
      loop: true,
      grabCursor: true,
      watchSlidesProgress: true,
      autoplay: prefersReducedMotion
        ? false
        : {
            delay: 6500,
            disableOnInteraction: false,
            pauseOnMouseEnter: true,
          },
      pagination: {
        el: "#heroSwiper .swiper-pagination",
        clickable: true,
      },
      navigation: {
        prevEl: "#heroSwiper .electrical-prev",
        nextEl: "#heroSwiper .electrical-next",
      },
      on: {
        init: playActiveHeroVideo,
        slideChangeTransitionStart: playActiveHeroVideo,
      },
    });
    if (prefersReducedMotion) {
      heroSwiperElement
        .querySelectorAll(".hero-video")
        .forEach((video) => video.pause());
    }
  }

  const categoryRow = document.querySelector(".electrical-home-row");
  const categoryMenu = categoryRow?.querySelector(".electrical-category-menu");
  const categoryPanel = categoryMenu?.querySelector("#electricalSubmenu");
  if (categoryRow && categoryMenu && categoryPanel) {
    const categoryContent = {
      tools: [
        [
          "دریل و پیچ‌گوشتی",
          "دریل چکشی",
          "دریل شارژی",
          "پیچ‌گوشتی برقی",
          "دریل بتن‌کن",
        ],
        ["برش و سایش", "فرز و مینی‌فرز", "اره عمودبر", "اره گردبر", "صفحه برش"],
        [
          "تجهیزات کارگاهی",
          "دستگاه جوش",
          "کارواش",
          "کمپرسور باد",
          "لوازم جانبی ابزار",
        ],
      ],
      "hand-tools": [
        [
          "ابزارهای دستی",
          "انواع آچار",
          "پیچ‌گوشتی دستی",
          "انبردست و سیم‌چین",
          "چکش و تبر",
        ],
        ["اندازه‌گیری دستی", "متر و خط‌کش", "تراز دستی", "گونیا", "کولیس"],
        [
          "لوازم کارگاهی",
          "جعبه ابزار",
          "گیره و پیچ‌دستی",
          "تیغه و مته",
          "متعلقات",
        ],
      ],
      components: [
        ["قطعات پایه", "مقاومت", "خازن", "دیود و LED", "سلف و ترانس"],
        [
          "قطعات نیمه‌رسانا",
          "آی‌سی",
          "ترانزیستور",
          "رگولاتور",
          "ماژول‌های آماده",
        ],
        ["برد و اتصال", "بردبورد", "برد مدار چاپی", "رله", "سنسور"],
      ],
      cables: [
        ["سیم و کابل", "سیم افشان", "کابل برق", "کابل شبکه", "کابل تلفن"],
        ["اتصالات", "فیش و سوکت", "ترمینال", "سرسیم", "کانکتور"],
        ["لوازم نصب", "داکت و بست", "لوله خرطومی", "چسب برق", "قرقره کابل"],
      ],
      lighting: [
        ["لامپ و LED", "لامپ LED", "لامپ هالوژن", "ریسه نواری", "پنل سقفی"],
        ["چراغ‌ها", "چراغ کارگاهی", "پروژکتور", "چراغ اضطراری", "چراغ خیابانی"],
        ["لوازم روشنایی", "سرپیچ", "درایور LED", "قاب و فریم", "سنسور حرکتی"],
      ],
      switchgear: [
        ["کلید و پریز", "کلید برق", "پریز توکار", "پریز صنعتی", "دیمر"],
        [
          "حفاظت مدار",
          "فیوز مینیاتوری",
          "محافظ جان",
          "کلید اتوماتیک",
          "تابلو برق",
        ],
        ["کنترل صنعتی", "کنتاکتور", "تایمر", "شستی و سیگنال", "ترموستات"],
      ],
      safety: [
        ["تجهیزات حفاظت", "دستکش عایق", "عینک ایمنی", "کفش کار", "ماسک"],
        [
          "محافظت برق",
          "محافظ ولتاژ",
          "ارت و اتصال زمین",
          "صاعقه‌گیر",
          "سرج ارستر",
        ],
        [
          "ایمنی کارگاه",
          "کلاه ایمنی",
          "لباس کار",
          "جعبه کمک‌های اولیه",
          "نوار هشدار",
        ],
      ],
      measurement: [
        ["ابزار اندازه‌گیری", "مولتی‌متر", "آمپرمتر", "ولت‌متر", "کلمپ‌متر"],
        ["تست و عیب‌یابی", "فازمتر", "تستر کابل", "تستر باتری", "دماسنج"],
        ["ابزار دقیق", "منبع تغذیه", "اسیلوسکوپ", "فرکانس‌متر", "لوازم جانبی"],
      ],
    };

    const triggers = [
      ...categoryMenu.querySelectorAll("button[data-category]"),
    ];
    triggers.forEach((trigger) =>
      trigger.setAttribute("aria-controls", "electricalSubmenu"),
    );
    const closeCategoryPanel = () => {
      categoryPanel.hidden = true;
      categoryRow.removeAttribute("data-open-category");
      triggers.forEach((trigger) => {
        trigger.classList.remove("active");
        trigger.setAttribute("aria-expanded", "false");
      });
    };
    const openCategoryPanel = (category) => {
      const groups = categoryContent[category];
      if (!groups) return;
      categoryPanel.replaceChildren();
      groups.forEach(([title, ...items]) => {
        const column = document.createElement("div");
        column.className = "electrical-submenu-column";
        const heading = document.createElement("strong");
        heading.textContent = title;
        column.append(heading);
        items.forEach((label) => {
          const link = document.createElement("a");
          link.href = `Search?q=${encodeURIComponent(label)}`;
          link.textContent = label;
          column.append(link);
        });
        categoryPanel.append(column);
      });
      categoryPanel.hidden = false;
      categoryRow.dataset.openCategory = category;
      triggers.forEach((trigger) => {
        const isOpen = trigger.dataset.category === category;
        trigger.classList.toggle("active", isOpen);
        trigger.setAttribute("aria-expanded", String(isOpen));
      });
    };

    if (window.matchMedia("(hover: hover) and (pointer: fine)").matches) {
      triggers.forEach((trigger) =>
        trigger.addEventListener("pointerenter", () =>
          openCategoryPanel(trigger.dataset.category),
        ),
      );
    }
    categoryMenu.addEventListener("focusin", (event) => {
      const trigger = event.target.closest("button[data-category]");
      if (trigger) openCategoryPanel(trigger.dataset.category);
    });
    triggers.forEach((trigger) =>
      trigger.addEventListener("click", () => {
        const alreadyOpen =
          categoryRow.dataset.openCategory === trigger.dataset.category &&
          !categoryPanel.hidden;
        if (alreadyOpen) closeCategoryPanel();
        else openCategoryPanel(trigger.dataset.category);
      }),
    );
    categoryRow.addEventListener("pointerleave", closeCategoryPanel);
    categoryRow.addEventListener("focusout", (event) => {
      if (!categoryRow.contains(event.relatedTarget)) closeCategoryPanel();
    });
  }

  const innerCategoryGroups = {
    tools: [
      ["دریل و پیچ‌گوشتی", "دریل چکشی", "دریل شارژی", "پیچ‌گوشتی برقی"],
      ["فرز و برش", "فرز آهنگری", "مینی‌فرز", "اره عمودبر"],
      ["تجهیزات کارگاهی", "دستگاه جوش", "کارواش", "کمپرسور باد"],
    ],
    "hand-tools": [
      ["آچار و بکس", "آچار فرانسه", "ست بکس", "آچار آلن"],
      ["پیچ‌گوشتی و انبر", "پیچ‌گوشتی دستی", "انبردست", "سیم‌چین"],
      ["لوازم ابزار", "جعبه ابزار", "مته و سری", "گیره و پیچ‌دستی"],
    ],
    components: [
      ["مقاومت و خازن", "مقاومت", "خازن", "دیود"],
      ["آی‌سی و نیمه‌رسانا", "آی‌سی", "ترانزیستور", "رگولاتور"],
      ["برد و ماژول", "بردبورد", "ماژول آماده", "سنسور"],
    ],
    cables: [
      ["سیم و کابل", "سیم افشان", "کابل برق", "کابل شبکه"],
      ["سوکت و کانکتور", "فیش برق", "سوکت شبکه", "کانکتور"],
      ["اتصالات نصب", "ترمینال", "سرسیم", "داکت و بست"],
    ],
    lighting: [
      ["لامپ و LED", "لامپ LED", "پنل سقفی", "ریسه نواری"],
      ["چراغ کارگاهی", "چراغ بازرسی", "چراغ اضطراری", "پروژکتور"],
      ["لوازم روشنایی", "سرپیچ", "درایور LED", "سنسور حرکتی"],
    ],
    switchgear: [
      ["کلید و پریز", "کلید برق", "پریز توکار", "پریز صنعتی"],
      ["فیوز و محافظ", "فیوز مینیاتوری", "محافظ جان", "محافظ ولتاژ"],
      ["تجهیزات کنترل", "کنتاکتور", "تایمر", "شستی و سیگنال"],
    ],
    safety: [
      ["ایمنی فردی", "دستکش عایق", "عینک ایمنی", "کفش کار"],
      ["حفاظت برق", "محافظ ولتاژ", "تجهیزات ارت", "صاعقه‌گیر"],
      ["ایمنی کارگاه", "کلاه ایمنی", "لباس کار", "جعبه کمک‌های اولیه"],
    ],
    measurement: [
      ["ابزار اندازه‌گیری", "مولتی‌متر", "آمپرمتر", "ولت‌متر"],
      ["تست و عیب‌یابی", "فازمتر", "تستر کابل", "تستر باتری"],
      ["ابزار دقیق", "کلمپ‌متر", "اسیلوسکوپ", "منبع تغذیه"],
    ],
  };

  document.querySelectorAll(".category-nav-mega").forEach((menu) => {
    const sidebar = menu.querySelector(".category-mega-sidebar");
    const content = menu.querySelector(".category-mega-panels");
    if (!sidebar || !content) return;
    const buttons = [...sidebar.querySelectorAll("[data-mega-category]")];
    const reset = () => {
      buttons.forEach((button) => {
        button.classList.remove("active");
        button.setAttribute("aria-expanded", "false");
      });
      content.replaceChildren();
      const hint = document.createElement("p");
      hint.className = "category-mega-hint";
      hint.textContent = "برای دیدن زیرشاخه‌ها، یک گروه را انتخاب کنید.";
      content.append(hint);
    };
    const activate = (button) => {
      const groups = innerCategoryGroups[button.dataset.megaCategory];
      if (!groups) return;
      buttons.forEach((item) => {
        const active = item === button;
        item.classList.toggle("active", active);
        item.setAttribute("aria-expanded", String(active));
      });
      content.replaceChildren();
      groups.forEach(([title, ...items]) => {
        const group = document.createElement("section");
        group.className = "category-mega-group";
        const heading = document.createElement("a");
        heading.className = "category-mega-title";
        heading.href = `Search?q=${encodeURIComponent(title)}`;
        heading.textContent = title;
        group.append(heading);
        items.forEach((label) => {
          const link = document.createElement("a");
          link.href = `Search?q=${encodeURIComponent(label)}`;
          link.textContent = label;
          group.append(link);
        });
        content.append(group);
      });
    };
    buttons.forEach((button) => {
      button.setAttribute("aria-expanded", "false");
      button.addEventListener("pointerenter", () => activate(button));
      button.addEventListener("focus", () => activate(button));
      button.addEventListener("click", () => activate(button));
    });
    const dropdown = menu.closest(".category-nav-dropdown");
    if (dropdown) {
      let closeTimer;
      const keepOpen = () => {
        window.clearTimeout(closeTimer);
        dropdown.classList.add("is-mega-open");
        if (!buttons.some((button) => button.classList.contains("active"))) {
          activate(buttons[0]);
        }
      };
      const closeSoon = () => {
        window.clearTimeout(closeTimer);
        closeTimer = window.setTimeout(() => {
          dropdown.classList.remove("is-mega-open");
          reset();
        }, 350);
      };
      dropdown.addEventListener("pointerenter", keepOpen);
      dropdown.addEventListener("pointerleave", closeSoon);
      dropdown.addEventListener("focusin", keepOpen);
      dropdown.addEventListener("focusout", (event) => {
        if (!dropdown.contains(event.relatedTarget)) closeSoon();
      });
      dropdown.addEventListener("hidden.bs.dropdown", () => {
        window.clearTimeout(closeTimer);
        dropdown.classList.remove("is-mega-open");
        reset();
      });
    }
  });

  /* Route mega-menu selections once, independently from slider lifecycle events. */
  document.addEventListener("click", (event) => {
    const link = event.target.closest(
      ".mega-sidebar a, .mega-links a, .brand-menu .dropdown-item",
    );
    if (!link) return;
    event.preventDefault();
    const label =
      link.querySelector("strong")?.textContent.trim() ||
      link.textContent.trim();
    const params = new URLSearchParams();
    const type = ["مردانه", "زنانه", "اسپرت", "کلاسیک"].find((item) =>
      label.includes(item),
    );
    if (link.closest(".brand-menu")) params.set("brand", label);
    else if (type) params.set("type", type);
    location.href = `Search${params.toString() ? `?${params}` : ""}`;
  });

  const storage = window.HNUI.storage;

  const megaData = {
    men: [
      ["سبک مردانه", "کلاسیک", "مینیمال", "کرنوگراف", "اسپرت", "روزمره"],
      ["نوع بند", "چرم طبیعی", "استیل ضدزنگ", "بند مش", "سیلیکونی", "برزنتی"],
      ["رنگ محبوب", "سرمه‌ای", "مشکی", "نقره‌ای", "سبز زیتونی", "سفید"],
      [
        "انتخاب سریع",
        "پرفروش مردانه",
        "تازه‌رسیده‌ها",
        "نسخه محدود",
        "زیر ۲۰ میلیون",
        "مناسب هدیه",
      ],
    ],
    women: [
      ["سبک زنانه", "مینیمال", "مجلسی", "کلاسیک", "فشن", "روزمره"],
      ["رنگ و متریال", "رزگلد", "طلایی", "نقره‌ای", "صدفی", "سرامیکی"],
      [
        "اندازه صفحه",
        "صفحه ظریف",
        "صفحه متوسط",
        "صفحه بزرگ",
        "بند باریک",
        "بند جواهری",
      ],
      [
        "انتخاب سریع",
        "پرفروش زنانه",
        "کالکشن مهتاب",
        "هدیه تولد",
        "زیر ۱۸ میلیون",
        "مدل‌های جدید",
      ],
    ],
    sport: [
      [
        "کاربرد",
        "دویدن و تمرین",
        "کوهنوردی",
        "شنا",
        "دوچرخه‌سواری",
        "استفاده روزانه",
      ],
      ["امکانات", "کرنوگراف", "ضدآب", "تاریخ‌شمار", "شب‌تاب", "زمان‌سنج"],
      [
        "نوع طراحی",
        "دیجیتال",
        "آنالوگ اسپرت",
        "هیبریدی",
        "صفحه بزرگ",
        "فوق سبک",
      ],
      [
        "انتخاب سریع",
        "مقاوم‌ترین‌ها",
        "بند سیلیکونی",
        "مدل‌های مشکی",
        "زیر ۱۵ میلیون",
        "تازه‌رسیده‌ها",
      ],
    ],
    classic: [
      ["طراحی", "سه عقربه", "صفحه ساده", "رومن", "وینتیج", "دست‌ساز"],
      [
        "نوع بند",
        "چرم قهوه‌ای",
        "چرم مشکی",
        "استیل نقره‌ای",
        "استیل طلایی",
        "بند مش",
      ],
      [
        "مناسب برای",
        "جلسه رسمی",
        "مراسم",
        "استایل اداری",
        "هدیه سازمانی",
        "استفاده روزانه",
      ],
      [
        "مجموعه‌ها",
        "آرمان کلاسیک",
        "میراث",
        "نسخه بوشهر",
        "کالکشن ۱۴۰۵",
        "مدل‌های محدود",
      ],
    ],
    couple: [
      ["ست بر اساس سبک", "کلاسیک", "مینیمال", "اسپرت", "رزگلد", "دو رنگ"],
      ["مناسبت", "سالگرد", "عروسی", "تولد", "نامزدی", "هدیه مشترک"],
      [
        "شخصی‌سازی",
        "حک اسم",
        "حک تاریخ",
        "جعبه ویژه",
        "کارت هدیه",
        "بسته‌بندی لوکس",
      ],
      [
        "انتخاب سریع",
        "محبوب‌ترین ست‌ها",
        "ست زیر ۳۵ میلیون",
        "نسخه محدود",
        "جدیدترین‌ها",
        "راهنمای انتخاب",
      ],
    ],
    gift: [
      ["هدیه برای", "آقایان", "بانوان", "مدیران", "همسر", "دوست"],
      ["مناسبت", "تولد", "سالگرد", "روز پدر", "روز مادر", "موفقیت کاری"],
      [
        "بازه قیمت",
        "زیر ۱۵ میلیون",
        "۱۵ تا ۲۵ میلیون",
        "۲۵ تا ۴۰ میلیون",
        "بالاتر از ۴۰ میلیون",
        "کارت هدیه",
      ],
      [
        "خدمات هدیه",
        "بسته‌بندی ویژه",
        "حک اختصاصی",
        "ارسال سورپرایزی",
        "پیام دست‌نویس",
      ],
    ],
    sale: [
      [
        "تخفیف‌ها",
        "پیشنهاد امروز",
        "تخفیف هفتگی",
        "فروش پایان فصل",
        "تخفیف ست‌ها",
        "آخرین موجودی",
      ],
      [
        "درصد تخفیف",
        "تا ۱۰٪",
        "تا ۲۰٪",
        "تا ۳۰٪",
        "بیش از ۳۰٪",
        "ارسال رایگان",
      ],
      ["دسته‌ها", "مردانه", "زنانه", "اسپرت", "کلاسیک", "لوازم جانبی"],
      [
        "فرصت محدود",
        "نوا نسخه شب",
        "آرمان رزگلد",
        "مهتاب مینیمال",
        "ست‌های منتخب",
        "مشاهده همه",
      ],
    ],
  };

  let navFrame = null;
  const updateNav = () => {
    navFrame = null;
    $nav.toggleClass("scrolled", $window.scrollTop() > 40);
  };
  $window.on("scroll", () => {
    if (navFrame === null) navFrame = window.requestAnimationFrame(updateNav);
  });
  updateNav();

  const revealSelectors = [
    ".section-head",
    ".category-showcase-head",
    ".showcase-card",
    ".product-col",
    ".brands-heading",
    ".brand-card",
    ".featured-main",
    ".featured-side",
    ".recommend-box",
    ".testimonial-intro",
    ".testimonial-card",
    ".story-image",
    ".story-copy",
    ".journal-main",
    ".journal-list",
    ".benefit",
    ".store-card",
    ".newsletter",
  ];
  const $revealItems = $(revealSelectors.join(", "));
  $revealItems.each(function (index) {
    const $item = $(this);
    if ($item.is(".story-image")) $item.attr("data-reveal", "right");
    else if ($item.is(".story-copy")) $item.attr("data-reveal", "left");
    else $item.attr("data-reveal", "");
    $item.css("--reveal-delay", `${Math.min(index % 6, 5) * 70}ms`);
  });

  if (prefersReducedMotion || !("IntersectionObserver" in window)) {
    $revealItems.addClass("is-visible");
  } else {
    const revealObserver = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (!entry.isIntersecting) return;
          entry.target.classList.add("is-visible");
          revealObserver.unobserve(entry.target);
        });
      },
      { threshold: 0.12, rootMargin: "0px 0px -50px" },
    );
    $revealItems.each(function () {
      revealObserver.observe(this);
    });
  }

  const $megaGrid = $("#megaContentGrid");
  const $megaCategoryLinks = $("[data-mega-category]");
  const megaGroupIcon = (title) => {
    const icons = {
      "سبک مردانه": "bi-stars",
      "سبک زنانه": "bi-stars",
      "سبک اسپرت": "bi-activity",
      "سبک کلاسیک": "bi-gem",
      "نوع بند": "bi-link-45deg",
      "رنگ محبوب": "bi-palette2",
      "رنگ و متریال": "bi-palette2",
      "اندازه صفحه": "bi-aspect-ratio",
      "انتخاب سریع": "bi-lightning-charge",
    };
    return icons[title] || "bi-compass";
  };
  const renderMegaCategory = (category) => {
    if (!megaData[category]) return;
    $megaGrid.html(
      megaData[category]
        .map(
          (group) =>
            `<div class="col-3 mega-links"><h4><i class="bi ${megaGroupIcon(group[0])}" aria-hidden="true"></i>${group[0]}</h4>${group
              .slice(1)
              .map((item) => `<a href="#collection">${item}</a>`)
              .join("")}</div>`,
        )
        .join(""),
    );
    $megaCategoryLinks
      .toggleClass("active", false)
      .filter(`[data-mega-category="${category}"]`)
      .addClass("active");
  };

  $megaCategoryLinks.on("mouseenter focus", function () {
    renderMegaCategory($(this).data("mega-category"));
  });
  renderMegaCategory("men");

  const initOwlCarousel = ($element, options) => {
    if ($.fn.owlCarousel) $element.owlCarousel(options);
  };

  const carouselMotion = {
    smartSpeed: 600,
    autoplaySpeed: 600,
    navSpeed: 600,
    dotsSpeed: 600,
    dragEndSpeed: 600,
    slideBy: 1,
  };

  const catalogProducts = window.HNProducts || [];
  if (catalogProducts.length >= 6) {
    const featuredBindings = [
      { selector: ".featured-main", product: catalogProducts[0] },
      { selector: ".featured-side", product: catalogProducts[5] },
    ];
    featuredBindings.forEach(({ selector, product }) => {
      const $card = $(selector);
      if (!$card.length) return;
      $card.find("img").attr({ src: product.image, alt: product.name });
      $card.find("h3").text(product.name);
      $card
        .find(".featured-content > p, .featured-side-content > p")
        .text(product.detail?.intro || product.type);
      $card
        .find(".featured-content > small")
        .text(`${product.brand} · ${product.type}`);
      $card
        .find(".featured-price, .featured-buy strong")
        .text(`${faNumber(product.price * 1000000)} تومان`);
      $card.find(".add-cart").attr({
        "data-id": String(product.id),
        "data-name": product.name,
        "data-price": String(product.price * 1000000),
        "data-image": product.image,
      });
    });
  }
  const $amazingItems = $("#amazing .row > div");
  if ($amazingItems.length === 3 && catalogProducts.length > 15) {
    [catalogProducts[0], catalogProducts[5], catalogProducts[10]].forEach(
      (product, index) => {
        const $item = $amazingItems.eq(index);
        $item.removeClass("col-md-4").addClass("col-12 col-md-6 col-xl");
        $item.find(".product-image").removeClass("cream warm light");
        $item.find("img").attr({ src: product.image, alt: product.name });
        $item
          .find(".badge-offer")
          .text(
            product.discountPercent
              ? `${faNumber(product.discountPercent)}٪ تخفیف`
              : "پیشنهاد ویژه",
          );
        $item.find(".showcase-card-body small").text(product.brand);
        $item.find(".showcase-card-body h3").text(product.name);
        $item
          .find(".showcase-card-body strong")
          .text(`${faNumber(product.price * 1000000)} تومان`);
        $item.find(".add-cart").attr({
          "data-id": String(product.id),
          "data-name": product.name,
          "data-price": String(product.price * 1000000),
          "data-image": product.image,
        });
      },
    );
    window.HNUI?.enhance(document.querySelector("#amazing"));
  }
  const productCardMarkup = (product) => `
    <div class="showcase-slide">
      <article class="showcase-card">
        <div class="product-image">
          <a href="Product?id=${product.id}" aria-label="مشاهده ${escapeHtml(product.name)}">
            <img loading="lazy" decoding="async" src="${escapeHtml(product.image)}" alt="${escapeHtml(product.name)}">
          </a>
          ${product.badge ? `<span class="badge-best">${escapeHtml(product.badge)}</span>` : ""}
        </div>
        <div class="showcase-card-body">
          <h3>${escapeHtml(product.name)}</h3>
          <strong>${faNumber(product.price * 1000000)} تومان</strong>
          <button type="button" class="add-cart" data-id="${product.id}" data-name="${escapeHtml(product.name)}" data-price="${product.price * 1000000}" data-image="${escapeHtml(product.image)}">
            <i class="bi bi-bag-plus"></i> افزودن به سبد
          </button>
        </div>
      </article>
    </div>`;
  const railCardMarkup = (product) => `
    <article class="rail-card">
      <div class="rail-image">
        <a href="Product?id=${product.id}" aria-label="مشاهده ${escapeHtml(product.name)}">
          <img loading="lazy" decoding="async" src="${escapeHtml(product.image)}" alt="${escapeHtml(product.name)}">
        </a>
        ${product.badge ? `<span>${escapeHtml(product.badge)}</span>` : ""}
      </div>
      <h3>${escapeHtml(product.name)}</h3>
      <strong>${faNumber(product.price * 1000000)} <small>تومان</small></strong>
      <button type="button" class="add-cart" data-id="${product.id}" data-name="${escapeHtml(product.name)}" data-price="${product.price * 1000000}" data-image="${escapeHtml(product.image)}">
        <i class="bi bi-bag-plus"></i> افزودن به سبد
      </button>
    </article>`;

  if (catalogProducts.length >= 24) {
    $("#bestsellersSlider").html(
      catalogProducts.slice(0, 12).map(productCardMarkup).join(""),
    );
    $("#newestSlider").html(
      catalogProducts.slice(14, 20).map(productCardMarkup).join(""),
    );
    $("#productRail").html(
      catalogProducts.slice(20, 25).map(railCardMarkup).join(""),
    );
  }

  initOwlCarousel($("#productRail"), {
    ...carouselMotion,
    rtl: true,
    loop: true,
    rewind: false,
    center: false,
    autoWidth: false,
    stagePadding: 0,
    margin: 18,
    nav: true,
    dots: true,
    autoplay: !prefersReducedMotion,
    autoplayTimeout: 5500,
    autoplayHoverPause: true,
    lazyLoad: true,
    navText: [
      '<i class="bi bi-arrow-right"></i>',
      '<i class="bi bi-arrow-left"></i>',
    ],
    responsive: {
      0: { items: 1 },
      768: { items: 3 },
      1200: { items: 4 },
    },
  });

  initOwlCarousel($("#brandsSlider"), {
    ...carouselMotion,
    rtl: true,
    loop: false,
    rewind: false,
    startPosition: 0,
    margin: 16,
    nav: false,
    dots: false,
    autoplay: false,
    navText: [
      '<i class="bi bi-arrow-right"></i>',
      '<i class="bi bi-arrow-left"></i>',
    ],
    responsive: {
      0: { items: 2 },
      576: { items: 3 },
      768: { items: 4 },
      1200: { items: 6 },
    },
  });

  const showcaseSliderOptions = {
    ...carouselMotion,
    rtl: true,
    loop: true,
    rewind: false,
    center: false,
    autoWidth: false,
    stagePadding: 0,
    margin: 18,
    nav: true,
    dots: true,
    autoplay: !prefersReducedMotion,
    autoplayTimeout: 4200,
    autoplayHoverPause: true,
    lazyLoad: true,
    navText: [
      '<i class="bi bi-arrow-right"></i>',
      '<i class="bi bi-arrow-left"></i>',
    ],
    responsive: {
      0: { items: 1 },
      768: { items: 2 },
      1100: { items: 5 },
    },
  };

  initOwlCarousel(
    $("#bestsellersSlider, #newestSlider"),
    showcaseSliderOptions,
  );

  $(".showcase-card, .rail-card").each(function () {
    const $card = $(this);
    const $image = $card.find(".product-image, .rail-image").first();
    if (!$image.find(".favorite").length) {
      $image.prepend(
        '<button class="favorite" type="button" aria-label="افزودن به علاقه‌مندی"><i class="bi bi-heart"></i></button>',
      );
    }
  });
  $(".favorite").attr("aria-pressed", "false");

  const $cartWrap = $("#cartWrap");
  const $cartToggle = $("#cartToggle");
  const $cartPanel = $("#cartPanel");
  const $cartItems = $("#cartItems");
  const $cartFooter = $("#cartFooter");
  const $favoritesWrap = $("#favoritesWrap");
  const $favoritesToggle = $("#favoritesToggle");
  const $favoritesPanel = $("#favoritesPanel");
  const $compareWrap = $("#compareWrap");
  const $compareToggle = $("#compareToggle");
  const $comparePanel = $("#comparePanel");
  const $accountWrap = $("#accountWrap");
  const $accountToggle = $("#accountToggle");
  const $accountPanel = $("#accountPanel");
  const syncMobileHeaderPanelLock = () => {
    const lockPage =
      $window.width() <= mobileBreakpoint &&
      ($cartPanel.hasClass("open") ||
        $favoritesPanel.hasClass("open") ||
        $comparePanel.hasClass("open") ||
        $accountPanel.hasClass("open"));
    $body.toggleClass("cart-mobile-open", lockPage);
    $html.toggleClass("cart-mobile-open", lockPage);
  };
  const setFavoriteOpen = (open) => {
    $favoritesWrap.toggleClass("open", open);
    $favoritesPanel
      .toggleClass("open", open)
      .attr("aria-hidden", String(!open));
    $favoritesToggle.attr("aria-expanded", String(open));
    syncMobileHeaderPanelLock();
  };
  const placeFavoritePanel = () => {
    if ($window.width() <= mobileBreakpoint) $favoritesPanel.appendTo($body);
    else $favoritesPanel.appendTo($favoritesWrap);
    setFavoriteOpen(false);
  };
  const setCompareOpen = (open) => {
    $compareWrap.toggleClass("open", open);
    $comparePanel.toggleClass("open", open).attr("aria-hidden", String(!open));
    $compareToggle.attr("aria-expanded", String(open));
    syncMobileHeaderPanelLock();
  };
  const placeComparePanel = () => {
    if ($window.width() <= mobileBreakpoint) $comparePanel.appendTo($body);
    else $comparePanel.appendTo($compareWrap);
    setCompareOpen(false);
  };
  placeFavoritePanel();
  placeComparePanel();
  const readStoredArray = (key) => {
    try {
      const storedValue = storage.json(key, []);
      if (!Array.isArray(storedValue)) return [];
      return storedValue
        .filter((item) => item && typeof item === "object")
        .map((item) => ({
          id: String(item.id || "").slice(0, 80),
          name: String(item.name || "").slice(0, 120),
          price: Number(item.price),
          image: String(item.image || "")
            .slice(0, 250)
            .replace(
              /^((?:Shop\/)?images\/watch-(?:blue|cream|silver|story|hero))\.(?:jpe?g|png)$/i,
              "$1.webp",
            ),
          quantity: Math.min(
            99,
            Math.max(1, Number.parseInt(item.quantity, 10) || 1),
          ),
        }))
        .filter(
          (item) =>
            item.id &&
            item.name &&
            Number.isFinite(item.price) &&
            item.price >= 0 &&
            /^(?:Shop\/)?images\/[\w.-]+$/i.test(item.image),
        );
    } catch {
      return [];
    }
  };
  let cart = window.HNUI.cart.read();

  const setCartOpen = (open) => {
    $cartWrap.toggleClass("open", open);
    $cartPanel.toggleClass("open", open);
    $cartToggle.attr("aria-expanded", String(open));
    syncMobileHeaderPanelLock();
  };
  const placeCartPanel = () => {
    if ($window.width() <= mobileBreakpoint) $cartPanel.appendTo($body);
    else $cartPanel.appendTo($cartWrap);
    setCartOpen(false);
  };
  placeCartPanel();

  const $loginModalElement = $("#loginModal");
  const loginModal =
    window.bootstrap?.Modal && $loginModalElement.length
      ? bootstrap.Modal.getOrCreateInstance($loginModalElement[0], {
          backdrop: "static",
          keyboard: true,
        })
      : null;
  let userLoggedIn = window.HNUI.auth.isLoggedIn();

  const setAccountOpen = (open) => {
    if (open) {
      setCartOpen(false);
      setFavoriteOpen(false);
      setCompareOpen(false);
    }
    $accountWrap.toggleClass("open", open);
    $accountPanel.toggleClass("open", open);
    $accountToggle.attr("aria-expanded", String(open));
    syncMobileHeaderPanelLock();
  };
  const placeAccountPanel = () => {
    if ($window.width() <= mobileBreakpoint) $accountPanel.appendTo($body);
    else $accountPanel.appendTo($accountWrap);
    setAccountOpen(false);
  };
  const updateAccountUI = () => {
    $accountToggle
      .toggleClass("logged-out", !userLoggedIn)
      .toggleClass("logged-in", userLoggedIn)
      .html(
        userLoggedIn
          ? '<i class="bi bi-person"></i><span class="account-label">حساب من</span><i class="bi bi-chevron-down"></i>'
          : '<i class="bi bi-person"></i><span class="account-label">ورود / ثبت‌نام</span><i class="bi bi-chevron-down"></i>',
      );
  };
  placeAccountPanel();
  let wasMobile = $window.width() <= mobileBreakpoint;
  let resizeFrame = null;
  $window.on("resize", () => {
    if (resizeFrame !== null) return;
    resizeFrame = window.requestAnimationFrame(() => {
      resizeFrame = null;
      const isMobile = $window.width() <= mobileBreakpoint;
      if (isMobile === wasMobile) return;
      wasMobile = isMobile;
      placeCartPanel();
      placeAccountPanel();
      placeFavoritePanel();
      placeComparePanel();
    });
  });
  updateAccountUI();

  $accountToggle.on("click", (event) => {
    event.stopPropagation();
    if (!userLoggedIn) {
      setCartOpen(false);
      if (loginModal) loginModal.show();
      return;
    }
    setAccountOpen(!$accountPanel.hasClass("open"));
  });
  $accountPanel.on("click", (event) => event.stopPropagation());
  let accountHoverTimer = null;
  const clearAccountHoverTimer = () => {
    window.clearTimeout(accountHoverTimer);
    accountHoverTimer = null;
  };
  const scheduleAccountClose = () => {
    if ($window.width() <= mobileBreakpoint) return;
    clearAccountHoverTimer();
    accountHoverTimer = window.setTimeout(() => {
      if (!$accountWrap.is(":hover") && !$accountPanel.is(":hover"))
        setAccountOpen(false);
    }, 220);
  };
  $accountWrap.on("mouseenter", () => {
    if ($window.width() <= mobileBreakpoint || !userLoggedIn) return;
    clearAccountHoverTimer();
    setCartOpen(false);
    setAccountOpen(true);
  });
  $accountWrap.on("mouseleave", scheduleAccountClose);
  $accountPanel.on("mouseenter", clearAccountHoverTimer);
  $accountPanel.on("mouseleave", scheduleAccountClose);

  let loginMode = "login";
  let pendingAuthMode = "login";
  const $loginEmailStep = $("#loginEmailStep");
  const $registerEmailStep = $("#registerEmailStep");
  const $forgotPasswordStep = $("#forgotPasswordStep");
  const $emailOtpStep = $("#emailOtpStep");
  const $emailOtpInput = $("#loginEmailOtp");
  const $loginEmailError = $("#loginEmailError");
  const $registerEmailError = $("#registerEmailError");
  const $emailOtpError = $("#emailOtpError");
  const selectLoginMode = (mode) => {
    loginMode = mode === "register" ? "register" : "login";
    const isLogin = loginMode === "login";
    $("#loginModeTab")
      .toggleClass("active", isLogin)
      .attr("aria-selected", String(isLogin));
    $("#registerModeTab")
      .toggleClass("active", !isLogin)
      .attr("aria-selected", String(!isLogin));
    $loginEmailStep.toggleClass("d-none", !isLogin).prop("hidden", !isLogin);
    $registerEmailStep.toggleClass("d-none", isLogin).prop("hidden", isLogin);
    $forgotPasswordStep.addClass("d-none").prop("hidden", true);
    $emailOtpStep.addClass("d-none").prop("hidden", true);
    $(".login-auth-tabs").removeClass("d-none");
    $loginEmailError.empty();
    $registerEmailError.empty();
    $emailOtpError.empty();
  };

  $("#loginModeTab").on("click", () => selectLoginMode("login"));
  $("#registerModeTab").on("click", () => selectLoginMode("register"));

  $("#forgotPasswordLink").on("click", () => {
    $("#forgotPasswordEmail").val($("#loginEmail").val().trim());
    $("#forgotPasswordError").empty();
    $(".login-auth-tabs").addClass("d-none");
    $loginEmailStep.addClass("d-none").prop("hidden", true);
    $registerEmailStep.addClass("d-none").prop("hidden", true);
    $emailOtpStep.addClass("d-none").prop("hidden", true);
    $forgotPasswordStep.removeClass("d-none").prop("hidden", false);
    $("#forgotPasswordEmail").trigger("focus");
  });

  $("#forgotPasswordBack").on("click", () => {
    selectLoginMode("login");
    $("#loginEmail").trigger("focus");
  });

  $("#forgotPasswordForm").on("submit", (event) => {
    event.preventDefault();
    const emailInput = $("#forgotPasswordEmail")[0];
    if (!emailInput.value.trim() || !emailInput.validity.valid) {
      $("#forgotPasswordError").text("ایمیل را با قالب صحیح وارد کنید.");
      return;
    }
    $("#forgotPasswordError").text(
      "ارسال ایمیل بازنشانی هنوز به سرویس ایمیل سایت متصل نشده است.",
    );
  });

  const requestEmailCode = (mode, email) => {
    pendingAuthMode = mode;
    $("#sentEmail").text(email);
    $("#emailOtpTitle").text(
      mode === "register"
        ? "کد فعال‌سازی را وارد کنید"
        : "کد ورود را وارد کنید",
    );
    $(".login-auth-tabs").addClass("d-none");
    $loginEmailStep.addClass("d-none").prop("hidden", true);
    $registerEmailStep.addClass("d-none").prop("hidden", true);
    $emailOtpStep.removeClass("d-none").prop("hidden", false);
    $emailOtpInput.val("123456").trigger("focus");
  };

  $("#loginEmailForm").on("submit", (event) => {
    event.preventDefault();
    const email = $("#loginEmail").val().trim();
    const password = $("#loginPassword").val();
    if (!email || !$("#loginEmail")[0].validity.valid || !password) {
      $loginEmailError.text("ایمیل معتبر و رمز عبور را وارد کنید.");
      return;
    }
    $loginEmailError.empty();
    userLoggedIn = true;
    window.HNUI.auth.login();
    updateAccountUI();
    if (loginModal) loginModal.hide();
  });

  $("#registerEmailForm").on("submit", (event) => {
    event.preventDefault();
    const emailInput = $("#registerEmail")[0];
    const email = emailInput.value.trim();
    const firstName = $("#registerFirstName").val().trim();
    const lastName = $("#registerLastName").val().trim();
    if (!firstName || !lastName || !emailInput.validity.valid) {
      $registerEmailError.text("نام، نام خانوادگی و ایمیل معتبر را وارد کنید.");
      return;
    }
    $registerEmailError.empty();
    requestEmailCode("register", email);
  });

  $("#loginEmailOtp").on("input", function () {
    this.value = window.HNUI.toEnglishDigits(this.value)
      .replace(/\D/g, "")
      .slice(0, 6);
  });

  $("#emailOtpForm").on("submit", (event) => {
    event.preventDefault();
    const code = window.HNUI.toEnglishDigits($emailOtpInput.val().trim());
    if (!/^\d{6}$/.test(code)) {
      $emailOtpError.text("کد شش‌رقمی را کامل وارد کنید.");
      return;
    }
    $emailOtpError.empty();
    userLoggedIn = true;
    window.HNUI.auth.login();
    updateAccountUI();
    if (loginModal) loginModal.hide();
  });

  $("#loginEmailBack").on("click", () => {
    selectLoginMode(pendingAuthMode);
    $(
      pendingAuthMode === "register" ? "#registerEmail" : "#loginEmail",
    ).trigger("focus");
  });
  $loginModalElement.on("hidden.bs.modal", () => {
    $(
      "#loginEmailForm, #registerEmailForm, #emailOtpForm, #forgotPasswordForm",
    ).each(function () {
      this.reset();
    });
    $("#sentEmail").empty();
    $emailOtpInput.val("");
    selectLoginMode("login");
  });
  $(".account-links>a:last-child").on("click", (event) => {
    event.preventDefault();
    userLoggedIn = false;
    window.HNUI.auth.logout();
    setAccountOpen(false);
    updateAccountUI();
  });

  const saveCart = () => {
    cart = window.HNUI?.cart.write(cart) || cart;
    renderCart();
  };
  const renderCart = () => {
    const totalCount = cart.reduce((sum, item) => sum + item.quantity, 0);
    const totalPrice = cart.reduce(
      (sum, item) => sum + item.price * item.quantity,
      0,
    );
    $("#cartCount").text(faNumber(totalCount));
    $("#cartTitleCount").text(`${faNumber(totalCount)} محصول`);
    $("#cartTotal").text(`${faNumber(totalPrice)} تومان`);
    $cartFooter.toggle(cart.length > 0);
    if (!cart.length) {
      $cartItems.html(
        '<div class="empty-cart header-panel-empty"><span class="empty-cart-icon header-panel-empty-icon"><i class="bi bi-cart3" aria-hidden="true"></i></span><strong>سبد خرید شما خالی است</strong><p>برای شروع، یک محصول به سبد خرید اضافه کنید.</p></div>',
      );
      return;
    }
    $cartItems.html(
      cart
        .map((item) => {
          const safeId = escapeHtml(item.id);
          const safeName = escapeHtml(item.name);
          const safeImage = escapeHtml(item.image);
          return `<div class="cart-item" data-cart-id="${safeId}"><img src="${safeImage}" alt="${safeName}" loading="lazy" decoding="async"><div class="cart-item-info"><strong>${safeName}</strong><div class="cart-item-actions"><span>${faNumber(item.price)} تومان</span><div class="cart-item-controls"><div class="cart-quantity"><button type="button" data-action="plus" aria-label="افزایش تعداد">+</button><b>${faNumber(item.quantity)}</b><button type="button" data-action="minus" aria-label="کاهش تعداد">−</button></div><button class="cart-remove" type="button" data-action="remove" aria-label="حذف محصول"><i class="bi bi-trash3"></i></button></div></div></div></div>`;
        })
        .join(""),
    );
  };

  document.addEventListener("hn:cart-change", () => {
    cart = window.HNUI?.cart.read() || cart;
    renderCart();
  });
  document.addEventListener("hn:cart-added", () => setCartOpen(true));

  $cartItems.on("click", "[data-action]", function (event) {
    const $button = $(this);
    const $row = $button.closest("[data-cart-id]");
    const item = cart.find(
      (product) => String(product.id) === String($row.attr("data-cart-id")),
    );
    if (!item) return;
    if ($button.data("action") === "plus")
      window.HNUI.cart.update(item.id, item.quantity + 1);
    if ($button.data("action") === "minus")
      window.HNUI.cart.update(item.id, item.quantity - 1);
    if ($button.data("action") === "remove") {
      event.stopPropagation();
      window.HNUI.cart.remove(item.id);
      setCartOpen(true);
    }
  });

  $cartToggle.on("click", (event) => {
    event.stopPropagation();
    setAccountOpen(false);
    setFavoriteOpen(false);
    setCompareOpen(false);
    setCartOpen(!$cartPanel.hasClass("open"));
  });
  $favoritesPanel.on("click", "[data-favorite-remove]", function (event) {
    event.stopPropagation();
    const removedId = String(this.dataset.favoriteRemove || "");
    window.HNUI.storage.set(
      "hn-favorites",
      JSON.stringify(readFavoriteList().filter((id) => id !== removedId)),
    );
    setFavoriteOpen(true);
  });
  $favoritesToggle.on("click", (event) => {
    if (
      $window.width() > mobileBreakpoint ||
      event.button !== 0 ||
      event.metaKey ||
      event.ctrlKey ||
      event.shiftKey ||
      event.altKey ||
      $favoritesPanel.hasClass("open")
    )
      return;

    event.preventDefault();
    event.stopPropagation();
    setCartOpen(false);
    setAccountOpen(false);
    setCompareOpen(false);
    setFavoriteOpen(true);
  });
  let favoriteHoverTimer = null;
  $favoritesWrap.on("mouseenter", () => {
    if ($window.width() <= mobileBreakpoint) return;
    window.clearTimeout(favoriteHoverTimer);
    setCartOpen(false);
    setCompareOpen(false);
    setFavoriteOpen(true);
  });
  $favoritesWrap.on("mouseleave", () => {
    if ($window.width() <= mobileBreakpoint) return;
    favoriteHoverTimer = window.setTimeout(() => {
      if (!$favoritesWrap.is(":hover") && !$favoritesPanel.is(":hover"))
        setFavoriteOpen(false);
    }, 220);
  });
  $favoritesPanel.on("mouseenter", () =>
    window.clearTimeout(favoriteHoverTimer),
  );
  $favoritesPanel.on("mouseleave", () => {
    if ($window.width() > mobileBreakpoint) setFavoriteOpen(false);
  });
  $("#comparePanel").on("click", "[data-compare-remove]", function (event) {
    event.stopPropagation();
    const removedId = String(this.dataset.compareRemove || "");
    window.HNUI.storage.set(
      "hn-compare",
      JSON.stringify(readCompareList().filter((id) => id !== removedId)),
    );
    syncCompareButtons();
    setCompareOpen(true);
  });
  let compareHoverTimer = null;
  $compareWrap.on("mouseenter", () => {
    if ($window.width() <= mobileBreakpoint) return;
    window.clearTimeout(compareHoverTimer);
    setCartOpen(false);
    setFavoriteOpen(false);
    setCompareOpen(true);
  });
  $compareWrap.on("mouseleave", () => {
    if ($window.width() <= mobileBreakpoint) return;
    compareHoverTimer = window.setTimeout(() => {
      if (!$compareWrap.is(":hover") && !$comparePanel.is(":hover"))
        setCompareOpen(false);
    }, 220);
  });
  $compareToggle.on("click", (event) => {
    if (
      $window.width() > mobileBreakpoint ||
      event.button !== 0 ||
      event.metaKey ||
      event.ctrlKey ||
      event.shiftKey ||
      event.altKey ||
      $comparePanel.hasClass("open")
    )
      return;

    event.preventDefault();
    event.stopPropagation();
    setCartOpen(false);
    setAccountOpen(false);
    setFavoriteOpen(false);
    setCompareOpen(true);
  });
  $comparePanel.on("mouseenter", () => window.clearTimeout(compareHoverTimer));
  $comparePanel.on("mouseleave", () => {
    if ($window.width() > mobileBreakpoint) setCompareOpen(false);
  });
  let cartHoverTimer = null;
  const clearCartHoverTimer = () => {
    window.clearTimeout(cartHoverTimer);
    cartHoverTimer = null;
  };
  $cartWrap.on("mouseenter", () => {
    if ($window.width() <= mobileBreakpoint) return;
    clearCartHoverTimer();
    setAccountOpen(false);
    setCartOpen(true);
  });
  $cartWrap.on("mouseleave", () => {
    if ($window.width() <= mobileBreakpoint) return;
    clearCartHoverTimer();
    cartHoverTimer = window.setTimeout(() => {
      if (!$cartWrap.is(":hover") && !$cartPanel.is(":hover"))
        setCartOpen(false);
    }, 220);
  });
  $cartPanel.on("mouseenter", clearCartHoverTimer);
  $cartPanel.on("mouseleave", () => {
    if ($window.width() <= mobileBreakpoint) return;
    clearCartHoverTimer();
    cartHoverTimer = window.setTimeout(() => {
      if (!$cartWrap.is(":hover") && !$cartPanel.is(":hover"))
        setCartOpen(false);
    }, 220);
  });
  $cartPanel.on("click", (event) => event.stopPropagation());
  $document.on("click", (event) => {
    const $target = $(event.target);
    if (!$target.closest("#cartWrap, #cartPanel, .add-cart").length)
      setCartOpen(false);
    if (!$target.closest("#compareWrap, #comparePanel").length)
      setCompareOpen(false);
    if (!$target.closest("#favoritesWrap, #favoritesPanel").length)
      setFavoriteOpen(false);
    if (!$target.closest("#accountWrap, #accountPanel").length)
      setAccountOpen(false);
  });
  renderCart();

  $document.on("click", ".favorite", function () {
    const $button = $(this);
    const productId = String(
      $button.attr("data-product-id") ||
        $button
          .closest(".product-card, .showcase-card, .rail-card")
          .find(".add-cart[data-id]")
          .first()
          .attr("data-id") ||
        "",
    );
    if (!productId) return;
    cacheProductFromCard(
      $button.closest(".product-card, .showcase-card, .rail-card")[0],
      productId,
    );
    const selected = readFavoriteList();
    const next = selected.includes(productId)
      ? selected.filter((id) => id !== productId)
      : [...selected, productId];
    window.HNUI.storage.set("hn-favorites", JSON.stringify(next));
    const isFavorite = next.includes(productId);
    $button
      .attr("data-product-id", productId)
      .toggleClass("active", isFavorite)
      .attr("aria-pressed", String(isFavorite));
    if (isFavorite) {
      pulseListControl(document.querySelector("#favoritesToggle"));
      flyProductToControl(
        this,
        null,
        document.querySelector("#favoritesToggle"),
      );
    }
    $button.attr(
      "aria-label",
      isFavorite ? "حذف از علاقه‌مندی‌ها" : "افزودن به علاقه‌مندی‌ها",
    );
    $button
      .find("i")
      .attr("class", isFavorite ? "bi bi-heart-fill" : "bi bi-heart");
    $("#liveToast .toast-body").text(
      isFavorite
        ? "محصول به علاقه‌مندی‌ها اضافه شد."
        : "محصول از علاقه‌مندی‌ها حذف شد.",
    );
    const toast = $("#searchToast").length
      ? $("#searchToast")
      : $("#liveToast");
    if (toast.is("#searchToast")) {
      toast.text(
        isFavorite
          ? "محصول به علاقه‌مندی‌ها اضافه شد."
          : "محصول از علاقه‌مندی‌ها حذف شد.",
      );
      toast.addClass("show");
      window.setTimeout(() => toast.removeClass("show"), 1800);
    } else bootstrap.Toast.getOrCreateInstance(toast[0]).show();
  });

  const $productColumns = $(".product-col");
  const filterProducts = (filter) => {
    $productColumns.each(function () {
      const $item = $(this);
      $item.toggleClass(
        "d-none",
        filter !== "all" && !$item.data("category").includes(filter),
      );
    });
  };

  $("#productTabs button").on("click", function () {
    const $button = $(this);
    $("#productTabs button.active").removeClass("active");
    $button.addClass("active");
    filterProducts($button.data("filter"));
  });
  filterProducts("all");

  initOwlCarousel($("#testimonialSlider"), {
    ...carouselMotion,
    rtl: true,
    loop: true,
    margin: 18,
    nav: true,
    dots: true,
    autoplay: !prefersReducedMotion,
    autoplayTimeout: 5000,
    autoplayHoverPause: true,
    lazyLoad: true,
    navText: [
      '<i class="bi bi-arrow-right"></i>',
      '<i class="bi bi-arrow-left"></i>',
    ],
    responsive: {
      0: { items: 1 },
      768: { items: 2 },
      992: { items: 3 },
    },
  });

  $(".owl-carousel").each(function () {
    const $slider = $(this);
    $slider.children(".owl-nav").slice(1).remove();
    $slider.find(".owl-nav .owl-prev").slice(1).remove();
    $slider.find(".owl-nav .owl-next").slice(1).remove();
  });

  $document.on("click", 'a[href^="#"]', function (event) {
    const href = $(this).attr("href");
    if (!href || href === "#") return;
    const $target = $(href);
    if (!$target.length) return;
    event.preventDefault();
    if (prefersReducedMotion) {
      window.scrollTo(0, $target.offset().top);
      return;
    }
    $("html, body").stop().animate({ scrollTop: $target.offset().top }, 500);
  });

  $document.on("click", '.mega-menu a[href="#collection"]', () => {
    bootstrap.Dropdown.getOrCreateInstance(
      $(".mega-dropdown .dropdown-toggle")[0],
    ).hide();
  });

  $("#newsletterForm").on("submit", (event) => {
    event.preventDefault();
    $("#formMessage").text("عضویت شما با موفقیت ثبت شد.");
    $(event.currentTarget).trigger("reset");
  });

  // Rich product discovery: quick view, comparison, finder and tactile previews.
  const productFromElement = (element) => {
    const $root = $(element).closest(
      ".product-card, .showcase-card, .rail-card",
    );
    const $cartButton = $root.find(".add-cart").first();
    const name = String(
      $cartButton.data("name") ||
        $root.find("h3").first().text() ||
        "محصول منتخب",
    );
    const productId = String($cartButton.data("id") || "");
    const catalogProduct = (window.HNProducts || []).find(
      (product) => String(product.id) === productId,
    );
    return {
      id: productId || name,
      name,
      price: Number($cartButton.data("price")) || 0,
      image: String(
        $cartButton.data("image") ||
          $root.find("img").first().attr("src") ||
          "Shop/images/products/tool-hammer-drill.png",
      ),
      code: catalogProduct
        ? `HN-${String(catalogProduct.id).padStart(3, "0")}`
        : $root.find("small").first().text() || "کد محصول",
      brand: catalogProduct?.brand || "",
      type: catalogProduct?.type || "",
      movement: catalogProduct?.movement || "",
      strap: catalogProduct?.strap || "",
      detail: catalogProduct?.detail || null,
    };
  };
  let activeProduct = null;
  const quickViewModal = bootstrap.Modal.getOrCreateInstance(
    $("#quickViewModal")[0],
  );
  $('.quick[href="#"]').each(function () {
    $(this).replaceWith(
      `<button type="button" class="quick">${$(this).html()}</button>`,
    );
  });

  const decorateProductCard = (card) => {
    const $card = $(card);
    if ($card.attr("data-action-dock-ready") === "true") return;
    const $image = $card.find(".product-image, .rail-image").first();
    const $cartButton = $card.find(".add-cart").first();
    if (!$image.length || !$cartButton.length) return;
    const productId = String($cartButton.data("id") || "");
    if (!$image.children(".favorite").length) {
      $image.prepend(
        `<button class="favorite" type="button" data-product-id="${escapeHtml(productId)}" aria-label="افزودن به علاقه‌مندی‌ها" aria-pressed="false" title="علاقه‌مندی‌ها"><i class="bi bi-heart" aria-hidden="true"></i></button>`,
      );
    }
    $image.children(".favorite").attr("data-product-id", productId);
    if (productId && !$image.find(".compare-trigger[data-product-id]").length) {
      $image.append(
        `<button type="button" class="compare-trigger" data-product-id="${escapeHtml(productId)}" aria-label="افزودن به مقایسه" aria-pressed="false" title="افزودن به مقایسه"><i class="bi bi-arrow-left-right" aria-hidden="true"></i></button>`,
      );
    }
    let $actions = $image.children(".product-card-actions");
    if (!$actions.length)
      $actions = $(
        '<div class="product-card-actions" role="group" aria-label="عملیات محصول"></div>',
      ).appendTo($image);
    $image.children(".favorite, .compare-trigger").appendTo($actions);
    $cartButton.attr({
      "aria-label": "افزودن به سبد خرید",
      title: "افزودن به سبد خرید",
    });
    $card.attr("data-action-dock-ready", "true");
    syncCompareButtons();
    syncFavoriteButtons();
    if (!$image.find(".quick").length)
      $image.append(
        '<button type="button" class="quick">مشاهده سریع <i class="bi bi-arrow-left"></i></button>',
      );
  };
  const decorateProductCards = (root = document) => {
    if (root.matches?.(".product-card, .showcase-card, .rail-card"))
      decorateProductCard(root);
    root
      .querySelectorAll?.(".product-card, .showcase-card, .rail-card")
      .forEach(decorateProductCard);
  };
  const detailCardSelector =
    ".product-card, .showcase-card, .rail-card, .featured-main, .featured-side, .favorite-item-card, .compare-product-card";
  const detailHrefForCard = (card) => {
    const productLink = [...card.querySelectorAll("a[href]")].find((link) => {
      const url = new URL(link.href, window.location.href);
      return /\/Product\/?$/i.test(url.pathname) && url.searchParams.has("id");
    });
    if (productLink) return productLink.href;
    const cartButton = card.querySelector(".add-cart[data-id]");
    const productId = String(cartButton?.dataset.id || "");
    const productName = String(cartButton?.dataset.name || "");
    const product = (window.HNProducts || []).find(
      (item) =>
        String(item.id) === productId ||
        (productName && item.name === productName),
    );
    return product ? `Product?id=${encodeURIComponent(product.id)}` : "";
  };
  const markDetailCards = (root = document) => {
    const cards = [];
    if (root.matches?.(detailCardSelector)) cards.push(root);
    cards.push(...(root.querySelectorAll?.(detailCardSelector) || []));
    cards.forEach((card) => {
      const href = detailHrefForCard(card);
      card.classList.toggle("product-card-clickable", Boolean(href));
      if (href) card.dataset.productDetailUrl = href;
      else delete card.dataset.productDetailUrl;
    });
  };
  decorateProductCards();
  markDetailCards();
  const productCardObserver = new MutationObserver((records) => {
    records.forEach((record) =>
      record.addedNodes.forEach((node) => {
        if (node instanceof Element) {
          decorateProductCards(node);
          markDetailCards(node);
          const parentCard = node.closest(detailCardSelector);
          if (parentCard) markDetailCards(parentCard);
        }
      }),
    );
  });
  productCardObserver.observe(document.body, {
    childList: true,
    subtree: true,
  });
  document.addEventListener("click", (event) => {
    if (
      event.button !== 0 ||
      event.metaKey ||
      event.ctrlKey ||
      event.shiftKey ||
      event.altKey
    )
      return;
    if (
      event.target.closest(
        "a, button, input, select, textarea, summary, [role='button']",
      )
    )
      return;
    if (String(window.getSelection?.() || "").trim()) return;
    const card = event.target.closest(detailCardSelector);
    const href = card?.dataset.productDetailUrl;
    if (href) window.location.assign(href);
  });
  syncCompareButtons();

  $document.on("click", ".compare-trigger[data-product-id]", function (event) {
    event.preventDefault();
    event.stopPropagation();
    const productId = String(this.dataset.productId || "");
    if (!productId) return;
    cacheProductFromCard(
      this.closest(".product-card, .showcase-card, .rail-card"),
      productId,
    );
    const selected = readCompareList();
    const next = selected.includes(productId)
      ? selected.filter((id) => id !== productId)
      : [...selected, productId];
    window.HNUI.storage.set("hn-compare", JSON.stringify(next));
    syncCompareButtons();
    if (next.includes(productId)) {
      pulseListControl(document.querySelector("#compareToggle"));
      flyProductToControl(this, null, document.querySelector("#compareToggle"));
    }
    setCartOpen(false);
  });

  const openQuickView = (product) => {
    activeProduct = product;
    $("#quickViewImage").attr({
      src: product.image,
      alt: product.name,
    });
    $("#quickViewTitle").text(product.name);
    $("#quickViewCode").text(
      `${product.brand ? `${product.brand} · ` : ""}${product.code}`,
    );
    $("#quickViewPrice").text(`${faNumber(product.price)} تومان`);
    $(".quick-description").text(
      product.detail?.intro || "مشخصات این مدل را پیش از انتخاب بررسی کنید.",
    );
    const quickSpecs = [
      ["گروه محصول", product.type],
      ["نوع تغذیه", product.movement],
      ["محیط کاربرد", product.strap],
    ].filter(([, value]) => value);
    $("#quickViewSpecs").html(
      quickSpecs
        .map(
          ([label, value]) =>
            `<div><i class="bi bi-info-circle" aria-hidden="true"></i><span>${escapeHtml(label)}</span><b>${escapeHtml(value)}</b></div>`,
        )
        .join(""),
    );
    $("#interactiveViewer").removeClass("auto-spin zoomed").css({
      "--rx": "0deg",
      "--ry": "0deg",
      "--scale": "1",
      "--shine": "-55%",
    });
    quickViewModal.show();
  };
  $document.on("click", ".quick", function (event) {
    event.preventDefault();
    openQuickView(productFromElement(this));
  });
  $("#interactiveViewer")
    .on("pointermove", function (event) {
      if (prefersReducedMotion) return;
      const rect = this.getBoundingClientRect();
      const x = (event.clientX - rect.left) / rect.width;
      const y = (event.clientY - rect.top) / rect.height;
      $(this).css({
        "--ry": `${(x - 0.5) * 24}deg`,
        "--rx": `${(0.5 - y) * 18}deg`,
        "--shine": `${x * 120 - 60}%`,
      });
    })
    .on("pointerleave", function () {
      $(this).css({ "--rx": "0deg", "--ry": "0deg" });
    });
  $("#viewerRotate").on("click", () =>
    $("#interactiveViewer").toggleClass("auto-spin"),
  );
  $("#viewerZoom").on("click", function () {
    const zoomed = $("#interactiveViewer")
      .toggleClass("zoomed")
      .hasClass("zoomed");
    $("#interactiveViewer").css("--scale", zoomed ? "1.35" : "1");
    $(this)
      .find("i")
      .attr("class", zoomed ? "bi bi-zoom-out" : "bi bi-zoom-in");
  });

  $(".quick-add-cart").on("click", function () {
    if (!activeProduct) return;
    window.HNUI?.cart.add(activeProduct, 1, this);
  });

  $("#mobileMenu").on("click", "a[href]", function (event) {
    const href = this.getAttribute("href");
    if (!href || href === "#") return;
    event.preventDefault();
    const offcanvasElement = document.getElementById("mobileMenu");
    const offcanvas = window.bootstrap?.Offcanvas.getInstance(offcanvasElement);
    if (offcanvas) offcanvas.hide();
    window.setTimeout(
      () => window.location.assign(new URL(href, document.baseURI).href),
      120,
    );
  });
  document.addEventListener("hn:cart-added", (event) => {
    const { product, trigger } = event.detail || {};
    const target = $("#cartToggle")[0];
    if (!target) return;

    /* Start the same immediate receive pulse used by favorites and compare. */
    window.clearTimeout(target._cartReceivedTimer);
    target.classList.remove("cart-received");
    void target.offsetWidth;
    target.classList.add("cart-received");
    target._cartReceivedTimer = window.setTimeout(() => {
      target.classList.remove("cart-received");
      target._cartReceivedTimer = null;
    }, 2100);

    flyProductToControl(trigger, product, target);
  });
});
