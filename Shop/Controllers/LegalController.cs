using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class LegalController : PageController
{
    public Task<ViewResult> Privacy() => PageAsync(new PageModel(
        new SeoModel("حریم خصوصی | ندا اولیائی", "بخوانید چه فهرست‌ها و تنظیماتی در مرورگر ذخیره می‌شوند، فرم‌های سایت چگونه کار می‌کنند و برای مدیریت یا پاک‌کردن اطلاعات ذخیره‌شده از کجا اقدام کنید.", Breadcrumbs: [new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("حریم خصوصی", "/legal/privacy")]),
        "Privacy"));

    public Task<ViewResult> Terms() => PageAsync(new PageModel(
        new SeoModel("شرایط استفاده | ندا اولیائی", "شرایط استفاده از وب‌سایت، اطلاعات محصولات و قیمت‌ها، حساب کاربری و سبد خرید را بخوانید؛ پرداخت آنلاین و ثبت سفارش در حال حاضر فعال نیست.", Breadcrumbs: [new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("شرایط استفاده", "/legal/terms")]),
        "Terms"));

    public Task<ViewResult> Returns() => PageAsync(new PageModel(
        new SeoModel("رویه بازگرداندن کالا | ندا اولیائی", "راهنمای ثبت درخواست انصراف از خرید، پیگیری کالای معیوب یا مغایر با سفارش و هماهنگی بازگشت؛ شرایط هر درخواست بر اساس مقررات و اطلاعات همان خرید بررسی می‌شود.", Breadcrumbs: [new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("رویه بازگرداندن کالا", "/legal/returns")]),
        "Returns"));
}
