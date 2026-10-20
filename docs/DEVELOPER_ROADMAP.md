# رودمپ توسعه و نقشهٔ پروژه

این نقشه راهنمای معماری و نقطهٔ شروع ردیابی هر قابلیت است. برای مسیر یادگیری، دسته‌بندی اسناد و خلاصهٔ سریع، [`README.md`](README.md) را ببینید. این سند وضعیت فعلی مخزن را توصیف می‌کند؛ موارد «رودمپ پیشنهادی» هدف آینده‌اند، نه قابلیت تکمیل‌شده. بازبینی معماری: **۱۸ اکتبر ۲۰۲۶**.

برای selectorها، breakpointها، landmarkهای route، state/eventهای JavaScript و عیب‌یابی UI، مرجع مکمل [`UI_LAYER_REFERENCE.md`](UI_LAYER_REFERENCE.md) را بخوانید. برای metadata و structured data به [`SEO_METADATA.md`](SEO_METADATA.md) مراجعه کنید. این جزئیات snapshot هستند و باید همراه تغییر معماری/asset به‌روز بمانند.

## ۱. خلاصهٔ معماری فعلی

- راه‌حل: `Shop.sln`؛ پروژهٔ اجرایی: `Shop/Shop.csproj` با `Microsoft.NET.Sdk.Web` و هدف `net10.0`.
- الگو: ASP.NET Core MVC با Controller و Razor View؛ رابط کاربری فارسی و راست‌به‌چپ است.
- `Shop/Program.cs` فقط MVC، فایل‌های استاتیک و route پیش‌فرض را راه‌اندازی می‌کند. در ترکیب فعلی، دیتابیس، Entity Framework، سرویس دامنه، API یا احراز هویت سمت سرور ثبت نشده است.
- بسیاری از مسیرهای Controller، View و محتوای فعلی را برمی‌گردانند. کاتالوگ ۳۰ قلمی ابزار در `Shop/wwwroot/shop/js/store/catalog.js` نگهداری می‌شود؛ قیمت‌ها (به میلیون تومان)، تخفیف‌ها، دیدگاه‌ها و موجودی از پایگاه دادهٔ فروش دریافت نمی‌شوند. توضیحات فنی در شش گروه کالایی مشترک‌اند و مشخصات مدل‌محور تا اتصال منبع دادهٔ معتبر نباید حدس زده شوند. فیلدهای روشن‌تر `powerSource` (منبع تغذیه) و `useCase` (مورد استفاده) هستند؛ `movement` و `strap` برای سازگاری با کد و داده‌های قدیمی فعلاً نگه داشته شده‌اند. محتوا و تعامل‌های سبد، علاقه‌مندی، مقایسه و بخشی از حساب در مرورگر و `localStorage` مدیریت می‌شوند.
- نتیجه: رفتار فعلی فروشگاه را دمو/نمونه فرض کنید؛ دادهٔ `localStorage` را دادهٔ سرور، سفارش واقعی یا احراز هویت واقعی تلقی نکنید.

### قرارداد مشترک سبد خرید

- `HNUI.cart` در `js/core/ui-core.js` منبع واحد نرمال‌سازی و ثبت آیتم سبد است؛ کارت خانه، جستجو، پیشنهادها، quick view و صفحهٔ دیتیل باید از `HNUI.cart.add` عبور کنند، نه اینکه دادهٔ موازی برای پنل هدر بسازند.
- هنگام نرمال‌سازی، محصول با شناسه‌اش در `window.HNProducts` پیدا می‌شود؛ ویژگی‌های پیش‌فرض و خدمات الزامی به پیکربندی افزوده می‌شوند. متعلقات اختیاریِ انتخاب‌نشده در state می‌مانند، اما در نمای سبد/مدال/فاکتور نشان داده نمی‌شوند و هزینه‌ای ندارند.
- `HNUI.cart.renderOptions(item)` تنها renderer مشترک ویژگی‌ها و متعلقات است و در پنل هدر، صفحهٔ سبد و فاکتور استفاده می‌شود. برچسب خدمات انتخاب‌شده فقط «الزامی» یا «اختیاری» است.
- قیمت و موجودی هنوز محلی/دمویی‌اند. هنگام تغییر منطق قیمت، بررسی کنید مبلغ گزینه و خدمت اجباری فقط یک‌بار به قیمت پایه اضافه شود و تغییر تعداد، جمع ردیف و جمع فاکتور را هماهنگ نگه دارد.

### هویت و اطلاعات تماس فعلی

