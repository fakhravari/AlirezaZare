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
| مگامنو با حرکت pointer بسته می‌شود | gap بین trigger/panel، `pointerleave`, stacking/overlay، eventها در `main.js` و CSS min-width 992 |
| تغییر CSS صفحه اعمال نمی‌شود | load order بعد از `@section Styles`, selector specificity و cache |
| ظاهر با hover اولیه متفاوت است | کلاس active پیش‌فرض، hover/focus CSS، JS category initialization |
| drawer/modal اسکرول نمی‌خورد | ارتفاع viewport، `overflow-y`, flex child min-height، scroll lock، backdrop و breakpoint |
| count/cart/list بین tabها فرق دارد | helper `HNUI`, `hn:*` eventها و native `storage` listener |
| route فقط گاهی render خراب دارد | URL/hash/query/popstate، localStorage demo و script صفحه |
| عکس crop/اندازه متفاوت است | container aspect ratio، `object-fit/object-position` و CSS card owner |

## ماتریس بازبینی

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

## فرمان‌های ردیابی

```powershell
git status --short
rg -n "نام-کلاس|#نامId|data-نام" Shop/Views Shop/wwwroot/shop/css Shop/wwwroot/shop/js
rg -n "@media" Shop/wwwroot/shop/css
rg -n "localStorage|storage\.(get|set|json)|hn:" Shop/wwwroot/shop/js Shop/Views
rg -n "<link|<script|RenderSection|RenderBody" Shop/Views/Shared/_Shop.cshtml
dotnet build Shop.sln
```

برای تغییر CSS/JS، ابتدا diff و `git status` را بررسی کنید، فقط فایل مالک را تغییر دهید، `git diff --check` اجرا کنید و سپس صفحهٔ مرتبط را با matrix این سند بررسی کنید. build برای تغییر Razor/C# مناسب است؛ build موفق به‌تنهایی رفتار بصری یا JS را ثابت نمی‌کند.



مسئولیت پوسته و landmark در [layout](layout.md)، رفتار پروفایل در [حساب](../account/profile.md) و تراز کارت خانه در [فروشگاه](../store/catalog.md) ثبت می‌شود. پس از کشف اختلاف، راهنمای حوزهٔ مسئول را اصلاح کنید.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۲۱ اکتبر ۲۰۲۶.
