using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class AboutController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("درباره ما | ندا اولیائی", "با فروشگاه ندا اولیائی آشنا شوید؛ حوزه‌های ابزار برقی، اندازه‌گیری و قطعات فنی، شیوهٔ معرفی محصولات و راه‌های ارتباط با مجموعه را بخوانید.", Breadcrumbs: [new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("درباره ما", "/about")]));
        return await PageAsync(model);
    }
}
