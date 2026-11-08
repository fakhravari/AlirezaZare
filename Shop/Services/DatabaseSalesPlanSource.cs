using Shop.Database.Application.Store.Contracts;
using Shop.Models;

namespace Shop.Services;

public sealed class DatabaseSalesPlanSource(IStoreSalesPlanRepository repository)
    : ISalesPlanSource, IProductPurchaseConfigurationSource
{
    public async ValueTask<SalesPlanCatalog> ReadAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.ReadSalesPlanSnapshotAsync(cancellationToken);
        return new SalesPlanCatalog(1, "IRT",
            snapshot.Plans.Select(plan => new SalesPlan(plan.Id, plan.Code, plan.Name, plan.Type,
                plan.IsDefault, plan.IsEnabled, plan.StartsAtUtc, plan.EndsAtUtc, plan.Label, plan.ShowCountdown)).ToArray(),
            snapshot.Offers.Select(offer => new ProductSalesOffer(offer.ProductId, offer.SalesPlanId,
                offer.PriceAmount, offer.DiscountPercent, offer.StockQuantity, offer.MaxPurchaseQuantity, offer.IsEnabled)
            {
                AccessoryPriceAmounts = offer.AccessoryPriceAmounts,
                AccessoryIds = offer.AccessoryIds,
                SpecificationOverrides = offer.SpecificationOverrides
            }).ToArray());
    }

    public async Task<ProductPurchaseConfiguration?> ReadAsync(int productId, CancellationToken cancellationToken = default)
    {
        var configuration = await repository.ReadProductPurchaseConfigurationAsync(productId, cancellationToken);
        return configuration is null ? null : new ProductPurchaseConfiguration(
            configuration.MaxPurchaseQuantity,
            configuration.Options.Select(option => new ProductPurchaseOption(option.Key,
                option.Values.Select(value => new ProductPurchaseOptionValue(value.Key, value.PriceDelta)).ToArray())).ToArray(),
            configuration.Accessories.Select(accessory => new ProductAccessory(accessory.Key, accessory.IsRequired)).ToArray());
    }
}
