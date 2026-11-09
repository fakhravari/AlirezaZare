# لایهٔ دیتابیس و API انبار

[Migration 036](../../Shop/Features/Store/Database/Migrations/036-category-product-content.sql) پس از یکپارچه‌سازی کالا در migration 035، محتوای مشترک را به دستهٔ برگ منتقل و جدول `Store.ProductDetail` و کلید آن را حذف می‌کند. تعارض نگاشت یا گروه بدون دسته، انتقال را متوقف می‌کند. در ۹ نوامبر ۲۰۲۶، migrationهای 035 و 036 و مجوزهای محتوای دسته در یک تراکنش روی پایگاه متصل و جداگانه روی پایگاه آزمایشی محلی اجرا شدند؛ ۳۰ کالا، ۱۸۰ مانده و ۳۰۵ بازخورد حفظ شدند.

[Migration 034](../../Shop/Features/Store/Database/Migrations/034-product-feedback-from-details.sql) دیدگاه‌ها و پرسش‌های نمونه را برای هر محصول با وضعیت در انتظار تأیید به جدول بازخورد منتقل و دو ستون JSON را حذف می‌کند. این migration در ۹ نوامبر ۲۰۲۶ روی پایگاه متصل اجرا شد؛ ۱۶۰ دیدگاه و ۱۴۵ پرسش منتقل شدند و سه مورد از هر نوع تأیید شد. همچنین پس از بازسازی جداگانهٔ پایگاه خالی محلی تا 033، migration 034 و اجرای مجدد آن موفق بود و ۱۶۰ دیدگاه و ۱۴۵ پرسش نمونه با وضعیت در انتظار بررسی منتقل شد. اجرای migration در پایگاه‌های دیگر جداگانه لازم است.

این راهنما مرز پروژهٔ دسترسی داده، قرارداد repository و API خواندنی انبار را مشخص می‌کند. Domain، Application و Infrastructure یک پروژهٔ `Shop.Database` هستند و تفکیک آن‌ها با فولدر و namespace انجام می‌شود.

## بازبینی مالکیت schemaها — ۹ نوامبر ۲۰۲۶

این بازبینی بر اساس جدول‌های موجود پایگاه متصل، migrationها و مصرف‌کننده‌های کد است؛ تعداد ردیف‌ها و داده‌های نمونه به محیط وابسته‌اند. خالی‌بودن جدول به‌تنهایی نشان بی‌استفاده‌بودن آن نیست.

| schema | نقش جدول‌ها | ارزیابی |
|---|---|---|
| `Admin` | کاربر سیستم، نقش، مجوز و تخصیص دسترسی انبار | از حساب مشتری مستقل و محل آن مناسب است. جدول تاریخچهٔ migration آن در پایگاه بررسی‌شده حذف شده، اما اسکریپت‌های Admin همچنان به آن وابسته‌اند. |
| `Account` | پروفایل، نشانی، سبد، علاقه‌مندی، سفارش، تعامل و بازخورد مقاله، کوپن | مالکیت داده‌های کاربر مناسب است؛ تعریف عمومی کوپن و مصرف آن نامزد انتقال به دامنهٔ فروش هستند. سفارش نیز با رشد جریان فروش قابل انتقال به schema فروش خواهد بود. |
| `Geo` | کشور، استان و شهر انتخاب نشانی و کشور برند | مرجع مشترک کشور؛ migration 028 کشورهای فروشگاه و پرچم را به این schema منتقل می‌کند. |
| `Mag` | مقاله، بخش، منبع، برچسب، مقالهٔ مرتبط، معیار و دسته‌بندی | محل مناسب مجله؛ نام‌های `MagazineArticle*` در کنار `Article*` یکدست نیستند و کوتاه‌سازی نام، اختیاری و نیازمند migration است. |
| `Store` | دسته، برند، مشخصات، رسانه، پیشنهاد فروش، محتوای فروشگاه و هویت سایت | جدول اصلی کالا پس از migration 035 به `Warehouse.Product` منتقل شده؛ جداول وابستهٔ کاتالوگ و فروش همچنان در Store هستند. |
| `Warehouse` | شرکت و انبار، واحد، موجودیت اصلی کالا، مالک، طرح فروش، موجودی، اسناد، گردش و صف سفارش پرداخت‌شده | کالا یک ریشهٔ مشترک در این schema دارد؛ تعریف انبار و فروش روی همان شناسه انجام می‌شوند. |

