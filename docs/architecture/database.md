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

API عمومی انبار برای خواندن کاتالوگ انبار و موجودی است. APIهای نوشتن سند و تعاریف در `Shop.Admin` محافظت‌شده با cookie احراز هویت مستقل، مجوز درختی، ضد CSRF و دسترسی انبار کاربر هستند. هویت مدیر در `[Admin]` است و از حساب مشتری `[Account].[Profile]` جدا نگهداری می‌شود. جزئیات عملیات و محدودیت‌های باقی‌مانده در [راهنمای پنل](../admin/README.md) آمده است.

## اتصال و اجرای migration

Infrastructure مقدار `ConnectionStrings:DefaultConnection` را می‌خواند. اتصال در زمان ساخت DI اعتبارسنجی می‌شود و EF فقط هنگام درخواست داده اتصال واقعی را باز می‌کند. مدل EF در این مرحله read model است؛ ساخت/تغییر schema با SQL migrationهای نسخه‌دار انجام می‌شود، نه `EnsureCreated` یا migration خودکار زمان startup.

ترتیب لازم برای دیتابیس نمونه:

1. [001-initial-schema.sql](../../Shop/Features/Warehouse/Database/Migrations/001-initial-schema.sql) ساخت schema و رویهٔ تراکنشی انبار.
2. migrationهای فروشگاه [002](../../Shop/Features/Store/Database/Migrations/002-store-relations.sql)، [003](../../Shop/Features/Store/Database/Migrations/003-store-catalog-content.sql)، [004](../../Shop/Features/Store/Database/Migrations/004-store-fixture-data.sql)، [005](../../Shop/Features/Store/Database/Migrations/005-store-api-runtime-data.sql) و [006](../../Shop/Features/Store/Database/Migrations/006-store-sales-plan-configuration.sql) برای ساخت رابطه با محصول و طرح فروش و تنظیمات API.
3. [007-store-runtime-datasets.sql](../../Shop/Features/Store/Database/Migrations/007-store-runtime-datasets.sql) برای انتقال snapshot کاتالوگ، تحریریهٔ برند و FAQ به `Store.RuntimeDataset` و [008-account-magazine-geography.sql](../../Shop/Features/Store/Database/Migrations/008-account-magazine-geography.sql) برای حساب نمونه، محتوای مجله و جداول جغرافیا.
4. [002-sample-warehouse-data.sql](../../Shop/Features/Warehouse/Database/Migrations/002-sample-warehouse-data.sql) برای شرکت، انبار فروش و ذخیره، واحد، محصولات نگاشت‌شده، رسیدهای قطعی و موجودی نمونه.
5. [009-repair-persian-text.sql](../../Shop/Features/Store/Database/Migrations/009-repair-persian-text.sql) برای بازیابی رشته‌هایی که با code page اشتباه ذخیره شده بودند. این migration داده‌های Store، Warehouse و Account را اصلاح می‌کند و در هر دو schema ثبت می‌شود.
6. Migrationهای [010](../../Shop/Features/Store/Database/Migrations/010-account-commerce.sql)، [011](../../Shop/Features/Store/Database/Migrations/011-account-article-feedback.sql)، [012](../../Shop/Features/Store/Database/Migrations/012-account-profile-preferences.sql)، [013](../../Shop/Features/Store/Database/Migrations/013-account-price-alerts.sql)، [014](../../Shop/Features/Store/Database/Migrations/014-product-feedback.sql) و [015](../../Shop/Features/Store/Database/Migrations/015-account-recent-searches.sql) برای داده‌های حساب، سفارش، تعامل مقاله، هشدار قیمت، دیدگاه/پرسش و جست‌وجوهای اخیر.
7. بعد از migrationهای قبلی، [003-paid-orders-and-regular-minimum.sql](../../Shop/Features/Warehouse/Database/Migrations/003-paid-orders-and-regular-minimum.sql) و [004-document-external-reference.sql](../../Shop/Features/Warehouse/Database/Migrations/004-document-external-reference.sql) را به‌ترتیب اجرا کنید؛ اولی سفارش چندطرحی و حداقل موجودی طرح عادی را مدل می‌کند و دومی شمارهٔ سفارش/مرجع بیرونی سند دستی را جدا از کلید سفارش خودکار نگه می‌دارد.
8. سپس [016-account-warehouse-variants.sql](../../Shop/Features/Store/Database/Migrations/016-account-warehouse-variants.sql) را اجرا کنید تا شناسهٔ تنوع مشترک به سبد و ردیف سفارش متصل شود.
9. [017-warehouse-variant-sales-plan-prices.sql](../../Shop/Features/Store/Database/Migrations/017-warehouse-variant-sales-plan-prices.sql) را پس از 016 اجرا کنید تا override قیمت Store برای هر تنوع انبار و طرح فروش آماده شود.
10. [018-magazine-schema.sql](../../Shop/Features/Store/Database/Migrations/018-magazine-schema.sql) را اجرا کنید تا جدول‌های `MagazineArticle`، `MagazineArticleSection` و `MagazineArticleSource` از `Store` به schema مستقل `Mag` منتقل شوند. این migration داده و کلیدهای خارجی را حفظ می‌کند؛ در این مرحلهٔ تاریخی payload هنوز در `Store.RuntimeDataset` است و در مراحل 022 و 023 جایگزین و حذف می‌شود.
11. [019-normalize-account-snapshot.sql](../../Shop/Features/Store/Database/Migrations/019-normalize-account-snapshot.sql) را اجرا کنید تا دادهٔ snapshot حساب به جدول‌های رابطه‌ای حساب منتقل و `Account.ProfileSnapshot` حذف شود.
12. [020-magazine-editorial-content.sql](../../Shop/Features/Store/Database/Migrations/020-magazine-editorial-content.sql) را اجرا کنید تا ۱۵ راهنمای تازه به seed مجله اضافه شود؛ دادهٔ نمونهٔ این نسخه ۵۱ مقاله دارد.
13. [021-store-category-icon-class.sql](../../Shop/Features/Store/Database/Migrations/021-store-category-icon-class.sql) ستون `Store.Category.IconClass` را اضافه و برای دسته‌های موجود مقدار Bootstrap Icons ثبت می‌کند.
14. [022-normalize-magazine-runtime.sql](../../Shop/Features/Store/Database/Migrations/022-normalize-magazine-runtime.sql) payload مجله را به جدول‌های رابطه‌ای schema `Mag` منتقل می‌کند.
15. [023-drop-runtime-dataset.sql](../../Shop/Features/Store/Database/Migrations/023-drop-runtime-dataset.sql) پس از انتقال کاتالوگ، برند و مجله، جدول snapshot عمومی `Store.RuntimeDataset` را حذف می‌کند.
16. [024-drop-unused-store-tables.sql](../../Shop/Features/Store/Database/Migrations/024-drop-unused-store-tables.sql) جدول‌های خالی و بدون مصرف runtime (`ContentPageSection`، `ContactMessage` و `ContactChannel`) را حذف می‌کند؛ اگر هرکدام داده داشته باشند migration عمداً متوقف می‌شود.
17. [025-normalize-relational-values.sql](../../Shop/Features/Store/Database/Migrations/025-normalize-relational-values.sql) مقادیر تکراری مجله، رابطهٔ مقاله در حساب، طرح فروش سبد/سفارش و والد دسته‌بندی را به جدول مرجع و FK تبدیل می‌کند و ستون‌های متنی تکراری را حذف می‌کند.
18. [026-store-product-badge.sql](../../Shop/Features/Store/Database/Migrations/026-store-product-badge.sql) ستون اختیاری `Store.Product.Badge` را برای برچسب نمایشی محصول اضافه می‌کند؛ API کاتالوگ این مقدار را از جدول محصول می‌خواند.
19. در پایان [001-system-users-and-permissions.sql](../../Shop.Admin/Database/Migrations/001-system-users-and-permissions.sql) را اجرا کنید. این migration هویت مستقل کاربران پنل، نقش، مجوزهای درختی و دسترسی انتخابی کاربران به انبار را در schema جداگانهٔ `Admin` می‌سازد.
20. سپس [002-store-feedback-permissions.sql](../../Shop.Admin/Database/Migrations/002-store-feedback-permissions.sql) را اجرا کنید تا مشاهده و مدیریت دیدگاه‌ها/پرسش‌های فروشگاه به‌صورت مجوزهای جداگانه به درخت دسترسی افزوده شوند.
21. در انتها [005-document-audit.sql](../../Shop/Features/Warehouse/Database/Migrations/005-document-audit.sql) را اجرا کنید تا snapshot تغییرات سندهای انبار مستقل از عمر سند اصلی نگهداری شود.

