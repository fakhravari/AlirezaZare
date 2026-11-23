namespace Shop.Data.Application.Store.Models;

public sealed record StoreSalesPlanSnapshot(
    IReadOnlyList<StoreSalesPlanRow> Plans,
    IReadOnlyList<StoreSalesOfferRow> Offers,
    IReadOnlyList<StoreInventoryVariantRow> InventoryVariants)
{
}

public sealed record StoreInventoryVariantRow(
    int ProductId,
    long VariantId,
    string Name,
    string? Code,
    System.Text.Json.JsonElement? Attributes,
    bool IsDefault,
    IReadOnlyDictionary<string, decimal> StockBySalesPlan,
    IReadOnlyDictionary<string, decimal> PriceBySalesPlan,
    IReadOnlyDictionary<string, decimal> DiscountBySalesPlan)
{
    public IReadOnlyDictionary<string, decimal> FinalPriceBySalesPlan { get; init; } = new Dictionary<string, decimal>();
    public IReadOnlyDictionary<string, long> PriceVersionIdsBySalesPlan { get; init; } = new Dictionary<string, long>();
}

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
    IReadOnlyDictionary<string, string> SpecificationOverrides)
{
    public decimal FinalPriceAmount { get; init; }
    public long PriceVersionId { get; init; }
    public IReadOnlyDictionary<string, long> AccessoryPriceVersionIds { get; init; } = new Dictionary<string, long>();
}