موارد اولویت‌دار باقی‌مانده برای تغییر schema:

1. `Account.Address` با migration 030 به کلیدهای خارجی `Geo.Country`، `Geo.Province` و `Geo.City` تبدیل شد. اسکریپت نام‌های تکراری را حذف می‌کند، دادهٔ قبلی را با شناسه/کد یا نام نگاشت می‌کند و اگر رکوردی بی‌نگاشت بماند متوقف می‌شود. migration در ۹ نوامبر ۲۰۲۶ روی پایگاه متصل اجرا و ثبت شد؛ در آن پایگاه تعداد آدرس‌ها صفر بود و داده‌ای برای انتقال وجود نداشت. API نام‌ها را از رابطه می‌خواند و در snapshot سفارش کپی می‌کند؛ برای هر پایگاه دیگر نیز باید migration اجرا شود.
2. افزونگی `Store.Country` و `Geo.Country` با migration 028 رفع شد: پرچم و کشورهای فروشگاه به `Geo.Country` منتقل و FK برند با کد کشور نگاشت شد. شناسهٔ کشورها و روابط استان/شهر موجود Geo حفظ می‌شوند. API نشانی فقط کشورهایی را برمی‌گرداند که استان دارند؛ فهرست کاتالوگ تمام کشورها را می‌خواند.
3. طرح فروش در `Warehouse.SalesPlanReference` تجمیع شد و `Store.SalesPlan` حذف شد. migrationهای 031 و 032 شناسهٔ کالا را مشترک کردند؛ migration 035 اطلاعات تعریف کالا را به `Warehouse.Product` منتقل و `Store.Product` را حذف کرد؛ migration 036 هم محتوای مشترک محصول را به دسته منتقل کرد. هر دو migration در ۹ نوامبر ۲۰۲۶ روی پایگاه متصل ثبت شده‌اند. وضعیت فعلی: ۳۰ کالا، ۳۰ واحد، ۳۰ قلم موجودی، ۱۷ FK ورودی به `Warehouse.Product` و بدون جدول `Store.Product` یا `Store.ProductDetail`. قیمت‌های هر طرح همچنان در جدول‌های offer فروشگاه می‌مانند.
4. ستون‌های تکراری `Category`، `Title` و `ImagePath` از `Mag.MagazineArticleRelated` با migration 033 حذف شده‌اند. عنوان، دسته و تصویر اکنون از مقالهٔ مرتبط خوانده می‌شوند و override تحریری برای این موارد وجود ندارد.
5. تعریف `Account.Coupon` عمومی است و `CouponUsage` به سفارش متصل است؛ انتقال آن‌ها به `Store` از نظر مالکیت قابل بررسی است. `Account.CustomerOrder` نیز مفهوم فروش دارد، ولی انتقال آن و ردیف‌هایش تصمیم مرزبندی دامنه است و باید همراه تمام FKها، queryها و APIها انجام شود.

برای هر انتقال یا تغییر نام، migration افزایشی با حفظ داده، به‌روزرسانی EF/SQL و قراردادها، آزمون migration روی پایگاه موجود و ساخت پایگاه خالی لازم است. صرف تغییر نام ظاهری بدون رفع تکرار یا شفاف‌کردن مالکیت اولویت پایین‌تری دارد.

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

### دلیل وجود جدول‌های تاریخچهٔ migration

