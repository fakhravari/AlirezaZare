# CSS و مالکیت استایل

CSS فروشگاه در `Shop/wwwroot/shop/css/` دسته‌بندی شده است: `core/`, `layout/`, `store/`, `pages/`. پنل Admin مالک جداگانه در `Admin/wwwroot/css/` دارد. مرجع ownerهای UI، `tools/ui-owners.json` است.

## روش تغییر

1. selector را از markup واقعی پیدا کنید و همهٔ تعریف‌ها را `rg` کنید.
2. layout و View را برای cascade و stylesheetهای واقعاً بارشده بررسی کنید.
3. قانون را در فایل owner اصلاح کنید؛ قاعدهٔ page-specific را به shared stylesheet منتقل نکنید مگر مالکیت مشترک روشن باشد.
4. selectorهای عمومی، `!important` و override انتهایی جدید را فقط با دلیل محدود و ثبت‌شده اضافه کنید.
5. RTL، breakpointهای نزدیک تغییر، focus/hover، reduced motion و سرریز را مرور کنید.

توکن‌ها و پایهٔ مشترک در `css/core/design-tokens.css`, `components.css`, `ui-core.css` و `site.css` هستند؛ منبع دقیق هر component در owner manifest آمده است. کپی کردن قانون مشترک در چند صفحه باعث drift می‌شود.

متن کوتاه نبود قیمت تنوع در کارت محصول با `.product-price-unavailable` و رنگ ثانویهٔ رابط نمایش داده می‌شود تا از قیمت کالا برجسته‌تر نباشد؛ مالکیت آن با `product-card` و `shared-product-card.css` است.

بازبینی: `node tools/check-ui.mjs` و مرور مرورگری متناسب؛ این checker جای QA بصری نیست. روش تکمیلی در [verification](verification.md) است.

نقطه‌های انتخاب اسلاید بنر صفحهٔ اصلی در `electrical-theme.css` چیدمان ظرف و در `industrial-system.css` hit-area و نشانگر دیداری دارند؛ نشانگرها کوچک نگه داشته شده‌اند و hit-area حدود ۲۴ پیکسل برای لمس حفظ می‌شود.

ورودی تاریخ در نوار فیلتر مدیریت و فیلتر طرح‌های فروش عرض ترجیحی ۱۱rem دارد و به اندازهٔ فضای خالی ردیف کشیده نمی‌شود؛ در صفحهٔ باریک می‌تواند کوچک‌تر شود.

ستون عملیات جدول‌های DataTables پنل با کلاس `admin-datatable-actions` در renderer مشترک `Admin/wwwroot/js/admin-ui.js` مشخص می‌شود؛ دکمه‌ها با عرض ۱۸rem در یک ردیف می‌مانند. پنل، محفظهٔ جدول و ظرف DataTables حداقل‌عرض صفر دارند و جدول عریض در محفظهٔ خودش اسکرول افقی می‌گیرد؛ بنابراین عرض صفحه از قاب بیرون نمی‌زند. این رفتار در `Admin/wwwroot/css/admin-ui.css` نگهداری می‌شود.

در جدول «اتصال تنوع به محصول»، ویرایش و حذف فقط با آیکن نمایش داده می‌شوند؛ نام دکمه همچنان از راه `aria-label` و عنوان راهنما در دسترس است.

[پوسته](layout.md) · [فهرست UI](README.md) · [فهرست docs](../README.md)
