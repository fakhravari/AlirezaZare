# پوسته، اجزای مشترک و ترتیب منابع

## ترکیب Razor

[Views/_ViewStart.cshtml](../../Shop/Views/_ViewStart.cshtml) پوسته را روی [_Shop.cshtml](../../Shop/Views/Shared/_Shop.cshtml) قرار می‌دهد:

```text
_Shop
  ├─ _SeoMetadata و منابع head
  ├─ _HomeHeader یا _InnerHeader → _Header
  ├─ #main-content → RenderBody / View صفحه
  ├─ _Footer
  └─ _Panels، دکمهٔ بازگشت به بالا و scriptها
```

بدنهٔ صفحه `home-page` یا `inner-page` و کلاس `mobile-page--{controller}-{action}` دارد. برای Search مقدار صحیح `mobile-page--product-index` است. variant هدر با `ViewData["HeaderVariant"]` تعیین می‌شود؛ منوی خانه و داخلی DOM یکسانی ندارند.

در خانه section `Hero` داخل `header.hero#home` رندر می‌شود؛ این section جایگاه محتوای hero است و جزو ترتیب CSS/JS نیست.

پنل‌ها بیرون `#main-content` هستند. خطای z-index، قفل scroll یا اندازهٔ پنل را فقط به View صفحه نسبت ندهید. skip link به `div#main-content[tabindex="-1"]` می‌رود؛ هر View، main معنایی خودش را درون آن حفظ می‌کند. عنصر main تو در تو نسازید.

## اجزای مشترک

| Partial/component | markup یا نشانه | مالک CSS/JS اصلی | جاهایی که باید رگرسیون شوند |
|---|---|---|---|
| هدر | `_HomeHeader`, `_InnerHeader`, `_Header`؛ body کلاس home/inner | `shared-header.css`, `shared-mega-menu.css`, themeها؛ `layout/main.js` | خانه + یک inner page، دسکتاپ + موبایل، login/logout و search open |
| مگامنو | خانه و inner markup/variant متفاوت؛ inner selectorهایی مثل `.page-mega-sidebar` | `shared-mega-menu.css`, `electrical-theme.css`, `industrial-system.css`, `mega-menu-unified.css`; باز/بسته و category در `main.js` | hover trigger → pointer حرکت به panel، keyboard focus، شاخهٔ پیش‌فرض، breakpoint 992؛ پنل inner زیر هدر و در قاب مرکزیِ عریض قرار می‌گیرد؛ حالت فعال/hover دسکتاپ دسته‌بندی زمینهٔ زرد و آیکن سرمه‌ای دارد |
| account popover/drawer هدر | `.shared-account-card`, `.shared-account-popover`, `.account-links` | `shared-account.css`; تعامل در `main.js` | کاربر خارج/وارد، desktop/mobile، Escape، focus و scroll |
| cart/favorites/compare panels | `.page-cart-panel`, `.search-cart-drawer`, `.favorites-panel`, `.compare-panel` و item/footerها | `shared-cart.css`, `commerce.css`; `main.js` + `commerce.js` | سبد خالی/پر، تعداد زیاد آیتم، پنل باز و scroll داخلی، responsive |
| modal ورود/ثبت‌نام سراسری | `#loginModal`, `.login-modal-form-panel`, `.login-close` در `_Panels.cshtml` | layout در `style.css`, `luxury-theme.css`, `industrial-system.css`; قواعد فرم در `shared-login.css`; تعامل در `main.js` | ورود/ثبت‌نام/OTP، viewport کوتاه موبایل، اسکرول داخلی، بستن و بازگشت focus |
| dialog ویرایش پروفایل | `.shared-login-modal.profile-edit-dialog` در `Views/Profile/Index.cshtml` | `shared-login.css` به‌علاوهٔ `account-page.css`; رفتار در View پروفایل | باز/بسته‌شدن native dialog، اعتبارسنجی و focus |
| فرم آدرس | `_AddressForm`, `#addressDialog` در Cart و dialog آدرس پروفایل | `checkout.css`, `account-page.css`; `address-fields.js`, `iran-places.js`, `account-page.js` | شهر بدون استان، استان/شهر معتبر، validation و ذخیره؛ Checkout این فرم را ندارد و فقط آدرس انتخابی را پیش‌نمایش می‌کند |
| مزیت خرید مطمئن | `_PurchaseBenefits`, `.purchase-benefits` | `style.css` و overrideهای `industrial-system.css`; JS اختصاصی ندارد | صفحهٔ محصول و هر مسیر دیگری که partial را رندر می‌کند |
| لینک‌های مرتبط | `_RelatedLinks` | `commerce.css`, overrideهای `pages.css`; JS اختصاصی ندارد | صفحات service/editorial که partial را مصرف می‌کنند |
| کارت محصول | shared card markup و rendererهای store | `shared-product-card.css`, `commerce.css` | Home/Search/Related/Compare/Favorites؛ crop تصویر و عنوان طولانی |

