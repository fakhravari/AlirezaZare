using Shop.Database.Application.Store.Models;

namespace Shop.Database.Application.Store.Contracts;

public interface IStoreSiteIdentityRepository
{
    Task<StoreSiteIdentitySnapshot?> ReadAsync(CancellationToken cancellationToken);
}
