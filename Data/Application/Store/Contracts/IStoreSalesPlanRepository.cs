using Shop.Data.Application.Store.Models;

namespace Shop.Data.Application.Store.Contracts;

public interface IStoreSalesPlanRepository
{
    Task<StoreSalesPlanSnapshot> ReadSalesPlanSnapshotAsync(CancellationToken cancellationToken);

    Task<ProductPurchaseConfiguration?> ReadProductPurchaseConfigurationAsync(int productId,
        CancellationToken cancellationToken);
}