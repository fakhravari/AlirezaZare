# راهنمای عامل کدنویسی مخزن

برای آشنایی سریع، ابتدا [`README.md`](README.md) و سپس [`DEVELOPER_ROADMAP.md`](DEVELOPER_ROADMAP.md) را بخوانید. برای تغییر UI/CSS/JS، [`UI_LAYER_REFERENCE.md`](UI_LAYER_REFERENCE.md) و برای metadata/structured data، [`SEO_METADATA.md`](SEO_METADATA.md) نیز مرجع لازم هستند.

## روند کاری الزامی

1. ابتدا `git status --short` اجرا کنید؛ تغییرهای موجود کاربر را حفظ کنید و reset/clean نکنید.
2. مسیر کامل قابلیت را ردیابی کنید: route/controller → model یا منبع داده → Razor/partial → layout → CSS/JS. از جدول route و asset در Roadmap و UI Reference کمک بگیرید.
3. ترتیب assetهای واقعاً بارگذاری‌شده را در `Shop/Views/Shared/_Shop.cshtml` بررسی کنید؛ وجود فایل در repo به معنی استفاده‌شدن آن نیست.
4. کوچک‌ترین تغییر منسجم را در owner واقعی اعمال کنید. markup، listener یا selector عمومی موازی نسازید و `!important` تازه را راه‌حل پیش‌فرض نکنید.
5. فارسی و RTL را حفظ کنید؛ responsive، keyboard/focus، نام دسترس‌پذیر و reduced-motion را متناسب با تغییر بررسی کنید.
6. برای ظاهر، ابتدا token و component مشترک را بررسی کنید؛ قانون برنده و load order را از DevTools تأیید کنید و از append کردن override عمومی به `industrial-system.css` پرهیز کنید.
7. منبع حقیقت داده را مشخص کنید. دادهٔ ثابت C#، کاتالوگ JavaScript و `localStorage` را سرور، سفارش واقعی یا دیتابیس فرض نکنید.
8. اعتبارسنجی محدود و مرتبط انجام دهید؛ در صورت اقتضا `dotnet build Shop.sln` را اجرا کنید. solution فعلاً test project ندارد؛ تستی را که اجرا نشده ادعا نکنید.
9. اگر route، asset، source of truth یا رفتار تغییر کرد، README و سند تخصصی مرتبط را همگام کنید و لینک‌های داخلی را بررسی کنید.

## محدودهٔ مخزن

- `Shop/` برنامهٔ ASP.NET Core MVC و تنها پروژهٔ فعلی `Shop.sln` است.
- `ClockTemplate/` فعلاً artifactهای تولید/ساخت دارد، نه پروژهٔ source عضو solution؛ پیش از هر نتیجه‌گیری عضویت و source را بررسی کنید.
- فایل generated، vendor یا ظاهراً بدون مصرف را صرفاً برای cleanup حذف نکنید؛ ارجاع، build و runtime را اول بررسی کنید.

## منابع و اولویت

- `README.md` فهرست و مسیر یادگیری اسناد است؛ `DEVELOPER_ROADMAP.md` مرجع معماری و `UI_LAYER_REFERENCE.md` مرجع جزئیات UI است.
- برای metadata، structured data، sitemap و robots از `SEO_METADATA.md` استفاده کنید.
- اگر سند با کد یا رفتار runtime اختلاف داشت، کد منبع نهایی است؛ پس از بررسی، سند مرتبط و فهرست README را اصلاح کنید.