`[Store].[SchemaMigration]` و `[Warehouse].[SchemaMigration]` دادهٔ محصول یا موجودی نیستند؛ هر کدام کلید و زمان اجرای تغییرهای ساختار و دادهٔ حوزهٔ خود را ثبت می‌کنند. اسکریپت‌ها پیش از اعمال تغییر، کلید خود را بررسی می‌کنند و پس از موفقیت آن را ثبت می‌کنند؛ به این ترتیب تشخیص می‌دهند کدام تغییر پیش‌تر روی همان پایگاه انجام شده است. کلید هر migration در جدول مربوط یکتاست. نمونه‌های ساخت جدول و استفاده از آن در [migration اولیهٔ فروشگاه](../../Shop/Features/Store/Database/Migrations/002-store-relations.sql) و [migration اولیهٔ انبار](../../Shop/Features/Warehouse/Database/Migrations/001-initial-schema.sql) دیده می‌شود.

این دو جدول در پیاده‌سازی فعلی کاربرد دارند و حذف نشده‌اند: برای نمونه [migration مالکیت کالا](../../Shop/Features/Store/Database/Migrations/029-unify-stock-ownership.sql) به تاریخچهٔ Store و [migration سفارش و حداقل موجودی](../../Shop/Features/Warehouse/Database/Migrations/003-paid-orders-and-regular-minimum.sql) به تاریخچهٔ Warehouse وابسته‌اند. [اصلاح متن فارسی](../../Shop/Features/Store/Database/Migrations/009-repair-persian-text.sql) هر دو تاریخچه را بررسی می‌کند و در صورت ثبت ناقص متوقف می‌شود.

