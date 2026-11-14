# JavaScript و state مرورگر

اسکریپت‌های فروشگاه در `Shop/wwwroot/shop/js/` به core، layout، store و pages دسته‌بندی شده‌اند. مالک هر component در `tools/ui-owners.json` و ترتیب load در [پوسته](layout.md) است.

## قواعد مصرف

- ابتدا script و قرارداد event/API موجود را پیدا کنید؛ handler مشترک موازی نسازید.
- state مرورگر را با سرور یکسان فرض نکنید. برای قیمت، cart و حساب مرجع حوزه را بخوانید.
- ورودی API و دادهٔ ذخیره‌شده را normalize/validate کنید و از درج HTML ناامن بپرهیزید.
- handler صفحه را به root همان صفحه محدود و نصب چندباره را مهار کنید.
- timeout، خطای شبکه، پاسخ دیررس و ارسال تکراری را مدیریت کنید.

`ui-core.js` API عمومی `window.HNUI` را فراهم می‌کند؛ `storage.js` و `cart.js` ابزار مشترک فروشگاه‌اند. قراردادهای مربوط به commerce در [سبد](../store/cart.md)، حساب در [پروفایل](../account/profile.md) و داده در [منابع داده](../architecture/data-sources.md) مرجع‌اند.

انتخاب‌گرهای بومی در layoutهای اصلی Shop و Admin به Select2 فارسی/RTL تبدیل می‌شوند؛ راه‌انداز مشترک هر برنامه selectهای جدیدی را که بعداً به DOM اضافه می‌شوند نیز می‌گیرد. برای select داخل modal، dropdown باید در خود modal بماند. پس از تغییر programmatic گزینه‌ها، مقدار انتخاب‌شده را با رویداد `change` هماهنگ کنید؛ برای کنترل‌های استثنایی که باید native بمانند از `data-native-select` استفاده کنید.

فرم تماس در `pages/contact.js`، پیگیری سفارش در `pages/tracking.js` و پیام‌های پروفایل در `pages/account-page.js` از API هم‌مبدأ با `HNUI.api.request` استفاده می‌کنند. فرم خبرنامه در `layout/main.js` نشست را به سرور واگذار می‌کند تا ترجیح حساب یا ایمیل مهمان در مقصد درست ذخیره شود. جزئیات مسیر و مالکیت داده در [راهنمای حساب](../account/profile.md)، [صفحه‌های محتوا](../content/pages.md) و [منابع داده](../architecture/data-sources.md) است.

پیش از تحویل، checker UI، syntax/test موجود و Console/Network صفحه را بررسی کنید؛ جزئیات [کیفیت](../development/quality.md).

[فهرست UI](README.md) · [فهرست docs](../README.md)

بازبینی: ۱۴ نوامبر ۲۰۲۶.
