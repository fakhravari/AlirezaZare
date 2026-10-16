using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class ContactController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel(
            "تماس با ما | کمپانی علی‌رضا زارع",
            "راه‌های تماس با کمپانی علی‌رضا زارع برای مشاوره پیش از خرید، پیگیری سفارش و خدمات پس از فروش."));
        return await PageAsync(model);
    }
}
