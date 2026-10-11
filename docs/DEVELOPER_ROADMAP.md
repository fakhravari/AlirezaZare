# رودمپ توسعه و نقشهٔ پروژه

این سند نقطهٔ شروع هر توسعه‌دهنده یا ایجنتی است که باید ساختار پروژه، محل کد هر قابلیت و مسیر امن اعمال تغییر را بفهمد. این نقشه وضعیت فعلی مخزن را توصیف می‌کند؛ موارد بخش «رودمپ پیشنهادی» هدف‌های آینده‌اند، نه قابلیت‌های تکمیل‌شده.

## ۱. خلاصهٔ معماری فعلی

- راه‌حل: `Shop.sln`؛ پروژهٔ اجرایی: `Shop/Shop.csproj` با `Microsoft.NET.Sdk.Web` و هدف `net10.0`.
- الگو: ASP.NET Core MVC با Controller و Razor View؛ رابط کاربری فارسی و راست‌به‌چپ است.
- `Shop/Program.cs` فقط MVC، فایل‌های استاتیک و route پیش‌فرض را راه‌اندازی می‌کند. در ترکیب فعلی، دیتابیس، Entity Framework، سرویس دامنه، API یا احراز هویت سمت سرور ثبت نشده است.
- بسیاری از مسیرهای Controller صرفاً View و دادهٔ نمایشی برمی‌گردانند. کاتالوگ نمونه در `Shop/wwwroot/Shop/js/store/catalog.js` است؛ محتوا و تعامل‌های سبد، علاقه‌مندی، مقایسه و بخشی از حساب در مرورگر و `localStorage` مدیریت می‌شوند.
- نتیجه: رفتار فعلی فروشگاه را دمو/نمونه فرض کنید؛ دادهٔ `localStorage` را دادهٔ سرور، سفارش واقعی یا احراز هویت واقعی تلقی نکنید.

## ۲. نقشهٔ پوشه‌ها و مالکیت کد

| مسیر | مسئولیت |
|---|---|
| `Shop/Program.cs` | پیکربندی pipeline و route عمومی MVC |
| `Shop/Controllers/` | مسیرها و آماده‌سازی مدل‌های صفحه |
| `Shop/Models/` | مدل‌های صفحه/SEO و محتوای ثابت مقاله |
| `Shop/Views/` | Razor UI؛ صفحات محصول، پروفایل، خرید، محتوا و layoutهای مشترک |
| `Shop/Views/Shared/_Shop.cshtml` | سند HTML اصلی، ترتیب بارگذاری CSS/JS، هدر/فوتر/پنل‌ها |
| `Shop/Views/Shared/_Header.cshtml` | markup هدر مشترک؛ تفاوت صفحهٔ خانه و داخلی با `HeaderVariant` |
| `Shop/wwwroot/Shop/css/core/` | توکن‌های طراحی، کامپوننت‌های مشترک و پایهٔ UI |
| `Shop/wwwroot/Shop/css/layout/` | پوسته، هدر، پنل‌ها، موبایل و overrideهای layout |
| `Shop/wwwroot/Shop/css/pages/` | استایل اختصاصی صفحه‌ها |
| `Shop/wwwroot/Shop/css/store/` | کارت محصول، خرید، مقایسه و صفحه‌بندی فروشگاه |
| `Shop/wwwroot/Shop/js/core/` | زیرساخت مرورگر، اعتبارسنجی و محتوای مشترک |
| `Shop/wwwroot/Shop/js/layout/` | تعامل‌های هدر، منوها، پنل‌ها و layout |
| `Shop/wwwroot/Shop/js/store/` | دادهٔ کاتالوگ و رفتار فروشگاهی |
| `Shop/wwwroot/Shop/js/pages/` | رفتار مختص یک صفحه یا جریان کاری |
| `Shop/wwwroot/Shop/images/`, `videos/`, `vendor/` | رسانه‌ها، فونت‌ها و وابستگی‌های محلی؛ پیش از جایگزینی/حذف، تمام ارجاعات را بررسی کنید |

