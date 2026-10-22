# صفحهٔ جزئیات محصول

## مسیر اجرا

[ProductController.Details](../../Shop/Controllers/ProductController.cs) → [CatalogRepository](../../Shop/Services/CatalogRepository.cs) → `PageModel<ProductDetailsModel>` → [Details.cshtml](../../Shop/Views/Product/Details.cshtml) → `commerce.js` و `#productView`.

Controller شناسه را از انتهای slug می‌خواند و محصول را از کاتالوگ پیدا می‌کند. canonical از نام انگلیسی محصول و id ساخته می‌شود. مسیرهای قدیمی ابتدا به `/products/product-{id}` هدایت می‌شوند؛ Controller فعلی پیشوند slug ورودی را با canonical تطبیق اجباری نمی‌دهد. نامعتبر بودن id یا نبود محصول، 404 می‌دهد.

## مالکیت markup و asset

- ریشهٔ View، `main.product-detail-page` است؛ hero و breadcrumb در Razor قرار دارند. محتوای فروشگاهی در `section.flow-main.container > #productView` با JavaScript ساخته می‌شود. به‌دنبال `main.flow-main` برای این صفحه نگردید.
- در section `Styles`: `pages.css`، `product-detail.css` و `form-confirm-dialog.css`.
- در section `PageStyles`: `product-detail-pricing.css` و `product-detail-options.css`.
- scriptهای مستقیم View: `form-confirm-dialog.js` و `page-content.js`. renderer اصلی `store/commerce.js` از layout می‌آید؛ catalog و countries نیز منابع سراسری‌اند.
- `_RelatedLinks` و `_PurchaseBenefits` مصرف می‌شوند. ترتیب نهایی همهٔ منابع در [پوسته](../frontend/layout.md) است؛ stylesheet صفحه آخرین قانون نیست.

## مسیرهای تغییر

| نیاز | محل بررسی |
|---|---|
| عنوان سرور، canonical و JSON-LD | Controller، CatalogRepository و [SEO](../seo/metadata.md) |
| برند فارسی، کشور و breadcrumb دسته | کاتالوگ، HNCountries و markup renderer در commerce.js |
| عکس و ویدئو | `image/mainImage/thumbnailImage`، آرایهٔ `images` و `videos`؛ [رسانه](../media/images.md) |
| ویژگی‌ها، گزینه‌ها، متعلقات و قیمت | کاتالوگ و renderer؛ افزایش قیمت گزینه‌های swatch زیر رنگ به‌شکل برچسب ظریف دیده می‌شود و مبلغ نهایی طبق [سبد](cart.md) محاسبه می‌شود |
| تب معرفی، بررسی، مشخصات، دیدگاه و پرسش | commerce.js؛ متن اولیه از `detail` و دادهٔ محلی feedback؛ رویداد پایان اسکرول تب مقصد را پیش از رسیدن به بخش آزاد نمی‌کند |
| اشتراک و کپی | handlerهای صفحه و commerce/page-content؛ ثبت دوبارهٔ listener را کنترل کنید |
| افزودن به سبد | رویداد و گزینهٔ انتخابی → `HNUI.cart.add`؛ state موازی نسازید |

دیدگاه و پرسش با کلید `hn-product-${type}-${productId}` در مرورگر ذخیره می‌شوند و با دادهٔ اولیه ترکیب می‌شوند. این جریان endpoint ثبت عمومی یا moderation سمت سرور ندارد. قرارداد کلید و رویداد را در [JavaScript](../frontend/javascript.md) ببینید.

## بازبینی

ورود از لینک محصول در موبایل باید بالای صفحه را نشان دهد؛ focus برنامه‌ای دکمهٔ سبد را بدون درخواست کاربر فعال نکنید. متن فارسی بلند، مقدار ویژگی و عنوان را بدون بریدگی ناموجه بررسی کنید؛ justify نباید فاصلهٔ حروف/کلمات را بیش از حد بکشد.

گالری را با تصویر اصلی، thumbnail، ویدئو، جابه‌جایی رسانه، بستن/Escape و بازگشت focus بررسی کنید. وضعیت فعال thumbnail باید قابل تشخیص باشد. تب‌ها، باز/بسته‌شدن متن با reduced-motion، feedback و صفحه‌بندی را هم ببینید. در افزودن به سبد، پیش‌فرض‌ها، خدمت اجباری، متعلقهٔ اختیاری و تغییر تعداد باید در هدر، سبد و فاکتور نتیجهٔ یکسان داشته باشند.

محتوای اولیه، قیمت و موجودی نمونه‌اند. JSON-LD محصول فعلی اطلاعات پایه دارد و offer معتبر از backend ارائه نمی‌کند؛ برای کامل‌کردن ظاهر، دادهٔ فروش یا مشخصات مدل را حدس نزنید.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۲۲ اکتبر ۲۰۲۶.