| مورد | مقدار جاری | محل‌های اصلی مصرف |
|---|---|---|
| نام فروشگاه | ندا اولیائی | partialهای Header/Footer/Panels، محتوای صفحه‌ها، عنوان‌های SEO و متن‌های JavaScript |
| تلفن تماس | `03969455457` | هدر، فوتر، Contact، Tracking و پیام‌های پشتیبانی |
| ایمیل تماس | `neda.oliyai67@gmail.com` | `Views/Contact/Index.cshtml` |

این مقادیر در حال حاضر در چند محل به‌صورت literal تکرار شده‌اند و از تنظیمات مرکزی خوانده نمی‌شوند. هنگام تغییر هویت، با `rg` کل `Shop/` را برای نام، شماره، ایمیل و `tel:`/`mailto:` جست‌وجو کنید؛ لینک مستقیم `wa.me` را جداگانه با قالب بین‌المللی معتبر بررسی کنید.

## ۲. نقشهٔ پوشه‌ها و مالکیت کد

| مسیر | مسئولیت |
|---|---|
| `Shop/Program.cs` | پیکربندی pipeline و route عمومی MVC |
| `Shop/Controllers/` | مسیرها و آماده‌سازی مدل‌های صفحه |
| `Shop/Models/` | مدل‌های صفحه/SEO و محتوای ثابت مقاله |
| `Shop/Views/` | Razor UI؛ صفحات محصول، پروفایل، خرید، محتوا و layoutهای مشترک |
| `Shop/Views/Shared/_Shop.cshtml` | سند HTML اصلی، ترتیب بارگذاری CSS/JS، هدر/فوتر/پنل‌ها |
| `Shop/Views/Shared/_Header.cshtml` | markup هدر مشترک؛ تفاوت صفحهٔ خانه و داخلی با `HeaderVariant` |
| `Shop/wwwroot/shop/css/core/` | توکن‌های طراحی، کامپوننت‌های مشترک و پایهٔ UI |
| `Shop/wwwroot/shop/css/layout/` | پوسته، هدر، پنل‌ها، موبایل و overrideهای layout |
| `Shop/wwwroot/shop/css/pages/` | استایل اختصاصی صفحه‌ها |
| `Shop/wwwroot/shop/css/store/` | کارت محصول، خرید، مقایسه و صفحه‌بندی فروشگاه |
| `Shop/wwwroot/shop/js/core/` | زیرساخت مرورگر، اعتبارسنجی و محتوای مشترک |
| `Shop/wwwroot/shop/js/layout/` | تعامل‌های هدر، منوها، پنل‌ها و layout |
| `Shop/wwwroot/shop/js/store/` | دادهٔ کاتالوگ و رفتار فروشگاهی |
| `Shop/wwwroot/shop/js/pages/` | رفتار مختص یک صفحه یا جریان کاری |
| `Shop/wwwroot/shop/images/`, `videos/`, `vendor/` | رسانه‌ها، فونت‌ها و وابستگی‌های محلی؛ پیش از جایگزینی/حذف، تمام ارجاعات را بررسی کنید |

## ۳. مسیرهای اصلی و نقطهٔ شروع تغییر

| صفحه/قابلیت | مسیر متداول | Controller و View اصلی |
|---|---|---|
| خانه | `/` | `HomeController` → `Views/Home/Index.cshtml` |
| جستجو و فهرست محصول | `/search` | `ProductController.Index` → `Views/Product/List.cshtml` |
| جزئیات محصول | `/products/{slug}` که انتهای slug شامل `-{id}` است (مسیرهای قبلی `/products/{id}` و `/product?id=…` با ۳۰۱ هدایت می‌شوند) | `ProductController.Details` → `Views/Product/Details.cshtml`؛ ساخت slug در `store/catalog.js` و لینک‌دهی عمومی در `layout/main.js`؛ رندر جزئیات در `store/commerce.js` |
| سبد و تسویه | `/cart`, `/checkout` | `CartController`, `CheckoutController` و Viewهای هم‌نام |
| مقایسه و علاقه‌مندی | `/compare`, `/favorites` | Controller و Viewهای هم‌نام؛ state محلی با کدهای store |
| پروفایل | `/account` (هدایت سازگاری به پروفایل), `/profile`, `/profile/orders`, `/profile/addresses`, `/profile/favorites` | `AccountController` فقط alias سازگاری است؛ صفحه‌ها در `ProfileController` و `Views/Profile/Index.cshtml` رندر می‌شوند |
| مجله | `/magazine`, `/magazine/{slug}` (مسیر قبلی `/magazine/details/{slug}` حفظ و هدایت می‌شود) | `MagazineController`، `Views/Magazine/`، دادهٔ مقاله و اعداد ثابت در `Models/JournalArticles.cs` |
| برندها | `/brands`, `/brands/{slug}` | `BrandsController` و Viewهای فهرست/جزئیات؛ اطلاعات پایه در `BrandCatalog` و متن تحریریه در `wwwroot/shop/js/store/brand-editorial.json` |
| محتوا/خدمات | `/about`, `/contact`, `/faq`, `/shipping`, `/tracking`, `/warranty` | Controller و View هم‌نام |
| قوانین | `/legal/privacy`, `/legal/terms`, `/legal/returns` | `LegalController` و Viewهای `Views/Legal/` |
| خطا | `/error` | `ErrorController` → `Views/Error/Index.cshtml` |

