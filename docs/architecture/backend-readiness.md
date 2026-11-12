# مرزهای فعلی backend

این صفحه مرزهای اجرایی مهم را خلاصه می‌کند؛ وضعیت یکپارچگی را از ثبت DI و مسیرهای جاری کد بسنجید.

## نکات قطعی از کد

- برنامهٔ فروشگاه، سرویس‌های repository و دیتابیس را در `Shop/Program.cs` ثبت می‌کند.
- درگاه در DI با `UnconfiguredPaymentGateway` ثبت شده است؛ نام بانک/درگاه در جدول و UI به معنی اتصال عملیاتی نیست.
- طرح‌های فروش و quote از `ISalesPlanSource` و پیاده‌سازی دیتابیس استفاده می‌کنند.
- نسخه‌های قیمت در `Store.PriceVersion` نگهداری می‌شوند؛ تاریخچه به نسخه‌های ثبت‌شده محدود است.
- endpointهای مدیریتی در پروژهٔ Admin هستند و باید مجوز endpoint و دامنهٔ نقش را بررسی کرد.

## برای تأیید هر قابلیت

Controller/API → registration در `Program.cs` → interface و implementation → repository/DbContext → View/JS مصرف‌کننده را دنبال کنید. برای هر محیط، connection string و migration history همان پایگاه را جدا بررسی کنید. Seed دادهٔ نمونه را معادل دادهٔ production نگیرید.

مسیرهای بیشتر در [منابع داده](data-sources.md)، [دیتابیس](database.md)، [پرداخت](../store/payment.md)، [طرح فروش](../store/sales-plans.md) و [مدیریت](../admin/README.md) است.

این سند شکاف‌های عملیاتی را فهرست ثابت نمی‌کند؛ کد و تنظیمات محیط ممکن است تغییر کنند. مرور کد: ۱۲ نوامبر ۲۰۲۶.
