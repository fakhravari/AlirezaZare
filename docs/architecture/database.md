# مدل و زیرساخت پایگاه داده

پروژهٔ داده `Data/Data.csproj` است و هر دو وب‌برنامه به آن ارجاع دارند. EF Core context در `Data/Infrastructure/Persistence/ShopDbContext.cs` قرار دارد؛ entityها زیر `Data/Domain/` و قراردادها زیر `Data/Application/` هستند.

## هنگام تغییر schema

1. entity، نگاشت Fluent و repository مصرف‌کننده را با هم پیدا کنید.
2. migrationهای موجود در مخزن را با `rg --files Data Shop | rg 'Migrations|\.sql$'` بیابید و بررسی کنید. پوشهٔ قبلی `Shop/Features` در این محیط به `C:\AlirezaZare-build-artifacts\shop-features\Features` منتقل شده و migrationهای SQL آن خارج از مخزن هستند.
3. migration افزایشی و امن برای دادهٔ موجود بسازید؛ ترتیب اجرا و پیش‌شرط‌ها را مستند کنید.
4. اجرای محلی/مقصد را جداگانه بررسی کنید و marker ثبت‌شده در همان DB را از وجود فایل migration تفکیک کنید.
5. قرارداد مصرف‌کننده را در راهنمای حوزه به‌روز کنید.

Migration `098-admin-menu-permission-groups.sql` گروه‌های درخت مجوز را با دسته‌بندی منوی پنل هم‌راستا می‌کند. پیش از تغییر والدها، مجوزهای مؤثر نقش‌های فعلی را گسترش می‌دهد تا مرتب‌سازی درخت به‌تنهایی سطح دسترسی نقش‌ها را تغییر ندهد. در ۱۶ نوامبر ۲۰۲۶ روی پایگاه اصلی `bsshopir_fakhravari` اجرا شد و marker آن تأیید شد؛ برای پایگاه‌های دیگر باید جداگانه اجرا شود.

Migration `099-home-testimonials.sql` جدول `Store.HomeTestimonial` را برای انتخاب، ترتیب و تصویر دیدگاه‌های تأییدشدهٔ صفحهٔ نخست می‌سازد، حداکثر شش دیدگاه تازه را برای حفظ نمایش فعلی انتخاب می‌کند و مجوزهای صفحه و API را به شاخهٔ «مشتریان و بازخورد» می‌افزاید. در ۱۶ نوامبر ۲۰۲۶ روی پایگاه اصلی اجرا شد؛ marker، جدول، چهار دیدگاه تأییدشدهٔ منتقل‌شده و مجوزهای صفحه/API پس از اجرا بررسی شدند.

اطلاعات تماس شرکت‌های انبار در entity `Company` و جدول `Warehouse.Company` نگهداری می‌شود. `Phone` تا ۵۰، `Address` تا ۵۰۰ و `Description` تا ۲۰۰۰ نویسه و همگی اختیاری‌اند. migration افزایشی `010-company-contact-details.sql` پس از migration `009-aggregated-sales-dispatch` این ستون‌ها را اضافه می‌کند؛ در ۱۶ نوامبر ۲۰۲۶ روی پایگاه اصلی اعمال و marker و وجود هر سه ستون تأیید شد.

هیچ سندی فهرست ثابتِ «همهٔ migrationهای اجراشده» نیست؛ وضعیت اجرای هر پایگاه را از جدول history/marker همان پایگاه و migration runner واقعی بخوانید. پیش از حذف یا بازسازی داده، backup، شمارش وابستگی و مجوز صریح لازم است.

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

در ۱۵ نوامبر ۲۰۲۶، markerهای `081-admin-page-permissions`، `094-store-brand-display-setting`، `095-remove-checkout-preference` و `096-store-slider` روی دیتابیس اصلی `bsshopir_fakhravari` اعمال و بازخوانی شدند؛ markerهای قبلی Store و Warehouse نیز حاضر بودند. migration آخر جدول قدیمی انتخاب ترجیح پرداختِ ذخیره‌نشده را حذف کرد؛ شمار سفارش‌ها پس از این اجرا ۱۸ بود و جدول سفارش تغییری نکرد. ستون `Store.Brand.ShowInStore` برای ۲۱ برند مقدار غیرتهی دارد؛ migration اسلایدر نیز ۱۲ ردیف دسکتاپ و موبایل ساخته است.

در ۱۶ نوامبر ۲۰۲۶، migration `097-warehouse-types` با اتصال ثبت‌شده در `Admin/appsettings.json` روی `bsshopir_fakhravari` اجرا شد. marker migration، دو نوع انبار، دو تخصیص موجود، صفر مقدار `IdWarehouseType` خالی و صفر تخصیص تکراری تأیید شدند. هر دو `Shop/appsettings.json` و `Admin/appsettings.json` به همان سرور و پایگاه اشاره می‌کنند. صفحهٔ `/Warehouse/Warehouses` از اجرای محلی Admin با همین اتصال باز شد و دو ردیف موجود را نمایش داد.

بازبینی دادهٔ فعلی فروشگاه: ۲۱ برند فعال و قابل‌نمایش‌اند؛ از ۳۲ کالا، ۲۹ کالا `IsCatalogActive=1` و `IsStoreEnabled=1` دارند و ۳ کالا منتشر نیستند. مسیر runtime کالا را با وضعیت انتشار کالا، برند فعال و دسته‌بندی فعال محدود می‌کند. `ShowInStore` فقط فهرست و معرفی برند را کنترل می‌کند و برند فعالِ مخفی، ارتباط کالا را قطع نمی‌کند. چون اکنون برند غیرفعال یا مخفی در دادهٔ واقعی وجود ندارد، اثر این دو حالت از شرط‌های repository بازبینی شد، نه از نمونهٔ تولیدی.
