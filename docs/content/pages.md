# مجله و صفحه‌های محتوایی

صفحه‌های مجله از `Shop/Controllers/MagazineController.cs` و `Shop/Views/Magazine/` عبور می‌کنند؛ صفحات FAQ، تماس، درباره، قوانین، ارسال و خدمات controllerهای مستقل در `Shop/Controllers/` دارند و برخی محتواها از `PageController`/entityهای Store خوانده می‌شوند.

## مسیر تغییر

route → Controller → source/DbContext → View → page script/style. برای محتوای قابل مدیریت، endpoint Admin و entity را هم دنبال کنید؛ ویرایشگر مدیریتی و صفحهٔ عمومی دو مالک جدا هستند. رسانه‌ها از [راهنمای رسانه](../media/images.md) پیروی می‌کنند و metadata از [راهنمای SEO](../seo/metadata.md).

پیش از افزودن متن ثابت، بررسی کنید محتوای معادل از CMS/دیتابیس تأمین نمی‌شود. seedهای محتوایی را دادهٔ نمونه معرفی کنید. بررسی: صفحهٔ خالی/محتوا، لینک‌ها، ترتیب RTL، عنوان و تصویر، و viewport موبایل.

نمونه نقاط ورود: `MagazineController`, `BrandsController`, `FaqController`, `AboutController`, `ContactController`, `LegalController`, `ShippingController`, `TrackingController`, `WarrantyController` زیر `Shop/Controllers/`.

فرم تماس صفحهٔ `/contact` در `Shop/wwwroot/shop/js/pages/contact.js` به `POST /api/store/contact` وصل است. پس از اعتبارسنجی و محدودسازی درخواست، پیام ابتدا در جدول `Store.ContactFormMessage` ثبت می‌شود و سپس اعلان ایمیلی با `IEmailSender` مشترک به `PublicEmail` تلاش می‌شود. خطای اعلان ایمیلی مانع ثبت پیام یا نمایش موفقیت فرم نمی‌شود؛ صندوق مدیریت و وضعیت‌های پیام در [راهنمای پنل مدیریت](../admin/README.md) توضیح داده شده‌اند. ساخت جدول و مجوزها در `078-contact-message-inbox.sql` است.

[محتوا](README.md) · [فهرست docs](../README.md)

بازبینی routeها: ۱۲ نوامبر ۲۰۲۶.
