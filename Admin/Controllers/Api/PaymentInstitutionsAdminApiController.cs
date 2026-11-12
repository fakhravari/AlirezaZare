using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;
using Shop.Data.Application.Store;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Admin.Controllers.Api;

[Area("Store")]
[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/payment-institutions")]
public sealed class PaymentInstitutionsAdminApiController(ShopDbContext db, IMediaCdnBaseUrlProvider mediaCdn) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var rows = await db.StoreBanks.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.Code, x.NameFa, x.LogoPath, x.IsActive }).ToListAsync(cancellationToken);
        string cdn = await mediaCdn.GetBaseUrlAsync(cancellationToken);
        var items = rows.Select(x => new PaymentInstitutionRow(x.Id, x.Code, x.NameFa, x.LogoPath,
            x.LogoPath is null ? null : ContentCdn.ToUrl(x.LogoPath, cdn), x.IsActive)).ToArray();
        return Ok(new { total = items.Length, page = 1, pageSize = items.Length, items });
    }

    public sealed record PaymentInstitutionRow(long Id, string Code, string NameFa, string? LogoPath, string? LogoUrl, bool IsActive);
}
