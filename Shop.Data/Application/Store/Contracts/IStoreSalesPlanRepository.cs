using Shop.Database.Application.Store.Models;

namespace Shop.Database.Application.Store.Contracts;

public interface IStoreSalesPlanRepository
{
    Task<StoreSalesPlanSnapshot> ReadSalesPlanSnapshotAsync(CancellationToken cancellationToken);
    Task<ProductPurchaseConfiguration?> ReadProductPurchaseConfigurationAsync(int productId, CancellationToken cancellationToken);
}
