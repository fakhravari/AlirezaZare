$(function () {
  window.HNUI.mount("site-layout", document, () => {
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
  const searchProducts = Array.isArray(window.HNProducts)
    ? window.HNProducts
    : [];
  const headerSearchListings = (() => {
    let salesPlanCatalog = {};
    try {
      salesPlanCatalog = JSON.parse(
        document.querySelector("#storeSalesPlans")?.textContent || "{}",
      );
    } catch {
      salesPlanCatalog = {};
    }

    const productsById = new Map(
      searchProducts.map((product) => [Number(product.id), product]),
    );
    const plansById = new Map(
      (Array.isArray(salesPlanCatalog.plans) ? salesPlanCatalog.plans : []).map(
        (plan) => [plan.id, plan],
      ),
    );
    const now = Date.now();
    const offers = (
      Array.isArray(salesPlanCatalog.offers) ? salesPlanCatalog.offers : []
    ).flatMap((offer) => {
      const product = productsById.get(Number(offer.productId));
      const plan = plansById.get(offer.salesPlanId);
      const priceAmount = Number(offer.priceAmount);
      const discountPercent = Number(offer.discountPercent) || 0;
      if (
        !product ||
        !plan ||
        plan.isDefault ||
        plan.isEnabled === false ||
        offer.isEnabled === false ||
        Number(offer.stockQuantity) <= 0 ||
        !Number.isFinite(priceAmount) ||
        discountPercent < 0 ||
        discountPercent >= 100
      )
        return [];

      const startsAt = plan.startsAtUtc ? Date.parse(plan.startsAtUtc) : NaN;
      const endsAt = plan.endsAtUtc ? Date.parse(plan.endsAtUtc) : NaN;
      if ((Number.isFinite(startsAt) && now < startsAt) || (Number.isFinite(endsAt) && now >= endsAt)) return [];
      return [{ product, plan, offer, priceAmount, discountPercent }];
    });
    const productsWithActiveOffers = new Set(
      offers.map(({ product }) => Number(product.id)),
    );
    const regularListings = searchProducts
      .filter((product) => !productsWithActiveOffers.has(Number(product.id)))
      .map((product) => ({ product, plan: null, offer: null }));

    return [...regularListings, ...offers];
  })();
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
          ["header-search-suggestion-details", "mobile-menu-search-suggestion-details"],
          ["header-search-plan-badge", "mobile-menu-search-plan-badge"],
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
    const normalizeSearchText = window.HNUI.normalizeSearchText;
    const recentSearchKey = "hn-header-recent-searches";
    let recentSearches = [];
    const popularSearches = [
      "Ø¯Ø±ÛŒÙ„ Ø´Ø§Ø±Ú˜ÛŒ",
      "Ù…ÙˆÙ„ØªÛŒâ€ŒÙ…ØªØ± Ø¯ÛŒØ¬ÛŒØªØ§Ù„",
      "Ø³Øª Ù…ØªÙ‡ Ùˆ Ø³Ø±ÛŒ",
      "Ø§ÛŒØ³ØªÚ¯Ø§Ù‡ Ù‡ÙˆÛŒÙ‡",
      "Ú†Ø±Ø§Øº Ú©Ø§Ø±Ú¯Ø§Ù‡ÛŒ",
    ];
    const readRecentSearches = () => recentSearches;
    const loadRecentSearches = async () => {
      if (!window.HNUI.auth.isLoggedIn()) { recentSearches = []; return; }
      try {
        const response = await window.HNUI.api.request("account/recent-searches");
        if (!response.ok) return;
        const result = await response.json();
        recentSearches = Array.isArray(result.items) ? result.items.filter((item) => typeof item === "string").slice(0, 6) : [];
      } catch { recentSearches = []; }
    };
    const rememberSearch = async (value) => {
      const query = String(value || "")
        .trim()
        .slice(0, 80);
      if (Array.from(query).length < 3 || !window.HNUI.auth.isLoggedIn()) return;
      try {
        const response = await window.HNUI.api.request("account/recent-searches", {
          method: "POST", headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ query }),
        });
        if (response.ok) await loadRecentSearches();
      } catch { /* Search itself remains available when history cannot be saved. */ }
    };
    const quickSearchLink = (query, icon) =>
      `<a class="header-search-quick-chip" data-search-query="${escapeHtml(query)}" href="${searchUrl(query)}"><i class="bi ${icon}" aria-hidden="true"></i><span>${escapeHtml(query)}</span></a>`;
    const renderQuickSearchPanel = () => {
      const recent = readRecentSearches();
      suggestions.innerHTML = `<section class="header-search-quick-section" aria-labelledby="recentSearchesTitle"><div class="header-search-quick-heading"><strong id="recentSearchesTitle"><i class="bi bi-clock-history" aria-hidden="true"></i> Ø¬Ø³ØªØ¬ÙˆÙ‡Ø§ÛŒ Ø§Ø®ÛŒØ±</strong>${recent.length ? '<button type="button" data-clear-search-history>Ù¾Ø§Ú©â€ŒÚ©Ø±Ø¯Ù†</button>' : ""}</div>${recent.length ? `<div class="header-search-quick-chips">${recent.map((query) => quickSearchLink(query, "bi-clock-history")).join("")}</div>` : '<p class="header-search-quick-empty">Ø¬Ø³ØªØ¬ÙˆÙ‡Ø§ÛŒ Ø§Ø®ÛŒØ± Ø§ÛŒÙ†Ø¬Ø§ Ù†Ù…Ø§ÛŒØ´ Ø¯Ø§Ø¯Ù‡ Ù…ÛŒâ€ŒØ´ÙˆÙ†Ø¯.</p>'}</section><section class="header-search-quick-section" aria-labelledby="popularSearchesTitle"><div class="header-search-quick-heading"><strong id="popularSearchesTitle"><i class="bi bi-graph-up-arrow" aria-hidden="true"></i> Ø¬Ø³ØªØ¬ÙˆÙ‡Ø§ÛŒ Ù¾Ø±ØªÚ©Ø±Ø§Ø±</strong></div><div class="header-search-quick-chips">${popularSearches.map((query) => quickSearchLink(query, "bi-arrow-up-right")).join("")}</div></section>`;
      suggestions.innerHTML = isolateDrawerSearchMarkup(
        suggestions.innerHTML,
      );
      openSuggestions();
    };
    let suggestionsCloseTimer = 0;
    let suggestionsRenderTimer = 0;
    let planExpiryRenderTimer = 0;
    const closeSuggestions = () => {
      window.clearTimeout(suggestionsCloseTimer);
      window.clearInterval(planExpiryRenderTimer);
      suggestions.hidden = true;
      if (!isDrawerSearch)
        document.body.classList.remove("has-search-suggestions");
    };
    const openSuggestions = () => {
      window.clearTimeout(suggestionsCloseTimer);
      suggestions.hidden = false;
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
    const schedulePlanExpiryRefresh = () => {
      window.clearInterval(planExpiryRenderTimer);
      const now = Date.now();
      const expiries = headerSearchListings
        .map(({ plan }) => plan?.endsAtUtc ? Date.parse(plan.endsAtUtc) : NaN)
        .filter((end) => Number.isFinite(end) && end > now);
      if (!expiries.length) return;
      planExpiryRenderTimer = window.setInterval(() => {
        if (Math.min(...expiries) > Date.now()) return;
        window.clearInterval(planExpiryRenderTimer);
        planExpiryRenderTimer = 0;
        if (!suggestions.hidden && Array.from(normalizeSearchText(searchInput.value)).length >= 3)
          renderSuggestions();
      }, Math.min(1000, Math.max(50, Math.min(...expiries) - now)));
    };
    const renderSuggestions = () => {
      const query = normalizeSearchText(searchInput.value);
      if (Array.from(query).length < 3) {
        closeSuggestions();
        return;
      }

      const now = Date.now();
      const matches = headerSearchListings
        .filter(({ plan }) => {
          if (!plan) return true;
          const startsAt = plan.startsAtUtc ? Date.parse(plan.startsAtUtc) : NaN;
          const endsAt = plan.endsAtUtc ? Date.parse(plan.endsAtUtc) : NaN;
          return (!Number.isFinite(startsAt) || now >= startsAt) &&
            (!Number.isFinite(endsAt) || now < endsAt);
        })
        .map((listing) => {
          const { product, plan } = listing;
          const productName = String(product.name || "").replace(/\s+\d+$/, "");
          const fields = [
            productName,
            product.brand,
            product.type,
            String(product.englishName || "").replace(/\s+\d+$/, ""),
            plan?.name,
            plan?.label,
            plan?.code,
          ];
          const normalizedFields = fields.map(normalizeSearchText);
          const matchIndex = normalizedFields.findIndex((field) =>
            field.includes(query),
          );
          return { ...listing, matchIndex };
        })
        .filter(({ matchIndex }) => matchIndex >= 0)
        .sort(
          (a, b) =>
            a.matchIndex - b.matchIndex ||
            Number(Boolean(b.plan)) - Number(Boolean(a.plan)) ||
            (b.product.newest || 0) - (a.product.newest || 0),
        )
        .filter(
          (() => {
            const seenListings = new Set();
            return ({ product, plan }) => {
              const listingKey = `${Number(product.id)}:${plan?.id || "regular"}`;
              if (seenListings.has(listingKey)) return false;
              seenListings.add(listingKey);
              return true;
            };
          })(),
        )
        .slice(0, 5)
        .map(({ product, plan, priceAmount, discountPercent }) => ({
          product,
          plan,
          currentPrice: plan
            ? Math.round(priceAmount * (100 - discountPercent) / 100)
            : Math.round(Number(product.price) * 1000000),
          listPrice: plan && discountPercent > 0 ? priceAmount : null,
        }));

      const resultMarkup = matches.length
        ? `<div class="header-search-suggestions-head"><span><i class="bi bi-stars" aria-hidden="true"></i> Ù¾ÛŒØ´Ù†Ù‡Ø§Ø¯ Ø¨Ø±Ø§ÛŒ Ø¬Ø³ØªØ¬ÙˆÛŒ Ø´Ù…Ø§</span><small>${faNumber(matches.length)} Ù¾ÛŒØ´Ù†Ù‡Ø§Ø¯</small></div><ul>${matches
            .map(
              ({ product, plan, currentPrice, listPrice }) => {
                const planBadge = plan
                  ? `<span class="header-search-plan-badge" title="${escapeHtml(plan.label || plan.name)}"><i class="bi bi-lightning-charge-fill" aria-hidden="true"></i>Ø·Ø±Ø­ ${escapeHtml(plan.name || plan.code)}</span>`
                  : "";
                const priceMarkup = listPrice
                  ? `<b><span>${faNumber(currentPrice)} <small>ØªÙˆÙ…Ø§Ù†</small></span><del>${faNumber(listPrice)}</del></b>`
                  : `<b>${faNumber(currentPrice)} <small>ØªÙˆÙ…Ø§Ù†</small></b>`;
                const productUrl = window.HNProductUrl(
                  product,
                  plan ? { salesPlanId: plan.id } : undefined,
                );
                return `<li><a class="header-search-suggestion" role="option" href="${escapeHtml(productUrl)}"><span class="header-search-suggestion-image"><img src="${escapeHtml(product.image)}" alt="" loading="lazy" decoding="async"></span><span class="header-search-suggestion-copy"><strong>${escapeHtml(product.name)}</strong><small><span class="header-search-suggestion-details">${escapeHtml(product.brand)} <bdi>Â· ${escapeHtml(product.type)}</bdi></span>${planBadge}</small>${priceMarkup}</span><i class="bi bi-arrow-left" aria-hidden="true"></i></a></li>`;
              },
            )
            .join(
              "",
            )}</ul><a class="header-search-all" href="${searchUrl(searchInput.value.trim())}">Ù…Ø´Ø§Ù‡Ø¯Ù‡ Ù‡Ù…Ù‡ Ù†ØªØ§ÛŒØ¬ <i class="bi bi-arrow-left" aria-hidden="true"></i></a>`
        : `<div class="header-search-empty"><i class="bi bi-search" aria-hidden="true"></i><span>Ù…Ø­ØµÙˆÙ„ÛŒ Ø¨Ø§ Ø§ÛŒÙ† Ø¹Ø¨Ø§Ø±Øª Ù¾ÛŒØ¯Ø§ Ù†Ø´Ø¯.</span><a href="${searchUrl(searchInput.value.trim())}">Ø¬Ø³ØªØ¬Ùˆ Ø¯Ø± Ù‡Ù…Ù‡ Ù…Ø­ØµÙˆÙ„Ø§Øª <i class="bi bi-arrow-left" aria-hidden="true"></i></a></div>`;

      suggestions.innerHTML = resultMarkup;
      suggestions.innerHTML = isolateDrawerSearchMarkup(
        suggestions.innerHTML,
      );
      suggestions
        .querySelectorAll('[role="option"]')
        .forEach((option) => option.removeAttribute("role"));
      openSuggestions();
      schedulePlanExpiryRefresh();
    };

    searchInput.addEventListener("input", () => {
      window.clearTimeout(suggestionsRenderTimer);
      if (Array.from(normalizeSearchText(searchInput.value)).length < 3) {
        closeSuggestions();
        return;
      }
      suggestionsRenderTimer = window.setTimeout(renderSuggestions, 250);
    });
    headerSearchForm.addEventListener("submit", (event) => {
      if (Array.from(normalizeSearchText(searchInput.value)).length < 3) {
        event.preventDefault();
        if (isDrawerSearch) searchInput.focus();
        return;
      }
      void rememberSearch(searchInput.value);
    });
    suggestions.addEventListener("click", (event) => {
      const clearHistory = event.target.closest("[data-clear-search-history]");
      if (clearHistory) {
        if (window.HNUI.auth.isLoggedIn()) {
          void window.HNUI.api.request("account/recent-searches", { method: "DELETE" }).then((response) => {
            if (response.ok) recentSearches = [];
          });
        }
        renderQuickSearchPanel();
        return;
      }
      const link = event.target.closest("a");
      if (link) void rememberSearch(link.dataset.searchQuery || searchInput.value);
    });
    searchInput.addEventListener("focus", () => {
      window.clearTimeout(suggestionsRenderTimer);
      if (Array.from(normalizeSearchText(searchInput.value)).length >= 3)
        renderSuggestions();
      else void loadRecentSearches().then(renderQuickSearchPanel);
    });
    document.addEventListener("hn:auth-change", (event) => {
      recentSearches = [];
      if (event.detail) void loadRecentSearches().then(renderQuickSearchPanel);
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
        active ? "Ø­Ø°Ù Ø§Ø² Ø¹Ù„Ø§Ù‚Ù‡â€ŒÙ…Ù†Ø¯ÛŒâ€ŒÙ‡Ø§" : "Ø§ÙØ²ÙˆØ¯Ù† Ø¨Ù‡ Ø¹Ù„Ø§Ù‚Ù‡â€ŒÙ…Ù†Ø¯ÛŒâ€ŒÙ‡Ø§",
      );
      $(this)
        .find("i")
        .attr("class", active ? "bi bi-heart-fill" : "bi bi-heart");
    });
  };
  const cacheProductFromCard = (card, id) => Boolean(card && id);
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
        .attr("aria-label", active ? "Ø­Ø°Ù Ø§Ø² Ù…Ù‚Ø§ÛŒØ³Ù‡" : "Ø§ÙØ²ÙˆØ¯Ù† Ø¨Ù‡ Ù…Ù‚Ø§ÛŒØ³Ù‡")
        .attr("title", active ? "Ø­Ø°Ù Ø§Ø² Ù…Ù‚Ø§ÛŒØ³Ù‡" : "Ø§ÙØ²ÙˆØ¯Ù† Ø¨Ù‡ Ù…Ù‚Ø§ÛŒØ³Ù‡")
        .find("i")
        .attr("class", "bi bi-arrow-left-right");
    });
  };
  const renderNavCounters = () => {
    const favoriteCount = storedListCount("hn-favorites");
    const compareCount = storedListCount("hn-compare");
    $("#favoritesCount").text(faNumber(favoriteCount));
    $("#compareCount").text(faNumber(compareCount));
    $("#compareToggle").attr("aria-label", `Ù…Ù‚Ø§ÛŒØ³Ù‡ Ù…Ø­ØµÙˆÙ„Ø§Øª ${faNumber(compareCount)}`);
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
    $("#favoritesPanelCount").text(`${faNumber(ids.length)} Ù…Ø­ØµÙˆÙ„`);
    if (!ids.length) {
      $("#favoritesPanelItems").html(
        '<div class="compare-panel-empty header-panel-empty"><span class="header-panel-empty-icon"><i class="bi bi-heart" aria-hidden="true"></i></span><strong>Ù‡Ù†ÙˆØ² Ù…Ø­ØµÙˆÙ„ÛŒ Ø¯Ø± Ø¹Ù„Ø§Ù‚Ù‡â€ŒÙ…Ù†Ø¯ÛŒâ€ŒÙ‡Ø§ Ù†ÛŒØ³Øª</strong><p>Ø¨Ø±Ø§ÛŒ Ø°Ø®ÛŒØ±Ù‡ØŒ Ø¢ÛŒÚ©Ù† Ù‚Ù„Ø¨ Ø±ÙˆÛŒ Ú©Ø§Ø±Øª Ù…Ø­ØµÙˆÙ„ Ø±Ø§ Ø¨Ø²Ù†ÛŒØ¯.</p></div>',
      );
      return;
    }
    $("#favoritesPanelItems").html(
      ids
        .map((id) => {
          const product = products.get(id) || {
            name: "Ù…Ø­ØµÙˆÙ„ Ù…Ù†ØªØ®Ø¨",
            price: 0,
            image: "content/products/images/card/cordless-hammer-drill-hn-001-row-001-card-800.jpg",
          };
          const name = product.name || "Ù…Ø­ØµÙˆÙ„ Ù…Ù†ØªØ®Ø¨";
          const href = window.HNProductUrl?.(id) || "/search";
          return `<article class="compare-panel-item" data-favorite-id="${escapeHtml(id)}"><img src="${escapeHtml(product.image || "content/products/images/card/cordless-hammer-drill-hn-001-row-001-card-800.jpg")}" alt="" loading="lazy"><span><strong><a class="header-panel-product-link" href="${escapeHtml(href)}">${escapeHtml(name)}</a></strong><small>${faNumber(Number(product.price) * (Number(product.price) < 1000000 ? 1000000 : 1))} ØªÙˆÙ…Ø§Ù†</small></span><button class="cart-remove" type="button" data-favorite-remove="${escapeHtml(id)}" aria-label="Ø­Ø°Ù ${escapeHtml(name)} Ø§Ø² Ø¹Ù„Ø§Ù‚Ù‡â€ŒÙ…Ù†Ø¯ÛŒâ€ŒÙ‡Ø§"><i class="bi bi-trash3" aria-hidden="true"></i></button></article>`;
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
          "Ù…Ø­ØµÙˆÙ„ Ù…Ù†ØªØ®Ø¨",
        price: Number(button.dataset.price) || 0,
        image:
          button.dataset.image ||
          image?.getAttribute("src") ||
          "content/products/images/card/cordless-hammer-drill-hn-001-row-001-card-800.jpg",
      });
    });
    $("#comparePanelCount").text(`${faNumber(ids.length)} Ù…Ø­ØµÙˆÙ„`);
    if (!ids.length) {
      $("#comparePanelItems").html(
        '<div class="compare-panel-empty header-panel-empty"><span class="header-panel-empty-icon"><i class="bi bi-arrow-left-right" aria-hidden="true"></i></span><strong>Ù‡Ù†ÙˆØ² Ù…Ø­ØµÙˆÙ„ÛŒ Ø¨Ø±Ø§ÛŒ Ù…Ù‚Ø§ÛŒØ³Ù‡ Ù†ÛŒØ³Øª</strong><p>Ø¨Ø±Ø§ÛŒ Ù…Ù‚Ø§ÛŒØ³Ù‡ØŒ Ø¢ÛŒÚ©Ù† Ù…Ù‚Ø§ÛŒØ³Ù‡ Ø±ÙˆÛŒ Ú©Ø§Ø±Øª Ù…Ø­ØµÙˆÙ„ Ø±Ø§ Ø¨Ø²Ù†ÛŒØ¯.</p></div>',
      );
      return;
    }
    $("#comparePanelItems").html(
      ids
        .map((id) => {
          const product = products.get(id) || {
            name: "Ù…Ø­ØµÙˆÙ„ Ù…Ù†ØªØ®Ø¨",
            price: 0,
            image: "content/products/images/card/cordless-hammer-drill-hn-001-row-001-card-800.jpg",
          };
          const href = window.HNProductUrl?.(id) || "/search";
          return `<article class="compare-panel-item" data-compare-id="${escapeHtml(id)}"><img src="${escapeHtml(product.image)}" alt="" loading="lazy"><span><strong><a class="header-panel-product-link" href="${escapeHtml(href)}">${escapeHtml(product.name)}</a></strong><small>${faNumber(product.price)} ØªÙˆÙ…Ø§Ù†</small></span><button class="cart-remove" type="button" data-compare-remove="${escapeHtml(id)}" aria-label="Ø­Ø°Ù ${escapeHtml(product.name)} Ø§Ø² Ù…Ù‚Ø§ÛŒØ³Ù‡"><i class="bi bi-trash3" aria-hidden="true"></i></button></article>`;
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
    ".testimonial-intro",
    ".testimonial-card",
    ".story-image",
    ".story-copy",
    ".journal-main",
    ".journal-list",
    ".benefit",
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
    if (!$.fn.owlCarousel) return;
    const updateControlNames = (event) => {
      const carousel = event?.target || $element[0];
      carousel?.querySelectorAll(".owl-nav .owl-prev").forEach((button) => {
        button.setAttribute("role", "button");
        button.setAttribute("aria-label", "Ø§Ø³Ù„Ø§ÛŒØ¯ Ù‚Ø¨Ù„ÛŒ");
      });
      carousel?.querySelectorAll(".owl-nav .owl-next").forEach((button) => {
        button.setAttribute("role", "button");
        button.setAttribute("aria-label", "Ø§Ø³Ù„Ø§ÛŒØ¯ Ø¨Ø¹Ø¯ÛŒ");
      });
      carousel?.querySelectorAll(".owl-dots .owl-dot").forEach((button, index) => {
        button.setAttribute("role", "button");
        button.setAttribute("aria-label", `Ø±ÙØªÙ† Ø¨Ù‡ Ø§Ø³Ù„Ø§ÛŒØ¯ ${faNumber(index + 1)}`);
      });
    };
    const initialized = options.onInitialized;
    const refreshed = options.onRefreshed;
    $element.owlCarousel({
      ...options,
      onInitialized(event) {
        updateControlNames(event);
        initialized?.call(this, event);
      },
      onRefreshed(event) {
        updateControlNames(event);
        refreshed?.call(this, event);
      },
    });
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
  if (catalogProducts.length) {
    const featuredBindings = [
      { selector: ".featured-main", product: window.HNCatalog.find(1) },
      { selector: ".featured-side", product: window.HNCatalog.find(6) },
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
        .text(`${product.brand} Â· ${product.type}`);
      $card
        .find(".featured-price, .featured-buy strong")
        .text(`${faNumber(product.price * 1000000)} ØªÙˆÙ…Ø§Ù†`);
      $card.find(".add-cart").attr({
        "data-id": String(product.id),
        "data-name": product.name,
        "data-price": String(product.price * 1000000),
        "data-image": product.image,
      });
    });
  }
  const categoryFilters = {"Ø§Ø¨Ø²Ø§Ø± Ø¨Ø±Ù‚ÛŒ": "power-tools", "Ø§Ø¨Ø²Ø§Ø± Ø§Ù†Ø¯Ø§Ø²Ù‡â€ŒÚ¯ÛŒØ±ÛŒ": "measurement", "Ù„Ø­ÛŒÙ…â€ŒÚ©Ø§Ø±ÛŒ Ùˆ ØªØ¹Ù…ÛŒØ± Ø¨Ø±Ø¯": "electronics", "Ø±ÙˆØ´Ù†Ø§ÛŒÛŒ Ùˆ Ù„Ø§Ù…Ù¾": "lighting"};
  const grid = document.querySelector("#productGrid");
  if (grid) grid.innerHTML = (window.HNCatalog.collections.selected || []).map((id) => {
    const product = window.HNCatalog.find(id);
    return product ? '<div class="col-md-6 col-lg-3 col-xxl-2 product-col" data-category="' + (categoryFilters[product.categoryPath[1]] || "all") + '">' + window.HNProductCard.render(product) + '</div>' : "";
  }).join("");
  const amazingProducts = [
    ...catalogProducts.filter(
      (product) => product.discountPercent > 0 && product.stockQuantity > 0,
    ),
    ...catalogProducts.filter(
      (product) => product.stockQuantity > 0 && !product.discountPercent,
    ),
  ].slice(0, 6);
  const sliderImagePaths = [
    "shop/images/sliders/slide-power-tools.webp",
    "shop/images/sliders/slide-measurement-tools.webp",
    "shop/images/sliders/slide-soldering-tools.webp",
    "shop/images/sliders/slide-workshop-lighting.webp",
  ];
  const sliderImagePath = (product) =>
    sliderImagePaths[Number(product.imageSet)] || product.image;
  const amazingSection = document.querySelector("#amazing");
  const amazingSlider = amazingSection?.querySelector("#amazingSlider");
  if (amazingSlider && amazingProducts.length) {
    const amazingCount = amazingSection.querySelector("#amazingCount");
    if (amazingCount) amazingCount.textContent = faNumber(amazingProducts.length);
    amazingSlider.innerHTML = amazingProducts.map((product) => `<div class="showcase-slide">${window.HNProductCard.render(product, {
      variant: "grid", image: sliderImagePath(product), offer: true,
      badge: product.discountPercent ? "" : "Ù…Ù†ØªØ®Ø¨",
    })}</div>`).join("");
    window.HNUI?.enhance(amazingSection);
  }
  const homeCollectionCardMarkup = (product) => window.HNProductCard.render(product, {
    variant: "grid",
    image: sliderImagePath(product),
  });
  const collections = window.HNCatalog.collections;
  const collectionProducts = (name) => (collections[name] || []).map((id) => window.HNCatalog.find(id)).filter(Boolean);
  $("#bestsellersSlider").html(collectionProducts("bestsellers").map(homeCollectionCardMarkup).join(""));
  $("#newestSlider").html(collectionProducts("newest").map(homeCollectionCardMarkup).join(""));

  const showcaseSliderOptions = {
    ...carouselMotion,
    rtl: true,
    loop: true,
    rewind: false,
    center: false,
    autoWidth: false,
    stagePadding: 0,
    margin: 14,
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
      0: { items: 1, margin: 10 },
      576: { items: 2, margin: 12 },
      768: { items: 3, margin: 14 },
      992: { items: 4, margin: 12 },
      1200: { items: 5, margin: 12 },
      1440: { items: 6, margin: 12 },
    },
  };

  initOwlCarousel($("#amazingSlider"), amazingSliderOptions);

  const bestsellersSliderOptions = {
    ...showcaseSliderOptions,
    responsive: {
      0: { items: 2, margin: 2 },
      768: { items: 3, margin: 14 },
      992: { items: 4, margin: 14 },
      1200: { items: 5, margin: 14 },
      1440: { items: 6, margin: 14 },
    },
  };
  initOwlCarousel($("#bestsellersSlider"), bestsellersSliderOptions);
  const newestSliderOptions = {
    ...showcaseSliderOptions,
    responsive: {
      0: { items: 2, margin: 2 },
      768: { items: 3, margin: 14 },
      992: { items: 4, margin: 14 },
      1200: { items: 5, margin: 14 },
      1440: { items: 6, margin: 14 },
    },
  };
  initOwlCarousel($("#newestSlider"), newestSliderOptions);

  $(".showcase-card, .rail-card").each(function () {
    const $card = $(this);
    const $image = $card.find(".product-image, .rail-image").first();
    if (!$image.find(".favorite").length) {
      $image.prepend(
        '<button class="favorite" type="button" aria-label="Ø§ÙØ²ÙˆØ¯Ù† Ø¨Ù‡ Ø¹Ù„Ø§Ù‚Ù‡â€ŒÙ…Ù†Ø¯ÛŒ"><i class="bi bi-heart"></i></button>',
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
  let userLoggedIn = false;
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
      .attr("aria-label", userLoggedIn ? "Ø­Ø³Ø§Ø¨ Ù…Ù†" : "ÙˆØ±ÙˆØ¯ / Ø«Ø¨Øªâ€ŒÙ†Ø§Ù… Ø¯Ø± Ø­Ø³Ø§Ø¨ Ú©Ø§Ø±Ø¨Ø±ÛŒ")
      .html(
        userLoggedIn
          ? '<i class="bi bi-person" aria-hidden="true"></i><span class="account-label">Ø­Ø³Ø§Ø¨ Ù…Ù†</span><i class="bi bi-chevron-down" aria-hidden="true"></i>'
          : '<i class="bi bi-person" aria-hidden="true"></i><span class="account-label">ÙˆØ±ÙˆØ¯ / Ø«Ø¨Øªâ€ŒÙ†Ø§Ù…</span><i class="bi bi-chevron-down" aria-hidden="true"></i>',
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
  let pendingRegistrationProfile = null;
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
        "Ø§ÛŒÙ…ÛŒÙ„ Ø±Ø§ Ø¨Ø§ Ù‚Ø§Ù„Ø¨ ØµØ­ÛŒØ­ ÙˆØ§Ø±Ø¯ Ú©Ù†ÛŒØ¯.",
        "error",
      );
      return;
    }
    window.HNUI.setMessage(
      document.querySelector("#forgotPasswordError"),
      "Ø§Ø±Ø³Ø§Ù„ Ø§ÛŒÙ…ÛŒÙ„ Ø¨Ø§Ø²Ù†Ø´Ø§Ù†ÛŒ Ù‡Ù†ÙˆØ² Ø¨Ù‡ Ø³Ø±ÙˆÛŒØ³ Ø§ÛŒÙ…ÛŒÙ„ Ø³Ø§ÛŒØª Ù…ØªØµÙ„ Ù†Ø´Ø¯Ù‡ Ø§Ø³Øª.",
      "warning",
    );
  });

  const requestEmailCode = async (mode, email) => {
    const $submit = $(mode === "register" ? "#registerEmailForm button[type='submit']" : "#loginEmailForm button[type='submit']");
    $submit.prop("disabled", true);
    try {
      const response = await window.HNUI.api.request("auth/email-code", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email }),
      });
      const result = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(result.message || "Ø§Ø±Ø³Ø§Ù„ Ú©Ø¯ Ø§Ù†Ø¬Ø§Ù… Ù†Ø´Ø¯. Ø¯ÙˆØ¨Ø§Ø±Ù‡ ØªÙ„Ø§Ø´ Ú©Ù†ÛŒØ¯.");
    } catch (error) {
      const $message = mode === "register" ? $registerEmailError : $loginEmailError;
      window.HNUI.setMessage($message[0], error.message || "Ø§Ø±ØªØ¨Ø§Ø· Ø¨Ø§ Ø³Ø±ÙˆÛŒØ³ Ø§Ø±Ø³Ø§Ù„ Ø§ÛŒÙ…ÛŒÙ„ Ø¨Ø±Ù‚Ø±Ø§Ø± Ù†Ø´Ø¯.", "error");
      return;
    } finally {
      $submit.prop("disabled", false);
    }
    pendingAuthMode = mode;
    $("#sentEmail").text(email);
    $("#emailOtpTitle").text(
      mode === "register"
        ? "Ú©Ø¯ ÙØ¹Ø§Ù„â€ŒØ³Ø§Ø²ÛŒ Ø±Ø§ ÙˆØ§Ø±Ø¯ Ú©Ù†ÛŒØ¯"
        : "Ú©Ø¯ ÙˆØ±ÙˆØ¯ Ø±Ø§ ÙˆØ§Ø±Ø¯ Ú©Ù†ÛŒØ¯",
    );
    $(".login-auth-tabs").addClass("d-none");
    $loginEmailStep.addClass("d-none").prop("hidden", true);
    $registerEmailStep.addClass("d-none").prop("hidden", true);
    $emailOtpStep.removeClass("d-none").prop("hidden", false);
    $emailOtpInput.val("").trigger("focus");
  };
  const migrateLegacyFeedback = async (account) => {
    const storage = window.HNUI.storage;
    for (const key of storage.legacyKeys()) {
      if (key === recentSearchKey) {
        const items = storage.json(key, []);
        let complete = true;
        for (const query of (Array.isArray(items) ? items : []).slice(0, 6)) {
          try {
            const response = await window.HNUI.api.request("account/recent-searches", {
              method: "POST", headers: { "Content-Type": "application/json" },
              body: JSON.stringify({ query: String(query).slice(0, 80) }),
            });
            if (!response.ok) complete = false;
          } catch { complete = false; }
        }
        if (complete) { storage.remove(key); storage.clearLegacy(key); }
        continue;
      }
      const productMatch = key.match(/^hn-product-(reviews|questions)-([1-9]\d*)$/);
      if (productMatch) {
        const items = storage.json(key, []);
        if (!Array.isArray(items) || !items.length) { storage.clearLegacy(key); continue; }
        try {
          const response = await window.HNUI.api.request(`store/products/${productMatch[2]}/feedback/${productMatch[1]}/import`, {
            method: "POST", headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ items: items.slice(0, 50).map((item) => ({
              name: item.name, text: item.text,
              rating: productMatch[1] === "reviews" ? Number(item.rating) || 5 : null,
              createdAt: item.createdAt || null,
            })) }),
          });
          if (response.ok) { storage.remove(key); storage.clearLegacy(key); }
        } catch { /* Keep the legacy copy in memory until the next API attempt. */ }
        continue;
      }

      const likedMatch = key.match(/^hn-article-feedback-v1-(.+)-liked$/);
      if (likedMatch && storage.legacy(key) === "true") {
        const slug = likedMatch[1];
        const alreadyLiked = (account.articleInteractions || []).some((item) => item.slug === slug && item.type === "like");
        if (!alreadyLiked) {
          try {
            const response = await window.HNUI.api.request(`account/articles/${encodeURIComponent(slug)}/interactions/like`, { method: "POST" });
            if (!response.ok) continue;
          } catch { continue; }
        }
        storage.remove(key); storage.clearLegacy(key);
        continue;
      }

      const articleMatch = key.match(/^hn-article-feedback-v1-(?!.*-liked$)(.+)$/);
      if (!articleMatch) continue;
      const slug = articleMatch[1];
      const items = storage.json(key, []);
      if (!Array.isArray(items) || !items.length) { storage.clearLegacy(key); continue; }
      const existing = new Set((account.articleFeedback || []).filter((item) => item.slug === slug).map((item) => `${item.name}\n${item.text}`));
      let complete = true;
      for (const item of items.slice(0, 50)) {
        const signature = `${item.name}\n${item.text}`;
        if (existing.has(signature)) continue;
        try {
          const response = await window.HNUI.api.request(`account/articles/${encodeURIComponent(slug)}/feedback`, {
            method: "POST", headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ name: item.name, text: item.text, rating: item.rating ?? null }),
          });
          if (!response.ok) { complete = false; continue; }
          existing.add(signature);
        } catch { complete = false; }
      }
      if (complete) { storage.remove(key); storage.clearLegacy(key); }
    }
    document.dispatchEvent(new CustomEvent("hn:legacy-data-imported"));
  };

  const completeDemoLogin = async () => {
    try {
      const response = await window.HNUI.api.request("account/state");
      if (response.ok) {
        const account = await response.json();
        await migrateLegacyFeedback(account);
        if (account.checkout?.selectedAddressId) window.HNUI.storage.set("hn-selected-address", String(account.checkout.selectedAddressId));
        if (account.checkout?.paymentMethod) window.HNUI.storage.set("hn-payment-method-v2", account.checkout.paymentMethod);
        const localAlerts = window.HNUI.storage.json("hn-price-watches-demo", []);
        const alertKey = (item) => `${item.productId}:${item.scope}:${JSON.stringify(item.variants || {})}`;
        const mergedAlerts = new Map([...(Array.isArray(account.priceAlerts) ? account.priceAlerts : []), ...(Array.isArray(localAlerts) ? localAlerts : [])].map((item) => [alertKey(item), item]));
        window.HNUI.storage.set("hn-price-watches-demo", JSON.stringify([...mergedAlerts.values()]));
        for (const [key, remote] of [["hn-favorites", account.favorites], ["hn-compare", account.compare]]) {
          const local = window.HNUI.storage.json(key, []);
          window.HNUI.storage.set(key, JSON.stringify([...new Set([...(Array.isArray(remote) ? remote.map((entry) => String(entry.id)) : []), ...(Array.isArray(local) ? local.map(String) : [])])]));
        }
        const addresses = Array.isArray(account.addresses) ? [...account.addresses] : [];
        for (const address of window.HNUI.storage.json("hn-delivery-addresses", [])) {
          if (addresses.some((saved) => String(saved.id) === String(address.id))) continue;
          const savedResponse = await window.HNUI.api.request("account/addresses", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ title: address.title, recipient: address.recipient, phone: address.phone, country: address.country, countryId: address.countryId, province: address.province, provinceId: address.provinceId, city: address.city, cityId: address.cityId, street: address.street, postal: address.postal }) });
          if (savedResponse.ok) { const saved = await savedResponse.json(); addresses.push({ ...address, id: String(saved.id) }); }
        }
        window.HNUI.storage.set("hn-delivery-addresses", JSON.stringify(addresses));
        const cart = window.HNUI.cart.read();
        const restored = (Array.isArray(account.cart) ? account.cart : []).flatMap((line) => {
          if (cart.some((item) => String(item.id) === String(line.id) && String(item.salesPlanId || "regular") === String(line.salesPlanId))) return [];
          const product = (window.HNProducts || []).find((candidate) => String(candidate.id) === String(line.id));
          return product ? [{ ...product, id: String(line.id), quantity: Number(line.quantity), salesPlanId: line.salesPlanId, options: line.options || {} }] : [];
        });
        if (restored.length) window.HNUI.cart.write([...cart, ...restored]);
      }
    } catch { }
    if (pendingAuthMode === "register" && pendingRegistrationProfile)
      await window.HNUI.api.request("account/profile", { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(pendingRegistrationProfile) });
    userLoggedIn = true;
    resumeAuthenticatedAction = true;
    window.HNUI.auth.login();
    for (const key of ["hn-favorites", "hn-compare"]) window.HNUI.storage.set(key, window.HNUI.storage.get(key, "[]"));
    window.HNUI.storage.set("hn-price-watches-demo", window.HNUI.storage.get("hn-price-watches-demo", "[]"));
    window.HNUI.cart.write(window.HNUI.cart.read());
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
        "Ø§ÛŒÙ…ÛŒÙ„ Ù…Ø¹ØªØ¨Ø± Ùˆ Ø±Ù…Ø² Ø¹Ø¨ÙˆØ± Ø±Ø§ ÙˆØ§Ø±Ø¯ Ú©Ù†ÛŒØ¯.",
        "error",
      );
      return;
    }
    window.HNUI.setMessage($loginEmailError[0], "");
    void requestEmailCode("login", email);
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
        "Ù†Ø§Ù…ØŒ Ù†Ø§Ù… Ø®Ø§Ù†ÙˆØ§Ø¯Ú¯ÛŒ Ùˆ Ø§ÛŒÙ…ÛŒÙ„ Ù…Ø¹ØªØ¨Ø± Ø±Ø§ ÙˆØ§Ø±Ø¯ Ú©Ù†ÛŒØ¯.",
        "error",
      );
      return;
    }
    pendingRegistrationProfile = { firstName, lastName, phone: null };
    window.HNUI.setMessage($registerEmailError[0], "");
    void requestEmailCode("register", email);
  });

  $("#loginEmailOtp").on("input", function () {
    this.value = window.HNUI.toEnglishDigits(this.value)
      .replace(/\D/g, "")
      .slice(0, 6);
  });

  $("#emailOtpForm").on("submit", async (event) => {
    event.preventDefault();
    const code = window.HNUI.toEnglishDigits($emailOtpInput.val().trim());
    if (!/^\d{6}$/.test(code)) {
      window.HNUI.setMessage(
        $emailOtpError[0],
        "Ú©Ø¯ Ø´Ø´â€ŒØ±Ù‚Ù…ÛŒ Ø±Ø§ Ú©Ø§Ù…Ù„ ÙˆØ§Ø±Ø¯ Ú©Ù†ÛŒØ¯.",
        "error",
      );
      return;
    }
    window.HNUI.setMessage($emailOtpError[0], "");
    const $submit = $("#emailOtpForm button[type='submit']").prop("disabled", true);
    try {
      const response = await window.HNUI.api.request("auth/email-code/verify", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: $("#sentEmail").text(), code }),
      });
      const result = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(result.message || "Ú©Ø¯ ØªØ£ÛŒÛŒØ¯ Ù…Ø¹ØªØ¨Ø± Ù†ÛŒØ³Øª.");
      completeDemoLogin();
    } catch (error) {
      window.HNUI.setMessage($emailOtpError[0], error.message || "Ø§Ø±ØªØ¨Ø§Ø· Ø¨Ø§ Ø³Ø±ÙˆÛŒØ³ ØªØ£ÛŒÛŒØ¯ Ø¨Ø±Ù‚Ø±Ø§Ø± Ù†Ø´Ø¯.", "error");
    } finally {
      $submit.prop("disabled", false);
    }
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
  void window.HNUI.api.request("auth/email-code/session").then(async (response) => {
    const session = response.ok ? await response.json() : { authenticated: false };
    userLoggedIn = session.authenticated === true;
    if (userLoggedIn) {
      try {
        const stateResponse = await window.HNUI.api.request("account/state");
        if (stateResponse.ok) {
          const account = await stateResponse.json();
          await migrateLegacyFeedback(account);
          if (account.checkout?.selectedAddressId) window.HNUI.storage.set("hn-selected-address", String(account.checkout.selectedAddressId));
          if (account.checkout?.paymentMethod) window.HNUI.storage.set("hn-payment-method-v2", account.checkout.paymentMethod);
          const localAlerts = window.HNUI.storage.json("hn-price-watches-demo", []);
          const alertKey = (item) => `${item.productId}:${item.scope}:${JSON.stringify(item.variants || {})}`;
          const mergedAlerts = new Map([...(Array.isArray(account.priceAlerts) ? account.priceAlerts : []), ...(Array.isArray(localAlerts) ? localAlerts : [])].map((item) => [alertKey(item), item]));
          window.HNUI.storage.set("hn-price-watches-demo", JSON.stringify([...mergedAlerts.values()]));
          for (const [key, remote] of [["hn-favorites", account.favorites], ["hn-compare", account.compare]]) {
            const local = window.HNUI.storage.json(key, []);
            const merged = [...new Set([...(Array.isArray(remote) ? remote.map((entry) => String(entry.id)) : []), ...(Array.isArray(local) ? local.map(String) : [])])];
            window.HNUI.storage.set(key, JSON.stringify(merged));
          }
          if (Array.isArray(account.addresses)) {
            const localAddresses = window.HNUI.storage.json("hn-delivery-addresses", []);
            const mergedAddresses = [...account.addresses];
            for (const address of Array.isArray(localAddresses) ? localAddresses : []) {
              if (mergedAddresses.some((saved) => String(saved.id) === String(address.id))) continue;
              const saveResponse = await window.HNUI.api.request("account/addresses", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ title: address.title, recipient: address.recipient, phone: address.phone, country: address.country, countryId: address.countryId, province: address.province, provinceId: address.provinceId, city: address.city, cityId: address.cityId, street: address.street, postal: address.postal }) });
              if (!saveResponse.ok) continue;
              const saved = await saveResponse.json();
              mergedAddresses.push({ ...address, id: String(saved.id) });
            }
            window.HNUI.storage.set("hn-delivery-addresses", JSON.stringify(mergedAddresses));
          }
          const localCart = window.HNUI.cart.read();
          const restored = (Array.isArray(account.cart) ? account.cart : []).flatMap((line) => {
            if (localCart.some((item) => String(item.id) === String(line.id) && String(item.salesPlanId || "regular") === String(line.salesPlanId))) return [];
            const product = (window.HNProducts || []).find((candidate) => String(candidate.id) === String(line.id));
            return product ? [{ ...product, id: String(line.id), quantity: Number(line.quantity), salesPlanId: line.salesPlanId, options: line.options || {} }] : [];
          });
          if (restored.length) window.HNUI.cart.write([...localCart, ...restored]);
        }
      } catch { }
      window.HNUI.auth.login();
      for (const key of ["hn-favorites", "hn-compare"]) window.HNUI.storage.set(key, window.HNUI.storage.get(key, "[]"));
      window.HNUI.storage.set("hn-price-watches-demo", window.HNUI.storage.get("hn-price-watches-demo", "[]"));
      window.HNUI.cart.write(window.HNUI.cart.read());
    } else window.HNUI.storage.remove("hn-demo-auth");
    updateAccountUI();
    if (!userLoggedIn && protectedPath) showLoginModal(() => window.location.reload());
  }).catch(() => { userLoggedIn = false; window.HNUI.storage.remove("hn-demo-auth"); updateAccountUI(); if (protectedPath) showLoginModal(() => window.location.reload()); });

  $(".account-links>a:last-child").on("click", (event) => {
    event.preventDefault();
    userLoggedIn = false;
    void window.HNUI.api.request("auth/email-code/logout", { method: "POST" });
    window.HNUI.auth.logout();
    setAccountOpen(false);
    updateAccountUI();
  });
  const saveCart = () => {
    cart = window.HNUI?.cart.write(cart) || cart;
    renderCart();
  };
  let renderedHeaderCartShape = "";
  const renderCart = () => {
    const totalCount = cart.reduce((sum, item) => sum + item.quantity, 0);
    const totalPrice = cart.reduce(
      (sum, item) => sum + item.price * item.quantity,
      0,
    );
    $("#cartCount").text(faNumber(totalCount));
    $("#cartTitleCount").text(`${faNumber(totalCount)} Ù…Ø­ØµÙˆÙ„`);
    $("#cartTotal").text(`${faNumber(totalPrice)} ØªÙˆÙ…Ø§Ù†`);
    $cartFooter.toggle(cart.length > 0);
    const shape = JSON.stringify(cart.map(({ quantity, ...item }) => item));
    if (cart.length && shape === renderedHeaderCartShape) {
      $cartItems.find("[data-cart-id]").each(function () {
        const item = cart.find((entry) => String(entry.id) === String(this.dataset.cartId));
        if (!item) return;
        this.querySelector(".cart-quantity-value").textContent = faNumber(item.quantity);
        this.querySelector('[data-action="minus"]').disabled = item.quantity <= 1;
        this.querySelector('[data-action="plus"]').disabled = item.quantity >= window.HNUI.cart.quantityLimit(item);
      });
      return;
    }
    renderedHeaderCartShape = shape;
    if (!cart.length) {
      $cartItems.html(
        '<div class="empty-cart header-panel-empty"><span class="empty-cart-icon header-panel-empty-icon"><i class="bi bi-cart3" aria-hidden="true"></i></span><strong>Ø³Ø¨Ø¯ Ø®Ø±ÛŒØ¯ Ø´Ù…Ø§ Ø®Ø§Ù„ÛŒ Ø§Ø³Øª</strong><p>Ø¨Ø±Ø§ÛŒ Ø´Ø±ÙˆØ¹ØŒ ÛŒÚ© Ù…Ø­ØµÙˆÙ„ Ø¨Ù‡ Ø³Ø¨Ø¯ Ø®Ø±ÛŒØ¯ Ø§Ø¶Ø§ÙÙ‡ Ú©Ù†ÛŒØ¯.</p></div>',
      );
      return;
    }
    $cartItems.html(
      cart
        .map((item) => {
          const safeId = escapeHtml(item.id);
          const safeName = escapeHtml(item.name);
          const safeImage = escapeHtml(item.image);
          const href = window.HNProductUrl?.(item.id, {salesPlanId: item.salesPlanId}) || "/search";
          const planDetails = item.salesPlanSnapshot;
          const planBadge = `<span class="cart-sales-plan-badge">${escapeHtml(window.HNUI.cart.salesPlanName(item.salesPlanId, planDetails?.name))}</span>`;
          return `<div class="cart-item" data-cart-id="${safeId}" data-sales-plan-id="${escapeHtml(item.salesPlanId || "regular")}"><div class="cart-item-primary"><img src="${safeImage}" alt="${safeName}" loading="lazy" decoding="async"><div class="cart-item-info"><strong><a class="header-panel-product-link" href="${escapeHtml(href)}">${safeName}</a></strong>${planBadge}<div class="cart-item-actions"><span>${faNumber(item.price)} ØªÙˆÙ…Ø§Ù†</span>${window.HNUI.cart.renderControls(item)}</div></div></div>${cartOptionsMarkup(item)}</div>`;
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
      (product) => String(product.id) === String($row.attr("data-cart-id")) &&
        product.salesPlanId === String($row.attr("data-sales-plan-id") || "regular"),
    );
    if (!item) return;
    if ($button.data("action") === "plus")
      window.HNUI.cart.update(item.id, item.quantity + 1, item.salesPlanId);
    if ($button.data("action") === "minus")
      window.HNUI.cart.update(item.id, item.quantity - 1, item.salesPlanId);
    if ($button.data("action") === "remove") {
      event.stopPropagation();
      window.HNUI.cart.remove(item.id, item.salesPlanId);
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
    if (!storedListCount("hn-favorites")) return;
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
    if (!storedListCount("hn-compare")) return;
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
    if (!cart.length) return;
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
      isFavorite ? "Ø­Ø°Ù Ø§Ø² Ø¹Ù„Ø§Ù‚Ù‡â€ŒÙ…Ù†Ø¯ÛŒâ€ŒÙ‡Ø§" : "Ø§ÙØ²ÙˆØ¯Ù† Ø¨Ù‡ Ø¹Ù„Ø§Ù‚Ù‡â€ŒÙ…Ù†Ø¯ÛŒâ€ŒÙ‡Ø§",
    );
    $button
      .find("i")
      .attr("class", isFavorite ? "bi bi-heart-fill" : "bi bi-heart");
    showListActionToast(
      isFavorite
        ? "Ù…Ø­ØµÙˆÙ„ Ø¨Ù‡ Ø¹Ù„Ø§Ù‚Ù‡â€ŒÙ…Ù†Ø¯ÛŒâ€ŒÙ‡Ø§ Ø§Ø¶Ø§ÙÙ‡ Ø´Ø¯."
        : "Ù…Ø­ØµÙˆÙ„ Ø§Ø² Ø¹Ù„Ø§Ù‚Ù‡â€ŒÙ…Ù†Ø¯ÛŒâ€ŒÙ‡Ø§ Ø­Ø°Ù Ø´Ø¯.",
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
    const targetElement = $target[0];
    const scrollMarginTop =
      Number.parseFloat(window.getComputedStyle(targetElement).scrollMarginTop) || 0;
    const scrollTop = Math.max(0, $target.offset().top - scrollMarginTop);
    if (prefersReducedMotion) {
      window.scrollTo(0, scrollTop);
      return;
    }
    $("html, body").stop().animate({ scrollTop }, 500);
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
      "Ø«Ø¨Øªâ€ŒÙ†Ø§Ù… Ø®Ø¨Ø±Ù†Ø§Ù…Ù‡ Ù‡Ù†ÙˆØ² ÙØ¹Ø§Ù„ Ù†ÛŒØ³ØªØ› Ø§Ø·Ù„Ø§Ø¹Ø§ØªÛŒ Ø§Ø±Ø³Ø§Ù„ ÛŒØ§ Ø°Ø®ÛŒØ±Ù‡ Ù†Ø´Ø¯.",
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
        "Ù…Ø­ØµÙˆÙ„ Ù…Ù†ØªØ®Ø¨",
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
          "content/products/images/card/cordless-hammer-drill-hn-001-row-001-card-800.jpg",
      ),
      code: catalogProduct
        ? catalogProduct.code
        : $root.find("small").first().text() || "Ú©Ø¯ Ù…Ø­ØµÙˆÙ„",
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
    if (!$image.find(".favorite").length) {
      $image.prepend(
        `<button class="favorite" type="button" data-product-id="${escapeHtml(productId)}" aria-label="Ø§ÙØ²ÙˆØ¯Ù† Ø¨Ù‡ Ø¹Ù„Ø§Ù‚Ù‡â€ŒÙ…Ù†Ø¯ÛŒâ€ŒÙ‡Ø§" aria-pressed="false" title="Ø¹Ù„Ø§Ù‚Ù‡â€ŒÙ…Ù†Ø¯ÛŒâ€ŒÙ‡Ø§"><i class="bi bi-heart" aria-hidden="true"></i></button>`,
      );
    }
    $image.find(".favorite").attr("data-product-id", productId);
    if (productId && !$image.find(".compare-trigger[data-product-id]").length) {
      $image.append(
        `<button type="button" class="compare-trigger" data-product-id="${escapeHtml(productId)}" aria-label="Ø§ÙØ²ÙˆØ¯Ù† Ø¨Ù‡ Ù…Ù‚Ø§ÛŒØ³Ù‡" aria-pressed="false" title="Ø§ÙØ²ÙˆØ¯Ù† Ø¨Ù‡ Ù…Ù‚Ø§ÛŒØ³Ù‡"><i class="bi bi-arrow-left-right" aria-hidden="true"></i></button>`,
      );
    }
    let $actions = $image.children(".product-card-actions");
    if (!$actions.length)
      $actions = $(
        '<div class="product-card-actions" role="group" aria-label="Ø¹Ù…Ù„ÛŒØ§Øª Ù…Ø­ØµÙˆÙ„"></div>',
      ).appendTo($image);
    $image.children(".favorite, .compare-trigger").appendTo($actions);
    const outOfStock = $cartButton.prop("disabled");
    const cartIconClass = outOfStock
      ? "bi bi-bag-x"
      : $cartButton.find("> i").attr("class") || "bi bi-bag-plus";
    $cartButton.html(
      `<i class="${escapeHtml(cartIconClass)}" aria-hidden="true"></i><span class="add-cart-label">${outOfStock ? "Ù†Ø§Ù…ÙˆØ¬ÙˆØ¯" : "Ø§ÙØ²ÙˆØ¯Ù† Ø¨Ù‡ Ø³Ø¨Ø¯"}</span>`,
    );
    $cartButton.attr({
      "aria-label": outOfStock ? "Ù†Ø§Ù…ÙˆØ¬ÙˆØ¯" : "Ø§ÙØ²ÙˆØ¯Ù† Ø¨Ù‡ Ø³Ø¨Ø¯ Ø®Ø±ÛŒØ¯",
      title: outOfStock ? "Ù…ÙˆØ¬ÙˆØ¯ÛŒ Ø§ÛŒÙ† Ú©Ø§Ù„Ø§ ØªÙ…Ø§Ù… Ø´Ø¯Ù‡ Ø§Ø³Øª" : "Ø§ÙØ²ÙˆØ¯Ù† Ø¨Ù‡ Ø³Ø¨Ø¯ Ø®Ø±ÛŒØ¯",
    });
    $card.attr("data-action-dock-ready", "true");
    syncCompareButtons();
    syncFavoriteButtons();
    if (!$image.find(".quick").length)
      $image.append(
        '<button type="button" class="quick">Ù…Ø´Ø§Ù‡Ø¯Ù‡ Ø³Ø±ÛŒØ¹ <i class="bi bi-arrow-left"></i></button>',
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
        ? "Ù…Ø­ØµÙˆÙ„ Ø¨Ù‡ ÙÙ‡Ø±Ø³Øª Ù…Ù‚Ø§ÛŒØ³Ù‡ Ø§Ø¶Ø§ÙÙ‡ Ø´Ø¯."
        : "Ù…Ø­ØµÙˆÙ„ Ø§Ø² ÙÙ‡Ø±Ø³Øª Ù…Ù‚Ø§ÛŒØ³Ù‡ Ø­Ø°Ù Ø´Ø¯.",
    );
    setCartOpen(false);
  });

  const openQuickView = (product) => {
    activeProduct = product;
    const rawStock = product.stockQuantity ?? product.inventoryCount ?? product.quantityInStock ?? product.availableQuantity ?? product.stock;
    const stock = rawStock === "" || rawStock === null || rawStock === undefined ? Number.NaN : Number(rawStock);
    const unavailable = Number.isFinite(stock) && stock >= 0 ? stock === 0 : product.inStock === false;
    const $quickAdd = $(".quick-add-cart");
    $quickAdd
      .prop("disabled", unavailable)
      .attr("aria-disabled", String(unavailable))
      .attr("aria-label", unavailable ? "Ù†Ø§Ù…ÙˆØ¬ÙˆØ¯" : "Ø§ÙØ²ÙˆØ¯Ù† Ø¨Ù‡ Ø³Ø¨Ø¯ Ø®Ø±ÛŒØ¯")
      .attr("title", unavailable ? "Ù…ÙˆØ¬ÙˆØ¯ÛŒ Ø§ÛŒÙ† Ú©Ø§Ù„Ø§ ØªÙ…Ø§Ù… Ø´Ø¯Ù‡ Ø§Ø³Øª" : "Ø§ÙØ²ÙˆØ¯Ù† Ø¨Ù‡ Ø³Ø¨Ø¯ Ø®Ø±ÛŒØ¯")
      .toggleClass("is-out-of-stock", unavailable)
      .html(`<i class="bi ${unavailable ? "bi-bag-x" : "bi-bag-plus"}" aria-hidden="true"></i> ${unavailable ? "Ù†Ø§Ù…ÙˆØ¬ÙˆØ¯" : "Ø§ÙØ²ÙˆØ¯Ù† Ø¨Ù‡ Ø³Ø¨Ø¯ Ø®Ø±ÛŒØ¯"}`);
    $(".stock-note > span").html(
      `<i aria-hidden="true"></i> ${unavailable ? "Ù†Ø§Ù…ÙˆØ¬ÙˆØ¯" : "Ù…ÙˆØ¬ÙˆØ¯ Ø¯Ø± Ø§Ù†Ø¨Ø§Ø±"}`,
    );
    $(".stock-note > small").text(
      unavailable ? "Ù…ÙˆØ¬ÙˆØ¯ÛŒ Ø§ÛŒÙ† Ú©Ø§Ù„Ø§ ØªÙ…Ø§Ù… Ø´Ø¯Ù‡ Ø§Ø³Øª." : "Ø²Ù…Ø§Ù† Ø§Ø±Ø³Ø§Ù„ Ø¨Ø± Ø§Ø³Ø§Ø³ Ù…Ù‚ØµØ¯ Ùˆ Ø³ÙØ§Ø±Ø´ Ø§Ø¹Ù„Ø§Ù… Ù…ÛŒâ€ŒØ´ÙˆØ¯.",
    );
    $("#quickViewImage").attr({
      src: product.image,
      alt: product.name,
    });
    $("#quickViewTitle").text(product.name);
    $("#quickViewCode").text(
      `${product.brand ? `${product.brand} Â· ` : ""}${product.code}`,
    );
    $("#quickViewPrice").text(`${faNumber(product.price)} ØªÙˆÙ…Ø§Ù†`);
    $(".quick-description").text(
      product.detail?.highlights?.[0] || product.detail?.intro || "Ù…Ø´Ø®ØµØ§Øª Ø§ÛŒÙ† Ù…Ø¯Ù„ Ø±Ø§ Ø¨Ø±Ø±Ø³ÛŒ Ú©Ù†ÛŒØ¯.",
    );
    const quickSpecs = [
      ["Ú¯Ø±ÙˆÙ‡ Ù…Ø­ØµÙˆÙ„", product.type],
      ["Ù…Ù†Ø¨Ø¹ ØªØºØ°ÛŒÙ‡", product.powerSource || product.movement],
      ["Ù…ÙˆØ±Ø¯ Ø§Ø³ØªÙØ§Ø¯Ù‡", product.useCase || product.strap],
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
    if (!activeProduct || this.disabled) return;
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
      ? `Ø­Ø¯Ø§Ú©Ø«Ø± ØªØ¹Ø¯Ø§Ø¯ Ù…Ø¬Ø§Ø² Ø¨Ø±Ø§ÛŒ Â«${product?.name || "Ø§ÛŒÙ† Ú©Ø§Ù„Ø§"}Â» ${faNumber(Number(limit))} Ø¹Ø¯Ø¯ Ø§Ø³Øª.`
      : product?.name && Number.isFinite(Number(stock))
        ? `Ù…ÙˆØ¬ÙˆØ¯ÛŒ Â«${product.name}Â» ÙÙ‚Ø· ${faNumber(Number(stock))} Ø¹Ø¯Ø¯ Ø§Ø³ØªØ› ØªØ¹Ø¯Ø§Ø¯ Ø³Ø¨Ø¯ Ø¨Ù‡ Ø³Ù‚Ù Ù…ÙˆØ¬ÙˆØ¯ÛŒ Ø±Ø³ÛŒØ¯.`
        : "ØªØ¹Ø¯Ø§Ø¯ Ø¯Ø±Ø®ÙˆØ§Ø³ØªÛŒ Ø§Ø² Ø³Ù‚Ù Ù…Ø¬Ø§Ø² Ú©Ø§Ù„Ø§ Ø¨ÛŒØ´ØªØ± Ø¨ÙˆØ¯Ø› ØªØ¹Ø¯Ø§Ø¯ Ø³Ø¨Ø¯ Ø¨Ù‡â€ŒØ±ÙˆØ²Ø±Ø³Ø§Ù†ÛŒ Ø´Ø¯.";
    showListActionToast(message, "warning");
  });
  });
});
