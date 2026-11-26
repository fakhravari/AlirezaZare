using Microsoft.Extensions.Caching.Memory;
using Shop.Data.Application.Store.Contracts;
using Shop.Models;

namespace Shop.Services;

public sealed class DatabaseSiteIdentitySource(
    IStoreSiteIdentityRepository repository,
    IMemoryCache cache)
{
    private const string CacheKey = "store-site-identity";

    public async ValueTask<SiteIdentityOptions> ReadAsync(CancellationToken cancellationToken)
    {
        SiteIdentityOptions? identity = await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            var row = await repository.ReadAsync(cancellationToken);

            return row is null
                ? null
                : new SiteIdentityOptions
                {
                    DisplayName = row.DisplayName,
                    StoreName = row.StoreName,
                    VisualBrandName = row.VisualBrandName,
                    SupportPhoneDisplay = row.SupportPhoneDisplay,
                    SupportPhoneUri = row.SupportPhoneUri,
                    WhatsAppNumber = row.WhatsAppNumber,
                    PublicEmail = row.PublicEmail,
                    FooterDescription = row.FooterDescription,
                    InstagramUrl = row.InstagramUrl,
                    TelegramUrl = row.TelegramUrl,
                    LinkedInUrl = row.LinkedInUrl
                };
        });

        return identity ?? throw new InvalidDataException("The Store.SiteSetting row (Id = 1) is missing.");
    }
}
