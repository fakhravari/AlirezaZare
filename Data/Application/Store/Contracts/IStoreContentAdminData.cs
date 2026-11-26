namespace Shop.Data.Application.Store.Contracts;

public interface IStoreContentAdminData
{
    Task<IReadOnlyList<ManagedPageListRow>> ReadManagedPagesAsync(CancellationToken cancellationToken);
    Task<ManagedPageEditData?> ReadManagedPageAsync(string pageKey, CancellationToken cancellationToken);
    Task<bool> SaveManagedPageAsync(string pageKey, string title, string description, bool published,
        IReadOnlyList<ManagedPageSectionEditData> sections, CancellationToken cancellationToken);
    Task<CategoryContentData?> ReadCategoryContentAsync(string key, CancellationToken cancellationToken);
    Task<bool> SaveCategoryContentAsync(string key, string intro, string highlights, string specs,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<StoreFaqAdminRow>> ReadFaqItemsAsync(CancellationToken cancellationToken);
    Task<StoreFaqSaveResult> SaveFaqAsync(long? id, StoreFaqAdminEdit edit, CancellationToken cancellationToken);
    Task<bool> DeleteFaqAsync(long id, CancellationToken cancellationToken);
}

public sealed record ManagedPageListRow(string PageKey, string RoutePath, string Title, bool Published,
    int SectionCount);
public sealed record ManagedPageSectionEditData(string SectionKey, string? Heading, string Body, bool Published);
public sealed record ManagedPageEditData(string Title, string Description, bool Published,
    IReadOnlyList<ManagedPageSectionEditData> Sections);
public sealed record CategoryContentData(string Intro, string Highlights, string Specs);
public sealed record StoreFaqAdminRow(long Id, string Question, string Answer, string? LinkText, string? LinkUrl,
    int SortOrder, bool Published);
public sealed record StoreFaqAdminEdit(string Question, string Answer, string? LinkText, string? LinkUrl, int SortOrder,
    bool Published);
public enum StoreFaqSaveResult { Saved, NotFound, FaqPageMissing }
