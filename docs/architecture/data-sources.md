# منابع داده و مرزهای اطمینان

برای هر صفحه منبع را از ViewModel/Controller به service، contract و repository ردیابی کنید. ثبت DI در `Shop/Program.cs` یا `Admin/Program.cs` و extension مشترک `Data/Infrastructure/DependencyInjection.cs` نشان می‌دهد کدام implementation runtime مصرف می‌شود؛ نام یک interface به‌تنهایی تضمین اتصال دیتابیس نیست.

## لایه‌ها

- `Shop/Controllers/` و `Admin/Controllers/`: HTTP، route و پاسخ.
- `Shop/Services/`: adapterهای دادهٔ سمت فروشگاه و هماهنگی جریان‌ها.
- `Data/Application/`: قراردادها و use caseهای داده.
- `Data/Infrastructure/Persistence/`: EF Core، DbContext و repositoryهای SQL.
- `Data/Application/Email/` و `Data/Infrastructure/Email/`: قرارداد و فرستندهٔ مشترک SMTP که در هر دو برنامه از `AddShopDatabase` ثبت می‌شود.
- `Data/Domain/`: entityها و قواعد دامنه.
- `wwwroot/**/js/`: state و rendering مرورگر؛ snapshot سمت کاربر به‌خودی‌خود منبع قطعی قیمت/سفارش نیست.

## مرز دسترسی صفحات به داده

صفحه‌ها و controllerهای `Shop/` و `Admin/` نباید `ShopDbContext` را تزریق یا resolve کنند و نباید query، تغییر entity، ذخیره یا تراکنش EF را اجرا کنند. آن‌ها باید قراردادهای کاربردی `Data/Application/` را مصرف کنند؛ پیاده‌سازی SQL و جزئیات `ShopDbContext` در `Data/Infrastructure/` می‌ماند. برای تغییرات چندمرحله‌ای، use case داده باید تراکنش و قواعد یکپارچگی را در همان لایه نگه دارد و controller فقط ورودی HTTP را اعتبارسنجی و نتیجه را به پاسخ تبدیل کند.

این مرز هنوز به‌طور کامل در کد اعمال نشده است: controllerهای Shop و endpointهای Admin در چندین حوزه مستقیم به `ShopDbContext` وابسته‌اند. پیش از توسعهٔ هر حوزه، همهٔ دسترسی‌های همان حوزه را به قرارداد و implementation در Data منتقل کنید؛ ارجاع مستقیم جدید اضافه نکنید. جست‌وجوی ممیزی از ریشه: `rg -l "ShopDbContext" Shop Admin -g '*.cs' -g '!**/obj/**' -g '!**/bin/**'`. موارد موجود شامل کاتالوگ و سفارش، حساب، محتوا، تنظیمات، کاربران، گزارش‌ها و گردش انبار است. فایل‌های `Shop/Controllers/AccountController.cs` و `Data/Infrastructure/Persistence/ShopDbContext.ModelConfiguration.cs` در زمان ممیزی تغییر محلی داشتند؛ آن تغییرها باید هنگام مهاجرت حفظ و جداگانه بازبینی شوند.

قراردادهای موجود در `Data/Application/` نقطهٔ شروع‌اند، اما پوشش همهٔ عملیات فعلی Shop و Admin را ندارند. پیش از حذف هر دسترسی از controller، قرارداد و implementation متناظر را اضافه کنید و هم‌زمان رفتار مجوز، اعتبارسنجی، تراکنش و پاسخ API را حفظ کنید. برای نمای کلی لایه‌ها [نمای معماری](overview.md) و برای قراردادهای هر قابلیت راهنمای حوزهٔ مربوط را ببینید.

## مرزهای مهم

