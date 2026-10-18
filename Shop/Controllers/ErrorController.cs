using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class ErrorController : PageController
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Index()
    {
        Response.StatusCode = 500;
        PageModel model = new(new SeoModel("خطا | فروشگاه ندا اولیائی", "در پردازش درخواست مشکلی رخ داد. به صفحهٔ اصلی برگردید یا دوباره تلاش کنید.", Robots: "noindex, nofollow"));
        return await PageAsync(model);
    }
}
