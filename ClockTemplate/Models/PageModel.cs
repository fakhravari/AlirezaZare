namespace ClockTemplate.Models;

public record PageModel(SeoModel Seo, string? ViewName = null, bool IsHome = false);

public sealed record PageModel<TModel>(TModel Data, SeoModel Seo, string? ViewName = null, bool IsHome = false) : PageModel(Seo, ViewName, IsHome);

public sealed record SeoModel(string Title, string Description, string? Keywords = null, string? CanonicalUrl = null, string Robots = "index, follow", string? ImageUrl = null);

public sealed record ProductDetailsModel(int? Id);

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
