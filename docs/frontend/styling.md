# CSS و مالکیت استایل

CSS فروشگاه در `Shop/wwwroot/shop/css/` دسته‌بندی شده است: `core/`, `layout/`, `store/`, `pages/`. پنل Admin مالک جداگانه در `Admin/wwwroot/css/` دارد. مرجع ownerهای UI، `tools/ui-owners.json` است.

## روش تغییر

1. selector را از markup واقعی پیدا کنید و همهٔ تعریف‌ها را `rg` کنید.
2. layout و View را برای cascade و stylesheetهای واقعاً بارشده بررسی کنید.
3. قانون را در فایل owner اصلاح کنید؛ قاعدهٔ page-specific را به shared stylesheet منتقل نکنید مگر مالکیت مشترک روشن باشد.
4. selectorهای عمومی، `!important` و override انتهایی جدید را فقط با دلیل محدود و ثبت‌شده اضافه کنید.
5. RTL، breakpointهای نزدیک تغییر، focus/hover، reduced motion و سرریز را مرور کنید.

توکن‌ها و پایهٔ مشترک در `css/core/design-tokens.css`, `components.css`, `ui-core.css` و `site.css` هستند؛ منبع دقیق هر component در owner manifest آمده است. کپی کردن قانون مشترک در چند صفحه باعث drift می‌شود.

بازبینی: `node tools/check-ui.mjs` و مرور مرورگری متناسب؛ این checker جای QA بصری نیست. روش تکمیلی در [verification](verification.md) است.

دکمه‌های عملیات در `admin-ui.css` با `.admin-action-btn` یکدست می‌شوند و همهٔ گونه‌ها حاشیهٔ قابل مشاهده دارند؛ حاشیهٔ عملیات حساس مانند حذف و فعال‌سازی رنگ معنایی خود را حفظ می‌کند. hover حذف باید روی زمینهٔ قرمز روشن، رنگ متن قرمز تیره را حفظ کند؛ selector داخل `#content` عمداً از رنگ سفید پیش‌فرض قالب برنده می‌شود.

سویچ وضعیت پنل مدیریت در `admin-ui.css` مالک مشترک دارد: ورودی checkbox با `data-admin-switch` و `role="switch"` همچنان مقدار و رفتار بومی فرم را حفظ می‌کند. برای ورودی‌های ساخته‌شده با JavaScript از `AdminUI.initStatusSwitch` استفاده می‌شود؛ انتخاب ردیف‌ها، دسترسی‌ها و فیلترهای چندگزینه‌ای checkbox باقی می‌مانند.

[پوسته](layout.md) · [فهرست UI](README.md) · [فهرست docs](../README.md)
`nAdmin section navigation shares the nav-pills and geography tab styles in Admin/wwwroot/css/admin-ui.css, including active, hover, keyboard focus, and wrapping on narrow screens.
