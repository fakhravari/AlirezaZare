# پنل مدیریت فروشگاه و انبار

پنل وب مستقل `Admin/` به پروژهٔ `Data/` متصل است. صفحه‌ها در `Admin/Areas/Store/Views/` و `Admin/Areas/Warehouse/Views/`، endpointها در `Admin/Controllers/Api/` و رفتار مرورگر در `Admin/wwwroot/js/` قرار دارند. برای یافتن owner رابط، `tools/ui-owners.json` را جست‌وجو کنید.

`Admin/Program.cs` با فراخوانی `AddShopDatabase` سرویس مشترک ایمیل `Shop.Data.Application.Email.Contracts.IEmailSender` را از لایهٔ `Data` دریافت می‌کند؛ پیاده‌سازی SMTP تنظیمات فرستنده را از شناسهٔ ۱ در `StoreSiteIdentity` می‌خواند. این سرویس در DI ادمین در دسترس است و برای هر فرایند مدیریتی با نیاز ارسال ایمیل قابل تزریق است.

## حوزه‌ها

- صفحهٔ تنظیم اسلایدر در `SiteSettings/Slider` تصویر دسکتاپ/موبایل را به تفکیک شش جایگاه فعلی جایگزین می‌کند. کارت‌های نظر صفحهٔ اصلی از نظرهای تأییدشدهٔ محصولات می‌آیند؛ مدیریت وضعیت و پاسخ از صفحهٔ دیدگاه‌های فروشگاه انجام می‌شود.

- کاتالوگ و قیمت: APIهای `StoreCatalogAdminApiController` و `StoreVariantPricesAdminApiController`.
- سفارش و ارسال: `StoreOrdersAdminApiController` و `StorePaidOrdersDispatchApiController`.
- صندوق فرم تماس: صفحهٔ `contact-messages` در `FeaturesController`، فهرست و تغییر وضعیت در `StoreContactMessagesAdminApiController` و رابط در `ContactMessages.cshtml`. مجوز صفحه و API در migration `078-contact-message-inbox.sql` ثبت می‌شود.
- پیام‌های داخل حساب مشتری: صفحهٔ `account-messages` در `FeaturesController` و API `StoreAccountMessagesAdminApiController` برای یافتن مشتری، ثبت پیام و مشاهدهٔ سابقه. مشتری پیام را در `/Profile/Messages` می‌بیند و وضعیت خواندن را ثبت می‌کند.
- انبار: `WarehouseAdminApiController`، `WarehouseDocumentsApiController` و `WarehouseMovementsApiController`. محاسبه و ثبت ledger سند در `Data/Infrastructure/Warehouse/WarehouseStockLedger.cs` و قرارداد آن در `Data/Application/Warehouse/Contracts/IWarehouseStockLedger.cs` است؛ endpointهای Admin همچنان مالک HTTP، مجوز و تراکنش درخواست‌اند.
- محتوای فروشگاه: endpointهای FAQ، دسته، مقاله، صفحات خدمات و رسانه در همین پوشهٔ API.
- حساب مدیر و مجوزها: `Admin/Controllers/AuthController.cs` و مدل‌های `Data/Domain/Admin/Entities/`.
- تنظیمات سایت و SMTP: `SiteSettingsController` و `Admin/Areas/Admin/Views/SiteSettings/Index.cshtml`؛ عمر کد تأیید ایمیل، توضیح فوتر و نشانی شبکه‌های مجازی هم در همین صفحه تنظیم و در `Store.SiteSetting` ذخیره می‌شوند. قرارداد ستون در [راهنمای دیتابیس](../architecture/database.md) آمده است.

فرم تماس، پیام را ابتدا در `Store.ContactFormMessage` ثبت می‌کند و سپس ایمیل اعلان را تلاش می‌کند؛ اگر SMTP در دسترس نباشد، پیام در صندوق باقی می‌ماند. صفحهٔ مدیریت با مجوز `Store:Features:Index:contact-messages` در منوی مشتریان و تعامل‌ها نمایش داده می‌شود. مدیر می‌تواند پیام‌ها را جست‌وجو و مشاهده کند و بین وضعیت‌های جدید، خوانده‌شده و بسته‌شده جابه‌جا کند.

مدیر از صفحهٔ «پیام‌های حساب مشتریان» مشتری را با نام/ایمیل پیدا می‌کند، سپس عنوان، دسته و متن پیام را ثبت می‌کند. سابقه و وضعیت خواندن در همان صفحه دیده می‌شود؛ مشتری پیام را در `/Profile/Messages` می‌خواند و می‌تواند آن را خوانده‌شده علامت بزند. صفحه و endpointها مجوزهای جداگانه دارند که migration `078-contact-message-inbox.sql` اضافه می‌کند.

در تعریف تنوع‌های انبار، `IsDefault` مشخص می‌کند کدام variant برای افزودن سریع از کارت فروشگاه انتخاب شود. در هر محصول حداکثر یک مقدار پیش‌فرض مجاز است؛ اولین variant محصول در صورت نبود پیش‌فرض، خودکار پیش‌فرض می‌شود. ساختار دیتابیس و انتخاب کارت در [طرح‌های فروش](../store/sales-plans.md) ثبت شده است.

## مسیر بررسی

۱. route و مجوز را در Controller/API پیدا کنید؛ ۲. منطق و DbContext را در `Data/` دنبال کنید؛ ۳. View و اسکریپت مالک را بررسی کنید؛ ۴. راهنمای حوزه را در [فروشگاه](../store/README.md)، [رسانه](../media/README.md) یا [معماری داده](../architecture/database.md) بخوانید.

دادهٔ نمایشی سفارش یا محتوای seed را با دادهٔ عملیاتی اشتباه نگیرید. اجرای migration، seed یا تغییر داده باید بر اساس محیط هدف و وضعیت واقعی آن مستند شود. نام و وضعیت migration را از فایل‌های جاری `Data/Infrastructure/Persistence/Migrations/` و ابزار اجرای پروژه تأیید کنید؛ این راهنما وضعیت اجرای پایگاه مقصد را تضمین نمی‌کند.

پوستهٔ مدیریت در `Admin/Views/Shared/_Layout.cshtml` است. از کلاس‌ها و ابزارهای قالب موجود استفاده کنید و قبل از افزودن CSS/JS مشترک، مالک را در `tools/ui-owners.json` ببینید. راهنمای UI مشترک: [فرانت‌اند](../frontend/README.md).

بازبینی با کد: ۱۴ نوامبر ۲۰۲۶.
