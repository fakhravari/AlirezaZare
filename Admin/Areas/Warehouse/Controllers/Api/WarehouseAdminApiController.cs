using Microsoft.AspNetCore.Mvc;
using Shop.Admin.Infrastructure;
using Shop.Data.Application.Warehouse.Contracts;
using Shop.Data.Application.Warehouse.Models;
using Shop.Data.Helpers;

namespace Shop.Admin.Areas.Warehouse.Controllers.Api;

[Area("Warehouse")]
[ApiController]
[Route("api/admin/warehouse")]
public sealed class WarehouseAdminApiController(IWarehouseReadService warehouseReadService,
    IWarehouseInventoryAdminService inventoryAdmin)
    : ControllerBase
{
    [HttpGet("warehouses")]
    [ProducesResponseType<IReadOnlyList<WarehouseInfo>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WarehouseInfo>>> GetWarehouses(CancellationToken cancellationToken)
    {
        IReadOnlyList<WarehouseInfo> warehouses = await warehouseReadService.GetWarehousesAsync(cancellationToken);
        if (User.IsInRole("system.full-access"))
            return Ok(warehouses);

        HashSet<long> allowed = GetAllowedWarehouseIds();

        return Ok(warehouses.Where(x => allowed.Contains(x.Id)).ToArray());
    }

    [HttpPut("stock/minimum")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetMinimum([FromBody] MinimumStockInput input, CancellationToken cancellationToken)
    {
        if (input.WarehouseId < 1 || input.InventoryItemId < 1 || input.MinimumQuantity < 0 ||
            input.MinimumQuantity > 999999999999999.999m)
            return ValidationProblem(detail: "انبار، کالا و حداقل موجودی نامعتبر است.", statusCode: 400);

        if (!User.IsInRole("system.full-access") && !GetAllowedWarehouseIds().Contains(input.WarehouseId))
            return Forbid();

        decimal minimum = decimal.Truncate(input.MinimumQuantity * 1000m) / 1000m;
        bool saved = await inventoryAdmin.SetMinimumStockAsync(input.WarehouseId, input.InventoryItemId, minimum,
            cancellationToken);
        if (!saved)
            return ValidationProblem(detail: "انبار یا قلم موجودی فعال نیست یا طرح عادی تعریف نشده است.",
                statusCode: 400);

        return Ok(new { minimumQuantity = minimum, message = "حداقل موجودی طرح عادی ذخیره شد." });
    }

    [HttpGet("stock")]
    [HttpGet("stock/plans")]
    public async Task<IActionResult> GetStock(
        [FromQuery] long? warehouseId,
        [FromQuery] string? salesPlanKey,
        [FromQuery] string? search,
        [FromQuery] bool lowStockOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (Request.Path.Value?.EndsWith("/stock/plans", StringComparison.OrdinalIgnoreCase) == true)
            return Ok((await inventoryAdmin.ReadActiveSalesPlansAsync(cancellationToken))
                .Select(plan => new { id = plan.Id, name = plan.Name }));

        if (!AdminListQuery.TryCreate(page, pageSize, search, out AdminPageRequest paging))
            return ValidationProblem(detail: "عبارت جست‌وجو یا صفحه‌بندی معتبر نیست.", statusCode: 400);

        search = paging.Search;

        bool fullAccess = User.IsInRole("system.full-access");
        HashSet<long> allowed = GetAllowedWarehouseIds();
        if (!fullAccess && warehouseId.HasValue && !allowed.Contains(warehouseId.Value))
            return Forbid();

        async Task<IReadOnlyList<WarehouseStockItem>> ReadPage(int requestedPage, int requestedSize)
        {
            if (fullAccess || warehouseId.HasValue)
                return await warehouseReadService.GetStockAsync(warehouseId, salesPlanKey, search, lowStockOnly,
                    requestedPage, requestedSize, cancellationToken);

            return await warehouseReadService.GetStockForWarehousesAsync(allowed.ToArray(), salesPlanKey, search,
                lowStockOnly, requestedPage, requestedSize, cancellationToken);
        }

        IReadOnlyList<WarehouseStockItem> items = await ReadPage(paging.Page, paging.PageSize);
        bool hasNext = items.Count == paging.PageSize && (await ReadPage(paging.Page + 1, 1)).Count > 0;

        return Ok(AdminListQuery.CreateLookaheadResult(items, hasNext, paging));
    }

    private HashSet<long> GetAllowedWarehouseIds() => User.FindAll("admin_warehouse")
        .Select(claim => long.TryParse(claim.Value, out long id) ? id : 0).Where(id => id > 0).ToHashSet();

    public sealed record MinimumStockInput(long WarehouseId, long InventoryItemId, decimal MinimumQuantity);
}
