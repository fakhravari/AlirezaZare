# مجله، برند و صفحه‌های خدماتی

## نقشهٔ مسئولیت

| حوزه | منبع داده و صفحه | نکتهٔ تغییر |
|---|---|---|
| مجله | [MagazineController](../../Shop/Controllers/MagazineController.cs)، [Views/Magazine](../../Shop/Views/Magazine)، `JournalArticles.cs` و `JournalArticles.Expanded.cs` | مقاله و آمار ثابت؛ `magazine.js` فیلتر/صفحه‌بندی و URL را مدیریت می‌کند |
| جزئیات مقاله | `Magazine/Details.cshtml`، `article-details.js` و `article-details.css` | TOC، progress، print، share و فاصلهٔ sticky header |
| برند | [BrandsController](../../Shop/Controllers/BrandsController.cs)، [BrandProfileModel.cs](../../Shop/Models/BrandProfileModel.cs)، [Views/Brands](../../Shop/Views/Brands) | پایه در BrandCatalog؛ تحریریه در `brand-editorial.json`؛ شمارش محصول با `brand-product-count.js` |
| FAQ | `FaqController`، [Faq/Index.cshtml](../../Shop/Views/Faq/Index.cshtml)، `FaqCatalog` | متن صفحه و schema باید از یک کاتالوگ بیایند |
| تماس | [Contact/Index.cshtml](../../Shop/Views/Contact/Index.cshtml)، `contact.js` و dialog تأیید | feedback فرم نمونه است؛ endpoint ارسال واقعی ثبت نشده |
| معرفی / ارسال / ضمانت / پیگیری | Controller و View هم‌نام | `pages.css` و فایل صفحه؛ پیگیری فعلی نتیجهٔ واقعی سفارش دریافت نمی‌کند |
| قوانین | [LegalController](../../Shop/Controllers/LegalController.cs) و [Views/Legal](../../Shop/Views/Legal) | سند حریم خصوصی و terms/returns ریشه‌ها و selectorهای اختصاصی دارند |
| خطا | [ErrorController](../../Shop/Controllers/ErrorController.cs) و [Error/Index.cshtml](../../Shop/Views/Error/Index.cshtml) | پوستهٔ عمومی همچنان اجرا می‌شود |

مسیر دقیق route، View، CSS و JS در [نقشهٔ صفحه‌ها](../architecture/routing.md) ثبت شده است؛ اینجا آن جدول را کپی نکنید.

## برند و محتوای دو منبعی

BrandsController اطلاعات پایه را از `BrandCatalog` می‌گیرد و متن تحریریه را با slug از [brand-editorial.json](../../Shop/wwwroot/shop/js/store/brand-editorial.json) اضافه می‌کند. مسیر خواندن فعلی در Controller از رشتهٔ `Shop` با S بزرگ استفاده می‌کند، در حالی که پوشهٔ واقعی `shop` است؛ هنگام انتقال به فایل‌سیستم حساس به حروف، این اختلاف باید اصلاح و بررسی شود. لوگو در `content/brands/` است و قرارداد کشور/رنگ/محصول در [کاتالوگ](../store/catalog.md) قرار دارد.

## هویت و تماس فعلی

| مورد | وضعیت در کد |
|---|---|
| نشان تصویری هدر | «برق شیراز» در `content/identity/store-electrical-lockup.svg` |
| نام فروشگاه در متن/SEO | «ندا اولیائی» هنوز در Controllerها، footer و متن‌های دیگر مصرف می‌شود |
| تلفن | `03969455457`، تکرارشده در Header، Footer، Contact و مسیرهای خدماتی |
| ایمیل | `neda.oliyai67@gmail.com` در Contact |

هویت فعلی یک تنظیم مرکزی واحد نیست؛ نشان هدر و نام متن/SEO دو بیان متفاوت دارند. پیش از تغییر نام، شماره، ایمیل یا لوگو، مصرف‌ها را در کل Shop پیدا کنید. `tel:`، `mailto:` و `wa.me` را جدا بررسی کنید؛ شمارهٔ واتساپ نیاز به قالب بین‌المللی درست دارد. اختلاف فعلی را با اطلاعات ساختگی پنهان نکنید.

## بازبینی محتوا

مقاله را با فهرست/جزئیات، فیلتر و back/forward، print و reduced-motion بررسی کنید. FAQ باید متن و JSON-LD یکسان داشته باشد؛ [راهنمای SEO](../seo/metadata.md) مرجع metadata و sitemap است. متن حقوقی، تماس و خدمات را با viewport موبایل، عنوان بلند، keyboard و پیوندهای مرتبط بررسی کنید. تصویر مختص محتوا طبق [رسانه](../media/images.md) به content تعلق دارد؛ تصویر تزئینی ثابت قالب در shop/images باقی می‌ماند.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۲۱ اکتبر ۲۰۲۶.
