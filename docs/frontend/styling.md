# توکن‌ها و مالکیت CSS

مسیرها در این راهنما نسبت به `Shop/wwwroot/shop/` هستند. ترتیب بارگذاری مرجع مستقل در [پوسته](layout.md) دارد؛ این صفحه مالکیت و روش انتخاب محل تغییر را توضیح می‌دهد.

## قواعد طراحی مشترک

نقشهٔ قابل‌کنترل مالکیت در [ui-owners.json](../../tools/ui-owners.json) است؛ قواعد قدیمی خارج مالک در [بودجهٔ legacy](../../tools/ui-legacy-budget.json) ثبت شده‌اند. `npm run check` افزایش !important یا override تازهٔ کارت/هدر خارج مالک را رد می‌کند. جزئیات تغییر کنترل‌شده در [کیفیت](../development/quality.md) است.

- رنگ، بردر، شعاع، سایه و حالت hover/focus را از توکن مشترک بگیرید؛ رنگ وضعیت موفقیت/خطا/هشدار را فقط وقتی معنای واقعی دارد نگه دارید.
- حالت نامعتبر فرم با مرز و سایهٔ کم‌رنگ مشخص می‌شود؛ صفحه می‌تواند پیام خطا را در قاب جمع‌وجور و خوانا نمایش دهد، بدون حلقهٔ قرمز پررنگ یا `!important` تازه.
- کارت‌ها/پنل‌ها را تودرتو و بی‌دلیل با پس‌زمینه و سایهٔ جدا نسازید؛ مرز بین سطح صفحه، پنل و کارت باید واضح و کم‌تعداد باشد.
- آیکن داخل کاشی یا دکمهٔ مربعی باید در هر دو محور وسط باشد؛ برای کاشی از `display: grid` همراه `place-items: center` استفاده کنید. `place-items` به‌تنهایی روی عنصر block آیکن را وسط‌چین نمی‌کند؛ در DevTools مقدار display برنده و جای glyph را هم بررسی کنید.
- توضیح hero نباید max-width ثابت و کوچک‌تری از قاب متن داشته باشد؛ از عرض container استفاده کند و فقط وقتی خود container جا ندارد، طبیعی به خط بعد برود. این قاعده برای heroهای داخلی و خانه برقرار است؛ breakpoint موبایل هم باید wrap طبیعی را حفظ کند.
- هدر، مگامنو، پنل‌های سبد/حساب، modal و صفحه‌بندی اجزای مشترک‌اند؛ تغییر آن‌ها را روی خانه، صفحهٔ داخلی و موبایل ارزیابی کنید.
- تصاویر باید نسبت/قاب مشخص و `object-fit` مناسب داشته باشند؛ در موبایل عرض، overflow و فضای امن محتوا را بررسی کنید.
- فونت‌ها و آیکن‌ها از فایل‌های محلی زیر `wwwroot/shop/vendor/` می‌آیند. برای آیکن جدید اول از Bootstrap Icons موجود استفاده کنید و وابستگی تازه وارد نکنید.
- در موبایل فاصلهٔ بیرونی پنل‌های `.flow-main` در `shared-surface-theme.css` کنترل می‌شود؛ فاصلهٔ محتوای صفحهٔ مجله از hero تا آرشیو در `mobile-pages.css` است. اگر فاصله زیاد به‌نظر رسید، حاشیهٔ بخش و padding والد را با هم بررسی کنید تا فاصله‌ها روی هم جمع نشوند.

## توکن‌های پایه

مرجع: `Shop/wwwroot/shop/css/core/design-tokens.css`.

| نقش | توکن مرجع | مقدار فعلی |
|---|---|---|
| سرمه‌ای | `--color-navy` | `#102a43` |
| آبی-خاکستری | `--color-blue` | `#243b53` |
| زرد تأکیدی | `--color-gold` | `#f6c515` |
| زمینهٔ روشن | `--color-cream` | `#f5f7f8` |
| سطح | `--color-surface` | `#fff` |
| متن اصلی | `--color-ink` | `#202a33` |
| متن کمکی | `--color-muted` | `#4c5b65` |
| خط/بردر | `--color-line` | `#dfe5e9` |

توکن‌های فاصله، container، radius و shadow نیز در همان فایل هستند؛ نام و مقدار را پیش از تعریف مقدار جدید بخوانید. `design-tokens.css` مرجع واحد پالت فعال و aliasهای سازگاری است. `electrical-theme.css` و `luxury-theme.css` قواعد ظاهر/چیدمان دارند و نباید رنگ‌های root را دوباره تعریف کنند؛ `industrial-system.css` مصرف‌کنندهٔ توکن‌ها و مالک overrideهای legacy است، نه منبع پالت. aliasهای قدیمی را با نامشان تفسیر نکنید: `--cream` به طلایی وصل است، اما `--cream2` زمینهٔ روشن است. برای متغیرهای `--industrial-*` مقدار مرجع را در همان فایل توکن‌ها تغییر دهید.

