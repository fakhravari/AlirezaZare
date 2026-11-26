using Shop.Data.Application.Store.Models;

namespace Shop.Data.Application.Store.Contracts;

public interface ICheckoutSalesPlans
{
    ValueTask<SalesPlanCatalog> ReadAsync(CancellationToken cancellationToken = default);
}