## ۳. مسیرهای اصلی و نقطهٔ شروع تغییر

| صفحه/قابلیت | مسیر متداول | Controller و View اصلی |
|---|---|---|
| خانه | `/` | `HomeController` → `Views/Home/Index.cshtml` |
| جستجو و فهرست محصول | `/Search` | `ProductController.Index` → `Views/Product/List.cshtml` |
| جزئیات محصول | `/Product?id=…` | `ProductController.Details` → `Views/Product/Details.cshtml`؛ رندر جزئیات و تعامل محصول عمدتاً در `js/store/commerce.js` |
| سبد و تسویه | `/Cart`, `/Checkout` | `CartController`, `CheckoutController` و Viewهای هم‌نام |
| مقایسه و علاقه‌مندی | `/Compare`, `/Favorites` | Controller و Viewهای هم‌نام؛ state محلی با کدهای store |
| پروفایل | `/Account` (هدایت به پروفایل), `/Profile`, `/Profile/Orders`, `/Profile/Addresses`, `/Profile/Favorites` | `AccountController`, `ProfileController`؛ صفحه‌ها از `Views/Account/Index.cshtml` استفاده می‌کنند و نوع بخش با مسیر/فرانت‌اند تعیین می‌شود |
| مجله | `/Magazine`, `/Magazine/Details/{slug}` | `MagazineController`، `Views/Magazine/`، دادهٔ مقاله در `Models/JournalArticles.cs` |
| محتوا/خدمات | `/About`, `/Brands`, `/Contact`, `/Faq`, `/Shipping`, `/Tracking`, `/Warranty` | Controller و View هم‌نام |
| قوانین | `/Legal/Privacy`, `/Legal/Terms`, `/Legal/Returns` | `LegalController` و Viewهای `Views/Legal/` |

Routeهای attribute-based مانند `Profile` و `Magazine` را کنار route پیش‌فرض در `Program.cs` بررسی کنید. مسیر و محتوای لینک‌ها ممکن است در JavaScript نیز normalize شود؛ قبل از تغییر URLها، Controller، Razor و `js/store/commerce.js` را با هم دنبال کنید.

## ۴. ترکیب Razor و layout

- `Views/_ViewStart.cshtml`، layout مشترک را روی `_Shop` تنظیم می‌کند.
- `_Shop.cshtml` ترتیب assetها را تعیین می‌کند و در خانه `_HomeHeader` و در سایر صفحه‌ها `_InnerHeader` را رندر می‌کند؛ هر دو partial به `_Header` مشترک می‌رسند.
- `_Header.cshtml` برای برخی اجزای هدر بر اساس `ViewData["HeaderVariant"]` markup متفاوت دارد. منوی خانه و منوی صفحات داخلی الزاماً DOM/CSS یکسان ندارند؛ تغییر ظاهری هدر باید هر دو حالت را بررسی کند.
- `_Panels.cshtml`، `_Footer.cshtml` و partialهایی مانند `_AddressForm.cshtml` را پیش از ساخت markup مشابه بررسی کنید.
- Viewهای صفحه می‌توانند `@section Styles` و `@section Scripts` تعریف کنند. ترتیب نهایی را از محل `RenderSectionAsync` در `_Shop.cshtml` بخوانید؛ بخش `Styles` پیش از بعضی layout stylesheetهای پایانی وارد می‌شود و ممکن است توسط آن‌ها override شود.

## ۵. قرارداد استایل و دیزاین‌سیستم

### توکن‌های مرجع

`Shop/wwwroot/Shop/css/core/design-tokens.css` مرجع اولیهٔ palette و مقادیر عمومی است. خانوادهٔ اصلی پروژه:

