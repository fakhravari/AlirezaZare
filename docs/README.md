# راهنمای مطالعه و نقشهٔ مستندات

این صفحه نقطهٔ شروع شناخت پروژه است. ترتیب پیشنهادی را دنبال کنید تا از نمای کلی معماری به مسیر دقیق کد برسید و جزئیات تکراری بین راهنماها را دوباره نخوانید.

## خلاصهٔ یک‌دقیقه‌ای

- راه‌حل `Shop.sln` فعلاً فقط پروژهٔ وب `Shop/Shop.csproj` را دارد؛ برنامه با ASP.NET Core MVC و Razor اجرا می‌شود.
- مسیر درخواست معمول: **Controller → Model/داده → Razor View → layout مشترک → CSS/JavaScript**.
- `Shop/Program.cs` دیتابیس، EF Core، ورود سروری یا ثبت سفارش را راه‌اندازی نمی‌کند. کاتالوگ محصولات و بیشتر تعامل‌های فروشگاه در رابط کاربری مدیریت می‌شوند و بخشی از state در `localStorage` مرورگر نگهداری می‌شود.
- آمار ثابت مجله در `Shop/Models/JournalArticles.cs` تعریف شده و در این پروژه به‌روزرسانی عملیاتی نمی‌شود.
- هنگام تعارض مستندات با کد، **کد و رفتار runtime منبع نهایی‌اند**؛ راهنماها را پس از کشف اختلاف به‌روز کنید.

## مسیر یادگیری بر اساس نیاز

### شروع از صفر — حدود ۲۰ دقیقه

1. همین صفحه را بخوانید.
2. بخش‌های معماری، پوشه‌ها، routeها و چرخهٔ داده در [`DEVELOPER_ROADMAP.md`](DEVELOPER_ROADMAP.md) را مرور کنید.
3. یک مسیر ساده مثل `/about` را از `AboutController` تا View دنبال کنید.
4. ترتیب assetها را در `Shop/Views/Shared/_Shop.cshtml` ببینید.
5. `dotnet build Shop.sln` را اجرا کنید.

### تغییر ظاهر، responsive یا تعامل صفحه

1. route را در جدول «صفحه به View/CSS/JS» در [`UI_LAYER_REFERENCE.md`](UI_LAYER_REFERENCE.md) پیدا کنید.
2. markup واقعی را در View و partial مصرف‌شده بررسی کنید.
3. فقط asset بارگذاری‌شده و قانون CSS برنده را تغییر دهید؛ ترتیب واقعی در `_Shop.cshtml` ثبت شده است.
4. طبق چک‌لیست UI، حالت موبایل/دسکتاپ، keyboard و stateهای خالی/پر را بازبینی کنید.

### تغییر کالا، سبد یا منبع داده

1. بخش «وضعیت و منبع داده» و «قرارداد مشترک سبد» در [`DEVELOPER_ROADMAP.md`](DEVELOPER_ROADMAP.md) را بخوانید.
2. `HNUI` و مصرف‌کننده‌هایش را در [`UI_LAYER_REFERENCE.md`](UI_LAYER_REFERENCE.md) دنبال کنید.
3. مشخص کنید داده از C#، JavaScript، فایل JSON یا `localStorage` می‌آید؛ state محلی را دادهٔ سرور فرض نکنید.

### تغییر SEO، عنوان یا structured data

[`SEO_METADATA.md`](SEO_METADATA.md) را کنار `SeoModel`، `SeoController`، `SeoMetadataOptions` و `_SeoMetadata.cshtml` بخوانید.

### عامل کدنویسی / تغییر چندلایه

ابتدا [`AGENTS.md`](AGENTS.md) را برای workflow بخوانید؛ سپس این نقشه و Roadmap را مرور کنید. برای هر UI، راهنمای UI را هم بخوانید؛ برای metadata و structured data، راهنمای SEO را مبنا بگیرید.

## دسته‌بندی اسناد

| سند | برای چه کاری است؟ | جزئیات اصلی |
|---|---|---|
| [`DEVELOPER_ROADMAP.md`](DEVELOPER_ROADMAP.md) | شناخت معماری و پیدا کردن owner کد | راه‌اندازی، پوشه‌ها، routeها، جریان داده، روش تغییر و بدهی‌ها |
| [`UI_LAYER_REFERENCE.md`](UI_LAYER_REFERENCE.md) | پیدا کردن markup و asset برندهٔ صفحه | ترتیب CSS/JS، selectorها، breakpointها، eventها، storage و regression |
| [`SEO_METADATA.md`](SEO_METADATA.md) | تغییر metadata و structured data | مدل صفحه، partial مشترک، sitemap/robots و تنظیمات اختیاری |
| [`AGENTS.md`](AGENTS.md) | workflow لازم برای تغییر مخزن | ترتیب مطالعه، ایمنی، سبک و اعتبارسنجی |

## نقشهٔ کد در یک نگاه

```text
Shop/Program.cs
  └─ Controllers/            route، orchestration و ساخت PageModel
      └─ Models/              مدل صفحه، SEO و دادهٔ محتوای ثابت
          └─ Views/           Razor صفحه‌ها و partialهای مشترک
              └─ Views/Shared/_Shop.cshtml
                  ├─ css/core, css/layout, css/pages, css/store
                  └─ js/core, js/layout, js/store, js/pages
```

برای دنبال‌کردن قابلیت از ابتدا تا انتها، Controller و route را پیدا کنید، منبع داده را تعیین کنید، markup و renderer را دنبال کنید، سپس asset و script واقعی را از layout تأیید کنید.

## قواعد مهم پروژه

- فقط `Shop/` عضو فعلی solution است؛ `ClockTemplate/` را بدون تأیید source و عضویت در solution، برنامهٔ اجرایی فرض نکنید.
- URLهای تولیدشده lowercase هستند (`RouteOptions.LowercaseUrls`).
- محصولات، قیمت، موجودی، سفارش و پرداخت را واقعی فرض نکنید؛ وضعیت دموی هر جریان در Roadmap آمده است.
- برای UI از tokenها و componentهای موجود شروع کنید؛ selector عمومی تازه یا override انتهای فایل راه‌حل پیش‌فرض نیست.
- منوی دسته‌بندی خانه و صفحات داخلی markup جدا دارند؛ در دسکتاپ پنل داخلی زیر هدر و در قاب مرکزی باز می‌شود، و حالت hover/فعال با زمینهٔ زرد و آیکن سرمه‌ای هم‌خوان می‌ماند. جزئیات در [`UI_LAYER_REFERENCE.md`](UI_LAYER_REFERENCE.md) است.
- پروژه test project مستقل ندارد. نتیجهٔ build را به‌جای تست رفتاری یا بصری معرفی نکنید.

## اعتبار مستندات

این نقشه و راهنماهای فنی در **۱۹ اکتبر ۲۰۲۶** با ساختار فعلی مخزن تطبیق داده شده‌اند. breakpointها، ترتیب assetها و مسیرها snapshot هستند؛ پیش از تغییر، آن‌ها را با فایل جاری و runtime دوباره بررسی کنید.
