# مدل و زیرساخت پایگاه داده

پروژهٔ داده `Data/Data.csproj` است و هر دو وب‌برنامه به آن ارجاع دارند. EF Core context در `Data/Infrastructure/Persistence/ShopDbContext.cs` قرار دارد؛ entityها زیر `Data/Domain/` و قراردادها زیر `Data/Application/` هستند.

## هنگام تغییر schema

1. entity، نگاشت Fluent و repository مصرف‌کننده را با هم پیدا کنید.
2. migrationهای موجود در مخزن را با `rg --files Data Shop | rg 'Migrations|\.sql$'` بیابید و بررسی کنید. از پوشهٔ قبلی `Shop/Features`، فایل migration ۰۹۱ گزارش‌ها و seed نمونهٔ صف انبار از تاریخچه بازگردانده شده‌اند؛ سایر فایل‌های منتقل‌شده به `C:\AlirezaZare-build-artifacts\shop-features\Features` در این محیط در دسترس نیستند. بازیابی فایل به معنی اجرای آن در پایگاه مقصد نیست.
3. migration افزایشی و امن برای دادهٔ موجود بسازید؛ ترتیب اجرا و پیش‌شرط‌ها را مستند کنید.
4. اجرای محلی/مقصد را جداگانه بررسی کنید و marker ثبت‌شده در همان DB را از وجود فایل migration تفکیک کنید.
5. قرارداد مصرف‌کننده را در راهنمای حوزه به‌روز کنید.

Migration `107-remove-product-power-source-use-case.sql` مقدارهای دو ستون قدیمی `Warehouse.Product.PowerSource` و `UseCase` را در `Warehouse.ProductLegacyFieldsArchive107` بایگانی می‌کند، تعریف‌های متکی به این ستون‌ها را غیرفعال می‌کند و سپس ستون‌ها را حذف می‌کند. وابستگی ناسازگار تراکنش را برمی‌گرداند. این migration در ۱۷ نوامبر ۲۰۲۶ روی پایگاه اصلی `bsshopir_fakhravari` اجرا شد؛ حذف هر دو ستون، یک marker، بایگانی ۳۲ ردیف از ۳۲ کالا و غیرفعال‌شدن دو تعریف وابسته تأیید شد. نسخه‌های قدیمی Shop و Admin تا استقرار هماهنگ نسخهٔ جدید با ساختار تازه سازگار نیستند.

Migration `105-product-key-code.sql` پیش از حذف `Warehouse.Product.Code` کدهای قبلی را همراه شناسه در `Warehouse.ProductCodeArchive105` بایگانی می‌کند و یکتایی و غیرخالی‌بودن `ProductKey` را تضمین می‌کند. کلیدهای تکراری، خالی یا وابستگی‌های ناسازگار باعث بازگشت تراکنش می‌شوند. این migration در ۱۸ نوامبر ۲۰۲۶ روی پایگاه اصلی `bsshopir_fakhravari` اجرا شد؛ حذف ستون، marker و بایگانی کد هر ۳۲ کالا تأیید و با نسخهٔ پیش از اجرا تطبیق داده شد.

Migration `106-product-badges.sql` عنوان‌های یکتای نشان موجود را در `Store.ProductBadge` ثبت می‌کند و `Warehouse.Product.IdProductBadge` را جایگزین متن `Badge` می‌کند. این migration در ۱۸ نوامبر ۲۰۲۶ روی همان پایگاه اجرا شد؛ دو نشان و ارتباط نشان ۱۰ کالا با دادهٔ پیش از اجرا تطبیق داده شدند. نسخهٔ JSON مقادیر کد و نشان قبل از دو migration در `C:/AlirezaZare-build-artifacts/product-migrations-105-106/before-20261118-000031.json` نگهداری می‌شود و پشتیبان کامل پایگاه نیست. استقرار هماهنگ نسخهٔ جدید Shop و Admin همچنان لازم است.

Migration `108-product-badge-permission.sql` مجوز مدیریت و تخصیص نشان را زیر `store.manager` اضافه می‌کند. این migration در ۱۸ نوامبر ۲۰۲۶ روی پایگاه اصلی `bsshopir_fakhravari` اجرا شد؛ marker و مجوز فعال با مسیر `Store:Catalog:Badges` تأیید شدند. دو نشان و تخصیص نشان به ۱۰ کالا نیز پس از اجرا بررسی شدند. نقش‌های دارای مجوز والد، دسترسی جدید را به ارث می‌برند؛ استقرار نسخهٔ جدید Admin برای استفاده از صفحه لازم است.