`Program.cs` گزینهٔ `RouteOptions.LowercaseUrls = true` را فعال می‌کند؛ بنابراین URLهای تولیدشده باید lowercase باشند، حتی اگر بعضی attribute routeها در کد با حروف بزرگ نوشته شده باشند. مسیرهای attribute-based را کنار route پیش‌فرض بررسی کنید. مسیر و محتوای لینک‌ها ممکن است در JavaScript نیز normalize شود؛ قبل از تغییر URLها، Controller، Razor و `js/store/commerce.js` را با هم دنبال کنید.

## ۴. ترکیب Razor و layout

- `Views/_ViewStart.cshtml`، layout مشترک را روی `_Shop` تنظیم می‌کند.
- `_Shop.cshtml` ترتیب assetها را تعیین می‌کند و در خانه `_HomeHeader` و در سایر صفحه‌ها `_InnerHeader` را رندر می‌کند؛ هر دو partial به `_Header` مشترک می‌رسند.
- `_Header.cshtml` برای برخی اجزای هدر بر اساس `ViewData["HeaderVariant"]` markup متفاوت دارد. منوی خانه و منوی صفحات داخلی الزاماً DOM/CSS یکسان ندارند؛ تغییر ظاهری هدر باید هر دو حالت را بررسی کند.
- `_Panels.cshtml`، `_Footer.cshtml` و partialهایی مانند `_AddressForm.cshtml` را پیش از ساخت markup مشابه بررسی کنید.
- Viewهای صفحه می‌توانند `@section Styles` و `@section Scripts` تعریف کنند. ترتیب نهایی را از محل `RenderSectionAsync` در `_Shop.cshtml` بخوانید؛ بخش `Styles` پیش از بعضی layout stylesheetهای پایانی وارد می‌شود و ممکن است توسط آن‌ها override شود.

### درخت پوسته و partialهای مشترک

ساختار کلی رندر چنین است: `_ViewStart` → `_Shop` → یکی از `_HomeHeader`/`_InnerHeader` → `_Header` → محتوای View با `RenderBody` → `_Footer` و `_Panels`. `_Panels` بیرون از محتوای صفحه درج می‌شود؛ پس مشکل z-index، overlay، scroll lock یا عرض پنل ممکن است از پوسته باشد، نه از View مسیر فعلی.

| Partial | وظیفه و مصرف | مالکیت استایل/رفتار برای ردیابی |
|---|---|---|
| `_Header.cshtml` با `_HomeHeader` و `_InnerHeader` | هدر و ناوبری؛ markup مگامنو در خانه و صفحات داخلی متفاوت است | `layout/shared-header.css`, `shared-mega-menu.css`, `electrical-theme.css`, `industrial-system.css`; تعامل‌ها عمدتاً `js/layout/main.js` |
| `_Panels.cshtml` | منوی موبایل، ورود، سبد، حساب، علاقه‌مندی/مقایسه و quick view | CSSهای `layout/shared-*.css`, `luxury-theme.css`, `style.css`, `mobile-drawer-compact.css`, `industrial-system.css`; رفتار عمدتاً `js/layout/main.js` و `js/store/commerce.js`؛ فعال‌بودن فایل را در Network تأیید کنید |
| `_Footer.cshtml` | فوتر مشترک | `layout/shared-footer.css` و قواعد پوسته؛ معمولاً رفتار اختصاصی صفحه ندارد |
| `_AddressForm.cshtml` | فرم مشترک آدرس در پروفایل و سبد خرید؛ تسویه فقط آدرس انتخاب‌شده را پیش‌نمایش می‌کند و این فرم را رندر نمی‌کند | `pages/checkout.css`, `pages/account-page.css`; `js/pages/address-fields.js`, `iran-places.js` به‌علاوهٔ کد صفحه |
| `_PurchaseBenefits.cshtml` | بلوک مزیت/خرید مطمئن در خانه و مسیرهای فروشگاهی | قوانین `.purchase-benefits` در `layout/style.css` و overrideهای `industrial-system.css`; مالک JS ندارد |
| `_RelatedLinks.cshtml` | نوار لینک‌های مرتبط در چند صفحه | پایه در `store/commerce.css` و اصلاح‌های صفحه در `pages/pages.css`; مالک JS ندارد |

