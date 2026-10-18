using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class TrackingController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("پیگیری سفارش | ندا اولیائی", "برای بررسی وضعیت سفارش، کد سفارش و شماره همراه ثبت‌شده را آماده کنید؛ راهنمای پیگیری و راه ارتباط با پشتیبانی فروشگاه در این صفحه آمده است.", Breadcrumbs: [new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("پیگیری سفارش", "/tracking")]));
        return await PageAsync(model);
    }
}
