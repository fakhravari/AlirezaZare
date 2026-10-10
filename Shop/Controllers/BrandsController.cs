using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class BrandsController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("برندهای ساعت | کمپانی علی‌رضا زارع", "برندهای ساعت؛ امضای متفاوت زمان"));
        return await PageAsync(model);
    }
}
