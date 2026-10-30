using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

[Route("Profile")]
public sealed class ProfileController(Shop.Services.IAccountDataSource accountDataSource) : PageController
{
    [HttpGet("")]
    public Task<ViewResult> Index() => ProfilePage("profile", "پروفایل من", "جزئیات پروفایل، نشانی‌ها و کالاهای ذخیره‌شده را مدیریت کنید.", "bi-person");

    [HttpGet("Edit")]
    public IActionResult Edit() => RedirectToAction(nameof(Index));

    [HttpGet("Orders")]
    public Task<ViewResult> Orders() => ProfilePage("orders", "سفارش‌های من", "وضعیت سفارش‌ها و سوابق خریدتان را یک‌جا دنبال کنید.", "bi-bag-check");

    [HttpGet("Addresses")]
    public Task<ViewResult> Addresses() => ProfilePage("addresses", "آدرس‌های من", "نشانی‌های تحویل را مدیریت کنید تا خرید بعدی ساده‌تر باشد.", "bi-geo-alt");

    [HttpGet("Favorites")]
    public Task<ViewResult> Favorites() => ProfilePage("favorites", "علاقه‌مندی‌ها", "محصول‌های ذخیره‌شده را مرور کنید و برای خرید مقایسه‌شان کنید.", "bi-heart");

    [HttpGet("Coupons")]
    public Task<ViewResult> Coupons() => ProfilePage("coupons", "کدهای تخفیف من", "وضعیت کدها، تعداد استفاده و فاکتورهای مرتبط را ببینید.", "bi-ticket-perforated");

    [HttpGet("PriceAlerts")]
    public Task<ViewResult> PriceAlerts() => ProfilePage("pricealerts", "به من اطلاع بده", "پیگیری‌های قیمت و موجودی کالاهای منتخب را مدیریت کنید.", "bi-bell");

    private async Task<ViewResult> ProfilePage(string section, string title, string description, string icon)
    {
        var account = await accountDataSource.ReadAsync(HttpContext.RequestAborted);
        var ordersById = account.Orders.ToDictionary(order => order.Id, StringComparer.Ordinal);
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        var coupons = account.Coupons.Select(coupon => new ProfileCouponViewModel(
            coupon.Id,
            coupon.Code,
            coupon.DiscountPercent,
            coupon.UsageLimit,
            coupon.IsActive(today),
            AccountDisplay.Date(coupon.ExpiresOn),
            coupon.OrderIds.Select(id => ordersById[id]).Select(order => new ProfileCouponUseViewModel(
                order.Number,
                order.DateLabel,
                order.OrderedOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture))).ToArray())).ToArray();
        PageModel<ProfilePageViewModel> model = new(
            new ProfilePageViewModel(section, title, description, icon, account.Orders, coupons),
            new SeoModel($"{title} | ندا اولیائی", description, Robots: "noindex, nofollow"),
            "~/Views/Profile/Index.cshtml");
        return await PageAsync(model);
    }
}
