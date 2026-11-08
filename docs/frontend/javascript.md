> وضعیت دادهٔ runtime (۸ نوامبر ۲۰۲۶): داده‌های کاتالوگ، برند، مجله، FAQ، حساب نمونه، جغرافیا و طرح فروش از APIهای متصل به SQL Server خوانده می‌شوند. سبد، علاقه‌مندی، مقایسه، نشانی، checkout، سفارش، پیگیری قیمت و تعامل مقاله از API حساب می‌آیند؛ دیدگاه/پرسش محصول از API محصول. کلیدهای قدیمی Shop در localStorage فقط برای انتقال یک‌باره خوانده و سپس حذف می‌شوند؛ دادهٔ runtime در localStorage نگهداری نمی‌شود.

# قرارداد JavaScript و state مرورگر

مسیرهای کوتاه `js/` در این صفحه نسبت به `Shop/wwwroot/shop/` هستند. برای ورود از صفحه به script، [نقشهٔ صفحه‌ها](../architecture/routing.md) و [ترتیب اجرا](layout.md) را ببینید.

ورودی API و payload انتقال legacy باید پیش از رندر escape و validate شود. انتقال legacy در `storage.js` است؛ handler اختصاصی را به ریشهٔ صفحه محدود کنید و با `HNUI.mount` یک‌بار ثبت کنید. کارت مشترک فروشگاه در [product-card.js](../../Shop/wwwroot/shop/js/store/product-card.js) optionهای طرح فروش را دریافت می‌کند؛ renderer صفحهٔ اول در [home-sales-plans.js](../../Shop/wwwroot/shop/js/pages/home-sales-plans.js) پلن عادی را حذف و برای offerهای ویژه کارت می‌سازد. افزودن از این کارت‌ها `salesPlanId` و snapshot نمایشی (نام، کد، مبلغ واحد، موجودی و سقف خرید) را به `HNUI.cart` می‌دهد؛ نام پلن ویژه در کارت و سبد هدر دیده می‌شود، اما پلن عادی برچسب ندارد. snapshot فقط برای نمایش است و Checkout باید آن را از API دوباره اعتبارسنجی کند. قرارداد دامنه در [فروش چندطرحی](../store/sales-plans.md) مرجع است.


`store/account-data.js` پس از JSONهای `commerceData` و (فقط در پروفایل) `accountData` اجرا می‌شود و projection فقط‌خواندنی `HNAccountData` را پیش از مصرف سبد و حساب منتشر می‌کند. دادهٔ خراب، غایب یا نسخهٔ ناشناخته به آرایه/پیشنهاد خالی تبدیل می‌شود. bootstrap عمومی سابقهٔ سفارش ندارد.

## API مشترک `window.HNUI`

در `js/core/ui-core.js` با `Object.freeze` منتشر می‌شود. اعضای قابل استفادهٔ فعلی:

| API | نقش |
|---|---|
| `HNUI.STORAGE_KEYS` | کلیدهای پایهٔ auth/cart |
| `HNUI.storage.get/set/remove/json/writable` | خواندن/نوشتن مقاوم در برابر خطای browser storage؛ `set` برای favorite/compare رویداد list را dispatch می‌کند |
| `HNUI.api.request(path, options)` | مسیر را زیر base path محلیِ تنظیم‌شده resolve می‌کند، درخواست را به همان origin محدود می‌کند، `Accept: application/json` و `credentials: same-origin` می‌گذارد و پس از timeout قابل‌تنظیم درخواست را abort می‌کند. retry خودکار ندارد؛ POSTهای OTP/quote/validate نباید بی‌کلید idempotency تکرار شوند. تنظیم در `ApiClient` در `appsettings.json` و metadata پوسته است. |
| `HNUI.normalizeSearchText(value)` | نرمال‌سازی مشترک عبارت فارسی/انگلیسی و نویسه‌های نامرئی برای پیشنهادهای هدر و نتایج Search |
| `HNUI.ready(callback)` | اجرای callback پس از آماده‌شدن DOM |
| `HNUI.mount(name, root, initialize)` | اجرای یک‌باره برای نام مؤلفه و root؛ root غایب یا نصب دوباره false برمی‌گرداند؛ خطای initialize دوباره قابل تلاش است |
| `HNUI.EVENTS`, `HNUI.emit(type, detail)` | نام ثابت رویداد و کنترل payloadهای cart/auth؛ برای رویداد مشترک CustomEvent موازی نسازید |
| `HNUI.cart.read/write/add/update/remove/totals/quantityLimit/renderOptions/renderControls` | normalize مشترک، renderer مشترک گزینه‌ها و کنترل تعداد/حذف، validation ساده و مدیریت سبد؛ `write` رویداد cart را dispatch می‌کند |
| `HNUI.auth.isLoggedIn/login/logout/requireLogin` | وضعیت نشست cookie تأییدشده در صفحه؛ `requireLogin(resume)` برای کاربر واردنشده رویداد `hn:auth-required` می‌فرستد و callback را پس از ورود ادامه می‌دهد |

