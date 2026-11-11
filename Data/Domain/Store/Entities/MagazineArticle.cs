namespace Shop.Data.Domain.Store.Entities;

public sealed class MagazineArticle
{
    public long Id { get; set; }
    public string ArticleKey { get; set; } = "";
    public long IdCategory { get; set; }
    public long IdBranch { get; set; }
    public long IdTopic { get; set; }
    public long IdArea { get; set; }
    public MagazineArticleCategory Category { get; set; } = null!;
    public MagazineArticleBranch Branch { get; set; } = null!;
    public MagazineArticleTopic Topic { get; set; } = null!;
    public MagazineArticleArea Area { get; set; } = null!;
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
    public string? Kicker { get; set; }
    public string? ReadTimeText { get; set; }
    public List<MagazineArticleSection> Sections { get; set; } = [];
}

public sealed class MagazineArticleCategory { public long Id { get; set; } public string CategoryKey { get; set; } = ""; }
public sealed class MagazineArticleBranch { public long Id { get; set; } public string BranchKey { get; set; } = ""; }
public sealed class MagazineArticleTopic { public long Id { get; set; } public string TopicKey { get; set; } = ""; }
public sealed class MagazineArticleArea { public long Id { get; set; } public string AreaKey { get; set; } = ""; }

public sealed class MagazineArticleSection
{
    public long Id { get; set; }
    public long IdMagazineArticle { get; set; }
    public string Heading { get; set; } = "";
    public string Body { get; set; } = "";
    public int SortOrder { get; set; }
}

public sealed class MagazineArticleSource { public long Id { get; set; } public long IdMagazineArticle { get; set; } public string Title { get; set; } = ""; public string Url { get; set; } = ""; public int SortOrder { get; set; } }
public sealed class MagazineArticleTag { public long Id { get; set; } public long IdMagazineArticle { get; set; } public string Label { get; set; } = ""; public string Filter { get; set; } = ""; public int SortOrder { get; set; } }
public sealed class MagazineArticleRelated { public long Id { get; set; } public long IdMagazineArticle { get; set; } public long IdRelatedArticle { get; set; } public string Category { get; set; } = ""; public string Title { get; set; } = ""; public string ImagePath { get; set; } = ""; public int SortOrder { get; set; } }
public sealed class MagazineArticleMetric { public long IdMagazineArticle { get; set; } public int Views { get; set; } public int Likes { get; set; } public decimal AverageRating { get; set; } public int Comments { get; set; } }

