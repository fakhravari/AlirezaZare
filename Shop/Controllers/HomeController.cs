using Shop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Shop.Controllers;

public sealed class HomeController : PageController
{
    private readonly SiteIdentityOptions _identity;

    public HomeController(IOptions<SiteIdentityOptions> identity) => _identity = identity.Value;

    public async Task<IActionResult> Index()
    {
        PageModel model = new(
            new SeoModel(
                $"{_identity.StoreName} | ابزار، قطعات و تجهیزات حرفه‌ای",
                $"{_identity.StoreName}؛ بررسی ابزار برقی، تجهیزات اندازه‌گیری، قطعات الکترونیکی و روشنایی همراه با اطلاعات هر کالا و راه ارتباط با فروشگاه."),
            IsHome: true);

        return await PageAsync(model);
    }
}
