using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class BrandsController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("برندهای ابزار و تجهیزات برق | کمپانی علی‌رضا زارع", "برندهای معتبر ابزار برقی، ابزار اندازه‌گیری و تجهیزات فنی"));
        return await PageAsync(model);
    }
}