Migrationهای 003 و 016 اصلاحات افزایشی‌اند و دادهٔ موجود را حفظ می‌کنند: طرح سربرگ صندوق پرداخت‌شده به ردیف‌های موجود منتقل می‌شود، ارجاع سند یک‌به‌یک به رابطهٔ یک‌به‌چند تبدیل می‌شود، و حداقل‌های قدیمیِ طرح‌های غیرعادی به طرح عادی همان انبار/قلم منتقل می‌شوند (در صورت چند مقدار، بیشترین مقدار نگه داشته می‌شود). شناسهٔ تنوع در CartItem و CustomerOrderItem nullable است تا سفارش/سبدهای قدیمی حفظ شوند؛ API ذخیرهٔ سبد و checkout اکنون این شناسه را می‌خواند/می‌نویسد و تعلق آن به محصول را کنترل می‌کند. Migrationهای 016، 017 و 020 در ۸ نوامبر ۲۰۲۶ روی پایگاه متصل به تنظیمات `Shop/appsettings.json` اجرا و ثبت شدند؛ اجرای این سه مورد روی هر پایگاه یا محیط میزبانی دیگر جداگانه تأیید نشده است.

`CatalogRepository`، `DatabaseMagazineContentSource` و `DatabaseAccountDataSource` از repository دیتابیس می‌خوانند. کاتالوگ، برند و مجله از جدول‌های رابطه‌ای خوانده می‌شوند و migration 023 جدول snapshot عمومی `Store.RuntimeDataset` را حذف می‌کند؛ دادهٔ جغرافیا و FAQ نیز رابطه‌ای است. فایل‌های JSON اولیه از Shop حذف شده‌اند و migrationهای seed مرجع بازسازی دیتابیس‌اند. وضعیت منابع کاربر در [منابع داده](data-sources.md) آمده است.

