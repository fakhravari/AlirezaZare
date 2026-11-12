namespace Shop.Data.Application.Store.Models;

public sealed record ProductPurchaseConfiguration(
    int MaxPurchaseQuantity,
    IReadOnlyList<ProductPurchaseOptionRow> Options,
    IReadOnlyList<ProductAccessoryRow> Accessories);

public sealed record ProductPurchaseOptionRow(
    string Key,
    IReadOnlyList<ProductPurchaseOptionValueRow> Values);

public sealed record ProductPurchaseOptionValueRow(string Key, decimal PriceDelta)
{
    public long Id { get; init; }
    public long PriceVersionId { get; init; }
}

public sealed record ProductAccessoryRow(string Key, bool IsRequired);
