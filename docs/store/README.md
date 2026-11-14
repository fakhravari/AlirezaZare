# فروشگاه و خرید

این بخش مالک قراردادهای تجاری سمت فروشگاه است. UI مشترک در [frontend](../frontend/README.md)، مدل‌های داده در [معماری](../architecture/README.md) و مدیریت در [admin](../admin/README.md) توضیح داده می‌شود.

| سند | مسیر شروع |
|---|---|
| [کاتالوگ](catalog.md) | `ProductController` → CatalogSource → Search |
| [جزئیات محصول](product.md) | قیمت، گزینه‌ها، رسانه و تاریخچه |
| [سبد و checkout](cart.md) | state مرورگر، quote و ثبت سفارش |
| [نمونهٔ گزارش T-SQL سبد](../t-sql/README.md) | نمایش سطری اقلام، ویژگی‌ها، متعلقات و محاسبهٔ قیمت |
| [پرداخت](payment.md) | PaymentController، سرویس کاربردی Data و اتصال gateway |
| [موسسات پرداخت](payment-institutions.md) | فهرست معرفی‌شده از StoreBank |
| [طرح‌های فروش](sales-plans.md) | offer، quote و تاریخچهٔ نسخه‌های قیمت |

دادهٔ سرور مرجع قیمت نهایی است؛ sample، cache مرورگر و نام درگاه را معادل تراکنش واقعی نگیرید. فهرست کلی: [docs](../README.md).
