# جزئیات محصول و تاریخچهٔ قیمت

صفحه از `Shop/Controllers/ProductController.cs` به `Shop/Views/Product/Details.cshtml` می‌رود. دادهٔ کاتالوگ از `ICatalogSource` و گزینه‌ها/طرح‌ها از `ISalesPlanSource` می‌آیند. منطق تعاملی دیتیل در `Shop/wwwroot/shop/js/store/commerce.js` است؛ مالک‌های CSS و JS را در `tools/ui-owners.json` بیابید.

## تغییر ویژگی صفحه

markup و ViewModel را با projection سرور هماهنگ کنید؛ انتخاب variant و option باید با quote سرور هم‌خوان باشد. تاریخچهٔ قیمت مرجعش [Store.PriceVersion](../../Data/Domain/Store/Entities/StorePriceVersion.cs) است و endpoint آن در `SalesPlansController.ProductPriceHistory` قرار دارد. درخواست `GET /api/store/sales-plans/products/{productId}/price-history` با `salesPlanId` و شناسهٔ نسخه‌های option انتخاب‌شده، رویدادهای قیمت پایه و optionهای معتبر را برمی‌گرداند. هر نسخه شامل ID، scope، مبلغ، تخفیف، وضعیت فعال و زمان UTC است.

هر scope یک نسخهٔ فعال جاری دارد؛ نسخه‌های قبلی برای تاریخچه حفظ می‌شوند. order item به نسخه‌های اعمال‌شده با `StoreOrderItemPriceReference` متصل است تا بررسی مبلغ سفارش ممکن باشد. جدول جداگانهٔ تاریخچهٔ محصول در این قرارداد مرجع نیست.

نسخه‌بندی از شروع ثبت داده‌ها تاریخچه می‌سازد؛ migration آغازین snapshot زمان نصب را ثبت می‌کند و سابقهٔ پیش از آن قابل بازسازی نیست. بخش‌هایی که رکورد تاریخی ندارند نباید نمودار ساختگی نشان دهند. مرجع جزئیات طرح و quote: [sales-plans](sales-plans.md).

## سایر stateها

پیگیری قیمت در account API ذخیره/بازیابی و در مرورگر با کلید قدیمی `hn-price-watches-demo` برای سازگاری و انتقال مدیریت می‌شود؛ این کلید را تاریخچهٔ قیمت تلقی نکنید. دیدگاه/پرسش از API بازخورد محصول عبور می‌کند؛ اگر View هنوز متن نمایشی دارد، آن را با دادهٔ server یکی نگیرید.

بررسی: ترکیب optionهای متفاوت، طرح نامعتبر/منقضی، دادهٔ تاریخچهٔ خالی، نسخهٔ فعال، IDهای نسخه برای مغایرت، خطای API و layout موبایل.

[فروشگاه](README.md) · [منابع داده](../architecture/data-sources.md) · [فهرست docs](../README.md)

بازبینی مسیر و قرارداد: ۱۲ نوامبر ۲۰۲۶.
