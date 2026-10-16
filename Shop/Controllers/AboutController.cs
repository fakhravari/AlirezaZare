using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class AboutController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("درباره ما | ندا اولیائی", "درباره ندا اولیائی؛ تأمین ابزار برقی، تجهیزات اندازه‌گیری و قطعات فنی با ضمانت اصالت."));
        return await PageAsync(model);
    }
}