- سرمه‌ای: `--color-navy` / `--industrial-navy`
- آبی-خاکستری: `--color-blue` / `--industrial-steel`
- زرد/طلایی تأکیدی: `--color-gold` / `--industrial-yellow`
- سطح روشن: `--color-cream` / `--industrial-bg`
- سفید، متن، متن کمکی و خط: `--color-surface`, `--color-ink`, `--color-muted`, `--color-line`
- شعاع و سایه: `--radius-*`, `--shadow-card`, `--shadow-card-hover`

`core/components.css` اجزای پایه‌ای مانند `.ui-surface-card` و `.ui-icon-tile` را تعریف می‌کند. اگر یک جزء در چند صفحه مشترک است، ابتدا امکان اصلاح همین مؤلفه و توکن‌هایش را بسنجید.

### نقشهٔ لایه‌های CSS و مالکیت آن‌ها

برای پیدا کردن محل درست تغییر، از این ترتیب مالکیت استفاده کنید؛ فایلِ دیرتر لزوماً مالک اصلی نیست، فقط ممکن است قانون قبلی را override کند:

| لایه | مسیر/فایل | محدودهٔ مسئولیت |
|---|---|---|
| وابستگی | `wwwroot/Shop/vendor/` | Bootstrap RTL، آیکن‌ها، فونت‌ها و carouselها؛ فقط تغییر vendor با دلیل و بررسی نسخه/وابستگی‌ها |
| پایه و توکن | `css/core/design-tokens.css`, `components.css`, `ui-core.css`, `site.css` | رنگ، تایپوگرافی، resetهای محدود، الگوهای UI عمومی و پایهٔ سایت |
| اجزای مشترک | `css/layout/shared-header.css`, `shared-mega-menu.css`, `shared-account.css`, `shared-cart.css`, `shared-login.css`, `shared-footer.css` | اجزایی که در چند route تکرار می‌شوند؛ header خانه/داخلی را جداگانه بررسی کنید |
| پوسته و legacy | `css/layout/luxury-theme.css`, `style.css`, `mvc-layout.css`, `electrical-theme.css`, `mobile-drawer-compact.css`, `industrial-system.css` | پوستهٔ عمومی، responsive و سازگاری‌های قدیمی؛ در این گروه هم‌پوشانی و override زیاد است |
| مؤلفهٔ فروشگاه | `css/store/shared-product-card.css`, `commerce.css`, `pagination.css` | کارت محصول، جزئیات/سبد/مقایسه و صفحه‌بندی فروشگاهی |
| صفحه | `css/pages/*.css` | فرم‌ها و layout اختصاصی یک صفحه یا یک جریان؛ selector را به ریشهٔ همان صفحه محدود کنید |

در کد فعلی، نام‌گذاری کاملاً namespace‌شده و انحصاری نیست؛ کلاس عمومی مثل `.card`, `section`, `button` یا selectorهای بدون ریشه ممکن است در صفحه‌های دیگر هم اثر بگذارد. هنگام اصلاح، ابتدا ریشهٔ واقعی صفحه/کامپوننت را در Razor پیدا کنید و selector را زیر همان ریشه نگه دارید. برای کامپوننت چندصفحه‌ای، استایل را در فایل shared مالک آن قرار دهید؛ برای یک صفحه، در CSS اختصاصی همان صفحه. کلاس تازهٔ ریشه را فقط وقتی لازم است به markup اضافه کنید و نام آن را قابل جستجو و یکتا انتخاب کنید.

### ترتیب CSS فعلی

ترتیب واقعی لینک‌ها در `Views/Shared/_Shop.cshtml` چنین است: فونت/Bootstrap/آیکن و vendorها → `design-tokens.css` → `components.css` → `ui-core.css` → CSSهای shared → `luxury-theme.css` → `site.css` → `style.css` → `shared-mega-menu.css` → CSSهای store → `@section Styles` صفحه → `mvc-layout.css` → `mobile-drawer-compact.css` → `pagination.css` → `electrical-theme.css` → `industrial-system.css`.

