using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class WarrantyController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("ضمانت و خدمات پس از فروش | ندا اولیائی", "مدارک خرید و کارت ضمانت را بررسی کنید؛ مدت پوشش، شرایط سرویس و روش پیگیری برای هر محصول در مدارک همان کالا مشخص می‌شود.", Breadcrumbs: [new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("ضمانت و خدمات پس از فروش", "/warranty")]));
        return await PageAsync(model);
    }
}