این جدول نقطهٔ شروع است، نه تضمین انحصاری‌بودن selector؛ برای هر تغییر، `rg` روی کلاس/ID واقعی markup را در تمام CSSها اجرا کنید.

## ۵. قرارداد استایل و دیزاین‌سیستم

### توکن‌های مرجع

`Shop/wwwroot/shop/css/core/design-tokens.css` مرجع اولیهٔ palette و مقادیر عمومی است. خانوادهٔ اصلی پروژه:

- سرمه‌ای: `--color-navy` / `--industrial-navy`
- آبی-خاکستری: `--color-blue` / `--industrial-steel`
- زرد/طلایی تأکیدی: `--color-gold` / `--industrial-yellow`
- سطح روشن: `--color-cream` / `--industrial-bg`
- سفید، متن، متن کمکی و خط: `--color-surface`, `--color-ink`, `--color-muted`, `--color-line`
- شعاع و سایه: `--radius-*`, `--shadow-card`, `--shadow-card-hover`
- نقش‌های معنایی: `--color-page`, `--color-content-surface`, `--color-text`, `--color-text-muted`, `--color-divider`, `--color-success`, `--color-danger`؛ فاصله‌ها از `--space-*` و متن ریز مشترک از `--font-size-caption`.

مقادیر کلیدی فعلی و معنای توکن canonical:

| نقش | توکن | مقدار فعلی |
|---|---|---|
| سرمه‌ای | `--color-navy` | `#102a43` |
| آبی-خاکستری | `--color-blue` | `#243b53` |
| تأکید زرد | `--color-gold` | `#f6c515` |
| زمینهٔ روشن | `--color-cream` | `#f5f7f8` |
| سطح / متن / متن کمکی / خط | `--color-surface`, `--color-ink`, `--color-muted`, `--color-line` | `#fff`, `#202a33`, `#65737d`, `#dee4e7` |

از توکن‌های `--color-*` به‌عنوان مرجع جدید استفاده کنید. aliasهای سازگاری قدیمی هم‌معنی ظاهری قابل‌حدس ندارند؛ برای نمونه `--cream` در فایل فعلی به `--color-gold` وصل است، درحالی‌که `--cream2` به زمینهٔ روشن وصل می‌شود. پیش از استفاده alias را در `design-tokens.css` دنبال کنید.

`core/components.css` اجزای پایه‌ای مانند `.ui-surface-card` و `.ui-icon-tile` را تعریف می‌کند. اگر یک جزء در چند صفحه مشترک است، ابتدا امکان اصلاح همین مؤلفه و توکن‌هایش را بسنجید.

### نقشهٔ لایه‌های CSS و مالکیت آن‌ها

برای پیدا کردن محل درست تغییر، از این ترتیب مالکیت استفاده کنید؛ فایلِ دیرتر لزوماً مالک اصلی نیست، فقط ممکن است قانون قبلی را override کند:

| لایه | مسیر/فایل | محدودهٔ مسئولیت |
|---|---|---|
| وابستگی | `wwwroot/shop/vendor/` | Bootstrap RTL، آیکن‌ها، فونت‌ها و carouselها؛ فقط تغییر vendor با دلیل و بررسی نسخه/وابستگی‌ها |
| پایه و توکن | `css/core/design-tokens.css`, `components.css`, `ui-core.css`, `site.css` | رنگ، تایپوگرافی، resetهای محدود، الگوهای UI عمومی و پایهٔ سایت |
| اجزای مشترک | `css/layout/shared-header.css`, `shared-mega-menu.css`, `shared-account.css`, `shared-cart.css`, `shared-login.css`, `shared-footer.css` | اجزایی که در چند route تکرار می‌شوند؛ `shared-cart.css` پنل سبد هدر را هم پوشش می‌دهد؛ header خانه/داخلی را جداگانه بررسی کنید |
| پوسته و legacy | `css/layout/luxury-theme.css`, `style.css`, `mvc-layout.css`, `electrical-theme.css`, `mobile-drawer-compact.css`, `industrial-system.css` | پوستهٔ عمومی، responsive و سازگاری‌های قدیمی؛ در این گروه هم‌پوشانی و override زیاد است |
| مؤلفهٔ فروشگاه | `css/store/shared-product-card.css`, `commerce.css`, `pagination.css` | کارت محصول، جزئیات/سبد/مقایسه و صفحه‌بندی فروشگاهی |
| صفحه | `css/pages/*.css` | فرم‌ها و layout اختصاصی یک صفحه یا یک جریان؛ selector را به ریشهٔ همان صفحه محدود کنید |