`industrial-system.css` آخر بارگذاری می‌شود و overrideهای زیادی دارد؛ بنابراین حتی CSS صفحه که در `@section Styles` آمده ممکن است بعداً بی‌اثر شود. ترتیب cascade را با چهار عامل بررسی کنید: origin/importance، specificity، ترتیب بارگذاری، و در صورت وجود media query/شرط فعال. صرفاً بالا بردن specificity یا افزودن `!important` تعارض را پنهان می‌کند. راه‌حل ترجیحی اصلاح قانون در فایل مالک و حذف/محدودکردن overrideٔ متناقض است؛ از ساختن یک لایهٔ override تازه در انتهای فایل خودداری کنید.

### نقشهٔ صفحه به CSS

این جدول assetهای اختصاصی‌ای را نشان می‌دهد که در Razor صفحه وارد می‌شوند؛ همهٔ صفحه‌ها علاوه بر آن CSS عمومی layout/store را از `_Shop.cshtml` می‌گیرند.

| Route / صفحه | Razor | CSS اختصاصی صفحه |
|---|---|---|
| `/` خانه | `Views/Home/Index.cshtml` | CSS اختصاصیِ لینک‌شده در خود View ندارد؛ از لایه‌های عمومی و پوسته استفاده می‌کند |
| `/Search` جستجو | `Views/Product/List.cshtml` | `pages/search.css` |
| `/Product?id=…` جزئیات محصول | `Views/Product/Details.cshtml` | `pages/pages.css`؛ بخش عمدهٔ UI فروشگاهی از `store/commerce.css` است |
| `/Cart` سبد | `Views/Cart/Index.cshtml` | `pages/checkout.css`, `pages/form-confirm-dialog.css`, `pages/pages.css` |
| `/Checkout` تسویه | `Views/Checkout/Index.cshtml` | `pages/checkout.css`, `pages/pages.css` |
| `/Compare` مقایسه | `Views/Compare/Index.cshtml` | `pages/pages.css` |
| `/Favorites` علاقه‌مندی | `Views/Favorites/Index.cshtml` | `pages/pages.css` |
| `/Profile`, `/Profile/Orders`, `/Profile/Addresses`, `/Profile/Favorites` | `Views/Account/Index.cshtml` | `pages/account-page.css`, `pages/checkout.css`, `pages/pages.css` |
| `/Profile/Edit` | — | Controller فعلی به `/Profile` redirect می‌کند؛ `Views/Profile/Edit.cshtml` را مسیر فعال فرض نکنید |
| `/Magazine` | `Views/Magazine/Index.cshtml` | `pages/magazine-luxury.css`, `pages/pages.css` |
| `/Magazine/Details/{slug}` | `Views/Magazine/Details.cshtml` | `pages/article-details.css`, `pages/pages.css` |
| `/About` | `Views/About/Index.cshtml` | `pages/pages.css` |
| `/Brands` | `Views/Brands/Index.cshtml` | `pages/brands.css`, `pages/pages.css` |
| `/Contact` | `Views/Contact/Index.cshtml` | `pages/contact.css`, `pages/form-confirm-dialog.css`, `pages/pages.css` |
| `/Faq`, `/Shipping`, `/Warranty` | View هم‌نام | `pages/pages.css` |
| `/Tracking` | `Views/Tracking/Index.cshtml` | `pages/pages.css`, `pages/tracking.css` |
| `/Legal/Privacy`, `/Legal/Terms`, `/Legal/Returns` | View هم‌نام در `Views/Legal/` | `pages/pages.css` |

### روند عیب‌یابی تداخل CSS

