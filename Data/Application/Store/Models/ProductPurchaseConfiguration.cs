namespace Shop.Data.Application.Store.Models;

public sealed record ProductPurchaseConfiguration(
    int MaxPurchaseQuantity,
    IReadOnlyList<ProductAccessoryRow> Accessories);

public sealed record ProductAccessoryRow(string Key, bool IsRequired);
