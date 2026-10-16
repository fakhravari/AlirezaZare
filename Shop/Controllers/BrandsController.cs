using Shop.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Shop.Controllers;

[Route("brands")]
public sealed class BrandsController : PageController
{
    private readonly IWebHostEnvironment _environment;

    public BrandsController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

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

        string contentPath = Path.Combine(
            _environment.WebRootPath,
            "Shop",
            "js",
            "store",
            "brand-editorial.json");
        using JsonDocument document = JsonDocument.Parse(System.IO.File.ReadAllText(contentPath));
        JsonElement source = document.RootElement.EnumerateArray().FirstOrDefault(item =>
            string.Equals(item.GetProperty("slug").GetString(), brand.Slug, StringComparison.OrdinalIgnoreCase));
        if (source.ValueKind != JsonValueKind.Object)
        {
            return NotFound();
        }

        return Json(new
        {
            Slug = source.GetProperty("slug").GetString(),
            brand.Name,
            PersianName = source.GetProperty("persianName").GetString(),
            CompanyIntroductions = source.GetProperty("companyIntroductions"),
            Established = source.GetProperty("established").GetString(),
            Country = source.GetProperty("country").GetString(),
            OfficialUrl = source.GetProperty("officialUrl").GetString(),
            Specialization = source.GetProperty("specialization").GetString(),
            FeaturedProducts = source.GetProperty("featuredProducts").GetString(),
            ProductFamilies = source.GetProperty("productFamilies"),
            Strengths = source.GetProperty("strengths"),
            Audience = source.GetProperty("audience").GetString(),
            ShoppingAdvice = source.GetProperty("shoppingAdvice").GetString()
        });
    }
}
