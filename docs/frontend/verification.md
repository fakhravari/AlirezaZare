# عیب‌یابی و بازبینی رابط کاربری

## برای CSS/ظاهر

1. URL دقیق، عنصر، parentها، متن/کلاس قابل مشاهده و viewport را ثبت کنید.
2. route را تا View و partial دنبال کنید؛ اگر markup با JS رندر می‌شود، renderer مربوط را هم پیدا کنید.
3. selector را در کل repo جست‌وجو کنید و مالکیت/ترتیب فایل‌ها را با `_Shop.cshtml` تطبیق دهید.
4. در DevTools → **Computed** property مشکل‌دار را باز کنید؛ rule برنده، فایل/خط، specificity، خط‌خورده‌ها و media query فعال را یادداشت کنید.
5. classهای state (`open`, `active`, `selected`, `is-*`, `aria-expanded`) و style inline را کنترل کنید؛ تفاوت state اغلب به‌اشتباه «رنگ CSS» گزارش می‌شود.
6. قانون متناقض را در منبع مالک اصلاح کنید؛ selector را scoped و token-based نگه دارید.
7. hard refresh؛ Network → CSS request و URL نسخه‌دار؛ سپس home/inner، desktop/mobile و hover/focus را مقایسه کنید.

## برای JavaScript/تعامل

1. Network تأیید کند فایل اجرا شده؛ Console را برای error و اجرای زودهنگام بررسی کنید.
2. ترتیب script، وجود عنصر هنگام binding، `DOMContentLoaded`، optional chaining/guard و duplicate listener را بررسی کنید.
3. از Elements/Console وضعیت DOM را قبل و بعد از event مقایسه کنید؛ delegated handler ممکن است عنصر تازه‌ساخته را هدف گرفته باشد.
4. event name و `detail`, storage key/value، `storage` cross-tab، `history`/query params و renderer را دنبال کنید.
5. UI را در حالت empty، populated، invalid/malformed storage، open/closed و keyboard اجرا کنید؛ از پاک‌کردن storage کاربر بدون اطلاع خودداری کنید.

## جدول نشانه → محل شروع

| نشانه | اول بررسی شود |
|---|---|
| همهٔ صفحات رنگ/فونت/بردر ناسازگار دارند | `design-tokens.css`, `style.css`, `electrical-theme.css`, `industrial-system.css` و rule برنده |
| فقط خانه یا inner header خراب است | `_HomeHeader`/`_InnerHeader`, `HeaderVariant`, `body.home-page`/`inner-page` |
| مگامنو با حرکت pointer بسته یا بی‌دلیل باز می‌شود | trigger واقعی، gap، `pointerleave` و state controller در `header-navigation.js`؛ CSS نباید از hover/focus wrapper منو را باز کند |
| تغییر CSS صفحه اعمال نمی‌شود | load order بعد از `@section Styles`, selector specificity و cache |
| ظاهر با hover اولیه متفاوت است | کلاس active پیش‌فرض، hover/focus CSS، JS category initialization |
| drawer/modal اسکرول نمی‌خورد | ارتفاع viewport، `overflow-y`, flex child min-height، scroll lock، backdrop و breakpoint |
| count/cart/list بین tabها فرق دارد | helper `HNUI`, `hn:*` eventها و native `storage` listener |
| route فقط گاهی render خراب دارد | URL/hash/query/popstate، localStorage demo و script صفحه |
| عکس crop/اندازه متفاوت است | container aspect ratio، `object-fit/object-position` و CSS card owner |

## ماتریس بازبینی

### الزام هر تغییر رابط

هر تغییر رابط یا رفتار نمایشی باید در موبایل، تبلت و دسکتاپ بازبینی شود. دامنهٔ بررسی فقط route اصلی نیست: همهٔ زیرمسیرها و aliasهای مرتبط، حالت‌های باز و بستهٔ modal/drawer، و مصرف‌کننده‌های اصلی مؤلفهٔ مشترک نیز باید پوشش داده شوند. برای هر صفحه، دست‌کم عرض‌های نمایندهٔ موبایل، تبلت و دسکتاپ و breakpointهای دو سوی مرزهای مرتبط بررسی شوند؛ نتیجه و موارد باز در راهنمای همان حوزه ثبت شود. این الزام برای تغییرهای جزئی CSS هم برقرار است.

