using ClockTemplate.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClockTemplate.Controllers;

public sealed class LegalController : PageController
{
    public Task<ViewResult> Privacy() => PageAsync(new PageModel(
        new SeoModel("حریم خصوصی | کمپانی علی‌رضا زارع", "نحوه نگهداری و استفاده از اطلاعات و تنظیمات شما در فروشگاه کمپانی علی‌رضا زارع."),
        "Privacy"));

    public Task<ViewResult> Terms() => PageAsync(new PageModel(
        new SeoModel("شرایط استفاده | کمپانی علی‌رضا زارع", "شرایط استفاده از وب‌سایت، حساب کاربری، سبد خرید و محتوای فروشگاه."),
        "Terms"));

    public Task<ViewResult> Returns() => PageAsync(new PageModel(
        new SeoModel("رویه بازگرداندن کالا | کمپانی علی‌رضا زارع", "راهنمای پیگیری انصراف از خرید، کالای معیوب یا مغایر با سفارش."),
        "Returns"));
}
