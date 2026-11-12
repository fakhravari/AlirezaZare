# تصویرها و رسانه‌های فروشگاه

assetهای ثابت رابط در `Shop/wwwroot/` و محتوای بارگذاری‌شده از مسیرهای مدیریت رسانه/CDN تأمین می‌شوند. uploadهای پنل از `Admin/Controllers/Api/StoreMediaUploadApiController.cs` و تصاویر محصول از `StoreProductImageApiController.cs` عبور می‌کنند. برای هر نوع فایل، قبل از تغییر storage path تنظیمات محیط و API را بررسی کنید.

## قرارداد استفاده

- مسیر ذخیره‌شده را از URL نهایی CDN تفکیک کنید.
- image اصلی، کارت و thumbnail نقش/اندازهٔ جدا دارند؛ فایل موجود را با crop نامتناسب جایگزین نکنید.
- filename/path را normalize و ورودی upload را سمت سرور اعتبارسنجی کنید.
- alt را بر اساس کارکرد تصویر بنویسید؛ تصویر تزئینی باید از دسترس‌پذیری مخفی بماند.
- از ابعاد واقعی asset و `srcset`/lazy loading فعلی پیروی کنید؛ اندازهٔ ثابت را بدون بررسی renderer تعمیم ندهید.

مصرف محصول در `Shop/Views/Product/Details.cshtml` و کارت مشترک؛ هویت/لوگو و نام‌گذاری در Viewها و CDN filter پیگیری می‌شوند. تغییر ظاهر در owner مربوط و `tools/ui-owners.json` ثبت است. بررسی: تصویر غایب، مسیر CDN، نسبت تصویر، موبایل و دسترس‌پذیری.

[محتوا](../content/README.md) · [فهرست docs](../README.md)

بازبینی pipeline: ۱۲ نوامبر ۲۰۲۶.
