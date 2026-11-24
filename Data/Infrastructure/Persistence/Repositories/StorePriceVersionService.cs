using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Store.Entities;

namespace Shop.Data.Infrastructure.Persistence.Repositories;

public sealed class StorePriceVersionService(ShopDbContext db) : IStorePriceVersionService
{
    public async Task<decimal> AppendAsync(decimal productId, long salesPlanId, long? productVariantId, decimal? accessoryId,
        decimal amount, decimal discountPercent, long idChangedBy, CancellationToken cancellationToken = default)
    {
        Validate(amount, discountPercent);
        var prior = await FindActiveAsync(productId, salesPlanId, productVariantId, accessoryId, cancellationToken);
        if (prior is not null) prior.IsActive = false;
        var version = new StorePriceVersion
        {
            IdProduct = productId,
            IdSalesPlan = salesPlanId,
            IdProductVariant = productVariantId,
            IdAccessory = accessoryId,
            PriceBeforeDiscountAmount = amount,
            DiscountPercent = discountPercent,
            FinalPriceAmount =
                decimal.Round(amount * (100m - discountPercent) / 100m, 0, MidpointRounding.AwayFromZero),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            IdChangedBy = idChangedBy,
            IdPreviousVersion = prior?.Id
        };
        db.StorePriceVersions.Add(version);
        await db.SaveChangesAsync(cancellationToken);

        return version.Id;
    }

    public async Task<bool> DeactivateAsync(decimal productId, long salesPlanId, long? productVariantId, decimal? accessoryId,
        CancellationToken cancellationToken = default)
    {
        var active = await FindActiveAsync(productId, salesPlanId, productVariantId, accessoryId, cancellationToken);
        if (active is null) return false;

        active.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    private Task<StorePriceVersion?> FindActiveAsync(decimal productId, long salesPlanId, long? productVariantId,
        decimal? accessoryId, CancellationToken cancellationToken) =>
        db.StorePriceVersions.SingleOrDefaultAsync(
            x => x.IdProduct == productId && x.IdSalesPlan == salesPlanId && x.IdProductVariant == productVariantId &&
                 x.IdAccessory == accessoryId && x.IsActive, cancellationToken);

    private static void Validate(decimal amount, decimal discountPercent)
    {
        if (amount < 0 || amount > 9007199254740991m || amount != decimal.Truncate(amount))
            throw new ArgumentOutOfRangeException(nameof(amount));
        if (discountPercent < 0 || discountPercent >= 100 || decimal.Round(discountPercent, 2) != discountPercent)
            throw new ArgumentOutOfRangeException(nameof(discountPercent));
    }
}
