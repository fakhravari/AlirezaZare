# کاتالوگ، فهرست‌ها و کارت محصول

## نقاط ورود و مالکیت

| بخش | نقطهٔ شروع | مالک رفتار / ظاهر |
|---|---|---|
| دادهٔ محصول | [catalog.js](../../Shop/wwwroot/shop/js/store/catalog.js) | `productCatalog` → `window.HNProducts` |
| خانه | [Home/Index.cshtml](../../Shop/Views/Home/Index.cshtml) | `layout/main.js`؛ `home-warmth.css` و کارت مشترک |
| Search | [Product/List.cshtml](../../Shop/Views/Product/List.cshtml) | [search.js](../../Shop/wwwroot/shop/js/pages/search.js)، `search.css` و `search-mobile.css` |
| مقایسه / علاقه‌مندی | [Compare](../../Shop/Views/Compare/Index.cshtml) و [Favorites](../../Shop/Views/Favorites/Index.cshtml) | `store/commerce.js`؛ `commerce.css` و قواعد responsive مشترک |
| کارت مشترک | [shared-product-card.css](../../Shop/wwwroot/shop/css/store/shared-product-card.css) | rendererهای main.js، search.js و commerce.js را نیز پیدا کنید |

جدول route و asset کامل در [نقشهٔ صفحه‌ها](../architecture/routing.md) است؛ ترتیب cascade را از [پوسته](../frontend/layout.md) بخوانید.

## قرارداد محصول

کاتالوگ فعلی ۳۰ ردیف نمونه دارد. `id` از شمارهٔ ردیف به‌علاوهٔ یک ساخته می‌شود؛ تغییر ترتیب آرایه می‌تواند شناسه، URL، state ذخیره‌شده و نام رسانه را تغییر دهد. ردیف را صرفاً برای مرتب‌سازی نمایش جابه‌جا نکنید.

| فیلد | نقش |
|---|---|
| `id`، `name`، `englishName` | هویت نمونه، عنوان فارسی و مبنای slug/نام فایل |
| `brand`، `brandCountryCode` | برند و اتصال به `HNCountries` |
| `type`، `categoryPath` | نوع و مسیر دسته‌بندی |
| `powerSource`، `useCase` | منبع تغذیه و مورد استفاده؛ aliasهای قدیمی `movement` و `strap` برای سازگاری باقی‌اند |
| `price`، `discountPercent` | قیمت نمونه بر حسب میلیون تومان و درصد تخفیف؛ تبدیل نمایش را دوباره اعمال نکنید |
| `stockQuantity`، `inStock`، `maxCartQuantity` | موجودی/حد تعداد نمونه |
| `colors` | فهرست رنگ‌های تعریف‌شده با slug، fa، en و hex |
| `image`، `mainImage`، `thumbnailImage` | نسخهٔ کارت، تصویر اصلی و بندانگشتی؛ [قرارداد رسانه](../media/images.md) |
| `images`، `videos` | رسانه‌های گالری؛ تصاویر شامل `src` و `thumbnail` هستند |
| `detail`، `variants` | توضیحات و گزینه‌ها؛ شرح فنی در شش گروه مشترک است و مشخصات تأییدشدهٔ مدل نیست |

نسخهٔ سمت سرور در [ProductSeoCatalog.cs](../../Shop/Models/ProductSeoCatalog.cs) متن catalog.js را با regex می‌خواند؛ فقط اضافه‌شدن فیلد به شیء JS، دادهٔ SEO را تغییر نمی‌دهد. بعد از تغییر قالب آرایه، صفحهٔ محصول و sitemap را بررسی کنید.

## رنگ و کشور

`productColorCatalog` در catalog.js هشت رنگ مشکی، سفید، قرمز، آبی، سبز، زرد، نارنجی و خاکستری را تعریف می‌کند و به `window.HNProductColors` می‌دهد. شکل رکورد `{slug, en, fa, hex}` است؛ `productColorsById` رنگ را به محصول متصل می‌کند. slug انگلیسی برای URL و اسم فارسی به‌همراه swatch کد رنگ برای نمایش استفاده می‌شود.

[countries.js](../../Shop/wwwroot/shop/js/store/countries.js) نام فارسی/انگلیسی و مسیر پرچم را منتشر می‌کند؛ `productBrandCountryCodes` اتصال برند به کد کشور را نگه می‌دارد. لوگوی برند و پرچم، تصویر داده هستند و به `content/` تعلق دارند. کشور معرفی‌شده را بدون منبع معتبر «کشور ساخت همان کالا» فرض نکنید.

## فهرست‌ها و URL state

Search فیلترها، sort و pagination را با query string و `popstate` هماهنگ می‌کند؛ رنگ در `state.color` یک Set از slugها است. نام فارسی یا hex را جای slug در URL قرار ندهید. پس از تغییر فیلتر، reload، back/forward، بدون نتیجه و ترکیب رنگ با قیمت را بررسی کنید. بازهٔ قیمت در RTL باید سمت راست «از» و چپ «تا» خوانده شود.

خانه چند renderer برای منتخب، پرفروش و تازه‌وارد دارد. عرض کارت، فاصلهٔ دو کارت و تراز فضای قیمت را با بخش اول خانه مقایسه کنید. ظاهر مشترک کارت در shared-product-card.css است؛ home-warmth.css فقط اصلاح‌های خانه را نگه می‌دارد. افزودن به سبد باید طبق [قرارداد سبد](cart.md) انجام شود.

## بازبینی مرتبط

دو کارت با عنوان کوتاه/بلند، قیمت با/بدون تخفیف، موجودی صفر و چند محصول را در موبایل و دسکتاپ بررسی کنید. گروه‌های مقایسه در موبایل باید بدون ایجاد overflow کل صفحه قابل پیمایش باشند. تغییر کارت مشترک را روی Home، Search، Related، Favorites و Compare بررسی کنید؛ rendererهای متفاوت را یکسان فرض نکنید.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۲۱ اکتبر ۲۰۲۶.
