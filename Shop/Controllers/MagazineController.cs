using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

[Route("Magazine")]
public sealed class MagazineController : PageController
{
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
            string articleUrl = $"{Request.Scheme}://{Request.Host}{Url.Content($"~/magazine/{relatedArticle.Slug}")}";
            string articleImage = $"{Request.Scheme}://{Request.Host}{Url.Content($"~/Shop/images/products/{Path.GetFileName(relatedArticle.ContentImage)}")}";
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
            return await PageAsync(new PageModel<MagazineDetailsModel>(relatedArticle, new SeoModel(relatedArticle.Title + " | مجله برق و ابزار ندا اولیائی", relatedArticle.Summary, CanonicalUrl: articleUrl, ImageUrl: articleImage, StructuredData: articleData, OpenGraphType: "article", SocialImageAlt: relatedArticle.Title, ArticleSection: relatedArticle.Category, Breadcrumbs: [new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("مجله برق و ابزار", "/magazine"), new SeoBreadcrumb(relatedArticle.Title, $"/magazine/{relatedArticle.Slug}")])));
        }

        return NotFound();
    }

    [HttpGet("Details/{slug}")]
    public IActionResult LegacyDetails(string slug) =>
        RedirectToActionPermanent(nameof(Details), new { slug });
}