این ماتریس مرجع مشترک بازبینی responsive است و راهنماهای حوزه‌ای باید به آن ارجاع دهند؛ اگر تغییر روی route یا جزء مشترک اثر می‌گذارد، فهرست routeها را از [نقشهٔ مسیریابی](../architecture/routing.md) و مالک را از `tools/ui-owners.json` استخراج کنید.

برای تغییر صفحه‌ای، route هدف + حداقل یک consumer مشترک را بررسی کنید. برای component مشترک، همهٔ مصرف‌های اصلی را با matrix زیر پوشش دهید:

| محور | حالت‌هایی که باید تست شوند |
|---|---|
| Viewport | 320، 360–390px موبایل، 575–640px، 768px، 992px، 1200px+؛ در breakpoint مشکوک یک px دو طرف مرز |
| State داده | خالی، یک آیتم، چند آیتم/عنوان بلند، خطا/ورودی نامعتبر، دادهٔ ذخیره‌شدهٔ قدیمی |
| تعامل | pointer hover، click، keyboard Tab/Enter/Escape، focus-visible، close و بازگشت focus |
| نمایش | overflow افقی، scroll داخلی modal/drawer، clipping، z-index، image fit، contrast و متن طولانی فارسی/RTL |
| Runtime | Network/CSS/JS load، Console بدون خطای مرتبط، تغییر tab/storage و back/forward برای URL-driven UI |
| Motion/print | `prefers-reduced-motion`; print برای مقاله‌ای که دکمه/stylesheet چاپ دارد |

در بررسی بصری، breakpointها را از کد actual استخراج کنید؛ 360/390/768/1024/1440 فقط smoke widths هستند و همهٔ مرزهای CSS را پوشش نمی‌دهند. از DevTools device emulation برای viewport استفاده کنید، اما تعامل واقعی pointer/touch را هم در صورت نیاز بررسی کنید.

تست header در `store.spec.mjs` برای هر route یک بار بارگذاری می‌کند و سپس breakpointهای 992 تا 1440px را با تغییر viewport می‌سنجد؛ drawer در 1024px جداگانه کلیک می‌شود. route را برای هر عرض دوباره بارگذاری نکنید، چون هم زمان تست بی‌جهت بالا می‌رود و هم timeout می‌تواند به‌اشتباه شبیه مشکل کلیک دیده شود.

برای هر تغییر رابط، پوشش پایهٔ خودکار پروژه همهٔ routeهای اصلی را در 320، 390، 768، 1024 و 1440px می‌سنجد؛ aliasهای مستندشده نیز باید به مقصد canonical برسند. تست دسترس‌پذیری routeها با axe و قواعد WCAG 2.0/2.1/2.2 سطح A و AA در desktop و mobile، نام سند و کنترل‌ها، زبان/جهت سند، یکتایی شناسه‌ها، مقصد ارجاع‌های ARIA، landmark اصلی، عنوان H1 و alt تصویرها را بررسی می‌کند. آزمون جداگانه با Tab و Enter لینک پرش به محتوا را روی خانه، محصول و حساب در دسکتاپ و موبایل کنترل می‌کند. دیالوگ‌های اصلی محصول، حساب، آدرس و فاکتور نیز هنگام بازبودن با axe بررسی می‌شوند. این سنجش مکمل بررسی دستی حالت‌هاست. modal/drawerهای اختصاصی باید دست‌کم در موبایل کوچک، عرض تبلت 768px و دسکتاپ باز شوند و حدود viewport، اسکرول داخلی و امکان بستنشان بررسی شود.

آزمون خودکار جای اجرای صفحه‌خوان واقعی را نمی‌گیرد. برای بستن ممیزی دسترس‌پذیری، مسیرهای اصلی را با صفحه‌خوان فارسی و انگلیسی و کلیدهای جهت‌یابی مرور کنید و خواندن landmarkها، عنوان‌ها، خطاهای فرم، وضعیت‌های پویا و دیالوگ‌ها را ثبت کنید؛ سنجش RTL را نیز در همین ماتریس نگه دارید.

برای ناوبری مشترک، علاوه بر hover، با Tab و Shift+Tab لینک‌های داخل منوی دسترسی سریع و پیشنهادهای جست‌وجوی drawer موبایل را پیمایش کنید؛ focus-visible باید مستقل از رنگ hover دیده شود.

