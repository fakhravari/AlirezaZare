# راه‌اندازی و اجرای محلی

این مخزن solution دات‌نت در `Shop.sln` و اسکریپت‌های کیفیت Node در `package.json` دارد. نسخهٔ هدف برنامه‌ها را از فایل csproj و نسخهٔ ابزارهای JS را از `package.json`/lockfile تعیین کنید؛ این راهنما نسخهٔ نصب‌شدهٔ دستگاه را تضمین نمی‌کند.

## ساخت

```powershell
dotnet restore Shop.sln --artifacts-path C:\AlirezaZare-build-artifacts\restore
dotnet build Shop.sln --artifacts-path C:\AlirezaZare-build-artifacts\build
```

اجرای فروشگاه محلی:

```powershell
dotnet run --project Shop/Shop.csproj --artifacts-path C:\AlirezaZare-build-artifacts\run
```

پروفایل اجرا، پورت و environment را از `Shop/Properties/launchSettings.json` بررسی کنید. پنل مستقل است و با پروژهٔ `Admin/Admin.csproj` اجرا می‌شود. هر دو به SQL Server و `Data/Data.csproj` وابسته‌اند؛ اتصال را از تنظیمات environment/User Secrets بگیرید و مقدار secret را در سند یا خروجی ترمینال ثبت نکنید. خروجی و intermediateهای .NET باید با `--artifacts-path` زیر `C:\AlirezaZare-build-artifacts\` باشند؛ در Visual Studio ساخت محلی را به این مسیر هدایت کنید و از مسیر پیش‌فرض `bin/obj` استفاده نکنید.

## بررسی‌های مستندات و UI

```powershell
npm ci
node docs/development/check-docs.mjs
node tools/check-ui.mjs
git diff --check
```

فهرست فرمان‌های کیفیت در [quality.md](quality.md) است. مسیرهای نمونهٔ صفحات را در [routing](../architecture/routing.md) ببینید و پیش از تست endpoint از تنظیم پایگاه محلی مطمئن شوید. هیچ فرمان مهاجرتی یا seed در راه‌اندازی عمومی اجرا نمی‌شود؛ migration را فقط برای پایگاه هدفی اجرا کنید که دسترسی‌اش را تعیین کرده‌اید.

## تشخیص خطا

- خطای DB: connection string محیط جاری و دسترسی SQL را بررسی کنید؛ کد 500 را با fallback دمو مخفی نکنید.
- پورت اشغال: پروفایل اجرا را بررسی یا پورت آزاد صریح انتخاب کنید.
- CSS/JS قدیمی: URL واقعی، `asp-append-version` و cache مرورگر را بررسی کنید.
- شکست npm check: ببینید کدام script شکست خورده؛ `npm run check` زنجیره‌ای است و شکست مرحلهٔ اول مراحل بعدی را اجرا نمی‌کند.

[فهرست توسعه](README.md) · [فهرست مستندات](../README.md)

بازبینی: ۱۲ نوامبر ۲۰۲۶.
