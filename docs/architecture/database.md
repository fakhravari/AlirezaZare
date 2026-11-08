# لایهٔ دیتابیس و API انبار

این راهنما مرز پروژهٔ دسترسی داده، قرارداد repository و API خواندنی انبار را مشخص می‌کند. Domain، Application و Infrastructure یک پروژهٔ `Shop.Database` هستند و تفکیک آن‌ها با فولدر و namespace انجام می‌شود.

## پروژه‌ها و وابستگی‌ها

```text
Shop.Data/
├── Domain/
├── Application/
└── Infrastructure/  ← Shop
```

- `Shop.Data/Domain` موجودیت‌های دامنهٔ انبار را نگه می‌دارد.
- `Shop.Data/Application` قرارداد repository، مدل خروجی و سرویس کاربردی را تعریف می‌کند؛ به EF Core وابسته نیست.
- `Shop.Data/Infrastructure`، `ShopDbContext` و پیاده‌سازی EF Core/SQL Server را نگه می‌دارد و وابستگی‌ها را با `AddShopDatabase` ثبت می‌کند.
- `Shop/Controllers/Api/WarehouseApiController.cs` فقط transport HTTP را مدیریت می‌کند؛ query مستقیم از controller ممنوع است.

مرز وابستگی کد همچنان از Infrastructure به Application و Domain است؛ هر سه namespace در یک assembly و یک پروژه‌اند. Shop فقط به پروژهٔ Shop.Database ارجاع دارد. View و JavaScript به `DbContext` یا Entityهای دیتابیس دسترسی مستقیم ندارند.

## APIهای خواندنی

| متد و مسیر | کاربرد | پارامترها |
|---|---|---|
| `GET /api/warehouse/warehouses` | فهرست انبارها و شرکت مالک | ندارد |
| `GET /api/warehouse/stock` | موجودی به تفکیک انبار و طرح فروش، همراه وضعیت حداقل موجودی | `warehouseId`, `salesPlanKey`, `search`, `lowStockOnly`, `page`, `pageSize` |

`pageSize` بین ۱ تا ۲۰۰ محدود است. کلید طرح مانند `regular`، `digi-time` یا `digi-af` است. API فقط GET دارد و تغییر سند/موجودی را انجام نمی‌دهد؛ تغییرات موجودی باید از command کاربردی و رویهٔ تراکنشی انبار عبور کنند.

APIهای انبار اکنون برای مرحلهٔ اتصال پنل قابل فراخوانی‌اند. چون احراز هویت مدیریتی در برنامه هنوز پیکربندی نشده، پیش از استقرار عمومی باید آن‌ها به مجوز مدیر محدود شوند.

## اتصال و اجرای migration

Infrastructure مقدار `ConnectionStrings:DefaultConnection` را می‌خواند. اتصال در زمان ساخت DI اعتبارسنجی می‌شود و EF فقط هنگام درخواست داده اتصال واقعی را باز می‌کند. مدل EF در این مرحله read model است؛ ساخت/تغییر schema با SQL migrationهای نسخه‌دار انجام می‌شود، نه `EnsureCreated` یا migration خودکار زمان startup.

ترتیب لازم برای دیتابیس نمونه:

1. [001-initial-schema.sql](../../Shop/Features/Warehouse/Database/Migrations/001-initial-schema.sql) ساخت schema و رویهٔ تراکنشی انبار.
2. migrationهای فروشگاه [002](../../Shop/Features/Store/Database/Migrations/002-store-relations.sql)، [003](../../Shop/Features/Store/Database/Migrations/003-store-catalog-content.sql)، [004](../../Shop/Features/Store/Database/Migrations/004-store-fixture-data.sql)، [005](../../Shop/Features/Store/Database/Migrations/005-store-api-runtime-data.sql) و [006](../../Shop/Features/Store/Database/Migrations/006-store-sales-plan-configuration.sql) برای ساخت رابطه با محصول و طرح فروش و تنظیمات API.
3. [007-store-runtime-datasets.sql](../../Shop/Features/Store/Database/Migrations/007-store-runtime-datasets.sql) برای انتقال snapshot کاتالوگ، تحریریهٔ برند و FAQ به `Store.RuntimeDataset` و [008-account-magazine-geography.sql](../../Shop/Features/Store/Database/Migrations/008-account-magazine-geography.sql) برای حساب نمونه، محتوای مجله و جداول جغرافیا.
4. [002-sample-warehouse-data.sql](../../Shop/Features/Warehouse/Database/Migrations/002-sample-warehouse-data.sql) برای شرکت، انبار فروش و ذخیره، واحد، محصولات نگاشت‌شده، رسیدهای قطعی و موجودی نمونه.
5. [009-repair-persian-text.sql](../../Shop/Features/Store/Database/Migrations/009-repair-persian-text.sql) برای بازیابی رشته‌هایی که با code page اشتباه ذخیره شده بودند. این migration داده‌های Store، Warehouse و Account را اصلاح می‌کند و در هر دو schema ثبت می‌شود.
6. Migrationهای [010](../../Shop/Features/Store/Database/Migrations/010-account-commerce.sql)، [011](../../Shop/Features/Store/Database/Migrations/011-account-article-feedback.sql)، [012](../../Shop/Features/Store/Database/Migrations/012-account-profile-preferences.sql)، [013](../../Shop/Features/Store/Database/Migrations/013-account-price-alerts.sql) و [014](../../Shop/Features/Store/Database/Migrations/014-product-feedback.sql) برای داده‌های حساب، سفارش، تعامل مقاله، هشدار قیمت و دیدگاه/پرسش محصول.

