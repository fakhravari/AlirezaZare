namespace Shop.Database.Application.Store.Models;

public sealed record StoreSalesPlanSnapshot(
    IReadOnlyList<StoreSalesPlanRow> Plans,
    IReadOnlyList<StoreSalesOfferRow> Offers);

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
