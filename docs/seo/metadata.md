# SEO و metadata مشترک

این راهنما مسیر metadata صفحه‌ها و structured data را توضیح می‌دهد. پیش از تغییر، [منابع داده](../architecture/data-sources.md) و برای URLها [مسیرها](../architecture/routing.md) را ببینید.

## مسیر داده

```text
Controller / ساخت PageModel
  → SeoModel در ViewModel صفحه
  → Views/Shared/_SeoMetadata.cshtml
  → meta، canonical، Open Graph، Twitter و JSON-LD در <head>
```

صفحه باید metadata مختص خودش را در `SeoModel` فراهم کند؛ partial مشترک خروجی HTML را تولید می‌کند. تنظیمات سطح سایت از بخش پیکربندی `Seo` خوانده می‌شوند. فایل‌های اصلی:

| مسئولیت | مسیر |
|---|---|
| مدل metadata صفحه | `Shop/Models/PageModel.cs` (`SeoModel`) |
| پیش‌فرض و تنظیمات سراسری | `Shop/Models/SeoMetadataOptions.cs` |
| ساخت origin و URL مطلق | `Shop/Services/SeoUrlBuilder.cs` |
| ساخت meta و JSON-LD | `Shop/Views/Shared/_SeoMetadata.cshtml` |
| endpointهای crawl | `Shop/Controllers/SeoController.cs` |
| تعریف FAQ مشترک | `Shop/Models/PageModel.cs` (`FaqCatalog`) |

## فیلدهای صفحه

`SeoModel` شامل عنوان، توضیح، کلیدواژه، canonical، robots، تصویر، structured data، نوع Open Graph، متن جایگزین تصویر اجتماعی، بخش مقاله و breadcrumbهاست. پیش از افزودن فیلد جدید، بررسی کنید partial آن را مصرف کند و Controller مقدار معناداری بفرستد.

- عنوان و توضیح باید با محتوای قابل‌مشاهدهٔ همان صفحه منطبق باشند؛ برای پرکردن طول، متن تکراری نسازید.
- canonical باید به URL اصلی همان محتوا اشاره کند؛ صفحه‌های pagination/filter را بدون بررسی indexability canonical نکنید.
- robots را فقط برای نیاز واقعی صفحه تنظیم کنید؛ مقدار پیش‌فرض را بی‌دلیل override نکنید.
- Breadcrumbهای JSON-LD باید با مسیر breadcrumb قابل‌مشاهده در صفحه، از نظر نام و URL هماهنگ باشند.
- دادهٔ ساختاریافته فقط از محتوای قابل‌مشاهده و منبع معتبر استفاده کند. قیمت، موجودی، SKU، امتیاز و پیشنهاد فروش را از دادهٔ تأییدنشده حدس نزنید.

## الزامات اجرایی سئو

برای صفحه‌های قابل ایندکس، هنگام افزودن یا تغییر View و Controller این موارد را بررسی کنید:

- `<title>`، توضیح متا و یک عنوان اصلی `h1` باید مشخص، غیرخالی و با محتوای قابل‌مشاهده هماهنگ باشند؛ عنوان‌ها و توضیح‌های تکراری را برای پرکردن طول نسازید.
- ترتیب headingها باید معنایی باشد. برای تصویر محتوایی `alt` توصیفی بنویسید؛ تصویر تزئینی یا لوگوی تکراری کنار نام باید `alt=""` داشته باشد. `title` تصویر اختیاری است و نباید جای `alt` را بگیرد.
- تصویرهای محتوایی مهم باید ابعاد یا نسبت تصویر پایدار داشته باشند؛ برای تصویرهای پایین صفحه `loading="lazy"` و برای تصویر شاخص بالای صفحه اولویت بارگذاری متناسب تنظیم شود.
- صفحه باید `lang`، viewport، canonical یکتا، robots، Open Graph و کارت X داشته باشد. پارامترهای فیلتر و جستجو را فقط وقتی canonical مستقل کنید که صفحهٔ مقصد محتوای مستقل و قابل ایندکس داشته باشد.
- لینک داخلی باید مقصد معنادار و متن توصیفی داشته باشد. لینک خارجی و دادهٔ بک‌لینک به منبع و سیاست مربوط نیاز دارد و از روی HTML همان صفحه قابل ارزیابی کامل نیست.
- JSON-LD را از همان داده‌ای بسازید که کاربر می‌بیند. برای محصول، `name`، تصویر، برند، دسته و SKU معتبر را بیاورید؛ `Offer`، قیمت، ارز، موجودی، امتیاز و review را فقط پس از اتصال به دادهٔ تأییدشده و همسان با صفحه اضافه کنید.
- بعد از تغییر route یا محتوای قابل ایندکس، `robots.txt`، sitemap و پاسخ‌های HTTP آن مسیر را نیز بررسی کنید. سرعت و Core Web Vitals باید با سنجش اجراشده ارزیابی شوند؛ وجود تگ به‌تنهایی تأیید عملکرد نیست.

