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
        PageModel<IReadOnlyList<BrandProfileModel>> model = new(BrandCatalog.All,
            new SeoModel("برندهای ابزار و تجهیزات برق | ندا اولیائی", "برندهای معتبر ابزار برقی، ابزار اندازه‌گیری و تجهیزات فنی"));
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

        brand = brand with { SourceEditorial = FindEditorial(brand.Slug) };

        PageModel<BrandProfileModel> model = new(
            brand,
            new SeoModel(
                $"برند {brand.PersianName}؛ محصولات و راهنمای انتخاب | ندا اولیائی",
                brand.Summary,
                CanonicalUrl: $"/brands/{brand.Slug}",
                ImageUrl: "/Shop/images/heroes/hero-electrical-editorial.png"));
        return await PageAsync(model);
    }

    private BrandEditorialDocument? FindEditorial(string slug)
    {
        string contentPath = Path.Combine(_environment.WebRootPath, "Shop", "js", "store", "brand-editorial.json");
        BrandEditorialDocument[] documents = JsonSerializer.Deserialize<BrandEditorialDocument[]>(System.IO.File.ReadAllText(contentPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        return documents.FirstOrDefault(document => string.Equals(document.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }
}