فرم ورود و ثبت‌نام در `layout/main.js` کد را با `POST /api/auth/email-code` درخواست و با `POST /api/auth/email-code/verify` اعتبارسنجی می‌کند؛ پس از تأیید، پاسخ سرور نشست cookie می‌سازد و وضعیت حساب از `GET /api/account/state` خوانده می‌شود. کد ثابت یا پذیرش OTP صرفاً بر اساس طول در مرورگر وجود ندارد. تنظیمات و محدودیت OTP در [مرز داده و backend](../architecture/backend-readiness.md) مرجع‌اند.
| `HNUI.scrollLock.lock/unlock` | شمارندهٔ قفل scroll با کلاس `ui-scroll-locked` |
| `HNUI.dialog` | helperهای dialog/focus/keyboard؛ جزئیات دقیق را در همان فایل بخوانید |
| `HNUI.setMessage`, `setMessageType`, `showToast` | پیام وضعیت و بازخورد UI؛ امضا و پیش‌فرض را در ui-core.js بررسی کنید |
| `HNUI.enhance(root)` | بهبودهای عمومی DOM داخل root |
| `toPersianDigits`, `toEnglishDigits`, `formatNumber`, `escapeHtml`, `localizeVisibleDigits` | ارقام، قالب عدد، escape متن و بومی‌سازی نمایش |

برای تبدیل رقم‌های فارسی/عربی در validation، OTP، شمارهٔ تماس و کد تخفیف، فقط از `HNUI.toEnglishDigits` استفاده کنید؛ mapper محلی تازه نسازید.

## قرارداد سبد

منبع واحد افزودن، نرمال‌سازی و رندر گزینه‌ها در [راهنمای سبد و تسویه](../store/cart.md) تعریف شده است. رفتار سبد را در آن راهنما تغییر دهید؛ این صفحه مالک API، event و کلیدهای مشترک است.

`HNUI.cart.renderControls(item, index = null)` کنترل مشترک هدر و صفحهٔ سبد را می‌سازد. بدون index از `data-action` و با index از `data-plus/minus/remove` استفاده می‌کند. نام محصول escape می‌شود و وضعیت دکمه‌ها با حداقل یک و `quantityLimit(item)` هماهنگ است. مالک ظاهر `.hn-cart-controls` در `shared-cart.css` است.

## مالکیت JS و رفتارهای کلیدی

