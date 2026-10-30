# معماری فعلی پروژه

این پروژه یک برنامهٔ ASP.NET Core MVC با رابط فارسی و RTL است. تنها برنامهٔ وب `Shop.sln`، [پروژهٔ Shop](../../Shop/Shop.csproj) با هدف `net10.0` است؛ solution همچنین پروژهٔ اجرایی بررسی قرارداد [Shop.Contracts](../../tests/Shop.Contracts/Shop.Contracts.csproj) را دارد. گروه‌های مستندات، مرز مسئولیت را توضیح می‌دهند؛ این دسته‌بندی به معنی وجود سرویس‌ها یا پروژه‌های مستقل نیست.


مرزهای قابل‌جایگزینی خواندن داده `ICatalogSource`، `IAccountDataSource` و `ICommerceOfferSource` هستند. وضعیت پیاده‌شده و شکاف‌های اتصال در [آمادگی backend](backend-readiness.md) ثبت شده است.

## جریان درخواست

```text
Program.cs / middleware / route
  → Controller: انتخاب صفحه و ساخت PageModel
  → Model: دادهٔ صفحه و SeoModel
  → Razor View + partial
  → _Shop.cshtml: پوسته و ترتیب CSS/JS
  → مرورگر: HNUI، کاتالوگ و rendererهای فروشگاه
```

[Program.cs](../../Shop/Program.cs) MVC، encoding متن فارسی، تنظیمات SEO، فایل‌های استاتیک و routing را تنظیم می‌کند. در محیط غیرتوسعه، handler خطا و HSTS نیز فعال‌اند. دیتابیس، EF Core، احراز هویت سروری و API ثبت سفارش در ترکیب فعلی ثبت نشده‌اند.

[PageController](../../Shop/Controllers/PageController.cs) فقط View متناظر یا `ViewName` تعیین‌شده در مدل را برمی‌گرداند. نام `PageAsync` به معنی دسترسی async به دیتابیس نیست؛ پیاده‌سازی فعلی از `Task.FromResult` استفاده می‌کند.

## مسئولیت پوشه‌ها

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
| `Shop/wwwroot/shop/js/store/` | رفتار فروشگاهی و adapter کاتالوگ؛ دادهٔ JSON در این پوشه قرار نمی‌گیرد |
| `Shop/wwwroot/shop/js/pages/` | رفتار مختص یک صفحه یا جریان کاری |
| `Shop/wwwroot/content/data/` | داده‌های ایستای ساختاریافتهٔ قابل‌ارائه در `wwwroot`، مستقل از کد UI |
| `Shop/App_Data/` | دادهٔ نمونهٔ سروری که نباید به‌عنوان فایل عمومی در `wwwroot` قرار بگیرد |
| `Shop/wwwroot/content/products/` | رسانه‌ها در پوشه‌های مشترک بر اساس نوع؛ نام فایل شامل محصول، کد کالا و کاربرد نسخه است و مسیر ویدئو فقط در محصول متناظر `content/data/catalog.json` ثبت می‌شود |
| `Shop/wwwroot/shop/images/`, `vendor/` | رسانه‌های قالب، فونت‌ها و وابستگی‌های محلی؛ پیش از جایگزینی/حذف، تمام ارجاعات را بررسی کنید |

`wwwroot/content/` مرز داده‌های عمومی و رسانهٔ وابسته به محتوای سایت است؛ `wwwroot/shop/` محل قالب و منابع UI است. جزئیات رسانه در [راهنمای رسانه](../media/images.md) قرار دارد. artifactهای `ClockTemplate/` را برنامهٔ source عضو solution فرض نکنید؛ قبل از استفاده عضویت و منبع آن را بررسی کنید.

## قرارداد نام‌گذاری و دسته‌بندی

- نام پوشه‌های MVC و Viewها از الگوی PascalCase چارچوب پیروی می‌کند؛ پوشهٔ View با نام Controller متناظر است و نام View معمولاً `Index.cshtml` یا `Details.cshtml` است. نام کلاس و فایل C# نیز PascalCase است.
- نام فایل‌های CSS، JavaScript، JSON و assetهای محتوایی lowercase-kebab-case است. فایل داده در پوشهٔ کد قرار نمی‌گیرد: دادهٔ عمومی زیر `wwwroot/content/data/`، منبع سروری خصوصی زیر `App_Data/` و رفتار مرورگر زیر `wwwroot/shop/js/{core,layout,pages,store}/` نگهداری می‌شود.
- `wwwroot/shop/css/{core,layout,pages,store}/` بر اساس دامنهٔ UI دسته‌بندی می‌شود. فایل‌های حجیم `layout/style.css`، `layout/industrial-system.css` و `pages/pages.css` بخشی از قواعد مشترک قالب و زنجیرهٔ فعلی بارگذاری‌اند؛ محل‌شان عمدی است و نباید صرفاً برای نام‌گذاری یا نظم ظاهری جابه‌جا شوند. نام‌های کلی آن‌ها میراث قالب است؛ قاعدهٔ تازه را در stylesheet مالک مؤلفه بنویسید و ترتیب فعلی را در [راهنمای پوسته](../frontend/layout.md) حفظ کنید. جزئیات در [قرارداد CSS](../frontend/styling.md) است.

## از کجا بررسی را شروع کنیم؟

| مسئله | مسیر بررسی |
|---|---|
| صفحه یا URL اشتباه | [مسیرها](routing.md) → Controller → مدل → View |
| داده یا قیمت ناهماهنگ | [منابع داده](data-sources.md) → [کاتالوگ](../store/catalog.md) یا [سبد](../store/cart.md) |
| ظاهر یا منوی مشترک | [پوسته و assetها](../frontend/layout.md) → [مالک CSS](../frontend/styling.md) |
| تعامل یا state مرورگر | [قرارداد JS](../frontend/javascript.md) → راهنمای همان حوزه |
| راه‌اندازی و تغییر امن | [اجرای پروژه](../development/setup.md) و [روند تغییر](../development/workflow.md) |

کد و رفتار runtime مرجع نهایی‌اند؛ اختلاف کشف‌شده باید در راهنمای مسئول همان حوزه اصلاح شود.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۳۰ اکتبر ۲۰۲۶.
