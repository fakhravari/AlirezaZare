using Shop.Models;
using Shop.Services;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

[Route("Magazine")]
public sealed class MagazineController : PageController
{
    private readonly SeoMetadataOptions _seoOptions;

    public MagazineController(IOptions<SeoMetadataOptions> seoOptions)
    {
        _seoOptions = seoOptions.Value;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("مجله برق و ابزار | ندا اولیائی", "راهنمای خرید، انتخاب ایمن، استفاده و نگهداری ابزار برقی و تجهیزات اندازه‌گیری؛ مقاله‌ها را بر اساس نیاز کارگاهی و موضوع موردنظر مرور کنید.", Breadcrumbs: [new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("مجله برق و ابزار", "/magazine")]));
        return await PageAsync(model);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        if (JournalArticles.Find(slug) is { } relatedArticle)
        {
            string siteOrigin = SeoUrlBuilder.SiteOrigin(_seoOptions.SiteUrl, Request);
            string siteRoot = SeoUrlBuilder.SiteRoot(_seoOptions.SiteUrl, Request);
            string articleUrl = SeoUrlBuilder.AbsolutePageUrl(siteRoot, $"/magazine/{relatedArticle.Slug}");
            string articleImage = SeoUrlBuilder.AbsoluteAssetUrl(siteOrigin, Url.Content($"~/{relatedArticle.ContentImage}"));
            var articleData = new Dictionary<string, object?>
            {
                ["@type"] = "Article",
                ["headline"] = relatedArticle.Title,
                ["description"] = relatedArticle.Summary,
                ["image"] = new[] { articleImage },
                ["author"] = new Dictionary<string, object?> { ["@type"] = "Organization", ["name"] = relatedArticle.Author },
                ["publisher"] = new Dictionary<string, object?> { ["@type"] = "Organization", ["name"] = "فروشگاه ندا اولیائی" },
                ["mainEntityOfPage"] = articleUrl,
                ["inLanguage"] = "fa-IR"
            };
            return await PageAsync(new PageModel<MagazineDetailsModel>(relatedArticle, new SeoModel(relatedArticle.Title + " | مجله برق و ابزار ندا اولیائی", relatedArticle.Summary, CanonicalUrl: $"/magazine/{relatedArticle.Slug}", ImageUrl: articleImage, StructuredData: articleData, OpenGraphType: "article", SocialImageAlt: relatedArticle.Title, ArticleSection: relatedArticle.Category, Breadcrumbs: [new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("مجله برق و ابزار", "/magazine"), new SeoBreadcrumb(relatedArticle.Title, $"/magazine/{relatedArticle.Slug}")])));
        }

        return NotFound();
    }

    [HttpGet("Details/{slug}")]
    public IActionResult LegacyDetails(string slug) =>
        RedirectToActionPermanent(nameof(Details), new { slug });
}
