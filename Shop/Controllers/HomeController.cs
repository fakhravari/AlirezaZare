using Shop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Controllers;

public sealed class HomeController : PageController
{
    private readonly SiteIdentityOptions _identity;
    private readonly Shop.Services.IMagazineContentSource _magazine;
    private readonly ShopDbContext _db;

    public HomeController(IOptions<SiteIdentityOptions> identity, Shop.Services.IMagazineContentSource magazine, ShopDbContext db)
    {
        _identity = identity.Value;
        _magazine = magazine;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        PageModel model = new(
            new SeoModel(
                $"{_identity.StoreName} | ابزار، قطعات و تجهیزات حرفه‌ای",
                $"{_identity.StoreName}؛ بررسی ابزار برقی، تجهیزات اندازه‌گیری، قطعات الکترونیکی و روشنایی همراه با اطلاعات هر کالا و راه ارتباط با فروشگاه."),
            IsHome: true);

        var magazine = await _magazine.ReadIndexAsync(HttpContext.RequestAborted);
        var approvedReviews = _db.ProductFeedback.AsNoTracking()
            .Where(x => x.FeedbackType == "reviews" && x.Status == "approved");
        int reviewCount = await approvedReviews.CountAsync(HttpContext.RequestAborted);
        double reviewAverage = reviewCount == 0 ? 0 : await approvedReviews.AverageAsync(x => (double?)(x.Rating ?? 0), HttpContext.RequestAborted) ?? 0;
        var reviews = await approvedReviews
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .Take(6).Select(x => new HomeReviewCard(x.DisplayName, x.Product.Name, x.FeedbackText, x.Rating ?? 0, x.CreatedAtUtc))
            .ToListAsync(HttpContext.RequestAborted);
        ViewData["HomeReviews"] = reviews;
        ViewData["HomeReviewAverage"] = Math.Round(reviewAverage, 1);
        ViewData["HomeReviewCount"] = reviewCount;
        return await PageAsync(new PageModel<MagazineIndexViewModel>(magazine, model.Seo, IsHome: true));
    }
}

public sealed record HomeReviewCard(string DisplayName, string ProductName, string Text, int Rating, DateTime CreatedAtUtc);