در این پروژه صفحهٔ جستجو فیلترهای مرورگری دارد و queryهای ترکیبی را به canonical `/search` برمی‌گرداند؛ تا وقتی صفحه‌های دسته یا برند محتوای مستقل سمت سرور ندارند، این queryها را به‌عنوان landing page جداگانه وارد sitemap نکنید. بیرون از HTML، بک‌لینک‌ها، دامنه‌های ارجاع‌دهنده، منشن برند و اعتبار دامنه نیازمند دادهٔ Search Console یا ابزار مستقل‌اند.

## Structured data و FAQ

`_SeoMetadata.cshtml` گراف JSON-LD مشترک شامل داده‌های سازمان/فروشگاه، وب‌سایت و صفحه را می‌سازد و در صورت وجود دادهٔ معتبر، structured data صفحه و breadcrumb را به آن اضافه می‌کند. پیش از تغییر schema، مدل، partial و Controller صفحه را با هم دنبال کنید تا منبع دادهٔ واحد حفظ شود.

صفحهٔ FAQ از `FaqCatalog` برای متن پرسش‌وپاسخ قابل‌مشاهده و `FAQPage` JSON-LD استفاده می‌کند. پرسش یا پاسخ را در همان کاتالوگ ویرایش کنید؛ markup موازی در View نسازید.

## تنظیمات سراسری اختیاری

کلیدها در بخش `Seo` تنظیم می‌شوند؛ برای متغیر محیطی از `Seo__GoogleSiteVerification` به‌جای `Seo:GoogleSiteVerification` استفاده کنید.

| کلید | کاربرد |
|---|---|
| `Seo:SiteUrl` | origin اصلی و عمومی سایت، مانند `https://example.com`؛ برای canonical، URL تصویرهای اجتماعی، robots و sitemap. در `Shop/appsettings.json` روی `https://bs-shop.ir/` تنظیم شده و در محیط انتشار با `Seo__SiteUrl` قابل بازنویسی است. فقط origin مطلق HTTP/HTTPS بدون نام کاربری، مسیر اضافی، query یا fragment پذیرفته می‌شود؛ مقدار خالی یا نامعتبر به origin درخواست جاری برمی‌گردد. |
| `Seo:SiteName` | نام مشترک Open Graph و JSON-LD |
| `Seo:Locale` | locale شبکه‌های اجتماعی؛ پیش‌فرض `fa_IR` |
| `Seo:DefaultImagePath` | تصویر اجتماعی جایگزین؛ پیش‌فرض تصویر hero فروشگاه |
| `Seo:DefaultImageAlt` | متن جایگزین تصویر اجتماعی پیش‌فرض |
| `Seo:GoogleSiteVerification` | توکن تأیید Google Search Console |
| `Seo:BingSiteVerification` | توکن تأیید Bing Webmaster Tools |
| `Seo:FacebookAppId` | شناسهٔ اپ برای `fb:app_id` |
| `Seo:FacebookDomainVerification` | توکن تأیید دامنهٔ Facebook |
| `Seo:XSite` | شناسهٔ حساب X، مانند `@example` |
| `Seo:XCreator` | شناسهٔ اختیاری سازندهٔ کارت اجتماعی در X |

توکن‌های تأیید و شناسه‌های شبکهٔ اجتماعی تا زمان دریافت مقدار واقعی از مالک سایت خالی می‌مانند؛ مقدار نمونه را واقعی تلقی یا در کد hardcode نکنید.

