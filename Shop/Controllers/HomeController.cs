using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class HomeController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(
            new SeoModel(
                "فروشگاه برق و ابزار ندا اولیائی | ابزار، قطعات و تجهیزات حرفه‌ای",
                "فروشگاه تخصصی برق و ابزار ندا اولیائی؛ بررسی ابزار برقی، تجهیزات اندازه‌گیری، قطعات الکترونیکی و روشنایی همراه با اطلاعات هر کالا و راه ارتباط با فروشگاه."),
            IsHome: true);

        return await PageAsync(model);
    }
}
