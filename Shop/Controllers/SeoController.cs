using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Shop.Models;
using Shop.Services;

namespace Shop.Controllers;

public sealed class SeoController : Controller
{
    private readonly CatalogRepository _catalog;

    public SeoController(CatalogRepository catalog)
    {
        _catalog = catalog;
    }

    [HttpGet("/robots.txt")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult Robots()
    {
        string body = $"User-agent: *\nAllow: /\nSitemap: {SiteRoot()}sitemap.xml\n";
        return Content(body, "text/plain; charset=utf-8", Encoding.UTF8);
    }

    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult Sitemap()
    {
        string[] staticPaths =
        [
            "/",
            "/search",
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
            .Concat(_catalog.Read().Brands.Select(brand => $"/brands/{brand.Slug}"))
            .Concat(JournalArticles.Slugs.Select(slug => $"/magazine/{slug}"))
            .Concat(_catalog.Read().Products.Select(product => $"/products/{product.Slug}-{product.Id}"));

        XNamespace sitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
        XDocument document = new(
            new XElement(sitemapNamespace + "urlset",
                paths.Distinct(StringComparer.OrdinalIgnoreCase).Select(path =>
                    new XElement(sitemapNamespace + "url",
                        new XElement(sitemapNamespace + "loc", AbsoluteUrl(path))))));

        return Content(document.ToString(SaveOptions.DisableFormatting), "application/xml; charset=utf-8", Encoding.UTF8);
    }

    private string SiteRoot() => $"{Request.Scheme}://{Request.Host}{Request.PathBase}/";

    private string AbsoluteUrl(string path) => new Uri(new Uri(SiteRoot()), path.TrimStart('/')).AbsoluteUri;

}