Migration `098-admin-menu-permission-groups.sql` گروه‌های درخت مجوز را با دسته‌بندی منوی پنل هم‌راستا می‌کند. پیش از تغییر والدها، مجوزهای مؤثر نقش‌های فعلی را گسترش می‌دهد تا مرتب‌سازی درخت به‌تنهایی سطح دسترسی نقش‌ها را تغییر ندهد. در ۱۶ نوامبر ۲۰۲۶ روی پایگاه اصلی `bsshopir_fakhravari` اجرا شد و marker آن تأیید شد؛ برای پایگاه‌های دیگر باید جداگانه اجرا شود.

Migration `099-home-testimonials.sql` جدول `Store.HomeTestimonial` را برای انتخاب، ترتیب و تصویر دیدگاه‌های تأییدشدهٔ صفحهٔ نخست می‌سازد، حداکثر شش دیدگاه تازه را برای حفظ نمایش فعلی انتخاب می‌کند و مجوزهای صفحه و API را به شاخهٔ «مشتریان و بازخورد» می‌افزاید. در ۱۶ نوامبر ۲۰۲۶ روی پایگاه اصلی اجرا شد؛ marker، جدول، چهار دیدگاه تأییدشدهٔ منتقل‌شده و مجوزهای صفحه/API پس از اجرا بررسی شدند.

اطلاعات تماس شرکت‌های انبار در entity `Company` و جدول `Warehouse.Company` نگهداری می‌شود. `Phone` تا ۵۰، `Address` تا ۵۰۰ و `Description` تا ۲۰۰۰ نویسه و همگی اختیاری‌اند. migration افزایشی `010-company-contact-details.sql` پس از migration `009-aggregated-sales-dispatch` این ستون‌ها را اضافه می‌کند؛ در ۱۶ نوامبر ۲۰۲۶ روی پایگاه اصلی اعمال و marker و وجود هر سه ستون تأیید شد.

هیچ سندی فهرست ثابتِ «همهٔ migrationهای اجراشده» نیست؛ وضعیت اجرای هر پایگاه را از جدول history/marker همان پایگاه و migration runner واقعی بخوانید. پیش از حذف یا بازسازی داده، backup، شمارش وابستگی و مجوز صریح لازم است.

در ۱۶ نوامبر ۲۰۲۶، متن‌های قابل‌ویرایش پایگاه اصلی `bsshopir_fakhravari` برای الگوی تبدیل اشتباه UTF-8 بررسی شدند. ۱۲ عنوان `Store.Slider.Title` و پنج عنوان `Admin.Permission.Name` که تبدیلشان به فارسی بدون اتلاف و به‌صورت برگشت‌پذیر قابل تأیید بود، در یک تراکنش اصلاح شدند. اسکن مجدد ۲۵۸ ستون متنی قابل‌ویرایش با همین الگو مورد دیگری نیافت؛ این نتیجه همهٔ شکل‌های احتمالی خرابی متن را پوشش نمی‌دهد. نسخهٔ مقادیر پیشین و کلید ردیف‌ها در `C:\AlirezaZare-build-artifacts\text-encoding-repair\before-update.json` نگهداری می‌شود.

دامنه‌های اصلی در entityهای `Store`، `Account`، `Warehouse` و `Admin` تفکیک شده‌اند. قیمت نسخه‌ای در `Store.PriceVersion` است. مایگریشن `082-price-version-typed-references.sql` ارجاع polymorphic قدیمی را به ستون‌های FK نوع‌دار تبدیل و `ScopeKey` و `TargetId` را حذف کرد؛ مایگریشن `083-price-version-remove-kind.sql` ستون تکراری `PriceKind` را حذف کرد، `084-price-version-single-reference.sql` پرشدن هم‌زمان بیش از یک FK را ممنوع کرد و `085-price-version-active-constraints.sql` یکتایی نسخهٔ فعال هر مرجع، مبلغ غیرمنفی و تخفیف بین صفر تا کمتر از صد را enforce می‌کند. نوع مؤلفه از تنها FK پرشده نتیجه می‌شود و تطبیق و یکتایی قیمت فعال بر کلیدهای FK استوار است. تاریخچهٔ قبل از نصب نسخه‌بندی را نمی‌توان از snapshot اولیه بازسازی کرد. مرجع قراردادها: [قیمت محصول](../store/product.md)، [طرح‌های فروش](../store/sales-plans.md) و [دیتابیس و داده‌ها](data-sources.md).

