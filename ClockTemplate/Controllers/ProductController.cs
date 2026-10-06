using ClockTemplate.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClockTemplate.Controllers;

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

    [HttpGet("/Product")]
    public async Task<IActionResult> Details([FromQuery] int? id)
    {
        PageModel<ProductDetailsModel> model = new(
            new ProductDetailsModel(id),
            new SeoModel("جزئیات محصول | فروشگاه برق و ابزار علی‌رضا زارع", "مشخصات، قیمت و شرایط خرید ابزار و تجهیزات برق از برندهای معتبر"));
        return await PageAsync(model);
    }
}
