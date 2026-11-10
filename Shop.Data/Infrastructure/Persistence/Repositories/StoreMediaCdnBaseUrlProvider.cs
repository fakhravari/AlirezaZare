using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shop.Database.Application.Store;
using Shop.Database.Application.Store.Contracts;

namespace Shop.Database.Infrastructure.Persistence.Repositories;

public sealed class StoreMediaCdnBaseUrlProvider(ShopDbContext dbContext, IOptions<MediaCdnOptions> options) : IMediaCdnBaseUrlProvider
{
    public async Task<string> GetBaseUrlAsync(CancellationToken cancellationToken = default)
    {
        string configuredFallback = MediaCdnOptions.NormalizeBaseUrl(options.Value.BaseUrl);
        string? databaseValue;
        try
        {
            databaseValue = await dbContext.StoreSiteIdentities.AsNoTracking()
                .Where(setting => setting.Id == 1)
                .Select(setting => setting.MediaCdnBaseUrl)
                .SingleOrDefaultAsync(cancellationToken);
        }
        catch (DbException)
        {
            return configuredFallback;
        }
        if (string.IsNullOrWhiteSpace(databaseValue)) return configuredFallback;
        try { return MediaCdnOptions.NormalizeBaseUrl(databaseValue); }
        catch (InvalidOperationException) { return configuredFallback; }
    }
}
