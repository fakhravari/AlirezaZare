# مدل و زیرساخت پایگاه داده

پروژهٔ داده `Data/Data.csproj` است و هر دو وب‌برنامه به آن ارجاع دارند. EF Core context در `Data/Infrastructure/Persistence/ShopDbContext.cs` قرار دارد؛ entityها زیر `Data/Domain/` و قراردادها زیر `Data/Application/` هستند.

## هنگام تغییر schema

1. entity، نگاشت Fluent و repository مصرف‌کننده را با هم پیدا کنید.
2. migrationهای موجود را با `rg --files Data Shop | rg 'Migrations|\.sql$'` بیابید و بررسی کنید؛ در این مخزن ممکن است migrationهای SQL در پوشهٔ featureهای `Shop/Features/**/Database/Migrations/` نیز باشند.
3. migration افزایشی و امن برای دادهٔ موجود بسازید؛ ترتیب اجرا و پیش‌شرط‌ها را مستند کنید.
4. اجرای محلی/مقصد را جداگانه بررسی کنید و marker ثبت‌شده در همان DB را از وجود فایل migration تفکیک کنید.
5. قرارداد مصرف‌کننده را در راهنمای حوزه به‌روز کنید.

هیچ سندی فهرست ثابتِ «همهٔ migrationهای اجراشده» نیست؛ وضعیت اجرای هر پایگاه را از جدول history/marker همان پایگاه و migration runner واقعی بخوانید. پیش از حذف یا بازسازی داده، backup، شمارش وابستگی و مجوز صریح لازم است.

دامنه‌های اصلی در entityهای `Store`، `Account`، `Warehouse` و `Admin` تفکیک شده‌اند. قیمت نسخه‌ای در `Store.PriceVersion` است؛ تاریخچهٔ قبل از نصب نسخه‌بندی را نمی‌توان از snapshot اولیه بازسازی کرد. مرجع قراردادها: [قیمت محصول](../store/product.md) و [دیتابیس و داده‌ها](data-sources.md).

ثبت‌نام خبرنامهٔ کاربران مهمان در entity `StoreNewsletterSubscriber` و جدول `Store.NewsletterSubscriber` نگهداری می‌شود؛ ایمیل با حروف کوچک canonical و دارای unique index است. کاربران واردشده در جدول جدید درج نمی‌شوند و از `Account.Profile.NewsletterEnabled` استفاده می‌کنند. ایجاد جدول و اصلاح متن محتوای seed در `076-customer-support-and-newsletter.sql` است؛ فایل SQL باید طبق فرایند استقرار روی هر پایگاه هدف اجرا و marker آن بررسی شود.

پیام فرم تماس در entity `StoreContactMessage` و جدول `Store.ContactFormMessage` ذخیره می‌شود؛ وضعیت‌های عملیاتی `new`, `read`, `closed` و زمان ثبت UTC است. صفحهٔ پنل و تغییر وضعیت از API مجوزدار انجام می‌شود. schema و مجوزهای صفحه/API در migration افزایشی `078-contact-message-inbox.sql` تعریف شده‌اند؛ وجود فایل، اجرای آن در پایگاه مقصد را تضمین نمی‌کند.

پیام‌های ارسالی مدیر به حساب مشتری در entity `AccountMessage` و جدول `Account.Message` نگهداری می‌شوند؛ پیام به پروفایل مالک وصل است، هنگام حذف پروفایل cascade می‌شود و وضعیت خواندن مختص همان پیام است. جدول و مجوزهای مدیریت پیام در migration `078-contact-message-inbox.sql` ساخته می‌شوند.

مدت اعتبار و فاصلهٔ ارسال مجدد کد ورود ایمیل در `Store.SiteSetting.EmailVerificationCodeLifetimeMinutes` قرار دارد. مهاجرت افزایشی `077-email-verification-code-lifetime.sql` ستون را با مقدار پیش‌فرض ۱۵ دقیقه اضافه می‌کند؛ صفحهٔ «تنظیمات سایت» در پنل مدیریت مقدار ۱ تا ۱۴۴۰ دقیقه را ذخیره می‌کند. رفتار احراز در [راهنمای حساب](../account/profile.md) توضیح داده شده است.

[معماری](README.md) · [توسعه](../development/README.md) · [فهرست docs](../README.md)

بازبینی ساختار: ۱۲ نوامبر ۲۰۲۶.
