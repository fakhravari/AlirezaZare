# کاتالوگ، جست‌وجو و فهرست کالا

ورود صفحهٔ جست‌وجو `GET /Search` در `Shop/Controllers/ProductController.cs` است. دادهٔ کاتالوگ از `ICatalogSource` و پیاده‌سازی `CatalogRepository` می‌آید؛ Viewهای فهرست در `Shop/Views/Product/` و رفتار سمت مرورگر در `Shop/wwwroot/shop/js/pages/search.js` و `js/store/catalog.js` است.

## مسیر ردیابی

Controller → `ICatalogSource`/repository → projection کالا/دسته/برند → View/partial → scriptهای مشترک کاتالوگ و کارت. برای تغییر taxonomy یا فیلتر، route و querystring را با `search.js` و لینک‌های header/breadcrumb هماهنگ کنید.

کارت محصول مشترک را در [راهنمای محصول](product.md) ببینید؛ مالکیت فایل را در `tools/ui-owners.json` بیابید. طرح فروش می‌تواند offer و قیمت جدا داشته باشد؛ مرجع آن [sales-plans](sales-plans.md) است.

## بررسی تغییر

حالت بدون نتیجه، فیلتر ترکیبی، صفحه‌بندی، لینک مستقیم با querystring و عرض موبایل را مرور کنید. منبع نهایی قیمت/موجودی را در API/server تأیید کنید؛ نمایش کارت snapshot محسوب می‌شود.

[فروشگاه](README.md) · [مسیرها](../architecture/routing.md) · [فهرست docs](../README.md)

بازبینی کد: ۱۲ نوامبر ۲۰۲۶.
