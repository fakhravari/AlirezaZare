using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Filters;

public sealed class MediaCdnViewDataFilter(IMediaCdnBaseUrlProvider mediaCdn) : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ViewResult view)
            view.ViewData["MediaCdnBaseUrl"] = await mediaCdn.GetBaseUrlAsync(context.HttpContext.RequestAborted);
        await next();
    }
}
