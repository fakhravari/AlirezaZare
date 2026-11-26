namespace Shop.Data.Application.Store.Contracts;

public interface IStoreMagazineAdminData
{
    Task<IReadOnlyList<MagazineArticleListData>> ReadArticlesAsync(CancellationToken cancellationToken);
    Task<MagazineArticleEditData?> ReadArticleAsync(long id, CancellationToken cancellationToken);
    Task<MagazineArticleSaveResult> SaveArticleAsync(long? id, MagazineArticleUpdate article,
        CancellationToken cancellationToken);
    Task<MagazineArticleDeleteResult> DeleteArticleAsync(long id, CancellationToken cancellationToken);
}

public sealed record MagazineArticleListData(decimal Id, string ArticleKey, string Title, string Category,
    bool IsPublished, int SectionCount);
public sealed record MagazineArticleSectionData(string Heading, string Body);
public sealed record MagazineArticleEditData(string ArticleKey, string Category, string Title, string Summary,
    string? Author, string? Kicker, string Branch, string Topic, string Area, string? HeroImagePath,
    string? ContentImagePath, int? ReadTimeMinutes, string? ReadTimeText, string? Lead, string? Conclusion,
    bool IsPublished, IReadOnlyList<MagazineArticleSectionData> Sections);
public sealed record MagazineArticleUpdate(string ArticleKey, string Category, string Title, string Summary,
    string? Author, string? Kicker, string Branch, string Topic, string Area, string? HeroImagePath,
    string? ContentImagePath, int? ReadTimeMinutes, string? ReadTimeText, string? Lead, string? Conclusion,
    bool IsPublished, IReadOnlyList<MagazineArticleSectionData> Sections);
public enum MagazineArticleSaveResultKind { Saved, DuplicateKey, NotFound }
public sealed record MagazineArticleSaveResult(MagazineArticleSaveResultKind Kind, decimal? Id = null);
public enum MagazineArticleDeleteResult { Deleted, NotFound, HasInteractions }
