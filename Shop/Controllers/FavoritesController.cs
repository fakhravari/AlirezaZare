using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class FavoritesController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("علاقه‌مندی‌ها | ندا اولیائی", "علاقه‌مندی‌ها؛ انتخاب‌هایی برای بعد"));
        return await PageAsync(model);
    }
}