برای جلوگیری از انتشار دامنهٔ alias یا Host ارسالی درخواست در URLهای ایندکس، `Seo:SiteUrl` در `Shop/appsettings.json` روی origin اصلی `https://bs-shop.ir/` تنظیم شده است. محیط انتشار می‌تواند آن را با متغیر `Seo__SiteUrl` بازنویسی کند. اگر تنظیم خالی یا نامعتبر باشد، کد برای سازگاری از scheme و Host درخواست استفاده می‌کند. `SiteRoot` مسیر `Request.PathBase` را نیز لحاظ می‌کند. تصویر hero محصول در View از همان `Seo.ImageUrl` مطلق مصرف می‌شود؛ در اجرای localhost با تنظیم فعلی، URL آن نیز روی دامنهٔ تنظیم‌شده است.

## robots و sitemap

`SeoController` این مسیرها را فراهم می‌کند. URLها از `Seo:SiteUrl` و مسیر پایهٔ برنامه ساخته می‌شوند؛ canonical و Open Graph نیز در partial مشترک همین origin را به‌کار می‌برند:

| مسیر | رفتار |
|---|---|
| `/robots.txt` | `User-agent: *`، `Allow: /` و URL sitemap؛ قاعدهٔ `Disallow` ندارد |
| `/sitemap.xml` | صفحه‌های ثابت و فهرست‌های ساخته‌شده از کاتالوگ برند، مقاله و محصول را برمی‌گرداند |

هنگام افزودن route یا تغییر slug، هم منبع تولید sitemap را بررسی کنید و هم URL canonical و لینک‌های قابل‌مشاهده را. خروجی sitemap تضمین نمی‌کند صفحه از نظر محتوایی آمادهٔ index است.

سبد، تسویه، مقایسه، علاقه‌مندی و پروفایل در sitemap نیستند. هر دو پاسخ robots و sitemap هدر cache یک‌ساعته دارند. Breadcrumb محصول در JSON-LD از مسیر والدهای دسته در کاتالوگ ساخته می‌شود و همان URLهای دسته را دارد که مسیر قابل‌مشاهدهٔ محصول استفاده می‌کند. Product Schema کد محصول را به‌عنوان `sku` می‌فرستد؛ Offer و rating تا زمان اتصال دادهٔ فروش معتبر تولید نمی‌شوند.

## پوسته و دسترس‌پذیری مشترک

`Views/Shared/_Shop.cshtml` نگه‌دارندهٔ `lang="fa"`, `dir="rtl"`, charset، viewport، favicon، partial متادیتا و پیوند «رفتن به محتوای اصلی» است. پیوند دسترس‌پذیری به قاب `#main-content` می‌رود؛ صفحهٔ خانه درون آن یک `<main id="home-main-content">` مستقل دارد و صفحه‌های داخلی main خود را در View تعریف می‌کنند. هنگام تغییر layout، از ایجاد main تو در تو یا حذف مقصد skip link جلوگیری کنید.

favicon فعلی SVG برداری برند در `wwwroot/content/identity/store-mark.svg` است. تصویر OG پیش‌فرض هنوز fallback عمومی است؛ برای صفحه‌ای با تصویر شاخص اختصاصی، `SeoModel.ImageUrl` و متن جایگزین آن را از همان تصویر واقعی صفحه تنظیم کنید.

## محدودیت‌های اندازه‌گیری

این لایه Google Analytics یا Google Tag Manager بارگذاری نمی‌کند. افزودن ابزار اندازه‌گیری نیازمند شناسهٔ واقعی و بررسی رضایت/حریم خصوصی است. داده‌های مجله، محصول و فروشگاه را بدون منبع معتبر به‌عنوان آمار عملیاتی یا structured offer منتشر نکنید.


منابع اصلی: [PageModel.cs](../../Shop/Models/PageModel.cs)، [SeoMetadataOptions.cs](../../Shop/Models/SeoMetadataOptions.cs)، [_SeoMetadata.cshtml](../../Shop/Views/Shared/_SeoMetadata.cshtml) و [SeoController.cs](../../Shop/Controllers/SeoController.cs). تغییر asset تصویر اجتماعی باید با [قرارداد رسانه](../media/images.md) هماهنگ باشد.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۲۶ اکتبر ۲۰۲۶.
