using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class CompareController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("مقایسه محصولات | ندا اولیائی", "مشخصات و ویژگی‌های محصولات انتخاب‌شده را کنار هم مقایسه کنید.", Robots: "noindex, nofollow"));
        return await PageAsync(model);
    }
}
