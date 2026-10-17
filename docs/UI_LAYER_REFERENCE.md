# مرجع کامل لایه‌های رابط کاربری

این سند مکمل [`DEVELOPER_ROADMAP.md`](DEVELOPER_ROADMAP.md) است و برای پیدا کردن مالک واقعی UI، ردیابی تداخل CSS/JavaScript و بازآزمایی صفحه‌ها نوشته شده است. این مرجع در **۱۸ اکتبر ۲۰۲۶** با ساختار و assetهای موجود در مخزن تطبیق داده شده است؛ نقشهٔ مسیرها، breakpointها و فایل‌ها snapshot هستند و پس از تغییر View یا asset باید دوباره بررسی شوند.

> این سند فهرست مفصل مالکیت و نقاط شروع جست‌وجو است، نه جایگزین خواندن markup و cascade واقعی. DOM نهایی، کلاس‌های state و stylesheet برنده را برای هر باگ در مرورگر تأیید کنید.

## ۱. نقشهٔ سریع پروژه

### مسیر اصلی رندر

```text
Shop/Program.cs
  → Controller و route
  → Model یا دادهٔ نمایشی
  → Razor View
  → Views/_ViewStart.cshtml
  → Views/Shared/_Shop.cshtml
      → body.home-page یا body.inner-page
      → _HomeHeader یا _InnerHeader → _Header
      → RenderBody (View صفحه)
      → _Footer و _Panels و back-to-top
      → CSS/JS عمومی + sectionهای صفحه
```

`_Panels` پس از محتوای View می‌آید، ولی خارج از ریشهٔ محتوای آن است؛ بنابراین overlay، drawer، z-index، scroll lock، هدر یا مگامنو را صرفاً در CSS صفحه جست‌وجو نکنید. هدر خانه و هدر صفحات داخلی هر دو به `_Header` می‌رسند، اما variant و markup آن‌ها ممکن است متفاوت باشد.

### آدرس‌های مهم

| چیزی که باید پیدا شود | مسیر |
|---|---|
| پیکربندی MVC و routeها | `Shop/Program.cs` |
| Controllerها | `Shop/Controllers/` |
| Viewها و partialها | `Shop/Views/` |
| Layout و ترتیب asset واقعی | `Shop/Views/Shared/_Shop.cshtml` |
| توکن‌ها و پایهٔ UI | `Shop/wwwroot/Shop/css/core/` |
| CSS پوسته و اجزای مشترک | `Shop/wwwroot/Shop/css/layout/` |
| CSS صفحه‌ها | `Shop/wwwroot/Shop/css/pages/` |
| CSS تجارت/کارت/صفحه‌بندی | `Shop/wwwroot/Shop/css/store/` |
| JavaScript عمومی | `Shop/wwwroot/Shop/js/core/`, `layout/`, `store/` |
| JavaScript صفحه‌ها | `Shop/wwwroot/Shop/js/pages/` |

`RouteOptions.LowercaseUrls` در `Shop/Program.cs` فعال است؛ URLهای تولیدشده را lowercase نگه دارید. نام کلاس/فایل‌های Controller و View به‌دلیل قرارداد ASP.NET ممکن است PascalCase باقی بماند. مقدارهای فعلی نام فروشگاه و اطلاعات تماس در بخش «هویت و اطلاعات تماس فعلی» از [`DEVELOPER_ROADMAP.md`](DEVELOPER_ROADMAP.md) ثبت شده‌اند؛ هنگام تغییر، viewهای مشترک، SEO و متن‌های JS را نیز جست‌وجو کنید.

## ۲. ترتیب واقعی بارگذاری و اثر آن

### CSS

ترتیب فعلی در `_Shop.cshtml` از زودتر به دیرتر:

1. فونت‌ها، Bootstrap RTL، آیکن‌ها و کتابخانه‌های vendor.
2. `css/core/design-tokens.css` → `components.css` → `ui-core.css`.
3. `css/layout/shared-header.css` → `shared-account.css` → `shared-cart.css` → `shared-login.css` → `shared-footer.css`.
4. `css/layout/luxury-theme.css` → `css/core/site.css` → `css/layout/style.css` → `shared-mega-menu.css`.
5. `commerce.css`.
6. `@section Styles` از View جاری.
7. `css/layout/mvc-layout.css` → `mobile-drawer-compact.css` → `css/store/pagination.css` → `electrical-theme.css` → `industrial-system.css` → `css/store/shared-product-card.css`.
8. `@section PageStyles` از View جاری.
9. `css/layout/content-typography.css` → `css/pages/search-mobile.css` → `css/layout/mobile-pages.css`.

قاعدهٔ مهم: CSS اختصاصی View در میانهٔ ترتیب است و `PageStyles` پس از `shared-product-card.css` می‌آید. `mobile-pages.css` آخرین stylesheet سراسری در layout فعلی است. `search-mobile.css` از layout بارگذاری می‌شود و selectorهای آن برای Search scope شده‌اند؛ لینک تکراری آن از View Search حذف شده است. `industrial-system.css` overrideهای زیادی دارد ولی آخرین فایل نیست؛ overrideهای `!important` نیز می‌توانند مستقل از ترتیب برنده شوند. وجود فایل در repo به معنی load شدنش نیست. ترتیب نهایی را در تب Network و DOM `<link>`ها تأیید کنید.

### JavaScript

ترتیب فعلی scriptها در انتهای `_Shop.cshtml`:

