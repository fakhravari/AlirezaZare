# منابع داده و مرزهای اطمینان

برای هر صفحه منبع را از ViewModel/Controller به service، contract و repository ردیابی کنید. ثبت DI در `Shop/Program.cs` نشان می‌دهد کدام implementation runtime مصرف می‌شود؛ نام یک interface به‌تنهایی تضمین اتصال دیتابیس نیست.

## لایه‌ها

- `Shop/Controllers/` و `Admin/Controllers/`: HTTP، route و پاسخ.
- `Shop/Services/`: adapterهای دادهٔ سمت فروشگاه و هماهنگی جریان‌ها.
- `Data/Application/`: قراردادها و use caseهای داده.
- `Data/Infrastructure/Persistence/`: EF Core، DbContext و repositoryهای SQL.
- `Data/Domain/`: entityها و قواعد دامنه.
- `wwwroot/**/js/`: state و rendering مرورگر؛ snapshot سمت کاربر به‌خودی‌خود منبع قطعی قیمت/سفارش نیست.

## مرزهای مهم

سبد مرورگر می‌تواند برای تعامل سریع state نگه دارد؛ سرور باید quote و سفارش را اعتبارسنجی کند. تاریخچهٔ قیمت از نسخه‌های قیمت ثبت‌شده در `Store.PriceVersion` خوانده می‌شود؛ migration snapshot نمی‌تواند تاریخچهٔ قبل از زمان اجرای خودش بازسازی کند. سفارش‌های نمونه یا seed برای تست نمایشی‌اند و نباید به‌عنوان خرید واقعی معرفی شوند. درگاه پرداخت در ثبت DI فعلی `UnconfiguredPaymentGateway` است؛ بنابراین وجود صفحه/endpoint پرداخت به معنی اتصال درگاه واقعی نیست.

برای مسیر دقیق صفحه‌ها از [routing](routing.md)، schema و migration از [database](database.md)، و قابلیت قیمت از [محصول](../store/product.md) و [طرح‌های فروش](../store/sales-plans.md) شروع کنید.

آخرین مرور ساختار کد: ۱۲ نوامبر ۲۰۲۶. وضعیت هر محیط/پایگاه داده باید جداگانه و با شواهد همان محیط تأیید شود.
