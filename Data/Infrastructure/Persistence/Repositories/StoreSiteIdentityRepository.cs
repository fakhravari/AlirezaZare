using Microsoft.EntityFrameworkCore;
using Shop.Database.Application.Store.Contracts;
using Shop.Database.Application.Store.Models;

namespace Shop.Database.Infrastructure.Persistence.Repositories;

public sealed class StoreSiteIdentityRepository(ShopDbContext dbContext) : IStoreSiteIdentityRepository
{
    public Task<StoreSiteIdentitySnapshot?> ReadAsync(CancellationToken cancellationToken) =>
        dbContext.StoreSiteIdentities.AsNoTracking()
            .Where(identity => identity.Id == 1)
            .Select(identity => new StoreSiteIdentitySnapshot(
                identity.DisplayName,
                identity.StoreName,
                identity.VisualBrandName,
                identity.SupportPhoneDisplay,
                identity.SupportPhoneUri,
                identity.WhatsAppNumber,
                identity.PublicEmail))
            .SingleOrDefaultAsync(cancellationToken);
}