```text
core/ui-core.js
→ jquery, jquery-migrate, owl.carousel, swiper, bootstrap.bundle
→ layout/main.js
→ core/form-validation.js
→ store/catalog.js
→ store/commerce.js
→ @section Scripts از View
→ core/electrical-content.js
```

فایل صفحه به globalهای قبلی وابسته است؛ جابه‌جایی ترتیب می‌تواند startup را بشکند. نسخه‌های قدیمی و بدون ارجاع `page-mega-menu.js`، `page-panels.js`، `shared-login.js` و `site-layout.js` حذف شده‌اند؛ منطق فعال در `main.js` و partialهای Shared نگه داشته شده تا یک رفتار دو implementation نداشته باشد.

## ۳. طراحی و قرارداد CSS

### توکن‌های پایه

مرجع: `Shop/wwwroot/Shop/css/core/design-tokens.css`.

| نقش | توکن مرجع | مقدار فعلی |
|---|---|---|
| سرمه‌ای | `--color-navy` | `#102a43` |
| آبی-خاکستری | `--color-blue` | `#243b53` |
| زرد تأکیدی | `--color-gold` | `#f6c515` |
| زمینهٔ روشن | `--color-cream` | `#f5f7f8` |
| سطح | `--color-surface` | `#fff` |
| متن اصلی | `--color-ink` | `#202a33` |
| متن کمکی | `--color-muted` | `#65737d` |
| خط/بردر | `--color-line` | `#dee4e7` |

توکن‌های فاصله، container، radius و shadow نیز در همان فایل هستند؛ نام و مقدار را پیش از تعریف مقدار جدید بخوانید. aliasهای قدیمی را با نامشان تفسیر نکنید: در وضعیت فعلی `--cream` به طلایی وصل است، اما `--cream2` زمینهٔ روشن است. aliasهای `--industrial-*` را نیز trace کنید.

### مالکیت فایل‌های CSS

| فایل/خانواده | مالکیت معمول | نکتهٔ عیب‌یابی |
|---|---|---|
| `css/core/design-tokens.css` | رنگ، فاصله، radius، سایه، تایپوگرافی پایه | تنها محل افزودن توکن عمومی جدید؛ alias را نیز بررسی کنید |
| `css/core/components.css` | سطح/کارت/آیکن‌های عمومی مثل `.ui-surface-card`, `.ui-icon-tile` | قبل از ساخت کارت تکراری، امکان استفاده از component را بررسی کنید |
| `css/core/ui-core.css` | utilityهای UI، قفل اسکرول و رفتارهای قابل‌دسترسی مشترک | با قواعد overflow/z-index صفحه تداخل احتمالی دارد |
| `css/layout/shared-header.css` | toggle حساب، جست‌وجو/سبد هدر و وضعیت responsive | خانه/داخلی را جداگانه تست کنید |
| `css/layout/shared-mega-menu.css` | `.page-mega-*` و مگامنوی دسکتاپ صفحات داخلی | منوی خانه variant دیگری دارد؛ hover و فاصلهٔ اشاره‌گر را تست کنید |
| `css/layout/shared-account.css` | `.shared-account-*`, لینک‌ها و account popover/drawer | وضعیت `.open` و breakpoint موبایل مهم است |
| `css/layout/shared-cart.css` | پنل‌های cart/favorites/compare، ردیف‌ها، footer و total | چند پنل مشابه دارد؛ selectorها را با ID/root واقعی محدود کنید |
| `css/layout/shared-login.css` | استایل پایهٔ `.shared-login-modal` برای dialog ویرایش پروفایل و بخشی از قواعد داخلی فرم `#loginModal` | با layout modal سراسری یکی نیست؛ `#loginModal` در `_Panels.cshtml` است و استایل layout آن بین `style.css`, `luxury-theme.css` و `industrial-system.css` تقسیم شده؛ `.shared-login-overlay` فعلاً در Viewها markup متناظر ندارد |
| `css/layout/shared-footer.css` | footer مشترک | در همهٔ routeها اثر دارد |
| `css/layout/luxury-theme.css` | تزئینات و پوستهٔ عمومی/legacy | قواعد گسترده و media متعدد؛ اولین مالک ظاهری فرض نشود |
| `css/layout/style.css` | stylesheet قدیمی و حجیم، قواعد عمومی و responsive | فایل legacy بزرگ؛ قواعد هم‌نام را قبل از ادیت بشمارید |
| `css/layout/mvc-layout.css` | قاب و layout صفحه‌های MVC | پس از `@section Styles` بارگذاری می‌شود |
| `css/layout/mobile-drawer-compact.css` | فشرده‌سازی drawer موبایل | فقط تا 991px؛ ممکن است بر shared drawer غلبه کند |
| `css/layout/electrical-theme.css` | هویت/چیدمان تم برق و ابزار | در انتهای cascade عمومی است |
| `css/layout/industrial-system.css` | overrideهای صنعتی/سراسری و responsive | overrideهای زیادی دارد ولی آخرین فایل نیست؛ فایل مالک مؤلفه فرض نشود |
| `css/store/shared-product-card.css` | کارت‌های مشترک محصول | grid و ابعاد تصویر را در Search/خانه/Related بررسی کنید |
| `css/layout/content-typography.css` | یکدست‌سازی تایپوگرافی و خوانایی محتوای فارسی | layout-wide؛ بعد از CSSهای قبلی می‌آید، در صفحه‌های محتوایی و RTL بررسی شود |
| `css/pages/search-mobile.css` | ریزچیدمان و کنترل‌های Search در موبایل | در layout بارگذاری می‌شود؛ selectorهای فایل باید به Search محدود بمانند |
| `css/layout/mobile-pages.css` | اصلاحات responsive مشترک صفحه‌ها | آخرین stylesheet layout؛ scope هر selector را بررسی کنید |
| `css/store/commerce.css` | سبد، جزئیات محصول، variant/accessory، مقایسه و favorite | هم بخش‌های صفحه و هم تعامل‌های commerce را پوشش می‌دهد |
| `css/store/pagination.css` | `.site-pagination` | بعد از View CSS می‌آید |
| `css/pages/pages.css` | قواعد عمومی/قدیمی چند صفحه | به علت گستردگی، وجود selector عمومی را کنترل کنید |
| `css/pages/account-page.css` | Profile و زیربخش‌های آن | با `shared-account.css` که پنل header را مالک است اشتباه نشود |
| `css/pages/search.css` | Search | فایل صفحه‌ای؛ `main.search-main` ریشهٔ منطقی را پیدا کنید |
| `css/pages/magazine-luxury.css` | فهرست Magazine | با `article-details.css` برای مقاله یکی نیست |
| `css/pages/article-details.css` | مقالهٔ مجله | print و reading progress نیز در همین فایل پوشش داده شده‌اند |
| `css/pages/checkout.css` | Cart/Checkout و address dialog/form | با `commerce.css` برای قیمت/سبد تداخل دارد |
| `css/pages/brands.css`, `contact.css`, `tracking.css` | صفحهٔ متناظر | page-root را حفظ کنید |
| `css/pages/form-confirm-dialog.css` | تأیید فرم | فایل کوچک/minified؛ قواعد باز/بسته و overlay را جدا بررسی کنید |
| `/profile/edit` | route سازگاری؛ به `/profile` هدایت می‌شود | View و CSS مستقل ندارد؛ ویرایش از dialog پروفایل انجام می‌شود |

