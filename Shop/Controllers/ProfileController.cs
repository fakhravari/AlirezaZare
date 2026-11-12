using Shop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Controllers;

[Route("Profile")]
public sealed class ProfileController(Shop.Services.IAccountDataSource accountDataSource, ShopDbContext db) : PageController
{
    [HttpGet("/account")]
    public IActionResult LegacyAccount() => RedirectToAction(nameof(Index));

    [HttpGet("")]
    public Task<IActionResult> Index() => ProfilePage("profile", "پروفایل من",
        "جزئیات پروفایل، نشانی‌ها و کالاهای ذخیره‌شده را مدیریت کنید.", "bi-person");

    [HttpGet("Edit")]
    public IActionResult Edit() => RedirectToAction(nameof(Index));

    [HttpGet("Orders")]
    public Task<IActionResult> Orders() => ProfilePage("orders", "سفارش‌های من",
        "وضعیت سفارش‌ها و سوابق خریدتان را یک‌جا دنبال کنید.", "bi-bag-check");

    [HttpGet("Addresses")]
    public Task<IActionResult> Addresses() => ProfilePage("addresses", "آدرس‌های من",
        "نشانی‌های تحویل را مدیریت کنید تا خرید بعدی ساده‌تر باشد.", "bi-geo-alt");

    [HttpGet("Favorites")]
    public Task<IActionResult> Favorites() => ProfilePage("favorites", "علاقه‌مندی‌ها",
        "محصول‌های ذخیره‌شده را مرور کنید و برای خرید مقایسه‌شان کنید.", "bi-heart");

    [HttpGet("Coupons")]
    public Task<IActionResult> Coupons() => ProfilePage("coupons", "کدهای تخفیف من",
        "وضعیت کدها، تعداد استفاده و فاکتورهای مرتبط را ببینید.", "bi-ticket-perforated");

    [HttpGet("PriceAlerts")]
    public Task<IActionResult> PriceAlerts() => ProfilePage("pricealerts", "به من اطلاع بده",
        "پیگیری‌های قیمت و موجودی کالاهای منتخب را مدیریت کنید.", "bi-bell");

    [HttpGet("Messages")]
    public Task<IActionResult> Messages() => ProfilePage("messages", "پیام‌ها",
        "پیام‌ها و اطلاعیه‌های ارسال‌شده از طرف فروشگاه را بخوانید.", "bi-envelope");

    private async Task<IActionResult> ProfilePage(string section, string title, string description, string icon)
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
                coupon.OrderIds.Where(ordersById.ContainsKey).Select(id => ordersById[id]).Select(order =>
                        new ProfileCouponUseViewModel(
                            order.Number,
                            order.DateLabel,
                            order.OrderedOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)))
                    .ToArray()))
            .ToArray();
        var genders = await db.AccountGenders.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new ProfileGenderOption(x.Id, x.Code, x.NameFa)).ToListAsync(HttpContext.RequestAborted);
        PageModel<ProfilePageViewModel> model = new(
            new ProfilePageViewModel(section, title, description, icon, Array.Empty<AccountOrder>(), coupons, genders),
            new SeoModel($"{title} | ندا اولیائی", description, Robots: "noindex, nofollow"),
            "~/Views/Profile/Index.cshtml");
        return await PageAsync(model);
    }
}