این‌ها نقطهٔ شروع‌اند؛ selectorهای دقیق و حالت واقعی را با `rg` و Elements panel پیدا کنید. از فرض «یک partial = یک CSS» پرهیز کنید.

## ترتیب واقعی CSS

این فهرست از ترتیب فعلی [_Shop.cshtml](../../Shop/Views/Shared/_Shop.cshtml) استخراج شده است. sectionهای View دقیقاً در محل مشخص‌شده وارد می‌شوند؛ `Styles` یا `PageStyles` آخرین قانون cascade نیستند. مسیرها نسبت به `wwwroot/` هستند.

- `shop/vendor/fonts/fonts.css`
- `shop/vendor/bootstrap/bootstrap.rtl.min.css`
- `shop/vendor/bootstrap-icons/bootstrap-icons.min.css`
- `shop/vendor/owl.carousel/owl.carousel.min.css`
- `shop/vendor/owl.carousel/owl.theme.default.min.css`
- `shop/vendor/swiper/swiper-bundle.min.css`
- `shop/css/core/design-tokens.css`
- `shop/css/core/components.css`
- `shop/css/core/ui-core.css`
- `shop/css/layout/shared-header.css`
- `shop/css/layout/shared-account.css`
- `shop/css/layout/shared-cart.css`
- `shop/css/layout/shared-login.css`
- `shop/css/layout/shared-footer.css`
- `shop/css/layout/luxury-theme.css`
- `shop/css/core/site.css`
- `shop/css/layout/style.css`
- `shop/css/layout/shared-mega-menu.css`
- `shop/css/store/commerce.css`
- **section `Styles` از View**
- `shop/css/layout/mvc-layout.css`
- `shop/css/layout/mobile-drawer-compact.css`
- `shop/css/store/pagination.css`
- `shop/css/layout/electrical-theme.css`
- `shop/css/layout/industrial-system.css`
- `shop/css/layout/quick-links-menu.css`
- `shop/css/store/shared-product-card.css`
- **section `PageStyles` از View**
- `shop/css/layout/content-typography.css`
- `shop/css/layout/mobile-pages.css`
- **section `SearchStyles` از View**
- `shop/css/pages/search-mobile.css`
- `shop/css/layout/empty-state-system.css`
- `shop/css/layout/shared-surface-theme.css`
- `shop/css/layout/page-hero-imagery.css`
- `shop/css/layout/mega-menu-unified.css`

## ترتیب واقعی JavaScript

- `shop/js/core/ui-core.js`
- `shop/vendor/jquery/jquery.min.js`
- `shop/vendor/jquery/jquery-migrate.min.js`
- `shop/vendor/owl.carousel/owl.carousel.min.js`
- `shop/vendor/swiper/swiper-bundle.min.js`
- `shop/vendor/bootstrap/bootstrap.bundle.min.js`
- `shop/js/layout/main.js`
- `shop/js/core/form-validation.js`
- `shop/js/store/countries.js`
- `shop/js/store/catalog.js`
- `shop/js/store/commerce.js`
- **section `Scripts` از View**
- `shop/js/core/electrical-content.js`

`HNUI` پیش از مصرف‌کننده‌ها منتشر می‌شود؛ `countries.js` پیش از کاتالوگ و `commerce.js` بعد از کاتالوگ است. ترتیب اجرا، ready callback و guard عنصر را هم در script بررسی کنید؛ وجود فایل در مخزن، مصرف‌شدنش را ثابت نمی‌کند.

## هنگام تغییر پوسته

خانه و یک صفحهٔ داخلی، حرکت اشاره‌گر بین trigger و پنل، keyboard/Escape، بازگشت focus و drawer موبایل را بررسی کنید. برای مگامنو، مالکیت زمینهٔ سطوح و پنل نهایی در `mega-menu-unified.css` را هم ببینید. پس از جابه‌جایی منبع، این فهرست و [نقشهٔ صفحه‌ها](../architecture/routing.md) را در صورت تغییر View به‌روز کنید.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۲۱ اکتبر ۲۰۲۶.
