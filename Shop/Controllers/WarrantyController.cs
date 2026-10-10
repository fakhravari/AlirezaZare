using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class WarrantyController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("ضمانت اصالت | کمپانی علی‌رضا زارع", "ضمانت اصالت؛ اطمینان، با جزئیات روشن"));
        return await PageAsync(model);
    }
}
