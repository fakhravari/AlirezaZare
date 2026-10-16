using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class CompareController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("مقایسه محصولات | ندا اولیائی", "محصولات انتخاب‌شده را کنار هم بررسی کنید."));
        return await PageAsync(model);
    }
}
