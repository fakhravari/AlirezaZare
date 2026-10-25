# کاتالوگ، فهرست‌ها و کارت محصول

## نقاط ورود و مالکیت

| بخش | نقطهٔ شروع | مالک رفتار / ظاهر |
|---|---|---|
| دادهٔ محصول | [catalog.json](../../Shop/wwwroot/content/data/catalog.json) | [CatalogRepository](../../Shop/Services/CatalogRepository.cs) و resolver [catalog.js](../../Shop/wwwroot/shop/js/store/catalog.js) |
| خانه | [Home/Index.cshtml](../../Shop/Views/Home/Index.cshtml) | `layout/main.js`؛ `home-warmth.css` و کارت مشترک |
| Search | [Product/List.cshtml](../../Shop/Views/Product/List.cshtml) | [search.js](../../Shop/wwwroot/shop/js/pages/search.js)، `search.css` و `search-mobile.css` |
| مقایسه / علاقه‌مندی | [Compare](../../Shop/Views/Compare/Index.cshtml) و [Favorites](../../Shop/Views/Favorites/Index.cshtml) | `store/commerce.js`؛ `commerce.css` و قواعد responsive مشترک |
| کارت مشترک | [product-card.js](../../Shop/wwwroot/shop/js/store/product-card.js) و [shared-product-card.css](../../Shop/wwwroot/shop/css/store/shared-product-card.css) | یک renderer با variantهای grid/slider/search/related/favorite؛ مصرف در main/search/commerce |

جدول route و asset کامل در [نقشهٔ صفحه‌ها](../architecture/routing.md) است؛ ترتیب cascade را از [پوسته](../frontend/layout.md) بخوانید.

## قرارداد محصول

کاتالوگ فعلی ۳۰ محصول نمونه با `schemaVersion: 1` دارد. `id`، `code` و `slug` در هر رکورد صریح‌اند؛ تغییر ترتیب آرایه، هویت، URL و رسانه را عوض نمی‌کند. شناسهٔ حذف‌شده را به کالای دیگری اختصاص ندهید. URL با `/products/{slug}-{id}` ساخته می‌شود و مسیرهای قدیمی یک‌مرحله‌ای به canonical هدایت می‌شوند.

جدول‌های `brands`, `countries`, `categories`, `colors`, `media` و `details` با کلید مشخص نگهداری می‌شوند. محصول با `brandId`, `categoryId`, `colorIds`, `mediaId`, `detailId` به آن‌ها متصل است؛ برند با `countryCode` به کشور وصل می‌شود. `collections.selected/bestsellers/newest` فقط شناسهٔ محصول دارند و ترتیب نمایش خانه را تعیین می‌کنند. نام، قیمت، مسیر فایل یا شمارهٔ ردیف کلید رابطه نیست.

resolver، شکل مصرفی سازگار زیر را می‌سازد؛ `HNProducts` و اشیای درونی آن frozen هستند. برای پیدا کردن محصول از `HNCatalog.find(id)` و برای لینک از `HNCatalog.url(id)` استفاده کنید. برای نمایش فهرست‌ها داده را کپی و مرتب کنید، نه اینکه شیء مشترک را تغییر دهید.

| فیلد | نقش |
|---|---|
| `id`، `code`، `slug`، `name`، `englishName` | هویت پایدار، کد کالا، slug صریح و عنوان فارسی/انگلیسی |
| `brand`، `brandCountryCode` | برند و اتصال به `HNCountries` |
| `type`، `categoryPath` | نوع و مسیر دسته‌بندی |
| `powerSource`، `useCase` | منبع تغذیه و مورد استفاده؛ aliasهای قدیمی `movement` و `strap` برای سازگاری باقی‌اند |
| `price`، `discountPercent` | قیمت نمونه بر حسب میلیون تومان و درصد تخفیف؛ تبدیل نمایش را دوباره اعمال نکنید |
| `stockQuantity`، `inStock`، `maxCartQuantity` | موجودی/حد تعداد نمونه |
| `colors` | فهرست رنگ‌های تعریف‌شده با slug، fa، en و hex |
| `image`، `mainImage`، `thumbnailImage` | نسخهٔ کارت، تصویر اصلی و بندانگشتی؛ [قرارداد رسانه](../media/images.md) |
| `images`، `videos` | رسانه‌های گالری؛ تصاویر شامل `src` و `thumbnail` هستند |
| `detail`، `variants` | توضیحات و گزینه‌ها؛ شرح فنی در شش گروه مشترک است و مشخصات تأییدشدهٔ مدل نیست |

CatalogRepository همان JSON را با System.Text.Json می‌خواند و شناسه/کد تکراری، رابطهٔ شکسته، slug نامعتبر، مسیر نامعتبر، قیمت/موجودی منفی و collection نامعتبر را رد می‌کند. JSON مرورگر با encoder پیش‌فرض امن در `_CatalogData` منتشر می‌شود. دادهٔ قالب، قیمت و موجودی همچنان نمونه است.

## رنگ و کشور

