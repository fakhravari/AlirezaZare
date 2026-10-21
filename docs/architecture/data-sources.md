# منابع داده و مرزهای فعلی

هدف این صفحه تعیین منبع حقیقت است؛ ساختار فیلدهای محصول در [کاتالوگ](../store/catalog.md) و کلیدها/رویدادهای مرورگر در [JavaScript](../frontend/javascript.md) نگهداری می‌شوند.

| داده | منبع فعلی | مصرف و محدودیت |
|---|---|---|
| مسیر و مدل صفحه | [Controllers](../../Shop/Controllers) و [PageModel](../../Shop/Models/PageModel.cs) | آماده‌سازی Razor و SEO؛ سرویس دامنهٔ مستقل ندارد |
| محصولات، قیمت، رنگ و ویژگی‌ها | [catalog.js](../../Shop/wwwroot/shop/js/store/catalog.js) → `window.HNProducts` | کاتالوگ ۳۰ قلمی نمونه؛ قیمت و موجودی از پایگاه فروش دریافت نمی‌شوند |
| عنوان، slug و تصویر محصول در سرور | [ProductSeoCatalog.cs](../../Shop/Models/ProductSeoCatalog.cs) | متن همان catalog.js را می‌خواند؛ ارتباط به قالب آرایه‌های فایل وابسته است |
| کشور و پرچم | [countries.js](../../Shop/wwwroot/shop/js/store/countries.js) → `window.HNCountries` | نام‌های فارسی/انگلیسی و مسیر SVG؛ اتصال برند به کشور در catalog.js |
| اطلاعات پایهٔ برند | [BrandProfileModel.cs](../../Shop/Models/BrandProfileModel.cs)، `BrandCatalog` | Controller برند؛ متن تحریریه منبع جدا دارد |
| متن تحریریهٔ برند | [brand-editorial.json](../../Shop/wwwroot/shop/js/store/brand-editorial.json) | خوانده‌شدن توسط BrandsController؛ نام مسیر واقعی را در محیط حساس به بزرگی حروف هم بررسی کنید |
| مقاله‌ها و آمار مجله | [JournalArticles.cs](../../Shop/Models/JournalArticles.cs) و [JournalArticles.Expanded.cs](../../Shop/Models/JournalArticles.Expanded.cs) | دادهٔ ثابت؛ آمار نمایش‌داده‌شده عملیاتی یا خودکار نیست |
| FAQ | `FaqCatalog` در [PageModel.cs](../../Shop/Models/PageModel.cs) | منبع مشترک متن صفحه و FAQPage JSON-LD |
| سبد، ورود، مقایسه، علاقه‌مندی، آدرس و پروفایل | `HNUI`، script صفحه و `localStorage` | state مرورگر؛ سفارش، پرداخت یا هویت تأییدشدهٔ سرور نیست |
| رسانه‌ها | [ساختار content](../media/images.md) | مسیرها از catalog.js، مدل C#، Razor و CSS مصرف می‌شوند |

## هنگام تغییر داده

ابتدا مصرف‌کننده‌های سرور و مرورگر را پیدا کنید. افزودن فیلد به JS به‌تنهایی مدل C#، metadata یا sitemap را به‌روز نمی‌کند. تغییر ساختار آرایه‌های catalog.js ممکن است parser فعلی ProductSeoCatalog را بشکند؛ هر دو مسیر را بررسی کنید.

دادهٔ ذخیره‌شده در مرورگر را ورودی قابل‌اعتماد فرض نکنید. افزودن persistence واقعی نیاز به مدل دامنه، validation، هویت، مجوز و endpoint سرور دارد؛ برنامهٔ این کار در [رودمپ](../development/roadmap.md) آمده است.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۲۱ اکتبر ۲۰۲۶.
