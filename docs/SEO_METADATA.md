# راهنمای SEO و metadata مشترک

این راهنما مسیر metadata صفحه‌ها و structured data را توضیح می‌دهد. برای ترتیب یادگیری و دیگر مستندات از [`README.md`](README.md) شروع کنید؛ نمای معماری و وضعیت داده‌ها در [`DEVELOPER_ROADMAP.md`](DEVELOPER_ROADMAP.md) آمده است.

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

## Structured data و FAQ

`_SeoMetadata.cshtml` گراف JSON-LD مشترک شامل داده‌های سازمان/فروشگاه، وب‌سایت و صفحه را می‌سازد و در صورت وجود دادهٔ معتبر، structured data صفحه و breadcrumb را به آن اضافه می‌کند. پیش از تغییر schema، مدل، partial و Controller صفحه را با هم دنبال کنید تا منبع دادهٔ واحد حفظ شود.

صفحهٔ FAQ از `FaqCatalog` برای متن پرسش‌وپاسخ قابل‌مشاهده و `FAQPage` JSON-LD استفاده می‌کند. پرسش یا پاسخ را در همان کاتالوگ ویرایش کنید؛ markup موازی در View نسازید.

## تنظیمات سراسری اختیاری

کلیدها در بخش `Seo` تنظیم می‌شوند؛ برای متغیر محیطی از `Seo__GoogleSiteVerification` به‌جای `Seo:GoogleSiteVerification` استفاده کنید.

| کلید | کاربرد |
|---|---|
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

## robots و sitemap

`SeoController` این مسیرها را فراهم می‌کند:

| مسیر | رفتار |
|---|---|
| `/robots.txt` | قواعد crawl و نشانی sitemap را برمی‌گرداند |
| `/sitemap.xml` | صفحه‌های ثابت و فهرست‌های ساخته‌شده از کاتالوگ برند، مقاله و محصول را برمی‌گرداند |

هنگام افزودن route یا تغییر slug، هم منبع تولید sitemap را بررسی کنید و هم URL canonical و لینک‌های قابل‌مشاهده را. خروجی sitemap تضمین نمی‌کند صفحه از نظر محتوایی آمادهٔ index است.

## پوسته و دسترس‌پذیری مشترک

`Views/Shared/_Shop.cshtml` نگه‌دارندهٔ `lang="fa"`, `dir="rtl"`, charset، viewport، favicon، partial متادیتا و پیوند «رفتن به محتوای اصلی» است. پیوند دسترس‌پذیری به قاب `#main-content` می‌رود؛ صفحهٔ خانه درون آن یک `<main id="home-main-content">` مستقل دارد و صفحه‌های داخلی main خود را در View تعریف می‌کنند. هنگام تغییر layout، از ایجاد main تو در تو یا حذف مقصد skip link جلوگیری کنید.

favicon فعلی SVG برداری برند در `wwwroot/Shop/images/shared/store-mark.svg` است. تصویر OG پیش‌فرض هنوز fallback عمومی است؛ برای صفحه‌ای با تصویر شاخص اختصاصی، `SeoModel.ImageUrl` و متن جایگزین آن را از همان تصویر واقعی صفحه تنظیم کنید.

## محدودیت‌های اندازه‌گیری

این لایه Google Analytics یا Google Tag Manager بارگذاری نمی‌کند. افزودن ابزار اندازه‌گیری نیازمند شناسهٔ واقعی و بررسی رضایت/حریم خصوصی است. داده‌های مجله، محصول و فروشگاه را بدون منبع معتبر به‌عنوان آمار عملیاتی یا structured offer منتشر نکنید.