`CatalogRepository`، `DatabaseMagazineContentSource` و `DatabaseAccountDataSource` از repository دیتابیس می‌خوانند. `Store.RuntimeDataset` نگهدارندهٔ payloadهای JSON عمومی است؛ دادهٔ جغرافیا و FAQ از جدول‌های رابطه‌ای خوانده می‌شود. فایل‌های JSON اولیه از Shop حذف شده‌اند و migrationهای seed مرجع بازسازی دیتابیس‌اند. وضعیت منابع کاربر در [منابع داده](data-sources.md) آمده است.

فایل‌های migration شامل رشته‌های فارسی UTF-8 هستند؛ اجرای دستی با `sqlcmd` باید code page ورودی و خروجی را UTF-8 تنظیم کند (برای نمونه `-f 65001`). از اجرای اسکریپت با code page پیش‌فرض Windows خودداری کنید؛ همان کاراکترها را می‌تواند به mojibake تبدیل کند.

Migration دادهٔ نمونه idempotent است و ماندهٔ `StockBalance` را مستقیم درج نمی‌کند؛ رسیدهای قطعی را ثبت و برای هر سند `Warehouse.RebuildDocumentStock` را اجرا می‌کند. داده‌ها fixture نمایشی‌اند و نباید به‌عنوان موجودی واقعی کسب‌وکار استفاده شوند.

## مسیر توسعه

برای افزودن قابلیت، مدل دامنه و قرارداد use case را در Domain/Application اضافه کنید، query/command و mapping دیتابیس را در Infrastructure پیاده کنید، و API را در feature مربوط Shop بسازید. عملیات نوشتن باید با transaction و قواعد ثبت حرکت موجودی سازگار باشد. قراردادهای کلی داده در [منابع داده](data-sources.md) و مرزهای موجودی در [آمادگی backend](backend-readiness.md) نگهداری می‌شوند.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۸ نوامبر ۲۰۲۶.

## حساب مشتری، سبد و سفارش (۸ نوامبر ۲۰۲۶)

Migrationهای `010-account-commerce.sql` و `011-account-article-feedback.sql` schema حساب را گسترش می‌دهند. `Account.Profile` ریشهٔ مالکیت است؛ `Cart`, `Address`, `Favorite`, `CompareItem`, `CheckoutPreference`, `ArticleInteraction`, `ArticleFeedback` و `CustomerOrder` با `IdProfile` به آن وصل‌اند. `CartItem` و `CustomerOrderItem` به محصول Store کلید خارجی دارند. سفارش و اقلام آن نام/قیمت/گزینه و snapshot نشانی را نگه می‌دارند تا سابقه با تغییر دادهٔ محصول عوض نشود. ایجاد سفارش با تراکنش SQL انجام می‌شود و درگاه واقعی هنوز بیرون از این پیاده‌سازی است. Migration هر دو به‌صورت transaction و با کلید یکتا در `Store.SchemaMigration` ثبت می‌شوند.

Migration `014-product-feedback.sql` جدول `[Store].[ProductFeedback]` را می‌سازد. هر رکورد به محصول وصل است و در صورت ورود کاربر، `IdProfile` اختیاری به `[Account].[Profile]` اشاره می‌کند؛ `FeedbackType` دیدگاه یا پرسش را مشخص می‌کند و `Status` فقط `pending`، `approved` یا `rejected` می‌پذیرد. پاسخ مدیر و زمان آن نیز در همان رکورد ثبت می‌شوند. API عمومی فقط موارد تأییدشده را فهرست می‌کند، به‌جز موارد pending متعلق به حساب واردشده. ثبت عمومی rate-limited است و خروجی اولیه برای moderation نگه داشته می‌شود.

خواندن، ثبت و انتقال بازخورد قدیمی در `StoreProductFeedbackRepository` با EF Core و LINQ انجام می‌شود؛ تبدیل JSON در SQL Server، `OPENJSON` و اجرای دستی command برای این قابلیت استفاده نمی‌شوند. مدل‌های `StoreProduct`، `AccountProfile` و `ProductFeedback` در `ShopDbContext` به schemaهای `Store` و `Account` نگاشت شده‌اند. فایل migration بالا فقط برای ایجاد نسخه‌بندی‌شدهٔ schema باقی می‌ماند.

Migration "012-account-profile-preferences.sql" adds gender and newsletter settings to the user profile. Current runtime uses migrations 010 through 012 for customer-owned data.


Migration "013-account-price-alerts.sql" پیگیری قیمت/موجودی را با مالکیت حساب و کلید خارجی محصول اضافه می‌کند. با این migrationها سبد، سفارش، نشانی، ترجیح checkout، فهرست‌ها، پروفایل و تعامل مقاله/پیگیری قیمت به کاربر متصل‌اند.
