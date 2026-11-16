using Shop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;
using Shop.Data.Application.Store;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Controllers;

public sealed class HomeController : PageController
{
    private readonly SiteIdentityOptions _identity;
    private readonly Shop.Services.IMagazineContentSource _magazine;
    private readonly Shop.Services.ICatalogSource _catalog;
    private readonly ShopDbContext _db;
    private readonly IMediaCdnBaseUrlProvider _mediaCdn;

    public HomeController(IOptions<SiteIdentityOptions> identity, Shop.Services.IMagazineContentSource magazine, Shop.Services.ICatalogSource catalog, ShopDbContext db, IMediaCdnBaseUrlProvider mediaCdn)
    {
        _identity = identity.Value;
        _magazine = magazine;
        _catalog = catalog;
        _db = db;
        _mediaCdn = mediaCdn;
    }

    public async Task<IActionResult> Index()
    {
        PageModel model = new(
            new SeoModel(
                $"{_identity.StoreName} | ابزار، قطعات و تجهیزات حرفه‌ای",
                $"{_identity.StoreName}؛ بررسی ابزار برقی، تجهیزات اندازه‌گیری، قطعات الکترونیکی و روشنایی همراه با اطلاعات هر کالا و راه ارتباط با فروشگاه."),
            IsHome: true);

        var magazine = await _magazine.ReadIndexAsync(HttpContext.RequestAborted);
        ViewData["HomeBrands"] = (await _catalog.ReadAsync(HttpContext.RequestAborted)).Brands;
        var approvedReviews = _db.ProductFeedback.AsNoTracking()
            .Where(x => x.FeedbackType == "reviews" && x.Status == "approved");
        int reviewCount = await approvedReviews.CountAsync(HttpContext.RequestAborted);
        double reviewAverage = reviewCount == 0 ? 0 : await approvedReviews.AverageAsync(x => (double?)(x.Rating ?? 0), HttpContext.RequestAborted) ?? 0;
        var reviewItems = await (from featured in _db.StoreHomeTestimonials.AsNoTracking()
            join feedback in approvedReviews on featured.IdProductFeedback equals feedback.Id
            where featured.IsActive
            orderby featured.SortOrder, feedback.CreatedAtUtc descending, feedback.Id descending
            select new { feedback.DisplayName, ProductName = feedback.Product.Name, feedback.FeedbackText,
                Rating = feedback.Rating ?? 0, feedback.CreatedAtUtc, featured.ImagePath })
            .ToListAsync(HttpContext.RequestAborted);
        string cdnBaseUrl = await _mediaCdn.GetBaseUrlAsync(HttpContext.RequestAborted);
        var reviews = reviewItems.Select(x => new HomeReviewCard(x.DisplayName, x.ProductName, x.FeedbackText,
            x.Rating, x.CreatedAtUtc, string.IsNullOrWhiteSpace(x.ImagePath) ? null : ContentCdn.ToUrl(x.ImagePath, cdnBaseUrl))).ToArray();
        ViewData["HomeSliders"] = await _db.StoreSliders.AsNoTracking().Where(x => x.ShowOnMobile || x.ShowOnDesktop).OrderBy(x => x.Id).ToListAsync(HttpContext.RequestAborted);
        ViewData["HomeReviews"] = reviews;
        ViewData["HomeReviewAverage"] = Math.Round(reviewAverage, 1);
        ViewData["HomeReviewCount"] = reviewCount;
        return await PageAsync(new PageModel<MagazineIndexViewModel>(magazine, model.Seo, IsHome: true));
    }
}

public sealed record HomeReviewCard(string DisplayName, string ProductName, string Text, int Rating, DateTime CreatedAtUtc, string? ImageUrl);
