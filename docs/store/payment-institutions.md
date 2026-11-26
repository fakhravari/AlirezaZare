# فهرست موسسات پرداخت

صفحهٔ عمومی با `PaymentInstitutionsController` قرارداد `IStorePublicPageData` را مصرف می‌کند. قرارداد در `Data/Application/Store/Contracts/IStorePublicPageData.cs` و query پایگاه داده در `Data/Infrastructure/Persistence/Repositories/StorePublicPageData.cs` است؛ implementation نام و مسیر لوگوی بانک‌های فعال را برمی‌گرداند. این فهرست معرفی محتواست؛ وجود ردیف یا لوگو به معنی اتصال درگاه نیست. وضعیت پرداخت عملیاتی در [پرداخت](payment.md) آمده است.

تغییر schema/رکورد را از entity `StoreBank`، نگاشت context و migration مربوط دنبال کنید. برای افزودن یا غیرفعال‌کردن موسسه شناسهٔ پایدار و ارجاع سفارش را حفظ کنید؛ حذف را بدون بررسی FKها انجام ندهید. رسانهٔ logo باید از pipeline محتوا/CDN پیروی کند: [راهنمای رسانه](../media/images.md).

View عمومی: `Shop/Views/PaymentInstitutions/Index.cshtml`. بررسی کنید فقط موارد فعال رندر شوند، مسیر لوگوی غایب graceful باشد و هیچ اطلاعات محرمانهٔ پرداخت نمایش داده نشود.

[فروشگاه](README.md) · [فهرست docs](../README.md)

بازبینی: ۱۲ نوامبر ۲۰۲۶.
