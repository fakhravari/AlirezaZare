# قرارداد JavaScript و state مرورگر

مسیرهای کوتاه `js/` در این صفحه نسبت به `Shop/wwwroot/shop/` هستند. برای ورود از صفحه به script، [نقشهٔ صفحه‌ها](../architecture/routing.md) و [ترتیب اجرا](layout.md) را ببینید.

ورودی ذخیره‌شده در مرورگر باید پیش از رندر escape و validate شود. fallback و migration در storage.js است؛ handler اختصاصی را به ریشهٔ صفحه محدود کنید و با `HNUI.mount` یک‌بار ثبت کنید.


`store/account-data.js` پس از JSONهای `commerceData` و (فقط در پروفایل) `accountData` اجرا می‌شود و projection فقط‌خواندنی `HNAccountData` را پیش از مصرف سبد و حساب منتشر می‌کند. دادهٔ خراب، غایب یا نسخهٔ ناشناخته به آرایه/پیشنهاد خالی تبدیل می‌شود. bootstrap عمومی سابقهٔ سفارش ندارد.

## API مشترک `window.HNUI`

در `js/core/ui-core.js` با `Object.freeze` منتشر می‌شود. اعضای قابل استفادهٔ فعلی:

| API | نقش |
|---|---|
| `HNUI.STORAGE_KEYS` | کلیدهای پایهٔ auth/cart |
| `HNUI.storage.get/set/remove/json/writable` | خواندن/نوشتن مقاوم در برابر خطای browser storage؛ `set` برای favorite/compare رویداد list را dispatch می‌کند |
| `HNUI.ready(callback)` | اجرای callback پس از آماده‌شدن DOM |
| `HNUI.mount(name, root, initialize)` | اجرای یک‌باره برای نام مؤلفه و root؛ root غایب یا نصب دوباره false برمی‌گرداند؛ خطای initialize دوباره قابل تلاش است |
| `HNUI.EVENTS`, `HNUI.emit(type, detail)` | نام ثابت رویداد و کنترل payloadهای cart/auth؛ برای رویداد مشترک CustomEvent موازی نسازید |
| `HNUI.cart.read/write/add/update/remove/totals/quantityLimit/renderOptions/renderControls` | normalize مشترک، renderer مشترک گزینه‌ها و کنترل تعداد/حذف، validation ساده و مدیریت سبد؛ `write` رویداد cart را dispatch می‌کند |
| `HNUI.auth.isLoggedIn/login/logout/requireLogin` | وضعیت ورود در مرورگر؛ `requireLogin(resume)` برای ورود نداشتن رویداد `hn:auth-required` می‌فرستد و callback را پس از ورود ادامه می‌دهد |

فرم ثبت‌نام در `layout/main.js` کد را با `POST /api/auth/email-code` درخواست و با `POST /api/auth/email-code/verify` اعتبارسنجی می‌کند؛ کد ثابت یا قابل‌قبول‌بودن هر شش رقم در مرورگر وجود ندارد. کد تأیید سروری است، اما `completeDemoLogin` پس از تأیید هنوز state محلی ورود را فعال می‌کند؛ تا ساخت حساب و session backend اضافه نشده، ورود را احراز هویت پایدار تلقی نکنید. تنظیمات و محدودیت OTP در [مرز داده و backend](../architecture/backend-readiness.md) مرجع‌اند.
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
| `js/core/storage.js` | storage، نسخهٔ رکورد، backup و migration | legacy/future/malformed/quota؛ نوشتن و حذف موفق/ناموفق |
| `js/store/cart.js` | normalize، قیمت، موجودی، سبد و renderer گزینه‌ها | شناسهٔ محصول و قرارداد سبد |
| `js/layout/header-navigation.js` | باز/بسته، کیبورد، hover و گروه‌های دسته در هدر | trigger واقعی، state صریح و خانه/داخلی |
| `js/layout/main.js` | پیشنهاد/جست‌وجوی اخیر، شمارنده‌ها، account/login/cart/list panels و هماهنگی خانه؛ پیمایش لینک‌های hash با رعایت `scroll-margin-top` مقصد | listenerهای delegated و panel state؛ حفظ offset هدر/نوار sticky و پشتیبانی از reduced motion |
| `js/core/form-validation.js` | validation عمومی/delegated فرم | `data-*`، selector فرم، خطاهای inline و submit handler اختصاصی؛ `data-validation="full-name"` دست‌کم دو بخش نام را می‌سنجد و `data-required-message` متن خطای فیلد اجباری را جایگزین می‌کند |
| `js/store/catalog.js` | resolverِ catalog.json؛ HNCatalog و APIهای سازگار HNProducts/HNCountries/HNProductColors | شناسه‌های صریح، relation و immutable بودن؛ داده دمو است |
| `js/store/product-card.js` | renderer مشترک کارت و قیمت | variantهای grid/slider/search/related/favorite؛ escape و data-* مشترک |
| `js/store/commerce.js` | جزئیات محصول، variant/accessory، قیمت، gallery، feedback، cart، coupon، compare و favorites rendering | selectorهای `#productView`, `#cartView`, `#favoritesView`, `#compareView`؛ event/storage و rendererهای HTML؛ موقعیت نشانگر تب محصول با offset نوار چسبان هماهنگ است |
| `js/pages/search.js` | URL-backed filter/sort/page، نتایج و mobile filter drawer | query params، `popstate`, empty state و cart/list controls |
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