CSS فعلی با ترتیب stylesheetها cascade می‌شود و از `@layer` استفاده نمی‌کند. فایل‌ها را جداگانه داخل cascade layer نپیچید: قانون‌های عادی بدون layer بر قانون‌های عادی داخل layer اولویت دارند و ترتیب فعلی می‌تواند بی‌اثر شود. مهاجرت به `@layer` فقط باید یک‌جا و با پوشش منابع vendor، layout، View و page انجام شود.

برای محتوای پهن دسکتاپ، `--container-wide-max` سقف مشترک ۱۴۸۰px است. خانه، جستجو، آرشیو مجله، مقاله و مگامنوی صفحات داخلی باید از همین سقف و فاصلهٔ مرکزی استفاده کنند؛ بخش‌های معمولی‌تر می‌توانند از سقف Bootstrap یا قاب ۱۳۶۰px صفحه استفاده کنند. هدرها و heroهای تمام‌عرض پس‌زمینهٔ خود را حفظ می‌کنند و محتوای داخلی‌شان در قاب پهن هم‌تراز می‌شود.

در hero صفحه‌ها، مسیر راهنما مستقیماً به تیتر اصلی می‌رسد؛ برچسب یا kicker تزئینی بالای تیتر نمایش داده نمی‌شود. نام صفحه و توضیح آن در خود تیتر و متن معرفی می‌آیند. این قاعده فقط برای hero است و برچسب‌های عنوان‌بخش‌های داخل محتوا را حذف نمی‌کند.

## مالکیت فایل‌های CSS

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
| `css/layout/luxury-theme.css` | تزئینات و پوستهٔ عمومی/legacy | توکن root تعریف نمی‌کند؛ قواعد گسترده و media متعدد دارد و اولین مالک ظاهری فرض نشود |
| `css/layout/style.css` | stylesheet قدیمی و حجیم، قواعد عمومی و responsive | فایل legacy بزرگ؛ قواعد هم‌نام را قبل از ادیت بشمارید |
| `css/layout/mvc-layout.css` | قاب و layout صفحه‌های MVC | پس از `@section Styles` بارگذاری می‌شود |
| `css/layout/mobile-drawer-compact.css` | فشرده‌سازی drawer موبایل | فقط تا 991px؛ ممکن است بر shared drawer غلبه کند |
| `css/layout/electrical-theme.css` | هویت/چیدمان تم برق و ابزار | قواعد صفحهٔ اول؛ مرجع رنگ در design-tokens.css است |
| `css/layout/industrial-system.css` | overrideهای صنعتی/سراسری و responsive | مصرف‌کنندهٔ توکن‌های مرکزی و دارای overrideهای legacy؛ آخرین فایل نیست و مالک مؤلفه فرض نشود |
| `css/layout/quick-links-menu.css` | منوی دسترسی سریع مشترک | پس از پوستهٔ صنعتی و پیش از کارت محصول بارگذاری می‌شود |
| `css/store/shared-product-card.css` | کارت‌های مشترک محصول و قیمت؛ شامل ۶۸ قاعدهٔ منتقل‌شده از industrial-system | renderer واحد product-card.js؛ media queryهای منتقل‌شده حفظ شده‌اند؛ Search/خانه/Related/Favorites بررسی شوند |
| `css/layout/content-typography.css` | یکدست‌سازی تایپوگرافی و خوانایی محتوای فارسی | layout-wide؛ بعد از CSSهای قبلی می‌آید، در صفحه‌های محتوایی و RTL بررسی شود |
| `css/pages/search-mobile.css` | ریزچیدمان و کنترل‌های Search در موبایل | در layout بارگذاری می‌شود؛ selectorهای فایل باید به Search محدود بمانند |
| `css/layout/mobile-pages.css` | اصلاحات responsive مشترک صفحه‌ها | پیش از `SearchStyles` و stylesheetهای نهایی مشترک می‌آید؛ scope هر selector را بررسی کنید |
| `css/layout/empty-state-system.css` | الگوی مشترک حالت‌های بدون نتیجه/بدون داده | در انتهای stylesheetهای مشترک می‌آید؛ به صفحهٔ مصرف‌کننده محدودش کنید |
| `css/layout/shared-surface-theme.css` | پرداخت نهایی سطح‌ها و پنل‌های مشترک | پیش از page-hero-imagery.css و mega-menu-unified.css؛ ترتیب نهایی را در راهنمای پوسته بررسی کنید |
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
| `css/pages/product-detail.css`، `product-detail-pricing.css` و `product-detail-options.css` | چیدمان دیتیل، قیمت و گزینه‌های محصول | [راهنمای محصول](../store/product.md)؛ بین Styles و PageStyles تقسیم شده‌اند |
| `css/pages/home-warmth.css` | ظاهر بخش‌های خانه و فضای عنوان/قیمت | مالک تراز مشترک کارت همچنان shared-product-card.css است |
| `css/layout/page-hero-imagery.css` | تصویر hero صفحه‌ها | پس از shared-surface-theme.css بارگذاری می‌شود |
| `css/layout/mega-menu-unified.css` | هماهنگی نهایی مگامنوی خانه/داخلی | آخرین CSS فعلی layout؛ trigger، سطح دوم/سوم و hover را با markup هدر بررسی کنید |

