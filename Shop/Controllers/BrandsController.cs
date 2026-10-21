using Shop.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Shop.Controllers;

[Route("brands")]
public sealed class BrandsController : PageController
{
    private readonly IWebHostEnvironment _environment;
    private readonly Shop.Services.CatalogRepository _catalog;

    public BrandsController(IWebHostEnvironment environment, Shop.Services.CatalogRepository catalog)
    {
        _environment = environment;
        _catalog = catalog;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        PageModel<IReadOnlyList<BrandProfileModel>> model = new(_catalog.Read().Brands,
            new SeoModel("برندهای ابزار برقی و تجهیزات اندازه‌گیری | ندا اولیائی", "معرفی برندهای ابزار برقی، دریل و پیچ‌گوشتی، ابزار اندازه‌گیری و تجهیزات لحیم‌کاری؛ بررسی محصولات و راهنمای انتخاب هر برند.", Breadcrumbs: [new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("برندها", "/brands")]));
        return await PageAsync(model);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        BrandProfileModel? brand = _catalog.Read().Brands.FirstOrDefault(brand => string.Equals(brand.Slug, slug, StringComparison.OrdinalIgnoreCase));
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
                ImageUrl: "/shop/images/heroes/hero-electrical-editorial.png",
                Breadcrumbs: [new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("برندها", "/brands"), new SeoBreadcrumb(brand.PersianName, $"/brands/{brand.Slug}")]));
        return await PageAsync(model);
    }

    private BrandEditorialDocument? FindEditorial(string slug)
    {
        string contentPath = Path.Combine(_environment.WebRootPath, "shop", "js", "store", "brand-editorial.json");
        BrandEditorialDocument[] documents = JsonSerializer.Deserialize<BrandEditorialDocument[]>(System.IO.File.ReadAllText(contentPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        return documents.FirstOrDefault(document => string.Equals(document.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }
}
