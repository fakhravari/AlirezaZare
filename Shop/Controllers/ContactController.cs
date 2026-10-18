using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class ContactController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel(
            "تماس با ما | ندا اولیائی",
            "راه‌های تماس با ندا اولیائی برای مشاوره پیش از خرید، پیگیری سفارش و خدمات پس از فروش.",
            Breadcrumbs: [new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("تماس با ما", "/contact")]));
        return await PageAsync(model);
    }
}
