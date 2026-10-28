# منابع داده و مرزهای فعلی

هدف این صفحه تعیین منبع حقیقت است؛ ساختار فیلدهای محصول در [کاتالوگ](../store/catalog.md) و کلیدها/رویدادهای مرورگر در [JavaScript](../frontend/javascript.md) نگهداری می‌شوند.

| داده | منبع فعلی | مصرف و محدودیت |
|---|---|---|
| مسیر و مدل صفحه | [Controllers](../../Shop/Controllers) و [PageModel](../../Shop/Models/PageModel.cs) | آماده‌سازی Razor و SEO |
| محصول، برند، کشور، رنگ، دسته، رسانه و فهرست‌های خانه | [catalog.json](../../Shop/wwwroot/content/data/catalog.json) | منبع مشترک ساختاریافته با schemaVersion و شناسه‌های صریح؛ ۳۰ محصول نمونه |
| کاتالوگ سمت سرور | [CatalogRepository.cs](../../Shop/Services/CatalogRepository.cs) | singleton با cache بر اساس زمان/اندازهٔ فایل؛ اعتبارسنجی رابطه‌ها و تولید snapshot برای محصول، برند و sitemap |
| کاتالوگ مرورگر | [_CatalogData.cshtml](../../Shop/Views/Shared/_CatalogData.cshtml) → [catalog.js](../../Shop/wwwroot/shop/js/store/catalog.js) | JSON امن داخل script غیرقابل‌اجرا؛ resolver، دادهٔ immutable و API سازگار HNProducts/HNCountries |
| کشور، استان و شهر نشانی | [iran-places.js](../../Shop/wwwroot/shop/js/pages/iran-places.js) | دادهٔ محلی پیوست با سه مدل Country/Province/City؛ ۱ کشور، ۳۱ استان و ۲۵۲۲ شهر. همهٔ ۱۴ ستون ورودی نگهداری می‌شوند؛ ستون CountryName که در ورودی نام شهر دارد در City با نام sourceCountryName حفظ شده است. فرم استان و شهر را انتخاب می‌کند؛ شهرها بر اساس استان فیلتر می‌شوند و کشور ایران در رکورد آدرس ثابت می‌ماند. |
| اطلاعات تحریریهٔ پایهٔ برند | [BrandProfileModel.cs](../../Shop/Models/BrandProfileModel.cs) | متن/تاریخچه از BrandCatalog؛ نام، نام فارسی و لوگو هنگام ساخت snapshot از catalog.json گرفته می‌شوند |
| متن تحریریهٔ برند | [brand-editorial.json](../../Shop/wwwroot/content/data/brand-editorial.json) | دادهٔ عمومی ساختاریافته در `wwwroot/content/data/`؛ خوانده‌شدن سمت سرور توسط BrandsController |
| مقاله‌ها و آمار مجله | [JournalArticles.cs](../../Shop/Models/JournalArticles.cs) و [JournalArticles.Expanded.cs](../../Shop/Models/JournalArticles.Expanded.cs) | دادهٔ ثابت؛ منابع ثبت‌شده یا fallback موضوعی، برچسب‌ها و مقالات مرتبط هنگام ساخت مدل دیتیل محاسبه می‌شوند؛ آمار نمایش‌داده‌شده عملیاتی یا خودکار نیست |
| FAQ | `FaqCatalog` در [PageModel.cs](../../Shop/Models/PageModel.cs) | منبع مشترک متن صفحه و FAQPage JSON-LD |
| ارسال و تأیید کد ایمیل | [EmailVerificationController](../../Shop/Controllers/EmailVerificationController.cs)، [EmailVerificationCodeStore](../../Shop/Services/EmailVerificationCodeStore.cs) و [SmtpEmailSender](../../Shop/Services/SmtpEmailSender.cs) | SMTP از `Email:Smtp` در appsettings خوانده می‌شود؛ OTP در حافظهٔ سرور به‌شکل hash و یک‌بارمصرف است. ساخت حساب و session همچنان دمو هستند؛ برای چند replica store مشترک لازم است |
| سبد، ورود، مقایسه، علاقه‌مندی، آدرس و پروفایل | `HNUI`، script صفحه و `localStorage` | state مرورگر؛ سفارش، پرداخت یا هویت تأییدشدهٔ سرور نیست |
| رسانه‌ها | [ساختار content](../media/images.md) | مسیرهای محصول در catalog.json ثبت‌اند؛ نام فایل از ترتیب آرایه ساخته نمی‌شود |


کاتالوگ در مصرف‌کننده‌های سرور از `ICatalogSource.ReadAsync` خوانده می‌شود؛ `CatalogRepository` پیاده‌سازی فایل آن است. سفارش و کوپن در `Shop/App_Data/account-demo.json` با مدل `AccountData` نگهداری می‌شوند؛ `IAccountDataSource` برای حساب و `ICommerceOfferSource` برای پیشنهاد عمومی است. شناسهٔ سفارش مرجع سابقهٔ مصرف کوپن است، تاریخ انتقال ISO و مبلغ حساب عدد صحیح تومان (IRT) است. وضعیت کامل آمادگی و محدودیت‌ها در [اتصال backend](backend-readiness.md) ثبت شده است.

## هنگام تغییر داده

فایل‌های دادهٔ عمومی را کنار کد JavaScript نگذارید؛ آن‌ها را در `wwwroot/content/data/` نگه دارید و کد را از `wwwroot/shop/js/` مصرف کنید. دادهٔ خصوصی سرور در `App_Data/` می‌ماند. دادهٔ محصول را فقط در catalog.json تغییر دهید. `id` و `code` پایدارند؛ relationها و مسیرها باید معتبر بمانند. parser متن JavaScript حذف شده است. تغییر schema باید هم‌زمان در CatalogSnapshot و resolver مرورگر اعمال شود؛ قرارداد دقیق در [کاتالوگ](../store/catalog.md) و بررسی‌های خودکار در [کیفیت](../development/quality.md) ثبت شده‌اند.

دادهٔ ذخیره‌شده در مرورگر را ورودی قابل‌اعتماد فرض نکنید. OTP ایمیل با endpoint سرور تأیید می‌شود؛ سایر persistenceها همچنان به مدل دامنه، validation، هویت، مجوز و endpoint سرور نیاز دارند؛ برنامهٔ این کار در [رودمپ](../development/roadmap.md) آمده است.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۲۸ اکتبر ۲۰۲۶.
