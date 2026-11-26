using Shop.Data.Application.Store.Models;

namespace Shop.Services;

public interface IProductPurchaseConfigurationSource
{
    Task<ProductPurchaseConfiguration?> ReadAsync(int productId, string salesPlanId,
        CancellationToken cancellationToken = default);
}
