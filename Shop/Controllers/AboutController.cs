using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class AboutController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("درباره ما | کمپانی علی‌رضا زارع", "درباره کمپانی علی‌رضا زارع؛ واردات و عرضه تخصصی ساعت‌های اورجینال با ضمانت اصالت."));
        return await PageAsync(model);
    }
}