`colors` در catalog.json هشت رنگ اصلی را با شکل `{slug, en, fa, hex}` تعریف می‌کند و `colorIds` محصول به آن‌ها وصل است. slug انگلیسی برای URL و نام فارسی به‌همراه swatch کد رنگ برای نمایش استفاده می‌شود؛ API سازگار `HNProductColors` باقی است.

ویژگی‌های قابل انتخاب محصول در `variants` به شکل کلید ویژگی با `label` و `options` تعریف می‌شوند؛ گزینه‌ها `value`، `label`، `swatch` و `priceDelta` دارند. `variantStock` موجودی هر ترکیب را با کلیدی مانند `color=yellow` نگه می‌دارد؛ برای چند ویژگی، ترتیب کلیدها باید با ترتیب `variants` و قالب `key=value|key=value` هماهنگ باشد. دیتای فعلی رنگ، قیمت و موجودی چند کالا نمونه است و باید پیش از فروش واقعی با منبع موجودی معتبر جایگزین شود.

`countries` در همان JSON نام فارسی/انگلیسی و مسیر پرچم را نگه می‌دارد؛ `brands[id].countryCode` اتصال برند را تعیین می‌کند. API سازگار `HNCountries` باقی است و countries.js جداگانه حذف شده است. لوگوی برند و پرچم در `content/` قرار دارند. کشور برند به‌خودی‌خود کشور ساخت همان کالا نیست.

## فهرست‌ها و URL state

Search فیلترها، sort و pagination را با query string و `popstate` هماهنگ می‌کند؛ رنگ در `state.color` یک Set از slugها است. نام فارسی یا hex را جای slug در URL قرار ندهید. پس از تغییر فیلتر، reload، back/forward، بدون نتیجه و ترکیب رنگ با قیمت را بررسی کنید. بازهٔ قیمت در RTL با حداقل و دستگیرهٔ «از» در سمت راست و حداکثر و دستگیرهٔ «تا» در سمت چپ نمایش داده می‌شود؛ جابه‌جایی هر دستگیره باید همان حد متناظر را تغییر دهد.

خانه، جستجو، مشابه و علاقه‌مندی، markup و قیمت کارت را از `HNProductCard.render(product, {variant})` می‌گیرند. `HNProductCard.price` قیمت مشترک مقایسه را نیز می‌سازد؛ جدول مقایسه چیدمان جدا دارد. قیمت Search از چیدمان دو ردیفهٔ صفحهٔ اول پیروی می‌کند؛ فاصلهٔ ستون تخفیف ۱۰px و مبلغ جاری در کارت باریک ۱۶px است تا نشان تخفیف با واحد تومان تداخل نکند. عرض، فاصله و قیمت کارت مشترک در shared-product-card.css مدیریت می‌شود؛ home-warmth.css فقط چیدمان اختصاصی خانه را نگه می‌دارد؛ راهنمای انتخاب در بالای کاتالوگ با قاب گرم کم‌رنگ و آیکن اطلاع‌رسانی نمایش داده می‌شود و در موبایل padding و فونت کوچک‌تر دارد. افزودن به سبد طبق [قرارداد سبد](cart.md) است.

تصویر کارت در نتایج جست‌وجو با `cover` و وسط‌چین نمایش داده می‌شود تا قاب را کامل پر کند؛ قیمت Search هم از چیدمان دو ردیفهٔ صفحهٔ اول پیروی می‌کند، فاصلهٔ ستون تخفیف ۱۰px و مبلغ جاری در کارت باریک ۱۶px است تا نشان تخفیف با واحد تومان تداخل نکند. variantهای خانه و کالای مشابه مالکیت تصویر جداگانه دارند.

## بازبینی مرتبط

بخش قیمت کارت مقایسه ارتفاع مشترک ۱۰۸px و `flex-shrink: 0` دارد تا ردیف قیمت قبلی و نشان تخفیف، شروع ویژگی‌ها را نسبت به کالای بدون تخفیف جابه‌جا نکند. عنوان موبایل نیز حداقل فضای دو خط را نگه می‌دارد؛ قواعد در `commerce.css` هستند.

در موبایل، هر کارت مقایسه به اندازهٔ عرض قابل‌دیدن گروه است و گزینه‌های بعدی داخل همان گروه افقی پیمایش می‌شوند؛ راهنمای کشیدن کارت‌ها و دسترسی فوکوس صفحه‌کلید در renderer مشترک مقایسه قرار دارد. صفحه نباید اسکرول افقی مستقل بگیرد. نشان بالای تصویر کارت محصول در `shared-product-card.css` عرض محدود دارد تا وارد محدودهٔ دکمه‌های علاقه‌مندی/مقایسه نشود.

دو کارت با عنوان کوتاه/بلند، قیمت با/بدون تخفیف و موجودی صفر را در موبایل و دسکتاپ بررسی کنید. گروه‌های مقایسه در موبایل باید بدون overflow کل صفحه قابل پیمایش باشند. `npm test` حفظ خروجی کاتالوگ پیش از انتقال، استقلال از ترتیب، مسیر فایل‌ها و قرارداد renderer را بررسی می‌کند؛ [کیفیت](../development/quality.md) مرجع فرمان‌هاست.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۲۵ اکتبر ۲۰۲۶.
