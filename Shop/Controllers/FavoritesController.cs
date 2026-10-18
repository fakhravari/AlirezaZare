using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class FavoritesController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("علاقه‌مندی‌ها | ندا اولیائی", "فهرست کالاهایی که برای بررسی یا خرید بعدی ذخیره کرده‌اید.", Robots: "noindex, nofollow"));
        return await PageAsync(model);
    }
}
