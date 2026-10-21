(function (window, document) {
  "use strict";
  window.HNUI.ready(() => window.HNUI.mount("header-navigation", document, () => {
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

  const desktop = window.matchMedia("(min-width: 992px)");
  const hover = window.matchMedia("(hover: hover) and (pointer: fine)");
  const controllers = new Set();
  const bindDropdown = (dropdown, {state, menuSelector, onOpen = () => {}, onClose = () => {}}) => {
    const trigger = dropdown.querySelector(":scope > .nav-link");
    const menu = dropdown.querySelector(":scope > " + menuSelector);
    if (!trigger || !menu) return;
    let closeTimer = null;
    let restoringFocus = false;
    const isOpen = () => dropdown.classList.contains(state);
    const close = () => {
      window.clearTimeout(closeTimer);
      const wasOpen = isOpen();
      dropdown.classList.remove(state, "show", "is-brand-switching");
      menu.classList.remove("show");
      trigger.setAttribute("aria-expanded", "false");
      if (wasOpen) onClose();
    };
    controllers.add(close);
    const open = () => {
      if (!desktop.matches || restoringFocus) return;
      window.clearTimeout(closeTimer);
      controllers.forEach((other) => { if (other !== close) other(); });
      const wasOpen = isOpen();
      dropdown.classList.add(state);
      trigger.setAttribute("aria-expanded", "true");
      if (!wasOpen) onOpen();
      document.querySelectorAll(".header-search-suggestions").forEach((host) => { host.hidden = true; });
    };
    const closeSoon = () => {
      window.clearTimeout(closeTimer);
      closeTimer = window.setTimeout(() => {
        if (!menu.matches(":hover") && !trigger.matches(":hover") && !dropdown.contains(document.activeElement)) close();
      }, 220);
    };
    trigger.addEventListener("pointerenter", () => { if (hover.matches) open(); });
    // Empty navigation space never opens a menu; the panel can only retain an already-open menu.
    menu.addEventListener("pointerenter", () => { if (isOpen()) window.clearTimeout(closeTimer); });
    dropdown.addEventListener("pointerleave", closeSoon);
    dropdown.addEventListener("focusin", (event) => {
      if ((event.target === trigger && trigger.matches(":focus-visible")) || menu.contains(event.target)) open();
    });
    dropdown.addEventListener("focusout", (event) => { if (!dropdown.contains(event.relatedTarget)) close(); });
    trigger.addEventListener("click", (event) => {
      if (!desktop.matches) return;
      event.preventDefault(); event.stopPropagation();
      if (isOpen()) close(); else open();
    });
    dropdown.addEventListener("keydown", (event) => {
      if (!desktop.matches) return;
      if (event.key === "Escape") {
        event.preventDefault(); event.stopPropagation(); close();
        restoringFocus = true; trigger.focus(); restoringFocus = false;
      } else if (event.key === "ArrowDown" && event.target === trigger) {
        event.preventDefault(); open(); menu.querySelector("a, button")?.focus();
      }
    });
    document.addEventListener("pointerdown", (event) => { if (!dropdown.contains(event.target)) close(); });
    desktop.addEventListener("change", close);
    close();
  };
  document.querySelectorAll("#mainNav .brand-dropdown").forEach((dropdown) =>
    bindDropdown(dropdown, {state: "is-brand-open", menuSelector: ".brand-menu"}));
  document.querySelectorAll("#mainNav .quick-links-dropdown").forEach((dropdown) =>
    bindDropdown(dropdown, {state: "is-quick-links-open", menuSelector: ".quick-links-menu"}));

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
    let activeCategoryGroups = [];
    let restoringHomeFocus = false;
    const updateHomeCategoryPanelSize = () => {
      if (!activeCategoryGroups.length) return;
      const gap = 12;
      const horizontalPadding = 36;
      const minimumColumnWidth = 184;
      const configuredMaxWidth =
        Number.parseFloat(
          window
            .getComputedStyle(document.documentElement)
            .getPropertyValue("--container-wide-max"),
        ) || 1480;
      const maxMenuWidth = Math.max(
        320,
        Math.min(window.innerWidth - 340, 1004, configuredMaxWidth),
      );
      const availableColumns = Math.max(
        1,
        Math.floor(
          (maxMenuWidth - horizontalPadding + gap) /
            (minimumColumnWidth + gap),
        ),
      );
      const columns = Math.min(
        5,
        activeCategoryGroups.length,
        availableColumns,
      );
      const idealWidth =
        horizontalPadding +
        columns * minimumColumnWidth +
        (columns - 1) * gap;
      const menuWidth = Math.min(
        maxMenuWidth,
        Math.max(Math.min(720, maxMenuWidth), idealWidth),
      );
      categoryPanel.style.setProperty(
        "--home-category-mega-columns",
        String(columns),
      );
      categoryPanel.style.width = `${menuWidth}px`;
    };
    const closeCategoryPanel = () => {
      categoryPanel.hidden = true;
      categoryRow.removeAttribute("data-open-category");
      activeCategoryGroups = [];
      categoryPanel.style.removeProperty("--home-category-mega-columns");
      categoryPanel.style.removeProperty("width");
      triggers.forEach((trigger) => {
        trigger.classList.remove("active");
        trigger.setAttribute("aria-expanded", "false");
      });
    };
    const openCategoryPanel = (category) => {
      if (!desktop.matches || restoringHomeFocus) return;
      const groups = categoryContent[category];
      if (!groups) return;
      controllers.forEach((close) => { if (close !== closeCategoryPanel) close(); });
      activeCategoryGroups = groups;
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
      updateHomeCategoryPanelSize();
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
      if (trigger?.matches(":focus-visible")) openCategoryPanel(trigger.dataset.category);
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
    window.addEventListener("resize", () => {
      if (!categoryPanel.hidden) updateHomeCategoryPanelSize();
    });
    categoryRow.addEventListener("pointerleave", closeCategoryPanel);
    controllers.add(closeCategoryPanel);
    desktop.addEventListener("change", closeCategoryPanel);
    categoryRow.addEventListener("keydown", (event) => {
      if (event.key !== "Escape" || categoryPanel.hidden) return;
      const trigger = triggers.find((item) => item.dataset.category === categoryRow.dataset.openCategory);
      event.preventDefault();
      closeCategoryPanel();
      restoringHomeFocus = true;
      trigger?.focus();
      restoringHomeFocus = false;
    });
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
    if (dropdown) bindDropdown(dropdown, {state: "is-mega-open", menuSelector: ".category-nav-mega",
      onOpen: () => { if (!buttons.some((button) => button.classList.contains("active"))) activate(buttons[0]); },
      onClose: reset});
    window.addEventListener("resize", updateColumnCount, {passive: true});
  });


  }));
})(window, document);
