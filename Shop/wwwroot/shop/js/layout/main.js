$(function () {
  const innerCategoryGroups = {
    tools: [
      ["دریل و پیچ‌گوشتی", "دریل چکشی", "دریل شارژی", "پیچ‌گوشتی برقی", "دریل بتن‌کن"],
      ["برش و سایش", "فرز و مینی‌فرز", "اره عمودبر", "اره گردبر", "صفحه برش"],
      ["تجهیزات کارگاهی", "دستگاه جوش", "کارواش", "کمپرسور باد", "لوازم جانبی ابزار"],
      ["مته و سوراخ‌کاری", "مته بتن", "مته فلز", "مته چوب", "مته الماسه"],
      ["صفحه و تیغه", "صفحه برش", "صفحه ساب", "تیغ اره عمودبر", "تیغ اره گردبر"],
      ["باتری و متعلقات", "باتری یدکی", "شارژر ابزار", "سه‌نظام", "ذغال دستگاه"],
    ],
    "hand-tools": [
      ["آچار و بکس", "آچار فرانسه", "ست بکس", "آچار آلن", "آچار تخت و رینگی", "آچار ترکمتر", "آچار لوله‌گیر"],
      ["پیچ‌گوشتی و انبر", "پیچ‌گوشتی دستی", "انبردست", "سیم‌چین", "دم‌باریک", "ست پیچ‌گوشتی", "انبرقفلی"],
      ["لوازم ابزار", "جعبه ابزار", "مته و سری", "گیره و پیچ‌دستی", "کاتر و تیغ", "چکش و ابزار ضربه‌ای", "متر و خط‌کش"],
    ],
    components: [
      ["مقاومت و خازن", "مقاومت", "خازن", "دیود", "سلف", "پل دیود"],
      ["آی‌سی و نیمه‌رسانا", "آی‌سی", "ترانزیستور", "رگولاتور", "ماسفت", "اپتوکوپلر"],
      ["برد و ماژول", "بردبورد", "ماژول آماده", "سنسور", "برد آردوینو", "ماژول تغذیه"],
      ["سنسور و ماژول", "سنسور دما", "سنسور رطوبت", "سنسور فاصله", "ماژول رله", "ماژول ارتباطی"],
      ["تجهیزات لحیم‌کاری", "هویه", "نوک هویه", "سیم لحیم", "قلع‌کش", "هیتر و هویه"],
      ["بردهای توسعه", "برد آردوینو", "برد ESP32", "برد رزبری پای", "برد سوراخ‌دار", "جامپر و سیم رابط"],
    ],
    cables: [
      ["سیم و کابل", "سیم افشان", "کابل برق", "کابل شبکه", "کابل شیلددار", "سیم مفتولی", "کابل ابزار دقیق"],
      ["سوکت و کانکتور", "فیش برق", "سوکت شبکه", "کانکتور", "فیش BNC", "کانکتور مخابراتی", "فیش موزی"],
      ["اتصالات نصب", "ترمینال", "سرسیم", "داکت و بست", "وارنیش حرارتی", "گلند کابل", "بست کمربندی"],
    ],
    lighting: [
      ["لامپ و LED", "لامپ LED", "پنل سقفی", "ریسه نواری", "لامپ فیلامنتی", "چراغ خطی"],
      ["چراغ کارگاهی", "چراغ بازرسی", "چراغ اضطراری", "پروژکتور", "چراغ سیار", "چراغ ضدانفجار"],
      ["لوازم روشنایی", "سرپیچ", "درایور LED", "سنسور حرکتی", "فتوسل", "قاب و متعلقات چراغ"],
    ],
    switchgear: [
      ["کلید و پریز", "کلید برق", "پریز توکار", "پریز صنعتی", "کلید تبدیل", "پریز ارت‌دار"],
      ["فیوز و محافظ", "فیوز مینیاتوری", "محافظ جان", "محافظ ولتاژ", "فیوز کاردی", "کلید اتوماتیک"],
      ["تجهیزات کنترل", "کنتاکتور", "تایمر", "شستی و سیگنال", "رله حرارتی", "کنترل فاز"],
    ],
    safety: [
      ["ایمنی فردی", "دستکش عایق", "عینک ایمنی", "کفش کار", "کلاه ایمنی", "لباس کار"],
      ["حفاظت برق", "محافظ ولتاژ", "تجهیزات ارت", "صاعقه‌گیر", "دستگاه تست عایق", "تجهیزات قفل و برچسب"],
      ["ایمنی کارگاه", "کلاه ایمنی", "لباس کار", "جعبه کمک‌های اولیه", "گوشی ایمنی", "ماسک تنفسی"],
    ],
    measurement: [
      ["ابزار اندازه‌گیری", "مولتی‌متر", "آمپرمتر", "ولت‌متر", "فرکانس‌متر", "LCR متر"],
      ["تست و عیب‌یابی", "فازمتر", "تستر کابل", "تستر باتری", "تستر عایق", "ردیاب سیم"],
      ["ابزار دقیق", "کلمپ‌متر", "اسیلوسکوپ", "منبع تغذیه", "دماسنج لیزری", "لوکس‌متر"],
      ["دما و رطوبت", "دماسنج دیجیتال", "دماسنج لیزری", "رطوبت‌سنج", "دماسنج محیطی", "ترموکوپل"],
      ["اندازه‌گیری محیط", "لوکس‌متر", "صوت‌سنج", "فشارسنج", "سرعت‌سنج", "کیفیت‌سنج هوا"],
      ["منابع تست و کالیبراسیون", "منبع تغذیه آزمایشگاهی", "مولد سیگنال", "کالیبراتور", "بار الکترونیکی", "پراب و کابل تست"],
    ],
  };

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
  const cartOptionsMarkup = (item) => window.HNUI.cart.renderOptions(item);
  const renderCategoryGroups = (host, groups, categoryName) => {
    const heading = document.createElement("div");
    heading.className = "electrical-submenu-heading";
    const copy = document.createElement("span");
    copy.className = "electrical-submenu-heading-copy";
    const title = document.createElement("strong");
    title.textContent = `زیرشاخه‌های ${categoryName || "محصولات"}`;
    const caption = document.createElement("small");
    caption.textContent = "برای دیدن کالاها، یک زیرشاخه را انتخاب کنید";
    copy.append(title, caption);
    const count = document.createElement("span");
    count.className = "electrical-submenu-heading-count";
    count.textContent = `${groups.length.toLocaleString("fa-IR")} گروه`;
    heading.append(copy, count);

    const cards = groups.map(([groupName, ...items], index) => {
      const card = document.createElement("section");
      card.className = "electrical-submenu-column category-mega-group";
      card.dataset.index = String(index);
      const groupLink = document.createElement("a");
      groupLink.className = "category-mega-title";
      groupLink.href = `Search?q=${encodeURIComponent(groupName)}`;
      groupLink.textContent = groupName;
      card.append(groupLink);
      items.forEach((label) => {
        const link = document.createElement("a");
        link.href = `Search?q=${encodeURIComponent(label)}`;
        link.textContent = label;
        card.append(link);
      });
      return card;
    });
    host.replaceChildren(heading, ...cards);
  };
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
    let suggestionsCloseTimer = 0;
    const closeSuggestions = () => {
      window.clearTimeout(suggestionsCloseTimer);
      suggestions.hidden = true;
      searchInput.setAttribute("aria-expanded", "false");
      if (!isDrawerSearch)
        document.body.classList.remove("has-search-suggestions");
    };
    const openSuggestions = () => {
      window.clearTimeout(suggestionsCloseTimer);
      suggestions.hidden = false;
      searchInput.setAttribute("aria-expanded", "true");
      if (!isDrawerSearch)
        document.body.classList.add("has-search-suggestions");
    };
    const closeSuggestionsAfterPointerLeave = () => {
      window.clearTimeout(suggestionsCloseTimer);
      suggestionsCloseTimer = window.setTimeout(() => {
        const pointerStillInSearch = headerSearchForm.matches(":hover");
        const keyboardFocusInSuggestions = suggestions.contains(
          document.activeElement,
        );
        if (!pointerStillInSearch && !keyboardFocusInSuggestions)
          closeSuggestions();
      }, 500);
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
                `<li><a class="header-search-suggestion" role="option" href="${window.HNProductUrl(product)}"><span class="header-search-suggestion-image"><img src="${escapeHtml(product.image)}" alt="" loading="lazy" decoding="async"></span><span class="header-search-suggestion-copy"><strong>${escapeHtml(product.name)}</strong><small>${escapeHtml(product.brand)} <bdi>· ${escapeHtml(product.type)}</bdi></small><b>${faNumber(product.price * 1000000)} <small>تومان</small></b></span><i class="bi bi-arrow-left" aria-hidden="true"></i></a></li>`,
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
    headerSearchForm.addEventListener("submit", (event) => {
      if (isDrawerSearch && !searchInput.value.trim()) {
        event.preventDefault();
        searchInput.focus();
        return;
      }
      rememberSearch(searchInput.value);
    });
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
    if (!isDrawerSearch) {
      headerSearchForm.addEventListener("pointerleave", () => {
        if (window.matchMedia("(min-width: 992px)").matches)
          closeSuggestionsAfterPointerLeave();
      });
      headerSearchForm.addEventListener("pointerenter", () =>
        window.clearTimeout(suggestionsCloseTimer),
      );
      suggestions.addEventListener("pointerenter", () =>
        window.clearTimeout(suggestionsCloseTimer),
      );
      suggestions.addEventListener(
        "pointerleave",
        closeSuggestionsAfterPointerLeave,
      );
    }
    if (!isDrawerSearch && window.matchMedia("(min-width: 992px)").matches) {
      document
        .querySelectorAll("#mainNav .brand-dropdown")
        .forEach((brandDropdown) => {
          let brandCloseTimer = 0;
          const brandTrigger = brandDropdown.querySelector(":scope > .nav-link");
          const brandMenu = brandDropdown.querySelector(":scope > .brand-menu");
          const isBrandControlActive = () =>
            Boolean(
              brandTrigger?.matches(":hover") ||
                brandMenu?.querySelector("a:hover") ||
                brandDropdown.contains(document.activeElement),
            );
          const keepBrandOpen = () => {
            window.clearTimeout(brandCloseTimer);
            brandDropdown.classList.remove("is-brand-switching");
            brandDropdown.classList.add("is-brand-open");
            brandTrigger?.setAttribute("aria-expanded", "true");
          };
          const closeBrandSoon = () => {
            window.clearTimeout(brandCloseTimer);
            brandCloseTimer = window.setTimeout(() => {
              if (isBrandControlActive()) return;
              brandDropdown.classList.remove("is-brand-open");
              if (!brandDropdown.classList.contains("show")) {
                brandTrigger?.setAttribute("aria-expanded", "false");
              }
            }, 500);
          };
          const dismissSearch = () => {
            if (suggestions.hidden) return;
            searchInput.blur();
            closeSuggestions();
          };
          brandTrigger?.addEventListener("pointerenter", () => {
            keepBrandOpen();
          });
          brandDropdown.addEventListener("pointerleave", closeBrandSoon);
          brandMenu?.addEventListener("pointerleave", closeBrandSoon);
          brandMenu?.addEventListener("pointerover", (event) => {
            if (event.target instanceof Element && event.target.closest("a")) {
              keepBrandOpen();
            } else {
              closeBrandSoon();
            }
          });
          brandDropdown.addEventListener("focusin", () => {
            keepBrandOpen();
            dismissSearch();
          });
          brandDropdown.addEventListener("focusout", (event) => {
            if (!brandDropdown.contains(event.relatedTarget)) closeBrandSoon();
          });
          brandDropdown.parentElement
            ?.querySelectorAll(":scope > .nav-item:not(.brand-dropdown)")
            .forEach((siblingItem) => {
              siblingItem.addEventListener("pointerenter", () => {
                window.clearTimeout(brandCloseTimer);
                brandDropdown.classList.remove("is-brand-open", "show");
                brandMenu?.classList.remove("show");
                brandDropdown.classList.add("is-brand-switching");
                brandTrigger?.setAttribute("aria-expanded", "false");
              });
            });
          brandDropdown.addEventListener("hidden.bs.dropdown", () => {
            window.clearTimeout(brandCloseTimer);
            brandDropdown.classList.remove("is-brand-open", "is-brand-switching");
            brandTrigger?.setAttribute("aria-expanded", "false");
          });
        });
      document
        .querySelectorAll("#mainNav .quick-links-dropdown")
        .forEach((quickLinksDropdown) => {
          let quickLinksCloseTimer = 0;
          const quickLinksTrigger = quickLinksDropdown.querySelector(":scope > .nav-link");
          const quickLinksMenu = quickLinksDropdown.querySelector(":scope > .quick-links-menu");
          const keepQuickLinksOpen = () => {
            window.clearTimeout(quickLinksCloseTimer);
            quickLinksDropdown.classList.add("is-quick-links-open");
            quickLinksTrigger?.setAttribute("aria-expanded", "true");
          };
          const closeQuickLinksSoon = () => {
            window.clearTimeout(quickLinksCloseTimer);
            quickLinksCloseTimer = window.setTimeout(() => {
              if (quickLinksDropdown.matches(":hover, :focus-within")) return;
              quickLinksDropdown.classList.remove("is-quick-links-open");
              if (!quickLinksDropdown.classList.contains("show")) {
                quickLinksTrigger?.setAttribute("aria-expanded", "false");
              }
            }, 220);
          };
          const dismissSearch = () => {
            if (suggestions.hidden) return;
            searchInput.blur();
            closeSuggestions();
          };

          quickLinksDropdown.addEventListener("pointerenter", () => {
            keepQuickLinksOpen();
          });
          quickLinksDropdown.addEventListener("pointerleave", closeQuickLinksSoon);
          quickLinksMenu?.addEventListener("pointerenter", keepQuickLinksOpen);
          quickLinksDropdown.addEventListener("focusin", () => {
            keepQuickLinksOpen();
            dismissSearch();
          });
          quickLinksDropdown.addEventListener("focusout", (event) => {
            if (!quickLinksDropdown.contains(event.relatedTarget)) closeQuickLinksSoon();
          });
          quickLinksDropdown.addEventListener("hidden.bs.dropdown", () => {
            window.clearTimeout(quickLinksCloseTimer);
            quickLinksDropdown.classList.remove("is-quick-links-open");
            quickLinksTrigger?.setAttribute("aria-expanded", "false");
          });
        });
    }
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
        "content/products/cordless-hammer-drill-hn-001-row-001.png",
      type: "مشخصات محصول",
      powerSource: "مطابق مشخصات کالا",
      useCase: "مطابق اطلاعات کالا",
      movement: "مطابق مشخصات کالا",
      strap: "مطابق اطلاعات کالا",
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
            image: "content/products/cordless-hammer-drill-hn-001-row-001.png",
          };
          return `<article class="compare-panel-item" data-favorite-id="${escapeHtml(id)}"><img src="${escapeHtml(product.image || "content/products/cordless-hammer-drill-hn-001-row-001.png")}" alt="" loading="lazy"><span><strong>${escapeHtml(product.name || "محصول منتخب")}</strong><small>${faNumber(Number(product.price) * (Number(product.price) < 1000000 ? 1000000 : 1))} تومان</small></span><button class="cart-remove" type="button" data-favorite-remove="${escapeHtml(id)}" aria-label="حذف ${escapeHtml(product.name || "محصول")} از علاقه‌مندی‌ها"><i class="bi bi-trash3" aria-hidden="true"></i></button></article>`;
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
          "content/products/cordless-hammer-drill-hn-001-row-001.png",
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
            image: "content/products/cordless-hammer-drill-hn-001-row-001.png",
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
    const categoryContent = innerCategoryGroups;

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
      const activeCategory = triggers.find(
        (trigger) => trigger.dataset.category === category,
      );
      categoryPanel.dataset.groupCount = String(groups.length);
      renderCategoryGroups(
        categoryPanel,
        groups,
        activeCategory?.querySelector("span")?.textContent.trim(),
      );
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

  document.querySelectorAll(".category-nav-mega").forEach((menu) => {
    const sidebar = menu.querySelector(".category-mega-sidebar");
    const content = menu.querySelector(".category-mega-panels");
    if (!sidebar || !content) return;
    const buttons = [...sidebar.querySelectorAll("[data-mega-category]")];
    buttons.forEach((button) => button.setAttribute("aria-controls", "categoryMegaPanels"));
    let activeGroups = [];
    const updateColumnCount = () => {
      if (!activeGroups.length) return;
      const gap = 12;
      const minimumCardWidth = 184;
      const rootStyles = window.getComputedStyle(document.documentElement);
      const configuredMaxWidth =
        Number.parseFloat(rootStyles.getPropertyValue("--container-wide-max")) || 1480;
      const maxMenuWidth = Math.max(320, Math.min(window.innerWidth - 36, configuredMaxWidth));
      const availableContentWidth = maxMenuWidth - 276 - 44;
      const availableColumns = Math.max(
        1,
        Math.floor((availableContentWidth + gap) / (minimumCardWidth + gap)),
      );
      const columns = Math.min(5, activeGroups.length, availableColumns);
      const idealMenuWidth = 276 + 44 + columns * minimumCardWidth + (columns - 1) * gap;
      const menuWidth = Math.min(maxMenuWidth, Math.max(Math.min(720, maxMenuWidth), idealMenuWidth));
      const rightInset = Math.max(18, (window.innerWidth - configuredMaxWidth) / 2);
      content.style.setProperty(
        "--category-mega-columns",
        String(columns),
      );
      menu.style.width = `${menuWidth}px`;
      menu.style.right = `${rightInset}px`;
      menu.style.left = "auto";
    };
    const reset = () => {
      buttons.forEach((button) => {
        button.classList.remove("active");
        button.setAttribute("aria-expanded", "false");
      });
      activeGroups = [];
      content.style.removeProperty("--category-mega-columns");
      menu.style.removeProperty("width");
      menu.style.removeProperty("right");
      menu.style.removeProperty("left");
      content.replaceChildren();
      const hint = document.createElement("p");
      hint.className = "category-mega-hint";
      hint.textContent = "برای دیدن زیرشاخه‌ها، یک گروه را انتخاب کنید.";
      content.append(hint);
    };
    const activate = (button) => {
      const groups = innerCategoryGroups[button.dataset.megaCategory];
      if (!groups) return;
      activeGroups = groups;
      buttons.forEach((item) => {
        const active = item === button;
        item.classList.toggle("active", active);
        item.setAttribute("aria-expanded", String(active));
      });
      content.dataset.groupCount = String(groups.length);
      const activeCategory = button.querySelector("span")?.textContent.trim();
      renderCategoryGroups(content, groups, activeCategory);
      window.requestAnimationFrame(updateColumnCount);
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
        }, 220);
      };
      dropdown.addEventListener("pointerenter", keepOpen);
      dropdown.addEventListener("pointerleave", closeSoon);
      dropdown.addEventListener("focusin", keepOpen);
      dropdown.addEventListener("focusout", (event) => {
        if (!dropdown.contains(event.relatedTarget)) closeSoon();
      });
      window.addEventListener("resize", updateColumnCount, { passive: true });
      dropdown.addEventListener("hidden.bs.dropdown", () => {
        window.clearTimeout(closeTimer);
        dropdown.classList.remove("is-mega-open");
        reset();
      });
    }
  });

  const storage = window.HNUI.storage;

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
  const catalogSaleMarkup = (product) => {
    const discount = Number(product?.discountPercent) || 0;
    const current = Math.round(Number(product?.price) * 1000000);
    if (!discount || discount >= 100 || !current) return "";
    const previous = Math.round(current / (1 - discount / 100));
    return `<strong class="product-price-current">${faNumber(current)} <small>تومان</small></strong><span class="product-price-deal"><del class="price-old">${faNumber(previous)} تومان</del><b class="product-price-discount">٪${faNumber(discount)}</b></span>`;
  };
  const decorateCatalogPriceCards = (root = document) => {
    root
      .querySelectorAll(".product-card, .showcase-card, .rail-card")
      .forEach((card) => {
        const id = card.querySelector(".add-cart[data-id]")?.dataset.id;
        const product = catalogProducts.find((item) => String(item.id) === id);
        if (!product) return;
        const saleMarkup = catalogSaleMarkup(product);
        const homeGridCard = card.closest("#productGrid");
        if (!saleMarkup && !homeGridCard) return;
        const current = Math.round(Number(product.price) * 1000000);
        if (!current) return;
        const regularMarkup =
          `<strong class="product-price-current">${faNumber(current)} ` +
          "<small>تومان</small></strong>";
        const price = card.querySelector(
          ".product-info > p, .showcase-card-body > strong, .rail-card > strong",
        );
        if (price) {
          if (price.tagName === "P") {
            price.classList.toggle("has-discount", Boolean(saleMarkup));
            price.classList.toggle("is-regular", !saleMarkup);
            price.innerHTML = saleMarkup || regularMarkup;
          } else if (saleMarkup) {
            price.outerHTML = `<div class="product-card-sale-price has-discount">${saleMarkup}</div>`;
          }
        }
      });
  };
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
  const amazingProducts = [
    ...catalogProducts.filter(
      (product) => product.discountPercent > 0 && product.stockQuantity > 0,
    ),
    ...catalogProducts.filter(
      (product) => product.stockQuantity > 0 && !product.discountPercent,
    ),
  ].slice(0, 12);
  const sliderImagePaths = [
    "shop/images/sliders/slide-power-tools.png",
    "shop/images/sliders/slide-measurement-tools.png",
    "shop/images/sliders/slide-soldering-tools.png",
    "shop/images/sliders/slide-workshop-lighting.png",
  ];
  const sliderImagePath = (product) =>
    sliderImagePaths[Number(product.imageSet)] || product.image;
  const amazingSection = document.querySelector("#amazing");
  const amazingSlider = amazingSection?.querySelector("#amazingSlider");
  if (amazingSlider && amazingProducts.length) {
    amazingSlider.innerHTML = amazingProducts
      .map(
        (product) => `<div class="showcase-slide">
          <article class="showcase-card">
            <div class="product-image">
              <a href="${window.HNProductUrl(product)}" aria-label="مشاهده ${escapeHtml(product.name)}">
                <img loading="lazy" decoding="async" src="${escapeHtml(sliderImagePath(product))}" alt="${escapeHtml(product.name)}">
              </a>
              <span class="badge-offer">${product.discountPercent ? `${faNumber(product.discountPercent)}٪ تخفیف` : "پیشنهاد ویژه"}</span>
            </div>
            <div class="showcase-card-body">
              <small>${escapeHtml(product.brand || "پیشنهاد فروشگاه")}</small>
              <h3><a class="showcase-product-detail" href="${window.HNProductUrl(product)}">${escapeHtml(product.name)}</a></h3>
              <strong>${faNumber(product.price * 1000000)} تومان</strong>
              <button type="button" class="add-cart" data-id="${product.id}" data-name="${escapeHtml(product.name)}" data-price="${product.price * 1000000}" data-image="${escapeHtml(product.image)}">
                <i class="bi bi-bag-plus" aria-hidden="true"></i> افزودن به سبد
              </button>
            </div>
          </article>
        </div>`,
      )
      .join("");
    decorateCatalogPriceCards(amazingSection);
    window.HNUI?.enhance(amazingSection);
  }
  const productCardMarkup = (product) => `
    <div class="showcase-slide">
      <article class="showcase-card">
        <div class="product-image">
          <a href="${window.HNProductUrl(product)}" aria-label="مشاهده ${escapeHtml(product.name)}">
            <img loading="lazy" decoding="async" src="${escapeHtml(sliderImagePath(product))}" alt="${escapeHtml(product.name)}">
          </a>
          ${product.badge ? `<span class="${product.badge === "جدید" ? "badge-new" : "badge-best"}">${escapeHtml(product.badge)}</span>` : ""}
        </div>
        <div class="showcase-card-body">
          <h3><a class="showcase-product-detail" href="${window.HNProductUrl(product)}">${escapeHtml(product.name)}</a></h3>
          <strong>${faNumber(product.price * 1000000)} تومان</strong>
          <button type="button" class="add-cart" data-id="${product.id}" data-name="${escapeHtml(product.name)}" data-price="${product.price * 1000000}" data-image="${escapeHtml(product.image)}">
            <i class="bi bi-bag-plus"></i> افزودن به سبد
          </button>
        </div>
      </article>
    </div>`;
  if (catalogProducts.length >= 30) {
    $("#bestsellersSlider").html(
      catalogProducts.slice(0, 24).map(productCardMarkup).join(""),
    );
    $("#newestSlider").html(
      catalogProducts.slice(20, 26).map(productCardMarkup).join(""),
    );
  }
  decorateCatalogPriceCards();

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
      0: { items: 2 },
      768: { items: 2 },
      1100: { items: 5 },
      1400: { items: 6 },
    },
  };

  const amazingSliderOptions = {
    ...showcaseSliderOptions,
    responsive: {
      0: { items: 2, margin: 2 },
      768: { items: 3, margin: 20 },
      992: { items: 4, margin: 20 },
      1200: { items: 5, margin: 20 },
      1440: { items: 6, margin: 20 },
    },
  };

  initOwlCarousel($("#amazingSlider"), amazingSliderOptions);

  const bestsellersSliderOptions = {
    ...showcaseSliderOptions,
    responsive: {
      0: { items: 2, margin: 2 },
      768: { items: 3, margin: 20 },
      992: { items: 4, margin: 20 },
      1200: { items: 5, margin: 20 },
      1440: { items: 6, margin: 20 },
    },
  };
  initOwlCarousel($("#bestsellersSlider"), bestsellersSliderOptions);
  const newestSliderOptions = {
    ...showcaseSliderOptions,
    responsive: {
      0: { items: 2, margin: 2 },
      768: { items: 3, margin: 20 },
      992: { items: 4, margin: 20 },
      1200: { items: 5, margin: 20 },
      1440: { items: 6, margin: 20 },
    },
  };
  initOwlCarousel($("#newestSlider"), newestSliderOptions);

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
    if (open && !userLoggedIn) return;
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
    if (open && !userLoggedIn) return;
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
    if (open && !userLoggedIn) return;
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
  const protectedRoots = new Set([
    "profile",
    "cart",
    "checkout",
    "compare",
    "favorites",
  ]);
  const protectedPath = protectedRoots.has(
    window.location.pathname.split("/").filter(Boolean)[0]?.toLowerCase(),
  );
  let pendingAuthenticatedAction = null;
  let resumeAuthenticatedAction = false;

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
  const showLoginModal = (resume) => {
    pendingAuthenticatedAction =
      typeof resume === "function" ? resume : null;
    setCartOpen(false);
    setFavoriteOpen(false);
    setCompareOpen(false);
    setAccountOpen(false);
    if (loginModal) loginModal.show();
  };
  document.addEventListener("hn:auth-required", (event) => {
    showLoginModal(event.detail?.resume);
  });
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
      showLoginModal();
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
      window.HNUI.setMessage(
        document.querySelector("#forgotPasswordError"),
        "ایمیل را با قالب صحیح وارد کنید.",
        "error",
      );
      return;
    }
    window.HNUI.setMessage(
      document.querySelector("#forgotPasswordError"),
      "ارسال ایمیل بازنشانی هنوز به سرویس ایمیل سایت متصل نشده است.",
      "warning",
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
  const completeDemoLogin = () => {
    userLoggedIn = true;
    resumeAuthenticatedAction = true;
    window.HNUI.auth.login();
    updateAccountUI();
    if (loginModal) loginModal.hide();
    else {
      const resume = pendingAuthenticatedAction;
      pendingAuthenticatedAction = null;
      resumeAuthenticatedAction = false;
      resume?.();
    }
  };

  $("#loginEmailForm").on("submit", (event) => {
    event.preventDefault();
    const email = $("#loginEmail").val().trim();
    const password = $("#loginPassword").val();
    if (!email || !$("#loginEmail")[0].validity.valid || !password) {
      window.HNUI.setMessage(
        $loginEmailError[0],
        "ایمیل معتبر و رمز عبور را وارد کنید.",
        "error",
      );
      return;
    }
    window.HNUI.setMessage($loginEmailError[0], "");
    completeDemoLogin();
  });

  $("#registerEmailForm").on("submit", (event) => {
    event.preventDefault();
    const emailInput = $("#registerEmail")[0];
    const email = emailInput.value.trim();
    const firstName = $("#registerFirstName").val().trim();
    const lastName = $("#registerLastName").val().trim();
    if (!firstName || !lastName || !emailInput.validity.valid) {
      window.HNUI.setMessage(
        $registerEmailError[0],
        "نام، نام خانوادگی و ایمیل معتبر را وارد کنید.",
        "error",
      );
      return;
    }
    window.HNUI.setMessage($registerEmailError[0], "");
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
      window.HNUI.setMessage(
        $emailOtpError[0],
        "کد شش‌رقمی را کامل وارد کنید.",
        "error",
      );
      return;
    }
    window.HNUI.setMessage($emailOtpError[0], "");
    completeDemoLogin();
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
    const resume = resumeAuthenticatedAction
      ? pendingAuthenticatedAction
      : null;
    pendingAuthenticatedAction = null;
    resumeAuthenticatedAction = false;
    if (resume) window.setTimeout(resume, 0);
    else if (protectedPath && !window.HNUI.auth.isLoggedIn())
      window.location.replace("/");
  });
  document.addEventListener("hn:auth-change", (event) => {
    userLoggedIn = Boolean(event.detail);
    updateAccountUI();
    if (!userLoggedIn) {
      setCartOpen(false);
      setFavoriteOpen(false);
      setCompareOpen(false);
      setAccountOpen(false);
      if (protectedPath) window.location.replace("/");
    }
  });
  $(".account-links>a:last-child").on("click", (event) => {
    event.preventDefault();
    userLoggedIn = false;
    window.HNUI.auth.logout();
    setAccountOpen(false);
    updateAccountUI();
  });
  if (protectedPath && !userLoggedIn)
    showLoginModal(() => window.location.reload());

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
          return `<div class="cart-item" data-cart-id="${safeId}"><img src="${safeImage}" alt="${safeName}" loading="lazy" decoding="async"><div class="cart-item-info"><strong>${safeName}</strong>${cartOptionsMarkup(item)}<div class="cart-item-actions"><span>${faNumber(item.price)} تومان</span><div class="cart-item-controls"><div class="cart-quantity"><button type="button" data-action="minus" aria-label="کاهش تعداد">−</button><b>${faNumber(item.quantity)}</b><button type="button" data-action="plus" aria-label="افزایش تعداد">+</button></div><button class="cart-remove" type="button" data-action="remove" aria-label="حذف محصول"><i class="bi bi-trash3"></i></button></div></div></div></div>`;
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
    if (!window.HNUI.auth.requireLogin(() => $cartToggle.trigger("click"))) {
      event.preventDefault();
      return;
    }
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
  $favoritesPanel.on("click", ".compare-panel-close", (event) => {
    event.preventDefault();
    event.stopPropagation();
    setFavoriteOpen(false);
    $favoritesToggle.trigger("focus");
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
    const favoritesToggle = $favoritesToggle[0];
    if (
      !window.HNUI.auth.requireLogin(() => favoritesToggle.click())
    )
      return;
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
  $comparePanel.on("click", ".compare-panel-close", (event) => {
    event.preventDefault();
    event.stopPropagation();
    setCompareOpen(false);
    $compareToggle.trigger("focus");
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
    const compareToggle = $compareToggle[0];
    if (!window.HNUI.auth.requireLogin(() => compareToggle.click()))
      return;
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
  $cartPanel.on("click", ".cart-panel-close", (event) => {
    event.preventDefault();
    setCartOpen(false);
    $cartToggle.trigger("focus");
  });
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

  const showListActionToast = (message, type = "success") =>
    window.HNUI.showToast(message, type);

  $document.on("click", ".favorite", function () {
    const $button = $(this);
    const button = this;
    if (!window.HNUI.auth.requireLogin(() => button.click())) return;
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
    showListActionToast(
      isFavorite
        ? "محصول به علاقه‌مندی‌ها اضافه شد."
        : "محصول از علاقه‌مندی‌ها حذف شد.",
    );
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
    window.HNUI.setMessage(
      document.querySelector("#formMessage"),
      "ثبت‌نام خبرنامه هنوز فعال نیست؛ اطلاعاتی ارسال یا ذخیره نشد.",
      "warning",
    );
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
          "content/products/cordless-hammer-drill-hn-001-row-001.png",
      ),
      code: catalogProduct
        ? `HN-${String(catalogProduct.id).padStart(3, "0")}`
        : $root.find("small").first().text() || "کد محصول",
      brand: catalogProduct?.brand || "",
      type: catalogProduct?.type || "",
      powerSource: catalogProduct?.powerSource || catalogProduct?.movement || "",
      useCase: catalogProduct?.useCase || catalogProduct?.strap || "",
      movement: catalogProduct?.powerSource || catalogProduct?.movement || "",
      strap: catalogProduct?.useCase || catalogProduct?.strap || "",
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
    const cartIconClass = $cartButton.find("> i").attr("class") || "bi bi-bag-plus";
    $cartButton.html(
      `<i class="${escapeHtml(cartIconClass)}" aria-hidden="true"></i><span class="add-cart-label">افزودن به سبد</span>`,
    );
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
    ".product-card, .showcase-card, .rail-card, .featured-main, .featured-side, .favorite-item-card";
  const normalizeProductLinks = (root) => {
    const links = [];
    const pageTargets = [];
    if (root.matches?.("a[href], form[action]")) pageTargets.push(root);
    pageTargets.push(...(root.querySelectorAll?.("a[href], form[action]") || []));
    pageTargets.forEach((target) => {
      const attribute = target.matches("form") ? "action" : "href";
      const value = target.getAttribute(attribute) || "";
      if (!value || value.startsWith("#") || value.startsWith("?")) return;
      const legacyProductId = value.match(/^\/?Product\?id=(\d+)(?:&|$)/i)?.[1];
      if (legacyProductId) {
        const productUrl = window.HNProductUrl?.(legacyProductId);
        if (productUrl) target.setAttribute(attribute, productUrl);
        return;
      }
      const isRootRelative = value.startsWith("/");
      const firstSegment = value.split(/[/?#]/, 1)[0];
      if (!isRootRelative && !/^[A-Z]/.test(firstSegment)) return;
      const url = new URL(value, window.location.origin);
      if (url.origin !== window.location.origin) return;
      if (/\.[a-z\d]{1,8}$/i.test(url.pathname)) return;
      target.setAttribute(
        attribute,
        `${url.pathname.toLowerCase()}${url.search}${url.hash}`,
      );
    });
    const productLinks = [];
    if (root.matches?.("a[href]")) productLinks.push(root);
    productLinks.push(...(root.querySelectorAll?.("a[href]") || []));
    productLinks.forEach((link) => {
      const href = link.getAttribute("href") || "";
      const productId =
        href.match(/^\/?Product\?id=(\d+)(?:&|$)/i)?.[1] ||
        href.match(/^\/?products\/(\d+)\/?$/i)?.[1];
      const productUrl = productId && window.HNProductUrl?.(productId);
      if (productUrl) link.setAttribute("href", productUrl);
    });
  };
  normalizeProductLinks(document);
  document.addEventListener(
    "click",
    (event) => {
      const link = event.target.closest?.("a[href]");
      if (link) normalizeProductLinks(link);
    },
    true,
  );
  document.addEventListener("submit", (event) => {
    if (event.target instanceof HTMLFormElement)
      normalizeProductLinks(event.target);
  }, true);
  const detailHrefForCard = (card) => {
    const productLink = [...card.querySelectorAll("a[href]")].find((link) => {
      const url = new URL(link.href, window.location.href);
      return /\/products\/[a-z0-9-]+-\d+\/?$/i.test(url.pathname) || /\/products\/\d+\/?$/i.test(url.pathname) || (/\/Product\/?$/i.test(url.pathname) && url.searchParams.has("id"));
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
    return product ? window.HNProductUrl?.(product) || "" : "";
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
          normalizeProductLinks(node);
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
    const button = this;
    if (!window.HNUI.auth.requireLogin(() => button.click())) return;
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
    const isCompared = next.includes(productId);
    if (isCompared) {
      pulseListControl(document.querySelector("#compareToggle"));
      flyProductToControl(this, null, document.querySelector("#compareToggle"));
    }
    showListActionToast(
      isCompared
        ? "محصول به فهرست مقایسه اضافه شد."
        : "محصول از فهرست مقایسه حذف شد.",
    );
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
      product.detail?.highlights?.[0] || product.detail?.intro || "مشخصات این مدل را بررسی کنید.",
    );
    const quickSpecs = [
      ["گروه محصول", product.type],
      ["منبع تغذیه", product.powerSource || product.movement],
      ["مورد استفاده", product.useCase || product.strap],
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
  document.addEventListener("hn:cart-stock-limit", (event) => {
    const { product, stock, limit, reason } = event.detail || {};
    const message = reason === "cart" && Number.isFinite(Number(limit))
      ? `حداکثر تعداد مجاز برای «${product?.name || "این کالا"}» ${faNumber(Number(limit))} عدد است.`
      : product?.name && Number.isFinite(Number(stock))
        ? `موجودی «${product.name}» فقط ${faNumber(Number(stock))} عدد است؛ تعداد سبد به سقف موجودی رسید.`
        : "تعداد درخواستی از سقف مجاز کالا بیشتر بود؛ تعداد سبد به‌روزرسانی شد.";
    showListActionToast(message, "warning");
  });
});
