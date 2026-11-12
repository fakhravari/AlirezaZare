# طرح‌های فروش، offer و quote

قرارداد API در `Shop/Controllers/SalesPlansController.cs` است؛ منبع runtime از `ISalesPlanSource`/`DatabaseSalesPlanSource` و repositoryهای `Data/` تأمین می‌شود. صفحهٔ محصول، کارت‌ها و checkout مصرف‌کننده‌اند. تغییرات مدیریتی را در endpointهای Admin مربوط به کاتالوگ/قیمت دنبال کنید.

## API جاری

- `GET /api/store/sales-plans`: فهرست طرح‌ها و offerها.
- `GET /api/store/sales-plans/products/{productId}`: طرح‌ها، offer، variantها و price options کالا.
- `GET /api/store/sales-plans/products/{productId}/price-history?salesPlanId=...&optionPriceVersionIds=...`: نسخه‌های قیمت برای scopeهای انتخاب‌شده.
- `POST /api/store/sales-plans/quote`: محاسبهٔ خط با انتخاب‌ها.
- `POST /api/store/sales-plans/validate-cart`: محاسبهٔ مجدد اقلام cart.

طرح فروش مشخص می‌کند کدام offer فعال و قابل خرید است. quote سمت سرور منبع مبلغ/شرایط خرید است؛ snapshot cart فقط برای نمایش و ارسال انتخاب است. طرح غیرفعال، خارج از بازه یا offer نامعتبر نباید به طرح دیگری fallback شود.

## قیمت و تاریخچه

نسخه‌های قیمت در `Store.PriceVersion` نگهداری می‌شوند. برای هر `ScopeKey` نسخهٔ فعال آخرین قیمت جاری است و نسخه‌های قبلی با زمان UTC، مبلغ، تخفیف، دلیل/عامل تغییر و پیوند نسخهٔ قبلی باقی می‌مانند. انواع scope در `Data/Domain/Store/Entities/StorePriceVersion.cs` تعریف شده‌اند. API تاریخچه در [راهنمای محصول](product.md) تشریح شده است.

در هر ردیف سفارش، `StoreOrderItemPriceReference` شناسهٔ هر نسخهٔ قیمت اعمال‌شده را وصل می‌کند؛ با این رابطه می‌توان مبلغ پایه، variant، option و accessoryهای قیمت‌دار را برای مغایرت‌گیری بازسازی کرد. تاریخچهٔ قبل از شروع versioning با snapshot اولیه قابل بازیابی نیست.

## هنگام تغییر قرارداد

قیمت/موجودی را در UI دوباره محاسبه نکنید؛ quote را از API بگیرید. شناسهٔ پایدار plan، product، variant و price version را حفظ کنید. مبلغ در قرارداد جاری تومان صحیح و ارز `IRT` است؛ واحد و گردکردن را در DTO و تست قرارداد بررسی کنید.

مرجع جریان خرید: [سبد](cart.md)، مرجع schema: [پایگاه داده](../architecture/database.md)، مرجع داده‌های runtime: [منابع داده](../architecture/data-sources.md).

بررسی: plan نامعتبر/منقضی، چند offer برای یک کالا، variant/option قیمت‌دار، تغییر قیمت بین مشاهده و checkout، و نگهداری ID قیمت در order item.

[فروشگاه](README.md) · [فهرست docs](../README.md)

بازبینی قرارداد جاری: ۱۲ نوامبر ۲۰۲۶.
