# روند کار روی مخزن و مستندات

قواعد سراسری در [AGENTS.md ریشه](../AGENTS.md) قرار دارند. ابتدا [فهرست مستندات](README.md)، سپس [نمای معماری](architecture/overview.md) و راهنمای حوزهٔ مسئول را بخوانید. برای UI، [پوسته](frontend/layout.md)، [CSS](frontend/styling.md) و [JavaScript](frontend/javascript.md) مرجع‌اند؛ برای metadata از [SEO](seo/metadata.md) استفاده کنید.

## روند الزامی

1. ابتدا `git status --short` و diff را بررسی کنید. تغییرهای موجود کاربر را حفظ کنید و reset/clean نکنید.
2. مسیر قابلیت را از route/controller → مدل یا منبع داده → Razor/partial → layout → CSS/JS ردیابی کنید. از [نقشهٔ صفحه‌ها](architecture/routing.md) کمک بگیرید.
3. منابع واقعاً بارگذاری‌شده را در `Shop/Views/Shared/_Shop.cshtml` بررسی کنید؛ وجود فایل در مخزن به معنی مصرف‌شدن نیست.
4. کوچک‌ترین تغییر منسجم را در مالک واقعی اعمال کنید. markup، listener یا selector عمومی موازی نسازید؛ `!important` تازه و override انتهایی راه‌حل پیش‌فرض نیست.
5. برای ظاهر، ابتدا token و component مشترک را بررسی و قانون برنده را در DevTools تأیید کنید. افزودن override عمومی به industrial-system.css را جای اصلاح مالک قرار ندهید.
6. فارسی، RTL، responsive، keyboard/focus، نام دسترس‌پذیر و reduced-motion را متناسب با تغییر حفظ و بررسی کنید.
7. منبع حقیقت داده را مشخص کنید؛ C# ثابت، کاتالوگ JS و localStorage به معنی دیتابیس، سفارش یا ورود سروری نیستند.
8. فقط بررسی مرتبط را اجرا کنید؛ برای C#/Razor در صورت اقتضا `dotnet build Shop.sln` و برای UI بازبینی رفتاری/بصری لازم است. solution فعلی test project مستقل ندارد؛ تست اجرا‌نشده را موفق اعلام نکنید.
9. راهنمای مسئول را همراه تغییر route، asset، داده یا رفتار به‌روز کنید. طبق [قانون مستندسازی](development/documentation.md)، راهنمای هر بخش باید در پوشهٔ خودش ایجاد شود؛ فهرست بخش و لینک‌های جابه‌جاشده را هم اصلاح کنید.
10. برای تغییر مستندات، `node docs/development/check-docs.mjs` و `git diff --check` را اجرا کنید؛ نتیجه و محدودیت بررسی را صریح گزارش دهید.

## محدوده و منابع

- `Shop/` تنها پروژهٔ اجرایی فعلی `Shop.sln` است. `ClockTemplate/` را بدون تأیید source و عضویت solution برنامهٔ اجرایی فرض نکنید.
- فایل generated، vendor یا ظاهراً بی‌مصرف را صرفاً برای cleanup حذف نکنید؛ ارجاع و runtime را اول بررسی کنید.
- جزئیات قابلیت در راهنمای حوزهٔ خودش، قرارداد مشترک در مرجع واحد و کارهای آینده در [roadmap](development/roadmap.md) ثبت می‌شوند.
- در اختلاف سند و کد، پس از بررسی کد/runtime، سند مسئول را اصلاح کنید؛ به راهنمای منسوخ استناد قطعی نکنید.
