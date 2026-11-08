using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Domain.Warehouse.Entities;
using Shop.Database.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[Area("Warehouse")]
[ApiController]
[Route("api/admin/warehouse/documents")]
public sealed class WarehouseDocumentsApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] long? warehouseId, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 200 || status is not (null or "D" or "F"))
            return ValidationProblem("فیلتر صفحه یا وضعیت سند معتبر نیست.");
        long[] allowed = await AllowedWarehouseIdsAsync(cancellationToken);
        if (!User.IsInRole("system.full-access") && warehouseId.HasValue && !allowed.Contains(warehouseId.Value))
            return Forbid();

        var query = from document in db.WarehouseDocuments.AsNoTracking()
                    join warehouse in db.Warehouses.AsNoTracking() on document.IdWarehouse equals warehouse.Id
                    join type in db.WarehouseDocumentTypes.AsNoTracking() on document.IdDocumentType equals type.Id
                    join party in db.WarehouseParties.AsNoTracking() on document.IdParty equals party.Id
                    join plan in db.SalesPlanReferences.AsNoTracking() on document.IdSalesPlanReference equals plan.Id
                    where warehouse.IsActive
                    select new { document, warehouse, type, party, plan };

        if (warehouseId.HasValue) query = query.Where(x => x.document.IdWarehouse == warehouseId.Value);
        else if (!User.IsInRole("system.full-access")) query = query.Where(x => allowed.Contains(x.document.IdWarehouse));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.document.Status == status);
        var items = await query.OrderByDescending(x => x.document.DocumentDate).ThenByDescending(x => x.document.DocumentNumber)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new DocumentListItem(x.document.Id, x.document.IdWarehouse, x.document.IdDocumentType,
                x.document.IdParty, x.document.IdSalesPlanReference, x.document.DocumentNumber, x.document.DocumentDate,
                x.document.Status, x.warehouse.Name, x.type.Name, x.type.Direction, x.type.AffectsStock,
                x.party.Name, x.plan.DisplayName, x.document.Reason, x.document.ExternalReference, x.document.CustomerOrderKey))
            .ToListAsync(cancellationToken);
        return Ok(new { items, page, pageSize });
    }

    [HttpGet("lookups")]
    public async Task<IActionResult> Lookups(CancellationToken cancellationToken)
    {
        long[] allowed = await AllowedWarehouseIdsAsync(cancellationToken);
        var warehouseQuery = db.Warehouses.AsNoTracking().Where(x => x.IsActive);
        if (!User.IsInRole("system.full-access")) warehouseQuery = warehouseQuery.Where(x => allowed.Contains(x.Id));
        var warehouses = await warehouseQuery.OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Code, x.Name, x.IdCompany, x.IsSalesWarehouse }).ToListAsync(cancellationToken);
        var types = await db.WarehouseDocumentTypes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Code, x.Name, x.Direction, x.AffectsStock }).ToListAsync(cancellationToken);
        var partyQuery = db.WarehouseParties.AsNoTracking().Where(x => x.IsActive);
        if (!User.IsInRole("system.full-access")) partyQuery = partyQuery.Where(x => x.IdWarehouse == null || allowed.Contains(x.IdWarehouse.Value));
        var parties = await partyQuery.OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.PartyType, x.IdWarehouse }).ToListAsync(cancellationToken);
        var plans = await db.SalesPlanReferences.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayName)
            .Select(x => new { x.Id, x.StoreSalesPlanKey, x.DisplayName, x.IsRegular }).ToListAsync(cancellationToken);
        var units = await (from productUnit in db.WarehouseProductUnits.AsNoTracking()
                           join unit in db.WarehouseUnits.AsNoTracking() on productUnit.IdUnit equals unit.Id
                           where productUnit.IsActive && unit.IsActive
                           select new { productUnit.IdProduct, unit.Id, unit.Name, productUnit.ConversionFactor, productUnit.IsBaseUnit })
            .ToListAsync(cancellationToken);
        var unitGroups = units.GroupBy(x => x.IdProduct).ToDictionary(group => group.Key,
            group => group.Select(x => new { x.Id, x.Name, x.ConversionFactor, x.IsBaseUnit }).ToArray());
        var items = await db.InventoryItems.AsNoTracking().Where(x => x.IsActive && x.Product.IsActive
                && (x.ProductVariant == null || x.ProductVariant.IsActive))
            .OrderBy(x => x.Product.Name).ThenBy(x => x.ProductVariant!.Name)
            .Select(x => new { x.Id, x.IdProduct, x.IdProductVariant, ProductName = x.Product.Name,
                VariantName = x.ProductVariant == null ? null : x.ProductVariant.Name, x.Product.IdBaseUnit })
            .ToListAsync(cancellationToken);
        var inventoryItems = items.Select(item => new
        {
            item.Id, item.IdProduct, item.IdProductVariant, item.ProductName, item.VariantName, item.IdBaseUnit,
            Units = unitGroups.GetValueOrDefault(item.IdProduct) ?? []
        });
        return Ok(new { warehouses, types, parties, plans, inventoryItems });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Detail(long id, CancellationToken cancellationToken)
    {
        WarehouseDocumentRecord? document = await db.WarehouseDocuments.AsNoTracking().Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (document is null) return NotFound();
        if (!await CanAccessWarehouseAsync(document.IdWarehouse, cancellationToken)) return Forbid();
        return Ok(new DocumentDetail(document.Id, document.IdWarehouse, document.IdDocumentType, document.IdParty,
            document.IdSalesPlanReference, document.DocumentNumber, document.DocumentDate, document.Status,
            document.Reason, document.ExternalReference, document.CustomerOrderKey, document.CreatedByUserKey, document.CreatedAtUtc,
            document.UpdatedByUserKey, document.UpdatedAtUtc,
            document.Lines.OrderBy(x => x.Id).Select(x => new DocumentLineDetail(x.IdInventoryItem, x.IdUnit,
                x.Quantity, x.ConversionFactor, x.BaseQuantity, x.ProductNameSnapshot, x.VariantNameSnapshot, x.UnitNameSnapshot)).ToArray()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] SaveWarehouseDocumentInput input, CancellationToken cancellationToken)
    {
        if (!await CanAccessWarehouseAsync(input.WarehouseId, cancellationToken)) return Forbid();
        IActionResult? failure = await ValidateHeaderAsync(input, cancellationToken);
        if (failure is not null) return failure;
        IActionResult? lineFailure = ValidateLines(input.Lines);
        if (lineFailure is not null) return lineFailure;
        PreparedLine[] lines = await PrepareLinesAsync(input.Lines!, cancellationToken);
        if (lines.Length != input.Lines!.Count) return ValidationProblem("کالا یا واحد انتخاب‌شده برای یکی از اقلام معتبر نیست.");

        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var document = new WarehouseDocumentRecord
        {
            IdWarehouse = input.WarehouseId,
            IdDocumentType = input.DocumentTypeId,
            IdParty = input.PartyId,
            IdSalesPlanReference = input.SalesPlanId,
            DocumentDate = input.DocumentDate,
            Status = input.Status,
            ExternalReference = input.ExternalReference?.Trim(),
            Reason = input.Reason.Trim(),
            CreatedByUserKey = CurrentUserKey(),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.WarehouseDocuments.Add(document);
        await db.SaveChangesAsync(cancellationToken);
        AddLines(document.Id, lines);
        await db.SaveChangesAsync(cancellationToken);
        try
        {
            await RebuildStockAsync(document.Id, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Ok(new { document.Id, document.DocumentNumber, message = "سند انبار ثبت شد." });
        }
        catch (SqlException exception) when (exception.Number == 51002)
        {
            return Conflict(new { detail = "موجودی این عملیات منفی می‌شود و سند ذخیره نشد." });
        }
    }

    [HttpPut("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(long id, [FromBody] SaveWarehouseDocumentInput input, CancellationToken cancellationToken)
    {
        WarehouseDocumentRecord? document = await db.WarehouseDocuments.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (document is null) return NotFound();
        if (!await CanAccessWarehouseAsync(document.IdWarehouse, cancellationToken)
            || !await CanAccessWarehouseAsync(input.WarehouseId, cancellationToken)) return Forbid();
        if (await IsPaidOrderLinkedAsync(id, cancellationToken))
            return Conflict(new { detail = "سند متصل به سفارش از مسیر مدیریت سفارش تغییر می‌کند." });
        IActionResult? failure = await ValidateHeaderAsync(input, cancellationToken);
        if (failure is not null) return failure;
        IActionResult? lineFailure = ValidateLines(input.Lines);
        if (lineFailure is not null) return lineFailure;
        PreparedLine[] lines = await PrepareLinesAsync(input.Lines!, cancellationToken);
        if (lines.Length != input.Lines!.Count) return ValidationProblem("کالا یا واحد انتخاب‌شده برای یکی از اقلام معتبر نیست.");

        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            await ReverseMovementsAndClearLinesAsync(id, cancellationToken);
            document.IdWarehouse = input.WarehouseId;
            document.IdDocumentType = input.DocumentTypeId;
            document.IdParty = input.PartyId;
            document.IdSalesPlanReference = input.SalesPlanId;
            document.DocumentDate = input.DocumentDate;
            document.Status = input.Status;
            document.ExternalReference = input.ExternalReference?.Trim();
            document.Reason = input.Reason.Trim();
            document.UpdatedByUserKey = CurrentUserKey();
            document.UpdatedAtUtc = DateTime.UtcNow;
            AddLines(id, lines);
            await db.SaveChangesAsync(cancellationToken);
            await RebuildStockAsync(id, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Ok(new { document.Id, document.DocumentNumber, message = "سند و گردش موجودی باهم به‌روزرسانی شدند." });
        }
        catch (SqlException exception) when (exception.Number is 51002 or 51003)
        {
            return Conflict(new { detail = "ویرایش سند باعث منفی‌شدن موجودی می‌شود؛ تغییر ثبت نشد." });
        }
    }

    [HttpDelete("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        WarehouseDocumentRecord? document = await db.WarehouseDocuments.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (document is null) return NotFound();
        if (!await CanAccessWarehouseAsync(document.IdWarehouse, cancellationToken)) return Forbid();
        if (await IsPaidOrderLinkedAsync(id, cancellationToken))
            return Conflict(new { detail = "سند متصل به سفارش از مسیر مدیریت سفارش تغییر می‌کند." });

        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            await ReverseMovementsAndClearLinesAsync(id, cancellationToken);
            db.WarehouseDocuments.Remove(document);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Ok(new { deleted = true });
        }
        catch (SqlException exception) when (exception.Number == 51003)
        {
            return Conflict(new { detail = "بازگردانی گردش این سند موجودی منفی ایجاد می‌کند؛ حذف نشد." });
        }
    }

    private async Task<IActionResult?> ValidateHeaderAsync(SaveWarehouseDocumentInput input, CancellationToken cancellationToken)
    {
        if (input.WarehouseId < 1 || input.DocumentTypeId < 1 || input.PartyId < 1 || input.SalesPlanId < 1
            || input.DocumentDate == default || input.DocumentDate > DateOnly.FromDateTime(DateTime.Today)
            || input.Status is not ("D" or "F") || string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length > 1000
            || (input.ExternalReference?.Trim().Length ?? 0) > 128)
        {
            return ValidationProblem("انبار، نوع سند، مرجع، طرح، تاریخ، وضعیت و دلیل را درست تکمیل کنید.");
        }
        bool valid = await db.Warehouses.AnyAsync(x => x.Id == input.WarehouseId && x.IsActive, cancellationToken)
            && await db.WarehouseDocumentTypes.AnyAsync(x => x.Id == input.DocumentTypeId && x.IsActive, cancellationToken)
            && await db.WarehouseParties.AnyAsync(x => x.Id == input.PartyId && x.IsActive, cancellationToken)
            && await db.SalesPlanReferences.AnyAsync(x => x.Id == input.SalesPlanId && x.IsActive, cancellationToken);
        if (!valid)
        {
            return ValidationProblem("یکی از انبار، نوع سند، مرجع یا طرح فروش غیرفعال است.");
        }
        long? linkedWarehouseId = await db.WarehouseParties.AsNoTracking().Where(x => x.Id == input.PartyId)
            .Select(x => x.IdWarehouse).SingleOrDefaultAsync(cancellationToken);
        if (linkedWarehouseId.HasValue && !await CanAccessWarehouseAsync(linkedWarehouseId.Value, cancellationToken)) return Forbid();
        var selectedType = await db.WarehouseDocumentTypes.AsNoTracking().Where(x => x.Id == input.DocumentTypeId)
            .Select(x => new { x.Code, x.Name }).SingleAsync(cancellationToken);
        if ((selectedType.Code.Contains("return", StringComparison.OrdinalIgnoreCase) || selectedType.Name.Contains("مرجوع"))
            && (string.IsNullOrWhiteSpace(input.ExternalReference)
                || !await db.WarehouseParties.AnyAsync(x => x.Id == input.PartyId && x.IsActive
                    && (x.PartyType == "Person" || x.PartyType == "Organization"), cancellationToken)))
            return ValidationProblem("برای سند مرجوعی، مشتری و شمارهٔ سفارش را ثبت کنید.");
        return null;
    }

    private IActionResult? ValidateLines(IReadOnlyList<WarehouseDocumentLineInput>? lines)
    {
        if (lines is null || lines.Count is < 1 or > 200 || lines.Any(x => x.InventoryItemId < 1 || x.UnitId < 1 || x.Quantity <= 0 || x.Quantity > 999999999999999.999m))
            return ValidationProblem("سند باید یک تا ۲۰۰ قلم با مقدار مثبت داشته باشد.");
        return null;
    }

    private async Task<PreparedLine[]> PrepareLinesAsync(IReadOnlyList<WarehouseDocumentLineInput> input, CancellationToken cancellationToken)
    {
        var itemIds = input.Select(x => x.InventoryItemId).ToArray();
        var inventoryItems = await db.InventoryItems.AsNoTracking().Include(x => x.Product).Include(x => x.ProductVariant)
            .Where(x => itemIds.Contains(x.Id) && x.IsActive && x.Product.IsActive && (x.ProductVariant == null || x.ProductVariant.IsActive))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var productIds = inventoryItems.Values.Select(x => x.IdProduct).Distinct().ToArray();
        var selectedUnits = await (from productUnit in db.WarehouseProductUnits.AsNoTracking()
                                   join unit in db.WarehouseUnits.AsNoTracking() on productUnit.IdUnit equals unit.Id
                                   where productIds.Contains(productUnit.IdProduct) && productUnit.IsActive && unit.IsActive
                                   select new { productUnit.IdProduct, productUnit.IdUnit, productUnit.ConversionFactor, unit.Name })
            .ToListAsync(cancellationToken);

        var result = new List<PreparedLine>();
        foreach (WarehouseDocumentLineInput line in input)
        {
            if (!inventoryItems.TryGetValue(line.InventoryItemId, out InventoryItem? item) || item is null) return [];
            var unit = selectedUnits.SingleOrDefault(x => x.IdProduct == item.IdProduct && x.IdUnit == line.UnitId);
            if (unit is null) return [];
            decimal quantity = decimal.Truncate(line.Quantity * 1000m) / 1000m;
            decimal baseQuantity = decimal.Truncate(quantity * unit.ConversionFactor * 1000m) / 1000m;
            if (quantity <= 0 || baseQuantity <= 0 || baseQuantity > 999999999999999.999m) return [];
            result.Add(new PreparedLine(item.Id, unit.IdUnit, quantity, unit.ConversionFactor, baseQuantity,
                item.Product.Name, item.ProductVariant?.Name, unit.Name));
        }
        return result.ToArray();
    }

    private void AddLines(long documentId, IEnumerable<PreparedLine> lines)
    {
        foreach (PreparedLine line in lines)
            db.WarehouseDocumentLines.Add(new WarehouseDocumentLineRecord
            {
                IdDocument = documentId,
                IdInventoryItem = line.InventoryItemId,
                IdUnit = line.UnitId,
                Quantity = line.Quantity,
                ConversionFactor = line.ConversionFactor,
                BaseQuantity = line.BaseQuantity,
                ProductNameSnapshot = line.ProductName,
                VariantNameSnapshot = line.VariantName,
                UnitNameSnapshot = line.UnitName
            });
    }

    private Task<int> RebuildStockAsync(long documentId, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlInterpolatedAsync($"EXEC [Warehouse].[RebuildDocumentStock] @IdDocument={documentId}", cancellationToken);

    private async Task ReverseMovementsAndClearLinesAsync(long documentId, CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            IF EXISTS
            (
                SELECT 1
                FROM [Warehouse].[StockBalance] balance WITH (UPDLOCK, HOLDLOCK)
                INNER JOIN
                (
                    SELECT [IdWarehouse], [IdInventoryItem], [IdSalesPlanReference], SUM([QuantityDelta]) AS [QuantityDelta]
                    FROM [Warehouse].[StockMovement] WHERE [IdDocument]={documentId}
                    GROUP BY [IdWarehouse], [IdInventoryItem], [IdSalesPlanReference]
                ) movement ON movement.[IdWarehouse]=balance.[IdWarehouse]
                    AND movement.[IdInventoryItem]=balance.[IdInventoryItem]
                    AND movement.[IdSalesPlanReference]=balance.[IdSalesPlanReference]
                WHERE balance.[Quantity] - movement.[QuantityDelta] < 0
            ) THROW 51003, 'Removing this document would make stock negative.', 1;

            UPDATE balance
            SET balance.[Quantity]=balance.[Quantity]-movement.[QuantityDelta], balance.[UpdatedAtUtc]=SYSUTCDATETIME()
            FROM [Warehouse].[StockBalance] balance WITH (UPDLOCK, HOLDLOCK)
            INNER JOIN
            (
                SELECT [IdWarehouse], [IdInventoryItem], [IdSalesPlanReference], SUM([QuantityDelta]) AS [QuantityDelta]
                FROM [Warehouse].[StockMovement] WHERE [IdDocument]={documentId}
                GROUP BY [IdWarehouse], [IdInventoryItem], [IdSalesPlanReference]
            ) movement ON movement.[IdWarehouse]=balance.[IdWarehouse]
                AND movement.[IdInventoryItem]=balance.[IdInventoryItem]
                AND movement.[IdSalesPlanReference]=balance.[IdSalesPlanReference];
            DELETE FROM [Warehouse].[StockMovement] WHERE [IdDocument]={documentId};
            """, cancellationToken);
        await db.WarehouseDocumentLines.Where(x => x.IdDocument == documentId).ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<bool> CanAccessWarehouseAsync(long id, CancellationToken cancellationToken) =>
        User.IsInRole("system.full-access") || await db.AdminUserWarehouseAccesses.AsNoTracking()
            .AnyAsync(x => x.IdUser == CurrentUserId() && x.IdWarehouse == id, cancellationToken);

    private async Task<bool> IsPaidOrderLinkedAsync(long documentId, CancellationToken cancellationToken) =>
        await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM [Warehouse].[PaidOrderInboxDocument] WHERE [IdWarehouseDocument]={0}", documentId)
            .SingleAsync(cancellationToken) > 0;

    private async Task<long[]> AllowedWarehouseIdsAsync(CancellationToken cancellationToken) => User.IsInRole("system.full-access")
        ? await db.Warehouses.AsNoTracking().Select(x => x.Id).ToArrayAsync(cancellationToken)
        : await db.AdminUserWarehouseAccesses.AsNoTracking().Where(x => x.IdUser == CurrentUserId())
            .Select(x => x.IdWarehouse).ToArrayAsync(cancellationToken);

    private long CurrentUserId() => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out long id) ? id : 0;
    private string CurrentUserKey() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "system";

    private sealed record PreparedLine(long InventoryItemId, long UnitId, decimal Quantity, decimal ConversionFactor,
        decimal BaseQuantity, string ProductName, string? VariantName, string UnitName);
    private sealed record DocumentListItem(long Id, long WarehouseId, long DocumentTypeId, long PartyId, long SalesPlanId,
        long Number, DateOnly Date, string Status, string Warehouse,
        string Type, string Direction, bool AffectsStock, string Party, string SalesPlan, string? Reason, string? ExternalReference, string? OrderKey);
    private sealed record DocumentDetail(long Id, long WarehouseId, long DocumentTypeId, long PartyId, long SalesPlanId,
        long Number, DateOnly Date, string Status, string? Reason, string? ExternalReference, string? OrderKey, string CreatedBy, DateTime CreatedAt,
        string? UpdatedBy, DateTime? UpdatedAt, IReadOnlyList<DocumentLineDetail> Lines);
    private sealed record DocumentLineDetail(long InventoryItemId, long UnitId, decimal Quantity, decimal ConversionFactor,
        decimal BaseQuantity, string ProductName, string? VariantName, string UnitName);
}

public sealed record SaveWarehouseDocumentInput(long WarehouseId, long DocumentTypeId, long PartyId, long SalesPlanId,
    DateOnly DocumentDate, string Status, string Reason, string? ExternalReference, IReadOnlyList<WarehouseDocumentLineInput>? Lines);
public sealed record WarehouseDocumentLineInput(long InventoryItemId, long UnitId, decimal Quantity);
