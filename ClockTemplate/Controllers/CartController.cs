using ClockTemplate.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClockTemplate.Controllers;

public sealed class CartController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("سبد خرید | کمپانی هادی نوروزی", "سبد خرید؛ یک قدم نزدیک‌تر به انتخاب شما"));
        return await PageAsync(model);
    }
}
