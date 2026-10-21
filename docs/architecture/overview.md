# معماری فعلی پروژه

این پروژه یک برنامهٔ ASP.NET Core MVC با رابط فارسی و RTL است. `Shop.sln` فقط [پروژهٔ Shop](../../Shop/Shop.csproj) با هدف `net10.0` را اجرا می‌کند. گروه‌های مستندات، مرز مسئولیت را توضیح می‌دهند؛ این دسته‌بندی به معنی وجود سرویس‌ها یا پروژه‌های مستقل نیست.

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
| `Shop/wwwroot/shop/js/store/` | دادهٔ کاتالوگ و رفتار فروشگاهی |
| `Shop/wwwroot/shop/js/pages/` | رفتار مختص یک صفحه یا جریان کاری |
| `Shop/wwwroot/content/products/` | رسانه‌ها در پوشه‌های مشترک بر اساس نوع؛ نام فایل شامل محصول، کد کالا و کاربرد نسخه است و مسیر ویدئو فقط در محصول متناظر `store/catalog.js` ثبت می‌شود |
| `Shop/wwwroot/shop/images/`, `vendor/` | رسانه‌های قالب، فونت‌ها و وابستگی‌های محلی؛ پیش از جایگزینی/حذف، تمام ارجاعات را بررسی کنید |

`wwwroot/content/` مرز رسانهٔ وابسته به داده و `wwwroot/shop/` محل قالب و منابع UI است. جزئیات در [رسانه](../media/images.md) قرار دارد. artifactهای `ClockTemplate/` را برنامهٔ source عضو solution فرض نکنید؛ قبل از استفاده عضویت و منبع آن را بررسی کنید.

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

آخرین تطبیق با کد: ۲۱ اکتبر ۲۰۲۶.
