using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class CartController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("سبد خرید | ندا اولیائی", "سبد خرید؛ بررسی کالاهای انتخاب‌شده و ادامهٔ فرایند خرید.", Robots: "noindex, nofollow"));
        return await PageAsync(model);
    }
}
