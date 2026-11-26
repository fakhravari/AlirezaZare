namespace Shop.Data.Application.Store.Contracts;

public interface IStoreProductImageAdminData
{
    Task<bool> ProductExistsAsync(decimal productId, CancellationToken cancellationToken);
    Task<string?> ReadProductNameAsync(decimal productId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductImageData>> ReadImagesAsync(decimal productId, CancellationToken cancellationToken);
    Task<bool> AddImagesAsync(decimal productId, string productName, IReadOnlyList<ProductImageUploadData> images,
        CancellationToken cancellationToken);
    Task<bool> SetDefaultAsync(decimal productId, string path, CancellationToken cancellationToken);
    Task<ProductImageData?> ReadImageAsync(decimal productId, string path, CancellationToken cancellationToken);
    Task<bool> DeleteImageRecordAsync(decimal productId, string path, CancellationToken cancellationToken);
    Task<bool> ReorderAsync(decimal productId, IReadOnlyList<decimal> ids, CancellationToken cancellationToken);
}

public sealed record ProductImageData(decimal Id, string Path, string? ThumbnailPath, bool IsDefault, int SortOrder);
public sealed record ProductImageUploadData(string MainPath, string CardPath, string ThumbnailPath);