1. با route و selector/متن قابل مشاهده شروع کنید؛ markup را در View/partial پیدا کنید و ببینید صفحه چه ریشه یا کلاسِ وضعیت فعالی دارد.
2. تمام تعریف‌ها و مصرف‌ها را جستجو کنید؛ برای مثال `rg -n "\.mega-menu|mega-menu" Shop/Views Shop/wwwroot/Shop/css Shop/wwwroot/Shop/js` و سپس فایل‌های بالادستی و پایین‌دستی جدول ترتیب را باز کنید.
3. در DevTools روی عنصر، `Computed` و `Styles` را ببینید: قانون برنده، فایل و خط، specificity، مقدار خط‌خورده، media query فعال و کلاس‌های حالت (`active`, `open`, `is-*`) را ثبت کنید.
4. همان مؤلفه را در خانه و صفحهٔ داخلی، و در عرض دسکتاپ و موبایل مقایسه کنید. `home-page`/`inner-page` و variant هدر می‌توانند markup یا selector متفاوت بسازند.
5. قانون را در فایل مالک اصلاح کنید؛ selector را به ریشهٔ صفحه/کامپوننت محدود کنید. از selectorهای سراسری مثل `section`, `button`, `.card` و `!important` جدید پرهیز کنید.
6. با hard refresh/cache-busting بررسی کنید که مرورگر asset جدید را گرفته؛ query-version دستی در بعضی Viewها با `asp-append-version` در layout فرق دارد.

الگوی تشخیص سریع:

| نشانه | اول کجا را بررسی کنیم |
|---|---|
| ظاهر در همهٔ صفحات اشتباه است | توکن‌های `core/` و CSS shared/layout |
| فقط خانه یا صفحات داخلی خراب است | `_HomeHeader` / `_InnerHeader`، `HeaderVariant` و کلاس‌های body |
| فقط یک route ایراد دارد | CSS اختصاصی همان View و ترتیب overrideهای بعد از `@section Styles` |
| فقط موبایل ایراد دارد | media queryهای فایل مالک، `mobile-drawer-compact.css` و overflow والدها |
| قانون در DevTools خط خورده/بی‌اثر است | specificity و فایل‌های دیرتر؛ قانون تکراری را اصلاح کنید، نه اینکه صرفاً قوی‌ترش کنید |
| تفاوت بعد از refresh باقی می‌ماند | URL نهایی asset، نسخهٔ cache، و اینکه CSS واقعاً از View/layout بارگذاری شده باشد |

### قانون‌های UI

- رنگ، بردر، شعاع، سایه و حالت hover/focus را از توکن مشترک بگیرید؛ رنگ وضعیت موفقیت/خطا/هشدار را فقط وقتی معنای واقعی دارد نگه دارید.
- کارت‌ها/پنل‌ها را تودرتو و بی‌دلیل با پس‌زمینه و سایهٔ جدا نسازید؛ مرز بین سطح صفحه، پنل و کارت باید واضح و کم‌تعداد باشد.
- هدر، مگامنو، پنل‌های سبد/حساب، modal و صفحه‌بندی اجزای مشترک‌اند؛ تغییر آن‌ها را روی خانه، صفحهٔ داخلی و موبایل ارزیابی کنید.
- تصاویر باید نسبت/قاب مشخص و `object-fit` مناسب داشته باشند؛ در موبایل عرض، overflow و فضای امن محتوا را بررسی کنید.
- فونت‌ها و آیکن‌ها از فایل‌های محلی زیر `wwwroot/Shop/vendor/` می‌آیند. برای آیکن جدید اول از Bootstrap Icons موجود استفاده کنید و وابستگی تازه وارد نکنید.

## ۶. قرارداد JavaScript و داده

