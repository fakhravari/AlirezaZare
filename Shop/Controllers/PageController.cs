using Shop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Shop.Controllers;

public abstract class PageController : Controller
{
    protected async Task<ViewResult> PageAsync(PageModel model)
    {
        SiteIdentityOptions identity = HttpContext.RequestServices
            .GetRequiredService<IOptions<SiteIdentityOptions>>().Value;

        string StoreName(string value) => value
            .Replace("فروشگاه برق و ابزار ندا اولیائی", identity.StoreName, StringComparison.Ordinal)
            .Replace("فروشگاه ندا اولیائی", identity.StoreName, StringComparison.Ordinal)
            .Replace("ندا اولیائی", identity.DisplayName, StringComparison.Ordinal);

        model = model with
        {
            Seo = model.Seo with
            {
                Title = StoreName(model.Seo.Title),
                Description = StoreName(model.Seo.Description),
                SocialImageAlt = model.Seo.SocialImageAlt is null ? null : StoreName(model.Seo.SocialImageAlt)
            }
        };
        ViewResult result = string.IsNullOrWhiteSpace(model.ViewName) ? View(model) : View(model.ViewName, model);
        return await Task.FromResult(result);
    }
}