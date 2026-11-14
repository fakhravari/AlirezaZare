using Shop.Data.Application.Store.Contracts;
using Shop.Models;

namespace Shop.Services;

public sealed class DatabaseSalesPlanSource(IStoreSalesPlanRepository repository)
    : ISalesPlanSource, IProductPurchaseConfigurationSource
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
                PriceVersionId = offer.PriceVersionId,
                AccessoryPriceVersionIds = offer.AccessoryPriceVersionIds
            }).ToArray())
        {
            InventoryVariants = snapshot.InventoryVariants.Select(variant => new ProductInventoryVariant(
                variant.ProductId, variant.VariantId, variant.Name, variant.Code,
                ParseAttributes(variant.AttributesJson), variant.IsDefault, variant.StockBySalesPlan,
                variant.PriceBySalesPlan, variant.DiscountBySalesPlan)
            { PriceVersionIdsBySalesPlan = variant.PriceVersionIdsBySalesPlan }).ToArray(),
            PriceOptions = snapshot.PriceOptions.GroupBy(option => option.ProductId).ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<SalesPlanPriceOption>)group.Select(option => new SalesPlanPriceOption(
                    option.Key, option.Label, option.Values.Select(value => new SalesPlanPriceOptionValue(
                        value.Key, value.Label, value.PriceDelta)
                    { Id = value.Id, PriceVersionId = value.PriceVersionId }).ToArray())).ToArray())
        };
    }

    private static System.Text.Json.JsonElement? ParseAttributes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        using var document = System.Text.Json.JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    public async Task<ProductPurchaseConfiguration?> ReadAsync(int productId,
        CancellationToken cancellationToken = default)
    {
        var configuration = await repository.ReadProductPurchaseConfigurationAsync(productId, cancellationToken);
        return configuration is null
            ? null
            : new ProductPurchaseConfiguration(
                configuration.MaxPurchaseQuantity,
                configuration.Options.Select(option => new ProductPurchaseOption(option.Key,
                    option.Values.Select(value => new ProductPurchaseOptionValue(value.Key, value.PriceDelta) { Id = value.Id, PriceVersionId = value.PriceVersionId })
                        .ToArray())).ToArray(),
                configuration.Accessories.Select(accessory => new ProductAccessory(accessory.Key, accessory.IsRequired))
                    .ToArray());
    }
}