### جزئیات نرمال‌سازی و محدودیت اجرا

Migration 025 مقادیر تکراری دسته، شاخه، موضوع و حوزهٔ مقاله را به ترتیب در `Mag.ArticleCategory`، `Mag.ArticleBranch`، `Mag.ArticleTopic` و `Mag.ArticleArea` با کلید یکتا نگه می‌دارد؛ `Mag.MagazineArticle` به هر چهار جدول FK اجباری دارد. مقالهٔ مرتبط با `IdRelatedArticle`، تعامل و بازخورد حساب با `IdMagazineArticle`، طرح هر قلم سبد و سفارش با `IdSalesPlan` و والد دسته با `IdParentCategory` به رکورد اصلی وصل می‌شود. کلیدهای متنی قدیمی این روابط پس از انتقال حذف شده‌اند؛ query برای قرارداد خروجی، کلیدهای لازم را از رکوردهای مرتبط می‌سازد. در شکست انتقال مرجع، migration تراکنش را متوقف می‌کند. قیمت، نام و نشانی ثبت‌شده در سفارش برای حفظ تاریخچه همچنان snapshot هستند؛ همهٔ تکرارهای دیگر پروژه نیز به معنای تکمیل این migration حذف نشده‌اند.

در بررسی ۹ نوامبر ۲۰۲۶، migration 025 روی پایگاه متصل اجرا و تکرار آن با ثبت migration بررسی شد؛ این بررسی به معنی تأیید همهٔ محیط‌ها یا ساخت کامل پایگاه خالی نیست. اسکریپت‌های 004 و 021 نیز برای اجرای مجدد پس از حذف ستون قدیمی والد دسته اصلاح و روی پایگاه موجود بررسی شدند. داده‌های seed نمونه‌اند. نتیجهٔ آزمون‌ها و موارد اجرا‌نشده در [کنترل کیفیت](../development/quality.md) ثبت شده است.

