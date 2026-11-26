namespace Shop.Data.Application.Store.Contracts;

public interface IStoreHomeCollectionAdminData
{
    Task<HomeCollectionAdminData> ReadAsync(string search, int page, CancellationToken cancellationToken);
    Task<HomeCollectionMutationResult> AddProductAsync(decimal productId, CancellationToken cancellationToken);
    Task<HomeCollectionMutationResult> UpdateProductAsync(long id, int sortOrder, string operation,
        CancellationToken cancellationToken);
    Task<HomeCollectionMutationResult> AddCategoryAsync(long categoryId, CancellationToken cancellationToken);
    Task<HomeCollectionMutationResult> UpdateCategoryAsync(long id, int sortOrder, bool isActive, string operation,
        CancellationToken cancellationToken);
}

public sealed record HomeCollectionProductData(decimal Id, decimal ProductId, string Name, string Code, int SortOrder,
    bool IsStoreEnabled);
public sealed record HomeCollectionCategoryData(long Id, string Name);
public sealed record HomeCollectionTabData(long Id, long CategoryId, string Name, int SortOrder, bool IsActive,
    bool CategoryActive);
public sealed record HomeCollectionCandidateData(decimal Id, string Name, string Code);
public sealed record HomeCollectionAdminData(IReadOnlyList<HomeCollectionProductData> Products,
    IReadOnlyList<HomeCollectionTabData> Tabs, IReadOnlyList<HomeCollectionCategoryData> Categories,
    IReadOnlyList<HomeCollectionCandidateData> Candidates, string Query, int Page, int Total, bool TabsReady);
public enum HomeCollectionMutationResult { Saved, NotFound, InvalidProduct, InvalidCategory, SortOrderLimit, InvalidOperation }
