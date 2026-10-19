namespace Shop.Models;

public record PageModel(SeoModel Seo, string? ViewName = null, bool IsHome = false);

public sealed record PageModel<TModel>(TModel Data, SeoModel Seo, string? ViewName = null, bool IsHome = false) : PageModel(Seo, ViewName, IsHome);

public sealed record SeoModel(
    string Title,
    string Description,
    string? Keywords = null,
    string? CanonicalUrl = null,
    string Robots = "index, follow",
    string? ImageUrl = null,
    object? StructuredData = null,
    string OpenGraphType = "website",
    string? SocialImageAlt = null,
    string? ArticleSection = null,
    IReadOnlyList<SeoBreadcrumb>? Breadcrumbs = null);

public sealed record SeoBreadcrumb(string Name, string Url);

public sealed record FaqEntry(string Question, string Answer, string? LinkText = null, string? LinkUrl = null)
{
    public string StructuredAnswer => LinkText is null ? Answer : $"{Answer} {LinkText}";
}

public static class FaqCatalog
{
    public static IReadOnlyList<FaqEntry> Items { get; } =
    [
        new("چطور ابزار مناسب انتخاب کنم؟", "از فیلترهای صفحهٔ محصولات برای انتخاب گروه ابزار، برند، نوع تغذیه و بازهٔ قیمت استفاده کنید."),
        new("آیا امکان خرید آنلاین وجود دارد؟", "در این نسخه می‌توانید محصول را بررسی و سبد خرید را آماده کنید؛ پرداخت واقعی فعال نیست. برای خرید با فروشگاه تماس بگیرید."),
        new("چگونه سفارش را پیگیری کنم؟", "به صفحه پیگیری سفارش مراجعه کنید. تا اتصال سامانه آنلاین، پشتیبانی تلفنی پاسخ‌گوی شماست."),
        new("هزینه و زمان ارسال چقدر است؟", "این موارد با توجه به مقصد و موجودی، توسط فروشگاه تأیید می‌شود."),
        new("شرایط ضمانت و مرجوعی چیست؟", "شرایط ضمانت هر محصول روی کارت ضمانت و فاکتور درج می‌شود. راهنمای انصراف از خرید و بازگرداندن کالا را در", "رویه بازگرداندن کالا", "/legal/returns"),
        new("هزینه و زمان ارسال چگونه مشخص می‌شود؟", "پس از بررسی موجودی کالا و نشانی مقصد، روش ارسال، هزینه و زمان تقریبی پیش از تأیید سفارش با شما هماهنگ می‌شود."),
        new("برای پیگیری سفارش چه اطلاعاتی لازم است؟", "کد سفارش یا فاکتور و شماره همراهی را که هنگام هماهنگی خرید اعلام کرده‌اید آماده کنید؛ در نسخه فعلی پیگیری آنلاین به سامانه سفارش متصل نیست."),
        new("آیا فهرست علاقه‌مندی‌ها و سبد خرید باقی می‌ماند؟", "این نسخه برخی فهرست‌ها را در فضای ذخیره‌سازی مرورگر نگه می‌دارد. با پاک‌کردن داده‌های سایت یا استفاده از مرورگر یا دستگاه دیگر، ممکن است این فهرست‌ها در دسترس نباشند."),
        new("آیا فرم تماس پیام را برای فروشگاه ارسال می‌کند؟", "فرم‌های نسخه فعلی به سامانه ارسال پیام متصل نیستند. برای دریافت پاسخ، از راه‌های تماس درج‌شده در صفحه تماس با ما استفاده کنید.", "صفحه تماس با ما", "/contact"),
        new("هنگام دریافت کالا چه مواردی را بررسی کنم؟", "مدل، تعداد اقلام، سلامت بسته‌بندی و مدارک همراه کالا را با اطلاعات سفارش تطبیق دهید. در صورت مشاهده مغایرت یا آسیب، پیش از ارسال مجدد با پشتیبانی هماهنگ کنید.")
    ];
}

public sealed record ProductDetailsModel(int? Id, string? Name = null);

public sealed record MagazineDetailsModel(
    string Slug,
    string Category,
    string Kicker,
    string Title,
    string Summary,
    string Author,
    string PublishedAt,
    string ReadTime,
    string HeroImage,
    string ContentImage,
    string Lead,
    IReadOnlyList<MagazineSectionModel> Sections,
    string Conclusion);

public sealed record MagazineSectionModel(string Id, string Title, string Content);
