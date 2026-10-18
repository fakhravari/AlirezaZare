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
            new SeoModel("برندهای ابزار برقی و تجهیزات اندازه‌گیری | ندا اولیائی", "معرفی برندهای ابزار برقی، دریل و پیچ‌گوشتی، ابزار اندازه‌گیری و تجهیزات لحیم‌کاری؛ بررسی محصولات و راهنمای انتخاب هر برند.", Breadcrumbs: [new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("برندها", "/brands")]));
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
                ImageUrl: "/Shop/images/heroes/hero-electrical-editorial.png",
                Breadcrumbs: [new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("برندها", "/brands"), new SeoBreadcrumb(brand.PersianName, $"/brands/{brand.Slug}")]));
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
