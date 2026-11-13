# نقشهٔ routeها و صفحه‌ها

این نقشه برای پیدا کردن نقطهٔ ورود است. مسیر قطعی را در attributeهای Controller و `Program.cs` بررسی کنید؛ صفحه‌ها و static assets ممکن است تغییر کنند. ترتیب مشترک assetها در [frontend/layout](../frontend/layout.md) است.

## فروشگاه عمومی

| حوزه | نقطهٔ ورود |
|---|---|
| خانه | `Shop/Controllers/HomeController.cs` → `Shop/Views/Home/` |
| جست‌وجو و دیتیل | `ProductController.cs` → `Views/Product/`; مسیر دیتیل `/products/{slug}` و جست‌وجو `/Search` |
| سبد و checkout | `CartController.cs`, `CheckoutController.cs` → `Views/Cart/`, `Views/Checkout/` |
| حساب | `AccountController.cs`, `ProfileController.cs` → `Views/Profile/` و endpointهای `/api/account` |
| پیگیری سفارش | `/Tracking` → `TrackingController.cs` → `POST /api/store/orders/tracking` |
| پرداخت | `PaymentController.cs` برای `/api/payment`; صفحهٔ موسسات در `PaymentInstitutionsController.cs` |
| محتوا | `MagazineController`, `BrandsController`, `FaqController`, `PageController` و controllerهای درباره/قانون/ارسال |
| مقایسه و علاقه‌مندی | `CompareController.cs`, `FavoritesController.cs` و Viewهای متناظر |
| خطا | `ErrorController.cs` و `Views/Error/` |

## APIهای مهم

فروشگاه APIهای account، sales plan، payment، feedback محصول، جغرافیا و warehouse دارد. route attribute و Authorization را مستقیماً از Controller بخوانید؛ `routing` سند فهرست همهٔ قراردادهای payload نیست. `SalesPlansController` در `/api/store/sales-plans` و تاریخچه/quote را ارائه می‌کند.

فرم تماس و خبرنامهٔ خانه به‌ترتیب از `POST /api/store/contact` و `POST /api/store/newsletter` استفاده می‌کنند. ورود با کد ایمیل در `/api/auth/email-code` است. مسیرهای عمومی محدودسازی درخواست و اعتبارسنجی سمت سرور دارند؛ قرارداد داده را در راهنمای حوزه بخوانید.

## پنل مدیریت

پنل MVC در `Admin/` از Areas استفاده می‌کند. Views و routeها را زیر `Admin/Areas/**/Controllers/` و `Views/` بیابید؛ APIهای JSON عمدتاً در `Admin/Controllers/Api/` هستند. برای یافتن صفحه از منو/Controller و سپس manifest مالکیت UI استفاده کنید. فهرست حوزه‌ها در [admin README](../admin/README.md) است.

مدیریت پیام‌های فرم تماس از `Store/Features?feature=contact-messages` و APIهای `/api/admin/store/contact-messages` عبور می‌کند؛ مجوزها به صفحهٔ `Store:Features:Index:contact-messages` و endpointهای همان API متصل‌اند.

ارسال پیام مدیر به حساب مشتری در `Store/Features?feature=account-messages` و API `/api/admin/store/account-messages` انجام می‌شود. مالک پیام از هویت نشست مشتری در `/api/account/messages` تعیین می‌شود و مشتری در `/Profile/Messages` پیام‌های خودش را می‌بیند.

## Asset و UI

CSS/JS مستقیم صفحه ممکن است از Razor sectionها بار شود یا از layout مشترک بیاید. فایل موجود در `wwwroot` به معنی load نیست: View و layout را دنبال کنید و `tools/ui-owners.json` را برای مالک ببینید.

[منابع داده](data-sources.md) · [نمای معماری](overview.md) · [فهرست docs](../README.md)

مرور مسیرها: ۱۴ نوامبر ۲۰۲۶.
