# نقشهٔ مسیر، View و منابع اختصاصی

این جدول نقطهٔ شروع بررسی هر صفحه است. مسیر CSSها نسبت به `wwwroot/shop/css/` و JSها نسبت به `wwwroot/shop/js/` است؛ ستون‌ها فقط منابع مستقیم View را نشان می‌دهند. منابع سراسری و جایگاه sectionها در [پوسته](../frontend/layout.md) مستند شده‌اند. renderer مرورگر ممکن است DOM دیگری درون View بسازد.

## جدول صفحه‌ها

| مسیر | Controller → View | CSS مستقیم View | JS مستقیم View | نقطهٔ بررسی |
|---|---|---|---|---|
| `/` | [HomeController](../../Shop/Controllers/HomeController.cs) → [Index](../../Shop/Views/Home/Index.cshtml) | `pages/home-warmth.css` | — | خانه، grid و اسلایدرهای کالا |
| `/PaymentInstitutions` | [PaymentInstitutionsController](../../Shop/Controllers/PaymentInstitutionsController.cs) → [Index](../../Shop/Views/PaymentInstitutions/Index.cshtml) | — | — | نمایش نام فهرست درگاه‌های پرداخت |
| `/search` | [ProductController](../../Shop/Controllers/ProductController.cs) → [List](../../Shop/Views/Product/List.cshtml) | `pages/search.css` (در `SearchStyles` section) | `pages/search.js` | فیلتر `?sales-plan={planId}` با شناسهٔ پایدار، مرتب‌سازی، URL و صفحه‌بندی |
| `/products/{slug}` | [ProductController](../../Shop/Controllers/ProductController.cs) → [Details](../../Shop/Views/Product/Details.cshtml) | `pages/pages.css`، `pages/product-detail.css`، `pages/form-confirm-dialog.css`، `pages/product-detail-pricing.css`، `pages/product-detail-options.css` | `pages/form-confirm-dialog.js`، `pages/page-content.js` | main.product-detail-page، #productView و گالری؛ `?salesPlanId={planId}` طرح را مشخص می‌کند و `regular` در URL لازم نیست |
| `/cart` | [CartController](../../Shop/Controllers/CartController.cs) → [Index](../../Shop/Views/Cart/Index.cshtml) | `pages/pages.css`، `pages/checkout.css`، `pages/form-confirm-dialog.css` | `pages/page-content.js`، `pages/iran-places.js`، `pages/address-fields.js`، `pages/checkout.js`، `pages/form-confirm-dialog.js` | #cartView، آدرس و گزینه‌های سبد |
| `/checkout` | [CheckoutController](../../Shop/Controllers/CheckoutController.cs) → [Index](../../Shop/Views/Checkout/Index.cshtml) | `pages/pages.css`، `pages/checkout.css` | `pages/page-content.js`، `pages/checkout.js` | بازاعتبارسنجی و ایجاد سفارش پایدار؛ پرداخت واقعی هنوز متصل نیست |
| `/compare` | [CompareController](../../Shop/Controllers/CompareController.cs) → [Index](../../Shop/Views/Compare/Index.cshtml) | `pages/form-confirm-dialog.css` | `pages/form-confirm-dialog.js` | #compareView؛ صفر، یک یا چند محصول |
| `/favorites` | [FavoritesController](../../Shop/Controllers/FavoritesController.cs) → [Index](../../Shop/Views/Favorites/Index.cshtml) | `pages/pages.css` | `pages/page-content.js` | #favoritesView؛ حالت خالی و حذف |
| `/profile` و زیرمسیرها | [ProfileController](../../Shop/Controllers/ProfileController.cs) → [Index](../../Shop/Views/Profile/Index.cshtml) | `pages/pages.css`، `pages/checkout.css`، `pages/account-page.css` | `pages/page-content.js`، `pages/iran-places.js`، `pages/address-fields.js`، `pages/account-page.js`، `pages/orders-pagination.js` | کوپن عمومی از `IAccountDataSource`، state شخصی از API حساب؛ پیام‌ها نمونهٔ نمایشی |
| `/magazine` | [MagazineController](../../Shop/Controllers/MagazineController.cs) → [Index](../../Shop/Views/Magazine/Index.cshtml) | `pages/pages.css`، `pages/magazine-luxury.css` | `pages/magazine.js` | `PageModel<MagazineIndexViewModel>` از `IMagazineContentSource`؛ فیلتر مقاله و صفحه‌بندی |
| `/magazine/{slug}` | [MagazineController](../../Shop/Controllers/MagazineController.cs) → [Details](../../Shop/Views/Magazine/Details.cshtml) | `pages/pages.css`، `pages/article-details.css`، `pages/form-confirm-dialog.css` | `pages/page-content.js`، `pages/form-confirm-dialog.js`، `pages/article-details.js` | TOC، progress، print و share |
| `/brands` | [BrandsController](../../Shop/Controllers/BrandsController.cs) → [Index](../../Shop/Views/Brands/Index.cshtml) | `pages/pages.css`، `pages/brands.css` | `pages/page-content.js` | فهرست و شبکهٔ برندها |
| `/brands/{slug}` | [BrandsController](../../Shop/Controllers/BrandsController.cs) → [Details](../../Shop/Views/Brands/Details.cshtml) | `pages/pages.css`، `pages/brands.css` | `pages/brand-product-count.js` | معرفی برند و شمارش محصول |
| `/about` | [AboutController](../../Shop/Controllers/AboutController.cs) → [Index](../../Shop/Views/About/Index.cshtml) | `pages/pages.css`، `pages/about.css` | `pages/page-content.js` | معرفی فروشگاه |
| `/contact` | [ContactController](../../Shop/Controllers/ContactController.cs) → [Index](../../Shop/Views/Contact/Index.cshtml) | `pages/contact.css`، `pages/form-confirm-dialog.css`، `pages/pages.css` | `pages/page-content.js`، `pages/form-confirm-dialog.js`، `pages/contact.js` | فرم و feedback نمونه |
| `/faq` | [FaqController](../../Shop/Controllers/FaqController.cs) → [Index](../../Shop/Views/Faq/Index.cshtml) | `pages/pages.css`، `pages/faq.css` | `pages/page-content.js` | پرسش‌وپاسخ و FAQ schema |
| `/shipping` | [ShippingController](../../Shop/Controllers/ShippingController.cs) → [Index](../../Shop/Views/Shipping/Index.cshtml) | `pages/pages.css`، `pages/shipping.css` | `pages/page-content.js` | مراحل ارسال |
| `/warranty` | [WarrantyController](../../Shop/Controllers/WarrantyController.cs) → [Index](../../Shop/Views/Warranty/Index.cshtml) | `pages/pages.css`، `pages/warranty.css` | `pages/page-content.js` | راهنمای ضمانت |
| `/tracking` | [TrackingController](../../Shop/Controllers/TrackingController.cs) → [Index](../../Shop/Views/Tracking/Index.cshtml) | `pages/pages.css`، `pages/tracking.css` | `pages/page-content.js` | فرم پیگیری نمونه |
| `/legal/privacy` | [LegalController](../../Shop/Controllers/LegalController.cs) → [Privacy](../../Shop/Views/Legal/Privacy.cshtml) | `pages/pages.css` | — | سند حریم خصوصی |
| `/legal/terms` | [LegalController](../../Shop/Controllers/LegalController.cs) → [Terms](../../Shop/Views/Legal/Terms.cshtml) | `pages/pages.css` | — | شرایط استفاده |
| `/legal/returns` | [LegalController](../../Shop/Controllers/LegalController.cs) → [Returns](../../Shop/Views/Legal/Returns.cshtml) | `pages/pages.css` | — | شرایط بازگشت |
| `/error` و `/Error/{statusCode}` | [ErrorController](../../Shop/Controllers/ErrorController.cs) → [Index](../../Shop/Views/Error/Index.cshtml) | — | — | خطاهای 4xx/5xx صفحهٔ راهنمای بدون ایندکس می‌گیرند و status اصلی حفظ می‌شود |

