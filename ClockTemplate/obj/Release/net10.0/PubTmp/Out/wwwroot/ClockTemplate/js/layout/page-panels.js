document.addEventListener("DOMContentLoaded", () => {
  const accountButton = document.querySelector(".page-account-toggle");
  const cartButton = document.querySelector(".page-cart-toggle");
  if (!accountButton || !cartButton) return;
  const overlay = document.querySelector(".page-overlay"),
    menuButton = document.querySelector(".page-menu-toggle"),
    menuPanel = document.querySelector(".page-menu-drawer"),
    cartPanel = document.querySelector(".page-cart-panel"),
    accountPanel = document.querySelector(".page-account-panel"),
    loginBox = document.querySelector(".page-login-box");
  menuButton.setAttribute("aria-controls", "pageMenuDrawer");
  menuButton.setAttribute("aria-expanded", "false");
  menuPanel.id = "pageMenuDrawer";
  menuPanel.setAttribute("aria-label", "منوی اصلی");
  cartPanel.setAttribute("role", "dialog");
  cartPanel.setAttribute("aria-modal", "true");
  cartPanel.setAttribute("aria-label", "سبد خرید");
  accountButton.setAttribute("aria-haspopup", "dialog");
  cartButton.setAttribute("aria-haspopup", "dialog");
  document.querySelectorAll("[data-page-close]").forEach((button) => {
    if (!button.getAttribute("aria-label"))
      button.setAttribute("aria-label", "بستن پنل");
  });
  const drawerLinks = menuPanel.querySelectorAll("nav>a");
  if (drawerLinks[0]) drawerLinks[0].href = "Search";
  if (drawerLinks[1]) drawerLinks[1].href = "Search";
  const fa = (n) => window.HNUI.formatNumber(n);
  const escapeHtml = (value) => window.HNUI.escapeHtml(value);
  const phoneStep = document.querySelector("#pagePhoneStep");
  const otpStep = document.querySelector("#pageOtpStep");
  const phoneInput = document.querySelector("#pageLoginPhone");
  const otpInput = document.querySelector("#pageLoginOtp");
  const phoneError = document.querySelector("#pagePhoneError");
  const otpError = document.querySelector("#pageOtpError");
  const englishDigits = (value) =>
    value.replace(/[۰-۹]/g, (digit) => "۰۱۲۳۴۵۶۷۸۹".indexOf(digit));
  const resetLogin = () => {
    phoneStep.classList.remove("d-none");
    otpStep.classList.add("d-none");
    otpInput.value = "";
    phoneError.textContent = "";
    otpError.textContent = "";
  };
  let lastPanelTrigger = null;
  const closeAll = () => {
    [overlay, menuPanel, cartPanel, accountPanel, loginBox].forEach((el) => {
      el.classList.remove("open");
      el.setAttribute("aria-hidden", "true");
    });
    overlay.classList.remove("login-mode");
    document.documentElement.classList.remove("cart-mobile-open");
    document.body.classList.remove("cart-mobile-open");
    document.documentElement.style.overflow = "";
    cartButton.setAttribute("aria-expanded", "false");
    accountButton.setAttribute("aria-expanded", "false");
    menuButton.setAttribute("aria-expanded", "false");
    resetLogin();
    if (lastPanelTrigger && document.contains(lastPanelTrigger)) {
      const trigger = lastPanelTrigger;
      lastPanelTrigger = null;
      setTimeout(() => trigger.focus(), 0);
    }
  };
  const renderAuth = () => {
    const logged = window.HNUI.auth.isLoggedIn();
    accountButton.innerHTML = logged
      ? '<i class="bi bi-person-check"></i><span>حساب من</span><i class="bi bi-chevron-down"></i>'
      : '<i class="bi bi-person"></i><span>ورود / ثبت‌نام</span>';
    return logged;
  };
  const renderCart = () => {
    const cart = window.HNUI.cart.read(),
      count = cart.reduce((s, i) => s + (Number(i.quantity) || 0), 0),
      total = cart.reduce(
        (s, i) => s + (Number(i.price) || 0) * (Number(i.quantity) || 0),
        0,
      );
    cartButton.querySelector("b").textContent = fa(count);
    document.querySelector(".page-cart-count").textContent =
      `${fa(count)} محصول`;
    document.querySelector(".page-cart-total strong").textContent =
      `${fa(total)} تومان`;
    const box = document.querySelector(".page-cart-items");
    box.innerHTML = cart.length
      ? cart
          .map((i) => {
            const image = String(i.image || "ClockTemplate/images/products/watch-blue.webp")
              .replace(/[^\w.\/-]/g, "")
              .replace(
                /^((?:ClockTemplate\/)?images\/watch-(?:blue|cream|silver|story|hero))\.(?:jpe?g|png)$/i,
                "$1.webp",
              );
            const id = escapeHtml(i.id);
            const name = escapeHtml(i.name || "محصول");
            return `<article class="page-cart-item" data-cart-id="${id}"><img src="${image}" alt="${name}" loading="lazy" decoding="async"><div class="cart-item-info"><strong>${name}</strong><span>${fa(i.price)} تومان</span><div class="cart-quantity"><button type="button" data-cart-action="plus" aria-label="افزایش تعداد">+</button><b>${fa(i.quantity)}</b><button type="button" data-cart-action="minus" aria-label="کاهش تعداد">−</button></div></div><button class="cart-remove" type="button" data-cart-action="remove" aria-label="حذف محصول"><i class="bi bi-trash3"></i></button></article>`;
          })
          .join("")
      : '<div class="page-cart-empty"><span class="empty-cart-icon"><i class="bi bi-bag"></i></span><strong>سبد خرید شما خالی است</strong><p>محصولات موردعلاقه‌تان را به سبد اضافه کنید.</p></div>';
    document.querySelector(".page-cart-footer").style.display = cart.length
      ? "block"
      : "none";
  };
  const open = (panel) => {
    lastPanelTrigger = document.activeElement;
    closeAll();
    lastPanelTrigger = document.activeElement;
    overlay.classList.add("open");
    if (panel === loginBox) overlay.classList.add("login-mode");
    panel.classList.add("open");
    panel.setAttribute("aria-hidden", "false");
    if (panel === menuPanel) menuButton.setAttribute("aria-expanded", "true");
    document.documentElement.style.overflow = "hidden";
    setTimeout(() => panel.querySelector("button,a[href],input")?.focus(), 40);
  };
  const setCartOpen = (openState) => {
    closeAll();
    if (!openState) return;
    const mobile = window.innerWidth <= 991;
    if (mobile) {
      overlay.classList.add("open");
      document.documentElement.classList.add("cart-mobile-open");
      document.body.classList.add("cart-mobile-open");
      document.documentElement.style.overflow = "hidden";
    }
    cartPanel.classList.add("open");
    cartButton.setAttribute("aria-expanded", "true");
  };
  const alignToButton = (panel, button) => {
    if (window.innerWidth <= 991) {
      panel.style.removeProperty("top");
      panel.style.removeProperty("left");
      return;
    }
    const rect = button.getBoundingClientRect();
    panel.style.top = `${Math.round(rect.bottom + 24)}px`;
    panel.style.left = `${Math.round(rect.left)}px`;
  };
  const positionHeaderPanels = () => {
    alignToButton(accountPanel, accountButton);
    alignToButton(cartPanel, cartButton);
  };
  positionHeaderPanels();
  window.addEventListener("resize", positionHeaderPanels);
  let accountHoverTimer;
  const clearAccountHover = () => clearTimeout(accountHoverTimer);
  const setAccountOpen = (openState) => {
    closeAll();
    if (!openState) return;
    if (window.innerWidth <= 991) {
      open(accountPanel);
      accountButton.setAttribute("aria-expanded", "true");
      return;
    }
    alignToButton(accountPanel, accountButton);
    accountPanel.classList.add("open");
    accountPanel.setAttribute("aria-hidden", "false");
    accountButton.setAttribute("aria-expanded", "true");
  };
  const openAccountHover = () => {
    if (window.innerWidth <= 991 || !renderAuth()) return;
    clearAccountHover();
    setAccountOpen(true);
  };
  const scheduleAccountClose = () => {
    if (window.innerWidth <= 991) return;
    clearAccountHover();
    accountHoverTimer = setTimeout(() => {
      if (!accountButton.matches(":hover") && !accountPanel.matches(":hover")) {
        accountPanel.classList.remove("open");
        accountPanel.setAttribute("aria-hidden", "true");
        accountButton.setAttribute("aria-expanded", "false");
      }
    }, 220);
  };
  let cartHoverTimer;
  const clearCartHover = () => clearTimeout(cartHoverTimer);
  const openCartHover = () => {
    if (window.innerWidth <= 991) return;
    clearCartHover();
    renderCart();
    alignToButton(cartPanel, cartButton);
    setCartOpen(true);
  };
  const scheduleCartClose = () => {
    if (window.innerWidth <= 991) return;
    clearCartHover();
    cartHoverTimer = setTimeout(() => {
      if (!cartButton.matches(":hover") && !cartPanel.matches(":hover"))
        closeAll();
    }, 220);
  };
  menuButton.addEventListener("click", () => open(menuPanel));
  accountButton.addEventListener("click", (event) => {
    event.stopPropagation();
    const logged = renderAuth();
    if (!logged) {
      open(loginBox);
      return;
    }
    setAccountOpen(!accountPanel.classList.contains("open"));
  });
  accountButton.addEventListener("mouseenter", openAccountHover);
  accountButton.addEventListener("mouseleave", scheduleAccountClose);
  accountPanel.addEventListener("mouseenter", clearAccountHover);
  accountPanel.addEventListener("mouseleave", scheduleAccountClose);
  cartButton.setAttribute("aria-expanded", "false");
  cartButton.addEventListener("mouseenter", openCartHover);
  cartButton.addEventListener("mouseleave", scheduleCartClose);
  cartPanel.addEventListener("mouseenter", clearCartHover);
  cartPanel.addEventListener("mouseleave", scheduleCartClose);
  cartPanel.addEventListener("click", (event) => {
    const control = event.target.closest("[data-cart-action]");
    if (!control) return;
    const itemElement = control.closest("[data-cart-id]");
    const action = control.dataset.cartAction;
    const cart = window.HNUI.cart.read();
    const index = cart.findIndex((item) => String(item.id) === itemElement.dataset.cartId);
    if (index < 0) return;
    if (action === "plus") window.HNUI.cart.update(cart[index].id, cart[index].quantity + 1);
    if (action === "minus") window.HNUI.cart.update(cart[index].id, cart[index].quantity - 1);
    if (action === "remove") window.HNUI.cart.remove(cart[index].id);
    renderCart();
  });
  cartButton.addEventListener("click", (event) => {
    event.stopPropagation();
    const next = !cartPanel.classList.contains("open");
    renderCart();
    alignToButton(cartPanel, cartButton);
    setCartOpen(next);
  });
  cartPanel.addEventListener("click", (event) => event.stopPropagation());
  overlay.addEventListener("click", () => {
    if (!loginBox.classList.contains("open")) closeAll();
  });
  document.addEventListener("click", (event) => {
    if (
      cartPanel.classList.contains("open") &&
      !event.target.closest(".page-cart-panel,.page-cart-toggle")
    )
      closeAll();
  });
  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") closeAll();
  });
  document
    .querySelectorAll("[data-page-close]")
    .forEach((el) => el.addEventListener("click", closeAll));
  menuPanel.querySelectorAll("a[href]").forEach((link) =>
    link.addEventListener("click", (event) => {
      const href = link.getAttribute("href");
      if (!href) return;
      event.preventDefault();
      closeAll();
      window.location.assign(new URL(href, document.baseURI).href);
    }),
  );
  document.querySelector("#pagePhoneForm").addEventListener("submit", (event) => {
    event.preventDefault();
    const phone = englishDigits(phoneInput.value.trim());
    if (!/^09\d{9}$/.test(phone)) {
      phoneError.textContent = "شماره موبایل را با فرمت صحیح وارد کنید.";
      return;
    }
    phoneError.textContent = "";
    document.querySelector("#pageSentPhone").textContent = phone.replace(
      /(\d{4})(\d{3})(\d{4})/,
      "$1 $2 $3",
    );
    phoneStep.classList.add("d-none");
    otpStep.classList.remove("d-none");
    otpInput.focus();
  });
  document.querySelector("#pageOtpForm").addEventListener("submit", (event) => {
    event.preventDefault();
    const code = englishDigits(otpInput.value.trim());
    if (!/^\d{3}$/.test(code)) {
      otpError.textContent = "کد تأیید سه‌رقمی را کامل وارد کنید.";
      return;
    }
    otpError.textContent = "";
    localStorage.setItem("hn-demo-auth", "1");
    renderAuth();
    closeAll();
  });
  document.querySelector("#pageLoginBack").addEventListener("click", () => {
    otpStep.classList.add("d-none");
    phoneStep.classList.remove("d-none");
    phoneInput.focus();
  });
  document.querySelector(".page-logout").addEventListener("click", () => {
    localStorage.removeItem("hn-demo-auth");
    renderAuth();
    closeAll();
  });
  renderAuth();
  renderCart();
});


