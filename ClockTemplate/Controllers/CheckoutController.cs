using ClockTemplate.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClockTemplate.Controllers;

public sealed class CheckoutController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("پیش‌فاکتور و پرداخت نهایی | کمپانی علی‌رضا زارع", "بررسی پیش‌فاکتور، انتخاب آدرس و پرداخت نهایی"));
        return await PageAsync(model);
    }
}
