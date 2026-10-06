using ClockTemplate.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClockTemplate.Controllers;

public sealed class TrackingController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("پیگیری سفارش | کمپانی علی‌رضا زارع", "پیگیری سفارش؛ همراه شما، پس از خرید"));
        return await PageAsync(model);
    }
}
