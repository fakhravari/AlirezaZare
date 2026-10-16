using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class TrackingController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("پیگیری سفارش | ندا اولیائی", "پیگیری سفارش؛ همراه شما، پس از خرید"));
        return await PageAsync(model);
    }
}
