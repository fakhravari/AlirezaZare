# وضعیت مستندات و ادامهٔ کار

## بازنویسی مستندات — ۱۲ نوامبر ۲۰۲۶

- **وضعیت:** هر ۳۷ فایل Markdown زیر `docs/` بازنویسی/بازبینی شدند؛ ساختار موجود ۹ بخش دارد. `docs/development/check-docs.mjs` ابزار اجرایی است و سند Markdown محسوب نمی‌شود.
- **ناوبری:** مسیر شروع و مراحل ادامه در [README اصلی](../README.md) و [docs/AGENTS.md](../AGENTS.md) قرار دارد.
- **حفظ تغییرهای پیشین:** اصلاح‌های قیمت محصول و طرح فروش در همین کار در قرارداد تاریخچهٔ قیمت/شناسهٔ نسخه order ادغام شد؛ تغییرهای خارج docs حفظ شدند.
- **اعتبارسنجی پیوندها:** `node docs/development/check-docs.mjs` موفق؛ ۳۷ Markdown، ۹ بخش و ۱۹۵ پیوند محلی تأیید شد.
- **اعتبارسنجی UI:** `node tools/check-ui.mjs` موفق؛ ownerها و قرارداد بارگذاری بررسی شدند.
- **Patch:** `git diff --check` موفق؛ Git فقط هشدار پایان‌خط LF/CRLF نشان داد.
- **خروجی‌های موقت:** build، intermediate، `.tmp-build`، `.vs` و گزارش Playwright از checkout به `C:\AlirezaZare-build-artifacts\workspace-temp\` منتقل شدند؛ خروجی‌های قبلی چهار پروژه در `C:\AlirezaZare-build-artifacts\{Admin,Cdn,Data,Shop}\artifacts\` هستند. Git status تأیید کرد مسیرهای پروژه خالی شدند. `node_modules` باقی است چون وابستگی نصب‌شدهٔ npm است، نه خروجی موقت کار.
- **قاعدهٔ دائمی:** فایل‌های موقت و خروجی‌های آینده باید بیرون checkout، زیر `C:\AlirezaZare-build-artifacts\` ساخته شوند؛ این قاعده در [AGENTS.md مخزن](../../AGENTS.md) و [روند docs](../AGENTS.md) ثبت شده است. `.tmp-build/` نیز به ignore اضافه شد.
- **کنترل زنجیره‌ای:** پس از انتقال `.tmp-build`، `npm run check` موفق شد: scanner دو appsettings tracked مجاز را پذیرفت، checker UI و checker مستندات نیز سبز شدند.

## چطور کار را ادامه بدهیم

۱. `git status --short` و diff را بخوانید؛ تغییرهای موجود را حفظ کنید.
۲. هدف جاری را از درخواست کاربر بگیرید و از [فهرست اصلی](../README.md) وارد بخش مربوط شوید.
۳. route را تا source داده و View/asset ردیابی کنید و `tools/ui-owners.json` را برای UI ببینید.
۴. پس از تغییر، guide حوزه و این وضعیت را با شواهد تازه هماهنگ کنید.

این صفحه وضعیت محیط/دیتابیس یا انتشار را ادعا نمی‌کند؛ آن‌ها باید در محیط واقعی و برای همان درخواست تأیید شوند.
