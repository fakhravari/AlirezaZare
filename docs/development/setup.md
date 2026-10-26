# راه‌اندازی و اعتبارسنجی پروژه

پیش‌نیاز: .NET SDK سازگار با `net10.0`؛ برای کنترل‌های UI و تست‌ها Node.js 24 و وابستگی‌های ثبت‌شده در package-lock.json.

```powershell
dotnet restore Shop.sln
dotnet build Shop.sln
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project Shop/Shop.csproj --no-launch-profile --urls http://localhost:5100
```

درخواست‌های صفحه‌ای نمونه برای بررسی دستی: `/`, `/search`, `/products/cordless-hammer-drill-1`, `/cart`, `/checkout`, `/compare`, `/profile`, `/profile/orders`, `/profile/favorites`, `/magazine`, `/brands/ronix`.

- در تغییر CSS/JS دست‌کم صفحهٔ هدف، یک صفحهٔ دیگر با همان component مشترک و اندازهٔ موبایل را بررسی کنید.
- در تغییر route/controller مدل، `dotnet build Shop.sln` را اجرا کنید.
- در تغییر JS، syntax و حالت‌های no-data / malformed storage را بررسی کنید.
- پروژهٔ `tests/Shop.Contracts` در solution قرارداد C# را بررسی می‌کند؛ تست‌های JS/HTTP/مرورگر نیز اضافه شده‌اند. نصب و اجرای بررسی‌ها در [کیفیت](quality.md) است؛ موفقیت build را معادل تست مرورگر اعلام نکنید.
- stylesheet و scriptهای اختصاصی View از `asp-append-version="true"` استفاده می‌کنند؛ مسیرهای CSS/JS قالب در Razor با `~/shop/` و رسانه‌های داده با `~/content/` آغاز می‌شوند تا به مسیر جاری وابسته نباشند. اگر مرورگر CSS/JS قدیمی نشان داد، ابتدا URL نهایی و cache-busting واقعی asset را بررسی کنید.

## ساخت هنگام اجرای سایت روی Windows

اگر فایل اجرایی یا DLL پوشهٔ Debug توسط سایت در حال اجرا قفل باشد، build همان خروجی با MSB3021/MSB3027 شکست می‌خورد. می‌توان بدون توقف آن سایت، پیکربندی Release را ساخت و تست کرد:

```powershell
dotnet build Shop.sln -c Release
dotnet run --no-build -c Release --project tests/Shop.Contracts
$env:SHOP_TEST_DLL = "Shop/bin/Release/net10.0/Shop.dll"
npm.cmd run test:http
Remove-Item Env:SHOP_TEST_DLL
```

این راه فقط وقتی خروجی Release در حال استفاده نباشد از قفل Debug مستقل است. تست HTTP پورت آزاد خودش را انتخاب می‌کند؛ لازم نیست سایت پورت ۵۱۰۰ را متوقف کنید.

## بررسی مستندات

برای checker مستندات به Node.js نیاز است؛ این ابزار وابستگی npm ندارد و به اجرای سایت نیاز ندارد:

```powershell
node docs/development/check-docs.mjs
git diff --check
```

checker محل راهنماها، وجود README هر بخش، لینک محلی به سند/کد و دسترسی راهنماها از فهرست اصلی را بررسی می‌کند. صحت رفتار برنامه، URL اینترنتی یا اندازهٔ واقعی تصویر را تأیید نمی‌کند. تغییر صرفاً مستندات به build برنامه نیاز ندارد؛ اگر نمونه یا مسیر کد عوض شد، اعتبارسنجی مرتبط با همان تغییر را هم انجام دهید.

ورودی راه‌اندازی در [Program.cs](../../Shop/Program.cs)، پروژه در [Shop.csproj](../../Shop/Shop.csproj) و تنظیمات اجرای محلی در [launchSettings.json](../../Shop/Properties/launchSettings.json) قرار دارد. profile با نام `Shop` محیط Development و پورت ۵۱۰۰ را تعیین می‌کند. فرمان `--no-launch-profile` تنظیمات این profile را نمی‌خواند؛ به همین دلیل در نمونهٔ بالا محیط توسعه صریحاً در نشست PowerShell تنظیم شده است. بدون تعیین محیط، پیش‌فرض ASP.NET Core برابر Production است.

---

[فهرست این بخش](README.md) · [فهرست مستندات](../README.md)

آخرین تطبیق با کد: ۲۶ اکتبر ۲۰۲۶.
