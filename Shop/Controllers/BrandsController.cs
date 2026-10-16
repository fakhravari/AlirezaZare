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
                ImageUrl: brand.LogoFile is null ? null : $"/Shop/images/brands/{brand.LogoFile}"));
        return await PageAsync(model);
    }
}