### breakpointهای مشاهده‌شده

این‌ها breakpointهای موجودند، نه یک سیستم breakpoint رسمی. مرزهای متداخل را فعلاً حذف یا جابه‌جا نکنید؛ قانون فعال را در viewport واقعی پیدا کنید. مقدارهای رایج: 380، 575، 640، 767/768 و 991/992 پیکسل؛ برخی فایل‌ها مرزهای دیگری دارند.

| فایل/گروه | مرزهای ثبت‌شده (px) |
|---|---|
| `core/design-tokens.css` | max 575 |
| `layout/shared-header.css` | max 767, 991 |
| `layout/shared-account.css` | max 991؛ reduced-motion |
| `layout/shared-footer.css` | max 575, 991 |
| `layout/shared-cart.css` | max 380, 991؛ min 992 |
| `layout/shared-login.css` | max 575 |
| `layout/shared-mega-menu.css` | min 992 |
| `layout/luxury-theme.css` | max 380, 575, 767, 991 |
| `layout/mvc-layout.css` | max 575, 767 |
| `layout/mobile-drawer-compact.css` | max 991 |
| `layout/electrical-theme.css` | max 991؛ min 992 |
| `layout/industrial-system.css` | max 380, 389, 420, 575, 640, 700, 767, 991, 991.98, 1199؛ min 992, 1200, 1480؛ بازه‌های 390–767 و 992–1199؛ همچنین max-height 640، hover/none و reduced-motion |
| `layout/style.css` | max 380, 575, 767, 991, 1199؛ min 768, 992 |
| `pages/account-page.css` | max 380, 430, 560, 575, 640, 767, 991 |
| `pages/search.css` | max 359, 575, 767, 1100 |
| `pages/magazine-luxury.css` | max 480, 575, 767, 991, 1199؛ min 768, 992, 1200 |
| `pages/checkout.css` | max 480, 520, 575, 991 |
| `pages/pages.css` | max 575, 640, 767, 991؛ min 768, 992, 1200 |
| `pages/brands.css` | max 575, 767, 991؛ reduced-motion |
| `pages/contact.css` | max 767, 991؛ min 768؛ reduced-motion |
| `pages/article-details.css` | max 767؛ print، screen و reduced-motion |
| `pages/tracking.css` | max 575, 991؛ reduced-motion |
| `store/commerce.css` | max 380, 390, 575, 700, 767؛ بازهٔ 768–1199؛ min 1200؛ reduced-motion |
| `store/shared-product-card.css` | max 389, 575, 991؛ بازهٔ 992–1199 |
| `store/pagination.css` | max 575 |
| `layout/content-typography.css` | media query ندارد؛ قواعد تایپوگرافی عمومی |
| `pages/search-mobile.css` | max 767, 359, 319؛ فایل در layout لود می‌شود و فقط selectorهای Search دارد |
| `layout/mobile-pages.css` | max 767, 520, 389, 575, 359 |

برای فهرست به‌روزتر: `rg -n "@media" Shop/wwwroot/Shop/css`. تعارض را با viewport browser و Styles/Computed تأیید کنید؛ جدول ممکن است با تغییر CSS قدیمی شود.

### قرارداد انتخاب selector و پیدا کردن مالک

- ابتدا class/ID/data-attribute را از Razor یا DOM زنده بردارید؛ سپس همهٔ مصرف‌ها و تعریف‌ها را پیدا کنید:

```powershell
rg -n "نام-کلاس|#نامId|data-نام" Shop/Views Shop/wwwroot/Shop/css Shop/wwwroot/Shop/js
```

