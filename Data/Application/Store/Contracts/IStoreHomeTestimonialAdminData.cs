namespace Shop.Data.Application.Store.Contracts;

public interface IStoreHomeTestimonialAdminData
{
    Task<IReadOnlyList<HomeTestimonialAdminRow>> ReadAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<HomeTestimonialProduct>> ReadProductsAsync(CancellationToken cancellationToken);
    Task<bool> ProductExistsAsync(decimal productId, CancellationToken cancellationToken);
    Task<long> CreateAsync(HomeTestimonialReviewData input, CancellationToken cancellationToken);
    Task<bool> UpdateReviewAsync(long id, HomeTestimonialReviewData input, CancellationToken cancellationToken);
    Task<bool> UpdateDisplayAsync(long id, bool isActive, int sortOrder, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(long id, CancellationToken cancellationToken);
    Task<string?> ReadImagePathAsync(long id, CancellationToken cancellationToken);
    Task<bool> SetImagePathAsync(long id, string? path, CancellationToken cancellationToken);
}

public sealed record HomeTestimonialAdminRow(long Id, decimal IdProduct, string DisplayName, string FeedbackText,
    byte Rating, DateTime CreatedAtUtc, string ProductName, string ProductKey, bool IsActive, int SortOrder,
    string? ImagePath);
public sealed record HomeTestimonialProduct(decimal Id, string Name, string ProductKey, bool IsStoreEnabled);
public sealed record HomeTestimonialReviewData(decimal ProductId, string DisplayName, string FeedbackText,
    byte Rating, bool IsActive, int SortOrder);
