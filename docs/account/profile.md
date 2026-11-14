# پروفایل و داده‌های حساب

ورود/ثبت‌نام و endpointهای احراز را از `Shop/Controllers/AccountController.cs`، `EmailVerificationController.cs` و `PasswordLoginController.cs` دنبال کنید. صفحهٔ حساب در `ProfileController.cs` و `Shop/Views/Profile/` است. سمت مرورگر، projection حساب در `Shop/wwwroot/shop/js/store/account-data.js` و صفحه در `js/pages/account-page.js` مصرف می‌شود.

ورود با کد ایمیل در `EmailVerificationController` از `IEmailVerificationCodeStore` در `Data/Application/Account/Contracts` برای چرخهٔ کد و از قرارداد مشترک `Data/Application/Email/Contracts/IEmailSender` برای ارسال استفاده می‌کند. پیاده‌سازی SQL در `Data/Infrastructure/Identity/DatabaseEmailVerificationCodeStore.cs` است. مدت اعتبار و فاصلهٔ ارسال دوباره از `Store.SiteSetting.EmailVerificationCodeLifetimeMinutes` خوانده می‌شود و مدیر در بخش «ارسال ایمیل» تنظیمات سایت آن را تغییر می‌دهد؛ مقدار پیش‌فرض ۱۵ و بازهٔ مجاز ۱ تا ۱۴۴۰ دقیقه است. hash کد و شمارندهٔ حداکثر پنج تلاش در `Account.EmailVerificationCode` ذخیره می‌شود؛ کد خام ذخیره نمی‌شود. رکورد منقضی هنگام تأیید ناموفق یا پاک‌سازی فرصت‌طلبانه در درخواست کد بعدی حذف می‌شود؛ شکست ارسال هم رکورد را پاک می‌کند. جزئیات جدول در [راهنمای دیتابیس](../architecture/database.md) است.

خبرنامهٔ صفحهٔ اصلی به `POST /api/store/newsletter` می‌فرستد. کاربر واردشده با شناسهٔ نشست در `Account.Profile.NewsletterEnabled` ثبت می‌شود؛ برای مهمان، ایمیل lowercase-normalized در `Store.NewsletterSubscriber` ذخیره می‌شود. تکرار ایمیل مهمان پاسخ موفقِ ازقبل‌ثبت‌شده می‌گیرد. کنترل پروفایل هم از `PUT /api/account/profile` ترجیح خبرنامه را ذخیره می‌کند. پیگیری سفارش عمومی با `POST /api/store/orders/tracking` به کد سفارش و موبایل snapshot نشانی ارسال نیاز دارد؛ درخواست‌ها rate-limit می‌شوند و پاسخ فقط وضعیت، شرکت/کد حمل و تاریخچهٔ ارسال را شامل می‌شود.

پس از ورود، `main.js` داده‌های قدیمی مرورگر را به API حساب منتقل می‌کند؛ جستجوهای اخیر هم از این مسیر به `account/recent-searches` منتقل می‌شوند. پیام‌های پروفایل دیگر دادهٔ نمایشی نیستند: `GET /api/account/messages` فقط پیام‌های پروفایل جاری را از `Account.Message` می‌خواند و `PUT /api/account/messages/{id}/read` وضعیت خواندن همان پیام را با کنترل مالکیت و CSRF ثبت می‌کند. مدیر عنوان، دسته و متن حداکثر ۴۰۰۰ نویسه‌ای را از بخش «پیام‌های حساب مشتریان» پنل می‌فرستد.

## ردیابی داده

پروفایل حساب ستون nullable به نام `Password` دارد. ثبت‌نام و تغییر رمز مقدار رمز را مستقیماً در آن می‌نویسند و ورود مقدار ذخیره‌شده را با ورودی مقایسه می‌کند. این قرارداد متن ساده است و برای داده‌های جدید هش انجام نمی‌شود. مهاجرت `064-account-profile-password-column.sql` ستون قبلی را به `Password` تغییرنام می‌دهد؛ مقدارهای هش قدیمی تا زمان تغییر رمز با مسیر سازگاری بررسی می‌شوند.

رمز ورود و ثبت‌نام باید دست‌کم ۱ نویسه داشته باشد؛ طول بیش از ۵۰۰ نویسه، با محدودیت ستون ذخیره‌سازی سازگار نیست.

جنسیت پروفایل در `Account.Profile.IdGender` به مرجع `Account.Gender.Id` وصل است. کد و عنوان فارسی گزینه‌ها از مرجع خوانده می‌شوند؛ API و فرم پروفایل شناسهٔ مرجع را ارسال می‌کنند. مهاجرت `065-account-gender-reference.sql` مقدارهای متنی قدیمی را به شناسه نگاشت می‌کند و سپس ستون متنی را حذف می‌کند؛ `066-account-gender-id-column-convention.sql` نام کلید خارجی را با قرارداد `Id` در ابتدای نام هماهنگ می‌کند.

Controller/API → `IAccountDataSource` و implementation ثبت‌شده در `Shop/Program.cs` → repositoryهای Data → JSON/API و View. حساب با cookie سرور احراز می‌شود؛ شناسهٔ مالکیت را از payload مرورگر نپذیرید. سفارش/نشانی/علاقه‌مندی‌های نمایش‌داده‌شده باید با مجوز و پروفایل جاری فیلتر شوند.

نشانی‌های محلی ایران و جغرافیا را تا API و جدول‌های `Geo` ردیابی کنید. دادهٔ sample را با تاریخچهٔ واقعی سفارش اشتباه نگیرید. کارت‌ها و پیگیری‌های قیمت ممکن است snapshot یا state متفاوت داشته باشند؛ مرجع مسیر هرکدام در [سبد](../store/cart.md) و [قیمت محصول](../store/product.md) است.

فاکتور سفارش مبلغ پیش از تخفیف، کد کوپن، مبلغ تخفیف و مبلغ قابل پرداخت را از snapshot سفارش نمایش می‌دهد؛ محاسبه و ثبت آن در checkout مستند شده در [راهنمای سبد](../store/cart.md).

## بررسی

نشست مهمان/واردشده، پاسخ 401، CSRF، دادهٔ خالی، خطای API، مالکیت رکورد و RTL را بررسی کنید. قراردادهای مشترک JavaScript در [frontend/javascript](../frontend/javascript.md).

[حساب](README.md) · [فهرست docs](../README.md)

بازبینی مسیرهای کد: ۱۴ نوامبر ۲۰۲۶.
