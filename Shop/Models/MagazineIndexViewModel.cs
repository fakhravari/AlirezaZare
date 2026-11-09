namespace Shop.Models;

public sealed record MagazineArchivePost(
    string Slug,
    string Branch,
    string Topic,
    string Area,
    string Category,
    string Title,
    string Summary,
    string Image,
    string? PublishedAt,
    int ReadMinutes);

public sealed record MagazineIndexViewModel(IReadOnlyList<MagazineArchivePost> Posts)
{
    public int TotalCount => Posts.Count;
    public int BuyingCount => Posts.Count(post => post.Branch == "buying");
    public int EducationCount => Posts.Count(post => post.Branch == "education");
    public int SafetyCount => Posts.Count(post => post.Branch == "safety");
    public int ToolsCount => Posts.Count(post => post.Area == "tools");
    public int ElectricalCount => Posts.Count(post => post.Area == "electrical");
    public int WorkshopCount => Posts.Count(post => post.Area == "workshop");
}