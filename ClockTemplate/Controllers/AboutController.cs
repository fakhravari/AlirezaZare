using ClockTemplate.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClockTemplate.Controllers;

public sealed class AboutController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("درباره ما | کمپانی هادی نوروزی", "درباره کمپانی هادی نوروزی؛ واردات و عرضه تخصصی ساعت‌های اورجینال با ضمانت اصالت."));
        return await PageAsync(model);
    }
}
