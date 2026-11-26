using System.Data;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Store;

public sealed class StoreProductBadgeAdminData(ShopDbContext db) : IStoreProductBadgeAdminData
{
    public async Task<IReadOnlyList<ProductBadgeData>> ReadBadgesAsync(CancellationToken cancellationToken) =>
        await db.StoreProductBadges.AsNoTracking().OrderBy(badge => badge.Title)
            .Select(badge => new ProductBadgeData(badge.Id, badge.Title, badge.BackgroundColor, badge.TextColor,
                badge.BorderColor, db.Products.Count(product => product.IdBadge == badge.Id)))
            .ToListAsync(cancellationToken);

    public async Task<(int Total, IReadOnlyList<ProductBadgeProductData> Items)> ReadProductsAsync(string search,
        long? badgeId, bool withoutBadge, int offset, int pageSize, CancellationToken cancellationToken)
    {
        var query = db.Products.AsNoTracking().Where(product => search == "" || product.NameFa.Contains(search) ||
            product.ProductKey.Contains(search));
        if (badgeId.HasValue) query = query.Where(product => product.IdBadge == badgeId.Value);
        if (withoutBadge) query = query.Where(product => product.IdBadge == null);
        int total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(product => product.NameFa).ThenBy(product => product.Id).Skip(offset)
            .Take(pageSize).Select(product => new ProductBadgeProductData(product.Id, product.NameFa,
                product.ProductKey, product.IdBadge)).ToListAsync(cancellationToken);
        return (total, items);
    }

    public async Task<ProductBadgeSaveResult> SaveBadgeAsync(long? id, string title, string backgroundColor,
        string textColor, string borderColor, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        if (await db.StoreProductBadges.AnyAsync(badge => badge.Id != id && badge.Title == title,
                cancellationToken)) return new(ProductBadgeSaveResultKind.Duplicate);
        StoreProductBadge? badge = id.HasValue
            ? await db.StoreProductBadges.SingleOrDefaultAsync(item => item.Id == id.Value, cancellationToken)
            : new StoreProductBadge();
        if (badge is null) return new(ProductBadgeSaveResultKind.NotFound);
        badge.Title = title;
        badge.BackgroundColor = backgroundColor;
        badge.TextColor = textColor;
        badge.BorderColor = borderColor;
        if (!id.HasValue) db.StoreProductBadges.Add(badge);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(ProductBadgeSaveResultKind.Saved, new(badge.Id, badge.Title, badge.BackgroundColor,
            badge.TextColor, badge.BorderColor));
    }

    public async Task<ProductBadgeDeleteResult> DeleteBadgeAsync(long id, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        var badge = await db.StoreProductBadges.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (badge is null) return ProductBadgeDeleteResult.NotFound;
        if (await db.Products.AnyAsync(product => product.IdBadge == id, cancellationToken))
            return ProductBadgeDeleteResult.Assigned;
        db.StoreProductBadges.Remove(badge);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ProductBadgeDeleteResult.Deleted;
    }

    public async Task<ProductBadgeAssignmentResult> AssignAsync(long productId, long? badgeId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        var product = await db.Products.SingleOrDefaultAsync(item => item.Id == productId, cancellationToken);
        if (product is null) return ProductBadgeAssignmentResult.ProductNotFound;
        if (badgeId.HasValue && !await db.StoreProductBadges.AnyAsync(item => item.Id == badgeId.Value,
                cancellationToken)) return ProductBadgeAssignmentResult.BadgeNotFound;
        product.IdBadge = badgeId;
        product.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ProductBadgeAssignmentResult.Saved;
    }
}
