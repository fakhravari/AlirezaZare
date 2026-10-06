using ClockTemplate.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClockTemplate.Controllers;

public sealed class FaqController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("پرسش‌های متداول | کمپانی علی‌رضا زارع", "پرسش‌های متداول؛ پاسخ‌های کوتاه، انتخاب آسان"));
        return await PageAsync(model);
    }
}