در کد فعلی، نام‌گذاری کاملاً namespace‌شده و انحصاری نیست؛ کلاس عمومی مثل `.card`, `section`, `button` یا selectorهای بدون ریشه ممکن است در صفحه‌های دیگر هم اثر بگذارد. هنگام اصلاح، ابتدا ریشهٔ واقعی صفحه/کامپوننت را در Razor پیدا کنید و selector را زیر همان ریشه نگه دارید. برای کامپوننت چندصفحه‌ای، استایل را در فایل shared مالک آن قرار دهید؛ برای یک صفحه، در CSS اختصاصی همان صفحه. کلاس تازهٔ ریشه را فقط وقتی لازم است به markup اضافه کنید و نام آن را قابل جستجو و یکتا انتخاب کنید.

### ترتیب CSS فعلی

ترتیب واقعی لینک‌ها در `Views/Shared/_Shop.cshtml` چنین است: فونت/Bootstrap/آیکن و vendorها → `design-tokens.css` → `components.css` → `ui-core.css` → CSSهای shared → `luxury-theme.css` → `site.css` → `style.css` → `shared-mega-menu.css` → `commerce.css` → `@section Styles` صفحه → `mvc-layout.css` → `mobile-drawer-compact.css` → `pagination.css` → `electrical-theme.css` → `industrial-system.css` → `quick-links-menu.css` → `shared-product-card.css` → `@section PageStyles` → `content-typography.css` → `mobile-pages.css` → `@section SearchStyles` → `search-mobile.css` → `empty-state-system.css` → `shared-surface-theme.css`.

`shared-surface-theme.css` آخرین stylesheet سراسری در layout فعلی است. `search-mobile.css` پس از `@section SearchStyles` بارگذاری می‌شود و selectorهای آن باید برای Search محدود بمانند؛ `empty-state-system.css` و `shared-surface-theme.css` نیز انتهای cascade مشترک را شکل می‌دهند. `shared-product-card.css` زودتر از `@section PageStyles` می‌آید. scope selectorها و routeهای مصرف‌کننده را پیش از تغییر بررسی کنید.

`industrial-system.css` همچنان overrideهای زیادی دارد، اما آخرین فایل CSS نیست. ترتیب cascade را با چهار عامل بررسی کنید: origin/importance، specificity، ترتیب بارگذاری، و در صورت وجود media query/شرط فعال. صرفاً بالا بردن specificity یا افزودن `!important` تعارض را پنهان می‌کند. راه‌حل ترجیحی اصلاح قانون در فایل مالک و حذف/محدودکردن overrideٔ متناقض است؛ از ساختن یک لایهٔ override تازه در انتهای فایل خودداری کنید.

### نقشهٔ صفحه به CSS

این جدول assetهای اختصاصی‌ای را نشان می‌دهد که در Razor صفحه وارد می‌شوند؛ همهٔ صفحه‌ها علاوه بر آن CSS عمومی layout/store را از `_Shop.cshtml` می‌گیرند.

