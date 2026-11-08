using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Domain.Warehouse.Entities;
using Shop.Database.Application.Warehouse.Contracts;
using Shop.Database.Application.Warehouse.Models;
using Shop.Database.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[Area("Warehouse")]
[ApiController]
[Route("api/admin/warehouse")]
public sealed class WarehouseAdminApiController(IWarehouseReadService warehouseReadService, ShopDbContext db) : ControllerBase
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
        if (input.WarehouseId < 1 || input.InventoryItemId < 1 || input.MinimumQuantity < 0 || input.MinimumQuantity > 999999999999999.999m)
            return ValidationProblem("انبار، کالا و حداقل موجودی نامعتبر است.");
        if (!User.IsInRole("system.full-access") && !GetAllowedWarehouseIds().Contains(input.WarehouseId)) return Forbid();
        decimal minimum = decimal.Truncate(input.MinimumQuantity * 1000m) / 1000m;
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        bool valid = await db.Warehouses.AnyAsync(x => x.Id == input.WarehouseId && x.IsActive, cancellationToken)
            && await db.InventoryItems.AnyAsync(x => x.Id == input.InventoryItemId && x.IsActive && x.Product.IsActive, cancellationToken);
        var regularPlan = await db.SalesPlanReferences.AsNoTracking().Where(x => x.IsRegular && x.IsActive)
            .Select(x => new { x.Id }).SingleOrDefaultAsync(cancellationToken);
        if (!valid || regularPlan is null) return ValidationProblem("انبار یا قلم موجودی فعال نیست یا طرح عادی تعریف نشده است.");
        var balance = await db.StockBalances.SingleOrDefaultAsync(x => x.IdWarehouse == input.WarehouseId
            && x.IdInventoryItem == input.InventoryItemId && x.IdSalesPlanReference == regularPlan.Id, cancellationToken);
        if (balance is null)
        {
            balance = new StockBalance { IdWarehouse = input.WarehouseId, IdInventoryItem = input.InventoryItemId,
                IdSalesPlanReference = regularPlan.Id, Quantity = 0, MinimumQuantity = minimum };
            db.StockBalances.Add(balance);
        }
        else balance.MinimumQuantity = minimum;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { minimumQuantity = minimum, message = "حداقل موجودی طرح عادی ذخیره شد." });
    }

    [HttpGet("stock")]
    [ProducesResponseType<IReadOnlyList<WarehouseStockItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WarehouseStockItem>>> GetStock(
        [FromQuery] long? warehouseId,
        [FromQuery] string? salesPlanKey,
        [FromQuery] string? search,
        [FromQuery] bool lowStockOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 200)
            return ValidationProblem("شماره صفحه باید مثبت و اندازهٔ صفحه بین ۱ تا ۲۰۰ باشد.");

        if (User.IsInRole("system.full-access"))
            return Ok(await warehouseReadService.GetStockAsync(
                warehouseId, salesPlanKey, search, lowStockOnly, page, pageSize, cancellationToken));

        HashSet<long> allowed = GetAllowedWarehouseIds();
        if (warehouseId.HasValue && !allowed.Contains(warehouseId.Value))
            return Forbid();
        if (warehouseId.HasValue)
            return Ok(await warehouseReadService.GetStockAsync(
                warehouseId, salesPlanKey, search, lowStockOnly, page, pageSize, cancellationToken));
        return Ok(await warehouseReadService.GetStockForWarehousesAsync(
            allowed.ToArray(), salesPlanKey, search, lowStockOnly, page, pageSize, cancellationToken));
    }

    private HashSet<long> GetAllowedWarehouseIds() => User.FindAll("admin_warehouse")
        .Select(claim => long.TryParse(claim.Value, out long id) ? id : 0)
        .Where(id => id > 0)
        .ToHashSet();

    public sealed record MinimumStockInput(long WarehouseId, long InventoryItemId, decimal MinimumQuantity);
}