ثبت‌نام خبرنامهٔ کاربران مهمان در entity `StoreNewsletterSubscriber` و جدول `Store.NewsletterSubscriber` نگهداری می‌شود؛ ایمیل با حروف کوچک canonical و دارای unique index است. کاربران واردشده در جدول جدید درج نمی‌شوند و از `Account.Profile.NewsletterEnabled` استفاده می‌کنند. ایجاد جدول و اصلاح متن محتوای seed در `076-customer-support-and-newsletter.sql` است؛ فایل SQL باید طبق فرایند استقرار روی هر پایگاه هدف اجرا و marker آن بررسی شود.

پیام فرم تماس در entity `StoreContactMessage` و جدول `Store.ContactFormMessage` ذخیره می‌شود؛ وضعیت‌های عملیاتی `new`, `read`, `closed` و زمان ثبت UTC است. صفحهٔ پنل و تغییر وضعیت از API مجوزدار انجام می‌شود. schema و مجوزهای صفحه/API در migration افزایشی `078-contact-message-inbox.sql` تعریف شده‌اند؛ وجود فایل، اجرای آن در پایگاه مقصد را تضمین نمی‌کند.

پیام‌های ارسالی مدیر به حساب مشتری در entity `AccountMessage` و جدول `Account.Message` نگهداری می‌شوند؛ پیام به پروفایل مالک وصل است، هنگام حذف پروفایل cascade می‌شود و وضعیت خواندن مختص همان پیام است. جدول و مجوزهای مدیریت پیام در migration `078-contact-message-inbox.sql` ساخته می‌شوند.

متن پیام حساب حداکثر ۴۰۰۰ نویسه است. migration شمارهٔ ۰۷۸ جدول‌های پیام را می‌سازد و در صورت وجود schema مجوز Admin، رکوردهای مجوز صفحه/API را نیز اضافه می‌کند؛ marker ثبت‌شده به‌تنهایی وجود schema مجوزها را تضمین نمی‌کند.

مدت اعتبار و فاصلهٔ ارسال مجدد کد ورود ایمیل در `Store.SiteSetting.EmailVerificationCodeLifetimeMinutes` قرار دارد. مهاجرت افزایشی `077-email-verification-code-lifetime.sql` ستون را با مقدار پیش‌فرض ۱۵ دقیقه اضافه می‌کند؛ صفحهٔ «تنظیمات سایت» در پنل مدیریت مقدار ۱ تا ۱۴۴۰ دقیقه را ذخیره می‌کند. رفتار احراز در [راهنمای حساب](../account/profile.md) توضیح داده شده است.

توضیح فوتر و نشانی‌های اختیاری اینستاگرام، تلگرام و لینکدین در ستون‌های `FooterDescription`, `InstagramUrl`, `TelegramUrl` و `LinkedInUrl` از `Store.SiteSetting` ذخیره می‌شوند. صفحهٔ تنظیمات سایت مدیریتشان می‌کند و migration افزایشی `080-footer-content-and-social-links.sql` ستون‌ها را می‌افزاید؛ لینک‌های خالی در فوتر نمایش داده نمی‌شوند. قرارداد رندر در [راهنمای پوسته](../frontend/layout.md) است.

کد تأیید ایمیل در جدول `Account.EmailVerificationCode` فقط به صورت SHA-256 hash ذخیره می‌شود؛ کلید اصلی ایمیل canonical، انقضا و شمارندهٔ تلاش‌ها مانع چند کد هم‌زمان و بیش از پنج تلاش است. `079-database-email-verification-codes.sql` جدول و index انقضا را می‌سازد. migration باید طبق فرایند استقرار روی هر پایگاه هدف اجرا و marker آن جداگانه بررسی شود؛ از آن‌جا که کد خام قبلی فقط در حافظه بود، آن کدهای فعال به این جدول منتقل نمی‌شوند.

[معماری](README.md) · [توسعه](../development/README.md) · [فهرست docs](../README.md)

