using Microsoft.AspNetCore.Mvc;
using Shop.Models;
using Shop.Data.Application.Store;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Controllers;

public sealed class PaymentInstitutionsController(IStorePublicPageData pageData, IMediaCdnBaseUrlProvider mediaCdn) : PageController
{
    public async Task<IActionResult> Index()
    {
        var cdn = await mediaCdn.GetBaseUrlAsync(HttpContext.RequestAborted);
        var rows = await pageData.ReadPaymentInstitutionsAsync(HttpContext.RequestAborted);
        var institutions = rows.Select(x =>
                new PaymentInstitutionRow(x.Name, x.LogoPath is null ? null : ContentCdn.ToUrl(x.LogoPath, cdn)))
            .ToArray();
        ViewData["PaymentInstitutions"] = institutions;
        var model = new PageModel(new SeoModel("بانک‌ها و درگاه‌های پرداخت",
            "فهرست بانک‌ها و خدمات پرداخت ثبت‌شده در فروشگاه."));

        return await PageAsync(model);
    }

    public sealed record PaymentInstitutionRow(string NameFa, string? LogoUrl);
}
