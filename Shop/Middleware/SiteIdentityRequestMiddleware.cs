using Microsoft.Extensions.Options;
using Shop.Models;
using Shop.Services;

namespace Shop.Middleware;

public sealed class SiteIdentityRequestContext
{
    private SiteIdentityOptions? _value;

    public SiteIdentityOptions Value => _value ??
                                        throw new InvalidOperationException(
                                            "Site identity was not loaded for this request.");

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
