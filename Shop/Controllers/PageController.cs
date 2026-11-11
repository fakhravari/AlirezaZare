using Shop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace Shop.Controllers;

public abstract class PageController : Controller
{
    protected async Task<IActionResult> PageAsync(PageModel model)
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
        if (model.ContentKey is not null)
        {
            var db = HttpContext.RequestServices.GetRequiredService<ShopDbContext>();
            var page = await db.StoreContentPages.AsNoTracking().Include(x => x.Sections)
                .SingleOrDefaultAsync(x => x.PageKey == model.ContentKey, HttpContext.RequestAborted);
            if (page is null || !page.IsPublished) return NotFound();
            string title = StoreName(page.Title);
            string description = StoreName(page.Description ?? "");
            var managed = new ManagedPageContent(page.Id, page.PageKey, page.RoutePath, title, description, page.IsPublished,
                page.Sections.Where(x => x.IsPublished).OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
                    .Select(x => new ManagedPageSection(x.Id, x.SectionKey, x.Heading, x.Body, x.SortOrder, x.IsPublished)).ToArray());
            var crumbList = model.Seo.Breadcrumbs?.ToArray();
            if (crumbList is { Length: > 0 }) crumbList[^1] = crumbList[^1] with { Name = title };
            var breadcrumbs = crumbList;
            model = model with { Seo = model.Seo with { Title = title + " | " + identity.DisplayName, Description = description, Breadcrumbs = breadcrumbs }, ManagedContent = managed };
            // Known content routes have dedicated views with their own theme and interactions.
            // Keep the generic renderer only as a fallback for future content keys.
            if (model.ContentKey is not ("faq" or "contact" or "about" or "shipping" or "warranty" or "tracking" or "privacy" or "terms" or "returns"))
                return View("~/Views/Shared/ManagedContentPage.cshtml", model);
        }
        ViewResult result = string.IsNullOrWhiteSpace(model.ViewName) ? View(model) : View(model.ViewName, model);
        return result;
    }
}
