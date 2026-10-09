document.addEventListener("DOMContentLoaded", () => {
  const trigger = document.querySelector(".search-account");
  if (!trigger) return;
  document.body.insertAdjacentHTML(
    "beforeend",
    '<div class="shared-login-overlay"></div><section class="shared-login-modal" role="dialog" aria-modal="true" aria-labelledby="sharedLoginTitle"><button class="login-close" type="button" data-login-close aria-label="بستن"><i class="bi bi-x-lg"></i></button><div class="login-brand"><span>ع</span><div><strong>کمپانی علی‌رضا زارع</strong><small>ورود به حساب کاربری</small></div></div><div class="login-step"><h2 id="sharedLoginTitle">شماره موبایل خود را وارد کنید</h2><p>برای ورود یا ساخت حساب، کد تأیید به شماره موبایل شما ارسال می‌شود.</p><form><label for="sharedLoginPhone">شماره موبایل</label><div class="login-input"><i class="bi bi-phone"></i><input id="sharedLoginPhone" type="tel" inputmode="numeric" maxlength="11" placeholder="۰۹۱۲۱۲۳۴۵۶۷" autocomplete="tel" required></div><button type="submit">ادامه <i class="bi bi-arrow-left"></i></button></form></div></section>',
  );
  document.body.insertAdjacentHTML(
    "beforeend",
    '<aside class="shared-account-card shared-account-popover"><div class="account-user"><span class="account-avatar">م</span><div><strong>محمدحسین فخراوری</strong><small>مشتری کمپانی علی‌رضا زارع</small></div><i class="bi bi-chevron-left"></i></div><nav class="account-links"><a href="Profile"><i class="bi bi-person"></i><span>پروفایل من</span></a><a href="Profile/Orders"><i class="bi bi-bag"></i><span>سفارش‌های من</span><b>۰</b></a><a href="Profile/Addresses"><i class="bi bi-signpost-2"></i><span>آدرس‌های من</span></a><a href="Profile/Favorites"><i class="bi bi-heart"></i><span>لیست علاقه‌مندی‌ها</span></a><a href="Profile/Questions"><i class="bi bi-chat"></i><span>دیدگاه‌ها و پرسش‌ها</span></a></nav><button class="account-logout" type="button"><i class="bi bi-box-arrow-left"></i><span>خروج از حساب کاربری</span></button></aside>',
  );
  document.querySelectorAll(".shared-account-popover .account-links a").forEach((link) => {
    if (link.href.includes("Questions") || link.textContent.includes("دیدگاه‌ها و پرسش‌ها")) link.remove();
  });
  const modal = document.querySelector(".shared-login-modal"),
    overlay = document.querySelector(".shared-login-overlay"),
    accountPanel = document.querySelector(".shared-account-popover");
  trigger.setAttribute("aria-haspopup", "dialog");
  trigger.setAttribute("aria-expanded", "false");
  modal.setAttribute("aria-hidden", "true");
  const renderAuth = () => {
    const logged = window.HNUI?.auth.isLoggedIn() ?? false;
    trigger.classList.toggle("logged-in", logged);
    trigger.innerHTML = logged
      ? '<i class="bi bi-person"></i><span>حساب من</span><i class="bi bi-chevron-down"></i>'
      : '<i class="bi bi-person"></i><span>ورود / ثبت‌نام</span>';
    return logged;
  };
  const positionAccountPanel = () => {
    if (window.innerWidth <= 991) {
      accountPanel.style.removeProperty("top");
      accountPanel.style.removeProperty("left");
      return;
    }
    const rect = trigger.getBoundingClientRect();
    const panelWidth = accountPanel.offsetWidth || 336;
    const left = Math.min(
      window.innerWidth - panelWidth - 12,
      Math.max(12, rect.right - panelWidth),
    );
    accountPanel.style.top = `${Math.round(rect.bottom + 24)}px`;
    accountPanel.style.left = `${Math.round(left)}px`;
  };
  const close = () => {
    const wasOpen = modal.classList.contains("open");
    modal.classList.remove("open");
    modal.setAttribute("aria-hidden", "true");
    accountPanel.classList.remove("open");
    overlay.classList.remove("open");
    document.documentElement.style.overflow = "";
    trigger.setAttribute("aria-expanded", "false");
    if (wasOpen) window.HNUI?.dialog.deactivate(modal);
  };
  const open = () => {
    modal.classList.add("open");
    modal.setAttribute("aria-hidden", "false");
    overlay.classList.add("open");
    trigger.setAttribute("aria-expanded", "true");
    if (window.HNUI) window.HNUI.dialog.activate(modal, trigger);
    else {
      document.documentElement.style.overflow = "hidden";
      setTimeout(() => modal.querySelector("input").focus(), 80);
    }
  };
  let accountHoverTimer;
  const clearAccountHover = () => clearTimeout(accountHoverTimer);
  const openAccountHover = () => {
    if (window.innerWidth <= 991 || !renderAuth()) return;
    clearAccountHover();
    positionAccountPanel();
    accountPanel.classList.add("open");
    trigger.setAttribute("aria-expanded", "true");
  };
  const scheduleAccountClose = () => {
    if (window.innerWidth <= 991) return;
    clearAccountHover();
    accountHoverTimer = setTimeout(() => {
      if (!trigger.matches(":hover") && !accountPanel.matches(":hover")) {
        accountPanel.classList.remove("open");
        trigger.setAttribute("aria-expanded", "false");
      }
    }, 220);
  };
  trigger.addEventListener("click", (event) => {
    event.stopPropagation();
    if (renderAuth()) {
      positionAccountPanel();
      if (window.innerWidth > 991) accountPanel.classList.add("open");
      else accountPanel.classList.toggle("open");
      trigger.setAttribute(
        "aria-expanded",
        String(accountPanel.classList.contains("open")),
      );
    } else open();
  });
  trigger.addEventListener("mouseenter", openAccountHover);
  trigger.addEventListener("mouseleave", scheduleAccountClose);
  accountPanel.addEventListener("mouseenter", clearAccountHover);
  accountPanel.addEventListener("mouseleave", scheduleAccountClose);
  window.addEventListener("resize", positionAccountPanel);
  positionAccountPanel();
  document
    .querySelectorAll("[data-login-close]")
    .forEach((el) => el.addEventListener("click", close));
  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape") close();
    window.HNUI?.dialog.trap(
      e,
      modal.classList.contains("open") ? modal : null,
    );
  });
  modal.querySelector("form").addEventListener("submit", (e) => {
    e.preventDefault();
    window.HNUI?.auth.login();
    renderAuth();
    close();
  });
  accountPanel
    .querySelector(".account-logout")
    .addEventListener("click", () => {
      if (window.HNUI) window.HNUI.auth.logout();
      else window.HNUI?.auth.logout();
      renderAuth();
      close();
    });
  renderAuth();
});