جدول `[Admin].[SchemaMigration]` بنا به درخواست از پایگاه بررسی‌شده حذف شد؛ جدول‌های تاریخچهٔ Store و Warehouse حذف نشده‌اند. اسکریپت‌های Admin هنوز از آن استفاده می‌کنند: 001 در صورت نبود جدول آن را می‌سازد و سپس جدول‌های هویت را ایجاد می‌کند و 002 به آن وابسته است. بنابراین اجرای مجدد آن‌ها روی پایگاه دارای جدول‌های Admin بدون بررسی و اصلاح مسیر migration می‌تواند خطا دهد؛ حذف جدول تاریخچه به معنای مستقل‌شدن این اسکریپت‌ها از آن نیست.

در `[Store].[Product]`، `Id` کلید اصلی داخلی و مقصد FKهای رابطه‌ای است؛ برای نمونه `[Warehouse].[Product].[IdStoreProduct]` به همین شناسه وصل می‌شود. `StoreProductKey` کلید متنی سازگاری با شناسهٔ کاتالوگ قدیمی و نگاشت‌های واردشده است و هنوز در seedها و بعضی queryهای انتقال/تطبیق خوانده می‌شود؛ این ستون جای `Id` را در رابطهٔ FK نمی‌گیرد. `Code` کد تجاری کالا است و قرارداد جداگانه‌ای دارد. حذف `StoreProductKey` مستلزم مهاجرت جداگانهٔ مصرف‌کننده‌های قدیمی و استفاده از `Id` در آن مسیرهاست؛ در migration 026 حذف نشده است.

Migration [026-store-product-badge.sql](../../Shop/Features/Store/Database/Migrations/026-store-product-badge.sql) به‌صورت افزایشی ستون nullable `Badge` را به `[Store].[Product]` اضافه می‌کند. query کاتالوگ از `Product.Badge` مقدار `badge` خروجی را تولید می‌کند؛ نبود برچسب با `NULL` نمایش داده می‌شود. این migration در ۹ نوامبر ۲۰۲۶ روی پایگاه متصل به تنظیمات `Shop/appsettings.json` اجرا و ثبت شد.

فایل‌های migration شامل رشته‌های فارسی UTF-8 هستند؛ اجرای دستی با `sqlcmd` باید code page ورودی و خروجی را UTF-8 تنظیم کند (برای نمونه `-f 65001`). از اجرای اسکریپت با code page پیش‌فرض Windows خودداری کنید؛ همان کاراکترها را می‌تواند به mojibake تبدیل کند.

Migration دادهٔ نمونه idempotent است و ماندهٔ `StockBalance` را مستقیم درج نمی‌کند؛ رسیدهای قطعی را ثبت و برای هر سند `Warehouse.RebuildDocumentStock` را اجرا می‌کند. داده‌ها fixture نمایشی‌اند و نباید به‌عنوان موجودی واقعی کسب‌وکار استفاده شوند.

بازبینی ساختار ۸ نوامبر ۲۰۲۶ نشان داد سفارش فروشگاه `SalesPlanKey` را در سطح ردیف نگه می‌دارد، درحالی‌که صندوق انبار طرح را در سربرگ ثبت می‌کرد و سند نیز فقط یک طرح دارد. Migration 003 طرح را به هر ردیف صندوق منتقل می‌کند، برای هر سفارش/طرح سند یکتا می‌سازد و چند سند حاصل را به یک صندوق وصل می‌کند. بنابراین پردازش اتمی یک سفارش می‌تواند برای هر طرح سند جدا بسازد. همچنین حداقل موجودی فقط برای طرح عادی مجاز است و مقدار صفر هشدار کمبود ایجاد نمی‌کند.

تنوع انبار اکنون شناسهٔ سراسری و رابطه با محصول فروشگاه دارد. Migration 016 همین شناسه را در سبد و ردیف سفارش با FK ترکیبی (محصول + تنوع) تثبیت می‌کند؛ گزینه‌های قدیمی در `OptionsJson` حفظ می‌شوند. API انتخاب تنوع و ثبت سفارش این شناسه را می‌خواند و ذخیره می‌کند. هویت مدیر در schema جداگانهٔ `Admin` قرار دارد و هیچ کلید خارجی یا اشتراکی با `[Account].[Profile]` ندارد. نقش، درخت مجوز و دسترسی به انبار به کاربر سیستمی وصل می‌شوند. ورود اولیه با تنظیم امن `AdminBootstrap` انجام می‌شود و migration رمز یا حساب پیش‌فرض تولید نمی‌کند. migration 002 سطح مجوز جداگانه برای خواندن و moderation بازخوردها اضافه می‌کند. Migration 005 snapshot قبل/بعد ایجاد، ویرایش و حذف را با شناسهٔ کاربر، زمان و دلیل در جدول ممیزی مستقل از سند نگه می‌دارد؛ فهرست آن در صفحهٔ گردش قابل مشاهده است.

