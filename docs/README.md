# راهنمای بررسی پروژه

این فهرست نقطهٔ شروع شناخت و تغییر پروژه است. مستندات بر اساس مسئولیت به هشت گروه تقسیم شده‌اند؛ راهنمای هر بخش در پوشهٔ خودش نگهداری می‌شود.

## مسیر سریع بررسی

1. [نمای کلی معماری](architecture/overview.md) را بخوانید؛ برنامهٔ اجرایی `Shop/` و الگوی فعلی MVC است.
2. صفحه را در [نقشهٔ route، View و asset](architecture/routing.md) پیدا کنید.
3. [منبع حقیقت داده](architecture/data-sources.md) را مشخص کنید؛ سپس راهنمای حوزهٔ مربوط را از جدول زیر باز کنید.
4. در تغییر UI، [پوسته و ترتیب منابع](frontend/layout.md) و مالک CSS/JS را بررسی کنید.
5. طبق [روند تغییر](development/workflow.md) اصلاح و [اعتبارسنجی مرتبط](development/setup.md) را انجام دهید.

## گروه‌های مستندات

| گروه | مسئولیت | راهنمای شروع |
|---|---|---|
| [معماری](architecture/README.md) | ساختار برنامه، مسیر صفحه‌ها و مرز داده | [نمای کلی](architecture/overview.md) |
| [رابط کاربری](frontend/README.md) | پوسته، طراحی، JS مشترک و بازبینی UI | [ترتیب منابع](frontend/layout.md) |
| [فروشگاه](store/README.md) | کاتالوگ، فیلتر، محصول، سبد و تسویه | [کاتالوگ و فهرست‌ها](store/catalog.md) |
| [حساب کاربری](account/README.md) | ثبت‌نام و تأیید ایمیل، پروفایل، آدرس و سفارش | [پروفایل](account/profile.md) |
| [محتوا و خدمات](content/README.md) | مجله، برند، FAQ، قوانین و هویت | [صفحه‌های محتوایی](content/pages.md) |
| [رسانه](media/README.md) | پوشه‌ها، نام‌گذاری، تصاویر، ویدئو و ابعاد | [راهنمای رسانه](media/images.md) |
| [SEO](seo/README.md) | metadata، canonical، schema و crawl | [metadata](seo/metadata.md) |
| [توسعه](development/README.md) | اجرا، workflow، مستندسازی و برنامهٔ آینده | [راه‌اندازی](development/setup.md) |

## برای یک تغییر مشخص

| نیاز | مرجع اصلی |
|---|---|
| رنگ، فاصله، responsive یا تداخل CSS | [مالکیت CSS](frontend/styling.md) و [عیب‌یابی](frontend/verification.md) |
| هدر، مگامنو، modal و ترتیب load | [پوسته](frontend/layout.md) |
| event، storage، HNUI یا مصرف endpoint تأیید ایمیل | [قرارداد JavaScript](frontend/javascript.md) |
| فیلتر رنگ، کشور، کارت و لیست محصول | [کاتالوگ](store/catalog.md) |
| گالری، ویژگی، پرسش و دیدگاه محصول | [جزئیات محصول](store/product.md) |
| افزودن به سبد، گزینه و مبلغ | [سبد و تسویه](store/cart.md) |
| تصویر اصلی/کارت/thumbnail، بنر و انتقال محتوا | [رسانه و ابعاد](media/images.md) |
| نوشتن یا جابه‌جایی راهنما | [قانون مستندسازی](development/documentation.md) و [الگو](development/guide-template.md) |
| مالکیت مؤلفه، تست خودکار و CI | [کنترل کیفیت و قراردادها](development/quality.md) |
| پیشرفت تفکیک مالکیت و فایل‌های UI | [برنامهٔ مالکیت UI](frontend/ui-ownership-roadmap.md) |

## قواعد اصلی

- **راهنمای هر بخش در پوشهٔ همان بخش ساخته و در README آن فهرست می‌شود.** ریشهٔ docs فقط فهرست و قواعد مشترک دارد؛ جزئیات در چند سند تکرار نمی‌شوند.
- [AGENTS.md ریشهٔ مخزن](../AGENTS.md) این قانون را برای تغییرات کل پروژه اعمال می‌کند؛ [روند عامل](AGENTS.md) مراحل کار را مشخص می‌کند.
- کد و runtime مرجع نهایی‌اند. مسیرها و ترتیب منابع snapshot هستند؛ پس از کشف اختلاف، راهنمای مسئول به‌روز شود.
- تأیید کد ایمیل ثبت‌نام از backend و SMTP انجام می‌شود؛ ایجاد حساب و session، محصولات، سبد، کوپن و پرداخت هنوز عمدتاً نمونه/محلی‌اند. وضعیت هر مرز در [منابع داده](architecture/data-sources.md) آمده است.
- `Shop/` برنامهٔ وب است؛ `tests/Shop.Contracts` قرارداد C# را بررسی می‌کند. تست‌های JS، HTTP و مرورگر و workflow CI در [کیفیت](development/quality.md) توضیح داده شده‌اند؛ build موفق جای تست رفتار و ظاهر نیست.

برای بررسی ساختار و لینک‌های مستندات از ریشهٔ مخزن اجرا کنید:

```powershell
node docs/development/check-docs.mjs
```

آخرین تطبیق با کد: ۳۱ اکتبر ۲۰۲۶.
