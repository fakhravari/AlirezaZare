using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class ProductController : PageController
{
    [HttpGet("/Search")]
    public async Task<IActionResult> Index()
    {
        PageModel model = new(
            new SeoModel("جستجوی پیشرفته برق و ابزار | علی‌رضا زارع", "جستجو و فیلتر ابزار برقی، قطعات الکترونیکی و تجهیزات فنی علی‌رضا زارع"),
            ViewName: "List");
        return await PageAsync(model);
    }

    [HttpGet("/products/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var separator = slug.LastIndexOf('-');
        if (separator < 0 || !int.TryParse(slug[(separator + 1)..], out var id) || id < 1)
            return NotFound();

        PageModel<ProductDetailsModel> model = new(
            new ProductDetailsModel(id),
            new SeoModel("جزئیات محصول | فروشگاه برق و ابزار علی‌رضا زارع", "مشخصات، قیمت و شرایط خرید ابزار و تجهیزات برق از برندهای معتبر"));
        return await PageAsync(model);
    }

    [HttpGet("/products/{id:int}")]
    public IActionResult LegacyPath(int id) =>
        id > 0 ? RedirectPermanent($"/products/product-{id}") : NotFound();

    [HttpGet("/Product")]
    public IActionResult LegacyDetails([FromQuery] int? id) =>
        id is > 0 ? RedirectPermanent($"/products/product-{id.Value}") : NotFound();
}
