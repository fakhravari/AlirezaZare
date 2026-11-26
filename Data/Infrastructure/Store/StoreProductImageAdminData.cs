using System.Data;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Store;

public sealed class StoreProductImageAdminData(ShopDbContext db) : IStoreProductImageAdminData
{
    public Task<bool> ProductExistsAsync(decimal id, CancellationToken ct) => db.Products.AnyAsync(x => x.Id == id, ct);
    public Task<string?> ReadProductNameAsync(decimal id, CancellationToken ct) =>
        db.Products.Where(x => x.Id == id).Select(x => x.NameFa).SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ProductImageData>> ReadImagesAsync(decimal productId, CancellationToken ct) =>
        await db.StoreProductAssets.AsNoTracking().Where(x => x.IdProduct == productId &&
                x.IdMediaType == ProductMediaTypes.Image).OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new ProductImageData(x.Id, x.Path, x.ThumbnailPath, x.IsDefault, x.SortOrder))
            .ToListAsync(ct);

    public async Task<bool> AddImagesAsync(decimal productId, string productName,
        IReadOnlyList<ProductImageUploadData> images, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await db.Products.AnyAsync(x => x.Id == productId, ct)) return false;
        var current = db.StoreProductAssets.Where(x => x.IdProduct == productId && x.IdMediaType == ProductMediaTypes.Image);
        int nextOrder = (await current.MaxAsync(x => (int?)x.SortOrder, ct) ?? 0) + 1;
        bool hasDefault = await current.AnyAsync(x => x.IsDefault, ct);
        foreach (var image in images)
        {
            db.StoreProductAssets.Add(new StoreProductAsset
            {
                IdProduct = productId, IdMediaType = ProductMediaTypes.Image, Path = image.MainPath,
                CardPath = image.CardPath, ThumbnailPath = image.ThumbnailPath, Title = productName,
                SortOrder = nextOrder++, IsDefault = !hasDefault, CreatedAtUtc = DateTime.UtcNow
            });
            hasDefault = true;
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<bool> SetDefaultAsync(decimal productId, string path, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var images = await db.StoreProductAssets.Where(x => x.IdProduct == productId &&
            x.IdMediaType == ProductMediaTypes.Image).ToListAsync(ct);
        var selected = images.SingleOrDefault(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase));
        if (selected is null) return false;
        foreach (var image in images) image.IsDefault = false;
        selected.IsDefault = true;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    public Task<ProductImageData?> ReadImageAsync(decimal productId, string path, CancellationToken ct) =>
        db.StoreProductAssets.AsNoTracking().Where(x => x.IdProduct == productId &&
                x.IdMediaType == ProductMediaTypes.Image && x.Path == path)
            .Select(x => new ProductImageData(x.Id, x.Path, x.ThumbnailPath, x.IsDefault, x.SortOrder))
            .SingleOrDefaultAsync(ct);

    public async Task<bool> DeleteImageRecordAsync(decimal productId, string path, CancellationToken ct)
    {
        var image = await db.StoreProductAssets.SingleOrDefaultAsync(x => x.IdProduct == productId &&
            x.IdMediaType == ProductMediaTypes.Image && x.Path == path, ct);
        if (image is null) return false;
        db.StoreProductAssets.Remove(image);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ReorderAsync(decimal productId, IReadOnlyList<decimal> ids, CancellationToken ct)
    {
        var images = await db.StoreProductAssets.Where(x => x.IdProduct == productId &&
            x.IdMediaType == ProductMediaTypes.Image).ToListAsync(ct);
        if (ids.Count != images.Count || ids.Distinct().Count() != images.Count ||
            images.Any(x => !ids.Contains(x.Id))) return false;
        var byId = images.ToDictionary(x => x.Id);
        for (int i = 0; i < ids.Count; i++) byId[ids[i]].SortOrder = i + 1;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
