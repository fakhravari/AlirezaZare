# مدل و زیرساخت پایگاه داده

پروژهٔ داده `Data/Data.csproj` است و هر دو وب‌برنامه به آن ارجاع دارند. EF Core context در `Data/Infrastructure/Persistence/ShopDbContext.cs` قرار دارد؛ entityها زیر `Data/Domain/` و قراردادها زیر `Data/Application/` هستند.

## هنگام تغییر schema

1. entity، نگاشت Fluent و repository مصرف‌کننده را با هم پیدا کنید.
2. migrationهای موجود را با `rg --files Data Shop | rg 'Migrations|\.sql$'` بیابید و بررسی کنید؛ در این مخزن ممکن است migrationهای SQL در پوشهٔ featureهای `Shop/Features/**/Database/Migrations/` نیز باشند.
3. migration افزایشی و امن برای دادهٔ موجود بسازید؛ ترتیب اجرا و پیش‌شرط‌ها را مستند کنید.
4. اجرای محلی/مقصد را جداگانه بررسی کنید و marker ثبت‌شده در همان DB را از وجود فایل migration تفکیک کنید.
5. قرارداد مصرف‌کننده را در راهنمای حوزه به‌روز کنید.

هیچ سندی فهرست ثابتِ «همهٔ migrationهای اجراشده» نیست؛ وضعیت اجرای هر پایگاه را از جدول history/marker همان پایگاه و migration runner واقعی بخوانید. پیش از حذف یا بازسازی داده، backup، شمارش وابستگی و مجوز صریح لازم است.

دامنه‌های اصلی در entityهای `Store`، `Account`، `Warehouse` و `Admin` تفکیک شده‌اند. قیمت نسخه‌ای در `Store.PriceVersion` است؛ تاریخچهٔ قبل از نصب نسخه‌بندی را نمی‌توان از snapshot اولیه بازسازی کرد. مرجع قراردادها: [قیمت محصول](../store/product.md) و [دیتابیس و داده‌ها](data-sources.md).

[معماری](README.md) · [توسعه](../development/README.md) · [فهرست docs](../README.md)

بازبینی ساختار: ۱۲ نوامبر ۲۰۲۶.