- `js/core/ui-core.js`: namespace عمومی `window.HNUI`، دسترسی امن به storage، ابزارهای کمکی و رفتار عمومی.
- `js/core/form-validation.js`: اعتبارسنجی مشترک فرم‌ها؛ قواعد تازه را بدون بررسی فرم‌های مشابه دوباره تعریف نکنید.
- `js/store/catalog.js`: منبع کاتالوگ نمونه و اطلاعات نمایشی محصولات.
- `js/store/commerce.js`: رندر و تعامل کاتالوگ/محصول، سبد، علاقه‌مندی، مقایسه و برخی feedbackهای محلی؛ کلیدهای state و eventهای `hn:*` را پیش از تغییر بررسی کنید.
- `js/layout/main.js`: رفتارهای مشترک اصلی مانند هدر، جستجو، شمارنده‌ها و منوهای مشترک.
- `js/pages/`: منطق مختص صفحه؛ فایل و View متناظر را با هم پیدا کنید.
- `js/core/electrical-content.js`: ریزرفتارها و محتوای عمومی/تصاویر برخی صفحه‌های خدماتی و editorial؛ بعد از scriptهای صفحه اجرا می‌شود.
- قرارداد ذخیره‌سازی فعلی دمو است و `ui-core.js` برای خطا/عدم دسترسی به `localStorage` fallback دارد. ورودی ذخیره‌شده را هنگام رندر escape/validate کنید؛ دادهٔ مرورگر قابل‌اعتماد نیست.

### ترتیب اجرا و جریان داده

در `_Shop.cshtml` ترتیب scriptها این است: `core/ui-core.js` → jQuery و pluginها → Bootstrap → `layout/main.js` → `core/form-validation.js` → `store/catalog.js` → `store/commerce.js` → `@section Scripts` صفحه → `core/electrical-content.js`. کدهای بعدی ممکن است به globalهای قبلی وابسته باشند؛ جابه‌جایی ترتیب را تغییر معماری حساب کنید و بعد از آن همهٔ جریان‌های متأثر را بررسی کنید.

جریان معمول دادهٔ فعلی: `catalog.js` کاتالوگ نمونه را روی `window.HNProducts` می‌گذارد؛ `commerce.js` و صفحه‌هایی مثل Search آن را مصرف می‌کنند؛ تغییر سبد/لیست‌ها از helperهای `HNUI` و storage مرورگر عبور می‌کند؛ رویدادهای سفارشی `hn:*` برای تازه‌سازی شمارنده‌ها و viewهای باز استفاده می‌شوند. هنگام تغییر، نام کلید و payload واقعی event را در `ui-core.js`/مصرف‌کننده‌ها بخوانید؛ به حدس یا ایجاد مستقیم state موازی متکی نشوید. `storage` event نیز تغییر تب/پنجرهٔ دیگر را همگام می‌کند.

برای جلوگیری از باگ تعاملی، از event listener تکراری در `main.js`, `commerce.js` و View پرهیز کنید؛ ابتدا بررسی کنید listener مشترکی قبلاً ثبت نشده باشد. handler صفحه را تا حد ممکن به ریشهٔ همان صفحه محدود و در نبود markup با guard امن کنید. برای HTML پویا از escape helper موجود استفاده کنید و دادهٔ query string/localStorage را ورودی قابل‌اعتماد فرض نکنید.

### نقشهٔ صفحه به JavaScript

هر route به‌علاوهٔ scriptهای زیر، scriptهای عمومی layout و فروشگاه را نیز از `_Shop.cshtml` دریافت می‌کند.

