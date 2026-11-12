# سبد، پیش‌فاکتور و سفارش

صفحه‌های سبد و Checkout به‌ترتیب در `Shop/Controllers/CartController.cs` و `CheckoutController.cs` رندر می‌شوند. رفتار سبد مرورگر در `Shop/wwwroot/shop/js/store/cart.js` و هماهنگی commerce در `js/store/commerce.js` است. این state مرورگر برای نمایش/تعامل است؛ قیمت نهایی، موجودی و سفارش باید با API سرور اعتبارسنجی شوند.

## مسیر جریان

کارت کالا → سبد → quote/validate طرح فروش → احراز نشست و نشانی حساب → ثبت سفارش/ردیف‌ها در backend. جدول `Account.CartItem` شناسهٔ محصول، variant/طرح و تعداد را نگه می‌دارد؛ انتخاب مقدارهای purchase option و accessory در جدول‌های فرزند و با ID مرجع ذخیره می‌شوند. مهاجرت ۰۶۸ رابطه‌ها را از JSON قدیمی backfill می‌کند و JSON را نگه می‌دارد تا snapshot قبلی حذف نشود؛ سفارش نیز snapshot مستقل انتخاب‌ها را دارد. سرور باید مبلغ‌ها و تعلق IDها را از منبع معتبر دوباره اعتبارسنجی کند. مرجع قیمت و optionها [طرح فروش](sales-plans.md) و مرجع ورود/نشانی [حساب](../account/profile.md) است.

## نکات تغییر

- کلید خط باید محصول، تنوع و طرح انتخابی را از هم جدا کند.
- به snapshot قیمت مرورگر اعتماد نکنید؛ اختلاف quote باید پیش از پرداخت به کاربر اعلام شود.
- state مهمان/کاربر و مهاجرت دادهٔ قدیمی را در `storage.js` و API بررسی کنید.
- order detail باید ارتباط نسخهٔ قیمت را حفظ کند تا مغایرت‌گیری ممکن باشد؛ قبل از تغییر schema، entity و migration را در [database](../architecture/database.md) دنبال کنید.

صفحهٔ سبد و checkout noindex هستند. بررسی: مهمان و واردشده، cart خالی/پر، خطای quote، تغییر قیمت و ارسال تکراری.

[فروشگاه](README.md) · [پرداخت](payment.md) · [فهرست docs](../README.md)

بازبینی مسیرها: ۱۲ نوامبر ۲۰۲۶.