در ۱۴ نوامبر ۲۰۲۶، markerهای migrationهای `076-customer-support-and-newsletter`, `077-email-verification-code-lifetime` و `078-contact-message-inbox` در پایگاه اصلی بررسی‌شده تأیید شدند؛ ۰۷۷ پیش‌تر ثبت شده بود و ۰۷۶/۰۷۸ همان روز اعمال شدند. جدول‌ها، مجوزهای پنل و مقدار تنظیم اعتبار کد (۱۵ دقیقه) نیز در همان پایگاه بررسی شد. همان روز migration `079-database-email-verification-codes` روی پایگاه اصلی اجرا شد؛ وجود جدول شش‌ستونی `Account.EmailVerificationCode` و marker یکتای ۰۷۹ پس از اجرا تأیید شد. این یادداشت فقط وضعیت همان مقصد در زمان بررسی را ثبت می‌کند و وضعیت محیط‌های دیگر را مشخص نمی‌کند.

در ۱۴ نوامبر ۲۰۲۶، migration `080-footer-content-and-social-links` روی پایگاه اصلی `bsshopir_fakhravari` اجرا شد. marker ۰۸۰، چهار ستون جدید `Store.SiteSetting` و حفظ توضیح فوتر رکورد تنظیمات تأیید شدند.

بازبینی ساختار: ۱۴ نوامبر ۲۰۲۶.
- Migration `086-contact-replies-newsletter-admin.sql` ستون ایمیل و پاسخ فرم تماس و تاریخ لغو عضویت خبرنامه را اضافه می‌کند.
Migration `087-assign-store-feature-permissions.sql` مجوزهای خبرنامه و پاسخ فرم تماس را به نقش `system.full-access-store` تخصیص می‌دهد.
Migration `088-fix-admin-permission-unicode.sql` متن چهار مجوز جدید Admin را که در اجرای اولیه با encoding نادرست ثبت شده بود، به Unicode صحیح برمی‌گرداند.
Migration `089-repair-variant-attributes-json.sql` دادهٔ قدیمی `Warehouse.ProductVariant.AttributesJson` را به آرایهٔ ویژگی معتبر تبدیل می‌کند تا catalog و modal ورود به‌دلیل خطای renderer متوقف نشوند.

Migration `090-newsletter-permission-store-manager.sql` مجوز گروهی `store.manager` را به‌عنوان والد صفحهٔ اعضای خبرنامه در درخت مجوزها ثبت می‌کند و آن را به نقش `system.full-access-store` می‌دهد؛ مجوزهای فهرست و تغییر عضویت همچنان زیر مجوز صفحه باقی می‌مانند.

گزارشات فروشگاه به schema تازه نیاز ندارند. Migration `091-store-reports-permissions.sql` مجوز صفحه و endpointهای خواندن و خروجی Excel را زیر `store.manager` می‌سازد و دسترسی نقش `system.full-access-store` را از والد می‌گیرد. فایل migration به‌تنهایی به معنی اجرا شدن آن روی هیچ پایگاه داده‌ای نیست.

وضعیت مزیت کالا در `Store.ProductBenefit.IsActive` نگهداری می‌شود؛ migration `092-store-product-benefit-status.sql` ستون را با مقدار پیش‌فرض فعال برای ردیف‌های موجود می‌سازد. migration `093-drop-unused-paid-order-inbox.sql` فقط وقتی سه جدول قدیمی `Warehouse.PaidOrderInbox*` خالی باشند آن‌ها را حذف می‌کند و در صورت وجود داده عمداً متوقف می‌شود.
در جدول `Store.Brand`، ستون `IsActive` انتشار برند و ستون `ShowInStore` نمایش برند در معرفی‌های فروشگاه را مستقل کنترل می‌کند. migration `094-store-brand-display-setting.sql` ستون دوم را با مقدار پیش‌فرض فعال برای داده‌های قبلی اضافه می‌کند. فهرست برندهای عمومی از برندهای فعال و دارای `ShowInStore` ساخته می‌شود؛ کالاها فقط به فعال‌بودن برند وابسته‌اند. جزئیات رفتار در [کاتالوگ فروشگاه](../store/catalog.md) است.
## اجرای migrationها در دیتابیس اصلی

در ۱۷ نوامبر ۲۰۲۶، در پایگاه `bsshopir_fakhravari` ستون `Warehouse.ProductVariant.IsDefault` از قبل `NOT NULL` بود. برای کالای ۱، تنها تنوع فعال و دارای قلم انبار فعال، یعنی ردیف آزمایشی ۳۹، پیش‌فرض شد؛ تنوع آزمایشی غیرفعال ۴۰ پیش‌فرض نیست. این وضعیت فقط مربوط به همان پایگاه و زمان بررسی است.

