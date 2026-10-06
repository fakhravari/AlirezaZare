using ClockTemplate.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClockTemplate.Controllers;

[Route("Gallery")]
public sealed class GalleryController : PageController
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel(
            "گالری ساعت | علی‌رضا زارع",
            "روایت‌های تصویری از ساعت‌ها، جزئیات طراحی و انتخاب‌های فروشگاه علی‌رضا زارع."));
        return await PageAsync(model);
    }
}