Migration 017 جدول `[Store].[ProductVariantSalesPlanOffer]` را برای قیمت و تخفیف اختیاری هر ترکیب انبار در هر طرح می‌سازد. اگر override ثبت نشده باشد، قیمت offer محصول/طرح fallback می‌شود. این جدول در حوزه Store است و کلیدش به تنوع انبار اشاره می‌کند؛ ویژگی‌های inventory و technical specs با هم ادغام نمی‌شوند.

## مسیر توسعه

برای افزودن قابلیت، مدل دامنه و قرارداد use case را در Domain/Application اضافه کنید، query/command و mapping دیتابیس را در Infrastructure پیاده کنید، و API را در feature مربوط Shop بسازید. عملیات نوشتن باید با transaction و قواعد ثبت حرکت موجودی سازگار باشد. قراردادهای کلی داده در [منابع داده](data-sources.md) و مرزهای موجودی در [آمادگی backend](backend-readiness.md) نگهداری می‌شوند.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۸ نوامبر ۲۰۲۶.

## حساب مشتری، سبد و سفارش (۸ نوامبر ۲۰۲۶)

Migrationهای `010-account-commerce.sql` و `011-account-article-feedback.sql` schema حساب را گسترش می‌دهند. `Account.Profile` ریشهٔ مالکیت است؛ `Cart`, `Address`, `Favorite`, `CompareItem`, `CheckoutPreference`, `ArticleInteraction`, `ArticleFeedback` و `CustomerOrder` با `IdProfile` به آن وصل‌اند. تعامل و بازخورد مقاله با `IdMagazineArticle` و اقلام سبد/سفارش با `IdSalesPlan` به موجودیت‌های مرجع متصل‌اند. سفارش و اقلام آن نام/قیمت/گزینه و snapshot نشانی را نگه می‌دارند تا سابقه با تغییر دادهٔ محصول عوض نشود. ایجاد سفارش با تراکنش SQL انجام می‌شود و درگاه واقعی هنوز بیرون از این پیاده‌سازی است. Migration هر دو به‌صورت transaction و با کلید یکتا در `Store.SchemaMigration` ثبت می‌شوند.

Migration `014-product-feedback.sql` جدول `[Store].[ProductFeedback]` را می‌سازد. هر رکورد به محصول وصل است و در صورت ورود کاربر، `IdProfile` اختیاری به `[Account].[Profile]` اشاره می‌کند؛ `FeedbackType` دیدگاه یا پرسش را مشخص می‌کند و `Status` فقط `pending`، `approved` یا `rejected` می‌پذیرد. پاسخ مدیر و زمان آن نیز در همان رکورد ثبت می‌شوند. API عمومی فقط موارد تأییدشده را فهرست می‌کند، به‌جز موارد pending متعلق به حساب واردشده. ثبت عمومی rate-limited است و خروجی اولیه برای moderation نگه داشته می‌شود.

خواندن، ثبت و انتقال بازخورد قدیمی در `StoreProductFeedbackRepository` با EF Core و LINQ انجام می‌شود؛ تبدیل JSON در SQL Server، `OPENJSON` و اجرای دستی command برای این قابلیت استفاده نمی‌شوند. مدل‌های `StoreProduct`، `AccountProfile` و `ProductFeedback` در `ShopDbContext` به schemaهای `Store` و `Account` نگاشت شده‌اند. فایل migration بالا فقط برای ایجاد نسخه‌بندی‌شدهٔ schema باقی می‌ماند.

Migration "012-account-profile-preferences.sql" adds gender and newsletter settings to the user profile. Current runtime uses migrations 010 through 012 for customer-owned data.


Migration "013-account-price-alerts.sql" پیگیری قیمت/موجودی را با مالکیت حساب و کلید خارجی محصول اضافه می‌کند. با این migrationها سبد، سفارش، نشانی، ترجیح checkout، فهرست‌ها، پروفایل و تعامل مقاله/پیگیری قیمت به کاربر متصل‌اند.
