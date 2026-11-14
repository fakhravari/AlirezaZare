using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
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

public sealed class SiteIdentityRequestContext
{
    private SiteIdentityOptions? _value;

    public SiteIdentityOptions Value => _value ??
        throw new InvalidOperationException("Site identity was not loaded for this request.");

    internal void Set(SiteIdentityOptions value) => _value = value;
}

public sealed class RequestSiteIdentityOptions(SiteIdentityRequestContext context) : IOptions<SiteIdentityOptions>
{
    public SiteIdentityOptions Value => context.Value;
}

public sealed class SiteIdentityRequestMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        DatabaseSiteIdentitySource source,
        SiteIdentityRequestContext identityContext)
    {
        if (context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Path.StartsWithSegments("/healthz"))
        {
            await next(context);
            return;
        }

        identityContext.Set(await source.ReadAsync(context.RequestAborted));
        await next(context);
    }
}
