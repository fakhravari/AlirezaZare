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
        new("چطور ابزار مناسب انتخاب کنم؟", "فیلترهای گروه کالا، برند، منبع تغذیه و قیمت را برای محدودکردن گزینه‌ها به‌کار ببرید."),
        new("آیا امکان خرید آنلاین وجود دارد؟", "در این نسخه پرداخت واقعی فعال نیست؛ برای خرید با فروشگاه تماس بگیرید."),
        new("چگونه سفارش را پیگیری کنم؟", "تا اتصال سامانهٔ آنلاین، سفارش را از راه پشتیبانی تلفنی پیگیری کنید."),
        new("هزینه و زمان ارسال چقدر است؟", "مبلغ و زمان تحویل پس از بررسی مقصد و موجودی تأیید می‌شود."),
        new("شرایط ضمانت و مرجوعی چیست؟", "شرایط ضمانت را در کارت و فاکتور ببینید. راهنمای انصراف و بازگشت کالا در", "رویه بازگرداندن کالا", "/legal/returns"),
        new("هزینه و زمان ارسال چگونه مشخص می‌شود؟", "روش، هزینه و زمان تقریبی پس از بررسی مقصد و موجودی، پیش از تأیید سفارش هماهنگ می‌شود."),
        new("برای پیگیری سفارش چه اطلاعاتی لازم است؟", "کد سفارش یا فاکتور و شمارهٔ همراه ثبت‌شده را آماده کنید؛ پیگیری آنلاین هنوز به سامانه وصل نیست."),
        new("آیا فهرست علاقه‌مندی‌ها و سبد خرید باقی می‌ماند؟", "فهرست‌ها در مرورگر ذخیره می‌شوند؛ پاک‌کردن داده‌ها یا تغییر دستگاه ممکن است آن‌ها را حذف کند."),
        new("آیا فرم تماس پیام را برای فروشگاه ارسال می‌کند؟", "فرم تماس به سامانهٔ پیام متصل نیست. برای پاسخ از راه‌های تماس استفاده کنید.", "صفحه تماس با ما", "/contact"),
        new("هنگام دریافت کالا چه مواردی را بررسی کنم؟", "مدل، تعداد، بسته‌بندی و مدارک را با سفارش تطبیق دهید. برای مغایرت یا آسیب، پیش از ارسال مجدد هماهنگ کنید.")
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