| Route / صفحه | Razor | CSS اختصاصی صفحه |
|---|---|---|
| `/` خانه | `Views/Home/Index.cshtml` | CSS اختصاصیِ لینک‌شده در خود View ندارد؛ از لایه‌های عمومی و پوسته استفاده می‌کند |
| `/search` جستجو | `Views/Product/List.cshtml` | `pages/search.css`, `pages/search-mobile.css` |
| `/products/{slug}` جزئیات محصول؛ `{slug}` با `-{id}` تمام می‌شود | `Views/Product/Details.cshtml` | `pages/pages.css`؛ بخش عمدهٔ UI فروشگاهی از `store/commerce.css` است |
| `/cart` سبد | `Views/Cart/Index.cshtml` | `pages/checkout.css`, `pages/form-confirm-dialog.css`, `pages/pages.css` |
| `/checkout` تسویه | `Views/Checkout/Index.cshtml` | `pages/checkout.css`, `pages/pages.css`; ویژگی‌ها/متعلقات در فاکتور با renderer مشترک سبد نمایش داده می‌شوند و قیمت واحد از جمع همان ردیف جداست |
| `/compare` مقایسه | `Views/Compare/Index.cshtml` | `pages/pages.css` |
| `/favorites` علاقه‌مندی | `Views/Favorites/Index.cshtml` | `pages/pages.css` |
| `/profile`, `/profile/orders`, `/profile/addresses`, `/profile/favorites` | `Views/Profile/Index.cshtml` | `pages/account-page.css`, `pages/checkout.css`, `pages/pages.css` |
| `/profile/edit` | — | مسیر سازگاری است و به `/profile` redirect می‌کند؛ ویرایش حساب از dialog در `Views/Profile/Index.cshtml` انجام می‌شود |
| `/magazine` | `Views/Magazine/Index.cshtml` | `pages/magazine-luxury.css`, `pages/pages.css` |
| `/magazine/{slug}` | `Views/Magazine/Details.cshtml`؛ مسیر قدیمی `/magazine/details/{slug}` هدایت می‌شود | `pages/article-details.css`, `pages/pages.css` |
| `/about` | `Views/About/Index.cshtml` | `pages/pages.css` |
| `/brands` | `Views/Brands/Index.cshtml` | `pages/brands.css`, `pages/pages.css` |
| `/brands/{slug}` | `Views/Brands/Details.cshtml` | `pages/pages.css`, `pages/brands.css`؛ محتوای تحریریه از `wwwroot/shop/js/store/brand-editorial.json` |
| `/contact` | `Views/Contact/Index.cshtml` | `pages/contact.css`, `pages/form-confirm-dialog.css`, `pages/pages.css` |
| `/faq`, `/shipping`, `/warranty` | View هم‌نام | `pages/pages.css` |
| `/tracking` | `Views/Tracking/Index.cshtml` | `pages/pages.css`, `pages/tracking.css` |
| `/legal/privacy`, `/legal/terms`, `/legal/returns` | View هم‌نام در `Views/Legal/` | `pages/pages.css` |
| `/error` | `Views/Error/Index.cshtml` | CSS اختصاصی ندارد؛ از پوسته و قواعد عمومی استفاده می‌کند |

### landmarkهای ساختاری Viewها

این landmarkها ریشه‌های فعلی markup هستند و برای یافتن سریع محدودهٔ صفحه در DevTools یا `rg` به‌کار می‌روند؛ بخش‌های ساخته‌شده با JavaScript ممکن است پس از load به آن‌ها اضافه شوند.

| صفحه/گروه | landmarkهای اصلی در Razor |
|---|---|
| خانه | `section.electrical-showcase`, `section.py-section`, کارت‌ها و sliderهای محصول |
| جستجو | `section.search-intro` → `main.search-main` → `section.results-area` |
| جزئیات محصول | `main.flow-main`; بخش فروشگاهی و دادهٔ محصول را در `Views/Product/Details.cshtml` و rendererهای `commerce.js` با هم دنبال کنید |
| سبد | `main.flow-main`, `section.cart-address-section`, `section.cart-payment-section`; فرم آدرس partial مشترک است |
| تسویه | `main.flow-main.invoice-page`, کارت‌های invoice و فرم/انتخاب آدرس |
| مقایسه / علاقه‌مندی | `main.flow-main.compare-page` / `main.flow-main`; محتوای لیست از state فروشگاهی رندر می‌شود |
| پروفایل و زیربخش‌ها | `main.flow-main`, `section.account-section`, کارت‌های overview/order؛ route تعیین می‌کند کدام بخش فعال باشد |
| مجله | فهرست: `main.journal-page` و `article.journal-post`; مقاله: `main.article-page`, `article.article-body`, `section.article-section` |
| معرفی/برند/تماس/FAQ | hero و breadcrumb در Viewهای مربوط؛ محتوای اصلی عمدتاً `section.page-section` یا `main.flow-main`; Contact فرم و CTA جدا دارد |
| ارسال/رهگیری/ضمانت | `main.flow-main`; محتوای خدمات در کارت/فرم و `_RelatedLinks` مشترک |
| قوانین | `main.flow-main` → `article.flow-card` → `section.legal-section` |
| خطا | `main.container.py-5` با عنوان، توضیح و لینک بازگشت |
| ویرایش پروفایل | dialog در `Views/Profile/Index.cshtml`; `/profile/edit` فقط به `/profile` redirect می‌کند |

### روند عیب‌یابی تداخل CSS