| فایل | مسئولیت فعلی | قبل از تغییر چه چیزی را دنبال کنیم |
|---|---|---|
| `js/core/ui-core.js` | اتصال APIهای مشترک، auth، event/mount، lock/dialog، format/escape | قرارداد payload و accessibility |
| `js/core/storage.js` | پاک‌سازی کلیدهای قدیمی Shop و state موقت صفحه | انتقال legacy به API و حذف کلیدهای قدیمی |
| `js/store/cart.js` | normalize، قیمت، موجودی، سبد و renderer گزینه‌ها | شناسهٔ محصول و قرارداد سبد |
| `js/layout/header-navigation.js` | باز/بسته، کیبورد، hover و گروه‌های دسته در هدر | trigger واقعی، state صریح و خانه/داخلی |
| `js/layout/main.js` | پیشنهاد/جست‌وجوی اخیر، شمارنده‌ها، account/login/cart/list panels و هماهنگی خانه؛ پیمایش لینک‌های hash با رعایت `scroll-margin-top` مقصد | جستجوی هدر و drawer پس از ۳ نویسه پیشنهاد محصول و offer طرح فروش فعال و موجود را با debounce ۲۵۰ms می‌سازند؛ offer نام طرح و قیمت ویژه را نشان می‌دهد و پیوندش `salesPlanId` را نگه می‌دارد. submit کمتر از ۳ نویسه متوقف می‌شود. listenerهای delegated و panel state؛ حفظ offset هدر/نوار sticky و پشتیبانی از reduced motion |
| `js/core/form-validation.js` | validation عمومی/delegated فرم | `data-*`، selector فرم، خطاهای inline و submit handler اختصاصی؛ `data-validation="full-name"` دست‌کم دو بخش نام را می‌سنجد و `data-required-message` متن خطای فیلد اجباری را جایگزین می‌کند |
| `js/store/catalog.js` | resolver دادهٔ دریافت‌شده از دیتابیس؛ HNCatalog و APIهای سازگار HNProducts/HNCountries/HNProductColors | شناسه‌های صریح، relation و immutable بودن |
| `js/store/product-card.js` | renderer مشترک کارت و قیمت | variantهای grid/slider/search/related/favorite؛ escape و data-* مشترک |
| `js/pages/home-sales-plans.js` | ساخت بخش‌های صفحهٔ اول برای طرح‌های فروش فعال، با کارت مشترک و offer همان طرح | میزبان‌های خانه، ترتیب طرح‌ها، زمان پایان و حذف offer ناموجود؛ فروش عادی در شبکهٔ عادی خانه باقی می‌ماند |
| `js/store/commerce.js` | جزئیات محصول، variant/accessory، قیمت، gallery، feedback، cart، coupon، compare و favorites rendering | selectorهای `#productView`, `#cartView`, `#favoritesView`, `#compareView`؛ event/storage و rendererهای HTML؛ موقعیت نشانگر تب محصول با offset نوار چسبان هماهنگ است |
| `js/pages/search.js` | URL-backed filter/sort/page، نتایج و mobile filter drawer | query `sales-plan` شناسهٔ طرح را بازیابی می‌کند؛ فهرست همهٔ طرح‌ها یکپارچه است و badge کارت ویژه نوع آن را مشخص می‌کند. مرتب‌سازی روی کل نتایج اعمال می‌شود. جستجوی محصول پس از ۳ نویسه و debounce ۲۵۰ms است؛ query کوتاه نادیده گرفته می‌شود و پاک‌کردن فیلترها گروه‌های پنل فیلتر را می‌بندد. `popstate`, empty state و cart/list controls |
| `js/pages/account-page.js` | profile route/hash navigation، overview و CRUD آدرس | storage keys، modal، hashchange و `storage` event |
| `js/pages/checkout.js` | انتخاب آدرس، روش پرداخت و هماهنگی state سبد | روش پرداخت فعلاً demo است؛ Cart/Checkout هر دو consumer هستند |
| `js/pages/address-fields.js`, `iran-places.js` | مدل‌های Country/Province/City و انتخاب وابستهٔ استان/شهر | کشور ایران در رکورد آدرس ثابت است؛ انتخاب استان فهرست شهرهای همان استان را به‌روزرسانی می‌کند |
| `js/pages/orders-pagination.js` | صفحه‌بندی سفارش‌ها | URL state/popstate و empty/list state |
| `js/pages/magazine.js` | فیلتر/صفحه‌بندی و تاریخچهٔ URL مقاله‌ها | `history`, query string و back/forward |
| `js/pages/article-details.js` | print، فهرست مطالب، پیشرفت مطالعه و scroll/resize | sticky offset، passive listener و کاهش حرکت |
| `js/pages/brand-product-count.js` | شمارش محصولات برند | دادهٔ HNProducts و View جزئیات برند |
| `js/pages/contact.js` | اعتبارسنجی فرم تماس و نمایش وضعیت ارسال | پیام status؛ endpoint ارسال در سرور تعریف نشده است |
| `js/pages/page-content.js` | share/copy، forms، CTA و pagination عمومی بعضی صفحه‌ها | script در چند route مشترک است؛ handler دوباره ثبت نشود |
| `js/pages/form-confirm-dialog.js` | dialog تأیید فرم | backdrop/ESC و focus; در Product/Cart/Compare/Contact و جزئیات Magazine load می‌شود |
| `js/core/electrical-content.js` | ریزتعامل/محتوای عمومی برخی صفحه‌ها | پس از section Scripts اجرا می‌شود |

