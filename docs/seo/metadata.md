# Metadata، canonical و crawl

مدل SEO در `Shop/Models/SeoModel.cs` و options در `SeoMetadataOptions.cs`؛ خروجی مشترک در `Shop/Views/Shared/_SeoMetadata.cshtml` است. `Shop/Controllers/SeoController.cs` مسئول endpointهای crawl از جمله sitemap/robots است. صفحه‌ها metadata را هنگام ساخت ViewModel تنظیم می‌کنند.

## هنگام تغییر

- عنوان، توضیح، canonical، robots، Open Graph و دادهٔ ساختاریافته را با محتوای واقعی صفحه تطبیق دهید.
- URL مطلق را از تنظیمات معتبر بسازید؛ host درخواست را برای canonical production بی‌قید اعتماد نکنید.
- صفحه‌های حساب، cart و checkout باید سیاست indexability متناسب داشته باشند.
- sitemap فقط URLهای canonical و قابل دسترس را منتشر کند؛ صفحهٔ خطا یا محتوای unpublished نباید فهرست شود.

برای routeها از [معماری](../architecture/routing.md) و برای متن/تصویر از [محتوا](../content/pages.md) و [رسانه](../media/images.md) کمک بگیرید. بررسی: source HTML، status code، canonical، robots و XML sitemap؛ فقط اجرای build، crawl را تأیید نمی‌کند.

[SEO](README.md) · [فهرست docs](../README.md)

بازبینی کد: ۱۲ نوامبر ۲۰۲۶.
