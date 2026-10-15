using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

[Route("Profile")]
public sealed class ProfileController : PageController
{
    [HttpGet("")]
    public Task<ViewResult> Index() => ProfilePage("پروفایل من", "مدیریت اطلاعات و فعالیت‌های حساب کاربری");

    [HttpGet("Edit")]
    public IActionResult Edit() => RedirectToAction(nameof(Index));

    [HttpGet("Orders")]
    public Task<ViewResult> Orders() => ProfilePage("سفارش‌های من", "مشاهده سوابق و وضعیت سفارش‌ها");

    [HttpGet("Addresses")]
    public Task<ViewResult> Addresses() => ProfilePage("آدرس‌های من", "مدیریت آدرس‌های تحویل سفارش");

    [HttpGet("Favorites")]
    public Task<ViewResult> Favorites() => ProfilePage("علاقه‌مندی‌های من", "کالاهای ذخیره‌شده شما");

    private Task<ViewResult> ProfilePage(string title, string description)
    {
        PageModel model = new(new SeoModel($"{title} | کمپانی علی‌رضا زارع", description), "~/Views/Profile/Index.cshtml");
        return PageAsync(model);
    }
}
