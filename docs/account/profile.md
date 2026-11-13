# پروفایل و داده‌های حساب

ورود/ثبت‌نام و endpointهای احراز را از `Shop/Controllers/AccountController.cs`، `EmailVerificationController.cs` و `PasswordLoginController.cs` دنبال کنید. صفحهٔ حساب در `ProfileController.cs` و `Shop/Views/Profile/` است. سمت مرورگر، projection حساب در `Shop/wwwroot/shop/js/store/account-data.js` و صفحه در `js/pages/account-page.js` مصرف می‌شود.

پس از ورود، `main.js` داده‌های قدیمی مرورگر را به API حساب منتقل می‌کند؛ جستجوهای اخیر هم از این مسیر به `account/recent-searches` منتقل می‌شوند.

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

بازبینی مسیرهای کد: ۱۲ نوامبر ۲۰۲۶.
