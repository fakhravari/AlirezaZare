using Shop.Data.Application.Store.Models;

namespace Shop.Data.Application.Store.Contracts;

public interface IStoreSiteIdentityRepository
{
    Task<StoreSiteIdentitySnapshot?> ReadAsync(CancellationToken cancellationToken);
}