حذف مستقیم جدول یا ردیف‌های آن تشخیص اجرای قبلی را از بین می‌برد؛ اجرای اسکریپت‌های وابسته ممکن است خطا دهد یا تغییر قبلی دوباره اعمال شود. در صورت تصمیم به حذف یا ادغام این تاریخچه‌ها، ابتدا باید برای همهٔ اسکریپت‌های وابسته سازوکار جایگزین، انتقال تاریخچهٔ موجود و آزمون اجرای اولیه و مجدد روی پایگاه‌های موجود و خالی فراهم شود. وجود تاریخچه به‌تنهایی موفقیت بازسازی کامل یا سازگاری همهٔ migrationهای قدیمی را تضمین نمی‌کند.

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
18. [026-store-product-badge.sql](../../Shop/Features/Store/Database/Migrations/026-store-product-badge.sql) در نسخهٔ اولیه ستون اختیاری `Store.Product.Badge` را برای برچسب نمایشی محصول اضافه می‌کند؛ API کاتالوگ این مقدار را از جدول محصول می‌خواند.
19. [027-store-site-identity.sql](../../Shop/Features/Store/Database/Migrations/027-store-site-identity.sql) جدول singleton `Store.SiteIdentity` را می‌سازد و مقدارهای فعلی نام، تلفن، WhatsApp و ایمیل فروشگاه را فقط در صورت نبود ردیف `Id = 1` seed می‌کند.
   سپس [028-consolidate-countries.sql](../../Shop/Features/Store/Database/Migrations/028-consolidate-countries.sql) کشورها و پرچم‌ها را در `Geo.Country` ادغام می‌کند، FK برند را منتقل و `Store.Country` را حذف می‌کند. در تعارض نام یا پرچم، انتقال متوقف می‌شود تا داده‌ها از دست نروند. این migration در ۹ نوامبر ۲۰۲۶ روی پایگاه متصل اجرا شد؛ ۲۵۵ کشور و رابطهٔ ۲۱ برند حفظ شدند.
   سپس [029-unify-stock-ownership.sql](../../Shop/Features/Store/Database/Migrations/029-unify-stock-ownership.sql) نوع مالک و ارتباط مالک با انبار را می‌سازد، ارجاع‌های فروش و حساب را به `Warehouse.SalesPlanReference` منتقل و `Store.SalesPlan` را حذف می‌کند. موجودی و سندهای قبلی به تفکیک طرح حفظ می‌شوند؛ شناسه‌های طرح فروش در جداول وابسته ممکن است نگاشت مجدد شوند. در پایگاه متصل و در بازسازی پایگاه خالی محلی، حفظ ۱۸۰ مانده و ۶ سند تأیید شد. اصلاح migration 007 از بریده‌شدن JSON طولانی در ساخت پایگاه خالی جلوگیری می‌کند.
   سپس [030-account-address-geography-relations.sql](../../Shop/Features/Store/Database/Migrations/030-account-address-geography-relations.sql) نام‌های تکراری نشانی را به FKهای جغرافیا تبدیل می‌کند. اجرا متوقف می‌شود اگر هر نشانی موجود به کشور، استان و شهر متناظر نگاشت نشود؛ سفارش‌ها snapshot مستقل خود را نگه می‌دارند.
   در ادامه [031-remove-warehouse-store-product-key.sql](../../Shop/Features/Store/Database/Migrations/031-remove-warehouse-store-product-key.sql) پس از تطبیق کلیدهای قبلی، ستون متنی تکراری Warehouse را حذف می‌کند. این migration در ۹ نوامبر ۲۰۲۶ روی پایگاه متصل اجرا و ثبت شد؛ ۳۰ محصول و ۳۰ قلم موجودی حفظ شدند. کلید متنی `Store.Product.StoreProductKey` برای سازگاری seedهای قدیمی و media key حفظ شد؛ شناسهٔ خروجی کاتالوگ از `Store.Product.Id` خوانده می‌شد. migration 035 این کلید را با نام `ProductKey` به جدول کالا منتقل می‌کند.
   سپس [032-shared-store-warehouse-product-ids.sql](../../Shop/Features/Store/Database/Migrations/032-shared-store-warehouse-product-ids.sql) کلید Warehouse را با `Warehouse.Product.Id` یکی می‌کند، FKهای وابسته به محصول/تنوع را بازسازی می‌کند و پرچم فروشگاهی را اضافه می‌کند. شناسه‌های فعلی در پایگاه متصل پیش از انتقال برابر بودند؛ migration داده‌های مربوط به موجودی، واحد و تنوع را هم با نگاشت کلید انتقال می‌دهد. در اجرای ۹ نوامبر ۲۰۲۶، ۳۰ محصول، ۳۰ واحد کالا و ۳۰ قلم موجودی حفظ شدند.
   سپس [033-related-article-reference.sql](../../Shop/Features/Store/Database/Migrations/033-related-article-reference.sql) و [034-product-feedback-from-details.sql](../../Shop/Features/Store/Database/Migrations/034-product-feedback-from-details.sql) را اجرا کنید.
   در ادامه [035-unify-product-entity.sql](../../Shop/Features/Store/Database/Migrations/035-unify-product-entity.sql) ستون‌های فروشگاهی را به `Warehouse.Product` منتقل می‌کند، وضعیت‌های انبار و کاتالوگ را تفکیک می‌کند، FKهای ارجاع‌دهنده به `Store.Product` را به ریشهٔ جدید می‌برد و جدول `Store.Product` را حذف می‌کند. Migration داخل تراکنش اجرا می‌شود و در صورت نقص نگاشت یا داده rollback می‌کند.
   سپس [036-category-product-content.sql](../../Shop/Features/Store/Database/Migrations/036-category-product-content.sql) محتوای مشترک را به دسته منتقل و `Store.ProductDetail` را حذف می‌کند؛ در پایگاه متصل ۱۵ دسته این محتوا را دارند.
20. در پایان [001-system-users-and-permissions.sql](../../Shop.Admin/Database/Migrations/001-system-users-and-permissions.sql) را اجرا کنید. این migration هویت مستقل کاربران پنل، نقش، مجوزهای درختی و دسترسی انتخابی کاربران به انبار را در schema جداگانهٔ `Admin` می‌سازد.
21. سپس [002-store-feedback-permissions.sql](../../Shop.Admin/Database/Migrations/002-store-feedback-permissions.sql) را اجرا کنید تا مشاهده و مدیریت دیدگاه‌ها/پرسش‌های فروشگاه به‌صورت مجوزهای جداگانه به درخت دسترسی افزوده شوند.
22. در انتها [005-document-audit.sql](../../Shop/Features/Warehouse/Database/Migrations/005-document-audit.sql) را اجرا کنید تا snapshot تغییرات سندهای انبار مستقل از عمر سند اصلی نگهداری شود.
23. [006-draft-documents-no-stock-effect.sql](../../Shop/Features/Warehouse/Database/Migrations/006-draft-documents-no-stock-effect.sql) رویهٔ موجودی را اصلاح و گردش سندهای موقت موجود را در تراکنش بازسازی می‌کند؛ موجودی فقط با سند قطعی و نوع مؤثر تغییر می‌کند. این migration در ۹ نوامبر ۲۰۲۶ روی پایگاه متصل به پنل اجرا شد.
24. [003-category-content-permissions.sql](../../Shop.Admin/Database/Migrations/003-category-content-permissions.sql) مجوزهای محتوای دسته را اضافه می‌کند.

