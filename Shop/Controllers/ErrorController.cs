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

    [HttpGet("/Error/{statusCode:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Status(int statusCode)
    {
        int responseCode = statusCode is >= 400 and <= 599 ? statusCode : 404;
        Response.StatusCode = responseCode;
        bool isNotFound = responseCode == 404;
        PageModel model = new(new SeoModel(
            isNotFound ? "صفحه پیدا نشد | فروشگاه ندا اولیائی" : "خطا در درخواست | فروشگاه ندا اولیائی",
            isNotFound ? "این نشانی در فروشگاه پیدا نشد. از جستجوی کالاها استفاده کنید یا به صفحهٔ اصلی برگردید." : "در پردازش این درخواست مشکلی رخ داد. دوباره تلاش کنید یا به صفحهٔ اصلی برگردید.",
            Robots: "noindex, nofollow"), ViewName: "Index");
        return await PageAsync(model);
    }
}
