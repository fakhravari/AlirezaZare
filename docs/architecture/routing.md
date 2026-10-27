# نقشهٔ مسیر، View و منابع اختصاصی

این جدول نقطهٔ شروع بررسی هر صفحه است. مسیر CSSها نسبت به `wwwroot/shop/css/` و JSها نسبت به `wwwroot/shop/js/` است؛ ستون‌ها فقط منابع مستقیم View را نشان می‌دهند. منابع سراسری و جایگاه sectionها در [پوسته](../frontend/layout.md) مستند شده‌اند. renderer مرورگر ممکن است DOM دیگری درون View بسازد.

## جدول صفحه‌ها

| مسیر | Controller → View | CSS مستقیم View | JS مستقیم View | نقطهٔ بررسی |
|---|---|---|---|---|
| `/` | [HomeController](../../Shop/Controllers/HomeController.cs) → [Index](../../Shop/Views/Home/Index.cshtml) | `pages/home-warmth.css` | — | خانه، grid و اسلایدرهای کالا |
| `/search` | [ProductController](../../Shop/Controllers/ProductController.cs) → [List](../../Shop/Views/Product/List.cshtml) | `pages/search.css` (در `SearchStyles` section) | `pages/search.js` | فیلتر، مرتب‌سازی، URL و صفحه‌بندی |
| `/products/{slug}` | [ProductController](../../Shop/Controllers/ProductController.cs) → [Details](../../Shop/Views/Product/Details.cshtml) | `pages/pages.css`، `pages/product-detail.css`، `pages/form-confirm-dialog.css`، `pages/product-detail-pricing.css`، `pages/product-detail-options.css` | `pages/form-confirm-dialog.js`، `pages/page-content.js` | main.product-detail-page، #productView و گالری |
| `/cart` | [CartController](../../Shop/Controllers/CartController.cs) → [Index](../../Shop/Views/Cart/Index.cshtml) | `pages/pages.css`، `pages/checkout.css`، `pages/form-confirm-dialog.css` | `pages/page-content.js`، `pages/iran-places.js`، `pages/address-fields.js`، `pages/checkout.js`، `pages/form-confirm-dialog.js` | #cartView، آدرس و گزینه‌های سبد |
| `/checkout` | [CheckoutController](../../Shop/Controllers/CheckoutController.cs) → [Index](../../Shop/Views/Checkout/Index.cshtml) | `pages/pages.css`، `pages/checkout.css` | `pages/page-content.js`، `pages/checkout.js` | فاکتور و روش پرداخت نمونه |
| `/compare` | [CompareController](../../Shop/Controllers/CompareController.cs) → [Index](../../Shop/Views/Compare/Index.cshtml) | `pages/form-confirm-dialog.css` | `pages/form-confirm-dialog.js` | #compareView؛ صفر، یک یا چند محصول |
| `/favorites` | [FavoritesController](../../Shop/Controllers/FavoritesController.cs) → [Index](../../Shop/Views/Favorites/Index.cshtml) | `pages/pages.css` | `pages/page-content.js` | #favoritesView؛ حالت خالی و حذف |
| `/profile` و زیرمسیرها | [ProfileController](../../Shop/Controllers/ProfileController.cs) → [Index](../../Shop/Views/Profile/Index.cshtml) | `pages/pages.css`، `pages/checkout.css`، `pages/account-page.css` | `pages/page-content.js`، `pages/iran-places.js`، `pages/address-fields.js`، `pages/account-page.js`، `pages/orders-pagination.js` | نمای مشترک حساب و dialog ویرایش |
| `/magazine` | [MagazineController](../../Shop/Controllers/MagazineController.cs) → [Index](../../Shop/Views/Magazine/Index.cshtml) | `pages/pages.css`، `pages/magazine-luxury.css` | `pages/magazine.js` | فیلتر مقاله و صفحه‌بندی |
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
| `/error` | [ErrorController](../../Shop/Controllers/ErrorController.cs) → [Index](../../Shop/Views/Error/Index.cshtml) | — | — | صفحهٔ خطا |

## قرارداد route و سازگاری

- `Program.cs` گزینهٔ `LowercaseUrls = true` و مسیر پیش‌فرض `{controller=Home}/{action=Index}/{id?}` را دارد. attribute route را کنار این مسیر بخوانید؛ routeهای تولیدشده lowercase هستند.
- `/search` به `ProductController.Index` و View `List` می‌رسد. کلاس body از controller/action ساخته می‌شود: `mobile-page--product-index`؛ نام View در این کلاس نقشی ندارد.
- قرارداد slug صریح محصول، تطبیق canonical و هدایت مستقیم مسیرهای قدیمی در [راهنمای محصول](../store/product.md) مستند است؛ مسیر واسط `/products/product-{id}` تولید نمی‌شود.
- `/account` مسیر سازگاری پروفایل است. `/profile`، `/profile/orders`، `/profile/addresses`، `/profile/favorites`، `/profile/coupons` و `/profile/pricealerts` یک View دارند. `/profile/edit` به `/profile` برمی‌گردد و صفحهٔ مستقلی ندارد.
- مسیر قدیمی `/magazine/details/{slug}` به مسیر جدید مقاله هدایت می‌شود.
- `/robots.txt` و `/sitemap.xml` از `SeoController` می‌آیند و View صفحه ندارند؛ [راهنمای SEO](../seo/metadata.md) را ببینید.

## پس از تغییر URL یا View

Controller، لینک‌های Razor، تولید لینک در JS، canonical، sitemap و کلیدهای route/hash را با هم بررسی کنید. در صفحه‌های دارای renderer مثل محصول، سبد و مقایسه، markup نهایی را در `commerce.js` نیز پیدا کنید. رفتار اختصاصی را در راهنمای [فروشگاه](../store/README.md)، [حساب](../account/README.md) یا [محتوا](../content/README.md) ثبت کنید؛ این جدول فقط نقشهٔ ورود باقی بماند.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۲۷ اکتبر ۲۰۲۶.