سبد مرورگر می‌تواند برای تعامل سریع state نگه دارد؛ سرور باید quote و سفارش را اعتبارسنجی کند. تاریخچهٔ قیمت از نسخه‌های قیمت ثبت‌شده در `Store.PriceVersion` خوانده می‌شود؛ migration snapshot نمی‌تواند تاریخچهٔ قبل از زمان اجرای خودش بازسازی کند. سفارش‌های نمونه یا seed برای تست نمایشی‌اند و نباید به‌عنوان خرید واقعی معرفی شوند. درگاه پرداخت در ثبت DI فعلی `UnconfiguredPaymentGateway` است؛ بنابراین وجود صفحه/endpoint پرداخت به معنی اتصال درگاه واقعی نیست.

برای مسیر دقیق صفحه‌ها از [routing](routing.md)، schema و migration از [database](database.md)، و قابلیت قیمت از [محصول](../store/product.md) و [طرح‌های فروش](../store/sales-plans.md) شروع کنید.

ارسال کد ورود و اعلان فرم تماس در Shop از `IEmailSender` مشترک Data استفاده می‌کند؛ پیام فرم تماس پیش از تلاش SMTP در `Store.ContactFormMessage` ذخیره می‌شود. `AddShopDatabase` همین فرستنده را در DI ادمین نیز ثبت می‌کند؛ در کد فعلی پنل فقط تنظیمات SMTP مدیریت می‌شود و ارسال ایمیل از Admin انجام نمی‌گیرد. خبرنامهٔ مهمان در `Store.NewsletterSubscriber` و خبرنامهٔ حساب‌دار در `Account.Profile.NewsletterEnabled` می‌رود. پیام مدیر به مشتری در `Account.Message` ذخیره و از endpointهای پروفایل بر اساس شناسهٔ نشست خوانده می‌شود.

تبدیل رقم‌های فارسی و عربی به لاتین در `Data/Helpers/Extensions/StringExtensions.cs` مشترک است و هنگام نرمال‌سازی شمارهٔ تماس و پیگیری سفارش استفاده می‌شود.

کد تأیید ایمیل به شکل hash در `Account.EmailVerificationCode` نگهداری می‌شود؛ پیاده‌سازی `Data/Infrastructure/Identity/DatabaseEmailVerificationCodeStore.cs` برای ایجاد، حذف و تأیید از تراکنش `SERIALIZABLE` استفاده می‌کند. ستون `Email` کلید اصلی و ایندکس یکتا است؛ قفل‌های بازهٔ کلید در SQL Server از رقابت هم‌زمان روی همان ایمیل جلوگیری می‌کنند و به `sys.sp_getapplock` نیاز نیست. کد خام ذخیره نمی‌شود؛ پس از تأیید حذف می‌شود، رکورد منقضی هنگام تأیید یا پاک‌سازی فرصت‌طلبانه در درخواست کد بعدی حذف می‌شود و ارسال ناموفق نیز آن را پاک می‌کند. مقدار زمان اعتبار از `Store.SiteSetting.EmailVerificationCodeLifetimeMinutes` می‌آید. داده در restart و بین نمونه‌های برنامه مشترک است.

منطق پرداخت/ثبت attempt در `Data/Infrastructure/Payments/PaymentService.cs` است و قرارداد gateway مستقل از ASP.NET در `Data/Application/Store/Contracts/IPaymentGateway.cs` قرار دارد. چون این سرویس فقط توسط `Shop/Controllers/PaymentController.cs` مصرف می‌شود، ثبت `PaymentService` و gateway در composition root فروشگاه (`Shop/Program.cs`) انجام می‌شود، نه در DI مشترک `AddShopDatabase` که Admin نیز از آن استفاده می‌کند. `PaymentController` فقط نشست، antiforgery و نگاشت پاسخ HTTP را مدیریت می‌کند. دفتر گردش موجودی سند نیز در `Data/Infrastructure/Warehouse/WarehouseStockLedger.cs` قرار دارد؛ endpointهای Admin هنوز مالک کنترل مجوز و تراکنش کلی عملیات هستند.

آخرین مرور ساختار کد: ۱۴ نوامبر ۲۰۲۶. وضعیت هر محیط/پایگاه داده باید جداگانه و با شواهد همان محیط تأیید شود.