- selector صفحه را زیر ریشهٔ همان View نگه دارید (`.search-main …`, `.journal-page …`, `#cartView …`) و selector مشترک را در CSS مشترک مؤلفه اصلاح کنید.
- `section`, `button`, `.card`, `h2`, `.flow-card` و attribute selector بدون ریشه می‌توانند صفحه‌های نامرتبط را تغییر دهند؛ global بودن را عمداً تأیید کنید.
- قبل از اضافه‌کردن قانون، در DevTools عنصر واقعی را انتخاب کنید و source file/line، specificity، `!important`, media condition و state class برنده را ثبت کنید.
- راه‌حل اول اصلاح یا حذف قانون متناقض در مالک اصلی است؛ از افزودن override انتهایی یا `!important` تازه برای پنهان‌کردن cascade پرهیز کنید.

## ۴. نقشهٔ صفحه به View، CSS، JS و landmark

CSS/JS عمومی layout و store که بالاتر آمده‌اند به همهٔ صفحات اعمال می‌شوند؛ ستون asset اختصاصی فقط sectionهای View را نشان می‌دهد.

| Route | Controller/View | CSS اختصاصی | JS صفحه | landmark/نقطهٔ تعامل مهم |
|---|---|---|---|---|
| `/` | `HomeController` → `Views/Home/Index.cshtml` | section اختصاصی ندارد | section اختصاصی ندارد | `body.home-page`, `header.hero#home`, `section.electrical-showcase`, `section.py-section`, اسلایدرها؛ header home را با inner مقایسه کنید |
| `/search` | `ProductController.Index` → `Views/Product/List.cshtml` | `pages/search.css`, `pages/search-mobile.css` | `pages/search.js` | `section.search-intro` → `main.search-main` → `section.results-area`; فیلتر/مرتب‌سازی، URL، صفحه‌بندی، حالت خالی و filter drawer |
| `/products/{slug}` که slug با `-{id}` تمام می‌شود (مسیرهای قبلی `/products/{id}` و `/product?id=…` با ۳۰۱ هدایت می‌شوند) | `ProductController.Details` → `Views/Product/Details.cshtml` | `pages/pages.css`; بخش فروشگاه در `store/commerce.css` | `page-content.js` به‌عنوان script عمومی صفحه | slug از `store/catalog.js` و لینک‌های مشترک در `layout/main.js`; `main.flow-main`, product view و gallery; variant، accessory، قیمت، cart، favorite/compare، feedback |
| `/cart` | `CartController` → `Views/Cart/Index.cshtml` | `pages/checkout.css`, `form-confirm-dialog.css`, `pages.css` | `form-confirm-dialog.js`, `address-fields.js`, `checkout.js`, `iran-places.js`, `page-content.js` | `main.flow-main`, `#cartView`, address/payment sections; empty/populated, quantity, coupon, address dialog |
| `/checkout` | `CheckoutController` → `Views/Checkout/Index.cshtml` | `checkout.css`, `pages.css` | `checkout.js`, `page-content.js` | `main.flow-main.invoice-page`, invoice/address/payment؛ نمایش هر ویژگی/متعلقه در ردیف مستقل، قیمت واحد و جمع ردیف، سبد خالی/پر، روش پرداخت demo، modal آدرس |
| `/compare` | `CompareController` → `Views/Compare/Index.cshtml` | `pages.css` + `commerce.css` | `page-content.js` | `main.flow-main.compare-page`; zero/one/multiple product و حذف آیتم |
| `/favorites` | `FavoritesController` → `Views/Favorites/Index.cshtml` | `pages.css` + `commerce.css` | `page-content.js` | `main.flow-main`, `#favoritesView`; صفر/چند کارت و حذف favorite |
| `/profile`, `/profile/orders`, `/profile/addresses`, `/profile/favorites` | `ProfileController` → `Views/Profile/Index.cshtml` | `account-page.css`, `checkout.css`, `pages.css` | `account-page.js`, `address-fields.js`, `iran-places.js`, `orders-pagination.js`, `page-content.js` | `main.flow-main`, `section.account-section`; route/hash بخش فعال، addresses، order pagination و وضعیت خالی |
| `/profile/edit` | Controller به `/profile` هدایت می‌کند | — | — | مسیر سازگاری؛ صفحهٔ جداگانه ندارد |
| `/magazine` | `MagazineController` → `Views/Magazine/Index.cshtml` | `magazine-luxury.css`, `pages.css` | `magazine.js` | `main.journal-page`, `article.journal-post`; category/filter/sort، query string، popstate و responsive floating nav |
| `/magazine/{slug}` | `MagazineController` → `Views/Magazine/Details.cshtml`؛ مسیر قدیمی `/magazine/details/{slug}` به مسیر جدید هدایت می‌شود | `article-details.css`, `pages.css` | `article-details.js`, `page-content.js` | `main.article-page`, `article.article-body`, `section.article-section`; TOC، progress، checklist، print/share |
| `/about` | `AboutController` → `Views/About/Index.cshtml` | `pages.css` | `page-content.js` | hero/breadcrumb و `page-section`; لینک/CTA و sectionهای داینامیک را بررسی کنید |
| `/brands` | `BrandsController` → `Views/Brands/Index.cshtml` | `brands.css`, `pages.css` | `page-content.js` | hero، فهرست برند و کارت‌ها؛ responsive grid |
| `/brands/{slug}` | `BrandsController.Details` → `Views/Brands/Details.cshtml`؛ اطلاعات پایه از `BrandCatalog` و editorial از `brand-editorial.json` | `pages.css`, `brands.css` | `brand-product-count.js` | hero برند، CTA فروشگاه، حوزهٔ فعالیت، راهنمای انتخاب و شمارش محصولات |
| `/contact` | `ContactController` → `Views/Contact/Index.cshtml` | `contact.css`, `form-confirm-dialog.css`, `pages.css` | `contact.js`, `form-confirm-dialog.js`, `page-content.js` | فرم contact و CTA؛ validation، submit demo و feedback |
| `/faq` | `FaqController` و View هم‌نام | `pages.css` | `page-content.js` | page sections/FAQ items؛ باز و بسته‌شدن و keyboard state را در DOM ببینید |
| `/shipping`, `/warranty` | Controller و View هم‌نام | `pages.css` | `page-content.js` | `main.flow-main`, کارت‌های service و `_RelatedLinks` |
| `/tracking` | `TrackingController` → `Views/Tracking/Index.cshtml` | `pages.css`, `tracking.css` | `page-content.js` | `main.flow-main`, tracking form/status؛ نتیجه واقعی سفارش فعال نیست |
| `/legal/privacy`, `/legal/terms`, `/legal/returns` | `LegalController` → `Views/Legal/` | `pages.css` | اختصاصی ندارد | `main.flow-main > article.flow-card > section.legal-section` |
| `/error` | `ErrorController` → `Views/Error/Index.cshtml` | اختصاصی ندارد | اختصاصی ندارد | `main.container.py-5`; پوستهٔ مشترک همچنان load می‌شود |

