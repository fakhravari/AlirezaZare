using ClockTemplate.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClockTemplate.Controllers;

public sealed class ShippingController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("شرایط ارسال | کمپانی علی‌رضا زارع", "شرایط ارسال؛ از انتخاب تا تحویل"));
        return await PageAsync(model);
    }
}
