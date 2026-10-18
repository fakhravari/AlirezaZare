using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class ShippingController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("شرایط ارسال | ندا اولیائی", "راهنمای آماده‌سازی سفارش، روش و هزینه ارسال و نکات زمان تحویل کالا؛ جزئیات نهایی با توجه به مقصد و موجودی هنگام تأیید سفارش مشخص می‌شود.", Breadcrumbs: [new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("شرایط ارسال", "/shipping")]));
        return await PageAsync(model);
    }
}
