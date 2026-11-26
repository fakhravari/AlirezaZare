using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Admin.Infrastructure;
using Shop.Data.Application.Warehouse.Contracts;
using Shop.Data.Helpers;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Authorize]
[Route("api/admin/store/paid-orders-dispatch")]
public sealed class StorePaidOrdersDispatchApiController(
    IStorePaidOrderDispatchData data)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool warehouseQueue = false,
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!User.IsInRole("system.full-access"))
        {
            if (!warehouseQueue) return Forbid();

            var warehouseId = await data.ReadSalesWarehouseIdAsync(cancellationToken);
            if (warehouseId is null || !User.HasClaim("admin_warehouse",
                    warehouseId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)))
                return Forbid();
        }

        if (!AdminListQuery.TryCreate(page, pageSize, q, out AdminPageRequest paging))
            return BadRequest(new { title = "عبارت جست‌وجو یا صفحه‌بندی معتبر نیست." });

        var result = await data.ReadPaidOrdersAsync(paging.Search, paging.Page, paging.PageSize, cancellationToken);
        return Ok(AdminListQuery.CreateResult(result.Items, result.Total, paging));
    }

    [HttpPost]
    [Authorize(Roles = "system.full-access")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] DispatchInput input, CancellationToken cancellationToken)
    {
        if (input.OrderIds is null || input.OrderIds.Length is < 1 or > 200 || input.OrderIds.Any(id => id <= 0) ||
            input.OrderIds.Distinct().Count() != input.OrderIds.Length)
            return BadRequest(new { detail = "سفارش‌های انتخاب‌شده معتبر نیستند." });
        string actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "system";
        if (actor.Length > 128) return BadRequest(new { detail = "شناسهٔ مدیر نامعتبر است." });
        var result = await data.CreateAsync(input.OrderIds, actor, cancellationToken);
        if (result.Success) return Ok(new { result.VoucherId, result.VoucherNumber });
        string detail = result.Error switch
        {
            "orders-changed" => "وضعیت یکی از سفارش‌ها یا وابستگی‌های انبار تغییر کرده است؛ فهرست را دوباره بارگذاری کنید.",
            "dispatch-config" => "انبار فروش، نوع حواله یا مرجع آن تنظیم نشده است.",
            "warehouse-plan" => "طرح یکی از اقلام به انبار فروش متصل نیست.",
            "inventory-mapping" => "برای یکی از اقلام سفارش، کالا یا واحد انبار متناظر یکتا پیدا نشد.",
            "insufficient-stock" => "موجودی برای صدور حواله کافی نیست.",
            _ => "صدور حواله انجام نشد."
        };
        return Conflict(new { detail });
    }
    public sealed record DispatchInput(decimal[]? OrderIds);
}