صفحه‌های درباره، تماس، FAQ، ارسال، ضمانت، پیگیری و قوانین با `PageController` محتوای منتشرشده را از `Store.ContentPage` می‌خوانند. همهٔ این مسیرها View اختصاصی خود را اجرا می‌کنند؛ بنابراین CSS، تم و تعاملات صفحه باقی می‌مانند. عنوان و شرح از رکورد محتوا می‌آید و بخش‌های CMS در اجزای همان قالب قرار می‌گیرند: کارت‌های ارزش درباره، مراحل ارسال و ضمانت، کارت پشتیبانی پیگیری و بخش‌های قانونی داخل سند. partial مشترک `_ManagedPageSections.cshtml` فقط برای ساختار حقوقی است و کارت عمومی خدماتی رندر نمی‌کند. `ManagedContentPage.cshtml` فقط fallback برای کلیدهای ناشناخته است. مدیریت و انتشار این صفحه‌ها در `/Store/Features?feature=content` انجام می‌شود و به migrationهای 051 و 052 وابسته است.

## پنل مدیریت

| مسیر | Controller → View | وضعیت |
|---|---|---|
| `/` | [HomeController](../../Admin/Controllers/HomeController.cs) → انتقال به پنل | ورودی برنامه |
| `/Admin/Home/Index` | [HomeController](../../Admin/Areas/Admin/Controllers/HomeController.cs) → [Index](../../Admin/Areas/Admin/Views/Home/Index.cshtml) | پیشخوان کل و لینک حوزه‌ها |
| `/Admin/Users?section=users|roles|permissions` | [UsersController](../../Admin/Areas/Admin/Controllers/UsersController.cs) → Viewهای [کاربران](../../Admin/Areas/Admin/Views/Users/Index.cshtml)، [نقش‌ها](../../Admin/Areas/Admin/Views/Users/Roles.cshtml) و [درخت مجوزها](../../Admin/Areas/Admin/Views/Users/Permissions.cshtml) | مجوز مستقل `Admin:Users:Index:{section}` برای هر صفحه؛ API کاربران نیز section را بررسی می‌کند |
| `/Admin/SiteSettings/Index` | [SiteSettingsController](../../Admin/Areas/Admin/Controllers/SiteSettingsController.cs) → [Index](../../Admin/Areas/Admin/Views/SiteSettings/Index.cshtml) | تنظیمات عمومی سایت و پنل‌های رسانهٔ دیدگاه و هویت |
| `/Admin/SiteSettings/Slider` | [SiteSettingsController](../../Admin/Areas/Admin/Controllers/SiteSettingsController.cs) → [Slider](../../Admin/Areas/Admin/Views/SiteSettings/Slider.cshtml) | بارگذاری و مدیریت رسانه‌های اسلایدر؛ زیرمنوی تنظیمات سایت |
| `/Store/Dashboard/Index` | [DashboardController](../../Admin/Areas/Store/Controllers/DashboardController.cs) → [Index](../../Admin/Areas/Store/Views/Dashboard/Index.cshtml) | پیشخوان فروشگاه |
| `/Store/Catalog/Products`، `/Categories`، `/Brands`، `/Attributes`، `/PaymentInstitutions` | [CatalogController](../../Admin/Areas/Store/Controllers/CatalogController.cs) → [Index](../../Admin/Areas/Store/Views/Catalog/Index.cshtml) | فهرست‌های کاتالوگ از API خوانده می‌شوند؛ فهرست درگاه‌ها لوگو را در FTP بارگذاری و مسیر را در `Store.Bank.LogoPath` ثبت می‌کند |
| `/Store/Features?feature=content` | [FeaturesController](../../Admin/Areas/Store/Controllers/FeaturesController.cs) → [Faq.cshtml](../../Admin/Areas/Store/Views/Features/Faq.cshtml) | پرسش‌ها، مقاله‌ها و صفحه‌های خدماتی با API AJAX؛ فقط مدیر کامل |
| `/Store/Features?feature=geography` | [FeaturesController](../../Admin/Areas/Store/Controllers/FeaturesController.cs) → [Geography.cshtml](../../Admin/Areas/Store/Views/Features/Geography.cshtml) | ویرایش جغرافیا و بارگذاری پرچم از ردیف کشور |
| `/Store/Features?feature=...` | [FeaturesController](../../Admin/Areas/Store/Controllers/FeaturesController.cs) → [Index](../../Admin/Areas/Store/Views/Features/Index.cshtml) | دامنهٔ سفارش، مشتری و بازخورد |
| `/Warehouse/Dashboard/Index` | [DashboardController](../../Admin/Areas/Warehouse/Controllers/DashboardController.cs) → [Index](../../Admin/Areas/Warehouse/Views/Dashboard/Index.cshtml) | پیشخوان انبار |
| `/Warehouse/Warehouses` | [WarehousesController](../../Admin/Areas/Warehouse/Controllers/WarehousesController.cs) → [Index](../../Admin/Areas/Warehouse/Views/Warehouses/Index.cshtml) | فهرست انبارها از API |
| `/Warehouse/Stock` | [StockController](../../Admin/Areas/Warehouse/Controllers/StockController.cs) → [Index](../../Admin/Areas/Warehouse/Views/Stock/Index.cshtml) | موجودی با فیلتر AJAX |
| `/Warehouse/Features?feature=companies` | [FeaturesController](../../Admin/Areas/Warehouse/Controllers/FeaturesController.cs) → [تعریف‌های شرکت](../../Admin/Areas/Warehouse/Views/Features/Definitions.cshtml) | شرکت‌ها و تأمین‌کنندگان؛ در زیرمنوی کاربران و دسترسی‌ها |
| `/Warehouse/Features?feature=...` | [FeaturesController](../../Admin/Areas/Warehouse/Controllers/FeaturesController.cs) → [Index](../../Admin/Areas/Warehouse/Views/Features/Index.cshtml) | دامنهٔ رسید، حواله، انتقال، اصلاح و گردش؛ API عملیاتی هنوز ندارد |