برای شبکهٔ علاقه‌مندی پروفایل، مرزهای اختصاصی ۴۷۹/۴۸۰، ۱۰۹۹/۱۱۰۰ و ۱۲۷۹/۱۲۸۰px را هم جداگانه بررسی کنید؛ کارت‌ها در این نقاط از یک به دو، دو به سه و سه به چهار ستون می‌روند. وضعیت و breakpointهای جاری در [راهنمای پروفایل](../account/profile.md) ثبت شده‌اند.

## وضعیت بازبینی سراسری صفحات

در ۵ نوامبر ۲۰۲۶، `tests/browser/page-layouts.spec.mjs` گذشت: ۲۶ مسیر اصلی در عرض‌های ۳۲۰، ۳۹۰، ۷۶۸، ۱۰۲۴ و ۱۴۴۰px در پروژه‌های مرورگر desktop و mobile پاسخ ۲۰۰، وجود main و نبود overflow افقی داشتند؛ aliasها و صفحه‌های خطا نیز به مقصد/status مورد انتظار رسیدند. این اجرا ۹ موفق و یک skip وابسته به دستگاه داشت. `tests/browser/content-interactions.spec.mjs` نیز ۵ موفق و یک skip داشت؛ `npm run check`، کنترل مستندات و `git diff --check` موفق بودند.

**ادامهٔ لازم:** نتیجهٔ ماتریس خودکار smoke است و جای بازبینی دیداری را نمی‌گیرد. نمای نخست هر ۲۶ مسیر اصلی در Chrome در عرض‌های ۳۲۰، ۷۶۸ و ۱۴۴۰px مرور شد. در ۵ نوامبر ۲۰۲۶، مرتب‌سازی Search در ۳۲۰px طوری اصلاح شد که گزینه‌ها زیر عنوان در چند ردیف بیایند و scrollbar افقی یا گزینهٔ نیمه‌بریده نماند؛ آزمون رگرسیون عرض‌های ۳۲۰، ۳۶۰ و ۳۹۰px افزوده و موفق شد. رفتار sticky و اسکرول داخلی ستون مقاله در دسکتاپ، پنل سبد، فیلتر و منوی حساب در ۳۲۰px دیداری بررسی شدند. دیالوگ اطلاعات جانبی، گالری و تاریخچهٔ قیمت کالا، ویرایش پروفایل در ۳۲۰/۷۶۸/۱۴۴۰px، فاکتور سفارش، افزودن نشانی و آدرس سبد در موبایل نیز باز شدند؛ تأیید حذف نشانی را باز کردیم و لغو کردیم. هیچ‌کدام از قاب viewport بیرون نزدند و فرم‌های نشانی ذخیره نشدند. آزمون ماتریس ۹ موفق/۱ skip، تعاملات محتوا ۵ موفق/۱ skip و مجموعهٔ دیالوگ‌ها ۲۴ موفق/۴ skip وابسته به دستگاه داشتند. پنجرهٔ تأیید ثبت دیدگاه مجله نیز در Chrome دسکتاپ با متن ساختگی بازبینی و لغو شد؛ دیدگاه ذخیره نشد. پنجرهٔ ورود مهمان در Chrome دسکتاپ با مبدأ ایزوله بازبینی و بسته شد؛ هیچ فرمی ارسال نشد. آزمون‌های مرورگر زبانه‌های ورود و ثبت‌نام را در موبایل ۳۲۰×۶۴۰ و تبلت ۷۶۸×۸۰۰ از نظر قرارگیری کامل پنجره در viewport می‌سنجند؛ پس از افزودن کنترل زبانهٔ ثبت‌نام، هر ۱۲ آزمون دیالوگ موبایل/دسکتاپ گذشت. نشست اصلی Chrome و داده‌های آن دست‌نخورده ماند. هر ایراد را در فایل مالکِ `tools/ui-owners.json` اصلاح کنید؛ پایان کار به ماتریس، `npm run check`، کنترل مستندات و `git diff --check` نیاز دارد.

