using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

[Route("brands")]
public sealed class BrandsController : PageController
{
    private readonly Shop.Services.ICatalogSource _catalog;

    public BrandsController(Shop.Services.ICatalogSource catalog)
    {
        _catalog = catalog;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        PageModel<IReadOnlyList<BrandProfileModel>> model = new(
            (await _catalog.ReadAsync(HttpContext.RequestAborted)).Brands,
            new SeoModel("برندهای ابزار برقی و تجهیزات اندازه‌گیری | ندا اولیائی",
                "معرفی برندهای ابزار برقی، دریل و پیچ‌گوشتی، ابزار اندازه‌گیری و تجهیزات لحیم‌کاری؛ بررسی محصولات و راهنمای انتخاب هر برند.",
                Breadcrumbs: [new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("برندها", "/brands")]));
        return await PageAsync(model);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        BrandProfileModel? brand = (await _catalog.ReadAsync(HttpContext.RequestAborted)).Brands.FirstOrDefault(brand =>
            string.Equals(brand.Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (brand is null)
        {
            return NotFound();
        }

        PageModel<BrandProfileModel> model = new(
            brand,
            new SeoModel(
                $"برند {brand.PersianName}؛ محصولات و راهنمای انتخاب | ندا اولیائی",
                brand.Summary,
                CanonicalUrl: $"/brands/{brand.Slug}",
                ImageUrl: "/shop/images/heroes/hero-electrical-editorial.webp",
                Breadcrumbs:
                [
                    new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("برندها", "/brands"),
                    new SeoBreadcrumb(brand.PersianName, $"/brands/{brand.Slug}")
                ]));
        return await PageAsync(model);
    }

}
