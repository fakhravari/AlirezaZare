(function (window, document) {
  "use strict";

  const links = [
    ["Search", "دسته‌بندی", "bi-grid-3x3-gap"],
    ["Brands", "برندها", "bi-award"],
    ["Magazine", "مجله ساعت", "bi-journal-richtext"],
    ["About", "درباره ما", "bi-building"],
    ["Contact", "تماس با ما", "bi-headset"],
  ];

  const currentPage = () =>
    (window.location.pathname.split("/").pop() || "Home/Index").toLowerCase();

  const categoryMenu = `
    <li class="page-header-dropdown page-category-dropdown">
      <button class="nav-link" type="button"><i class="bi bi-grid-3x3-gap ms-1"></i> دسته‌بندی</button>
      <div class="page-header-menu page-category-menu">
        <aside class="page-mega-sidebar">
          <a class="active" href="Search?type=مردانه"><i class="bi bi-watch"></i><span>ساعت مردانه</span><i class="bi bi-chevron-left"></i></a>
          <a href="Search?type=زنانه"><i class="bi bi-gem"></i><span>ساعت زنانه</span><i class="bi bi-chevron-left"></i></a>
          <a href="Search?type=اسپرت"><i class="bi bi-stopwatch"></i><span>ساعت اسپرت</span><i class="bi bi-chevron-left"></i></a>
          <a href="Search?type=کلاسیک"><i class="bi bi-stars"></i><span>ساعت کلاسیک</span><i class="bi bi-chevron-left"></i></a>
          <a href="Search"><i class="bi bi-percent"></i><span>فروش ویژه</span><i class="bi bi-chevron-left"></i></a>
        </aside>
        <div class="page-mega-content">
          <div class="page-mega-columns"></div>
          <div class="page-mega-bottom"><span><i class="bi bi-patch-check"></i> ضمانت اصالت دو ساله</span><span><i class="bi bi-truck"></i> ارسال رایگان سراسر ایران</span><a href="Search">مشاهده همه ساعت‌ها <i class="bi bi-arrow-left"></i></a></div>
        </div>
      </div>
    </li>`;

  const brands = [
    ["bi-shield-plus", "ویکتورینوکس", "VICTORINOX"], ["bi-gem", "پتک فیلیپ", "PATEK PHILIPPE"],
    ["bi-shield-lock", "پلیس", "POLICE"], ["bi-trophy", "پولو", "U.S. POLO"],
    ["bi-arrow-left-right", "پولو اکسچنج", "POLO EXCHANGE"], ["bi-stars", "بورلی هیلز", "BEVERLY HILLS"],
  ];
  const brandMenu = `
    <li class="page-header-dropdown page-brand-dropdown">
      <button class="nav-link" type="button"><i class="bi bi-award ms-1"></i> برندها</button>
      <div class="page-header-menu page-brand-menu">
        ${brands.map(([icon, name, english]) => `<a href="Search?brand=${encodeURIComponent(name)}"><b><i class="bi ${icon}" aria-hidden="true"></i></b><span>${name}<small>${english}</small></span><i class="bi bi-chevron-left"></i></a>`).join("")}
        <div class="page-brand-footer"><a href="Brands">مشاهده همه برندها <i class="bi bi-arrow-left"></i></a><span><i class="bi bi-shield-check"></i><b>ضمانت اصالت</b></span></div>
      </div>
    </li>`;

  function headerTemplate() {
    const page = currentPage();
    const standardLinks = links.slice(2).map(([href, label]) =>
      `<li><a class="nav-link${page === href ? " active" : ""}" href="${href}">${label}</a></li>`,
    ).join("");
    return `<nav class="navbar navbar-expand-lg inner-nav fixed-top" data-site-header>
      <div class="container">
        <a class="brand" href="Home/Index" aria-label="صفحه اصلی علی‌رضا زارع"><span>ع</span> علی‌رضا زارع</a>
        <div class="collapse navbar-collapse"><ul class="navbar-nav mx-auto gap-lg-4">${categoryMenu}${brandMenu}${standardLinks}</ul></div>
        <button class="page-menu-toggle" type="button" aria-label="باز کردن منو"><i class="bi bi-list"></i></button>
        <div class="page-nav-actions"><button class="page-account-toggle" type="button"><i class="bi bi-person"></i><span>ورود / ثبت‌نام</span></button><button class="page-cart-toggle" type="button" aria-label="سبد خرید"><i class="bi bi-cart3"></i><b>۰</b></button></div>
      </div>
    </nav>`;
  }

  const footerTemplate = () => `<footer class="site-footer" data-site-footer><div class="container"><div class="footer-grid"><div class="footer-about"><a class="footer-logo" href="Home/Index"><span>ع</span> کمپانی علی‌رضا زارع</a><p>واردات و عرضه ساعت‌های اورجینال مردانه و زنانه در بوشهر؛ همراه با تضمین اصالت و پشتیبانی خرید.</p><div class="footer-socials"><a href="#" aria-label="اینستاگرام"><i class="bi bi-instagram"></i></a><a href="#" aria-label="واتساپ"><i class="bi bi-whatsapp"></i></a><a href="#" aria-label="تلگرام"><i class="bi bi-telegram"></i></a></div><div class="footer-trust"><span>اینماد</span><span>ساماندهی</span><span>مجوز کسب</span></div></div><div class="footer-column"><h4>دسته‌بندی‌ها</h4><a href="Search?type=مردانه">ساعت مردانه</a><a href="Search?type=زنانه">ساعت زنانه</a><a href="Search?type=کلاسیک">ساعت کلاسیک</a><a href="Search?type=اسپرت">ساعت اسپرت</a></div><div class="footer-column"><h4>خدمات مشتریان</h4><a href="Tracking">پیگیری سفارش</a><a href="Shipping">شرایط ارسال</a><a href="Warranty">ضمانت اصالت</a><a href="Faq">پرسش‌های متداول</a></div><div class="footer-column"><h4>کمپانی علی‌رضا زارع</h4><a href="About">درباره مجموعه</a><a href="Contact">فروشگاه‌های ما</a><a href="Blog">مجله ساعت</a><a href="Contact">تماس با ما</a></div><div class="footer-column"><h4>راه‌های ارتباطی</h4><a href="tel:+989125514388">مدیریت: ۰۹۱۲۵۵۱۴۳۸۸</a><a href="tel:+989177764324">پشتیبانی: ۰۹۱۷۷۷۶۴۳۲۴</a><a href="Home/Index">وب‌سایت رسمی</a></div></div><div class="footer-copy"><span>© ۱۴۰۵ کمپانی علی‌رضا زارع؛ تمامی حقوق محفوظ است.</span><span>تخصص ما واردات و عرضه ساعت‌های اورجینال است.</span></div></div></footer>`;

  const panelsTemplate = () => `<div data-site-panels><div class="page-overlay"></div><aside class="page-menu-drawer"><div class="page-menu-head"><a class="brand" href="Home/Index" aria-label="صفحه اصلی علی‌رضا زارع"><span>ع</span> علی‌رضا زارع</a><button type="button" data-page-close><i class="bi bi-x-lg"></i></button></div><nav>${links.map(([href, label, icon]) => `<a href="${href}"><i class="bi ${icon}"></i><span>${label}</span></a>`).join("")}</nav><div class="page-menu-contact"><small>پشتیبانی خرید</small><a href="tel:+989177764324">۰۹۱۷۷۷۶۴۳۲۴</a></div></aside><aside class="page-panel page-cart-panel"><span class="cart-sheet-handle" aria-hidden="true"></span><div class="page-panel-head"><div class="cart-head-title"><span class="cart-head-icon"><i class="bi bi-bag-check"></i></span><span><strong>سبد خرید شما</strong><small class="page-cart-count">۰ محصول</small></span></div><button type="button" data-page-close aria-label="بستن سبد خرید"><i class="bi bi-x-lg"></i></button></div><div class="page-cart-items"></div><div class="page-cart-footer"><div class="page-cart-total"><span>مبلغ قابل پرداخت</span><strong>۰ تومان</strong></div><small><i class="bi bi-shield-check"></i> خرید امن با ضمانت اصالت کالا</small><a href="Cart">مشاهده سبد و تسویه <i class="bi bi-arrow-left"></i></a></div></aside><aside class="page-panel page-account-panel shared-account-card"><div class="account-user"><span class="account-avatar">م</span><div><strong>محمدحسین فخراوری</strong><small>مشتری کمپانی علی‌رضا زارع</small></div><i class="bi bi-chevron-left"></i></div><nav class="account-links"><a href="Account#orders"><i class="bi bi-bag"></i><span>سفارش‌های من</span><b>۰</b></a><a href="Account#addresses"><i class="bi bi-signpost-2"></i><span>آدرس‌های من</span></a><a href="Favorites"><i class="bi bi-heart"></i><span>لیست علاقه‌مندی‌ها</span></a><a href="Account#questions"><i class="bi bi-chat"></i><span>دیدگاه‌ها و پرسش‌ها</span></a></nav><button class="page-logout account-logout" type="button"><i class="bi bi-box-arrow-left"></i><span>خروج از حساب کاربری</span></button></aside><section class="page-login-box shared-login-modal" role="dialog" aria-modal="true" aria-labelledby="pageLoginTitle"><button class="login-close" type="button" data-page-close aria-label="بستن"><i class="bi bi-x-lg"></i></button><div class="login-brand"><span>ع</span><div><strong>کمپانی علی‌رضا زارع</strong><small>ورود به حساب کاربری</small></div></div><div class="login-step" id="pagePhoneStep"><h2 id="pageLoginTitle">شماره موبایل خود را وارد کنید</h2><p>برای ورود یا ساخت حساب، کد تأیید به شماره موبایل شما ارسال می‌شود.</p><form id="pagePhoneForm"><label for="pageLoginPhone">شماره موبایل</label><div class="login-input"><i class="bi bi-phone"></i><input id="pageLoginPhone" type="tel" inputmode="numeric" maxlength="11" placeholder="۰۹۱۲۱۲۳۴۵۶۷" autocomplete="tel" required></div><small class="login-error" id="pagePhoneError"></small><button type="submit">ادامه <i class="bi bi-arrow-left"></i></button></form></div><div class="login-step d-none" id="pageOtpStep"><button type="button" class="login-back" id="pageLoginBack"><i class="bi bi-arrow-right"></i> تغییر شماره</button><h2>کد تأیید را وارد کنید</h2><p>کد سه‌رقمی ارسال‌شده به <strong id="pageSentPhone"></strong> را وارد کنید.</p><form id="pageOtpForm"><label for="pageLoginOtp">کد تأیید</label><input class="otp-input" id="pageLoginOtp" type="text" inputmode="numeric" maxlength="3" placeholder="ـ ـ ـ" autocomplete="one-time-code" required><small class="login-error" id="pageOtpError"></small><button type="submit">تأیید و ورود <i class="bi bi-check2"></i></button></form></div></section></div>`;

  function connectProfileRoutes() {
    document.querySelectorAll("[data-site-panels] .account-links").forEach((nav) => {
      const routes = new Map([
        ["سفارش‌های من", "Profile/Orders"],
        ["آدرس‌های من", "Profile/Addresses"],
        ["لیست علاقه‌مندی‌ها", "Profile/Favorites"],
        ["دیدگاه‌ها و پرسش‌ها", "Profile/Questions"],
      ]);
      if (!nav.querySelector('a[href="Profile"]')) {
        nav.insertAdjacentHTML("afterbegin", '<a href="Profile"><i class="bi bi-person"></i><span>پروفایل من</span></a>');
      }
      nav.querySelectorAll("a").forEach((link) => {
        const route = routes.get(link.textContent.trim());
        if (route) link.href = route;
      });
    });
  }

  function render() {
    document.querySelectorAll("[data-site-header-slot]").forEach((slot) => slot.outerHTML = headerTemplate());
    document.querySelectorAll("[data-site-footer-slot]").forEach((slot) => slot.outerHTML = footerTemplate());
    if (document.querySelector("[data-site-header]") && !document.querySelector("[data-site-panels]")) {
      document.body.insertAdjacentHTML("beforeend", panelsTemplate());
    }
    connectProfileRoutes();
  }

  render();
  window.HNSiteLayout = Object.freeze({ render });
})(window, document);

