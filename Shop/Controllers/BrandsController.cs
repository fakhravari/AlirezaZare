using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

[Route("brands")]
public sealed class BrandsController : PageController
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        PageModel<IReadOnlyList<BrandProfileModel>> model = new(
            BrandCatalog.All,
            new SeoModel("برندهای ابزار و تجهیزات برق | کمپانی علی‌رضا زارع", "برندهای معتبر ابزار برقی، ابزار اندازه‌گیری و تجهیزات فنی"));
        return await PageAsync(model);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        BrandProfileModel? brand = BrandCatalog.Find(slug);
        if (brand is null)
        {
            return NotFound();
        }

        PageModel<BrandProfileModel> model = new(
            brand,
            new SeoModel(
                $"برند {brand.PersianName}؛ محصولات و راهنمای انتخاب | علی‌رضا زارع",
                brand.Summary,
                CanonicalUrl: $"/brands/{brand.Slug}",
                ImageUrl: "/Shop/images/heroes/hero-electrical-editorial.png"));
        return await PageAsync(model);
    }

    [HttpGet("{slug}/editorial")]
    public IActionResult Editorial(string slug)
    {
        BrandProfileModel? brand = BrandCatalog.Find(slug);
        if (brand is null)
        {
            return NotFound();
        }

        return Json(new
        {
            brand.Slug,
            brand.Name,
            brand.PersianName,
            Introduction = brand.Editorial ?? brand.Summary,
            brand.Focus,
            brand.FoundedYear,
            brand.OfficialUrl,
            brand.EditorialContent.ProductFamilies,
            brand.EditorialContent.Strengths,
            brand.EditorialContent.Audience
        });
    }
}
