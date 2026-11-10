using Shop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Shop.Controllers;

public sealed class ContactController : PageController
{
    private readonly SiteIdentityOptions _identity;

    public ContactController(IOptions<SiteIdentityOptions> identity) => _identity = identity.Value;

    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel(
            $"تماس با ما | {_identity.DisplayName}",
            $"راه‌های تماس با {_identity.DisplayName} برای مشاوره پیش از خرید، پیگیری سفارش و خدمات پس از فروش.",
            Breadcrumbs: [new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("تماس با ما", "/contact")]));
        return await PageAsync(model with { ContentKey = "contact" });
    }
}