این routeها در برنامهٔ مستقل `Shop.Admin` هستند. مسیر `/` برای کاربر ناشناس مستقیماً به `/auth/login` می‌رود و پس از ورود به داشبورد مدیریت هدایت می‌شود. کلیک روی لینک داخلی، محتوای صفحه را با AJAX جایگزین و history مرورگر را به‌روز می‌کند. فهرست‌های کاتالوگ از `GET /api/admin/store/catalog/{products|categories|brands|attributes}` خوانده می‌شوند. عملیات محصول شامل ساخت پیش‌نویس، ویرایش، انتشار و خروج از انتشار است؛ دسته و برند ایجاد، ویرایش و وضعیت انتشار دارند؛ ویژگی نیز ایجاد، ویرایش و فعال/غیرفعال می‌شود. endpointهای نوشتن زیر `/api/admin/store/catalog/*/manage` قرار دارند و فقط دسترسی کامل، احراز هویت cookie و ضد CSRF را می‌پذیرند. رابط این عملیات را با AJAX به‌روزرسانی می‌کند. API انبار در این جدول شامل `GET /api/admin/warehouse/warehouses` و `GET /api/admin/warehouse/stock` است؛ درخواست‌ها و پاسخ‌ها `no-store` هستند.

## قرارداد route و سازگاری