1. با route و selector/متن قابل مشاهده شروع کنید؛ markup را در View/partial پیدا کنید و ببینید صفحه چه ریشه یا کلاسِ وضعیت فعالی دارد.
2. تمام تعریف‌ها و مصرف‌ها را جستجو کنید؛ برای مثال `rg -n "\.mega-menu|mega-menu" Shop/Views Shop/wwwroot/shop/css Shop/wwwroot/shop/js` و سپس فایل‌های بالادستی و پایین‌دستی جدول ترتیب را باز کنید.
3. در DevTools روی عنصر، `Computed` و `Styles` را ببینید: قانون برنده، فایل و خط، specificity، مقدار خط‌خورده، media query فعال و کلاس‌های حالت (`active`, `open`, `is-*`) را ثبت کنید.
4. همان مؤلفه را در خانه و صفحهٔ داخلی، و در عرض دسکتاپ و موبایل مقایسه کنید. `home-page`/`inner-page` و variant هدر می‌توانند markup یا selector متفاوت بسازند.
5. قانون را در فایل مالک اصلاح کنید؛ selector را به ریشهٔ صفحه/کامپوننت محدود کنید. از selectorهای سراسری مثل `section`, `button`, `.card` و `!important` جدید پرهیز کنید.
6. با hard refresh/cache-busting بررسی کنید که مرورگر asset جدید را گرفته؛ stylesheet و scriptهای View از `asp-append-version` استفاده می‌کنند و مسیرهای ثابت با `~/shop/` نوشته می‌شوند.

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
- فونت‌ها و آیکن‌ها از فایل‌های محلی زیر `wwwroot/shop/vendor/` می‌آیند. برای آیکن جدید اول از Bootstrap Icons موجود استفاده کنید و وابستگی تازه وارد نکنید.

## ۶. قرارداد JavaScript و داده

- `js/core/ui-core.js`: namespace عمومی `window.HNUI`، دسترسی امن به storage، ابزارهای کمکی و رفتار عمومی.
- `js/core/form-validation.js`: اعتبارسنجی مشترک فرم‌ها؛ قواعد تازه را بدون بررسی فرم‌های مشابه دوباره تعریف نکنید.
- `js/store/catalog.js`: منبع کاتالوگ محصولات و اطلاعات فعلی کالاها.
- `js/store/commerce.js`: رندر و تعامل کاتالوگ/محصول، سبد، علاقه‌مندی، مقایسه و برخی feedbackهای محلی؛ کلیدهای state و eventهای `hn:*` را پیش از تغییر بررسی کنید.
- `js/layout/main.js`: رفتارهای مشترک اصلی مانند هدر، جستجو، شمارنده‌ها و منوهای مشترک.
- `js/pages/`: منطق مختص صفحه؛ فایل و View متناظر را با هم پیدا کنید.
- `js/core/electrical-content.js`: ریزرفتارها و محتوای عمومی/تصاویر برخی صفحه‌های خدماتی و editorial؛ بعد از scriptهای صفحه اجرا می‌شود.
- قرارداد ذخیره‌سازی فعلی دمو است و `ui-core.js` برای خطا/عدم دسترسی به `localStorage` fallback دارد. ورودی ذخیره‌شده را هنگام رندر escape/validate کنید؛ دادهٔ مرورگر قابل‌اعتماد نیست.

### ترتیب اجرا و جریان داده

در `_Shop.cshtml` ترتیب scriptها این است: `core/ui-core.js` → jQuery و pluginها → Bootstrap → `layout/main.js` → `core/form-validation.js` → `store/catalog.js` → `store/commerce.js` → `@section Scripts` صفحه → `core/electrical-content.js`. کدهای بعدی ممکن است به globalهای قبلی وابسته باشند؛ جابه‌جایی ترتیب را تغییر معماری حساب کنید و بعد از آن همهٔ جریان‌های متأثر را بررسی کنید.

جریان معمول دادهٔ فعلی: `catalog.js` کاتالوگ نمونه را روی `window.HNProducts` می‌گذارد؛ `commerce.js` و صفحه‌هایی مثل Search آن را مصرف می‌کنند؛ تغییر سبد/لیست‌ها از helperهای `HNUI` و storage مرورگر عبور می‌کند؛ رویدادهای سفارشی `hn:*` برای تازه‌سازی شمارنده‌ها و viewهای باز استفاده می‌شوند. هنگام تغییر، نام کلید و payload واقعی event را در `ui-core.js`/مصرف‌کننده‌ها بخوانید؛ به حدس یا ایجاد مستقیم state موازی متکی نشوید. `storage` event نیز تغییر تب/پنجرهٔ دیگر را همگام می‌کند. همهٔ ورودی‌های افزودن محصول باید از `HNUI.cart.add` و normalize مشترک عبور کنند؛ نمایش گزینه‌ها در پنل هدر، سبد و فاکتور نیز فقط از `HNUI.cart.renderOptions` انجام می‌شود.