صفحه‌های ساخته‌شده با JavaScript ممکن است `innerHTML` جایگزین کنند؛ در این حالت selectorهای markup نهایی را هم در `commerce.js`/script صفحه پیدا کنید. Razor landmark به‌تنهایی همهٔ DOM را شرح نمی‌دهد.

## ۵. partialها و componentهای مشترک

| Partial/component | markup یا نشانه | مالک CSS/JS اصلی | جاهایی که باید رگرسیون شوند |
|---|---|---|---|
| هدر | `_HomeHeader`, `_InnerHeader`, `_Header`؛ body کلاس home/inner | `shared-header.css`, `shared-mega-menu.css`, themeها؛ `layout/main.js` | خانه + یک inner page، دسکتاپ + موبایل، login/logout و search open |
| مگامنو | خانه و inner markup/variant متفاوت؛ inner selectorهایی مثل `.page-mega-sidebar` | `shared-mega-menu.css`, `electrical-theme.css`, `industrial-system.css`; باز/بسته و category در `main.js` | hover trigger → pointer حرکت به panel، keyboard focus، شاخهٔ پیش‌فرض، breakpoint 992 |
| account popover/drawer هدر | `.shared-account-card`, `.shared-account-popover`, `.account-links` | `shared-account.css`; تعامل در `main.js` | کاربر خارج/وارد، desktop/mobile، Escape، focus و scroll |
| cart/favorites/compare panels | `.page-cart-panel`, `.search-cart-drawer`, `.favorites-panel`, `.compare-panel` و item/footerها | `shared-cart.css`, `commerce.css`; `main.js` + `commerce.js` | سبد خالی/پر، تعداد زیاد آیتم، پنل باز و scroll داخلی، responsive |
| modal ورود/ثبت‌نام سراسری | `#loginModal`, `.login-modal-form-panel`, `.login-close` در `_Panels.cshtml` | layout در `style.css`, `luxury-theme.css`, `industrial-system.css`; قواعد فرم در `shared-login.css`; تعامل در `main.js` | ورود/ثبت‌نام/OTP، viewport کوتاه موبایل، اسکرول داخلی، بستن و بازگشت focus |
| dialog ویرایش پروفایل | `.shared-login-modal.profile-edit-dialog` در `Views/Profile/Index.cshtml` | `shared-login.css` به‌علاوهٔ `account-page.css`; رفتار در View پروفایل | باز/بسته‌شدن native dialog، اعتبارسنجی و focus |
| فرم آدرس | `_AddressForm`, `#addressDialog` در Cart و dialog آدرس پروفایل | `checkout.css`, `account-page.css`; `address-fields.js`, `iran-places.js`, `account-page.js` | شهر بدون استان، استان/شهر معتبر، validation و ذخیره؛ Checkout این فرم را ندارد و فقط آدرس انتخابی را پیش‌نمایش می‌کند |
| مزیت خرید مطمئن | `_PurchaseBenefits`, `.purchase-benefits` | `style.css` و overrideهای `industrial-system.css`; JS اختصاصی ندارد | صفحهٔ محصول و هر مسیر دیگری که partial را رندر می‌کند |
| لینک‌های مرتبط | `_RelatedLinks` | `commerce.css`, overrideهای `pages.css`; JS اختصاصی ندارد | صفحات service/editorial که partial را مصرف می‌کنند |
| کارت محصول | shared card markup و rendererهای store | `shared-product-card.css`, `commerce.css` | Home/Search/Related/Compare/Favorites؛ crop تصویر و عنوان طولانی |

این‌ها نقطهٔ شروع‌اند؛ selectorهای دقیق و حالت واقعی را با `rg` و Elements panel پیدا کنید. از فرض «یک partial = یک CSS» پرهیز کنید.

## ۶. قرارداد JavaScript، state و داده

### API مشترک `window.HNUI`

در `js/core/ui-core.js` با `Object.freeze` منتشر می‌شود. اعضای قابل استفادهٔ فعلی:

