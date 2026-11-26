using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Store;

public sealed class StoreHomeTestimonialAdminData(ShopDbContext db) : IStoreHomeTestimonialAdminData
{
    public async Task<IReadOnlyList<HomeTestimonialAdminRow>> ReadAsync(CancellationToken ct) =>
        await db.StoreHomeTestimonials.AsNoTracking()
            .Join(db.Products.AsNoTracking(), item => item.IdProduct, product => product.Id,
                (item, product) => new HomeTestimonialAdminRow(item.Id, item.IdProduct, item.DisplayName,
                    item.FeedbackText, item.Rating, item.CreatedAtUtc, product.NameFa, product.ProductKey,
                    item.IsActive, item.SortOrder, item.ImagePath))
            .OrderByDescending(x => x.IsActive).ThenBy(x => x.SortOrder)
            .ThenByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).ToListAsync(ct);

    public async Task<IReadOnlyList<HomeTestimonialProduct>> ReadProductsAsync(CancellationToken ct) =>
        await db.Products.AsNoTracking().OrderBy(x => x.NameFa).ThenBy(x => x.Id)
            .Select(x => new HomeTestimonialProduct(x.Id, x.NameFa, x.ProductKey, x.IsStoreEnabled)).ToListAsync(ct);

    public Task<bool> ProductExistsAsync(decimal productId, CancellationToken ct) =>
        db.Products.AnyAsync(x => x.Id == productId, ct);

    public async Task<long> CreateAsync(HomeTestimonialReviewData input, CancellationToken ct)
    {
        var item = new StoreHomeTestimonial
        {
            IdProduct = input.ProductId, DisplayName = input.DisplayName, FeedbackText = input.FeedbackText,
            Rating = input.Rating, CreatedAtUtc = DateTime.UtcNow, IsActive = input.IsActive,
            SortOrder = input.SortOrder
        };
        db.StoreHomeTestimonials.Add(item);
        await db.SaveChangesAsync(ct);
        return item.Id;
    }

    public async Task<bool> UpdateReviewAsync(long id, HomeTestimonialReviewData input, CancellationToken ct)
    {
        var item = await db.StoreHomeTestimonials.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return false;
        item.IdProduct = input.ProductId; item.DisplayName = input.DisplayName; item.FeedbackText = input.FeedbackText;
        item.Rating = input.Rating; item.IsActive = input.IsActive; item.SortOrder = input.SortOrder;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateDisplayAsync(long id, bool isActive, int sortOrder, CancellationToken ct)
    {
        var item = await db.StoreHomeTestimonials.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return false;
        item.IsActive = isActive; item.SortOrder = sortOrder;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public Task<bool> ExistsAsync(long id, CancellationToken ct) =>
        db.StoreHomeTestimonials.AnyAsync(x => x.Id == id, ct);

    public Task<string?> ReadImagePathAsync(long id, CancellationToken ct) =>
        db.StoreHomeTestimonials.Where(x => x.Id == id).Select(x => x.ImagePath).SingleOrDefaultAsync(ct);

    public async Task<bool> SetImagePathAsync(long id, string? path, CancellationToken ct)
    {
        var item = await db.StoreHomeTestimonials.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return false;
        item.ImagePath = path;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
