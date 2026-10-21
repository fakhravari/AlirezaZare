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

در دسکتاپ، مگامنوی دسته‌بندی خانه و صفحات داخلی یک قاب دیداری مشترک دارند: پنل کناری با عنوان «دسته‌بندی قطعات»، عرض ثابت ۲۵۰px، ردیف‌های هم‌اندازه و کارت‌های زیرشاخه با فاصلهٔ یکسان. جایگاه منو همچنان به زمینهٔ خودش وابسته است؛ خانه از پنل دسته‌بندی کنار hero باز می‌شود و صفحات داخلی از نوار ناوبری.

عنوان پنل زیرشاخه‌ها در هر دو مگامنو، تعداد گروه‌ها را از آرایهٔ همان دسته و تعداد محصول را از مسیر دسته‌بندی `HNProducts` می‌گیرد و این دو عدد را در badge جدا نشان می‌دهد. `HNProducts` فعلاً همان ۳۰ محصول نمونهٔ `catalog.json` است؛ شمار badgeها وضعیت این کاتالوگ را نشان می‌دهد، نه موجودی کامل فروشگاه.

در خانه section `Hero` داخل `header.hero#home` رندر می‌شود؛ این section جایگاه محتوای hero است و جزو ترتیب CSS/JS نیست.

لوگوی `content/identity/store-electrical-lockup.svg` در هدر فقط از عرض ۹۹۲px به بالا نمایش داده می‌شود؛ تا عرض ۹۹۱px، هدر فقط دکمهٔ منو، حساب و ابزارهای خرید/مقایسه/علاقه‌مندی را دارد. همان لوگوی کامل در `.mobile-menu-logo` بالای drawer موبایل، با عرض حداکثر ۱۹۲px و نسبت ۲۵۶:۷۲ جایگزین نوشتهٔ قدیمی «ن / ندا اولیائی» است و به خانه لینک می‌شود. دکمهٔ بستن در ستون جدا و جستجو در ردیف بعد قرار دارند. قواعد هدر در `shared-header.css` و قاب لوگوی drawer در `mobile-drawer-compact.css` هستند؛ فاصلهٔ سه دکمهٔ خرید/مقایسه/علاقه‌مندی در موبایل ۵px است.

پنل‌ها بیرون `#main-content` هستند. خطای z-index، قفل scroll یا اندازهٔ پنل را فقط به View صفحه نسبت ندهید. skip link به `div#main-content[tabindex="-1"]` می‌رود؛ هر View، main معنایی خودش را درون آن حفظ می‌کند. عنصر main تو در تو نسازید.

## اجزای مشترک

