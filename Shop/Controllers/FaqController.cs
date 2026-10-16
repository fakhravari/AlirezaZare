using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class FaqController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("پرسش‌های متداول | ندا اولیائی", "پرسش‌های متداول؛ پاسخ‌های کوتاه، انتخاب آسان"));
        return await PageAsync(model);
    }
}
