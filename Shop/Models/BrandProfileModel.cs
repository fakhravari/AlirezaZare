namespace Shop.Models;

public sealed record BrandProfileModel(
    string Slug,
    string Name,
    string PersianName,
    string? LogoFile,
    string Summary,
    string Focus,
    IReadOnlyList<string> Highlights)
{
    public int? FoundedYear { get; init; }
    public string? OfficialUrl { get; init; }
    public string? Editorial { get; init; }
    public BrandEditorialContent EditorialContent { get; init; } = new([], [], string.Empty);
    public BrandEditorialDocument? SourceEditorial { get; init; }
}

public sealed record BrandEditorialDocument
{
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string PersianName { get; init; } = string.Empty;
    public string Established { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string OfficialUrl { get; init; } = string.Empty;
    public IReadOnlyList<string> CompanyIntroductions { get; init; } = [];
    public string Specialization { get; init; } = string.Empty;
    public string FeaturedProducts { get; init; } = string.Empty;
    public IReadOnlyList<string> ProductFamilies { get; init; } = [];
    public IReadOnlyList<string> Strengths { get; init; } = [];
    public string Audience { get; init; } = string.Empty;
    public string ShoppingAdvice { get; init; } = string.Empty;
    public string FullPastedText { get; init; } = string.Empty;
    public string FullDocumentText { get; init; } = string.Empty;
}

public sealed record BrandEditorialContent(
    IReadOnlyList<string> ProductFamilies,
    IReadOnlyList<string> Strengths,
    string Audience)
{
    public string Introduction { get; init; } = string.Empty;
    public string Specialization { get; init; } = string.Empty;
    public string ShoppingAdvice { get; init; } = string.Empty;
}