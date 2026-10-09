using ClockTemplate.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClockTemplate.Controllers;

public sealed class CompareController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("مقایسه محصولات | کمپانی علی‌رضا زارع", "محصولات انتخاب‌شده را کنار هم بررسی کنید."));
        return await PageAsync(model);
    }
}
