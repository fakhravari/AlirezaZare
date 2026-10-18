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
        new("شرایط ضمانت و مرجوعی چیست؟", "شرایط ضمانت هر محصول روی کارت ضمانت و فاکتور درج می‌شود. راهنمای انصراف از خرید و بازگرداندن کالا را در", "رویه بازگرداندن کالا", "/legal/returns")
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