پس از انتقال استایل toggle منو به `shared-header.css`، ماتریس `page-layouts.spec.mjs` دوباره در پروژه‌های desktop و mobile اجرا شد: ۲۶ مسیر اصلی در عرض‌های ۳۲۰، ۳۹۰، ۷۶۸، ۱۰۲۴ و ۱۴۴۰px، با ۱۱ تست موفق و یک skip شرطی. breakpoint رنگ toggle در خانه و مقاله به‌طور جداگانه در ۳۲۰، ۳۹۰، ۷۶۸، ۹۹۱، ۹۹۲، ۱۰۲۴ و ۱۱۹۹px کنترل شد.

در ادامهٔ همین بازبینی، نمایش لوگوی هدر نیز در breakpointهای ۹۹۱/۹۹۲px پوشش داده شد؛ تست هدفمند desktop موفق و skip موبایل وابسته به دستگاه بود. اتصال Chrome برقرار شد، ولی ناوبری تازه به `https://bs-shop.ir/` تا پایان مهلت پاسخ نداد؛ این اجرا تأیید دیداری تازه‌ای برای نسخهٔ آنلاین محسوب نمی‌شود و باید در تلاش بعدی بررسی شود.

در ۵ نوامبر ۲۰۲۶، کل Playwright روی `localhost` پس از آخرین پاک‌سازی CSS دوباره اجرا شد: ۹۲ موفق و ۱۶ skip وابسته به دستگاه از ۱۰۸ تست؛ شکست نداشت. `npm test` نیز ۳۷ از ۳۷ موفق بود. `npm run check`، `node docs/development/check-docs.mjs` و `git diff --check` نیز موفق شدند. این اجرا کنترل خودکار است و جای بازبینی دیداری دستی در Chrome را نمی‌گیرد؛ در این مرحله مرور فقط به localhost محدود بود.

پس از انتقال قاب آیکن‌های دسته‌بندی drawer، تست هدفمند روی localhost موفق شد: موبایل پاس و دسکتاپ به‌علت دستگاه skip شد. اندازهٔ آیکن‌ها ۳۱px و قاب لینک‌های دیگر ۲۸px باقی ماند.

پس از حذف selectorهای بی‌مصرف `.header-cart`، `header-guest-panels.spec.mjs` و `page-layouts.spec.mjs` روی localhost جمعاً ۲۳ موفق و ۷ skip وابسته به دستگاه داشتند؛ ۲۶ مسیر و پنل‌های واقعی هدر/سبد بررسی شدند.

پس از حذف دو `!important` زائد `display:flex` در هدر خانه، آزمون‌های هدر، layout و فروشگاه روی localhost دوباره اجرا شدند: ۷۱ موفق و ۱۵ skip وابسته به دستگاه از ۸۶ تست؛ ماتریس ۲۶ مسیر در موبایل، تبلت و دسکتاپ موفق بود.

پس از حذف دو اولویت پوشیده از مگامنوی داخلی، تست آن در viewport دسکتاپ localhost گذشت و رنگ لینک گروه `rgb(82, 101, 121)` و شعاع دکمه `8px` را تأیید کرد؛ پروژهٔ mobile طبق انتظار skip شد.

## فرمان‌های ردیابی

```powershell
git status --short
rg -n "نام-کلاس|#نامId|data-نام" Shop/Views Shop/wwwroot/shop/css Shop/wwwroot/shop/js
rg -n "@media" Shop/wwwroot/shop/css
rg -n "localStorage|storage\.(get|set|json)|hn:" Shop/wwwroot/shop/js Shop/Views
rg -n "<link|<script|RenderSection|RenderBody" Shop/Views/Shared/_Shop.cshtml
dotnet build Shop.sln
```

برای تغییر CSS/JS، ابتدا diff و `git status` را بررسی کنید، فقط فایل مالک را تغییر دهید، `npm run check` و `git diff --check` اجرا کنید و سپس صفحهٔ مرتبط را با matrix این سند بررسی کنید. [کنترل‌های خودکار و CI](../development/quality.md) unit، قرارداد C#، HTTP و مرورگر را پوشش می‌دهند؛ build موفق به‌تنهایی رفتار بصری یا JS را ثابت نمی‌کند.



مسئولیت پوسته و landmark در [layout](layout.md)، رفتار پروفایل در [حساب](../account/profile.md) و تراز کارت خانه در [فروشگاه](../store/catalog.md) ثبت می‌شود. پس از کشف اختلاف، راهنمای حوزهٔ مسئول را اصلاح کنید.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۱ نوامبر ۲۰۲۶.