| API | نقش |
|---|---|
| `HNUI.STORAGE_KEYS` | کلیدهای پایهٔ auth/cart |
| `HNUI.storage.get/set/remove/json` | خواندن/نوشتن مقاوم در برابر خطای browser storage؛ `set` برای favorite/compare رویداد list را dispatch می‌کند |
| `HNUI.ready(callback)` | اجرای callback پس از آماده‌شدن DOM |
| `HNUI.cart.read/write/add/update/remove/totals/renderOptions` | normalize مشترک، renderer مشترک ویژگی/متعلقات، validation ساده و مدیریت سبد؛ `write` رویداد cart را dispatch می‌کند |
| `HNUI.auth.isLoggedIn/login/logout/requireLogin` | وضعیت ورود نمایشی؛ `requireLogin(resume)` برای ورود نداشتن رویداد `hn:auth-required` می‌فرستد و callback را پس از ورود آزمایشی ادامه می‌دهد |
| `HNUI.scrollLock.lock/unlock` | شمارندهٔ قفل scroll با کلاس `ui-scroll-locked` |
| `HNUI.dialog` | helperهای dialog/focus/keyboard؛ جزئیات دقیق را در همان فایل بخوانید |
| `HNUI.enhance(root)` | بهبودهای عمومی DOM داخل root |
| `toPersianDigits`, `toEnglishDigits`, `formatNumber`, `escapeHtml`, `localizeVisibleDigits` | ارقام، قالب عدد، escape متن و بومی‌سازی نمایش |

برای تبدیل رقم‌های فارسی/عربی در validation، OTP، شمارهٔ تماس و کد تخفیف، فقط از `HNUI.toEnglishDigits` استفاده کنید؛ mapper محلی تازه نسازید.

### قرارداد رندر سبد

- همهٔ مسیرهای افزودن کالا باید از `HNUI.cart.add` عبور کنند تا آیتم با دادهٔ `window.HNProducts` نرمال و ویژگی‌های پیش‌فرض و خدمات الزامی یکسان اعمال شوند؛ برای پنل هدر state جداگانه نسازید.
- پنل سبد هدر، صفحهٔ سبد و فاکتور checkout برای نمایش ویژگی‌ها و متعلقات از `HNUI.cart.renderOptions(item)` استفاده می‌کنند. اگر markup یا برچسب‌ها تغییر کرد، renderer مشترک را اصلاح کنید، نه هر مصرف‌کننده را جداگانه.
- متعلقات اختیاریِ انتخاب‌نشده در نمایش و مبلغ نهایی وارد نمی‌شوند؛ موارد انتخاب‌شده با برچسب «الزامی» یا «اختیاری» نمایش داده می‌شوند. قیمت واحد را از جمع همان ردیف (قیمت واحد × تعداد) جدا نگه دارید.
- بعد از تغییر شکل داده یا renderer، هر سه سطح پنل هدر، صفحهٔ سبد و checkout را با کالای دارای ویژگی و متعلقه بررسی کنید؛ دادهٔ سبد محلی/دمویی است و جای سفارش سمت سرور را نمی‌گیرد.

### مالکیت JS و رفتارهای کلیدی

| فایل | مسئولیت فعلی | قبل از تغییر چه چیزی را دنبال کنیم |
|---|---|---|
| `js/core/ui-core.js` | storage، cart/auth helpers، eventها، lock/dialog، format/escape | قرارداد کلید/payload؛ fallback storage؛ accessibility |
| `js/layout/main.js` | header، پیشنهاد/جست‌وجوی اخیر، شمارنده‌ها، mega-menu، account/login/cart/list panels و تعامل عمومی | DOM واقعی partialها، listenerهای delegated، home/inner variant و panel state |
| `js/core/form-validation.js` | validation عمومی/delegated فرم | `data-*`، selector فرم، خطاهای inline و submit handler اختصاصی |
| `js/store/catalog.js` | کاتالوگ نمونه روی `window.HNProducts` | مدل محصول/قیمت/تصویر/variant؛ داده دمو است |
| `js/store/commerce.js` | جزئیات محصول، variant/accessory، قیمت، gallery، feedback، cart، coupon، compare و favorites rendering | selectorهای `#productView`, `#cartView`, `#favoritesView`, `#compareView`؛ event/storage و rendererهای HTML |
| `js/pages/search.js` | URL-backed filter/sort/page، نتایج و mobile filter drawer | query params، `popstate`, empty state و cart/list controls |
| `js/pages/account-page.js` | profile route/hash navigation، overview و CRUD آدرس | storage keys، modal، hashchange و `storage` event |
| `js/pages/checkout.js` | انتخاب آدرس، روش پرداخت و هماهنگی state سبد | روش پرداخت فعلاً demo است؛ Cart/Checkout هر دو consumer هستند |
| `js/pages/address-fields.js`, `iran-places.js` | dependent province/city selects و dataset استان‌ها | load order و markup field IDs/data |
| `js/pages/orders-pagination.js` | pagination نمایشی سفارش‌ها | URL state/popstate و empty/list state |
| `js/pages/magazine.js` | فیلتر/مرتب‌سازی و تاریخچهٔ URL مقاله‌ها | `history`, query string و back/forward |
| `js/pages/article-details.js` | print، فهرست مطالب، پیشرفت مطالعه و scroll/resize | sticky offset، passive listener و کاهش حرکت |
| `js/pages/contact.js` | اعتبارسنجی و submit نمایشی فرم contact | پیام status؛ ارسال backend واقعی را فرض نکنید |
| `js/pages/page-content.js` | share/copy، forms، CTA و pagination عمومی بعضی صفحه‌ها | script در چند route مشترک است؛ handler دوباره ثبت نشود |
| `js/pages/form-confirm-dialog.js` | dialog تأیید فرم | backdrop/ESC و focus; در Cart/Contact load می‌شود |
| `js/core/electrical-content.js` | ریزتعامل/محتوای عمومی برخی صفحه‌ها | پس از section Scripts اجرا می‌شود |

