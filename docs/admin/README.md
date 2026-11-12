# پنل مدیریت فروشگاه و انبار

پنل وب مستقل `Admin/` به پروژهٔ `Data/` متصل است. صفحه‌ها در `Admin/Areas/Store/Views/` و `Admin/Areas/Warehouse/Views/`، endpointها در `Admin/Controllers/Api/` و رفتار مرورگر در `Admin/wwwroot/js/` قرار دارند. برای یافتن owner رابط، `tools/ui-owners.json` را جست‌وجو کنید.

## حوزه‌ها

- کاتالوگ و قیمت: APIهای `StoreCatalogAdminApiController` و `StoreVariantPricesAdminApiController`.
- سفارش و ارسال: `StoreOrdersAdminApiController` و `StorePaidOrdersDispatchApiController`.
- انبار: `WarehouseAdminApiController`، `WarehouseDocumentsApiController` و `WarehouseMovementsApiController`.
- محتوای فروشگاه: endpointهای FAQ، دسته، مقاله، صفحات خدمات و رسانه در همین پوشهٔ API.
- حساب مدیر و مجوزها: `Admin/Controllers/AuthController.cs` و مدل‌های `Data/Domain/Admin/Entities/`.

## مسیر بررسی

۱. route و مجوز را در Controller/API پیدا کنید؛ ۲. منطق و DbContext را در `Data/` دنبال کنید؛ ۳. View و اسکریپت مالک را بررسی کنید؛ ۴. راهنمای حوزه را در [فروشگاه](../store/README.md)، [رسانه](../media/README.md) یا [معماری داده](../architecture/database.md) بخوانید.

دادهٔ نمایشی سفارش یا محتوای seed را با دادهٔ عملیاتی اشتباه نگیرید. اجرای migration، seed یا تغییر داده باید بر اساس محیط هدف و وضعیت واقعی آن مستند شود. نام و وضعیت migration را از فایل‌های جاری `Data/Infrastructure/Persistence/Migrations/` و ابزار اجرای پروژه تأیید کنید؛ این راهنما وضعیت اجرای پایگاه مقصد را تضمین نمی‌کند.

پوستهٔ مدیریت در `Admin/Views/Shared/_Layout.cshtml` است. از کلاس‌ها و ابزارهای قالب موجود استفاده کنید و قبل از افزودن CSS/JS مشترک، مالک را در `tools/ui-owners.json` ببینید. راهنمای UI مشترک: [فرانت‌اند](../frontend/README.md).
