using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Admin.Infrastructure;
using Shop.Data.Application.Warehouse.Contracts;
using Shop.Data.Domain.Warehouse.Entities;
using Shop.Data.Infrastructure.Persistence;
using Shop.Data.Helpers;

namespace Shop.Admin.Areas.Warehouse.Controllers.Api;

[Area("Warehouse")]
[ApiController]
[Route("api/admin/warehouse/vouchers")]
public sealed class VouchersApiController(ShopDbContext db, IWarehouseStockLedger stockLedger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] long? warehouseId, [FromQuery] string? status,
        [FromQuery] string? q,
        [FromQuery] int page = 1, [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!AdminListQuery.TryCreate(page, pageSize, q, out AdminPageRequest paging)
            || status is not (null or "D" or "F"))
            return ValidationProblem(detail: "فیلتر صفحه یا وضعیت سند معتبر نیست.", statusCode: 400);

        long[] allowed = await AllowedWarehouseIdsAsync(cancellationToken);
        if (!User.IsInRole("system.full-access") && warehouseId.HasValue && !allowed.Contains(warehouseId.Value))
            return Forbid();

        var query = db.Vouchers
            .AsNoTracking()
            .Join(
                db.Warehouses.AsNoTracking(),
                document => document.IdWarehouse,
                warehouse => warehouse.Id,
                (document, warehouse) => new { document, warehouse })
            .Join(
                db.VoucherTypes.AsNoTracking(),
                documentWarehouse => documentWarehouse.document.IdVoucherType,
                type => type.Id,
                (documentWarehouse, type) => new { documentWarehouse.document, documentWarehouse.warehouse, type })
            .Join(
                db.WarehouseParties.AsNoTracking(),
                documentDetails => documentDetails.document.IdParty,
                party => party.Id,
                (documentDetails, party) => new
                {
                    documentDetails.document,
                    documentDetails.warehouse,
                    documentDetails.type,
                    party
                })
            .Join(
                db.SalesPlanReferences.AsNoTracking(),
                documentDetails => documentDetails.document.IdSalesPlanReference,
                plan => plan.Id,
                (documentDetails, plan) => new
                {
                    documentDetails.document,
                    documentDetails.warehouse,
                    documentDetails.type,
                    documentDetails.party,
                    plan
                });

        if (warehouseId.HasValue) query = query.Where(x => x.document.IdWarehouse == warehouseId.Value);
        else if (!User.IsInRole("system.full-access"))
            query = query.Where(x => allowed.Contains(x.document.IdWarehouse));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.document.StatusReference.Code == status);
        if (paging.Search.Length > 0)
            query = query.Where(x => (x.document.ExternalReference ?? "").Contains(paging.Search)
                                     || (x.document.CustomerOrderKey ?? "").Contains(paging.Search)
                                     || x.warehouse.Name.Contains(paging.Search) ||
                                     x.warehouse.Code.Contains(paging.Search)
                                     || x.warehouse.Company.Name.Contains(paging.Search) ||
                                     x.type.Name.Contains(paging.Search)
                                     || x.party.Name.Contains(paging.Search) ||
                                     x.plan.DisplayName.Contains(paging.Search));
        int total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.document.VoucherDate)
            .ThenByDescending(x => x.document.VoucherNumber).Skip(paging.Offset).Take(paging.PageSize).Select(x =>
                new VoucherListItem(x.document.Id, x.document.IdWarehouse, x.document.IdVoucherType,
                    x.document.IdParty, x.document.IdSalesPlanReference, x.document.VoucherNumber,
                    x.document.VoucherDate,
                    x.document.StatusReference.Code, x.document.StatusReference.NameFa,
                    x.warehouse.Company.Name + " / " + x.warehouse.Name + " (" + x.warehouse.Code + ")",
                    x.type.Name, x.type.Direction, x.type.AffectsStock,
                    x.party.Name, x.plan.DisplayName, x.document.Reason, x.document.ExternalReference,
                    x.document.CustomerOrderKey)).ToListAsync(cancellationToken);

        return Ok(AdminListQuery.CreateResult(items, total, paging));
    }

    [HttpGet("lookups")]
    public async Task<IActionResult> Lookups(CancellationToken cancellationToken)
    {
        long[] allowed = await AllowedWarehouseIdsAsync(cancellationToken);
        var warehouseQuery = db.Warehouses.AsNoTracking().Where(x => x.IsActive);
        if (!User.IsInRole("system.full-access")) warehouseQuery = warehouseQuery.Where(x => allowed.Contains(x.Id));
        var warehouses = await warehouseQuery.OrderBy(x => x.Company.Name).ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id, x.Code, x.Name, x.IdCompany, CompanyName = x.Company.Name, x.IsSalesWarehouse,
                DefaultOwnerSalesPlanId = db.WarehouseStockOwners
                    .Where(link => link.IdWarehouse == x.Id && link.IsDefault).Select(link => (long?)link.IdStockOwner)
                    .FirstOrDefault()
            }).ToListAsync(cancellationToken);
        var types = await db.VoucherTypes.AsNoTracking().Where(x => x.IsActive && !x.IsSystemSalesDispatch)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Direction, x.AffectsStock, x.IsReturn, x.IsSystemSalesDispatch })
            .ToListAsync(cancellationToken);
        var partyQuery = db.WarehouseParties.AsNoTracking().Where(x => x.IsActive);
        if (!User.IsInRole("system.full-access"))
            partyQuery = partyQuery.Where(x => x.IdWarehouse == null || allowed.Contains(x.IdWarehouse.Value));
        var parties = await partyQuery.OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.PartyType, x.IdWarehouse }).ToListAsync(cancellationToken);
        var plans = await db.SalesPlanReferences.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayName)
            .Select(x => new { x.Id, x.StoreSalesPlanKey, x.DisplayName, x.IsRegular }).ToListAsync(cancellationToken);
        var units = await db.WarehouseProductUnits
            .AsNoTracking()
            .Join(
                db.WarehouseUnits.AsNoTracking(),
                productUnit => productUnit.IdUnit,
                unit => unit.Id,
                (productUnit, unit) => new { productUnit, unit })
            .Where(productUnit => productUnit.productUnit.IsActive && productUnit.unit.IsActive)
            .Select(productUnit => new
            {
                productUnit.productUnit.IdProduct,
                productUnit.unit.Id,
                productUnit.unit.NameFa,
                productUnit.productUnit.ConversionFactor,
                productUnit.productUnit.IsBaseUnit
            })
            .ToListAsync(cancellationToken);
        var unitGroups = units.GroupBy(x => x.IdProduct).ToDictionary(group => group.Key,
            group => group.Select(x => new { x.Id, Name = x.NameFa, x.ConversionFactor, x.IsBaseUnit }).ToArray());
        var items = await db.InventoryItems
            .AsNoTracking()
            .Join(
                db.WarehouseProductUnits.AsNoTracking().Where(link => link.IsActive && link.IsBaseUnit),
                inventoryItem => inventoryItem.IdProduct,
                baseUnit => baseUnit.IdProduct,
                (inventoryItem, baseUnit) => new { inventoryItem, baseUnit })
            .Where(inventoryItem => inventoryItem.inventoryItem.IsActive &&
                                    inventoryItem.inventoryItem.Product.IsWarehouseEnabled &&
                                    (inventoryItem.inventoryItem.ProductVariant == null ||
                                     inventoryItem.inventoryItem.ProductVariant.IsActive))
            .Select(inventoryItem => new
            {
                inventoryItem.inventoryItem.Id,
                inventoryItem.inventoryItem.IdProduct,
                inventoryItem.inventoryItem.IdProductVariant,
                ProductName = inventoryItem.inventoryItem.Product.Name,
                VariantName = inventoryItem.inventoryItem.ProductVariant == null
                    ? null
                    : inventoryItem.inventoryItem.ProductVariant.VariantName.Name,
                IdBaseUnit = inventoryItem.baseUnit.IdUnit
            })
            .OrderBy(item => item.ProductName)
            .ThenBy(item => item.VariantName)
            .ToListAsync(cancellationToken);
        long[] variantIds = items.Where(item => item.IdProductVariant.HasValue)
            .Select(item => item.IdProductVariant!.Value).Distinct().ToArray();
        var variantAttributes = await db.ProductVariantAttributeValues
            .AsNoTracking()
            .Join(
                db.ProductAttributeValues.AsNoTracking(),
                link => link.IdProductAttributeValue,
                value => value.Id,
                (link, value) => new { link, value })
            .Join(
                db.ProductAttributes.AsNoTracking(),
                linkValue => linkValue.value.IdProductAttribute,
                attribute => attribute.Id,
                (linkValue, attribute) => new { linkValue.link, linkValue.value, attribute })
            .Where(linkValueAttribute => variantIds.Contains(linkValueAttribute.link.IdProductVariant))
            .OrderBy(linkValueAttribute => linkValueAttribute.attribute.SortOrder)
            .ThenBy(linkValueAttribute => linkValueAttribute.attribute.Id)
            .Select(linkValueAttribute => new
            {
                linkValueAttribute.link.IdProductVariant,
                linkValueAttribute.attribute.Label,
                linkValueAttribute.value.DisplayValue
            })
            .ToListAsync(cancellationToken);
        var attributesByVariant = variantAttributes.GroupBy(item => item.IdProductVariant)
            .ToDictionary(group => group.Key,
                group => group.Select(item => new { label = item.Label, value = item.DisplayValue }).ToArray());
        var inventoryItems = items.Select(item => new
        {
            item.Id,
            item.IdProduct,
            item.IdProductVariant,
            item.ProductName,
            item.VariantName,
            VariantAttributes = item.IdProductVariant.HasValue
                ? attributesByVariant.GetValueOrDefault(item.IdProductVariant.Value) ?? []
                : [],
            item.IdBaseUnit,
            Units = unitGroups.GetValueOrDefault(item.IdProduct) ?? []
        });
        long[] warehouseIds = warehouses.Select(w => w.Id).ToArray();
        var ownerLinks = await db.WarehouseStockOwners.AsNoTracking().Where(x => warehouseIds.Contains(x.IdWarehouse))
            .ToListAsync(cancellationToken);

        var statuses = await db.VoucherStatuses.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new { x.Code, x.NameFa, x.NameEn }).ToListAsync(cancellationToken);
        return Ok(new { warehouses, types, parties, plans, inventoryItems, ownerLinks, statuses });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Detail(decimal id, CancellationToken cancellationToken)
    {
        Voucher? document = await db.Vouchers.AsNoTracking().Include(x => x.Lines).Include(x => x.StatusReference)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (document is null) return NotFound();

        if (!await CanAccessWarehouseAsync(document.IdWarehouse, cancellationToken)) return Forbid();

        return Ok(new VoucherDetail(document.Id, document.IdWarehouse, document.IdVoucherType, document.IdParty,
            document.IdSalesPlanReference, document.VoucherNumber, document.VoucherDate, document.StatusReference.Code,
            document.Reason, document.ExternalReference, document.CustomerOrderKey, document.CreatedByUserKey,
            document.CreatedAtUtc,
            document.UpdatedByUserKey, document.UpdatedAtUtc,
            document.Lines.OrderBy(x => x.Id).Select(x => new VoucherLineDetail(x.IdInventoryItem, x.IdUnit,
                x.IdSalesPlanReference ?? document.IdSalesPlanReference,
                x.Quantity, x.ConversionFactor, x.BaseQuantity, x.ProductNameSnapshot, x.VariantNameSnapshot,
                x.UnitNameSnapshot)).ToArray()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] SaveVoucherInput input,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        if (!await CanAccessWarehouseAsync(input.WarehouseId, cancellationToken)) return Forbid();

        IActionResult? lineFailure = ValidateLines(input.Lines);
        if (lineFailure is not null) return lineFailure;

        IActionResult? failure = await ValidateHeaderAsync(input, cancellationToken);
        if (failure is not null) return failure;

        if (!await ValidLinePlansAsync(input, cancellationToken))
            return ValidationProblem(detail: "طرح یکی از اقلام به انبار اختصاص ندارد یا غیرفعال است.", statusCode: 400);

        PreparedLine[] lines = await PrepareLinesAsync(input.Lines!, cancellationToken);
        if (lines.Length != input.Lines!.Count)
            return ValidationProblem(detail: "کالا یا واحد انتخاب‌شده برای یکی از اقلام معتبر نیست.", statusCode: 400);

        // Acquire balance keys before inserting documents so competing issues lock in the same order.
        var document = new Voucher
        {
            IdWarehouse = input.WarehouseId,
            IdVoucherType = input.VoucherTypeId,
            IdParty = input.PartyId,
            IdSalesPlanReference = input.SalesPlanId,
            VoucherDate = input.VoucherDate,
            IdVoucherStatus = await db.VoucherStatuses.Where(x => x.Code == input.Status).Select(x => x.Id)
                .SingleAsync(cancellationToken),
            ExternalReference = input.ExternalReference?.Trim(),
            Reason = input.Reason.Trim(),
            CreatedByUserKey = CurrentUserKey(),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Vouchers.Add(document);
        await db.SaveChangesAsync(cancellationToken);

        AddLines(document.Id, lines);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await RebuildStockAsync(document.Id, cancellationToken);
            db.VoucherAudits.Add(new VoucherAudit
            {
                VoucherId = checked((long)document.Id),
                VoucherNumber = document.VoucherNumber,
                WarehouseId = document.IdWarehouse,
                Action = "Created",
                ActorUserKey = CurrentActorKey(),
                OccurredAtUtc = DateTime.UtcNow,
                Reason = document.Reason,
                AfterJson = await CaptureSnapshotAsync(document.Id, cancellationToken)
            });
            await db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return Ok(new { document.Id, document.VoucherNumber, message = "سند انبار ثبت شد." });
        }
        catch (InsufficientWarehouseStockException)
        {
            return Conflict(new { detail = "موجودی این عملیات منفی می‌شود و سند ذخیره نشد." });
        }
    }

    [HttpPut("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(decimal id, [FromBody] SaveVoucherInput input,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        int claimed = await db.Vouchers.Where(x => x.Id == id)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), cancellationToken);
        if (claimed == 0) return NotFound();

        Voucher? document = await db.Vouchers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (document is null) return NotFound();

        if (!await CanAccessWarehouseAsync(document.IdWarehouse, cancellationToken)
            || !await CanAccessWarehouseAsync(input.WarehouseId, cancellationToken)) return Forbid();

        IActionResult? lineFailure = ValidateLines(input.Lines);
        if (lineFailure is not null) return lineFailure;

        if (await IsPaidOrderLinkedAsync(id, cancellationToken))
            return Conflict(new { detail = "سند متصل به سفارش از مسیر مدیریت سفارش تغییر می‌کند." });

        IActionResult? failure = await ValidateHeaderAsync(input, cancellationToken);
        if (failure is not null) return failure;

        if (!await ValidLinePlansAsync(input, cancellationToken))
            return ValidationProblem(detail: "طرح یکی از اقلام به انبار اختصاص ندارد یا غیرفعال است.", statusCode: 400);

        PreparedLine[] lines = await PrepareLinesAsync(input.Lines!, cancellationToken);
        if (lines.Length != input.Lines!.Count)
            return ValidationProblem(detail: "کالا یا واحد انتخاب‌شده برای یکی از اقلام معتبر نیست.", statusCode: 400);

        try
        {
            string before = await CaptureSnapshotAsync(id, cancellationToken);
            await ReverseMovementsAndClearLinesAsync(id, cancellationToken);
            document.IdWarehouse = input.WarehouseId;
            document.IdVoucherType = input.VoucherTypeId;
            document.IdParty = input.PartyId;
            document.IdSalesPlanReference = input.SalesPlanId;
            document.VoucherDate = input.VoucherDate;
            document.IdVoucherStatus = await db.VoucherStatuses.Where(x => x.Code == input.Status).Select(x => x.Id)
                .SingleAsync(cancellationToken);
            document.ExternalReference = input.ExternalReference?.Trim();
            document.Reason = input.Reason.Trim();
            document.UpdatedByUserKey = CurrentUserKey();
            document.UpdatedAtUtc = DateTime.UtcNow;
            AddLines(id, lines);
            await db.SaveChangesAsync(cancellationToken);

            await RebuildStockAsync(id, cancellationToken);
            db.VoucherAudits.Add(new VoucherAudit
            {
                VoucherId = checked((long)document.Id),
                VoucherNumber = document.VoucherNumber,
                WarehouseId = document.IdWarehouse,
                Action = "Updated",
                ActorUserKey = CurrentActorKey(),
                OccurredAtUtc = DateTime.UtcNow,
                Reason = document.Reason,
                BeforeJson = before,
                AfterJson = await CaptureSnapshotAsync(id, cancellationToken)
            });
            await db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return Ok(new
                { document.Id, document.VoucherNumber, message = "سند و گردش موجودی باهم به‌روزرسانی شدند." });
        }
        catch (InsufficientWarehouseStockException)
        {
            return Conflict(new { detail = "ویرایش سند باعث منفی‌شدن موجودی می‌شود؛ تغییر ثبت نشد." });
        }
    }

    [HttpDelete("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(decimal id, CancellationToken cancellationToken)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        int claimed = await db.Vouchers.Where(x => x.Id == id)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), cancellationToken);
        if (claimed == 0) return NotFound();

        Voucher? document = await db.Vouchers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (document is null) return NotFound();

        if (!await CanAccessWarehouseAsync(document.IdWarehouse, cancellationToken)) return Forbid();

        if (!await db.VoucherStatuses.AnyAsync(x => x.Id == document.IdVoucherStatus && x.Code == "D",
                cancellationToken))
            return Conflict(new { detail = "برای حذف سند، ابتدا وضعیت آن را به موقت تغییر دهید." });

        if (await IsPaidOrderLinkedAsync(id, cancellationToken))
            return Conflict(new { detail = "سند متصل به سفارش از مسیر مدیریت سفارش تغییر می‌کند." });

        try
        {
            string before = await CaptureSnapshotAsync(id, cancellationToken);
            await ReverseMovementsAndClearLinesAsync(id, cancellationToken);
            db.VoucherAudits.Add(new VoucherAudit
            {
                VoucherId = checked((long)document.Id),
                VoucherNumber = document.VoucherNumber,
                WarehouseId = document.IdWarehouse,
                Action = "Deleted",
                ActorUserKey = CurrentActorKey(),
                OccurredAtUtc = DateTime.UtcNow,
                Reason = document.Reason,
                BeforeJson = before
            });
            db.Vouchers.Remove(document);
            await db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return Ok(new { deleted = true });
        }
        catch (InsufficientWarehouseStockException)
        {
            return Conflict(new { detail = "بازگردانی گردش این سند موجودی منفی ایجاد می‌کند؛ حذف نشد." });
        }
    }

    private async Task<IActionResult?> ValidateHeaderAsync(SaveVoucherInput input,
        CancellationToken cancellationToken)
    {
        if (input.WarehouseId < 1 || input.VoucherTypeId < 1 || input.PartyId < 1 || input.SalesPlanId < 1
            || input.VoucherDate == default || input.VoucherDate > DateOnly.FromDateTime(DateTime.Today)
            || input.Status is not ("D" or "F") || string.IsNullOrWhiteSpace(input.Reason) ||
            input.Reason.Trim().Length > 1000
            || (input.ExternalReference?.Trim().Length ?? 0) > 128)
        {
            return ValidationProblem(detail: "انبار، نوع سند، مرجع، طرح، تاریخ، وضعیت و دلیل را درست تکمیل کنید.",
                statusCode: 400);
        }

        bool valid = await db.Warehouses.AnyAsync(x => x.Id == input.WarehouseId && x.IsActive, cancellationToken)
                     && await db.VoucherTypes.AnyAsync(x => x.Id == input.VoucherTypeId && x.IsActive,
                         cancellationToken)
                     && await db.WarehouseParties.AnyAsync(x => x.Id == input.PartyId && x.IsActive, cancellationToken)
                     && await db.SalesPlanReferences.AnyAsync(x => x.Id == input.SalesPlanId && x.IsActive,
                         cancellationToken);
        if (!valid)
        {
            return ValidationProblem(detail: "یکی از انبار، نوع سند، مرجع یا طرح فروش غیرفعال است.", statusCode: 400);
        }

        if (!await db.WarehouseStockOwners.AnyAsync(
                x => x.IdWarehouse == input.WarehouseId && x.IdStockOwner == input.SalesPlanId, cancellationToken))
            return ValidationProblem(detail: "مالک انتخاب‌شده به این انبار اختصاص داده نشده است.", statusCode: 400);

        long? linkedWarehouseId = await db.WarehouseParties.AsNoTracking().Where(x => x.Id == input.PartyId)
            .Select(x => x.IdWarehouse).SingleOrDefaultAsync(cancellationToken);
        if (linkedWarehouseId.HasValue && !await CanAccessWarehouseAsync(linkedWarehouseId.Value, cancellationToken))
            return Forbid();

        var selectedType = await db.VoucherTypes.AsNoTracking().Where(x => x.Id == input.VoucherTypeId)
            .Select(x => new { x.IsSystemSalesDispatch, x.IsReturn }).SingleAsync(cancellationToken);
        if (selectedType.IsSystemSalesDispatch)
            return ValidationProblem(detail: "حوالهٔ فروش فقط از صفحهٔ صدور حوالهٔ سفارش‌های پرداخت‌شده صادر می‌شود.",
                statusCode: 400);

        if (selectedType.IsReturn
            && (string.IsNullOrWhiteSpace(input.ExternalReference)
                || !await db.WarehouseParties.AnyAsync(x => x.Id == input.PartyId && x.IsActive
                    && (x.PartyType == "Person" || x.PartyType == "Organization"), cancellationToken)))
            return ValidationProblem(detail: "برای سند مرجوعی، مشتری و شمارهٔ سفارش را ثبت کنید.", statusCode: 400);

        return null;
    }

    private IActionResult? ValidateLines(IReadOnlyList<VoucherLineInput>? lines)
    {
        if (lines is null || lines.Count is < 1 or > 200 || lines.Any(x =>
                x.InventoryItemId < 1 || x.UnitId < 1 || x.Quantity <= 0 || x.Quantity > 999999999999999.999m))
            return ValidationProblem(detail: "سند باید یک تا ۲۰۰ قلم با مقدار مثبت داشته باشد.", statusCode: 400);

        return null;
    }

    private async Task<bool> ValidLinePlansAsync(SaveVoucherInput input, CancellationToken cancellationToken)
    {
        long[] plans = input.Lines!.Select(line => line.SalesPlanId ?? input.SalesPlanId).Distinct().ToArray();
        int count = await db.WarehouseStockOwners
            .AsNoTracking()
            .Join(
                db.SalesPlanReferences.AsNoTracking(),
                owner => owner.IdStockOwner,
                plan => plan.Id,
                (owner, plan) => new { owner, plan })
            .Where(ownerPlan => ownerPlan.owner.IdWarehouse == input.WarehouseId && ownerPlan.plan.IsActive &&
                                plans.Contains(ownerPlan.plan.Id))
            .Select(ownerPlan => ownerPlan.plan.Id)
            .CountAsync(cancellationToken);

        return count == plans.Length;
    }

    private async Task<PreparedLine[]> PrepareLinesAsync(IReadOnlyList<VoucherLineInput> input,
        CancellationToken cancellationToken)
    {
        var itemIds = input.Select(x => x.InventoryItemId).ToArray();
        var inventoryItems = await db.InventoryItems.AsNoTracking().Include(x => x.Product)
            .Include(x => x.ProductVariant).ThenInclude(x => x!.VariantName).Where(x => itemIds.Contains(x.Id) &&
                x.IsActive && x.Product.IsWarehouseEnabled &&
                (x.ProductVariant == null || x.ProductVariant.IsActive))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var productIds = inventoryItems.Values.Select(x => x.IdProduct).Distinct().ToArray();
        var selectedUnits = await db.WarehouseProductUnits
            .AsNoTracking()
            .Join(
                db.WarehouseUnits.AsNoTracking(),
                productUnit => productUnit.IdUnit,
                unit => unit.Id,
                (productUnit, unit) => new { productUnit, unit })
            .Where(productUnit => productIds.Contains(productUnit.productUnit.IdProduct) &&
                                  productUnit.productUnit.IsActive && productUnit.unit.IsActive)
            .Select(productUnit => new
            {
                productUnit.productUnit.IdProduct,
                productUnit.productUnit.IdUnit,
                productUnit.productUnit.ConversionFactor,
                productUnit.unit.NameFa
            })
            .ToListAsync(cancellationToken);

        var result = new List<PreparedLine>();
        foreach (VoucherLineInput line in input)
        {
            if (!inventoryItems.TryGetValue(line.InventoryItemId, out InventoryItem? item) || item is null) return [];

            var unit = selectedUnits.SingleOrDefault(x => x.IdProduct == item.IdProduct && x.IdUnit == line.UnitId);
            if (unit is null) return [];

            decimal quantity = decimal.Truncate(line.Quantity * 1000m) / 1000m;
            decimal baseQuantity = decimal.Truncate(quantity * unit.ConversionFactor * 1000m) / 1000m;
            if (quantity <= 0 || baseQuantity <= 0 || baseQuantity > 999999999999999.999m) return [];

            result.Add(new PreparedLine(item.Id, unit.IdUnit, line.SalesPlanId, quantity, unit.ConversionFactor,
                baseQuantity,
                item.Product.Name, item.ProductVariant?.VariantName.Name, unit.NameFa));
        }

        return result.ToArray();
    }

    private void AddLines(decimal voucherId, IEnumerable<PreparedLine> lines)
    {
        foreach (PreparedLine line in lines)
            db.VoucherLines.Add(new VoucherLine
            {
                IdVoucher = voucherId,
                IdInventoryItem = line.InventoryItemId,
                IdUnit = line.UnitId,
                IdSalesPlanReference = line.SalesPlanId,
                Quantity = line.Quantity,
                ConversionFactor = line.ConversionFactor,
                BaseQuantity = line.BaseQuantity,
                ProductNameSnapshot = line.ProductName,
                VariantNameSnapshot = line.VariantName,
                UnitNameSnapshot = line.UnitName
            });
    }

    private Task RebuildStockAsync(decimal voucherId, CancellationToken cancellationToken) =>
        stockLedger.RebuildVoucherAsync(voucherId, cancellationToken);

    private async Task<string> CaptureSnapshotAsync(decimal voucherId, CancellationToken cancellationToken)
    {
        Voucher document = await db.Vouchers.AsNoTracking().Include(x => x.Lines).Include(x => x.StatusReference)
            .SingleAsync(x => x.Id == voucherId, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            document.Id,
            document.VoucherNumber,
            document.IdWarehouse,
            document.IdVoucherType,
            document.IdParty,
            document.IdSalesPlanReference,
            document.VoucherDate,
            Status = document.StatusReference.Code,
            document.CustomerOrderKey,
            document.ExternalReference,
            document.Reason,
            document.CreatedByUserKey,
            document.CreatedAtUtc,
            document.UpdatedByUserKey,
            document.UpdatedAtUtc,
            Lines = document.Lines.OrderBy(x => x.Id).Select(line => new
            {
                line.IdInventoryItem,
                line.IdUnit,
                line.IdSalesPlanReference,
                line.Quantity,
                line.ConversionFactor,
                line.BaseQuantity,
                line.ProductNameSnapshot,
                line.VariantNameSnapshot,
                line.UnitNameSnapshot
            })
        });
    }

    private async Task ReverseMovementsAndClearLinesAsync(decimal voucherId, CancellationToken cancellationToken)
    {
        await stockLedger.ReverseMovementsAsync(voucherId, cancellationToken);
        await db.VoucherLines.Where(x => x.IdVoucher == voucherId).ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<bool> CanAccessWarehouseAsync(long id, CancellationToken cancellationToken) =>
        User.IsInRole("system.full-access") || await db.AdminUserWarehouseAccesses.AsNoTracking()
            .AnyAsync(x => x.IdUser == CurrentUserId() && x.IdWarehouse == id, cancellationToken);

    private Task<bool> IsPaidOrderLinkedAsync(decimal voucherId, CancellationToken cancellationToken) =>
        db.StoreCustomerOrders.AnyAsync(x => x.IdVoucher == voucherId, cancellationToken);

    private async Task<long[]> AllowedWarehouseIdsAsync(CancellationToken cancellationToken) =>
        User.IsInRole("system.full-access")
            ? await db.Warehouses.AsNoTracking().Select(x => x.Id).ToArrayAsync(cancellationToken)
            : await db.AdminUserWarehouseAccesses.AsNoTracking().Where(x => x.IdUser == CurrentUserId())
                .Select(x => x.IdWarehouse).ToArrayAsync(cancellationToken);

    private long CurrentUserId() => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out long id) ? id : 0;

    private string CurrentUserKey() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "system";

    private string CurrentActorKey() => $"{CurrentUserKey()}:{User.Identity?.Name ?? "system"}";

    private sealed record PreparedLine(
        decimal InventoryItemId,
        long UnitId,
        long? SalesPlanId,
        decimal Quantity,
        decimal ConversionFactor,
        decimal BaseQuantity,
        string ProductName,
        string? VariantName,
        string UnitName);

    private sealed record VoucherListItem(
        decimal Id,
        long WarehouseId,
        long VoucherTypeId,
        long PartyId,
        long SalesPlanId,
        long VoucherNumber,
        DateOnly VoucherDate,
        string Status,
        string StatusName,
        string Warehouse,
        string VoucherType,
        string Direction,
        bool AffectsStock,
        string Party,
        string SalesPlan,
        string? Reason,
        string? ExternalReference,
        string? OrderKey);

    private sealed record VoucherDetail(
        decimal Id,
        long WarehouseId,
        long VoucherTypeId,
        long PartyId,
        long SalesPlanId,
        long VoucherNumber,
        DateOnly VoucherDate,
        string Status,
        string? Reason,
        string? ExternalReference,
        string? OrderKey,
        string CreatedBy,
        DateTime CreatedAt,
        string? UpdatedBy,
        DateTime? UpdatedAt,
        IReadOnlyList<VoucherLineDetail> Lines);

    private sealed record VoucherLineDetail(
        decimal InventoryItemId,
        long UnitId,
        long SalesPlanId,
        decimal Quantity,
        decimal ConversionFactor,
        decimal BaseQuantity,
        string ProductName,
        string? VariantName,
        string UnitName);
}

public sealed record SaveVoucherInput(
    long WarehouseId,
    long VoucherTypeId,
    long PartyId,
    long SalesPlanId,
    DateOnly VoucherDate,
    string Status,
    string Reason,
    string? ExternalReference,
    IReadOnlyList<VoucherLineInput>? Lines);

public sealed record VoucherLineInput(decimal InventoryItemId, long UnitId, decimal Quantity, long? SalesPlanId);
