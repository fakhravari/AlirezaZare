using Shop.Models;

namespace Shop.Services;

public interface ISalesPlanSource
{
    ValueTask<SalesPlanCatalog> ReadAsync(CancellationToken cancellationToken = default);
}