| Partial/component | markup یا نشانه | مالک CSS/JS اصلی | جاهایی که باید رگرسیون شوند |
|---|---|---|---|
| هدر | `_HomeHeader`, `_InnerHeader`, `_Header`؛ body کلاس home/inner | `shared-header.css`, `shared-mega-menu.css`, themeها؛ `layout/main.js` | خانه + یک inner page، دسکتاپ + موبایل، login/logout و search open |
| مگامنو | خانه و inner markup/variant متفاوت؛ inner selectorهایی مثل `.page-mega-sidebar` | `shared-mega-menu.css`, `mega-menu-unified.css`, `quick-links-menu.css`; باز/بسته و category در `header-navigation.js` | trigger → panel، keyboard/ArrowDown/Escape، فضای خالی هدر، شاخهٔ پیش‌فرض و breakpoint 992 |
| account popover/drawer هدر | `.shared-account-card`, `.shared-account-popover`, `.account-links` | `shared-account.css`; تعامل در `main.js` | کاربر خارج/وارد، desktop/mobile، Escape، focus و scroll |
| cart/favorites/compare panels | `.page-cart-panel`, `.search-cart-drawer`, `.favorites-panel`, `.compare-panel` و item/footerها | `shared-cart.css`, `commerce.css`; `main.js` + `commerce.js` | سبد خالی/پر، تعداد زیاد آیتم، پنل باز و scroll داخلی، responsive |
| modal ورود/ثبت‌نام سراسری | `#loginModal`, `.login-modal-form-panel`, `.login-close` در `_Panels.cshtml` | layout در `style.css`, `luxury-theme.css`, `industrial-system.css`; قواعد فرم در `shared-login.css`; تعامل در `main.js` | ورود/ثبت‌نام/OTP، viewport کوتاه موبایل، اسکرول داخلی، بستن و بازگشت focus |
| dialog ویرایش پروفایل | `.shared-login-modal.profile-edit-dialog` در `Views/Profile/Index.cshtml` | `shared-login.css` به‌علاوهٔ `account-page.css`; رفتار در View پروفایل | باز/بسته‌شدن native dialog، اعتبارسنجی و focus |
| فرم آدرس | `_AddressForm`, `#addressDialog` در Cart و dialog آدرس پروفایل | `checkout.css`, `account-page.css`; `address-fields.js`, `iran-places.js`, `account-page.js` | شهر بدون استان، استان/شهر معتبر، validation و ذخیره؛ Checkout این فرم را ندارد و فقط آدرس انتخابی را پیش‌نمایش می‌کند |
| مزیت خرید مطمئن | `_PurchaseBenefits`, `.purchase-benefits` | `style.css` و overrideهای `industrial-system.css`; JS اختصاصی ندارد | صفحهٔ محصول و هر مسیر دیگری که partial را رندر می‌کند |
| لینک‌های مرتبط | `_RelatedLinks` | `commerce.css`, overrideهای `pages.css`; JS اختصاصی ندارد | صفحات service/editorial که partial را مصرف می‌کنند |
| کارت محصول | `HNProductCard.render` در `store/product-card.js` | `shared-product-card.css`؛ [نقشهٔ مالکیت](../../tools/ui-owners.json) | Home/Search/Related/Favorites و قیمت Compare؛ crop تصویر و عنوان طولانی |
| فوتر موبایل | `_Footer` و `.footer-main > .col-6` | `mobile-pages.css`؛ فاصلهٔ بالای همهٔ ستون‌های لینک از gutter مشترک Bootstrap می‌آید | هم‌ترازی عنوان‌های هر ردیف، به‌ویژه «کمپانی و قوانین» و «راه‌های ارتباطی»؛ margin-top اختصاصی ستون آخر نگذارید |

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

ابتدا `_CatalogData`، JSON امن `script[type="application/json"]#storeCatalog` را رندر می‌کند.

- `shop/js/core/storage.js`
- `shop/js/store/cart.js`
- `shop/js/core/ui-core.js`
- `shop/vendor/jquery/jquery.min.js`
- `shop/vendor/jquery/jquery-migrate.min.js`
- `shop/vendor/owl.carousel/owl.carousel.min.js`
- `shop/vendor/swiper/swiper-bundle.min.js`
- `shop/vendor/bootstrap/bootstrap.bundle.min.js`
- `shop/js/store/catalog.js`
- `shop/js/store/product-card.js`
- `shop/js/layout/header-navigation.js`
- `shop/js/layout/main.js`
- `shop/js/core/form-validation.js`
- `shop/js/store/commerce.js`
- **section `Scripts` از View**
- `shop/js/core/electrical-content.js`

storage/cart factory پیش از HNUI، resolver پیش از renderer و renderer پیش از مصرف‌کننده‌ها منتشر می‌شوند. این ترتیب با `npm run check` کنترل می‌شود. کاتالوگ کشور در همان JSON مشترک است. `HNUI.mount(name, root, initialize)` اجرای تکراری مؤلفه روی همان root را متوقف می‌کند؛ وجود فایل در مخزن به معنی مصرف‌شدنش نیست.

منوهای برند، دسته و دسترسی سریع فقط با state صریح `is-brand-open`, `is-mega-open`, `is-quick-links-open` باز می‌شوند. hover/focus روی wrapper گسترده نباید منو را باز کند؛ pointer فقط روی trigger باز می‌کند و panel باز را نگه می‌دارد. controller مشترک منوی قبلی را می‌بندد، Escape را همراه بازگشت فوکوس مدیریت می‌کند و در تغییر breakpoint وضعیت را پاک می‌کند.

## هنگام تغییر پوسته

خانه و یک صفحهٔ داخلی، حرکت اشاره‌گر بین trigger و پنل، keyboard/Escape، بازگشت focus و drawer موبایل را بررسی کنید. برای مگامنو، مالکیت زمینهٔ سطوح و پنل نهایی در `mega-menu-unified.css` را هم ببینید. پس از جابه‌جایی منبع، این فهرست و [نقشهٔ صفحه‌ها](../architecture/routing.md) را در صورت تغییر View به‌روز کنید.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۲۱ اکتبر ۲۰۲۶.
