using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[Area("Warehouse")]
[ApiController]
[Route("api/admin/warehouse/movements")]
public sealed class WarehouseMovementsApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] long? warehouseId, [FromQuery] string? search,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int page = 1,
        [FromQuery] bool audit = false, [FromQuery] bool summary = false, [FromQuery] string? salesPlan = null,
        [FromQuery] string? direction = null, CancellationToken cancellationToken = default)
    {
        search = search?.Trim();
        salesPlan = salesPlan?.Trim();
        if (page < 1 || page > 100000 || warehouseId is <= 0 || (from.HasValue && to.HasValue && from > to)
            || to == DateOnly.MaxValue || search?.Length > 100 || salesPlan?.Length > 100
            || direction is not (null or "" or "R" or "I") || (audit && summary))
            return ValidationProblem(detail: "فیلترها، بازهٔ تاریخ یا شمارهٔ صفحه معتبر نیست.", statusCode: 400);
        if (audit) return await GetAuditAsync(warehouseId, search, from, to, page, cancellationToken);
        if (summary) return await GetSummaryAsync(warehouseId, search, salesPlan, from, to, page, cancellationToken);
        var query = from line in db.WarehouseDocumentLines.AsNoTracking()
            join document in db.WarehouseDocuments.AsNoTracking() on line.IdDocument equals document.Id
            join type in db.WarehouseDocumentTypes.AsNoTracking() on document.IdDocumentType equals type.Id
            join warehouse in db.Warehouses.AsNoTracking() on document.IdWarehouse equals warehouse.Id
            join plan in db.SalesPlanReferences.AsNoTracking() on (line.IdSalesPlanReference ?? document.IdSalesPlanReference) equals plan.Id
            join movement in db.WarehouseStockMovements.AsNoTracking() on line.Id equals movement.IdDocumentLine into
                movements
            from movement in movements.DefaultIfEmpty()
            orderby document.DocumentDate descending, document.DocumentNumber descending, line.Id descending
            select new
            {
                document.Id, document.DocumentNumber, document.DocumentDate, document.Status,
                DocumentType = type.Name, type.Direction, type.AffectsStock,
                WarehouseId = warehouse.Id, Warehouse = warehouse.Name,
                SalesPlan = plan.DisplayName, SalesPlanKey = plan.StoreSalesPlanKey, line.ProductNameSnapshot, line.VariantNameSnapshot,
                line.Quantity, line.UnitNameSnapshot, line.ConversionFactor, line.BaseQuantity,
                QuantityDelta = movement == null ? 0m : movement.QuantityDelta,
                BalanceAfter = (from prior in db.WarehouseStockMovements
                    join priorDocument in db.WarehouseDocuments on prior.IdDocument equals priorDocument.Id
                    where prior.IdWarehouse == document.IdWarehouse && prior.IdInventoryItem == line.IdInventoryItem
                          && prior.IdSalesPlanReference == (line.IdSalesPlanReference ?? document.IdSalesPlanReference)
                          && (priorDocument.DocumentDate < document.DocumentDate ||
                              (priorDocument.DocumentDate == document.DocumentDate && priorDocument.DocumentNumber < document.DocumentNumber) ||
                              (priorDocument.DocumentDate == document.DocumentDate && priorDocument.DocumentNumber == document.DocumentNumber && prior.IdDocumentLine <= line.Id))
                    select (decimal?)prior.QuantityDelta).Sum() ?? 0m,
                document.Reason, document.CreatedByUserKey
            };
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out long parsed)
                ? parsed
                : 0;
            long[] allowed = await db.AdminUserWarehouseAccesses.AsNoTracking().Where(x => x.IdUser == userId)
                .Select(x => x.IdWarehouse).ToArrayAsync(cancellationToken);
            query = query.Where(x => allowed.Contains(x.WarehouseId));
        }

        if (warehouseId.HasValue)
        {
            if (!User.IsInRole("system.full-access"))
            {
                long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    out long parsed)
                    ? parsed
                    : 0;
                if (!await db.AdminUserWarehouseAccesses.AnyAsync(
                        x => x.IdUser == userId && x.IdWarehouse == warehouseId, cancellationToken)) return Forbid();
            }

            query = query.Where(x => x.WarehouseId == warehouseId);
        }

        if (from.HasValue) query = query.Where(x => x.DocumentDate >= from.Value);
        if (to.HasValue) query = query.Where(x => x.DocumentDate <= to.Value);
        if (!string.IsNullOrEmpty(salesPlan)) query = query.Where(x => x.SalesPlan.Contains(salesPlan) || x.SalesPlanKey.Contains(salesPlan));
        if (!string.IsNullOrEmpty(direction)) query = query.Where(x => x.Direction == direction);
        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            query = query.Where(x =>
                x.ProductNameSnapshot.Contains(term) ||
                (x.VariantNameSnapshot != null && x.VariantNameSnapshot.Contains(term)) ||
                x.DocumentType.Contains(term) || x.Reason!.Contains(term));
        }

        return Ok(await query.Skip((page - 1) * 100).Take(100).ToListAsync(cancellationToken));
    }

    private async Task<IActionResult> GetSummaryAsync(long? warehouseId, string? search, string? salesPlan,
        DateOnly? from, DateOnly? to, int page, CancellationToken cancellationToken)
    {
        var query = from movement in db.WarehouseStockMovements.AsNoTracking()
            join document in db.WarehouseDocuments.AsNoTracking() on movement.IdDocument equals document.Id
            join warehouse in db.Warehouses.AsNoTracking() on movement.IdWarehouse equals warehouse.Id
            join item in db.InventoryItems.AsNoTracking() on movement.IdInventoryItem equals item.Id
            join unit in db.WarehouseUnits.AsNoTracking() on item.Product.IdBaseUnit equals unit.Id
            join plan in db.SalesPlanReferences.AsNoTracking() on movement.IdSalesPlanReference equals plan.Id
            select new { movement, document, warehouse, item, unit, plan };
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out long parsed) ? parsed : 0;
            long[] allowed = await db.AdminUserWarehouseAccesses.AsNoTracking().Where(x => x.IdUser == userId)
                .Select(x => x.IdWarehouse).ToArrayAsync(cancellationToken);
            if (warehouseId.HasValue && !allowed.Contains(warehouseId.Value)) return Forbid();
            query = query.Where(x => allowed.Contains(x.movement.IdWarehouse));
        }
        if (warehouseId.HasValue) query = query.Where(x => x.movement.IdWarehouse == warehouseId.Value);
        if (!string.IsNullOrEmpty(search)) query = query.Where(x => x.item.Product.Name.Contains(search)
            || x.item.Product.ProductKey.Contains(search) || (x.item.ProductVariant != null && x.item.ProductVariant.Name.Contains(search)));
        if (!string.IsNullOrEmpty(salesPlan)) query = query.Where(x => x.plan.DisplayName.Contains(salesPlan) || x.plan.StoreSalesPlanKey.Contains(salesPlan));
        if (to.HasValue) query = query.Where(x => x.document.DocumentDate <= to.Value);

        DateOnly startDate = from ?? DateOnly.MinValue;
        var grouped = query.GroupBy(x => new
        {
            WarehouseId = x.warehouse.Id, Warehouse = x.warehouse.Name, ItemId = x.item.Id,
            Product = x.item.Product.Name, ProductKey = x.item.Product.ProductKey,
            Variant = x.item.ProductVariant == null ? null : x.item.ProductVariant.Name,
            PlanId = x.plan.Id, SalesPlan = x.plan.DisplayName, Unit = x.unit.Name
        }).Select(group => new
        {
            group.Key.WarehouseId, group.Key.Warehouse, group.Key.ItemId, group.Key.Product,
            group.Key.ProductKey, group.Key.Variant, group.Key.PlanId, group.Key.SalesPlan, group.Key.Unit,
            Opening = group.Sum(x => x.document.DocumentDate < startDate ? x.movement.QuantityDelta : 0m),
            Incoming = group.Sum(x => x.document.DocumentDate >= startDate && x.movement.QuantityDelta > 0 ? x.movement.QuantityDelta : 0m),
            Outgoing = group.Sum(x => x.document.DocumentDate >= startDate && x.movement.QuantityDelta < 0 ? -x.movement.QuantityDelta : 0m),
            Closing = group.Sum(x => x.movement.QuantityDelta)
        });
        int total = await grouped.CountAsync(cancellationToken);
        var items = await grouped.OrderBy(x => x.Warehouse).ThenBy(x => x.Product).ThenBy(x => x.Variant)
            .ThenBy(x => x.SalesPlan).ThenBy(x => x.ItemId).ThenBy(x => x.PlanId)
            .Skip((page - 1) * 100).Take(100).ToListAsync(cancellationToken);
        return Ok(new { items, total, page, pageSize = 100 });
    }

    private async Task<IActionResult> GetAuditAsync(long? warehouseId, string? search, DateOnly? from, DateOnly? to,
        int page, CancellationToken cancellationToken)
    {
        var query = from audit in db.WarehouseDocumentAudits.AsNoTracking()
            join warehouse in db.Warehouses.AsNoTracking() on audit.WarehouseId equals warehouse.Id
            select new { audit, Warehouse = warehouse.Name };
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out long parsed)
                ? parsed
                : 0;
            long[] allowed = await db.AdminUserWarehouseAccesses.AsNoTracking().Where(x => x.IdUser == userId)
                .Select(x => x.IdWarehouse).ToArrayAsync(cancellationToken);
            query = query.Where(x => allowed.Contains(x.audit.WarehouseId));
        }

        if (warehouseId.HasValue)
        {
            if (!User.IsInRole("system.full-access"))
            {
                long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    out long parsed)
                    ? parsed
                    : 0;
                if (!await db.AdminUserWarehouseAccesses.AnyAsync(
                        x => x.IdUser == userId && x.IdWarehouse == warehouseId, cancellationToken)) return Forbid();
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
            query = query.Where(x =>
                (x.audit.Reason != null && x.audit.Reason.Contains(term)) || x.audit.ActorUserKey.Contains(term));
        }

        int total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.audit.OccurredAtUtc).ThenByDescending(x => x.audit.Id)
            .Skip((page - 1) * 100).Take(100)
            .Select(x => new
            {
                x.audit.Id, x.audit.DocumentId, x.audit.DocumentNumber, x.audit.Action,
                x.audit.ActorUserKey, x.audit.OccurredAtUtc, x.audit.Reason, x.Warehouse,
                x.audit.BeforeJson, x.audit.AfterJson
            }).ToListAsync(cancellationToken);
        return Ok(new { items, total, page, pageSize = 100 });
    }
}
