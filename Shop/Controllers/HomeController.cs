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
                "فروشگاه تخصصی برق و ابزار ندا اولیائی؛ عرضه ابزار برقی، تجهیزات اندازه‌گیری، قطعات الکترونیکی و روشنایی با تضمین اصالت و خدمات پس از فروش."),
            IsHome: true);

        return await PageAsync(model);
    }
}
