# JavaScript و state مرورگر

اسکریپت‌های فروشگاه در `Shop/wwwroot/shop/js/` به core، layout، store و pages دسته‌بندی شده‌اند. مالک هر component در `tools/ui-owners.json` و ترتیب load در [پوسته](layout.md) است.

## قواعد مصرف

- ابتدا script و قرارداد event/API موجود را پیدا کنید؛ handler مشترک موازی نسازید.
- state مرورگر را با سرور یکسان فرض نکنید. برای قیمت، cart و حساب مرجع حوزه را بخوانید.
- ورودی API و دادهٔ ذخیره‌شده را normalize/validate کنید و از درج HTML ناامن بپرهیزید.
- handler صفحه را به root همان صفحه محدود و نصب چندباره را مهار کنید.
- timeout، خطای شبکه، پاسخ دیررس و ارسال تکراری را مدیریت کنید.

`ui-core.js` API عمومی `window.HNUI` را فراهم می‌کند؛ `storage.js` و `cart.js` ابزار مشترک فروشگاه‌اند. قراردادهای مربوط به commerce در [سبد](../store/cart.md)، حساب در [پروفایل](../account/profile.md) و داده در [منابع داده](../architecture/data-sources.md) مرجع‌اند.

پیش از تحویل، checker UI، syntax/test موجود و Console/Network صفحه را بررسی کنید؛ جزئیات [کیفیت](../development/quality.md).

[فهرست UI](README.md) · [فهرست docs](../README.md)

بازبینی: ۱۲ نوامبر ۲۰۲۶.