## breakpoint و responsive

مقادیر رایج فعلی 380، 389/390، 575، 640، 767/768 و 991/992px هستند؛ برخی صفحه‌ها مرزهای دیگری دارند. این‌ها سیستم breakpoint واحد و رسمی نیستند. قواعد هم‌پوشان را با بررسی همان viewport اصلاح کنید.

کلاس بدنه از controller/action ساخته می‌شود؛ Search با View نام List، کلاس `mobile-page--product-index` دارد. قاعده را از روی نام فایل View حدس نزنید. در جدول زیر یا snapshot قدیمی به‌دنبال آخرین media query نگردید؛ منبع جاری را بخوانید:

```powershell
rg -n "@media" Shop/wwwroot/shop/css
```

در بازبینی، موبایل 360–390، تبلت 768 و مرز دسکتاپ 992px را همراه یک پیکسل دو طرف breakpoint مؤثر کنترل کنید. عنوان بلند، قیمت بدون تخفیف و با تخفیف، RTL و گروه‌های قابل اسکرول را نیز ببینید. چک‌لیست کامل در [بازبینی UI](verification.md) است.

## متن و کنتراست در موبایل

- در صفحه‌های خانه و داخلی تا عرض ۷۶۷px، اندازهٔ پایهٔ متن یک پله به ۱۴px کاهش می‌یابد؛ placeholder فرم‌ها ۱۲px و با وزن معمولی است. اندازهٔ کنترل واقعی و تیترها جداگانه و در مالک خودشان حفظ می‌شود؛ متن کمکی یا placeholder را برای جا دادن محتوا بیش از این ریز نکنید.
- لینک‌های صفحات مرتبط و نام کامل پروفایل در عرض کم به خط بعد می‌روند؛ برای جا دادن آن‌ها از حذف متن با ellipsis یا کوچک‌کردن بیش از حد فونت استفاده نکنید.
- سه میان‌بر hero محصول در یک ردیف می‌مانند، اما متن هر میان‌بر می‌تواند داخل همان خانه بشکند. عنوان دسته‌های مجله نیز باید پس از اختصاص فضای آیکن و شمارنده، فضای کافی برای خواندن داشته باشد.
- متن‌های کمکی روی سطح روشن از `--color-text-muted` و نوشته‌های طلایی از `--color-accent-ink` استفاده کنند. متن رنگی تخفیف و متن سبز اعتماد باید روی پس‌زمینهٔ واقعی خود بررسی شوند.
- رنگ عمومی پاراگراف‌های صفحه نباید متن روشن `.article-next-step` روی زمینهٔ سرمه‌ای را بازنویسی کند؛ رنگ این بخش در مالک مقاله تعیین می‌شود.
- ابزارهای مطالعهٔ مقاله تا عرض ۹۹۱px یک‌ستونه‌اند تا در چیدمان دارای سایدبار، متن راهنما بین دکمه‌ها فشرده نشود.
- برای تشخیص بریدگی، علاوه بر عرض کل صفحه، `scrollWidth` هر متن و ancestor دارای overflow را بررسی کنید. جداکنندهٔ `::after` مسیر محصول و اسکرول عمدی اسلایدر/مقایسه نباید با بیرون‌زدگی متن اشتباه گرفته شوند. در محاسبهٔ کنتراست، گرادیان و شفافیت پس‌زمینه را هم در نظر بگیرید.

## قرارداد انتخاب selector و پیدا کردن مالک

- ابتدا class/ID/data-attribute را از Razor یا DOM زنده بردارید؛ سپس همهٔ مصرف‌ها و تعریف‌ها را پیدا کنید:

```powershell
rg -n "نام-کلاس|#نامId|data-نام" Shop/Views Shop/wwwroot/shop/css Shop/wwwroot/shop/js
```

- selector صفحه را زیر ریشهٔ همان View نگه دارید (`.search-main …`, `.journal-page …`, `#cartView …`) و selector مشترک را در CSS مشترک مؤلفه اصلاح کنید.
- `section`, `button`, `.card`, `h2`, `.flow-card` و attribute selector بدون ریشه می‌توانند صفحه‌های نامرتبط را تغییر دهند؛ global بودن را عمداً تأیید کنید.
- قبل از اضافه‌کردن قانون، در DevTools عنصر واقعی را انتخاب کنید و source file/line، specificity، `!important`, media condition و state class برنده را ثبت کنید.
- راه‌حل اول اصلاح یا حذف قانون متناقض در مالک اصلی است؛ از افزودن override انتهایی یا `!important` تازه برای پنهان‌کردن cascade پرهیز کنید.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۲۴ اکتبر ۲۰۲۶.
