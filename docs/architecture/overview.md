# نمای معماری

Solution فعلی در `Shop.sln` سازمان‌دهی شده است. `Shop/` وب‌فروشگاه ASP.NET Core MVC، `Admin/` پنل مدیریتی و `Data/` کتابخانهٔ دامنه و persistence است. هر دو وب‌برنامه به `Data/Data.csproj` ارجاع می‌دهند؛ پس `Shop.Data` پروژه/پوشهٔ این مخزن نیست.

## مسیر درخواست

در فروشگاه، ثبت سرویس‌ها و middleware از `Shop/Program.cs` شروع می‌شود؛ route پیش‌فرض MVC به Controller و سپس View/partial می‌رسد. دادهٔ فروشگاه از سرویس‌ها و repositoryهای `Shop/Services/` و قراردادهای برنامه در `Data/Application/` عبور می‌کند. EF Core و نگاشت‌ها در `Data/Infrastructure/Persistence/` و entityها در `Data/Domain/` قرار دارند.

پنل `Admin/` یک برنامهٔ وب با Areas/Views و APIهای مدیریتی است. UI و routeهای پنل را از همان کد جاری ردیابی کنید؛ شباهت domain به معنای اشتراک layout یا route نیست.

## نقطهٔ ورود حسب کار

- صفحه/route: [نقشهٔ مسیرها](routing.md)
- منبع داده و مرزهای نمونه/واقعی: [منابع داده](data-sources.md)
- EF Core، migration و schema: [دیتابیس](database.md)
- UI و asset: [راهنمای frontend](../frontend/README.md)
- اجرا و کنترل کیفیت: [راهنمای توسعه](../development/README.md)

کد، DI و رفتار runtime مرجع نهایی‌اند؛ این صفحه صرفاً نقشهٔ مفهومی است. تاریخ مرور: ۱۲ نوامبر ۲۰۲۶.
