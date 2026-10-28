using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shop.Models;
using Shop.Services;

namespace Shop.Controllers;

public sealed class SeoController : Controller
{
    private readonly ICatalogSource _catalog;
    private readonly SeoMetadataOptions _options;

    public SeoController(ICatalogSource catalog, IOptions<SeoMetadataOptions> options)
    {
        _catalog = catalog;
        _options = options.Value;
    }

    [HttpGet("/robots.txt")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult Robots()
    {
        string body = $"User-agent: *\nAllow: /\nSitemap: {SeoUrlBuilder.AbsolutePageUrl(SiteRoot(), "sitemap.xml")}\n";
        return Content(body, "text/plain; charset=utf-8", Encoding.UTF8);
    }

    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Sitemap()
    {
        var catalog = await _catalog.ReadAsync(HttpContext.RequestAborted);
        string[] staticPaths =
        [
            "/",
            "/about",
            "/contact",
            "/faq",
            "/shipping",
            "/tracking",
            "/warranty",
            "/legal/privacy",
            "/legal/terms",
            "/legal/returns",
            "/magazine",
            "/brands"
        ];

        IEnumerable<string> paths = staticPaths
            .Concat(catalog.Brands.Select(brand => $"/brands/{brand.Slug}"))
            .Concat(JournalArticles.Slugs.Select(slug => $"/magazine/{slug}"))
            .Concat(catalog.Products.Select(product => $"/products/{product.Slug}-{product.Id}"));

        XNamespace sitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
        XDocument document = new(
            new XElement(sitemapNamespace + "urlset",
                paths.Distinct(StringComparer.OrdinalIgnoreCase).Select(path =>
                    new XElement(sitemapNamespace + "url",
                        new XElement(sitemapNamespace + "loc", SeoUrlBuilder.AbsolutePageUrl(SiteRoot(), path))))));

        return Content(document.ToString(SaveOptions.DisableFormatting), "application/xml; charset=utf-8", Encoding.UTF8);
    }

    private string SiteRoot() => SeoUrlBuilder.SiteRoot(_options.SiteUrl, Request);

}
