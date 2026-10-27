using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

[Route("Profile")]
public sealed class ProfileController : PageController
{
    [HttpGet("")]
    public Task<ViewResult> Index() => ProfilePage("پروفایل من", "مشاهده و ویرایش اطلاعات پروفایل کاربری");

    [HttpGet("Edit")]
    public IActionResult Edit() => RedirectToAction(nameof(Index));

    [HttpGet("Orders")]
    public Task<ViewResult> Orders() => ProfilePage("سفارش‌های من", "مشاهده سوابق و وضعیت سفارش‌ها");

    [HttpGet("Addresses")]
    public Task<ViewResult> Addresses() => ProfilePage("آدرس‌های من", "مدیریت آدرس‌های تحویل سفارش");

    [HttpGet("Favorites")]
    public Task<ViewResult> Favorites() => ProfilePage("علاقه‌مندی‌های من", "کالاهای ذخیره‌شده شما");

    [HttpGet("Coupons")]
    public Task<ViewResult> Coupons() => ProfilePage("کدهای تخفیف من", "مشاهده کدهای تخفیف فعال و استفاده از آن‌ها");

    [HttpGet("PriceAlerts")]
    public Task<ViewResult> PriceAlerts() => ProfilePage("به من اطلاع بده", "مدیریت پیگیری‌های آزمایشی تغییر قیمت");

    private Task<ViewResult> ProfilePage(string title, string description)
    {
        PageModel model = new(new SeoModel($"{title} | ندا اولیائی", description, Robots: "noindex, nofollow"), "~/Views/Profile/Index.cshtml");
        return PageAsync(model);
    }
}