در منوهای دسکتاپ، حرکت نشانگر می‌تواند منو را باز کند و کلیک روی trigger آن را در حالت باز نگه می‌دارد؛ این کار مانع بسته‌شدن فوری منو هنگام ورود pointer به دکمه می‌شود. بستن با کلیک بیرون، خروج pointer از پنل و Escape انجام می‌شود. در تغییر رفتار trigger، خانه و صفحات داخلی را با pointer و صفحه‌کلید بررسی کنید.

## دادهٔ مرورگر و منبع API

دادهٔ حساب و تعامل‌های کاربر از API حساب خوانده و همان‌جا ذخیره می‌شوند؛ جستجوهای اخیر نیز از `api/account/recent-searches` می‌آیند. درخواست‌های API با `cache: no-store` ارسال می‌شوند و پاسخ‌های `/api` هدر `Cache-Control: no-store` دارند. بازخورد محصول از API محصول دریافت می‌شود. صفحه برای رندر و تعامل جاری، پاسخ API را در state موقت جاوااسکریپت نگه می‌دارد؛ این state میان بارگذاری صفحه‌ها پایدار نیست و مرجع داده محسوب نمی‌شود.

`storage.js` در شروع، کلیدهای قدیمی متعلق به Shop را فقط برای انتقال یک‌باره می‌خواند و از `localStorage` حذف می‌کند. payload انتقال تا پاسخ موفق API در حافظهٔ همان صفحه می‌ماند و پس از موفقیت کنار گذاشته می‌شود. کد جدید نباید از `localStorage` یا `sessionStorage` برای دادهٔ runtime استفاده کند یا در آن‌ها بنویسد. ورود، سبد، نشانی، فهرست‌ها، پیگیری قیمت، ترجیح checkout، تعامل مقاله و بازخورد محصول باید از endpoint مسئول بخوانند/بنویسند.

اگر API در دسترس نباشد، عملیات را موفق اعلام نکنید؛ payloadهای legacy فقط برای انتقال یک‌باره‌اند و state جاری صفحه پس از بارگذاری تازه از API می‌آید. برای تغییر قرارداد API یا انتقال داده، مصرف‌کننده‌ها را با `rg -n "localStorage|sessionStorage|storage\.(get|set|json)|hn-" Shop/wwwroot/shop/js Shop/Views` پیدا کنید و راهنمای حوزه را به‌روز کنید.

## رویدادهای سفارشی

| Event | payload فعلی | listenerهای مهم/اثر |
|---|---|---|
| `hn:cart-change` | `detail`: آرایهٔ cart نرمال‌شده | header/panel، commerce و checkout را sync می‌کند |
| `hn:cart-added` | `detail: { product, items, trigger }` | پنل cart ممکن است باز شود؛ focus trigger را حفظ کنید |
| `hn:cart-stock-limit` | `detail: { product, stock, maxCartQuantity, limit, requested, remaining, reason }`؛ reason برابر `stock` یا `cart` | اعلام محدودیت موجودی یا سقف تعداد هنگام add/update؛ بازخورد آن در `main.js` نمایش داده می‌شود |
| `hn:auth-change` | `detail`: boolean | هدر و بخش account را تازه می‌کند |
| `hn:auth-required` | `detail: { resume: Function|null }` | بازکردن مدال ورود؛ callback پس از ورود آزمایشی اجرا می‌شود |
| `hn:lists-change` | بدون `detail` | شمارنده‌ها، favorite/compare button و صفحه‌ها را sync می‌کند |
| native `storage` | `key`, `newValue`, `oldValue` | همگام‌سازی tabهای دیگر؛ تغییر cart در `ui-core.js` به `hn:cart-change` تبدیل می‌شود، تغییر address/payment فقط همان بخش را تازه می‌کند و تغییر نامرتبط نباید صفحه را reload کند. same-tab تغییرات باید از helper/event عبور کند |

هنگام افزودن event، یک source of truth حفظ کنید، payload را document کنید، handler را idempotent بنویسید و از dispatch مضاعف در View و shared JS جلوگیری کنید.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۲ نوامبر ۲۰۲۶.


قیمت نهایی کارت مشترک در HNProductCard.currentPrice(product, options) محاسبه می‌شود. فیلتر و مرتب‌سازی قیمت باید از همین تابع استفاده کنند تا تخفیف offer دوبار اعمال نشود و قیمت فروش عادی با مبلغ نهایی کارت یکسان بماند.