در ۹ نوامبر ۲۰۲۶، migrationهای Store تا 036، Warehouse تا 006 و Admin تا 003 با نسخهٔ فعلی روی یک پایگاه خالی مستقل SQL Server محلی به‌ترتیب اعمال شدند؛ ۳۰ کالا، ۱۸۰ مانده، ۳۰۵ بازخورد و محتوای ۱۵ دسته حفظ شدند. هر ۱۳ آزمون HTTP روی این پایگاه موفق بود. این تأیید به همان محیط آزمایشی محدود است.

Migrationهای 003 و 016 اصلاحات افزایشی‌اند و دادهٔ موجود را حفظ می‌کنند: طرح سربرگ صندوق پرداخت‌شده به ردیف‌های موجود منتقل می‌شود، ارجاع سند یک‌به‌یک به رابطهٔ یک‌به‌چند تبدیل می‌شود، و حداقل‌های قدیمیِ طرح‌های غیرعادی به طرح عادی همان انبار/قلم منتقل می‌شوند (در صورت چند مقدار، بیشترین مقدار نگه داشته می‌شود). شناسهٔ تنوع در CartItem و CustomerOrderItem nullable است تا سفارش/سبدهای قدیمی حفظ شوند؛ API ذخیرهٔ سبد و checkout اکنون این شناسه را می‌خواند/می‌نویسد و تعلق آن به محصول را کنترل می‌کند. Migrationهای 016، 017 و 020 در ۸ نوامبر ۲۰۲۶ روی پایگاه متصل به تنظیمات `Shop/appsettings.json` اجرا و ثبت شدند؛ اجرای این سه مورد روی هر پایگاه یا محیط میزبانی دیگر جداگانه تأیید نشده است.

`CatalogRepository`، `DatabaseMagazineContentSource` و `DatabaseAccountDataSource` از repository دیتابیس می‌خوانند. کاتالوگ، برند و مجله از جدول‌های رابطه‌ای خوانده می‌شوند و migration 023 جدول snapshot عمومی `Store.RuntimeDataset` را حذف می‌کند؛ دادهٔ جغرافیا و FAQ نیز رابطه‌ای است. فایل‌های JSON اولیه از Shop حذف شده‌اند و migrationهای seed مرجع بازسازی دیتابیس‌اند. وضعیت منابع کاربر در [منابع داده](data-sources.md) آمده است.

`SiteIdentityRequestMiddleware` هویت عمومی را برای درخواست‌های صفحه از `DatabaseSiteIdentitySource` می‌گیرد؛ repository فقط ردیف `Id = 1` جدول `[Store].[SiteIdentity]` را می‌خواند و مقدار به مدت یک دقیقه cache می‌شود. مصرف‌کننده‌های موجود همچنان از `IOptions<SiteIdentityOptions>` استفاده می‌کنند، اما این options در هر درخواست از دیتابیس پر می‌شوند و بخش `SiteIdentity` در `Shop/appsettings.json` حذف شده است. migration 027 در ۹ نوامبر ۲۰۲۶ روی پایگاه متصل به تنظیمات `Shop/appsettings.json` اجرا و ثبت شد.

### جزئیات نرمال‌سازی و محدودیت اجرا

