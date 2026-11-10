namespace Shop.Data.Domain.Store.Entities;

public sealed class MagazineArticle
{
    public long Id { get; set; }
    public string ArticleKey { get; set; } = "";
    public string Category { get; set; } = "";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string? Author { get; set; }
    public DateOnly? PublishedAt { get; set; }
    public int? ReadTimeMinutes { get; set; }
    public string? HeroImagePath { get; set; }
    public string? ContentImagePath { get; set; }
    public string? Lead { get; set; }
    public string? Conclusion { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? Branch { get; set; }
    public string? Topic { get; set; }
    public string? Area { get; set; }
    public string? Kicker { get; set; }
    public string? ReadTimeText { get; set; }
    public List<MagazineArticleSection> Sections { get; set; } = [];
}

public sealed class MagazineArticleSection
{
    public long Id { get; set; }
    public long IdMagazineArticle { get; set; }
    public string Heading { get; set; } = "";
    public string Body { get; set; } = "";
    public int SortOrder { get; set; }
}