### localStorage: کلیدهای شناخته‌شده

کلیدهای زیر دمو و client-side هستند؛ persistence سرور یا دادهٔ قابل‌اعتماد محسوب نمی‌شوند. پیش از migration/پاک‌سازی، همهٔ مصرف‌کننده‌ها را با `rg -n "نام-کلید" Shop` پیدا کنید.

| کلید | مصرف/نکته |
|---|---|
| `hadi-norouzi-cart` | cart نرمال‌شده توسط `HNUI.cart` |
| `hn-demo-auth` | وضعیت ورود نمایشی (`1`/`true`) |
| `hn-favorites` | شناسه‌های favorite |
| `hn-compare` | شناسه‌های مقایسه |
| `hn-compare-demo-seeded` | علامت seed نمایشی اولیهٔ Compare |
| `hn-product-cache` | cache اطلاعات محصول برای رندر list/panel |
| `hn-header-recent-searches` | تاریخچهٔ پیشنهاد search در هدر |
| `hn-demo-coupon` | کد تخفیف demo |
| `hn-delivery-addresses` | فهرست آدرس‌های نمایشی |
| `hn-selected-address` | شناسهٔ آدرس انتخابی |
| `hn-payment-method-v2` | روش پرداخت انتخابی نمایشی |
| `hn-profile` | دادهٔ profile نمایشی در `Views/Profile/Index.cshtml` |
| `hn-product-${type}-${productId}` | کلید واقعی feedback در `commerce.js`؛ `type` برابر `reviews` یا `questions` است. seedهای نمایشی جداگانه با دادهٔ ذخیره‌شده ترکیب می‌شوند. |

ممکن است scriptهای دیگر کلید تازه اضافه کنند؛ این جدول هنگام تغییر باید با `rg -n "localStorage|storage\.(get|set|json)|hn-" Shop/wwwroot/Shop/js Shop/Views` تطبیق داده شود.

### رویدادهای سفارشی

| Event | payload فعلی | listenerهای مهم/اثر |
|---|---|---|
| `hn:cart-change` | `detail`: آرایهٔ cart نرمال‌شده | header/panel، commerce و checkout را sync می‌کند |
| `hn:cart-added` | `detail: { product, items, trigger }` | پنل cart ممکن است باز شود؛ focus trigger را حفظ کنید |
| `hn:auth-change` | `detail`: boolean | هدر و بخش account را تازه می‌کند |
| `hn:auth-required` | `detail: { resume: Function|null }` | بازکردن مدال ورود؛ callback پس از ورود آزمایشی اجرا می‌شود |
| `hn:lists-change` | بدون `detail` | شمارنده‌ها، favorite/compare button و صفحه‌ها را sync می‌کند |
| native `storage` | `key`, `newValue`, `oldValue` | همگام‌سازی tabهای دیگر؛ same-tab تغییرات باید از helper/event عبور کند |

هنگام افزودن event، یک source of truth حفظ کنید، payload را document کنید، handler را idempotent بنویسید و از dispatch مضاعف در View و shared JS جلوگیری کنید.

## ۷. دستورالعمل حل تداخل CSS/JS

### برای CSS/ظاهر

1. URL دقیق، عنصر، parentها، متن/کلاس قابل مشاهده و viewport را ثبت کنید.
2. route را تا View و partial دنبال کنید؛ اگر markup با JS رندر می‌شود، renderer مربوط را هم پیدا کنید.
3. selector را در کل repo جست‌وجو کنید و مالکیت/ترتیب فایل‌ها را با `_Shop.cshtml` تطبیق دهید.
4. در DevTools → **Computed** property مشکل‌دار را باز کنید؛ rule برنده، فایل/خط، specificity، خط‌خورده‌ها و media query فعال را یادداشت کنید.
5. classهای state (`open`, `active`, `selected`, `is-*`, `aria-expanded`) و style inline را کنترل کنید؛ تفاوت state اغلب به‌اشتباه «رنگ CSS» گزارش می‌شود.
6. قانون متناقض را در منبع مالک اصلاح کنید؛ selector را scoped و token-based نگه دارید.
7. hard refresh؛ Network → CSS request و URL نسخه‌دار؛ سپس home/inner، desktop/mobile و hover/focus را مقایسه کنید.

### برای JavaScript/تعامل

1. Network تأیید کند فایل اجرا شده؛ Console را برای error و اجرای زودهنگام بررسی کنید.
2. ترتیب script، وجود عنصر هنگام binding، `DOMContentLoaded`، optional chaining/guard و duplicate listener را بررسی کنید.
3. از Elements/Console وضعیت DOM را قبل و بعد از event مقایسه کنید؛ delegated handler ممکن است عنصر تازه‌ساخته را هدف گرفته باشد.
4. event name و `detail`, storage key/value، `storage` cross-tab، `history`/query params و renderer را دنبال کنید.
5. UI را در حالت empty، populated، invalid/malformed storage، open/closed و keyboard اجرا کنید؛ از پاک‌کردن storage کاربر بدون اطلاع خودداری کنید.

