using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class CartController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("سبد خرید | ندا اولیائی", "سبد خرید؛ یک قدم نزدیک‌تر به انتخاب شما"));
        return await PageAsync(model);
    }
}
