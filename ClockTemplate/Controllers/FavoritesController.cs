using ClockTemplate.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClockTemplate.Controllers;

public sealed class FavoritesController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("علاقه‌مندی‌ها | کمپانی هادی نوروزی", "علاقه‌مندی‌ها؛ انتخاب‌هایی برای بعد"));
        return await PageAsync(model);
    }
}
