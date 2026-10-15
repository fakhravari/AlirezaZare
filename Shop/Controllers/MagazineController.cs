using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

[Route("Magazine")]
public sealed class MagazineController : PageController
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("مجله برق و ابزار | کمپانی علی‌رضا زارع", "راهنمای خرید، استفاده ایمن و نگهداری ابزار و تجهیزات فنی."));
        return await PageAsync(model);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        if (JournalArticles.Find(slug) is { } relatedArticle)
        {
            return await PageAsync(new PageModel<MagazineDetailsModel>(relatedArticle, new SeoModel(relatedArticle.Title + " | مجله علی‌رضا زارع", relatedArticle.Summary)));
        }

        return NotFound();
    }

    [HttpGet("Details/{slug}")]
    public IActionResult LegacyDetails(string slug) =>
        RedirectToActionPermanent(nameof(Details), new { slug });
}
