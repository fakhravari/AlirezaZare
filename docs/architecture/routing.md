# نقشهٔ routeها و صفحه‌ها

این نقشه برای پیدا کردن نقطهٔ ورود است. مسیر قطعی را در attributeهای Controller و `Program.cs` بررسی کنید؛ صفحه‌ها و static assets ممکن است تغییر کنند. ترتیب مشترک assetها در [frontend/layout](../frontend/layout.md) است.

## فروشگاه عمومی

| حوزه | نقطهٔ ورود |
|---|---|
| خانه | `Shop/Controllers/HomeController.cs` → `Shop/Views/Home/` |
| جست‌وجو و دیتیل | `ProductController.cs` → `Views/Product/`; مسیر دیتیل `/products/{slug}` و جست‌وجو `/Search` |
| سبد و checkout | `CartController.cs`, `CheckoutController.cs` → `Views/Cart/`, `Views/Checkout/` |
| حساب | `AccountController.cs`, `ProfileController.cs` → `Views/Profile/` و endpointهای `/api/account` |
| پرداخت | `PaymentController.cs` برای `/api/payment`; صفحهٔ موسسات در `PaymentInstitutionsController.cs` |
| محتوا | `MagazineController`, `BrandsController`, `FaqController`, `PageController` و controllerهای درباره/قانون/ارسال |
| مقایسه و علاقه‌مندی | `CompareController.cs`, `FavoritesController.cs` و Viewهای متناظر |
| خطا | `ErrorController.cs` و `Views/Error/` |

## APIهای مهم

فروشگاه APIهای account، sales plan، payment، feedback محصول، جغرافیا و warehouse دارد. route attribute و Authorization را مستقیماً از Controller بخوانید؛ `routing` سند فهرست همهٔ قراردادهای payload نیست. `SalesPlansController` در `/api/store/sales-plans` و تاریخچه/quote را ارائه می‌کند.

## پنل مدیریت

پنل MVC در `Admin/` از Areas استفاده می‌کند. Views و routeها را زیر `Admin/Areas/**/Controllers/` و `Views/` بیابید؛ APIهای JSON عمدتاً در `Admin/Controllers/Api/` هستند. برای یافتن صفحه از منو/Controller و سپس manifest مالکیت UI استفاده کنید. فهرست حوزه‌ها در [admin README](../admin/README.md) است.

## Asset و UI

CSS/JS مستقیم صفحه ممکن است از Razor sectionها بار شود یا از layout مشترک بیاید. فایل موجود در `wwwroot` به معنی load نیست: View و layout را دنبال کنید و `tools/ui-owners.json` را برای مالک ببینید.

[منابع داده](data-sources.md) · [نمای معماری](overview.md) · [فهرست docs](../README.md)

مرور مسیرها: ۱۲ نوامبر ۲۰۲۶.
