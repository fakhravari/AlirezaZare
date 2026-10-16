using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class LegalController : PageController
{
    public Task<ViewResult> Privacy() => PageAsync(new PageModel(
        new SeoModel("حریم خصوصی | ندا اولیائی", "نحوه نگهداری و استفاده از اطلاعات و تنظیمات شما در فروشگاه ندا اولیائی."),
        "Privacy"));

    public Task<ViewResult> Terms() => PageAsync(new PageModel(
        new SeoModel("شرایط استفاده | ندا اولیائی", "شرایط استفاده از وب‌سایت، حساب کاربری، سبد خرید و محتوای فروشگاه."),
        "Terms"));

    public Task<ViewResult> Returns() => PageAsync(new PageModel(
        new SeoModel("رویه بازگرداندن کالا | ندا اولیائی", "راهنمای پیگیری انصراف از خرید، کالای معیوب یا مغایر با سفارش."),
        "Returns"));
}
