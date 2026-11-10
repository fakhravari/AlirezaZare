namespace Shop.Database.Domain.Store.Entities;

public sealed class StoreContentPage
{
    public long Id { get; set; }
    public string PageKey { get; set; } = "";
    public string RoutePath { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string ContentSource { get; set; } = "Razor";
    public bool IsPublished { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public List<StoreContentPageSection> Sections { get; set; } = [];
}

public sealed class StoreContentPageSection
{
    public long Id { get; set; }
    public long IdContentPage { get; set; }
    public string SectionKey { get; set; } = "";
    public string? Heading { get; set; }
    public string? Body { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
}

public sealed class StoreFaqItem
{
    public long Id { get; set; }
    public long IdContentPage { get; set; }
    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";
    public string? LinkText { get; set; }
    public string? LinkUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
}
