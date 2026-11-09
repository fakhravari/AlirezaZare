namespace Shop.Services;

public interface IProductPurchaseConfigurationSource
{
    Task<ProductPurchaseConfiguration?> ReadAsync(int productId, CancellationToken cancellationToken = default);
}

public sealed record ProductPurchaseConfiguration(
    int MaxPurchaseQuantity,
    IReadOnlyList<ProductPurchaseOption> Options,
    IReadOnlyList<ProductAccessory> Accessories);

public sealed record ProductPurchaseOption(string Key, IReadOnlyList<ProductPurchaseOptionValue> Values);

public sealed record ProductPurchaseOptionValue(string Key, decimal PriceDelta);

public sealed record ProductAccessory(string Key, bool IsRequired);