using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[Area("Warehouse")]
[ApiController]
[Route("api/admin/warehouse/movements")]
public sealed class WarehouseMovementsApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] long? warehouseId, [FromQuery] string? search,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int page = 1,
        [FromQuery] bool audit = false, CancellationToken cancellationToken = default)
    {
        if (page < 1 || page > 100000 || (from.HasValue && to.HasValue && from > to))
            return ValidationProblem("بازهٔ تاریخ یا شمارهٔ صفحه معتبر نیست.");
        if (audit) return await GetAuditAsync(warehouseId, search, from, to, page, cancellationToken);
        var query = from line in db.WarehouseDocumentLines.AsNoTracking()
                    join document in db.WarehouseDocuments.AsNoTracking() on line.IdDocument equals document.Id
                    join type in db.WarehouseDocumentTypes.AsNoTracking() on document.IdDocumentType equals type.Id
                    join warehouse in db.Warehouses.AsNoTracking() on document.IdWarehouse equals warehouse.Id
                    join plan in db.SalesPlanReferences.AsNoTracking() on document.IdSalesPlanReference equals plan.Id
                    join movement in db.WarehouseStockMovements.AsNoTracking() on line.Id equals movement.IdDocumentLine into movements
                    from movement in movements.DefaultIfEmpty()
                    orderby document.DocumentDate descending, document.DocumentNumber descending, line.Id descending
                    select new
                    {
                        document.Id, document.DocumentNumber, document.DocumentDate, document.Status,
                        DocumentType = type.Name, type.Direction, type.AffectsStock,
                        WarehouseId = warehouse.Id, Warehouse = warehouse.Name,
                        SalesPlan = plan.DisplayName, line.ProductNameSnapshot, line.VariantNameSnapshot,
                        line.Quantity, line.UnitNameSnapshot, line.ConversionFactor, line.BaseQuantity,
                        QuantityDelta = movement == null ? 0m : movement.QuantityDelta,
                        document.Reason, document.CreatedByUserKey
                    };
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out long parsed) ? parsed : 0;
            long[] allowed = await db.AdminUserWarehouseAccesses.AsNoTracking().Where(x => x.IdUser == userId).Select(x => x.IdWarehouse).ToArrayAsync(cancellationToken);
            query = query.Where(x => allowed.Contains(x.WarehouseId));
        }
        if (warehouseId.HasValue)
        {
            if (!User.IsInRole("system.full-access"))
            {
                long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out long parsed) ? parsed : 0;
                if (!await db.AdminUserWarehouseAccesses.AnyAsync(x => x.IdUser == userId && x.IdWarehouse == warehouseId, cancellationToken)) return Forbid();
            }
            query = query.Where(x => x.WarehouseId == warehouseId);
        }
        if (from.HasValue) query = query.Where(x => x.DocumentDate >= from.Value);
        if (to.HasValue) query = query.Where(x => x.DocumentDate <= to.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            query = query.Where(x => x.ProductNameSnapshot.Contains(term) || (x.VariantNameSnapshot != null && x.VariantNameSnapshot.Contains(term)) || x.DocumentType.Contains(term) || x.Reason!.Contains(term));
        }
        return Ok(await query.Skip((page - 1) * 100).Take(100).ToListAsync(cancellationToken));
    }

    private async Task<IActionResult> GetAuditAsync(long? warehouseId, string? search, DateOnly? from, DateOnly? to,
        int page, CancellationToken cancellationToken)
    {
        var query = from audit in db.WarehouseDocumentAudits.AsNoTracking()
                    join warehouse in db.Warehouses.AsNoTracking() on audit.WarehouseId equals warehouse.Id
                    select new { audit, Warehouse = warehouse.Name };
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out long parsed) ? parsed : 0;
            long[] allowed = await db.AdminUserWarehouseAccesses.AsNoTracking().Where(x => x.IdUser == userId).Select(x => x.IdWarehouse).ToArrayAsync(cancellationToken);
            query = query.Where(x => allowed.Contains(x.audit.WarehouseId));
        }
        if (warehouseId.HasValue)
        {
            if (!User.IsInRole("system.full-access"))
            {
                long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out long parsed) ? parsed : 0;
                if (!await db.AdminUserWarehouseAccesses.AnyAsync(x => x.IdUser == userId && x.IdWarehouse == warehouseId, cancellationToken)) return Forbid();
            }
            query = query.Where(x => x.audit.WarehouseId == warehouseId.Value);
        }
        if (from.HasValue)
        {
            DateTime start = from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.audit.OccurredAtUtc >= start);
        }
        if (to.HasValue)
        {
            DateTime endExclusive = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.audit.OccurredAtUtc < endExclusive);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            query = query.Where(x => (x.audit.Reason != null && x.audit.Reason.Contains(term)) || x.audit.ActorUserKey.Contains(term));
        }
        int total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.audit.OccurredAtUtc).ThenByDescending(x => x.audit.Id)
            .Skip((page - 1) * 100).Take(100)
            .Select(x => new { x.audit.Id, x.audit.DocumentId, x.audit.DocumentNumber, x.audit.Action,
                x.audit.ActorUserKey, x.audit.OccurredAtUtc, x.audit.Reason, x.Warehouse,
                x.audit.BeforeJson, x.audit.AfterJson }).ToListAsync(cancellationToken);
        return Ok(new { items, total, page, pageSize = 100 });
    }
}
