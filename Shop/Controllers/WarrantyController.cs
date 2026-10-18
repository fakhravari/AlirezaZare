using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class WarrantyController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("ضمانت اصالت | ندا اولیائی", "مدارک خرید، شماره مدل و کارت ضمانت را هنگام تحویل بررسی کنید؛ شرایط پوشش، مدت خدمات و روش پیگیری برای هر محصول در مدارک همان کالا مشخص می‌شود.", Breadcrumbs: [new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("ضمانت اصالت", "/warranty")]));
        return await PageAsync(model);
    }
}
