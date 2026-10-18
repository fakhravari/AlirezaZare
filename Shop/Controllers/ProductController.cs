using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class ProductController : PageController
{
    private readonly IWebHostEnvironment _environment;

    public ProductController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet("/Search")]
    public async Task<IActionResult> Index()
    {
        PageModel model = new(
            new SeoModel("خرید ابزار برقی و تجهیزات فنی | فروشگاه ندا اولیائی", "جستجو و بررسی ابزار برقی، تجهیزات اندازه‌گیری، ابزار لحیم‌کاری و روشنایی؛ کالاها را بر اساس برند، کاربرد و قیمت فیلتر کنید.", CanonicalUrl: "/search", Breadcrumbs: [new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("جستجو و انتخاب ابزار", "/search")]),
            ViewName: "List");
        return await PageAsync(model);
    }

    [HttpGet("/products/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var separator = slug.LastIndexOf('-');
        if (separator < 0 || !int.TryParse(slug[(separator + 1)..], out var id) || id < 1)
            return NotFound();

        ProductSeoEntry? product = ProductSeoCatalog.Load(_environment.WebRootPath)
            .FirstOrDefault(item => item.Id == id);
        if (product is null) return NotFound();

        string canonicalPath = $"/products/{product.Slug}-{product.Id}";
        string productImage = $"{Request.Scheme}://{Request.Host}{Url.Content($"~/{product.ImagePath}")}";
        var structuredData = new Dictionary<string, object?>
        {
            ["@type"] = "Product",
            ["name"] = product.Name,
            ["description"] = product.Description,
            ["image"] = new[] { productImage },
            ["category"] = product.Type,
            ["brand"] = new Dictionary<string, object?> { ["@type"] = "Brand", ["name"] = product.Brand },
            ["url"] = $"{Request.Scheme}://{Request.Host}{Url.Content($"~{canonicalPath}")}"
        };

        PageModel<ProductDetailsModel> model = new(
            new ProductDetailsModel(id, product.Name),
            new SeoModel(
                $"{product.Name} {product.Brand} | فروشگاه ندا اولیائی",
                product.Description,
                CanonicalUrl: canonicalPath,
                ImageUrl: productImage,
                StructuredData: structuredData,
                SocialImageAlt: product.Name,
                Breadcrumbs:
                [
                    new SeoBreadcrumb("خانه", "/"),
                    new SeoBreadcrumb(product.Name, canonicalPath)
                ]));
        return await PageAsync(model);
    }

    [HttpGet("/products/{id:int}")]
    public IActionResult LegacyPath(int id) =>
        id > 0 ? RedirectPermanent($"/products/product-{id}") : NotFound();

    [HttpGet("/Product")]
    public IActionResult LegacyDetails([FromQuery] int? id) =>
        id is > 0 ? RedirectPermanent($"/products/product-{id.Value}") : NotFound();
}
