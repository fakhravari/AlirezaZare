# پروفایل، آدرس و سفارش‌ها

## مسیر و منابع

[ProfileController](../../Shop/Controllers/ProfileController.cs) مسیرهای `/profile`، `/profile/orders`، `/profile/addresses` و `/profile/favorites` را در [یک View مشترک](../../Shop/Views/Profile/Index.cshtml) نمایش می‌دهد. عنوان و breadcrumb از route ساخته می‌شوند. `/profile/edit` به پروفایل هدایت می‌شود و `AccountController` alias سازگاری است؛ View مستقلی برای آن نسازید. metadata پروفایل در وضعیت فعلی `noindex, nofollow` است.

| مسئولیت | مالک |
|---|---|
| ترکیب بخش‌ها و dialog ویرایش | `Views/Profile/Index.cshtml`؛ بخشی از ذخیرهٔ پروفایل inline است |
| محتوای سفارش‌ها و فاکتور | [_Orders.cshtml](../../Shop/Views/Profile/_Orders.cshtml)، dialog فاکتور در `account-page.js` و `orders-pagination.js` |
| route/hash، آدرس و overview | [account-page.js](../../Shop/wwwroot/shop/js/pages/account-page.js) |
| استان و شهر | `iran-places.js` → `address-fields.js`؛ [_AddressForm](../../Shop/Views/Shared/_AddressForm.cshtml) |
| ظاهر صفحه | `pages/account-page.css`؛ قواعد مشترک فرم/آدرس در `checkout.css` و `shared-login.css` |
| حساب داخل هدر | `layout/shared-account.css` و `layout/main.js`؛ با صفحهٔ پروفایل یک مالک نیست |

مسیرهای فایل و ترتیب دقیق scriptها در [نقشهٔ صفحه‌ها](../architecture/routing.md) و [پوسته](../frontend/layout.md) است.

## state و رفتار فعلی

پروفایل در `hn-profile`، آدرس‌ها در کلیدهای مشترک آدرس، علاقه‌مندی و مقایسه در state فهرست‌ها نگهداری می‌شوند؛ قرارداد کامل در [JavaScript](../frontend/javascript.md) است. ورود آزمایشی مرورگر احراز هویت سرور نیست و آمار سفارش/حساب، گزارش عملیاتی backend محسوب نمی‌شود.

overview تعداد مقایسه را از فهرست محلی می‌خواند. پنل فعالیت‌های اخیر و ثبت تاریخچهٔ تغییرات حذف شده‌اند. اگر مرورگر از نسخهٔ قبلی دادهٔ `hn-profile-activity` داشته باشد، همان داده دست‌نخورده باقی می‌ماند.

فهرست سفارش‌ها دادهٔ نمایشی است؛ نام کالا در کارت‌ها نشان داده نمی‌شود. جزئیات فاکتور در dialog بزرگ master-detail خلاصهٔ شماره، تاریخ، وضعیت، تعداد کالا و مبلغ را نشان می‌دهد. جزئیات تحویل به راهنمای پیگیری عمومی می‌رود و دادهٔ زندهٔ مرسوله ندارد؛ فاکتور نیز نمونهٔ نمایشی است.

کوپن‌های فعال فعلی در account-page.js تعریف شده‌اند؛ `HADI10` با تخفیف ۱۰٪ یک نمونه است. انتخاب کد در سبد با `hn-demo-coupon` هماهنگ می‌شود؛ برای واقعی‌شدن کوپن باید اعتبارسنجی و شرایط مصرف سرور اضافه شود.

## ویرایش و آدرس

ویرایش پروفایل native dialog با `.shared-login-modal.profile-edit-dialog` است؛ فرم ورود سراسری `#loginModal` در `_Panels` مسیر جدا دارد. هنگام تغییر بستن dialog، focus، validation و اسکرول داخلی را حفظ کنید. فرم آدرس در Cart و Profile مشترک است؛ Checkout فقط آدرس انتخاب‌شده را نشان می‌دهد.

## بازبینی

ورود مستقیم به هر route، refresh، back/forward و تغییر hash باید همان بخش را فعال کند. موبایل با نام/نام‌خانوادگی بلند، فیلد نامعتبر، dialog کوتاه‌قد، دکمهٔ بستن و ذخیره بررسی شود. سفارش‌ها با pagination، فهرست خالی، چند ردیف و فاصلهٔ متن‌ها بررسی شوند. علاقه‌مندی، تعداد مقایسه، کوپن و آدرس را در همان tab و tab دیگر نیز کنترل کنید؛ دادهٔ نمونه را سفارش واقعی معرفی نکنید.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۲۴ اکتبر ۲۰۲۶.
