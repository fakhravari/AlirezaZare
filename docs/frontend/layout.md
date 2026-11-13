# پوستهٔ فروشگاه و ترتیب asset

فروشگاه از `Shop/Views/_ViewStart.cshtml` به `Shop/Views/Shared/_Shop.cshtml` می‌رسد. این layout شامل metadata، header، محتوای صفحه، footer، panelهای مشترک و assetهای عمومی است.

## مسیر رندر

`_Shop.cshtml` ابتدا styleهای پایه و مشترک را بار می‌کند، سپس sectionهای `Styles` و `PageStyles`، بعد CSSهای مشترک تکمیلی و sectionهای صفحه را می‌آورد. ترتیب دقیق را در خود فایل بررسی کنید؛ ترتیب cascade بخشی از رفتار است. در انتهای body داده‌های Catalog/Commerce، اسکریپت‌های حساب، storage و cart، UI core، کتابخانه‌ها و scriptهای فروشگاه اجرا می‌شوند؛ scriptهای اختصاصی View در section و content adaptation پس از آن قرار دارند.

header خانه و صفحهٔ داخلی از partial جدا استفاده می‌کنند. صفحه‌های محصول و حساب نیز markup/script خود را دارند؛ جزئیات مسیرها در [نقشهٔ route](../architecture/routing.md) آمده است. پوستهٔ Admin مستقل است: `Admin/Views/Shared/_Layout.cshtml`.

فرم خبرنامهٔ footer در `Shop/Views/Home/_Newsletter.cshtml` و handler آن در `Shop/wwwroot/shop/js/layout/main.js` است. handler به `POST /api/store/newsletter` می‌فرستد؛ سرور با نشست معتبر ترجیح حساب را فعال می‌کند و برای مهمان ایمیل را در جدول خبرنامه ثبت می‌کند. قرارداد داده در [راهنمای حساب](../account/profile.md) آمده است.

## تغییر پوسته

- منابع واقعی را در layout و View پیدا کنید؛ وجود فایل CSS/JS به معنی بارگذاری نیست.
- ترتیب section را حفظ و وابستگی به API/DOM قبلی را کنترل کنید.
- owner را در `tools/ui-owners.json` بررسی کنید.
- نتیجه را در home و صفحهٔ داخلی و viewportهای مربوط مرور کنید.

رفتار CSS در [راهنمای CSS](styling.md)، قرارداد JS در [راهنمای JavaScript](javascript.md) و روش بازبینی در [verification](verification.md) است.

[فهرست UI](README.md) · [فهرست docs](../README.md)

بازبینی layout: ۱۴ نوامبر ۲۰۲۶.
