namespace Shop.Data.Application.Store.Contracts;

public interface IStoreProductBadgeAdminData
{
    Task<IReadOnlyList<ProductBadgeData>> ReadBadgesAsync(CancellationToken cancellationToken);
    Task<(int Total, IReadOnlyList<ProductBadgeProductData> Items)> ReadProductsAsync(string search, long? badgeId,
        bool withoutBadge, int offset, int pageSize, CancellationToken cancellationToken);
    Task<ProductBadgeSaveResult> SaveBadgeAsync(long? id, string title, string backgroundColor, string textColor,
        string borderColor, CancellationToken cancellationToken);
    Task<ProductBadgeDeleteResult> DeleteBadgeAsync(long id, CancellationToken cancellationToken);
    Task<ProductBadgeAssignmentResult> AssignAsync(long productId, long? badgeId,
        CancellationToken cancellationToken);
}

public sealed record ProductBadgeData(decimal Id, string Title, string BackgroundColor, string TextColor,
    string BorderColor, int ProductCount);
public sealed record ProductBadgeProductData(decimal Id, string Name, string Code, decimal? BadgeId);
public sealed record ProductBadgeResult(decimal Id, string Title, string BackgroundColor, string TextColor,
    string BorderColor);
public enum ProductBadgeSaveResultKind { Saved, Duplicate, NotFound }
public sealed record ProductBadgeSaveResult(ProductBadgeSaveResultKind Kind, ProductBadgeResult? Badge = null);
public enum ProductBadgeDeleteResult { Deleted, NotFound, Assigned }
public enum ProductBadgeAssignmentResult { Saved, ProductNotFound, BadgeNotFound }
