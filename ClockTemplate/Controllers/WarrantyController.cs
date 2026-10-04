using ClockTemplate.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClockTemplate.Controllers;

public sealed class WarrantyController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("ضمانت اصالت | کمپانی هادی نوروزی", "ضمانت اصالت؛ اطمینان، با جزئیات روشن"));
        return await PageAsync(model);
    }
}
