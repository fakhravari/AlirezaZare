using ClockTemplate.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClockTemplate.Controllers;

public sealed class ProductController : PageController
{
    [HttpGet("/Search")]
    public async Task<IActionResult> Index()
    {
        PageModel model = new(
            new SeoModel("جستجوی پیشرفته ساعت | هادی نوروزی", "جستجو و فیلتر پیشرفته ساعت‌های کمپانی هادی نوروزی"),
            ViewName: "List");
        return await PageAsync(model);
    }

    [HttpGet("/Product")]
    public async Task<IActionResult> Details([FromQuery] int? id)
    {
        PageModel<ProductDetailsModel> model = new(
            new ProductDetailsModel(id),
            new SeoModel("جزئیات ساعت | فروشگاه هادی نوروزی", "مشخصات و قیمت ساعت‌های اورجینال از برندهای معتبر"));
        return await PageAsync(model);
    }
}
