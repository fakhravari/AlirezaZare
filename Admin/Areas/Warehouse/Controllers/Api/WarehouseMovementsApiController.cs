using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Admin.Infrastructure;
using Shop.Data.Infrastructure.Persistence;
using Shop.Data.Helpers;

namespace Shop.Admin.Areas.Warehouse.Controllers.Api;

[Area("Warehouse")]
[ApiController]
[Route("api/admin/warehouse/movements")]
public sealed class WarehouseMovementsApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet]
    [HttpGet("products")]
    public async Task<IActionResult> Get([FromQuery] long? warehouseId, [FromQuery] string? search,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        [FromQuery] bool audit = false, [FromQuery] bool summary = false, [FromQuery] string? salesPlan = null,
        [FromQuery] string? direction = null, [FromQuery] long? productId = null,
        CancellationToken cancellationToken = default)
    {
        if (Request.Path.Value?.EndsWith("/products", StringComparison.OrdinalIgnoreCase) == true)
            return await GetProductsAsync(search, page, cancellationToken);

        if (!AdminListQuery.TryCreate(page, pageSize, search, out AdminPageRequest paging))
            return ValidationProblem(detail: "عبارت جست‌وجو یا صفحه‌بندی معتبر نیست.", statusCode: 400);

        search = paging.Search;
        salesPlan = salesPlan?.Trim();

        if (warehouseId is <= 0 || productId is <= 0 || (audit && productId.HasValue) ||
            (from.HasValue && to.HasValue && from > to)
            || to == DateOnly.MaxValue || salesPlan?.Length > AdminListQuery.MaximumSearchLength
            || direction is not (null or "" or "R" or "I") || (audit && summary))
            return ValidationProblem(detail: "فیلترها، بازهٔ تاریخ یا شمارهٔ صفحه معتبر نیست.", statusCode: 400);

        if (audit) return await GetAuditAsync(warehouseId, search, from, to, paging, cancellationToken);

        if (summary)
            return await GetSummaryAsync(warehouseId, productId, search, salesPlan, from, to, paging,
                cancellationToken);

        var query = db.VoucherLines
            .AsNoTracking()
            .Join(
                db.Vouchers.AsNoTracking(),
                line => line.IdVoucher,
                document => document.Id,
                (line, document) => new { line, document })
            .Join(
                db.InventoryItems.AsNoTracking(),
                lineDocument => lineDocument.line.IdInventoryItem,
                inventoryItem => inventoryItem.Id,
                (lineDocument, inventoryItem) => new { lineDocument.line, lineDocument.document, inventoryItem })
            .Join(
                db.VoucherTypes.AsNoTracking(),
                lineDetails => lineDetails.document.IdVoucherType,
                type => type.Id,
                (lineDetails, type) => new { lineDetails.line, lineDetails.document, lineDetails.inventoryItem, type })
            .Join(
                db.Warehouses.AsNoTracking(),
                lineDetails => lineDetails.document.IdWarehouse,
                warehouse => warehouse.Id,
                (lineDetails, warehouse) => new
                {
                    lineDetails.line,
                    lineDetails.document,
                    lineDetails.inventoryItem,
                    lineDetails.type,
                    warehouse
                })
            .Join(
                db.SalesPlanReferences.AsNoTracking(),
                lineDetails => lineDetails.line.IdSalesPlanReference ?? lineDetails.document.IdSalesPlanReference,
                plan => plan.Id,
                (lineDetails, plan) => new
                {
                    lineDetails.line,
                    lineDetails.document,
                    lineDetails.inventoryItem,
                    lineDetails.type,
                    lineDetails.warehouse,
                    plan
                })
            .GroupJoin(
                db.WarehouseStockMovements.AsNoTracking(),
                lineDetails => lineDetails.line.Id,
                movement => movement.IdVoucherLine,
                (lineDetails, movements) => new { lineDetails, movements })
            .SelectMany(
                lineDetailsMovements => lineDetailsMovements.movements.DefaultIfEmpty(),
                (lineDetailsMovements, movement) => new
            {
                lineDetailsMovements.lineDetails.line.Id,
                lineDetailsMovements.lineDetails.document.VoucherNumber,
                lineDetailsMovements.lineDetails.document.VoucherDate,
                Status = lineDetailsMovements.lineDetails.document.StatusReference.Code,
                StatusName = lineDetailsMovements.lineDetails.document.StatusReference.NameFa,
                ProductId = lineDetailsMovements.lineDetails.inventoryItem.IdProduct,
                VoucherType = lineDetailsMovements.lineDetails.type.Name,
                lineDetailsMovements.lineDetails.type.Direction,
                lineDetailsMovements.lineDetails.type.AffectsStock,
                WarehouseId = lineDetailsMovements.lineDetails.warehouse.Id,
                Warehouse = lineDetailsMovements.lineDetails.warehouse.Company.Name + " / " +
                            lineDetailsMovements.lineDetails.warehouse.Name + " (" +
                            lineDetailsMovements.lineDetails.warehouse.Code + ")",
                SalesPlan = lineDetailsMovements.lineDetails.plan.DisplayName,
                SalesPlanKey = lineDetailsMovements.lineDetails.plan.StoreSalesPlanKey,
                lineDetailsMovements.lineDetails.line.ProductNameSnapshot,
                lineDetailsMovements.lineDetails.line.VariantNameSnapshot,
                lineDetailsMovements.lineDetails.line.Quantity,
                lineDetailsMovements.lineDetails.line.UnitNameSnapshot,
                lineDetailsMovements.lineDetails.line.ConversionFactor,
                lineDetailsMovements.lineDetails.line.BaseQuantity,
                QuantityDelta = movement == null ? 0m : movement.QuantityDelta,
                BalanceAfter = db.WarehouseStockMovements
                    .AsNoTracking()
                    .Join(
                        db.Vouchers.AsNoTracking(),
                        prior => prior.IdVoucher,
                        priorDocument => priorDocument.Id,
                        (prior, priorDocument) => new { prior, priorDocument })
                    .Where(priorDetails =>
                        priorDetails.prior.IdWarehouse == lineDetailsMovements.lineDetails.document.IdWarehouse &&
                        priorDetails.prior.IdInventoryItem == lineDetailsMovements.lineDetails.line.IdInventoryItem &&
                        priorDetails.prior.IdSalesPlanReference ==
                        (lineDetailsMovements.lineDetails.line.IdSalesPlanReference ??
                         lineDetailsMovements.lineDetails.document.IdSalesPlanReference) &&
                        (priorDetails.priorDocument.VoucherDate < lineDetailsMovements.lineDetails.document.VoucherDate ||
                         (priorDetails.priorDocument.VoucherDate ==
                          lineDetailsMovements.lineDetails.document.VoucherDate &&
                          priorDetails.priorDocument.VoucherNumber <
                          lineDetailsMovements.lineDetails.document.VoucherNumber) ||
                         (priorDetails.priorDocument.VoucherDate ==
                          lineDetailsMovements.lineDetails.document.VoucherDate &&
                          priorDetails.priorDocument.VoucherNumber ==
                          lineDetailsMovements.lineDetails.document.VoucherNumber &&
                          priorDetails.prior.IdVoucherLine <= lineDetailsMovements.lineDetails.line.Id)))
                    .Select(priorDetails => (decimal?)priorDetails.prior.QuantityDelta)
                    .Sum() ?? 0m,
                lineDetailsMovements.lineDetails.document.Reason,
                lineDetailsMovements.lineDetails.document.CreatedByUserKey
            });
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out long parsed)
                ? parsed
                : 0;
            long[] allowed = (await db.AdminUserWarehouseAccesses
                .AsNoTracking()
                .Where(access => access.IdUser == userId)
                .Select(access => access.IdWarehouse)
                .ToListAsync(cancellationToken))
                .ToArray();
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

        if (from.HasValue) query = query.Where(x => x.VoucherDate >= from.Value);
        if (to.HasValue) query = query.Where(x => x.VoucherDate <= to.Value);
        if (productId.HasValue) query = query.Where(x => x.ProductId == productId.Value);
        if (!string.IsNullOrEmpty(salesPlan))
            query = query.Where(x => x.SalesPlan.Contains(salesPlan) || x.SalesPlanKey.Contains(salesPlan));
        if (!string.IsNullOrEmpty(direction)) query = query.Where(x => x.Direction == direction);
        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            query = query.Where(x =>
                x.ProductNameSnapshot.Contains(term) ||
                (x.VariantNameSnapshot != null && x.VariantNameSnapshot.Contains(term)) ||
                x.VoucherType.Contains(term) || x.Reason!.Contains(term));
        }

        query = query
            .OrderByDescending(row => row.VoucherDate)
            .ThenByDescending(row => row.VoucherNumber)
            .ThenByDescending(row => row.Id);
        int total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(paging.Offset).Take(paging.PageSize).ToListAsync(cancellationToken);

        return Ok(AdminListQuery.CreateResult(items, total, paging));
    }

    private async Task<IActionResult> GetSummaryAsync(long? warehouseId, long? productId, string? search,
        string? salesPlan,
        DateOnly? from, DateOnly? to, AdminPageRequest paging, CancellationToken cancellationToken)
    {
        var query = db.WarehouseStockMovements
            .AsNoTracking()
            .Join(
                db.Vouchers.AsNoTracking(),
                movement => movement.IdVoucher,
                document => document.Id,
                (movement, document) => new { movement, document })
            .Join(
                db.Warehouses.AsNoTracking(),
                movementDocument => movementDocument.movement.IdWarehouse,
                warehouse => warehouse.Id,
                (movementDocument, warehouse) => new { movementDocument.movement, movementDocument.document, warehouse })
            .Join(
                db.InventoryItems.AsNoTracking(),
                movementDetails => movementDetails.movement.IdInventoryItem,
                item => item.Id,
                (movementDetails, item) => new { movementDetails.movement, movementDetails.document,
                    movementDetails.warehouse, item })
            .Join(
                db.WarehouseProductUnits.AsNoTracking().Where(unitLink => unitLink.IsActive && unitLink.IsBaseUnit),
                movementDetails => movementDetails.item.IdProduct,
                baseUnit => baseUnit.IdProduct,
                (movementDetails, baseUnit) => new { movementDetails.movement, movementDetails.document,
                    movementDetails.warehouse, movementDetails.item, baseUnit })
            .Join(
                db.WarehouseUnits.AsNoTracking(),
                movementDetails => movementDetails.baseUnit.IdUnit,
                unit => unit.Id,
                (movementDetails, unit) => new { movementDetails.movement, movementDetails.document,
                    movementDetails.warehouse, movementDetails.item, unit })
            .Join(
                db.SalesPlanReferences.AsNoTracking(),
                movementDetails => movementDetails.movement.IdSalesPlanReference,
                plan => plan.Id,
                (movementDetails, plan) => new { movementDetails.movement, movementDetails.document,
                    movementDetails.warehouse, movementDetails.item, movementDetails.unit, plan });
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out long parsed)
                ? parsed
                : 0;
            long[] allowed = (await db.AdminUserWarehouseAccesses
                .AsNoTracking()
                .Where(access => access.IdUser == userId)
                .Select(access => access.IdWarehouse)
                .ToListAsync(cancellationToken))
                .ToArray();
            if (warehouseId.HasValue && !allowed.Contains(warehouseId.Value)) return Forbid();

            query = query.Where(x => allowed.Contains(x.movement.IdWarehouse));
        }

        if (warehouseId.HasValue) query = query.Where(x => x.movement.IdWarehouse == warehouseId.Value);
        if (productId.HasValue) query = query.Where(x => x.item.IdProduct == productId.Value);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(x => x.item.Product.Name.Contains(search)
                                     || x.item.Product.ProductKey.Contains(search) || (x.item.ProductVariant != null &&
                                         x.item.ProductVariant.VariantName.Name.Contains(search)));
        if (!string.IsNullOrEmpty(salesPlan))
            query = query.Where(x =>
                x.plan.DisplayName.Contains(salesPlan) || x.plan.StoreSalesPlanKey.Contains(salesPlan));
        if (to.HasValue) query = query.Where(x => x.document.VoucherDate <= to.Value);

        DateOnly startDate = from ?? DateOnly.MinValue;
        var grouped = query.GroupBy(x => new
        {
            WarehouseId = x.warehouse.Id,
            Warehouse = x.warehouse.Company.Name + " / " + x.warehouse.Name + " (" + x.warehouse.Code + ")",
            ItemId = x.item.Id,
            Product = x.item.Product.Name,
            ProductKey = x.item.Product.ProductKey,
            Variant = x.item.ProductVariant == null ? null : x.item.ProductVariant.VariantName.Name,
            PlanId = x.plan.Id,
            SalesPlan = x.plan.DisplayName,
            Unit = x.unit.NameFa
        }).Select(group => new
        {
            group.Key.WarehouseId,
            group.Key.Warehouse,
            group.Key.ItemId,
            group.Key.Product,
            group.Key.ProductKey,
            group.Key.Variant,
            group.Key.PlanId,
            group.Key.SalesPlan,
            group.Key.Unit,
            Opening = group.Sum(x => x.document.VoucherDate < startDate ? x.movement.QuantityDelta : 0m),
            Incoming = group.Sum(x =>
                x.document.VoucherDate >= startDate && x.movement.QuantityDelta > 0 ? x.movement.QuantityDelta : 0m),
            Outgoing = group.Sum(x =>
                x.document.VoucherDate >= startDate && x.movement.QuantityDelta < 0 ? -x.movement.QuantityDelta : 0m),
            Closing = group.Sum(x => x.movement.QuantityDelta)
        });
        int total = await grouped.CountAsync(cancellationToken);
        var items = await grouped.OrderBy(x => x.Warehouse).ThenBy(x => x.Product).ThenBy(x => x.Variant)
            .ThenBy(x => x.SalesPlan).ThenBy(x => x.ItemId).ThenBy(x => x.PlanId).Skip(paging.Offset)
            .Take(paging.PageSize).ToListAsync(cancellationToken);

        return Ok(AdminListQuery.CreateResult(items, total, paging));
    }

    private async Task<IActionResult> GetProductsAsync(string? search, int page, CancellationToken cancellationToken)
    {
        const int size = 20;
        if (!AdminListQuery.TryCreate(page, size, search, out AdminPageRequest paging))
            return ValidationProblem(detail: "عبارت جست‌وجو یا شمارهٔ صفحه معتبر نیست.", statusCode: 400);

        var query = db.VoucherLines
            .AsNoTracking()
            .Join(
                db.Vouchers.AsNoTracking(),
                line => line.IdVoucher,
                document => document.Id,
                (line, document) => new { line, document })
            .Join(
                db.InventoryItems.AsNoTracking(),
                lineDocument => lineDocument.line.IdInventoryItem,
                item => item.Id,
                (lineDocument, item) => new
                {
                    lineDocument.document.IdWarehouse,
                    item.Product.Id,
                    item.Product.Name,
                    Code = item.Product.ProductKey
                });
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out long parsed)
                ? parsed
                : 0;
            long[] allowed = (await db.AdminUserWarehouseAccesses
                .AsNoTracking()
                .Where(access => access.IdUser == userId)
                .Select(access => access.IdWarehouse)
                .ToListAsync(cancellationToken))
                .ToArray();
            query = query.Where(x => allowed.Contains(x.IdWarehouse));
        }

        if (!string.IsNullOrEmpty(paging.Search))
            query = query.Where(x => x.Name.Contains(paging.Search) || x.Code.Contains(paging.Search));
        var products = await query.Select(x => new { x.Id, x.Name, x.Code }).Distinct()
            .OrderBy(x => x.Name).ThenBy(x => x.Id).Skip(paging.Offset).Take(size + 1).ToListAsync(cancellationToken);
        return Ok(new { items = products.Take(size), more = products.Count > size });
    }

    private async Task<IActionResult> GetAuditAsync(long? warehouseId, string? search, DateOnly? from, DateOnly? to,
        AdminPageRequest paging, CancellationToken cancellationToken)
    {
        var query = db.VoucherAudits
            .AsNoTracking()
            .Join(
                db.Warehouses.AsNoTracking(),
                audit => audit.WarehouseId,
                warehouse => warehouse.Id,
                (audit, warehouse) => new
            {
                audit,
                Warehouse = warehouse.Company.Name + " / " + warehouse.Name + " (" + warehouse.Code + ")"
            });
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out long parsed)
                ? parsed
                : 0;
            long[] allowed = (await db.AdminUserWarehouseAccesses
                .AsNoTracking()
                .Where(access => access.IdUser == userId)
                .Select(access => access.IdWarehouse)
                .ToListAsync(cancellationToken))
                .ToArray();
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
            .Skip(paging.Offset).Take(paging.PageSize).Select(x => new
            {
                x.audit.Id,
                x.audit.VoucherId,
                x.audit.VoucherNumber,
                x.audit.Action,
                x.audit.ActorUserKey,
                x.audit.OccurredAtUtc,
                x.audit.Reason,
                x.Warehouse,
                x.audit.BeforeJson,
                x.audit.AfterJson
            }).ToListAsync(cancellationToken);

        return Ok(AdminListQuery.CreateResult(items, total, paging));
    }
}