### جدول نشانه → محل شروع

| نشانه | اول بررسی شود |
|---|---|
| همهٔ صفحات رنگ/فونت/بردر ناسازگار دارند | `design-tokens.css`, `style.css`, `electrical-theme.css`, `industrial-system.css` و rule برنده |
| فقط خانه یا inner header خراب است | `_HomeHeader`/`_InnerHeader`, `HeaderVariant`, `body.home-page`/`inner-page` |
| مگامنو با حرکت pointer بسته می‌شود | gap بین trigger/panel، `pointerleave`, stacking/overlay، eventها در `main.js` و CSS min-width 992 |
| تغییر CSS صفحه اعمال نمی‌شود | load order بعد از `@section Styles`, selector specificity و cache |
| ظاهر با hover اولیه متفاوت است | کلاس active پیش‌فرض، hover/focus CSS، JS category initialization |
| drawer/modal اسکرول نمی‌خورد | ارتفاع viewport، `overflow-y`, flex child min-height، scroll lock، backdrop و breakpoint |
| count/cart/list بین tabها فرق دارد | helper `HNUI`, `hn:*` eventها و native `storage` listener |
| route فقط گاهی render خراب دارد | URL/hash/query/popstate، localStorage demo و script صفحه |
| عکس crop/اندازه متفاوت است | container aspect ratio، `object-fit/object-position` و CSS card owner |

## ۸. چک‌لیست رگرسیون responsive و دسترس‌پذیری

برای تغییر صفحه‌ای، route هدف + حداقل یک consumer مشترک را بررسی کنید. برای component مشترک، همهٔ مصرف‌های اصلی را با matrix زیر پوشش دهید:

| محور | حالت‌هایی که باید تست شوند |
|---|---|
| Viewport | 360–390px موبایل، 575–640px، 768px، 992px، 1200px+؛ در breakpoint مشکوک یک px دو طرف مرز |
| State داده | خالی، یک آیتم، چند آیتم/عنوان بلند، خطا/ورودی نامعتبر، دادهٔ ذخیره‌شدهٔ قدیمی |
| تعامل | pointer hover، click، keyboard Tab/Enter/Escape، focus-visible، close و بازگشت focus |
| نمایش | overflow افقی، scroll داخلی modal/drawer، clipping، z-index، image fit، contrast و متن طولانی فارسی/RTL |
| Runtime | Network/CSS/JS load، Console بدون خطای مرتبط، تغییر tab/storage و back/forward برای URL-driven UI |
| Motion/print | `prefers-reduced-motion`; print برای مقاله‌ای که دکمه/stylesheet چاپ دارد |

در بررسی بصری، breakpointها را از کد actual استخراج کنید؛ 360/390/768/1024/1440 فقط smoke widths هستند و همهٔ مرزهای CSS را پوشش نمی‌دهند. از DevTools device emulation برای viewport استفاده کنید، اما تعامل واقعی pointer/touch را هم در صورت نیاز بررسی کنید.

## ۹. وضعیت فعلی و بدهی‌های شناخته‌شده

- CSSها هنوز با مالکیت کاملاً انحصاری/namespace جدا نشده‌اند؛ `style.css` و `industrial-system.css` بزرگ و دارای overrideهای چندمرحله‌ای‌اند.
- توکن‌های legacy و canonical هم‌زمان حضور دارند؛ نام alias همیشه معنای ظاهری مورد انتظار را نمی‌رساند.
- مرزهای responsive هم‌پوشان‌اند و در چند فایل مقدارهای 380/389/390، 991/991.98/992 و مرزهای صفحه‌ای وجود دارد.
- بعضی CSS/JSها در مخزن هستند ولی از layout جاری load نمی‌شوند؛ قبل از فعال‌سازی یا حذف، ارجاع و runtime را بررسی کنید.
- `/profile/edit` فقط مسیر سازگاری است و به `/profile` هدایت می‌شود؛ View و CSS مستقل ندارد.
- catalog، profile/auth، favorite/compare، address، coupon و payment عمدتاً demo/localStorage هستند؛ به‌عنوان backend واقعی معرفی نشوند.
- solution پروژهٔ تست مستقل ندارد؛ مستندسازی/بازبینی runtime جای ادعای تست خودکار را نمی‌گیرد.
- سند باید با هر تغییر route، partial، ترتیب asset، event یا storage key به‌روز شود؛ اطلاعات منسوخ از راهنمای «کامل» بدتر است.

## ۱۰. فرمان‌های روزمرهٔ جست‌وجو و بررسی

```powershell
git status --short
rg -n "نام-کلاس|#نامId|data-نام" Shop/Views Shop/wwwroot/Shop/css Shop/wwwroot/Shop/js
rg -n "@media" Shop/wwwroot/Shop/css
rg -n "localStorage|storage\.(get|set|json)|hn:" Shop/wwwroot/Shop/js Shop/Views
rg -n "<link|<script|RenderSection|RenderBody" Shop/Views/Shared/_Shop.cshtml
dotnet build Shop.sln
```

برای تغییر CSS/JS، ابتدا diff و `git status` را بررسی کنید، فقط فایل مالک را تغییر دهید، `git diff --check` اجرا کنید و سپس صفحهٔ مرتبط را با matrix این سند بررسی کنید. build برای تغییر Razor/C# مناسب است؛ build موفق به‌تنهایی رفتار بصری یا JS را ثابت نمی‌کند.
