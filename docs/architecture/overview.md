# معماری فعلی پروژه

solution دو برنامهٔ ASP.NET Core MVC دارد: فروشگاه عمومی [Shop](../../Shop/Shop.csproj) و پنل مدیریتی [Shop.Admin](../../Shop.Admin/Shop.Admin.csproj)، هر دو با هدف `net10.0`. پنل به assembly دیتابیس [Shop.Database](../../Shop.Data/Shop.Data.csproj) با فولدرهای Domain، Application و Infrastructure ارجاع دارد. پروژهٔ قرارداد [Shop.Contracts](../../tests/Shop.Contracts/Shop.Contracts.csproj) برای بررسی قراردادهای C# است. مرز داده و وضعیت فعلی پنل در [راهنمای دیتابیس](database.md) و [راهنمای مدیریت](../admin/README.md) ثبت شده است.


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

[Shop/Program.cs](../../Shop/Program.cs) فروشگاه و [Shop.Admin/Program.cs](../../Shop.Admin/Program.cs) پنل را پیکربندی می‌کنند. پنل از پوستهٔ RTL قالب Color Admin استفاده می‌کند، مرور داخلی را با AJAX انجام می‌دهد و پاسخ‌ها را بدون cache می‌فرستد. احراز هویت و مجوزهای مستقل مدیر، بازبینی بازخورد و عملیات اسناد و تعاریف انبار پیاده شده‌اند. مدیریت کامل سفارش، پرداخت واقعی و تأیید سفارش در انبار هنوز متصل نیستند؛ وضعیت هر بخش در [راهنمای مدیریت](../admin/README.md) و [مرزهای داده](data-sources.md) ثبت شده است.

[PageController](../../Shop/Controllers/PageController.cs) فقط View متناظر یا `ViewName` تعیین‌شده در مدل را برمی‌گرداند. نام `PageAsync` به معنی دسترسی async به دیتابیس نیست؛ پیاده‌سازی فعلی از `Task.FromResult` استفاده می‌کند.

## مسئولیت پوشه‌ها

| مسیر | مسئولیت |
|---|---|
| `Shop/Program.cs` | پیکربندی pipeline و route فروشگاه عمومی |
| `Shop.Admin/Program.cs` | پیکربندی routeهای پنل؛ سرویس دیتابیس را در صورت وجود connection string ثبت می‌کند |
| `Shop.Admin/Areas/Admin/` | پیشخوان کل و ورود به حوزه‌های مدیریت |
| `Shop.Admin/Areas/Store/` | پیشخوان فروشگاه، کاتالوگ خواندنی و دامنه‌های مدیریتی فروشگاه |
| `Shop.Admin/Areas/Warehouse/` | پیشخوان، فهرست انبارها و موجودی خواندنی و دامنه‌های عملیات انبار |
| `Shop.Admin/wwwroot/color_admin/` | قالب Color Admin؛ برنامه فقط assetهای پایهٔ پوستهٔ RTL را بارگذاری می‌کند |
| `Shop/Controllers/` | مسیرها و آماده‌سازی مدل‌های صفحه |
| `Shop/Controllers/Api/` | endpointهای HTTP؛ فقط از سرویس‌های Application استفاده می‌کنند |
| `Shop.Data/Domain/` | موجودیت‌های دامنهٔ مستقل از دیتابیس |
| `Shop.Data/Application/` | قراردادها، مدل‌های پاسخ و سرویس‌های کاربردی |
| `Shop.Data/Infrastructure/` | EF Core، `ShopDbContext` و repositoryهای SQL Server |
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
| `Shop/wwwroot/content/data/` | حذف شده؛ دادهٔ ساختاریافتهٔ runtime از queryهای دیتابیس و API ارائه می‌شود |
| `Shop/App_Data/` | دادهٔ نمونهٔ سروری که نباید به‌عنوان فایل عمومی در `wwwroot` قرار بگیرد |
| `Shop/wwwroot/content/products/` | رسانه‌ها در پوشه‌های مشترک بر اساس نوع؛ نام فایل شامل محصول، کد کالا و کاربرد نسخه است و مسیر ویدئو در خروجی کاتالوگ ساخته‌شده از روابط محصول/رسانه ثبت می‌شود |
| `Shop/wwwroot/shop/images/`, `vendor/` | رسانه‌های قالب، فونت‌ها و وابستگی‌های محلی؛ پیش از جایگزینی/حذف، تمام ارجاعات را بررسی کنید |

`wwwroot/content/` مرز داده‌های عمومی و رسانهٔ وابسته به محتوای سایت است؛ `wwwroot/shop/` محل قالب و منابع UI است. جزئیات رسانه در [راهنمای رسانه](../media/images.md) قرار دارد. artifactهای `ClockTemplate/` را برنامهٔ source عضو solution فرض نکنید؛ قبل از استفاده عضویت و منبع آن را بررسی کنید.

## قرارداد نام‌گذاری و دسته‌بندی

- نام پوشه‌های MVC و Viewها از الگوی PascalCase چارچوب پیروی می‌کند؛ پوشهٔ View با نام Controller متناظر است و نام View معمولاً `Index.cshtml` یا `Details.cshtml` است. نام کلاس و فایل C# نیز PascalCase است.
- نام فایل‌های CSS، JavaScript، JSON و assetهای محتوایی lowercase-kebab-case است. دادهٔ runtime در فایل محلی نگهداری نمی‌شود؛ منبع آن SQL Server/API است و رفتار مرورگر زیر `wwwroot/shop/js/{core,layout,pages,store}/` نگهداری می‌شود.
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

آخرین تطبیق با کد: ۸ نوامبر ۲۰۲۶.
