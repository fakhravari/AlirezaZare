using Shop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Shop.Controllers;

public sealed class HomeController : PageController
{
    private readonly SiteIdentityOptions _identity;
    private readonly Shop.Services.IMagazineContentSource _magazine;

    public HomeController(IOptions<SiteIdentityOptions> identity, Shop.Services.IMagazineContentSource magazine)
    {
        _identity = identity.Value;
        _magazine = magazine;
    }

    public async Task<IActionResult> Index()
    {
        PageModel model = new(
            new SeoModel(
                $"{_identity.StoreName} | ابزار، قطعات و تجهیزات حرفه‌ای",
                $"{_identity.StoreName}؛ بررسی ابزار برقی، تجهیزات اندازه‌گیری، قطعات الکترونیکی و روشنایی همراه با اطلاعات هر کالا و راه ارتباط با فروشگاه."),
            IsHome: true);

        var magazine = await _magazine.ReadIndexAsync(HttpContext.RequestAborted);
        return await PageAsync(new PageModel<MagazineIndexViewModel>(magazine, model.Seo, IsHome: true));
    }
}