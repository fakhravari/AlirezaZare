namespace Shop.Services;

public interface IProductPurchaseConfigurationSource
{
    Task<ProductPurchaseConfiguration?> ReadAsync(int productId, string salesPlanId,
        CancellationToken cancellationToken = default);
}

public sealed record ProductPurchaseConfiguration(
    int MaxPurchaseQuantity,
    IReadOnlyList<ProductAccessory> Accessories);

public sealed record ProductAccessory(string Key, bool IsRequired);