endpointهای API از routeهای MVC جدا هستند: `GET /api/warehouse/warehouses` و `GET /api/warehouse/stock` برای دادهٔ انبار، و `GET/POST /api/store/products/{id}/feedback/{type}` برای دیدگاه و پرسش محصول (`type`: `reviews` یا `questions`)؛ query و مدل پاسخ در [لایهٔ دیتابیس](database.md) مستند شده است.

کتابخانهٔ رسانهٔ تنظیمات سایت از `GET /api/admin/store/media/library` فهرست می‌گیرد و برای فایل‌های بارگذاری‌شدهٔ عمومی `POST /api/admin/store/media/delete` دارد؛ هر دو فقط برای مدیر کامل در دسترس‌اند و حذف با ضد CSRF انجام می‌شود. بارگذاری‌های متصل به رکورد از همان صفحهٔ مالک انجام می‌شوند: برند در کاتالوگ، پرچم در جغرافیا، مقاله در مدیریت محتوا و رسانهٔ کالا در کاتالوگ.

- `Program.cs` گزینهٔ `LowercaseUrls = true` و مسیر پیش‌فرض `{controller=Home}/{action=Index}/{id?}` را دارد. attribute route را کنار این مسیر بخوانید؛ routeهای تولیدشده lowercase هستند.
- `/search` به `ProductController.Index` و View `List` می‌رسد. کلاس body از controller/action ساخته می‌شود: `mobile-page--product-index`؛ نام View در این کلاس نقشی ندارد.
- قرارداد slug صریح محصول، تطبیق canonical و هدایت مستقیم مسیرهای قدیمی در [راهنمای محصول](../store/product.md) مستند است؛ مسیر واسط `/products/product-{id}` تولید نمی‌شود. لینک offer طرح، `salesPlanId` را برای نمایش همان طرح حمل می‌کند؛ طرح منقضیِ دارای زمان پایان با redirect دائمی به URL canonical عادی برمی‌گردد و canonical HTML همیشه پارامتر طرح را حذف می‌کند.
- `/account` مسیر سازگاری پروفایل است. `/profile`، `/profile/orders`، `/profile/addresses`، `/profile/favorites`، `/profile/coupons`، `/profile/pricealerts` و `/profile/messages` یک View دارند. `/profile/messages` پیام‌های نمونهٔ نمایشی با صفحه‌بندی دارد؛ به پنل ادمین یا backend وصل نیست. `/profile/edit` به `/profile` برمی‌گردد و صفحهٔ مستقلی ندارد.
- مسیر قدیمی `/magazine/details/{slug}` به مسیر جدید مقاله هدایت می‌شود.
- در انتشار، Hostهای غیر canonical بر اساس `Seo:SiteUrl` با redirect دائمی به همان path و query روی دامنهٔ اصلی هدایت می‌شوند؛ loopback در Development آزاد است.
- `/robots.txt` و `/sitemap.xml` از `SeoController` می‌آیند و View صفحه ندارند؛ [راهنمای SEO](../seo/metadata.md) را ببینید.

## پس از تغییر URL یا View

Controller، لینک‌های Razor، تولید لینک در JS، canonical، sitemap و کلیدهای route/hash را با هم بررسی کنید. در صفحه‌های دارای renderer مثل محصول، سبد و مقایسه، markup نهایی را در `commerce.js` نیز پیدا کنید. رفتار اختصاصی را در راهنمای [فروشگاه](../store/README.md)، [حساب](../account/README.md) یا [محتوا](../content/README.md) ثبت کنید؛ این جدول فقط نقشهٔ ورود باقی بماند.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۱۱ نوامبر ۲۰۲۶.