Migration 025 مقادیر تکراری دسته، شاخه، موضوع و حوزهٔ مقاله را به ترتیب در `Mag.ArticleCategory`، `Mag.ArticleBranch`، `Mag.ArticleTopic` و `Mag.ArticleArea` با کلید یکتا نگه می‌دارد؛ `Mag.MagazineArticle` به هر چهار جدول FK اجباری دارد. مقالهٔ مرتبط با `IdRelatedArticle`، تعامل و بازخورد حساب با `IdMagazineArticle`، طرح هر قلم سبد و سفارش با `IdSalesPlan` و والد دسته با `IdParentCategory` به رکورد اصلی وصل می‌شود. کلیدهای متنی قدیمی این روابط پس از انتقال حذف شده‌اند؛ query برای قرارداد خروجی، کلیدهای لازم را از رکوردهای مرتبط می‌سازد. در شکست انتقال مرجع، migration تراکنش را متوقف می‌کند. قیمت، نام و نشانی ثبت‌شده در سفارش برای حفظ تاریخچه همچنان snapshot هستند؛ همهٔ تکرارهای دیگر پروژه نیز به معنای تکمیل این migration حذف نشده‌اند.

در بررسی ۹ نوامبر ۲۰۲۶، migration 025 روی پایگاه متصل اجرا و تکرار آن با ثبت migration بررسی شد؛ این بررسی به معنی تأیید همهٔ محیط‌ها یا ساخت کامل پایگاه خالی نیست. اسکریپت‌های 004 و 021 نیز برای اجرای مجدد پس از حذف ستون قدیمی والد دسته اصلاح و روی پایگاه موجود بررسی شدند. داده‌های seed نمونه‌اند. نتیجهٔ آزمون‌ها و موارد اجرا‌نشده در [کنترل کیفیت](../development/quality.md) ثبت شده است.

جدول `[Admin].[SchemaMigration]` بنا به درخواست از پایگاه بررسی‌شده حذف شد؛ جدول‌های تاریخچهٔ Store و Warehouse حذف نشده‌اند. اسکریپت‌های Admin هنوز از آن استفاده می‌کنند: 001 در صورت نبود جدول آن را می‌سازد و سپس جدول‌های هویت را ایجاد می‌کند و 002 به آن وابسته است. بنابراین اجرای مجدد آن‌ها روی پایگاه دارای جدول‌های Admin بدون بررسی و اصلاح مسیر migration می‌تواند خطا دهد؛ حذف جدول تاریخچه به معنای مستقل‌شدن این اسکریپت‌ها از آن نیست.

از migration 035، `[Warehouse].[Product]` ریشهٔ یگانهٔ کالا است و `[Store].[Product]` حذف شده است. ستون‌های تعریف کاتالوگ، کد، نام، برند، دسته، قیمت پایه، slug، مشخصات متنی و نشان به همین جدول منتقل شده‌اند؛ شناسهٔ کالا برای فروشگاه، انبار، تنوع و موجودی یکسان است. قیمت‌گذاری وابسته به طرح همچنان در `Store.ProductSalesPlanOffer` و قیمت تنوع در `Store.ProductVariantSalesPlanOffer` می‌ماند. `IsWarehouseEnabled` وضعیت استفادهٔ انبار، `IsCatalogActive` فعال‌بودن در کاتالوگ و `IsStoreEnabled` اجازهٔ ارائه/قیمت‌گذاری فروشگاهی را جدا نگه می‌دارند. `ProductKey` فقط کلید متنی سازگاری رسانه و seedهای قدیمی است و شناسهٔ اصلی کالا نیست. Migration 035 تمام FKهای ارجاع‌دهنده به `Store.Product` را به `Warehouse.Product` منتقل کرد.

Migration [026-store-product-badge.sql](../../Shop/Features/Store/Database/Migrations/026-store-product-badge.sql) در نسخهٔ اولیه ستون nullable `Badge` را به `[Store].[Product]` اضافه کرد؛ migration 035 آن را به `[Warehouse].[Product]` منتقل کرد. query جاری کاتالوگ از `Warehouse.Product.Badge` مقدار `badge` خروجی را تولید می‌کند؛ نبود برچسب با `NULL` نمایش داده می‌شود.

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
