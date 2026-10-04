using ClockTemplate.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClockTemplate.Controllers;

public sealed class ErrorController : PageController
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Index()
    {
        Response.StatusCode = 500;
        PageModel model = new(new SeoModel("خطا", "خطایی در پردازش درخواست رخ داده."));
        return await PageAsync(model);
    }
}
