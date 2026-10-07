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
  const storedListCount = (key) => {
    const list = window.HNUI.storage.json(key, []);
    return Array.isArray(list) ? new Set(list.map(String)).size : 0;
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
        .attr("title", active ? "حذف از مقایسه" : "افزودن به مقایسه");
    });
  };
  const renderNavCounters = () => {
    $("#favoritesCount").text(faNumber(storedListCount("hn-favorites")));
    $("#compareCount").text(faNumber(storedListCount("hn-compare")));
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
        ["دریل و پیچ‌گوشتی", "دریل چکشی", "دریل شارژی", "پیچ‌گوشتی برقی", "دریل بتن‌کن"],
        ["برش و سایش", "فرز و مینی‌فرز", "اره عمودبر", "اره گردبر", "صفحه برش"],
        ["تجهیزات کارگاهی", "دستگاه جوش", "کارواش", "کمپرسور باد", "لوازم جانبی ابزار"],
      ],
      "hand-tools": [
        ["ابزارهای دستی", "انواع آچار", "پیچ‌گوشتی دستی", "انبردست و سیم‌چین", "چکش و تبر"],
        ["اندازه‌گیری دستی", "متر و خط‌کش", "تراز دستی", "گونیا", "کولیس"],
        ["لوازم کارگاهی", "جعبه ابزار", "گیره و پیچ‌دستی", "تیغه و مته", "متعلقات"],
      ],
      components: [
        ["قطعات پایه", "مقاومت", "خازن", "دیود و LED", "سلف و ترانس"],
        ["قطعات نیمه‌رسانا", "آی‌سی", "ترانزیستور", "رگولاتور", "ماژول‌های آماده"],
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
        ["حفاظت مدار", "فیوز مینیاتوری", "محافظ جان", "کلید اتوماتیک", "تابلو برق"],
        ["کنترل صنعتی", "کنتاکتور", "تایمر", "شستی و سیگنال", "ترموستات"],
      ],
      safety: [
        ["تجهیزات حفاظت", "دستکش عایق", "عینک ایمنی", "کفش کار", "ماسک"],
        ["محافظت برق", "محافظ ولتاژ", "ارت و اتصال زمین", "صاعقه‌گیر", "سرج ارستر"],
        ["ایمنی کارگاه", "کلاه ایمنی", "لباس کار", "جعبه کمک‌های اولیه", "نوار هشدار"],
      ],
      measurement: [
        ["ابزار اندازه‌گیری", "مولتی‌متر", "آمپرمتر", "ولت‌متر", "کلمپ‌متر"],
        ["تست و عیب‌یابی", "فازمتر", "تستر کابل", "تستر باتری", "دماسنج"],
        ["ابزار دقیق", "منبع تغذیه", "اسیلوسکوپ", "فرکانس‌متر", "لوازم جانبی"],
      ],
    };

    const triggers = [...categoryMenu.querySelectorAll("button[data-category]")];
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
          link.href = "Search";
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

  const $amazingItems = $("#amazing .row > div");
  if ($amazingItems.length === 3) {
    const $fourthItem = $amazingItems.eq(1).clone();
    $fourthItem.find(".showcase-card").attr("data-offer-index", "4");
    $fourthItem.find(".product-image").removeClass("cream").addClass("light");
    $fourthItem.find("img").attr({
      src: "ClockTemplate/images/products/tool-soldering-station.png",
      alt: "Damavand Sport watch",
    });
    $fourthItem.find(".showcase-card-body small").text("SPORT 05");
    $fourthItem.find(".showcase-card-body h3").text("Damavand Sport");
    $fourthItem.find(".showcase-card-body strong").text("۲۳,۶۰۰,۰۰۰ تومان");
    $fourthItem.find(".add-cart").attr({
      "data-id": "damavand-offer",
      "data-name": "Damavand Sport",
      "data-price": "23600000",
      "data-image": "ClockTemplate/images/products/tool-soldering-station.png",
    });
    $amazingItems.add($fourthItem).each(function () {
      $(this).removeClass("col-md-4").addClass("col-12 col-md-6 col-xl-3");
    });
    $("#amazing .row").append($fourthItem);
    window.HNUI?.enhance(document.querySelector("#amazing"));
  }

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
  const productCardMarkup = (product) => `
    <div class="showcase-slide">
      <article class="showcase-card">
        <div class="product-image">
          <a href="Product?id=${product.id}" aria-label="مشاهده ${escapeHtml(product.name)}">
            <img loading="lazy" decoding="async" src="${escapeHtml(product.image)}" alt="ساعت ${escapeHtml(product.name)}">
          </a>
          ${product.badge ? `<span class="badge-best">${escapeHtml(product.badge)}</span>` : ""}
        </div>
        <div class="showcase-card-body">
          <small>${escapeHtml(product.englishName)}</small>
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
          <img loading="lazy" decoding="async" src="${escapeHtml(product.image)}" alt="ساعت ${escapeHtml(product.name)}">
        </a>
        ${product.badge ? `<span>${escapeHtml(product.badge)}</span>` : ""}
      </div>
      <small>${escapeHtml(product.englishName)}</small>
      <h3>${escapeHtml(product.name)}</h3>
      <p>ساعت اورجینال منتخب فروشگاه، همراه با ضمانت اصالت.</p>
      <strong>${faNumber(product.price * 1000000)} <small>تومان</small></strong>
      <button type="button" class="add-cart" data-id="${product.id}" data-name="${escapeHtml(product.name)}" data-price="${product.price * 1000000}" data-image="${escapeHtml(product.image)}">
        <i class="bi bi-bag-plus"></i> افزودن به سبد
      </button>
    </article>`;

  if (catalogProducts.length >= 24) {
    $("#bestsellersSlider").append(
      catalogProducts.slice(8, 14).map(productCardMarkup).join(""),
    );
    $("#newestSlider").append(
      catalogProducts.slice(14, 20).map(productCardMarkup).join(""),
    );
    $("#productRail").append(
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
  const $cartClose = $("#cartClose");
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
              /^((?:ClockTemplate\/)?images\/watch-(?:blue|cream|silver|story|hero))\.(?:jpe?g|png)$/i,
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
            /^(?:ClockTemplate\/)?images\/[\w.-]+$/i.test(item.image),
        );
    } catch {
      return [];
    }
  };
  let cart = window.HNUI.cart.read();

  const setCartOpen = (open) => {
    $cartWrap.toggleClass("open", open);
    $cartPanel.toggleClass("open", open);
    $body.toggleClass(
      "cart-mobile-open",
      open && $window.width() <= mobileBreakpoint,
    );
    $html.toggleClass(
      "cart-mobile-open",
      open && $window.width() <= mobileBreakpoint,
    );
    $cartToggle.attr("aria-expanded", String(open));
  };
  const placeCartPanel = () => {
    if ($window.width() <= mobileBreakpoint) $cartPanel.appendTo($body);
    else $cartPanel.appendTo($cartWrap);
    setCartOpen(false);
  };
  placeCartPanel();

  const $accountWrap = $("#accountWrap");
  const $accountToggle = $("#accountToggle");
  const $accountPanel = $("#accountPanel");
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
    $accountWrap.toggleClass("open", open);
    $accountPanel.toggleClass("open", open);
    $accountToggle.attr("aria-expanded", String(open));
    if (open) setCartOpen(false);
  };
  const placeAccountPanel = () => {
    if ($window.width() <= mobileBreakpoint) $accountPanel.appendTo($body);
    else $accountPanel.appendTo($accountWrap);
    setAccountOpen(false);
  };
  const updateAccountUI = () => {
    $accountToggle
      .toggleClass("logged-out", !userLoggedIn)
      .html(
        userLoggedIn
          ? '<i class="bi bi-person-check"></i><span class="account-label">حساب من</span><i class="bi bi-chevron-down"></i>'
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
  const englishDigits = (value) =>
    value.replace(/[۰-۹]/g, (digit) => "۰۱۲۳۴۵۶۷۸۹".indexOf(digit));

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
    $("#forgotPasswordError").text("ارسال ایمیل بازنشانی هنوز به سرویس ایمیل سایت متصل نشده است.");
  });

  const requestEmailCode = (mode, email) => {
    pendingAuthMode = mode;
    $("#sentEmail").text(email);
    $("#emailOtpTitle").text(
      mode === "register" ? "کد فعال‌سازی را وارد کنید" : "کد ورود را وارد کنید",
    );
    $(".login-auth-tabs").addClass("d-none");
    $loginEmailStep.addClass("d-none").prop("hidden", true);
    $registerEmailStep.addClass("d-none").prop("hidden", true);
    $emailOtpStep.removeClass("d-none").prop("hidden", false);
    $emailOtpInput.val("").trigger("focus");
  };

  $("#loginEmailForm").on("submit", (event) => {
    event.preventDefault();
    const email = $("#loginEmail").val().trim();
    const password = $("#loginPassword").val();
    if (!email || !$("#loginEmail")[0].validity.valid || !password) {
      $loginEmailError.text("ایمیل معتبر و رمز عبور را وارد کنید.");
      return;
    }
    $loginEmailError.text("اعتبارسنجی ورود هنوز به سرور متصل نشده است.");
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
    this.value = englishDigits(this.value).replace(/\D/g, "").slice(0, 6);
  });

  $("#emailOtpForm").on("submit", (event) => {
    event.preventDefault();
    const code = englishDigits($emailOtpInput.val().trim());
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
    $(pendingAuthMode === "register" ? "#registerEmail" : "#loginEmail").trigger("focus");
  });
  $loginModalElement.on("hidden.bs.modal", () => {
    $("#loginEmailForm, #registerEmailForm, #emailOtpForm, #forgotPasswordForm").each(function () {
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
        '<div class="empty-cart"><span class="empty-cart-icon"><i class="bi bi-bag"></i></span><strong>سبد خرید شما خالی است</strong><p>محصولات موردعلاقه‌تان را به سبد اضافه کنید.</p></div>',
      );
      return;
    }
    $cartItems.html(
      cart
        .map((item) => {
          const safeId = escapeHtml(item.id);
          const safeName = escapeHtml(item.name);
          const safeImage = escapeHtml(item.image);
          return `<div class="cart-item" data-cart-id="${safeId}"><img src="${safeImage}" alt="${safeName}" loading="lazy" decoding="async"><div class="cart-item-info"><strong>${safeName}</strong><span>${faNumber(item.price)} تومان</span><div class="cart-quantity"><button type="button" data-action="plus" aria-label="افزایش تعداد">+</button><b>${faNumber(item.quantity)}</b><button type="button" data-action="minus" aria-label="کاهش تعداد">−</button></div></div><button class="cart-remove" type="button" data-action="remove" aria-label="حذف محصول"><i class="bi bi-trash3"></i></button></div>`;
        })
        .join(""),
    );
  };

  document.addEventListener("hn:cart-change", () => {
    cart = window.HNUI?.cart.read() || cart;
    renderCart();
  });
  document.addEventListener("hn:cart-added", () => setCartOpen(true));

  $cartItems.on("click", "[data-action]", function () {
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
    if ($button.data("action") === "remove") window.HNUI.cart.remove(item.id);
  });

  $cartToggle.on("click", (event) => {
    event.stopPropagation();
    setAccountOpen(false);
    setCartOpen(!$cartPanel.hasClass("open"));
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
  $cartClose.on("click", () => setCartOpen(false));
  $cartPanel.on("click", (event) => event.stopPropagation());
  $document.on("click", (event) => {
    const $target = $(event.target);
    if (!$target.closest("#cartWrap, #cartPanel, .add-cart").length)
      setCartOpen(false);
    if (!$target.closest("#accountWrap, #accountPanel").length)
      setAccountOpen(false);
  });
  renderCart();

  $document.on("click", ".favorite", function () {
    const $button = $(this);
    // Search owns its favorite feedback; avoid toggling the same control twice.
    if ($button.closest("#productResults").length) return;
    $button.toggleClass("active");
    const isFavorite = $button.hasClass("active");
    $button.attr({
      "aria-pressed": String(isFavorite),
      "aria-label": isFavorite
        ? "حذف از علاقه‌مندی‌ها"
        : "افزودن به علاقه‌مندی‌ها",
    });
    $button
      .find("i")
      .attr("class", isFavorite ? "bi bi-heart-fill" : "bi bi-heart");
    $("#liveToast .toast-body").text(
      isFavorite
        ? "محصول به علاقه‌مندی‌ها اضافه شد."
        : "محصول از علاقه‌مندی‌ها حذف شد.",
    );
    bootstrap.Toast.getOrCreateInstance($("#liveToast")[0]).show();
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
        "ساعت منتخب",
    );
    return {
      id: String($cartButton.data("id") || name),
      name,
      price: Number($cartButton.data("price")) || 0,
      image: String(
        $cartButton.data("image") ||
          $root.find("img").first().attr("src") ||
          "ClockTemplate/images/products/tool-hammer-drill.png",
      ),
      code: $root.find("small").first().text() || "ORIGINAL COLLECTION",
    };
  };
  let activeProduct = null;
  const quickViewModal = bootstrap.Modal.getOrCreateInstance(
    $("#quickViewModal")[0],
  );
  const wristModal = bootstrap.Modal.getOrCreateInstance($("#wristModal")[0]);

  $('.quick[href="#"]').each(function () {
    $(this).replaceWith(
      `<button type="button" class="quick">${$(this).html()}</button>`,
    );
  });

  $(".product-card, .showcase-card, .rail-card").each(function () {
    const $card = $(this);
    const $image = $card.find(".product-image, .rail-image").first();
    const $cartButton = $card.find(".add-cart").first();
    const productId = String($cartButton.data("id") || "");
    if (productId && !$image.find(".compare-trigger").length) {
      $image.append(
        `<button type="button" class="compare-trigger" data-product-id="${escapeHtml(productId)}" aria-label="افزودن به مقایسه" aria-pressed="false" title="افزودن به مقایسه"><i class="bi bi-arrow-left-right" aria-hidden="true"></i></button>`,
      );
    }
    if (!$image.find(".quick").length)
      $image.append(
        '<button type="button" class="quick">مشاهده سریع <i class="bi bi-arrow-left"></i></button>',
      );
  });
  syncCompareButtons();

  $document.on("click", ".compare-trigger[data-product-id]", function (event) {
    event.preventDefault();
    event.stopPropagation();
    const productId = String(this.dataset.productId || "");
    if (!productId) return;
    const selected = readCompareList();
    const next = selected.includes(productId)
      ? selected.filter((id) => id !== productId)
      : [...selected, productId];
    window.HNUI.storage.set("hn-compare", JSON.stringify(next));
    syncCompareButtons();
  });

  const openQuickView = (product) => {
    activeProduct = product;
    $("#quickViewImage, #wristWatch").attr({
      src: product.image,
      alt: product.name,
    });
    $("#quickViewTitle").text(product.name);
    $("#quickViewCode").text(product.code);
    $("#quickViewPrice").text(`${faNumber(product.price)} تومان`);
    $("#interactiveViewer")
      .removeClass("auto-spin zoomed")
      .css({
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

  $(
    "#mobileCategories .mobile-submenu a, #mobileBrands .mobile-brand-list a",
  ).attr("href", "Search");
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
  $("#wristTrigger").on("click", () => {
    quickViewModal.hide();
    window.setTimeout(() => wristModal.show(), 250);
  });
  $("#wristSize").on("input", function () {
    const size = Number(this.value);
    $("#wristSizeLabel").text(`${faNumber(size)} سانتی‌متر`);
    $("#wristWatch").css("transform", `scale(${1.25 - (size - 14) * 0.045})`);
  });

  document.addEventListener("hn:cart-added", (event) => {
    if (prefersReducedMotion) return;
    const { product, trigger } = event.detail || {};
    const origin = trigger instanceof Element ? trigger : document.activeElement;
    if (!(origin instanceof Element)) return;
    const source =
      $(origin)
        .closest(".product-card,.showcase-card,.rail-card,.featured-main,.featured-side")
        .find("img")
        .first()[0] ||
      (origin.matches(".quick-add-cart") && $("#quickViewImage")[0]) ||
      (origin.id === "addProduct" && $("#productView .flow-photo")[0]);
    const target = $("#cartToggle")[0];
    const image = source?.currentSrc || source?.getAttribute("src") || product?.image;
    if (!target || !image) return;
    const a = source?.getBoundingClientRect() || origin.getBoundingClientRect(),
      b = target.getBoundingClientRect(),
      startX = a.left + a.width / 2 - 31,
      startY = a.top + a.height / 2 - 31,
      endX = b.left + b.width / 2 - startX - 18,
      endY = b.top + b.height / 2 - startY - 18;
    const $fly = $(
      `<img class="fly-product" src="${image}" alt="">`,
    )
      .css({ left: startX, top: startY })
      .appendTo($body);
    const flight = $fly[0].animate(
      [
        { transform: "translate3d(0, 0, 0) scale(.86) rotate(-5deg)", opacity: 0 },
        { transform: "translate3d(0, 0, 0) scale(1) rotate(0)", opacity: 1, offset: 0.12 },
        {
          transform: `translate3d(${endX * 0.46}px, ${endY * 0.46 - 54}px, 0) scale(.66) rotate(8deg)`,
          opacity: 0.92,
          offset: 0.54,
        },
        {
          transform: `translate3d(${endX}px, ${endY}px, 0) scale(.18) rotate(22deg)`,
          opacity: 0,
        },
      ],
      { duration: 760, easing: "cubic-bezier(.2,.75,.2,1)", fill: "forwards" },
    );
    flight.onfinish = () => {
      $fly.remove();
      target.classList.remove("cart-received");
      void target.offsetWidth;
      target.classList.add("cart-received");
    };
  });
});