برای جلوگیری از باگ تعاملی، از event listener تکراری در `main.js`, `commerce.js` و View پرهیز کنید؛ ابتدا بررسی کنید listener مشترکی قبلاً ثبت نشده باشد. handler صفحه را تا حد ممکن به ریشهٔ همان صفحه محدود و در نبود markup با guard امن کنید. برای HTML پویا از escape helper موجود استفاده کنید و دادهٔ query string/localStorage را ورودی قابل‌اعتماد فرض نکنید.

### نقشهٔ صفحه به JavaScript

هر route به‌علاوهٔ scriptهای زیر، scriptهای عمومی layout و فروشگاه را نیز از `_Shop.cshtml` دریافت می‌کند.

| Route / صفحه | Script اختصاصیِ واردشده از View | منطق عمومی مکمل |
|---|---|---|
| `/` خانه | موردی در `@section Scripts` ندارد | `layout/main.js`, `store/commerce.js`, `core/electrical-content.js` |
| `/search` | `pages/search.js` | `HNProducts` از `store/catalog.js`، تعامل‌های cart/list از `commerce.js` |
| `/products/{slug}`؛ slug با `-{id}` تمام می‌شود | `pages/page-content.js` | خواندن شناسه از انتهای slug و رندر/variant/accessory/gallery/feedback در `store/commerce.js` |
| `/cart` | `form-confirm-dialog.js`, `address-fields.js`, `checkout.js`, `iran-places.js`, `page-content.js` | cart state/render در `commerce.js` |
| `/checkout` | `checkout.js`, `page-content.js` | cart state و `HNUI.cart.renderOptions` در `commerce.js`; لینک عنوان محصول و تفکیک قیمت واحد/جمع ردیف در `pages/checkout.css` |
| `/compare`, `/favorites` | `page-content.js` | رندر و state مقایسه/علاقه‌مندی در `commerce.js` |
| `/profile` و زیربخش‌ها | `account-page.js`, `address-fields.js`, `iran-places.js`, `orders-pagination.js`, `page-content.js` | پوسته و stateهای مشترک در `main.js`/`commerce.js` |
| `/magazine` | `magazine.js` | — |
| `/magazine/{slug}` | `article-details.js`, `page-content.js` | — |
| `/brands/{slug}` | `brand-product-count.js` | شمارش محصولات از کاتالوگ؛ محتوای تحریریه در View از فایل JSON خوانده می‌شود |
| `/contact` | `contact.js`, `form-confirm-dialog.js`, `page-content.js` | اعتبارسنجی عمومی در `form-validation.js` |
| `/about`, `/brands`, `/faq`, `/shipping`, `/tracking`, `/warranty` | `page-content.js` | محتوای مشترک در `electrical-content.js` |
| قوانین | script اختصاصی ندارد | فقط رفتارهای عمومی layout |
| `/error` | script اختصاصی ندارد | فقط رفتارهای عمومی layout |

نسخه‌های قدیمی `layout/page-mega-menu.js`, `page-panels.js`, `shared-login.js` و `site-layout.js` پس از تأیید نبود ارجاع در layout/View/scriptهای پروژه حذف شدند. پیاده‌سازی فعال header، mega-menu و پنل‌های حساب/سبد در `layout/main.js` و markup فعلی در partialهای Shared است؛ برای قابلیت تازه، همان مسیر فعال را توسعه دهید و نسخهٔ موازی نسازید.

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

درخواست‌های صفحه‌ای نمونه برای بررسی دستی: `/`, `/search`, `/products/cordless-hammer-drill-1`, `/cart`, `/checkout`, `/compare`, `/profile`, `/profile/orders`, `/profile/favorites`, `/magazine`, `/brands/ronix`.

- در تغییر CSS/JS دست‌کم صفحهٔ هدف، یک صفحهٔ دیگر با همان component مشترک و اندازهٔ موبایل را بررسی کنید.
- در تغییر route/controller مدل، `dotnet build Shop.sln` را اجرا کنید.
- در تغییر JS، syntax و حالت‌های no-data / malformed storage را بررسی کنید.
- پروژه فعلاً test project ثبت‌شده در solution ندارد؛ این را به‌عنوان نبود تست خودکار اعلام کنید، نه «تست موفق».
- stylesheet و scriptهای اختصاصی View از `asp-append-version="true"` استفاده می‌کنند؛ مسیرهای asset ثابت در Razor با `~/shop/` آغاز می‌شوند تا به مسیر جاری وابسته نباشند. اگر مرورگر CSS/JS قدیمی نشان داد، ابتدا URL نهایی و cache-busting واقعی asset را بررسی کنید.

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
