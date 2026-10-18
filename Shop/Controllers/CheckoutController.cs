using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class CheckoutController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("پیش‌فاکتور و پرداخت نهایی | ندا اولیائی", "بررسی اقلام سفارش، انتخاب نشانی تحویل و ادامهٔ پرداخت نهایی.", Robots: "noindex, nofollow"));
        return await PageAsync(model);
    }
}
