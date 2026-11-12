# راهنمای پروژه و نقطهٔ شروع

آخرین بازبینی مستندات با کد: **۱۲ نوامبر ۲۰۲۶**. کد و رفتار runtime مرجع‌اند؛ این تاریخ یعنی مسیرهای زیر در آن روز بررسی شده‌اند، نه اینکه هر وضعیت محیطی یا دادهٔ زنده تضمین شده باشد.

## اگر تازه شروع کرده‌اید یا از ادامهٔ کار جا مانده‌اید

۱. `git status --short` و `git diff` را ببینید تا تغییرهای موجود گم نشوند.
۲. [قواعد مخزن](../AGENTS.md) و [روند کار](AGENTS.md) را بخوانید.
۳. [نمای معماری](architecture/overview.md) را برای مرز Shop/Admin/Data مرور کنید.
۴. route را در [نقشهٔ صفحه‌ها](architecture/routing.md) بیابید و زنجیرهٔ Controller → منبع داده → View → asset را در کد دنبال کنید.
۵. سند حوزه را از جدول زیر انتخاب کنید؛ اگر UI است، مالکیت فایل را در `tools/ui-owners.json` بررسی کنید.
۶. پس از تغییر، سند مسئول را به‌روز کنید و بررسی‌های متناسب را از [راهنمای کیفیت](development/quality.md) اجرا کنید.

## نقشهٔ مستندات

| بخش | برای چه کاری | شروع |
|---|---|---|
| معماری | مرز پروژه، route و داده | [نمای کلی](architecture/README.md) |
| رابط کاربری | layout، CSS، JavaScript و بررسی UI | [راهنمای UI](frontend/README.md) |
| فروشگاه | کالا، قیمت، سبد و پرداخت | [راهنمای فروشگاه](store/README.md) |
| حساب | ورود، پروفایل و داده‌های حساب | [راهنمای حساب](account/README.md) |
| مدیریت | پنل فروشگاه و انبار | [راهنمای مدیریت](admin/README.md) |
| محتوا | صفحات ثابت و مقاله | [راهنمای محتوا](content/README.md) |
| رسانه | فایل‌های تصویری و رسانه‌ای | [راهنمای رسانه](media/README.md) |
| SEO | metadata و قابلیت crawl | [راهنمای SEO](seo/README.md) |
| توسعه | اجرا، فرایند، کیفیت و مستندات | [راهنمای توسعه](development/README.md) |

## مسیرهای پرتکرار

| کار | راهنما |
|---|---|
| یافتن route، Controller و View | [نقشهٔ صفحه‌ها](architecture/routing.md) |
| پیگیری داده از مرورگر تا DB/API | [منابع داده](architecture/data-sources.md) و [دیتابیس](architecture/database.md) |
| تغییر قیمت و تاریخچهٔ قیمت | [محصول](store/product.md) و [طرح‌های فروش](store/sales-plans.md) |
| سبد و مرحلهٔ سفارش | [سبد](store/cart.md) و [پرداخت](store/payment.md) |
| ترتیب و مالک CSS/JS | [پوسته](frontend/layout.md)، [CSS](frontend/styling.md)، [JavaScript](frontend/javascript.md) |
| راه‌اندازی و بررسی | [setup](development/setup.md)، [کیفیت](development/quality.md) |

## قواعد مرجع

- هر موضوع یک سند مرجع دارد؛ indexها فقط مسیر می‌دهند.
- دادهٔ نمونه را با قابلیت عملیاتی اشتباه نگیرید. پیکربندی/محیط و اتصال زنده را پیش از ادعا بررسی کنید.
- راهنماها در پوشهٔ مسئولیت خود قرار می‌گیرند؛ برای جزئیات [قانون مستندسازی](development/documentation.md) را ببینید.
- اعتبارسنجی مستندات: `node docs/development/check-docs.mjs` و `git diff --check`.