| Route / صفحه | Script اختصاصیِ واردشده از View | منطق عمومی مکمل |
|---|---|---|
| `/` خانه | موردی در `@section Scripts` ندارد | `layout/main.js`, `store/commerce.js`, `core/electrical-content.js` |
| `/Search` | `pages/search.js` | `HNProducts` از `store/catalog.js`، تعامل‌های cart/list از `commerce.js` |
| `/Product?id=…` | `pages/page-content.js` | رندر/variant/accessory/gallery/feedback محصول در `store/commerce.js` |
| `/Cart` | `form-confirm-dialog.js`, `address-fields.js`, `checkout.js`, `iran-places.js`, `page-content.js` | cart state/render در `commerce.js` |
| `/Checkout` | `checkout.js`, `page-content.js` | cart state در `commerce.js` |
| `/Compare`, `/Favorites` | `page-content.js` | رندر و state مقایسه/علاقه‌مندی در `commerce.js` |
| `/Profile` و زیربخش‌ها | `account-page.js`, `address-fields.js`, `iran-places.js`, `orders-pagination.js`, `page-content.js` | پوسته و stateهای مشترک در `main.js`/`commerce.js` |
| `/Magazine` | `magazine.js` | — |
| `/Magazine/Details/{slug}` | `article-details.js`, `page-content.js` | — |
| `/Contact` | `contact.js`, `form-confirm-dialog.js`, `page-content.js` | اعتبارسنجی عمومی در `form-validation.js` |
| `/About`, `/Brands`, `/Faq`, `/Shipping`, `/Tracking`, `/Warranty` | `page-content.js` | محتوای مشترک در `electrical-content.js` |
| قوانین | script اختصاصی ندارد | فقط رفتارهای عمومی layout |

چهار فایل `layout/page-mega-menu.js`, `page-panels.js`, `shared-login.js`, `site-layout.js` در مسیر assetهای فعلی از `_Shop.cshtml` یا Viewهای صفحه مستقیم ارجاع ندارند. قبل از افزودن قابلیت به آن‌ها، مصرف واقعی را با جستجوی نام فایل/توابع، partialها و درخواست‌های Network تأیید کنید؛ وجود فایل به معنای اجرای آن نیست. رفتارهای مشابه ممکن است در `layout/main.js` قرار گرفته باشند.

## ۷. روش کار روی قابلیت‌ها

1. نیاز را به صفحه/مسیر، حالت‌ها (خالی، پر، خطا، loading، موبایل) و رفتار مورد انتظار خرد کنید.
2. `git status --short` و `git diff` را ببینید؛ تغییرات قبلی کاربر را حفظ کنید.
3. از route به Controller، مدل/منبع داده، View، CSS و JS مسیر کامل را دنبال کنید؛ برای UI مشترک، خانه و صفحات داخلی را جداگانه بررسی کنید.
4. پیش از تغییر داده یا state، مشخص کنید منبع حقیقت سمت سرور است یا demo/localStorage. برای افزودن persistence واقعی، backend/API/validation/auth لازم است؛ تغییر صرفاً فرانت‌اند آن را واقعی نمی‌کند.
5. تغییر را در فایل مالک پیاده کنید، نه در یک override عمومی که صفحات نامرتبط را عوض کند.
6. فقط اعتبارسنجی متناسب را اجرا کنید و محدودیت‌ها/عدم امکان بررسی زنده را صریح گزارش دهید.

### چک‌لیست اختصاصی رفع تداخل CSS/JS

- **CSS:** selector → markup و ریشه → همهٔ تعریف‌ها → ترتیب واقعی layout/View → قانون برنده در DevTools → اصلاح مالک اصلی → بررسی home/inner و mobile/desktop.
- **JS:** کنترل کنید فایل واقعاً load شده (Network/Console) → ترتیب وابستگی‌ها → وجود element و guard → listenerهای تکراری → event و payload → storage/state → خطای Console.
- **بازآزمایی:** یک حالت عادی، یک حالت خالی/بدون داده، یک تعامل اصلی، و viewport موبایل و دسکتاپ را بررسی کنید. تغییر shared را حداقل روی یک صفحهٔ دیگرِ مصرف‌کننده هم کنترل کنید.
- اگر رفتار با دادهٔ قبلی مرورگر فرق دارد، فقط پس از ثبت نتیجه از storage پاک‌شده/پروفایل تمیز برای مقایسه استفاده کنید؛ دادهٔ کاربر را بی‌اطلاع حذف نکنید.

## ۸. راه‌اندازی و اعتبارسنجی

پیش‌نیاز: .NET SDK سازگار با `net10.0`.

