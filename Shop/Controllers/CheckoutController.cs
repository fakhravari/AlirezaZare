using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class CheckoutController : PageController
{
    public async Task<IActionResult> Index()
    {
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            if (long.TryParse(form["addressId"], out var addressId) && addressId > 0)
                ViewData["CheckoutAddressId"] = addressId;
            if (form["paymentMethod"] == "zarinpal")
                ViewData["CheckoutPaymentMethod"] = "zarinpal";
        }
        PageModel model = new(new SeoModel("پیش‌فاکتور و پرداخت نهایی | ندا اولیائی",
            "بررسی اقلام سفارش، انتخاب نشانی تحویل و ادامهٔ پرداخت نهایی.", Robots: "noindex, nofollow"));
        return await PageAsync(model);
    }
}