در ۱۵ نوامبر ۲۰۲۶، markerهای `081-admin-page-permissions`، `094-store-brand-display-setting`، `095-remove-checkout-preference` و `096-store-slider` روی دیتابیس اصلی `bsshopir_fakhravari` اعمال و بازخوانی شدند؛ markerهای قبلی Store و Warehouse نیز حاضر بودند. migration آخر جدول قدیمی انتخاب ترجیح پرداختِ ذخیره‌نشده را حذف کرد؛ شمار سفارش‌ها پس از این اجرا ۱۸ بود و جدول سفارش تغییری نکرد. ستون `Store.Brand.ShowInStore` برای ۲۱ برند مقدار غیرتهی دارد؛ migration اسلایدر نیز ۱۲ ردیف دسکتاپ و موبایل ساخته است.

در ۱۶ نوامبر ۲۰۲۶، migration `097-warehouse-types` با اتصال ثبت‌شده در `Admin/appsettings.json` روی `bsshopir_fakhravari` اجرا شد. marker migration، دو نوع انبار، دو تخصیص موجود، صفر مقدار `IdWarehouseType` خالی و صفر تخصیص تکراری تأیید شدند. هر دو `Shop/appsettings.json` و `Admin/appsettings.json` به همان سرور و پایگاه اشاره می‌کنند. صفحهٔ `/Warehouse/Warehouses` از اجرای محلی Admin با همین اتصال باز شد و دو ردیف موجود را نمایش داد.

بازبینی پیشین دادهٔ فروشگاه، پیش از migration ۱۰۰: ۲۱ برند فعال و قابل‌نمایش و از ۳۲ کالا، ۲۹ کالا با هر دو وضعیت قدیمی فعال و ۳ کالا منتشرنشده بودند. این شمارش وضعیت کنونی پایگاه را تضمین نمی‌کند. `ShowInStore` فقط فهرست و معرفی برند را کنترل می‌کند و برند فعالِ مخفی، ارتباط کالا را قطع نمی‌کند.

Migration `100-product-store-publication.sql` وضعیت انتشار قبلی را با حاصل AND دو ستون قدیمی در `Warehouse.Product.IsStoreEnabled` نگه می‌دارد و سپس `IsCatalogActive` را حذف می‌کند. در صورت وجود وابستگی ناشناخته که مانع حذف ستون شود، کل تراکنش برمی‌گردد؛ marker فقط پس از موفقیت ثبت می‌شود. برای حفظ سازگاری، Shop و Admin قدیمی را پیش از اجرا متوقف کنید و migration و نسخهٔ جدید هر دو برنامه را در یک پنجرهٔ استقرار اجرا کنید. رفتار جدید در [کاتالوگ](../store/catalog.md) ثبت شده است.

در ۱۷ نوامبر ۲۰۲۶، migration ۱۰۰ روی پایگاه اصلی `bsshopir_fakhravari` اجرا شد. marker یکتا و حذف ستون قدیمی تأیید شد؛ وضعیت انتشار هر ۳۲ کالا با وضعیت قبلی تطبیق داده شد: ۲۹ منتشر و ۳ پیش‌نویس. نسخهٔ مقادیر دو ستون پیش از اجرا در `C:\AlirezaZare-build-artifacts\product-publication-migration\before-20261117-105501.json` نگهداری می‌شود و پشتیبان کامل دیتابیس نیست. پس از migration، صفحهٔ اصلی فروشگاه HTTP 500 برگرداند؛ وضعیت صفحه پیش از migration بررسی نشد و در این نوبت استقراری انجام نشده است.

Migration `101-product-required-fields.sql` نام لاتین، قیمت فروش، درصد تخفیف و سقف خرید کالا را پس از اطمینان از نبود مقدار تهی الزامی می‌کند؛ در صورت وجود مقدار تهی بدون تغییر داده متوقف می‌شود. پیش‌فرض انتشار کالا صفر و پیش‌نویس است. این migration در ۱۷ نوامبر ۲۰۲۶ روی پایگاه اصلی اجرا و marker، ساختار و شمار ۳۲ کالا بررسی شد. فیلدهای اختیاری و ارجاع‌های مربوط به مراحل بعدی بدون تغییر باقی می‌مانند. سایر جدول‌ها برای الزامات اختصاصی هر حوزه به بررسی تکمیلی نیاز دارند.