```powershell
dotnet restore Shop.sln
dotnet build Shop.sln
dotnet run --project Shop/Shop.csproj --no-launch-profile --urls http://localhost:5100
```

درخواست‌های صفحه‌ای نمونه برای بررسی دستی: `/`, `/Search`, `/Product?id=1`, `/Cart`, `/Checkout`, `/Compare`, `/Profile`, `/Profile/Orders`, `/Profile/Favorites`, `/Magazine`.

- در تغییر CSS/JS دست‌کم صفحهٔ هدف، یک صفحهٔ دیگر با همان component مشترک و اندازهٔ موبایل را بررسی کنید.
- در تغییر route/controller مدل، `dotnet build Shop.sln` را اجرا کنید.
- در تغییر JS، syntax و حالت‌های no-data / malformed storage را بررسی کنید.
- پروژه فعلاً test project ثبت‌شده در solution ندارد؛ این را به‌عنوان نبود تست خودکار اعلام کنید، نه «تست موفق».
- `asp-append-version="true"` برای بسیاری از assetها فعال است؛ برخی Viewها هنوز query version دستی دارند. اگر مرورگر CSS/JS قدیمی نشان داد، ابتدا URL و cache-busting واقعی asset را بررسی کنید.

## ۹. رودمپ پیشنهادی توسعه

### مرحلهٔ اول — تثبیت منبع UI

- کاهش cascade تکراری `style.css`/`industrial-system.css` و تعیین مالک واحد برای هر component مشترک.
- تکمیل توکن‌های semantic (surface, border, muted text, focus, status, icon) و مصرف آن‌ها در صفحه‌های فعلی.
- یکپارچه‌کردن قرارداد هدر، مگامنو، پنل‌ها و modal بین خانه و صفحات داخلی؛ سپس حذف استایل‌های تکراری فقط پس از visual regression.

### مرحلهٔ دوم — تثبیت داده و مرزهای دامنه

- تعریف مدل‌های دامنهٔ Product, Variant, Accessory, Cart, Order, Address, User و اعتبارسنجی سمت سرور.
- انتقال کاتالوگ از JavaScript به منبع backend و تعریف repository/service/API مناسب.
- جایگزینی شبیه‌سازی auth و سفارش با هویت و persistence واقعی، همراه با مجوزدهی و مدیریت خطا.
- مستندسازی قرارداد API و source-of-truth هر داده پیش از اتصال UI.

### مرحلهٔ سوم — کیفیت قابل‌تکرار

- افزودن تست‌های unit برای منطق دامنه و validation؛ integration tests برای routeها/API.
- افزودن smoke/e2e برای مسیر خرید، حساب و جستجو با viewport دسکتاپ و موبایل.
- بررسی دسترس‌پذیری (کیبورد، focus، screen reader، کنتراست)، RTL، overflow، و خطاهای console/network.
- ایجاد CI برای restore/build/test و کنترل کیفیت assetها.

### مرحلهٔ چهارم — نگهداری و بهینه‌سازی

- پس از ثبت پوشش و ارجاع‌ها، شناسایی فایل‌های بلااستفاده، duplicate/vendorها و استایل‌های legacy؛ حذف مرحله‌ای و قابل برگشت.
- بهینه‌سازی تصویر/فونت/ویدئو، caching و بارگذاری تنبل بر پایهٔ اندازه‌گیری واقعی.
- افزودن logging/monitoring و سیاست‌های امن production (secrets, headers, error handling) هم‌زمان با backend واقعی.

## ۱۰. چک‌لیست handoff

- کدام route و لایه‌ها تغییر کرده‌اند؟
- کدام صفحات مشترک و viewportها بررسی شده‌اند؟
- داده واقعی است یا demo/localStorage؟
- چه build/test/validation اجرا شد و نتیجه چه بود؟
- آیا تغییر یا ریسک باقی‌مانده‌ای برای مرحلهٔ بعد هست؟