## localStorage: کلیدهای شناخته‌شده

رکوردهای ساختاریافته با شکل `{schemaVersion: 1, value: ...}` نوشته می‌شوند. آرایه/شیء قدیمی همچنان خوانده می‌شود و در اولین نوشتن نسخه‌دار، نسخهٔ خام یک‌بار در `hn-migration-backup:{key}` حفظ می‌شود. wrapper همیشه مقدار بازشده را به consumer می‌دهد؛ کد مصرف‌کننده نباید envelope را خودش باز کند.

نسخهٔ جدیدتر خوانده/بازنویسی/حذف نمی‌شود و `storage.writable(key)` false برمی‌گرداند. JSON خراب یا shape اشتباه fallback می‌گیرد؛ نوشتن معتبر می‌تواند آن را با backup اصلاح کند. cart، favorite/compare، آدرس‌ها، پروفایل، cache محصول، جستجوهای اخیر و کلیدهای `hn-product-reviews-{id}`/`hn-product-questions-{id}` قرارداد نسخه‌دار دارند. scalarهایی مثل auth، coupon و شناسهٔ آدرس همچنان رشته‌اند. تغییر schema باید migration صریح و تست داشته باشد؛ storage کاربر را برای رفع خطا پاک نکنید. دادهٔ قدیمی `hn-profile-activity` دیگر مصرف یا پاک نمی‌شود و در مرورگر باقی می‌ماند.

کلیدهای زیر دمو و client-side هستند؛ persistence سرور یا دادهٔ قابل‌اعتماد محسوب نمی‌شوند. پیش از migration/پاک‌سازی، همهٔ مصرف‌کننده‌ها را با `rg -n "نام-کلید" Shop` پیدا کنید.

| کلید | مصرف/نکته |
|---|---|
| `hadi-norouzi-cart` | cart نرمال‌شده توسط `HNUI.cart` |
| `hn-demo-auth` | وضعیت ورود در مرورگر (`1`/`true`) |
| `hn-favorites` | شناسه‌های favorite |
| `hn-compare` | شناسه‌های مقایسه |
| `hn-compare-demo-seeded` | علامت مقداردهی اولیهٔ Compare |
| `hn-product-cache` | cache اطلاعات محصول برای رندر list/panel |
| `hn-header-recent-searches` | تاریخچهٔ پیشنهاد search در هدر |
| `hn-demo-coupon` | کد تخفیف demo |
| `hn-delivery-addresses` | فهرست آدرس‌های ذخیره‌شده در مرورگر |
| `hn-selected-address` | شناسهٔ آدرس انتخابی |
| `hn-payment-method-v2` | روش پرداخت انتخابی در مرورگر |
| `hn-profile` | دادهٔ profile در `Views/Profile/Index.cshtml` |
| `hn-product-${type}-${productId}` | کلید feedback در `commerce.js`؛ `type` برابر `reviews` یا `questions` است. داده‌های اولیه با دادهٔ ذخیره‌شده ترکیب می‌شوند. |
| `hn-article-feedback-v1-${slug}` | آرایهٔ خام دیدگاه‌های محلی مقاله، حداکثر ۵۰ مورد؛ این کلید در قرارداد envelope نسخه‌دار storage.js ثبت نشده و `v1` فقط بخشی از نام کلید است. |
| `hn-article-feedback-v1-${slug}-liked` | وضعیت پسند مقاله به‌صورت رشتهٔ `true`/`false`؛ شمار پایه نمونه است و درخواست عمومی سرور ندارد. |

ممکن است scriptهای دیگر کلید تازه اضافه کنند؛ این جدول هنگام تغییر باید با `rg -n "localStorage|storage\.(get|set|json)|hn-" Shop/wwwroot/shop/js Shop/Views` تطبیق داده شود.

## رویدادهای سفارشی

| Event | payload فعلی | listenerهای مهم/اثر |
|---|---|---|
| `hn:cart-change` | `detail`: آرایهٔ cart نرمال‌شده | header/panel، commerce و checkout را sync می‌کند |
| `hn:cart-added` | `detail: { product, items, trigger }` | پنل cart ممکن است باز شود؛ focus trigger را حفظ کنید |
| `hn:cart-stock-limit` | `detail: { product, stock, maxCartQuantity, limit, requested, remaining, reason }`؛ reason برابر `stock` یا `cart` | اعلام محدودیت موجودی یا سقف تعداد هنگام add/update؛ بازخورد آن در `main.js` نمایش داده می‌شود |
| `hn:auth-change` | `detail`: boolean | هدر و بخش account را تازه می‌کند |
| `hn:auth-required` | `detail: { resume: Function|null }` | بازکردن مدال ورود؛ callback پس از ورود آزمایشی اجرا می‌شود |
| `hn:lists-change` | بدون `detail` | شمارنده‌ها، favorite/compare button و صفحه‌ها را sync می‌کند |
| native `storage` | `key`, `newValue`, `oldValue` | همگام‌سازی tabهای دیگر؛ same-tab تغییرات باید از helper/event عبور کند |

هنگام افزودن event، یک source of truth حفظ کنید، payload را document کنید، handler را idempotent بنویسید و از dispatch مضاعف در View و shared JS جلوگیری کنید.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۲۸ اکتبر ۲۰۲۶.
