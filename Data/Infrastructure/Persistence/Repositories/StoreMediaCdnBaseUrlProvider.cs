using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shop.Data.Application.Store;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Data.Infrastructure.Persistence.Repositories;

public sealed class StoreMediaCdnBaseUrlProvider(ShopDbContext dbContext, IOptions<MediaCdnOptions> options) : IMediaCdnBaseUrlProvider
{
    private string? requestBaseUrl;

    public async Task<string> GetBaseUrlAsync(CancellationToken cancellationToken = default)
    {
        if (requestBaseUrl is not null) return requestBaseUrl;
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
        if (string.IsNullOrWhiteSpace(databaseValue)) return requestBaseUrl = configuredFallback;
        try { return requestBaseUrl = MediaCdnOptions.NormalizeBaseUrl(databaseValue); }
        catch (InvalidOperationException) { return requestBaseUrl = configuredFallback; }
    }
}
