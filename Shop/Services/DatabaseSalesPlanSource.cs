using Shop.Data.Application.Store.Contracts;
using Shop.Data.Application.Store.Models;

namespace Shop.Services;

public sealed class DatabaseSalesPlanSource(IStoreSalesPlanRepository repository)
    : ICheckoutSalesPlans, IProductPurchaseConfigurationSource
{
    private SalesPlanCatalog? requestSnapshot;

    public async ValueTask<SalesPlanCatalog> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (requestSnapshot is not null) return requestSnapshot;

        var snapshot = await repository.ReadSalesPlanSnapshotAsync(cancellationToken);

        return requestSnapshot = new SalesPlanCatalog(1, "IRT",
            snapshot.Plans.Select(plan => new SalesPlan(plan.Id, plan.Code, plan.Name, plan.Type,
                    plan.IsDefault, plan.IsEnabled, plan.StartsAtUtc, plan.EndsAtUtc, plan.Label, plan.ShowCountdown))
                .ToArray(),
            snapshot.Offers.Select(offer => new ProductSalesOffer(offer.ProductId, offer.SalesPlanId,
                offer.PriceAmount, offer.DiscountPercent, offer.StockQuantity, offer.MaxPurchaseQuantity,
                offer.IsEnabled)
            {
                AccessoryPriceAmounts = offer.AccessoryPriceAmounts,
                AccessoryIds = offer.AccessoryIds,
                SpecificationOverrides = offer.SpecificationOverrides,
                FinalPriceAmount = offer.FinalPriceAmount,
                PriceVersionId = offer.PriceVersionId,
                AccessoryPriceVersionIds = offer.AccessoryPriceVersionIds
            }).ToArray())
        {
            InventoryVariants = snapshot.InventoryVariants.Select(variant => new ProductInventoryVariant(
                variant.ProductId, variant.VariantId, variant.Name, variant.Code,
                variant.Attributes, variant.IsDefault, variant.StockBySalesPlan,
                variant.PriceBySalesPlan, variant.DiscountBySalesPlan)
            {
                PriceVersionIdsBySalesPlan = variant.PriceVersionIdsBySalesPlan,
                FinalPriceBySalesPlan = variant.FinalPriceBySalesPlan,
                DisabledSalesPlanIds = variant.DisabledSalesPlanIds
            }).ToArray(),
        };
    }

    public Task<ProductPurchaseConfiguration?> ReadAsync(int productId, string salesPlanId,
        CancellationToken cancellationToken = default) =>
        repository.ReadProductPurchaseConfigurationAsync(productId, salesPlanId, cancellationToken);
}
