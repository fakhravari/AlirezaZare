# منابع داده و مرزهای فعلی

این سند منبع runtime هر دادهٔ فروشگاه را مشخص می‌کند. داده‌های کاتالوگ، برند، FAQ، مجله، حساب نمونه، نشانی جغرافیایی و طرح فروش از SQL Server خوانده می‌شوند؛ فایل‌های seed فقط در migrationها باقی می‌مانند تا پایگاه تازه مقداردهی شود.

| داده | منبع runtime | وضعیت |
|---|---|---|
| نام فروشگاه، نام نمایشی، نام بصری، تلفن، WhatsApp و ایمیل عمومی | `[Store].[SiteSetting]` از `IStoreSiteIdentityRepository` و `DatabaseSiteIdentitySource` | ردیف singleton با `Id = 1`؛ در هر درخواست صفحه از middleware به مصرف‌کننده‌ها می‌رسد و یک دقیقه در حافظه cache می‌شود. مقدارهای `SiteIdentity` از appsettings حذف شده‌اند. |
| محصول، برند، دسته، ویژگی، رسانه و فهرست خانه | query رابطه‌ای از `IStoreRuntimeDatasetRepository` و `CatalogRepository` | دیتابیس؛ `[Store].[Category].[IconClass]` و روابط Store/Warehouse منبع‌اند؛ فایل‌های `wwwroot/content/data/*.json` حذف شده‌اند. |
| محتوای تحریریهٔ برند | query روی `Store.Brand` و `Store.BrandEditorial` | دیتابیس؛ دادهٔ ساخت‌یافته از migration 007 وارد شده است. |
| مجله، مقاله و متادیتا | query روی جدول‌های schema `Mag` از `DatabaseMagazineContentSource` | دیتابیس؛ آرشیو، صفحهٔ خانه، جزئیات و sitemap از همین منبع می‌خوانند. |
| FAQ | جدول‌های `Store.FaqItem` و `Store.ContentPage` | دیتابیس؛ برای صفحه و FAQPage JSON-LD استفاده می‌شود. |
| کوپن عمومی و سفارش حساب | کوپن عمومی از `Store.Coupon` از طریق `DatabaseAccountDataSource`؛ state حساب از API احرازشده | منبع عمومی سفارش و سابقهٔ مصرف کوپن را برنمی‌گرداند؛ فهرست مصرف کوپن در projection عمومی خالی است. جزئیات در [پروفایل](../account/profile.md). |
| کشور، استان و شهر | جداول `Geo.Country`, `Geo.Province`, `Geo.City` و API جغرافیا | کشور برند و پرچم کاتالوگ نیز از `Geo.Country` می‌آیند؛ API نشانی فقط کشور دارای استان را برمی‌گرداند. `iran-places.js` فقط درخواست AJAX و سازگاری UI را فراهم می‌کند. |
| طرح فروش، قیمت، offer و موجودی | مرجع طرح در `Warehouse.SalesPlanReference`، پیشنهادها در Store و موجودی در Warehouse از `StoreSalesPlanRepository` | دیتابیس؛ APIهای خواندن، quote و validate به repository متصل‌اند. پرداخت واقعی هنوز وصل نیست. صدور حوالهٔ سفارش‌های دارای پرداخت تأییدشده در [راهنمای مدیریت](../admin/README.md) شرح داده شده و نیازمند migration است. طبق نیازمندی، رزرو پیش از پرداخت نداریم. |
| دادهٔ شخصی/تعاملی کاربر | API حساب و `[Account]`؛ API محصول و `[Store].[ProductFeedback]` | سبد، نشانی، فهرست‌ها، checkout، سفارش، پروفایل، جست‌وجوهای اخیر و تعامل مقاله از API حساب‌اند؛ نشانی فقط FKهای جغرافیایی را ذخیره می‌کند و نام‌ها با join به `Geo` خوانده می‌شوند؛ checkout نام‌های خوانده‌شده را snapshot می‌کند. دیدگاه و پرسش محصول از API محصول‌اند. مرجع خواندن و نوشتن API است؛ state صفحه فقط برای نمایش جاری نگه داشته می‌شود. کلیدهای legacy پس از انتقال موفق به API حذف می‌شوند. سفارش‌های seed همچنان دادهٔ نمایشی‌اند. |
| تصویر و asset ثابت | فایل‌های `Shop/wwwroot/content` | فایل ثابت رسانه است؛ رکورد و مسیر قابل‌نمایش کالا از دیتابیس می‌آید. |
| متن ثابت UI و route | Controller، View و `PageModel` | بخشی از کد محصول است، نه رکورد دادهٔ runtime. |

## قرارداد اتصال

لایهٔ دسترسی فروشگاه در پروژهٔ یکپارچهٔ [Shop.Database](../../Shop.Data) قرار دارد. قراردادهای خواندن در `Application`، مدل‌های دامنه در `Domain` و پیاده‌سازی SQL Server در `Infrastructure` هستند. اتصال برنامه از `ConnectionStrings:DefaultConnection` در appsettings خوانده می‌شود؛ دادهٔ `SiteIdentity` در آن قرار ندارد. جزئیات schema و migrationها در [راهنمای دیتابیس](database.md) است.

Migrationهای 007 و 008 داده‌های runtime کاتالوگ، تحریریه، حساب نمونه، مجله و جغرافیا را وارد دیتابیس می‌کنند. Migration 020 پانزده راهنمای تازه را به seed مجله می‌افزاید؛ تعداد دادهٔ نمونهٔ این نسخه ۵۱ مقاله است. پس از انتقال، فایل‌های منبع قبلی از Shop حذف شده‌اند؛ نسخهٔ قابل‌بازسازی داده فقط در migrationهای seed نگهداری می‌شود.

## موارد باقی‌مانده

cache محصول حذف شده است؛ اطلاعات فهرست، سبد، آدرس، checkout، پیگیری قیمت، جست‌وجوی اخیر و تعامل مقاله از API حساب خوانده و به همان API نوشته می‌شوند. نظر محصول از API خوانده و ثبت می‌شود و برای کاربر واردشده به پروفایل او متصل است. ایجاد سفارش در دیتابیس پایدار است؛ درگاه پرداخت واقعی هنوز پیاده نشده و اجرای migrationهای حواله در محیط هدف لازم است.

[کاتالوگ فروشگاه](../store/catalog.md) · [قرارداد JavaScript](../frontend/javascript.md) · [لایهٔ دیتابیس](database.md)
