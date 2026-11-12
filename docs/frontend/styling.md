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

[پوسته](layout.md) · [فهرست UI](README.md) · [فهرست docs](../README.md)
