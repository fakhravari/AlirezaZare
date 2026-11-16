# مجله و صفحه‌های محتوایی

صفحه‌های مجله از `Shop/Controllers/MagazineController.cs` و `Shop/Views/Magazine/` عبور می‌کنند؛ صفحات FAQ، تماس، درباره، قوانین، ارسال و خدمات controllerهای مستقل در `Shop/Controllers/` دارند و برخی محتواها از `PageController`/entityهای Store خوانده می‌شوند.

## مسیر تغییر

در پنل مدیریت، منوی «مدیریت محتوا» سه صفحهٔ مجزا دارد: `/Store/Features/Index?feature=content&section=faq` برای پرسش‌های متداول، `section=magazine` برای مقاله‌ها و `section=pages` برای صفحه‌های خدماتی. نشانی قدیمی بدون section به پرسش‌های متداول هدایت می‌شود. هر صفحه فقط ویرایشگر و API حوزهٔ خود را بارگذاری می‌کند؛ دسترسی هر سه همچنان به نقش `system.full-access` محدود است.

route → Controller → source/DbContext → View → page script/style. برای محتوای قابل مدیریت، endpoint Admin و entity را هم دنبال کنید؛ ویرایشگر مدیریتی و صفحهٔ عمومی دو مالک جدا هستند. رسانه‌ها از [راهنمای رسانه](../media/images.md) پیروی می‌کنند و metadata از [راهنمای SEO](../seo/metadata.md).

متن‌هایی که از ادیتور مدیریت ذخیره می‌شوند، در FAQ، مقاله، تماس، درباره، ارسال، پیگیری، ضمانت، پیام‌های حساب و صفحات محتوایی عمومی با `Data/Application/Store/StoreHtmlSanitizer.cs` پاک‌سازی و با حفظ قالب‌های مجاز راست‌به‌چپ نمایش داده می‌شوند. تگ‌های ساختاری مانند پاراگراف، عنوان، فهرست و لینک مجازند و ویژگی‌های ناامن حذف می‌شوند؛ متن‌های قدیمی ساده نیز شکست خط خود را حفظ می‌کنند. مقاله در `Shop/Views/Magazine/Details.cshtml` و پیام مشتری در `Shop/wwwroot/shop/js/pages/account-page.js` محتوای پاک‌سازی‌شده را به‌عنوان HTML رندر می‌کنند تا کد تگ‌ها به‌صورت متن دیده نشود. نمایش سابقهٔ پیام و پاسخ مدیر در Admin نیز همین پاک‌سازی را از API می‌گیرد.

پیش از افزودن متن ثابت، بررسی کنید محتوای معادل از CMS/دیتابیس تأمین نمی‌شود. seedهای محتوایی را دادهٔ نمونه معرفی کنید. بررسی: صفحهٔ خالی/محتوا، لینک‌ها، ترتیب RTL، عنوان و تصویر، و viewport موبایل.

نمونه نقاط ورود: `MagazineController`, `BrandsController`, `FaqController`, `AboutController`, `ContactController`, `LegalController`, `ShippingController`, `TrackingController`, `WarrantyController` زیر `Shop/Controllers/`.

فرم تماس صفحهٔ `/contact` در `Shop/wwwroot/shop/js/pages/contact.js` به `POST /api/store/contact` وصل است. پس از اعتبارسنجی و محدودسازی درخواست، پیام ابتدا در جدول `Store.ContactFormMessage` ثبت می‌شود و سپس اعلان ایمیلی با `IEmailSender` مشترک به `PublicEmail` تلاش می‌شود. خطای اعلان ایمیلی مانع ثبت پیام یا نمایش موفقیت فرم نمی‌شود؛ صندوق مدیریت و وضعیت‌های پیام در [راهنمای پنل مدیریت](../admin/README.md) توضیح داده شده‌اند. ساخت جدول و مجوزها در `078-contact-message-inbox.sql` است.

صفحهٔ `/Tracking` کد سفارش و شمارهٔ همراه را به `POST /api/store/orders/tracking` می‌فرستد. فقط سفارشی پیدا می‌شود که هر دو مقدار با سفارش و شمارهٔ snapshot نشانی ارسال منطبق باشند؛ سفارش نمایشی قابل استعلام نیست. پاسخ وضعیت سفارش و تاریخچهٔ ارسال/رهگیری را برمی‌گرداند و جزئیات خرید یا نشانی را افشا نمی‌کند. پیاده‌سازی API و فرم در `OrderTrackingApiController.cs` و `Shop/wwwroot/shop/js/pages/tracking.js` است؛ مسیر خرید و سفارش در [راهنمای سبد](../store/cart.md) آمده است.

[محتوا](README.md) · [فهرست docs](../README.md)

بازبینی routeها: ۱۴ نوامبر ۲۰۲۶.

- فرم تماس ایمیل اختیاری مشتری را برای امکان پاسخ مستقیم پشتیبانی دریافت می‌کند.
