using Microsoft.AspNetCore.Mvc;
using Shop.Models;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;
using Shop.Data.Application.Store;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Controllers;

public sealed class PaymentInstitutionsController(ShopDbContext db, IMediaCdnBaseUrlProvider mediaCdn) : PageController
{
    public async Task<IActionResult> Index()
    {
        var cdn = await mediaCdn.GetBaseUrlAsync(HttpContext.RequestAborted);
        var rows = await db.StoreBanks.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Id)
            .Select(x => new { x.NameFa, x.LogoPath }).ToListAsync(HttpContext.RequestAborted);
        var institutions = rows.Select(x => new PaymentInstitutionRow(x.NameFa,
            x.LogoPath is null ? null : ContentCdn.ToUrl(x.LogoPath, cdn))).ToArray();
        ViewData["PaymentInstitutions"] = institutions;
        var model = new PageModel(new SeoModel("بانک‌ها و درگاه‌های پرداخت", "فهرست بانک‌ها و خدمات پرداخت ثبت‌شده در فروشگاه."));
        return await PageAsync(model);
    }

    public sealed record PaymentInstitutionRow(string NameFa, string? LogoUrl);
}
