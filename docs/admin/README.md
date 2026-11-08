# پنل مدیریت فروشگاه و انبار

`Shop.Admin` برنامهٔ MVC مستقل پنل است و از پوستهٔ RTL فارسی Color Admin در `Shop.Admin/wwwroot/color_admin/` استفاده می‌کند. نمونهٔ قالب پیش از اتصال به پنل مرور شد: ۱۰۹ صفحهٔ HTML نمونه، پنج نسخهٔ رنگی (default، apple، facebook، google و material)، فونت IRANSansX، وابستگی‌ها و نمونه‌های داشبورد، جدول، فرم، محصول/سفارش و POS دارد. در برنامه فقط `default/app-rtl.min.css`، `vendor.min.css`، `persian-font.css`، `vendor.min.js` و `app.min.js` بارگذاری می‌شوند. فایل‌های demo و plugin کامل در پوشهٔ قالب باقی می‌مانند اما وارد asset manifest/build برنامه نمی‌شوند تا خروجی پنل فقط بخش‌های مصرف‌شده را پردازش کند.

## دسته‌بندی منو

- **پیشخوان کل** در `Areas/Admin`: راه ورود به دو دامنهٔ مستقل.
- **مدیریت فروشگاه** در `Areas/Store`: پیشخوان، کاتالوگ (محصول، دسته‌بندی، برند، ویژگی)، سفارش‌ها و طرح‌های فروش، مشتریان، دیدگاه‌ها، پرسش‌های محصول و محتوا.
- **مدیریت انبار** در `Areas/Warehouse`: پیشخوان، انبارها، موجودی کالا، رسید، حواله، انتقال، اصلاح موجودی، گردش کالا و شرکت‌ها/تأمین‌کنندگان.

صفحهٔ هر دامنه در Area خودش قرار دارد. `Areas/_ViewStart.cshtml` پوستهٔ مشترک `Views/Shared/_Layout.cshtml` را برای تمام Areaها فعال می‌کند. کاتالوگ، سرویس API و View اختصاصی دارد؛ قابلیت‌هایی که هنوز قرارداد مدیریتی ندارند، در `Areas/{Store,Warehouse}/Features/` توصیف می‌شوند و دادهٔ نمونه یا دکمهٔ تغییردهنده نشان نمی‌دهند.

## مسیرها و داده

| صفحه | مسیر | منبع فعلی |
|---|---|---|
| پیشخوان کل | `/Admin/Home/Index` | لینک بخش‌ها، بدون شاخص ساختگی |
| محصولات، دسته‌ها، برندها و ویژگی‌ها | `/Store/Catalog/{Products,Categories,Brands,Attributes}` | `GET /api/admin/store/catalog/{section}` از dataset دیتابیس |
| انبارها | `/Warehouse/Warehouses` | `GET /api/admin/warehouse/warehouses` |
| موجودی کالا | `/Warehouse/Stock` | `GET /api/admin/warehouse/stock` با فیلتر و صفحه‌بندی |
| سایر قابلیت‌ها | `/Store/Features?feature=...` و `/Warehouse/Features?feature=...` | صفحهٔ دامنه و وضعیت API؛ هنوز عملیات داده ندارند |

APIها فقط خواندنی‌اند و از repository/serviceهای `Shop.Database` استفاده می‌کنند. پاسخ‌ها `no-store` هستند؛ کلاینت هم `fetch` را با `cache: "no-store"` اجرا می‌کند. پیمایش داخلی پنل با AJAX و History API انجام می‌شود و صفحه را refresh نمی‌کند. تنظیم پیش‌فرض ذخیره‌سازی Color Admin غیرفعال است و پنل state داده را در `localStorage` یا `sessionStorage` نگه نمی‌دارد.

`Shop.Admin` ابتدا `ConnectionStrings:DefaultConnection` خودش را می‌خواند و اگر تنظیم نشده باشد از `Shop/appsettings.json` همان solution مقدار موجود را می‌گیرد؛ رشتهٔ اتصال کپی یا در source جدید ثبت نشده است. پروژه در صورت نبود connection string هم بالا می‌آید، اما endpointهای دیتابیسی بدون پیکربندی قابل استفاده نیستند.

## مرز قابلیت‌های مدیریتی

این مرحله منو، مسیرها و صفحه‌های RTL را ساخته و کاتالوگ و خواندن انبار را به API وصل کرده است. ثبت و ویرایش کالا/دسته/برند، سفارش و مشتری، moderation دیدگاه/پرسش، و رسید/خروج/انتقال/اصلاح انبار هنوز endpoint ندارند. پیش از افزودن عملیات نوشتن باید احراز هویت و مجوز مدیر تعریف و مدل‌های دامنهٔ لازم به `Shop.Database` اضافه شوند. تا آن زمان صفحه‌ها صریحاً خواندنی‌اند.

برای اجرا از ریشهٔ مخزن: `dotnet run --project Shop.Admin/Shop.Admin.csproj`.
