namespace Shop.Database.Application.Store.Models;

public sealed record ProductPurchaseConfiguration(
    int MaxPurchaseQuantity,
    IReadOnlyList<ProductPurchaseOptionRow> Options,
    IReadOnlyList<ProductAccessoryRow> Accessories);

public sealed record ProductPurchaseOptionRow(
    string Key,
    IReadOnlyList<ProductPurchaseOptionValueRow> Values);

public sealed record ProductPurchaseOptionValueRow(string Key, decimal PriceDelta);

public sealed record ProductAccessoryRow(string Key, bool IsRequired);
