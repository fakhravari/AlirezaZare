using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Application.Store.Models;

namespace Shop.Data.Infrastructure.Persistence.Repositories;

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
                identity.PublicEmail,
                identity.FooterDescription,
                identity.InstagramUrl,
                identity.TelegramUrl,
                identity.LinkedInUrl))
            .SingleOrDefaultAsync(cancellationToken);
}
