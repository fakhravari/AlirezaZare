using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Store;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/payment-institutions")]
public sealed class PaymentInstitutionsAdminApiController(IStorePublicPageData pageData, IMediaCdnBaseUrlProvider mediaCdn)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var rows = await pageData.ReadPaymentInstitutionAdminRowsAsync(cancellationToken);
        string cdn = await mediaCdn.GetBaseUrlAsync(cancellationToken);
        var items = rows.Select(x => new PaymentInstitutionRow(x.Id, x.Code, x.Name, x.LogoPath,
            x.LogoPath is null ? null : ContentCdn.ToUrl(x.LogoPath, cdn), x.IsActive)).ToArray();

        return Ok(new { total = items.Length, page = 1, pageSize = items.Length, items });
    }

    public sealed record PaymentInstitutionRow(
        long Id,
        string Code,
        string NameFa,
        string? LogoPath,
        string? LogoUrl,
        bool IsActive);
}
