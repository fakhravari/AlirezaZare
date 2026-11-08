namespace Shop.Database.Application.Store.Models;

public sealed record StoreSalesPlanSnapshot(
    IReadOnlyList<StoreSalesPlanRow> Plans,
    IReadOnlyList<StoreSalesOfferRow> Offers,
    IReadOnlyList<StoreInventoryVariantRow> InventoryVariants)
{
    public IReadOnlyList<StoreProductPriceOptionRow> PriceOptions { get; init; } = [];
}

public sealed record StoreProductPriceOptionRow(int ProductId, string Key, string Label,
    IReadOnlyList<StoreProductPriceOptionValueRow> Values);

public sealed record StoreProductPriceOptionValueRow(string Key, string Label, decimal PriceDelta);

public sealed record StoreInventoryVariantRow(
    int ProductId,
    long VariantId,
    string Name,
    string? Code,
    string? AttributesJson,
    IReadOnlyDictionary<string, decimal> StockBySalesPlan,
    IReadOnlyDictionary<string, decimal> PriceBySalesPlan,
    IReadOnlyDictionary<string, decimal> DiscountBySalesPlan);

public sealed record StoreSalesPlanRow(
    string Id,
    string Code,
    string Name,
    string Type,
    bool IsDefault,
    bool IsEnabled,
    DateTimeOffset? StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    string? Label,
    bool ShowCountdown);

public sealed record StoreSalesOfferRow(
    int ProductId,
    string SalesPlanId,
    decimal PriceAmount,
    decimal DiscountPercent,
    int StockQuantity,
    int MaxPurchaseQuantity,
    bool IsEnabled,
    IReadOnlyDictionary<string, decimal> AccessoryPriceAmounts,
    IReadOnlyList<string> AccessoryIds,
    IReadOnlyDictionary<string, string> SpecificationOverrides);
