# کنترل کیفیت و قراردادهای پایدار

## فرمان‌های محلی

پیش‌نیاز: .NET SDK 10 و Node.js 24. از ریشهٔ مخزن اجرا کنید؛ در PowerShell با محدودیت اجرای npm.ps1 از `npm.cmd` استفاده کنید.

```powershell
npm ci --ignore-scripts --no-fund --no-audit
npm run check
npm test
dotnet build Shop.sln
dotnet run --no-build --project tests/Shop.Contracts
npm run test:http
git diff --check
```

`test:http` باید پس از build اجرا شود؛ DLL ساخته‌شده را روی پورت آزاد با محیط Development اجرا و پس از بررسی متوقف می‌کند. هم‌زمان با build روی همان DLL اجرا نکنید.

| کنترل | پوشش | محدودیت |
|---|---|---|
| `npm run check` | syntax همهٔ JSهای برنامه، parse CSS، مالکیت component، سقف !important، ترتیب dependency و ساختار/لینک docs | جای بررسی بصری را نمی‌گیرد |
| `npm test` | شناسه/URL/رسانهٔ قبل و بعد از انتقال، جابه‌جایی ترتیب کاتالوگ، immutable بودن، روابط، مسیر واقعی فایل‌ها، قیمت/موجودی سبد، migration و mount/event/renderer | منطق JS در محیط VM؛ فاقد layout مرورگر |
| `tests/Shop.Contracts` | خواندن واقعی CatalogSnapshot، رد داده‌های ناسازگار و JSON امن برای script | برنامهٔ contract با exit code، بدون وابستگی تست خارجی |
| `npm run test:http` | پاسخ مسیرهای اصلی، کاتالوگ مشترک صفحات، redirect canonical، 404، sitemap و تصویرهای تمام صفحات sitemap/حساب و CSSهای مصرف‌شده | JS صفحه را اجرا نمی‌کند؛ کنترل مسیر تصاویر در [رسانه](../media/images.md) توضیح داده شده است |
| `npm run test:browser` | خانه/جستجو/محصول/گالری/سبد و منوی دسکتاپ در عرض ۱۴۴۰ و موبایل ۳۶۰ | نیازمند مرورگر Playwright؛ اجرای واقعی جدا از فهرست‌کردن تست‌هاست |

تست‌های رفتاری مرورگر در [store.spec.mjs](../../tests/browser/store.spec.mjs) و تنظیمات در [playwright.config.mjs](../../playwright.config.mjs) هستند. `npm run test:browser:list` فقط کشف و اعتبار تنظیمات تست را بررسی می‌کند؛ موفقیت آن به معنی اجرای مرورگر نیست. در محیط توسعهٔ عادی پس از نصب مرورگر آزمایشی با `npx playwright install chromium` می‌توان تست‌ها را اجرا کرد. اگر محیط عامل، ابزار مرورگر مشخصی الزام کرده، از همان ابزار برای بررسی محلی استفاده کنید و این فرمان را مسیر دورزدن محدودیت اتصال قرار ندهید.

## مالکیت و جلوگیری از برگشت بدهی

[ui-owners.json](../../tools/ui-owners.json) مالک markup/data/server/JS/CSS کاتالوگ، کارت، هدر، سبد و هستهٔ UI را ثبت می‌کند. [check-ui.mjs](../../tools/check-ui.mjs) مسیرهای مسئول و ترتیب بارگذاری را کنترل می‌کند. قبل از ساخت override یا renderer، این نقشه را بخوانید.

[ui-legacy-budget.json](../../tools/ui-legacy-budget.json) امضای قواعد فعلی component در فایل‌های قدیمی و سقف !important هر CSS را ثبت می‌کند. قواعد قبلی می‌توانند حذف یا به مالک منتقل شوند؛ افزودن/تغییر override خارج مالک با check رد می‌شود. برای رفع خطا baseline را یک‌جا بازتولید نکنید؛ قانون را به مالک ببرید. اگر انتقال کنترل‌شده به تغییر سقف نیاز داشت، diff دقیق همان ورودی، علت انتقال و نتیجهٔ بررسی مصرف‌کننده‌ها را ثبت کنید. امضاها مستقل از CRLF/LF هستند تا Windows و Linux نتیجهٔ یکسان بدهند.

۶۸ قانون اختصاصی کارت از industrial-system.css به shared-product-card.css منتقل شده‌اند. سایر قواعد legacy هنوز وجود دارند؛ مهاجرت بیشتر باید مرحله‌ای و همراه بررسی بصری باشد. حذف کامل CSSهای قدیمی انجام نشده است.

## CI

[quality.yml](../../.github/workflows/quality.yml) دو job دارد: قراردادها/build/HTTP و مرورگر دسکتاپ/موبایل. شکست browser گزارش HTML، trace و تصویر خطا را هفت روز نگه می‌دارد. CI پس از push یا pull request اجرا می‌شود؛ وجود فایل workflow به معنی اجرای موفق آن در GitHub نیست. قیمت، auth و پرداخت این تست‌ها دمو و در context جداگانهٔ آزمایشی‌اند.

پس از تغییر، فرمان‌های اجراشده، نتیجه و محدودیت بررسی را دقیق گزارش کنید. [ماتریس UI](../frontend/verification.md) مرجع بررسی بصری، RTL و دسترس‌پذیری است.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۲۱ اکتبر ۲۰۲۶.
