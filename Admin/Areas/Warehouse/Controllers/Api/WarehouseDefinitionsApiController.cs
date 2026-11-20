using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Domain.Warehouse.Entities;
using Shop.Data.Infrastructure.Persistence;
using WarehouseEntity = Shop.Data.Domain.Warehouse.Entities.Warehouse;

namespace Shop.Admin.Areas.Warehouse.Controllers.Api;

[Area("Warehouse")]
[ApiController]
[Route("api/admin/warehouse/definitions")]
public sealed class WarehouseDefinitionsApiController(ShopDbContext db) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet("{section}")]
    public async Task<IActionResult> Get(string section, CancellationToken cancellationToken) => section switch
    {
        "companies" => await GetCompaniesAsync(cancellationToken),
        "warehouses" => await GetWarehousesAsync(cancellationToken),
        "document-types" => Ok(await db.WarehouseDocumentTypes.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Code, x.Name, x.Direction, x.AffectsStock, x.IsActive }).ToListAsync(cancellationToken)),
        "references" => await GetReferencesAsync(cancellationToken),
        "unit-groups" => Ok(await db.WarehouseUnits.AsNoTracking().Where(x => x.IdParent == null)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.NameFa)
            .Select(x => new { x.Id, Name = x.NameFa, x.NameEn, x.IdParent, x.IsActive, x.SortOrder,
                CanDelete = !db.WarehouseUnits.Any(child => child.IdParent == x.Id) }).ToListAsync(cancellationToken)),
        "units" => Ok(await db.WarehouseUnits.AsNoTracking().Where(x => x.IdParent != null).OrderBy(x => x.NameFa)
            .Select(x => new { x.Id, x.NameFa, x.NameEn, x.IdParent, ParentName = db.WarehouseUnits.Where(parent => parent.Id == x.IdParent).Select(parent => parent.NameFa).FirstOrDefault(), x.IsActive,
                CanDelete = !db.WarehouseUnits.Any(child => child.IdParent == x.Id) &&
                            !db.WarehouseProductUnits.Any(link => link.IdUnit == x.Id) &&
                            !db.WarehouseDocumentLines.Any(line => line.IdUnit == x.Id) })
            .ToListAsync(cancellationToken)),
        "product-units" => Ok(await (from link in db.WarehouseProductUnits.AsNoTracking()
                                     join product in db.Products.AsNoTracking() on link.IdProduct equals product.Id
                                     join unit in db.WarehouseUnits.AsNoTracking() on link.IdUnit equals unit.Id
                                     where !link.IsActive
                                     orderby product.Name, link.IsBaseUnit descending, unit.NameFa
                                     select new
                                     {
                                         link.Id,
                                         link.IdProduct,
                                         ProductName = product.Name,
                                         link.IdUnit,
                                         UnitName = unit.NameFa,
                                         link.ConversionFactor,
                                         link.IsBaseUnit,
                                         link.IsActive
                                     }).ToListAsync(cancellationToken)),
        "variants" => await GetVariantsAsync(cancellationToken),
        "lookups" => await GetLookupsAsync(cancellationToken),
        "stock-owners" => Ok(await db.SalesPlanReferences.AsNoTracking().OrderBy(x => x.DisplayName)
            .Select(x => new { x.Id, x.StoreSalesPlanKey, x.DisplayName, x.IdStockOwnerType, x.Code, x.PlanType, x.IsActive }).ToListAsync(cancellationToken)),
        "sales-plan-mapping" => await GetSalesPlanMappingAsync(cancellationToken),
        _ => NotFound()
    };

    private async Task<IActionResult> GetVariantsAsync(CancellationToken cancellationToken)
    {
        if (!CanAccessVariants("variants")) return Forbid();
        var variants = await (from variant in db.ProductVariants.AsNoTracking()
                              join product in db.Products.AsNoTracking() on variant.IdProduct equals product.Id
                              orderby product.Name, variant.Name
                              select new { variant.Id, variant.IdProduct, ProductName = product.Name, variant.Code, variant.Name, variant.IsDefault, variant.IsActive })
            .ToListAsync(cancellationToken);
        long[] ids = variants.Select(x => x.Id).ToArray();
        var attributes = await (from link in db.ProductVariantAttributeValues.AsNoTracking()
                                join value in db.ProductAttributeValues.AsNoTracking() on link.IdProductAttributeValue equals value.Id
                                join attribute in db.ProductAttributes.AsNoTracking() on value.IdProductAttribute equals attribute.Id
                                where ids.Contains(link.IdProductVariant)
                                orderby attribute.SortOrder, attribute.Id
                                select new { link.IdProductVariant, attribute.Id, AttributeKey = attribute.Key, AttributeLabel = attribute.Label, ValueId = value.Id, value.ValueKey, value.DisplayValue, value.Swatch })
            .ToListAsync(cancellationToken);
        var byVariant = attributes.GroupBy(x => x.IdProductVariant).ToDictionary(x => x.Key,
            x => x.Select(a => new { attributeId = a.Id, valueId = a.ValueId, key = a.AttributeKey, label = a.AttributeLabel, value = a.ValueKey, display = a.DisplayValue, swatch = a.Swatch }).ToArray());
        return Ok(variants.Select(x => new
        {
            x.Id, x.IdProduct, x.ProductName, x.Code, x.Name, x.IsDefault, x.IsActive,
            Attributes = byVariant.GetValueOrDefault(x.Id) ?? []
        }));
    }

    [HttpGet("variant-options/{productId:long}")]
    public async Task<IActionResult> GetVariantOptions(long productId, [FromQuery] long? variantId, CancellationToken cancellationToken)
    {
        if (!CanAccessVariants("variants")) return Forbid();
        if (!await db.Products.AnyAsync(x => x.Id == productId && x.IsWarehouseEnabled, cancellationToken)) return NotFound();
        if (variantId.HasValue && !await db.ProductVariants.AnyAsync(x => x.Id == variantId && x.IdProduct == productId, cancellationToken)) return NotFound();
        var rows = await (from attribute in db.ProductAttributes.AsNoTracking()
                          join value in db.ProductAttributeValues.AsNoTracking() on attribute.Id equals value.IdProductAttribute
                          where attribute.IdProduct == productId && value.IdProduct == productId &&
                                (attribute.IsActive && value.IsActive || variantId.HasValue && db.ProductVariantAttributeValues.Any(link => link.IdProductVariant == variantId && link.IdProductAttribute == attribute.Id && link.IdProductAttributeValue == value.Id))
                          orderby attribute.SortOrder, attribute.Id, value.Id
                          select new { attribute.Id, attribute.Key, attribute.Label, ValueId = value.Id, value.ValueKey, ValueLabel = value.DisplayValue, value.Swatch })
            .ToListAsync(cancellationToken);
        return Ok(rows.GroupBy(x => new { x.Id, x.Key, x.Label }).Select(group => new
        {
            id = group.Key.Id, key = group.Key.Key, label = group.Key.Label,
            values = group.Select(x => new { id = x.ValueId, key = x.ValueKey, label = x.ValueLabel, swatch = x.Swatch }).ToArray()
        }).ToArray());
    }

    [HttpPost("{section}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string section, [FromBody] JsonElement payload,
        CancellationToken cancellationToken) =>
        !CanAccessVariants(section) ? Forbid() : await SaveCoreAsync(section, null, payload, cancellationToken);

    [HttpPut("{section}/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(string section, long id, [FromBody] JsonElement payload,
        CancellationToken cancellationToken) =>
        !CanAccessVariants(section) ? Forbid() : await SaveCoreAsync(section, id, payload, cancellationToken);

    [HttpDelete("{section}/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUnusedUnitDefinition(string section, long id, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        if (section is not ("units" or "unit-groups")) return NotFound();

        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        WarehouseUnit? unit = await db.WarehouseUnits.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (unit is null || (section == "units") != unit.IdParent.HasValue) return NotFound();

        if (section == "units")
        {
            bool inUse = await db.WarehouseUnits.AnyAsync(x => x.IdParent == id, cancellationToken) ||
                         await db.WarehouseProductUnits.AnyAsync(x => x.IdUnit == id, cancellationToken) ||
                         await db.WarehouseDocumentLines.AnyAsync(x => x.IdUnit == id, cancellationToken);
            if (inUse) return Conflict(new { detail = "واحدی که زیرواحد، اتصال کالا یا سابقهٔ سند انبار دارد قابل حذف نیست؛ در صورت نیاز آن را غیرفعال کنید." });
        }
        else if (await db.WarehouseUnits.AnyAsync(x => x.IdParent == id, cancellationToken))
            return Conflict(new { detail = "گروه دارای واحد یا زیرگروه است و قابل حذف نیست." });

        db.WarehouseUnits.Remove(unit);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = section == "units" ? "واحد حذف شد." : "گروه واحد حذف شد." });
    }

    [HttpPut("sales-plan-mapping")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMapping([FromBody] SalesPlanMappingInput input,
        CancellationToken cancellationToken)
    {
        if (input.CompanyId < 1 || input.WarehouseId < 1 || input.SalesPlanId < 1)
            return ValidationProblem(detail: "شرکت، انبار فروش و طرح سایت را انتخاب کنید.", statusCode: 400);

        if (!User.IsInRole("system.full-access")) return Forbid();

        WarehouseEntity? warehouse = await db.Warehouses.SingleOrDefaultAsync(x => x.Id == input.WarehouseId
                                                                             && x.IdCompany == input.CompanyId &&
                                                                             x.Company.IsActive &&
                                                                             x.IsActive, cancellationToken);
        if (warehouse is null) return ValidationProblem(detail: "انبار انتخاب‌شده، انبار فروش فعال شرکت نیست.", statusCode: 400);

        if (!await db.SalesPlanReferences.AnyAsync(x => x.Id == input.SalesPlanId && x.IsActive, cancellationToken))
            return ValidationProblem(detail: "طرح فروش انتخاب‌شده فعال نیست.", statusCode: 400);

        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var links = await db.WarehouseStockOwners.Where(x => x.IdWarehouse == input.WarehouseId).ToListAsync(cancellationToken);
        var selected = links.SingleOrDefault(x => x.IdStockOwner == input.SalesPlanId);
        if (selected is null)
        {
            selected = new WarehouseStockOwner { IdWarehouse = input.WarehouseId, IdStockOwner = input.SalesPlanId };
            db.WarehouseStockOwners.Add(selected);
        }
        if (input.IsDefault || !links.Any(x => x.IsDefault))
        {
            foreach (var link in links) link.IsDefault = false;
            selected.IsDefault = true;
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new { message = "مالک به انبار اختصاص یافت." });
    }

    [HttpDelete("sales-plan-mapping/{warehouseId:long}/{ownerId:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMapping(long warehouseId, long ownerId, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();

        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var link = await db.WarehouseStockOwners.SingleOrDefaultAsync(x => x.IdWarehouse == warehouseId && x.IdStockOwner == ownerId, cancellationToken);
        if (link is null) return NotFound();

        if (await db.WarehouseStockOwners.CountAsync(x => x.IdWarehouse == warehouseId, cancellationToken) <= 1
            || link.IsDefault
            || await db.WarehouseDocuments.AnyAsync(x => x.IdWarehouse == warehouseId && x.IdSalesPlanReference == ownerId, cancellationToken)
            || await db.WarehouseDocumentLines.AnyAsync(x => x.IdSalesPlanReference == ownerId
                && db.WarehouseDocuments.Any(document => document.Id == x.IdDocument && document.IdWarehouse == warehouseId), cancellationToken)
            || await db.WarehouseStockMovements.AnyAsync(x => x.IdWarehouse == warehouseId && x.IdSalesPlanReference == ownerId, cancellationToken))
            return Conflict(new { detail = "آخرین اتصال، مالک پیش‌فرض یا مالکی که سند یا گردش انبار دارد قابل حذف نیست." });

        var balances = await db.StockBalances
            .Where(x => x.IdWarehouse == warehouseId && x.IdSalesPlanReference == ownerId)
            .ToListAsync(cancellationToken);
        if (balances.Any(x => x.Quantity != 0))
            return Conflict(new { detail = "مالک دارای ماندهٔ غیرصفر است و اتصال آن قابل حذف نیست." });

        db.StockBalances.RemoveRange(balances);

        db.WarehouseStockOwners.Remove(link);
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Ok(new { message = "اتصال مالک حذف شد." });
    }

    [HttpPut("sales-plan-mapping/{warehouseId:long}/{ownerId:long}/default")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultMapping(long warehouseId, long ownerId, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var target = await db.WarehouseStockOwners.SingleOrDefaultAsync(x => x.IdWarehouse == warehouseId && x.IdStockOwner == ownerId, cancellationToken);
        if (target is null) return NotFound();
        await db.WarehouseStockOwners.Where(x => x.IdWarehouse == warehouseId && x.IsDefault)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.IsDefault, false), cancellationToken);
        await db.Entry(target).ReloadAsync(cancellationToken);
        target.IsDefault = true;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "مالک پیش‌فرض انبار تغییر کرد." });
    }

    [HttpPost("{section}/{id:long}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(string section, long id, CancellationToken cancellationToken)
    {
        if (!CanAccessVariants(section)) return Forbid();
        switch (section)
        {
            case "companies":
                Company? company = await db.Companies.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (company is null) return NotFound();
                if (!User.IsInRole("system.full-access")) return Forbid();
                if (await db.Warehouses.AnyAsync(x => x.IdCompany == id && x.IsActive, cancellationToken))
                    return Conflict(new { detail = "ابتدا انبارهای فعال شرکت را غیرفعال کنید." });
                company.IsActive = false;
                break;
            case "warehouses":
                WarehouseEntity? warehouse = await db.Warehouses.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (warehouse is null) return NotFound();
                if (!await CanAccessWarehouseAsync(id, cancellationToken)) return Forbid();
                if (await IsSiteWarehouseAsync(id, cancellationToken))
                    return Conflict(new { detail = "ابتدا انبار پیش‌فرض سایت را به انبار دیگری منتقل کنید." });
                if (warehouse.IsSalesWarehouse &&
                    await db.Companies.AnyAsync(x => x.Id == warehouse.IdCompany && x.IsActive, cancellationToken))
                    return Conflict(new { detail = "شرکت فعال باید یک انبار فروش داشته باشد؛ ابتدا انبار فروش دیگری برای همین شرکت تعیین کنید." });
                warehouse.IsActive = false;
                break;
            case "document-types":
                WarehouseDocumentType? type = await db.WarehouseDocumentTypes.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (type is null) return NotFound();
                type.IsActive = false;
                type.UpdatedAtUtc = DateTime.UtcNow;
                break;
            case "references":
                WarehouseParty? party = await db.WarehouseParties.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (party is null) return NotFound();
                if (party.IdWarehouse.HasValue &&
                    !await CanAccessWarehouseAsync(party.IdWarehouse.Value, cancellationToken)) return Forbid();
                party.IsActive = false;
                party.UpdatedAtUtc = DateTime.UtcNow;
                break;
            case "units":
                WarehouseUnit? unit = await db.WarehouseUnits.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (unit is null) return NotFound();
                if (await db.WarehouseProductUnits.AnyAsync(x => x.IdUnit == id && x.IsActive, cancellationToken))
                    return Conflict(new { detail = "این واحد هنوز در تعریف واحدهای کالا استفاده می‌شود؛ ابتدا آن اتصال‌ها را غیرفعال کنید." });
                unit.IsActive = false;
                break;
            case "unit-groups":
                WarehouseUnit? group = await db.WarehouseUnits.SingleOrDefaultAsync(x => x.Id == id && x.IdParent == null, cancellationToken);
                if (group is null) return NotFound();
                if (await db.WarehouseUnits.AnyAsync(x => x.IdParent == id && x.IsActive, cancellationToken))
                    return Conflict(new { detail = "برای غیرفعال‌کردن گروه، ابتدا واحدهای فعال آن را به گروه دیگری منتقل یا غیرفعال کنید." });
                group.IsActive = false;
                break;
            case "product-units":
                WarehouseProductUnit? productUnit = await db.WarehouseProductUnits.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (productUnit is null) return NotFound();
                if (productUnit.IsBaseUnit)
                    return Conflict(new { detail = "واحد اصلی کالا را غیرفعال نکنید؛ ابتدا واحد اصلی دیگری انتخاب کنید." });
                productUnit.IsActive = false;
                break;
            case "variants":
                await using (var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken))
                {
                    ProductVariant? variant = await db.ProductVariants.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                    if (variant is null) return NotFound();
                    if (variant.IsDefault)
                        return Conflict(new { detail = "تنوع پیش‌فرض قابل غیرفعال‌سازی نیست؛ ابتدا تنوع فعال دیگری را پیش‌فرض کنید." });
                    variant.IsActive = false;
                    variant.UpdatedAtUtc = DateTime.UtcNow;
                    var inventoryItem = await db.InventoryItems.SingleOrDefaultAsync(x => x.IdProductVariant == id, cancellationToken);
                    if (inventoryItem is not null) inventoryItem.IsActive = false;
                    await db.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return Ok(new { message = "رکورد غیرفعال شد و سابقه حفظ شد." });
                }
            default:
                return NotFound();
        }

        await db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "رکورد غیرفعال شد و سابقه حفظ شد." });
    }

    private async Task<IActionResult> SaveCoreAsync(string section, long? id, JsonElement payload,
        CancellationToken cancellationToken)
    {
        switch (section)
        {
            case "stock-owners":
                {
                    if (!User.IsInRole("system.full-access")) return Forbid();

                    StockOwnerInput? input = Parse<StockOwnerInput>(payload);
                    if (input is null || !ValidCodeName(input.Code, input.DisplayName, 64, 200) || input.IdStockOwnerType is not (1 or 2))
                        return ValidationProblem(detail: "کد، نام و نوع مالک معتبر الزامی است.", statusCode: 400);

                    if (!System.Text.RegularExpressions.Regex.IsMatch(input.Code.Trim(), "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
                        return ValidationProblem(detail: "کد باید با حروف کوچک انگلیسی، عدد و خط تیره باشد.", statusCode: 400);

                    if (await db.SalesPlanReferences.AnyAsync(x => x.Id != id &&
                        (x.StoreSalesPlanKey == input.Code.Trim() || x.Code == input.Code.Trim()), cancellationToken))
                        return Conflict(new { detail = "این کد قبلاً ثبت شده است." });

                    SalesPlanReference? entity = id.HasValue ? await db.SalesPlanReferences.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) : new();
                    if (entity is null) return NotFound();

                    if (id.HasValue && entity.IdStockOwnerType != input.IdStockOwnerType)
                        return Conflict(new { detail = "نوع مالک موجود قابل تغییر نیست." });

                    if (id.HasValue && !input.IsActive && (entity.IsRegular ||
                        await db.WarehouseStockOwners.AnyAsync(x => x.IdStockOwner == id.Value, cancellationToken)))
                        return Conflict(new { detail = "طرح عادی یا مالک متصل به انبار قابل غیرفعال‌سازی نیست." });

                    if (!id.HasValue)
                    {
                        entity.StoreSalesPlanKey = input.Code.Trim();
                        entity.IdStockOwnerType = input.IdStockOwnerType;
                        entity.PlanType = input.IdStockOwnerType == 1 ? input.Code.Trim() : "owner";
                    }
                    entity.Code = input.Code.Trim();
                    entity.DisplayName = input.DisplayName.Trim();
                    entity.IsActive = input.IsActive;

                    if (!id.HasValue) db.SalesPlanReferences.Add(entity);
                    await db.SaveChangesAsync(cancellationToken);

                    return Ok(new { entity.Id, message = "مالک ذخیره شد." });
                }
            case "companies":
                {
                    CompanyInput? input = Parse<CompanyInput>(payload);
                    if (input is null || !ValidCodeName(input.Code, input.Name, 50, 200))
                        return ValidationProblem(detail: "کد و نام شرکت را درست وارد کنید.", statusCode: 400);

                    if (input.Phone?.Trim().Length > 50 || input.Address?.Trim().Length > 500 || input.Description?.Trim().Length > 2000)
                        return ValidationProblem(detail: "طول تلفن، آدرس یا توضیحات بیش از حد مجاز است.", statusCode: 400);

                    if (!id.HasValue && !User.IsInRole("system.full-access")) return Forbid();

                    Company entity = id.HasValue ? await db.Companies.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? null! : new Company();
                    if (id.HasValue && entity is null) return NotFound();

                    if (id.HasValue && !User.IsInRole("system.full-access") &&
                        !await CanAccessCompanyAsync(id.Value, cancellationToken)) return Forbid();

                    entity.Code = input.Code.Trim();
                    entity.Name = input.Name.Trim();
                    entity.Phone = NormalizeOptional(input.Phone);
                    entity.Address = NormalizeOptional(input.Address);
                    entity.Description = NormalizeOptional(input.Description);
                    entity.IsActive = input.IsActive;

                    if (!id.HasValue) db.Companies.Add(entity);
                    await db.SaveChangesAsync(cancellationToken);

                    return Ok(new { entity.Id, message = "شرکت ذخیره شد." });
                }
            case "warehouses":
                {
                    WarehouseInput? input = Parse<WarehouseInput>(payload);
                    if (input is null || input.CompanyId < 1 || string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 400 || string.IsNullOrWhiteSpace(input.Code) || input.Code.Trim().Length > 50)
                        return ValidationProblem(detail: "شرکت، کد و نام انبار را درست وارد کنید.", statusCode: 400);

                    if (id.HasValue && !await CanAccessWarehouseAsync(id.Value, cancellationToken)) return Forbid();

                    if (!await db.Companies.AnyAsync(x => x.Id == input.CompanyId && x.IsActive, cancellationToken))
                        return ValidationProblem(detail: "شرکت فعال پیدا نشد.", statusCode: 400);

                    if (!User.IsInRole("system.full-access"))
                    {
                        if (!await CanAccessCompanyAsync(input.CompanyId, cancellationToken)) return Forbid();

                        if (id.HasValue)
                        {
                            long existingCompanyId = await db.Warehouses.Where(x => x.Id == id.Value).Select(x => x.IdCompany).SingleAsync(cancellationToken);
                            if (existingCompanyId != input.CompanyId) return Forbid();
                        }
                    }

                    if (input.OwnerSalesPlanId is null ||
                                                   !await db.SalesPlanReferences.AnyAsync(
                                                       x => x.Id == input.OwnerSalesPlanId && x.IsActive,
                                                       cancellationToken))
                        return ValidationProblem(detail: "برای هر انبار، مالک فعال الزامی است.", statusCode: 400);

                    if (input.IsSiteWarehouse && !input.IsSalesWarehouse)
                        return ValidationProblem(detail: "انبار پیش‌فرض سایت باید انبار فروش باشد.", statusCode: 400);

                    if (!input.IsActive && (input.IsSiteWarehouse || await IsSiteWarehouseAsync(id ?? 0, cancellationToken)))
                        return ValidationProblem(detail: "انبار متصل به سایت باید فعال باشد.", statusCode: 400);

                    if (await IsSiteWarehouseAsync(id ?? 0, cancellationToken) && !input.IsSalesWarehouse)
                        return Conflict(new { detail = "انبار متصل به سایت باید تا زمان جابه‌جایی اتصال، انبار فروش بماند." });

                    if (input.IsSiteWarehouse && !User.IsInRole("system.full-access")) return Forbid();

                    WarehouseEntity entity = id.HasValue ? await db.Warehouses.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? null! : new WarehouseEntity();
                    if (id.HasValue && entity is null) return NotFound();

                    if (id.HasValue && entity.IsSalesWarehouse && !input.IsSalesWarehouse)
                        return Conflict(new { detail = "شرکت فعال باید یک انبار فروش داشته باشد؛ برای انتقال نقش، انبار فروش جایگزین را فعال کنید." });

                    WarehouseEntity? previousSalesWarehouse = input.IsSalesWarehouse
                        ? await db.Warehouses.SingleOrDefaultAsync(
                            x => x.IdCompany == input.CompanyId && x.IsSalesWarehouse && (!id.HasValue || x.Id != id.Value),
                            cancellationToken)
                        : null;
                    if (previousSalesWarehouse is not null &&
                        await IsSiteWarehouseAsync(previousSalesWarehouse.Id, cancellationToken))
                        return Conflict(new { detail = "انبار فروش فعلی به سایت متصل است؛ ابتدا اتصال سایت را به انبار فروش دیگری منتقل کنید." });

                    await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
                    if (await db.Warehouses.AnyAsync(x => x.IdCompany == input.CompanyId && x.Name == input.Name.Trim() && (!id.HasValue || x.Id != id.Value), cancellationToken))
                        return Conflict(new { detail = "این نام انبار قبلاً برای شرکت ثبت شده است." });

                    if (previousSalesWarehouse is not null)
                    {
                        previousSalesWarehouse.IsSalesWarehouse = false;
                    }

                    entity.IdCompany = input.CompanyId;
                    entity.Code = input.Code.Trim();
                    entity.Name = input.Name.Trim();
                    entity.IsSalesWarehouse = input.IsSalesWarehouse;
                    entity.IsActive = input.IsActive;

                    if (!id.HasValue) db.Warehouses.Add(entity);
                    await db.SaveChangesAsync(cancellationToken);

                    var ownerLinks = await db.WarehouseStockOwners.Where(x => x.IdWarehouse == entity.Id).ToListAsync(cancellationToken);
                    foreach (var ownerLink in ownerLinks) ownerLink.IsDefault = false;
                    var selectedOwnerLink = ownerLinks.SingleOrDefault(x => x.IdStockOwner == input.OwnerSalesPlanId);
                    if (selectedOwnerLink is null)
                    {
                        selectedOwnerLink = new WarehouseStockOwner { IdWarehouse = entity.Id, IdStockOwner = input.OwnerSalesPlanId!.Value };
                        db.WarehouseStockOwners.Add(selectedOwnerLink);
                    }
                    selectedOwnerLink.IsDefault = true;
                    await db.SaveChangesAsync(cancellationToken);
                    if (input.IsSiteWarehouse && !await SetSiteWarehouseAsync(entity.Id, cancellationToken))
                        return Conflict(new { detail = "تنظیمات سایت پیدا نشد." });

                    await transaction.CommitAsync(cancellationToken);

                    return Ok(new { entity.Id, message = "انبار ذخیره شد." });
                }
            case "document-types":
                {
                    DocumentTypeInput? input = Parse<DocumentTypeInput>(payload);
                    if (input is null || !ValidCodeName(input.Code, input.Name, 50, 150) ||
                        input.Direction is not ("R" or "I"))
                        return ValidationProblem(detail: "کد، نام و جهت سند را درست وارد کنید.", statusCode: 400);

                    WarehouseDocumentType entity = id.HasValue
                        ? await db.WarehouseDocumentTypes.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? null!
                        : new WarehouseDocumentType();
                    if (id.HasValue && entity is null) return NotFound();

                    if (id.HasValue && await db.WarehouseDocuments.AnyAsync(x => x.IdDocumentType == id)
                                    && (entity.Direction != input.Direction || entity.AffectsStock != input.AffectsStock))
                        return Conflict(new { detail = "جهت و اثر موجودی نوع سندی که سابقه دارد قابل تغییر نیست؛ آن را غیرفعال و نوع تازه تعریف کنید." });

                    entity.Code = input.Code.Trim();
                    entity.Name = input.Name.Trim();
                    entity.Direction = input.Direction;
                    entity.AffectsStock = input.AffectsStock;
                    entity.IsActive = input.IsActive;
                    entity.UpdatedAtUtc = DateTime.UtcNow;

                    if (!id.HasValue) db.WarehouseDocumentTypes.Add(entity);
                    await db.SaveChangesAsync(cancellationToken);

                    return Ok(new { entity.Id, message = "نوع سند ذخیره شد." });
                }
            case "references":
                {
                    PartyInput? input = Parse<PartyInput>(payload);
                    if (input is null || !ValidCodeName(input.Name, input.Name, 200, 200)
                                      || input.PartyType is not ("Person" or "Organization" or "Warehouse" or "Other"))
                        return ValidationProblem(detail: "نوع و نام مرجع معتبر نیست.", statusCode: 400);

                    long? linkedWarehouse = input.PartyType == "Warehouse" ? input.WarehouseId : null;
                    if (input.PartyType == "Warehouse" && (linkedWarehouse is null ||
                                                           !await db.Warehouses.AnyAsync(
                                                               x => x.Id == linkedWarehouse && x.IsActive,
                                                               cancellationToken)))
                        return ValidationProblem(detail: "برای مرجع انبار، انبار فعال را انتخاب کنید.", statusCode: 400);

                    if (linkedWarehouse.HasValue &&
                        !await CanAccessWarehouseAsync(linkedWarehouse.Value, cancellationToken)) return Forbid();

                    WarehouseParty entity = id.HasValue ? await db.WarehouseParties.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? null! : new WarehouseParty();
                    if (id.HasValue && entity is null) return NotFound();

                    if (entity.IdWarehouse.HasValue &&
                        !await CanAccessWarehouseAsync(entity.IdWarehouse.Value, cancellationToken)) return Forbid();

                    entity.Name = input.Name.Trim();
                    entity.PartyType = input.PartyType;
                    entity.ExternalPartyKey = string.IsNullOrWhiteSpace(input.ExternalPartyKey) ? null : input.ExternalPartyKey.Trim();
                    entity.IdWarehouse = linkedWarehouse;
                    entity.IsActive = input.IsActive;
                    entity.UpdatedAtUtc = DateTime.UtcNow;

                    if (!id.HasValue) db.WarehouseParties.Add(entity);
                    await db.SaveChangesAsync(cancellationToken);

                    return Ok(new { entity.Id, message = "مرجع ذخیره شد." });
                }
            case "units":
                {
                    UnitInput? input = Parse<UnitInput>(payload);
                    if (input is null || string.IsNullOrWhiteSpace(input.NameFa) || input.NameFa.Trim().Length > 100 || (input.NameEn?.Trim().Length > 100) || !input.ParentId.HasValue)
                        return ValidationProblem(detail: "نام فارسی واحد و گروه والد فعال الزامی است؛ نام انگلیسی اختیاری است.", statusCode: 400);

                    if (!await db.WarehouseUnits.AnyAsync(x => x.Id == input.ParentId!.Value && x.IdParent == null && x.IsActive, cancellationToken))
                        return ValidationProblem(detail: "گروه والد انتخاب‌شده فعال نیست.", statusCode: 400);

                    WarehouseUnit entity = id.HasValue ? await db.WarehouseUnits.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? null! : new WarehouseUnit();
                    if (id.HasValue && entity is null) return NotFound();

                    if (id.HasValue && entity.IsActive && !input.IsActive && !User.IsInRole("system.full-access") &&
                        !User.HasClaim("admin_permission", "Warehouse:DefinitionsApi:Deactivate")) return Forbid();

                    if (id.HasValue && !input.IsActive && await db.WarehouseProductUnits.AnyAsync(x => x.IdUnit == id.Value && x.IsActive, cancellationToken))
                        return Conflict(new { detail = "این واحد برای کالا استفاده می‌شود؛ ابتدا واحدهای وابسته را تغییر دهید یا غیرفعال کنید." });

                    entity.NameFa = input.NameFa.Trim();
                    entity.NameEn = string.IsNullOrWhiteSpace(input.NameEn) ? null : input.NameEn.Trim();
                    entity.IdParent = input.ParentId;
                    entity.IsActive = input.IsActive;

                    if (!id.HasValue) db.WarehouseUnits.Add(entity);
                    await db.SaveChangesAsync(cancellationToken);

                    return Ok(new { entity.Id, message = "واحد ذخیره شد." });
                }
            case "unit-groups":
                {
                    UnitGroupInput? input = Parse<UnitGroupInput>(payload);
                    string name = input?.Name?.Trim() ?? "";
                    if (name.Length is < 1 or > 100 || input!.SortOrder < 0 || input.NameEn?.Trim().Length > 100)
                        return ValidationProblem(detail: "نام گروه و ترتیب نمایش معتبر الزامی است.", statusCode: 400);
                    if (await db.WarehouseUnits.AnyAsync(x => x.IdParent == null && x.NameFa == name && (!id.HasValue || x.Id != id.Value), cancellationToken))
                        return Conflict(new { detail = "گروهی با این نام از قبل ثبت شده است." });

                    WarehouseUnit? entity = id.HasValue
                        ? await db.WarehouseUnits.SingleOrDefaultAsync(x => x.Id == id && x.IdParent == null, cancellationToken)
                        : new WarehouseUnit();
                    if (entity is null) return NotFound();
                    if (id.HasValue && entity.IsActive && !input.IsActive &&
                        await db.WarehouseUnits.AnyAsync(x => x.IdParent == id.Value && x.IsActive, cancellationToken))
                        return Conflict(new { detail = "گروه دارای عضو فعال است؛ ابتدا اعضای آن را جابه‌جا یا غیرفعال کنید." });

                    entity.NameFa = name;
                    entity.NameEn = string.IsNullOrWhiteSpace(input.NameEn) ? null : input.NameEn.Trim();
                    entity.IdParent = null;
                    entity.SortOrder = input.SortOrder;
                    entity.IsActive = input.IsActive;
                    if (!id.HasValue) db.WarehouseUnits.Add(entity);
                    await db.SaveChangesAsync(cancellationToken);
                    return Ok(new { entity.Id, message = "گروه واحد ذخیره شد." });
                }
            case "product-units":
                {
                    ProductUnitInput? input = Parse<ProductUnitInput>(payload);
                    if (input is null || input.ProductId < 1 || input.UnitId < 1 || input.ConversionFactor <= 0 ||
                        input.ConversionFactor > 999999999.999999999m)
                        return ValidationProblem(detail: "کالا، واحد و ضریب تبدیل در محدودهٔ مجاز الزامی است.", statusCode: 400);

                    WarehouseProduct? product = await db.Products.SingleOrDefaultAsync(x => x.Id == input.ProductId && x.IsWarehouseEnabled, cancellationToken);
                    if (product is null ||
                        !await db.WarehouseUnits.AnyAsync(x => x.Id == input.UnitId && x.IdParent != null && x.IsActive, cancellationToken))
                        return ValidationProblem(detail: "کالا یا واحد فعال پیدا نشد.", statusCode: 400);

                    long? currentBaseUnitId = await db.WarehouseProductUnits.AsNoTracking()
                        .Where(x => x.IdProduct == input.ProductId && x.IsBaseUnit && x.IsActive)
                        .Select(x => (long?)x.IdUnit).SingleOrDefaultAsync(cancellationToken);
                    if (!currentBaseUnitId.HasValue)
                        return Conflict(new { detail = "برای کالا واحد پایهٔ فعال تعریف نشده است." });
                    if (input.IsBaseUnit && currentBaseUnitId.Value != input.UnitId)
                        return Conflict(new { detail = "تغییر واحد اصلی کالا بدون تبدیل مانده‌ها و سوابق مجاز نیست." });

                    WarehouseProductUnit? entity = id.HasValue
                        ? await db.WarehouseProductUnits.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
                        : await db.WarehouseProductUnits.SingleOrDefaultAsync(
                            x => x.IdProduct == input.ProductId && x.IdUnit == input.UnitId, cancellationToken);
                    if (id.HasValue && entity is null) return NotFound();
                    if (entity is { IsActive: false })
                        return Conflict(new { detail = "واحد غیرفعال‌شده فقط در تاریخچه قابل مشاهده است و دوباره فعال نمی‌شود." });

                    if (entity is not null && entity.IdProduct != input.ProductId && entity.IsBaseUnit)
                        return Conflict(new { detail = "واحد اصلی کالا را نمی‌توان به کالای دیگری منتقل کرد." });

                    if (entity?.IsBaseUnit == true && (!input.IsBaseUnit || !input.IsActive))
                        return Conflict(new { detail = "واحد اصلی کالا باید فعال بماند؛ ابتدا واحد اصلی جایگزین را تعریف کنید." });

                    if (!input.IsBaseUnit && !await db.WarehouseProductUnits.AnyAsync(
                            x => x.IdProduct == input.ProductId && x.IsBaseUnit && x.IsActive, cancellationToken))
                        return ValidationProblem(detail: "ابتدا واحد اصلی فعال کالا را تعریف کنید.", statusCode: 400);

                    await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
                    entity ??= new WarehouseProductUnit();
                    if (input.IsBaseUnit)
                    {
                        await db.WarehouseProductUnits.Where(x =>
                                x.IdProduct == input.ProductId && x.IsBaseUnit && (!id.HasValue || x.Id != id))
                            .ExecuteUpdateAsync(update => update.SetProperty(x => x.IsBaseUnit, false), cancellationToken);
                        entity.ConversionFactor = 1m;
                    }
                    else
                    {
                        if (entity.IsBaseUnit && entity.IdProduct == input.ProductId)
                            return Conflict(new { detail = "واحد اصلی کالا قابل تبدیل به واحد فرعی نیست؛ ابتدا واحد اصلی جدید را تعریف کنید." });

                        entity.ConversionFactor = decimal.Truncate(input.ConversionFactor * 1_000_000_000m) / 1_000_000_000m;
                    }

                    entity.IdProduct = input.ProductId;
                    entity.IdUnit = input.UnitId;
                    entity.IsBaseUnit = input.IsBaseUnit;
                    entity.IsActive = input.IsActive;

                    if (entity.Id == 0) db.WarehouseProductUnits.Add(entity);
                    await db.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    return Ok(new { entity.Id, message = "واحد کالا و ضریب تبدیل ذخیره شد." });
                }
            case "variants":
                {
                    VariantInput? input = Parse<VariantInput>(payload);
                    if (input is null || (!id.HasValue && input.ProductId < 1) || !ValidCodeName(input.Name, input.Name, 300, 300))
                        return ValidationProblem(detail: "نام تنوع و برای تنوع جدید، کالا را وارد کنید.", statusCode: 400);

                    if (!string.IsNullOrWhiteSpace(input.Code) && input.Code.Length > 100)
                        return ValidationProblem(detail: "کد تنوع حداکثر ۱۰۰ نویسه است.", statusCode: 400);

                    await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
                    ProductVariant entity = id.HasValue ? await db.ProductVariants.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? null! : new ProductVariant();
                    if (id.HasValue && entity is null) return NotFound();

                    if (id.HasValue && entity.IdProduct != input.ProductId)
                        return Conflict(new { detail = "کالای تنوع پس از ایجاد قابل تغییر نیست." });
                    long productId = id.HasValue ? entity.IdProduct : input.ProductId;
                    WarehouseProduct? product = await db.Products.SingleOrDefaultAsync(x => x.Id == productId && x.IsWarehouseEnabled, cancellationToken);
                    if (product is null) return ValidationProblem(detail: "محصول فعال پیدا نشد.", statusCode: 400);
                    if (!await ValidVariantAttributesAsync(productId, input.Attributes, cancellationToken))
                        return ValidationProblem(detail: "ویژگی‌ها و مقدارها باید از درخت تنوع همین کالا انتخاب شوند و ویژگی تکراری نباشد.", statusCode: 400);

                    if (entity.IsDefault && !input.IsActive)
                        return Conflict(new { detail = "تنوع پیش‌فرض را نمی‌توان غیرفعال کرد؛ ابتدا تنوع فعال دیگری را پیش‌فرض کنید." });

                    bool hasOtherDefault = await db.ProductVariants.AnyAsync(x => x.IdProduct == productId && x.Id != (id ?? 0) && x.IsDefault && x.IsActive, cancellationToken);
                    if (!id.HasValue && !hasOtherDefault && !input.IsActive)
                        return ValidationProblem(detail: "نخستین تنوع کالا باید فعال باشد تا به‌عنوان پیش‌فرض ثبت شود.", statusCode: 400);
                    if (!input.IsActive && input.IsDefault)
                        return ValidationProblem(detail: "تنوع پیش‌فرض باید فعال باشد.", statusCode: 400);
                    bool makeDefault = input.IsActive && (input.IsDefault || !hasOtherDefault);
                    if (!makeDefault && entity.IsDefault)
                        return ValidationProblem(detail: "برای هر کالا باید یک تنوع پیش‌فرض فعال باقی بماند.", statusCode: 400);

                    if (makeDefault)
                        await db.ProductVariants.Where(x => x.IdProduct == productId && x.Id != (id ?? 0) && x.IsDefault)
                            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsDefault, false), cancellationToken);
                    entity.IdProduct = productId;
                    entity.Code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim();
                    entity.Name = input.Name.Trim();
                    entity.IsDefault = makeDefault;
                    entity.IsActive = input.IsActive;
                    entity.UpdatedAtUtc = DateTime.UtcNow;

                    if (!id.HasValue) db.ProductVariants.Add(entity);
                    await db.SaveChangesAsync(cancellationToken);

                    if (input.Attributes is not null)
                        await SaveVariantAttributesAsync(entity, input.Attributes, cancellationToken);

                    if (!id.HasValue &&
                        !await db.InventoryItems.AnyAsync(x => x.IdProduct == productId && x.IdProductVariant == entity.Id,
                            cancellationToken))
                    {
                        db.InventoryItems.Add(new InventoryItem { IdProduct = productId, IdProductVariant = entity.Id, IsActive = true });
                        await db.SaveChangesAsync(cancellationToken);
                    }

                    await transaction.CommitAsync(cancellationToken);

                    return Ok(new { entity.Id, message = "تنوع و قلم موجودی آن ذخیره شد." });
                }
            default:
                return NotFound();
        }
    }

    private async Task<bool> ValidVariantAttributesAsync(long productId, IReadOnlyCollection<VariantAttributeInput>? attributes, CancellationToken cancellationToken)
    {
        if (attributes is null) return true;
        if (attributes.Count > 12) return false;
        if (attributes.Any(x => x is null || x.AttributeId < 1 || x.ValueId < 1) || attributes.Select(x => x.AttributeId).Distinct().Count() != attributes.Count) return false;
        foreach (VariantAttributeInput item in attributes)
            if (!await db.ProductAttributeValues.AnyAsync(value => value.Id == item.ValueId && value.IdProduct == productId && value.IdProductAttribute == item.AttributeId && value.Attribute.IdProduct == productId, cancellationToken)) return false;
        return true;
    }

    private bool CanAccessVariants(string section) => !string.Equals(section, "variants", StringComparison.OrdinalIgnoreCase) ||
        User.IsInRole("system.full-access") || User.HasClaim("admin_permission", "Warehouse:Features:Index:variants");

    private async Task SaveVariantAttributesAsync(ProductVariant variant, IReadOnlyCollection<VariantAttributeInput> input,
        CancellationToken cancellationToken)
    {
        var currentLinks = await db.ProductVariantAttributeValues.Where(x => x.IdProductVariant == variant.Id).ToListAsync(cancellationToken);
        db.ProductVariantAttributeValues.RemoveRange(currentLinks);
        if (currentLinks.Count != 0) await db.SaveChangesAsync(cancellationToken);
        if (input.Count == 0) return;

        foreach (VariantAttributeInput item in input)
        {
            ProductAttributeValue value = await db.ProductAttributeValues.SingleAsync(x => x.Id == item.ValueId && x.IdProductAttribute == item.AttributeId && x.IdProduct == variant.IdProduct, cancellationToken);
            db.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue { IdProduct = variant.IdProduct, ProductVariant = variant, AttributeValue = value });
        }
    }

    private async Task<IActionResult> GetWarehousesAsync(CancellationToken cancellationToken)
    {
        long? siteWarehouseId = await db.StoreSiteIdentities.AsNoTracking().Where(x => x.Id == 1) .Select(x => x.IdWarehouse).SingleOrDefaultAsync(cancellationToken);
        var query = from warehouse in db.Warehouses.AsNoTracking()
                    join company in db.Companies.AsNoTracking() on warehouse.IdCompany equals company.Id
                    join ownerLink in db.WarehouseStockOwners.AsNoTracking().Where(x => x.IsDefault) on warehouse.Id equals ownerLink.IdWarehouse into defaultLinks
                    from ownerLink in defaultLinks.DefaultIfEmpty()
                    join plan in db.SalesPlanReferences.AsNoTracking() on ownerLink.IdStockOwner equals plan.Id into plans
                    from plan in plans.DefaultIfEmpty()
                    orderby company.Name, warehouse.Name
                    select new
                    {
                        warehouse.Id,
                        warehouse.IdCompany,
                        CompanyName = company.Name,
                        warehouse.Code,
                        warehouse.Name,
                        warehouse.IsSalesWarehouse,
                        OwnerSalesPlanId = ownerLink == null ? (long?)null : ownerLink.IdStockOwner,
                        OwnerSalesPlan = plan == null ? null : plan.DisplayName,
                        warehouse.IsActive,
                        IsSiteWarehouse = warehouse.Id == siteWarehouseId
                    };
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out long parsed) ? parsed : 0;
            long[] allowed = await db.AdminUserWarehouseAccesses.AsNoTracking().Where(x => x.IdUser == userId) .Select(x => x.IdWarehouse).ToArrayAsync(cancellationToken);
            query = query.Where(x => allowed.Contains(x.Id));
        }

        return Ok(await query.ToListAsync(cancellationToken));
    }

    private async Task<IActionResult> GetCompaniesAsync(CancellationToken cancellationToken)
    {
        IQueryable<Company> query = db.Companies.AsNoTracking();
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out long parsed) ? parsed : 0;
            long[] companyIds = await (from access in db.AdminUserWarehouseAccesses.AsNoTracking()
                                       join warehouse in db.Warehouses.AsNoTracking() on access.IdWarehouse equals warehouse.Id
                                       where access.IdUser == userId
                                       select warehouse.IdCompany).Distinct().ToArrayAsync(cancellationToken);
            query = query.Where(x => companyIds.Contains(x.Id));
        }

        return Ok(await query.OrderBy(x => x.Name).Select(x => new { x.Id, x.Code, x.Name, x.Phone, x.Address, x.Description, x.IsActive }) .ToListAsync(cancellationToken));
    }

    private async Task<IActionResult> GetReferencesAsync(CancellationToken cancellationToken)
    {
        IQueryable<WarehouseParty> query = db.WarehouseParties.AsNoTracking();
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out long parsed) ? parsed : 0;
            long[] allowed = await db.AdminUserWarehouseAccesses.AsNoTracking().Where(x => x.IdUser == userId) .Select(x => x.IdWarehouse).ToArrayAsync(cancellationToken);
            query = query.Where(x => x.IdWarehouse == null || allowed.Contains(x.IdWarehouse.Value));
        }

        return Ok(await query.OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.PartyType, x.ExternalPartyKey, x.IdWarehouse, x.IsActive }).ToListAsync(cancellationToken));
    }

    private async Task<IActionResult> GetSalesPlanMappingAsync(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();

        long? siteId = await db.StoreSiteIdentities.AsNoTracking().Where(x => x.Id == 1).Select(x => x.IdWarehouse).SingleOrDefaultAsync(cancellationToken);
        var companies = await db.Companies.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name) .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken);
        var warehouses = await db.Warehouses.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Name).Select(x => new { x.Id, x.IdCompany, x.Name, x.Code, DefaultOwnerSalesPlanId = db.WarehouseStockOwners.Where(link => link.IdWarehouse == x.Id && link.IsDefault).Select(link => (long?)link.IdStockOwner).FirstOrDefault() }).ToListAsync(cancellationToken);
        var plans = await db.SalesPlanReferences.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayName)
            .Select(x => new { x.Id, x.DisplayName, x.StoreSalesPlanKey }).ToListAsync(cancellationToken);
        var links = await db.WarehouseStockOwners.AsNoTracking().ToListAsync(cancellationToken);

        return Ok(new { siteWarehouseId = siteId, companies, warehouses, plans, links });
    }

    private async Task<IActionResult> GetLookupsAsync(CancellationToken cancellationToken)
    {
        IQueryable<Company> companyQuery = db.Companies.AsNoTracking().Where(x => x.IsActive);
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out long parsed) ? parsed : 0;
            long[] allowedCompanyIds = await (from access in db.AdminUserWarehouseAccesses.AsNoTracking()
                                              join warehouse in db.Warehouses.AsNoTracking() on access.IdWarehouse equals warehouse.Id
                                              where access.IdUser == userId
                                              select warehouse.IdCompany).Distinct().ToArrayAsync(cancellationToken);
            companyQuery = companyQuery.Where(x => allowedCompanyIds.Contains(x.Id));
        }

        var companies = await companyQuery.OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Code }).ToListAsync(cancellationToken);
        var warehouseQuery = from warehouse in db.Warehouses.AsNoTracking()
                             join company in db.Companies.AsNoTracking() on warehouse.IdCompany equals company.Id
                             orderby company.Name, warehouse.Name
                             select new
                             {
                                 warehouse.Id,
                                 warehouse.IdCompany,
                                 CompanyName = company.Name,
                                 warehouse.Code,
                                 warehouse.Name,
                                 warehouse.IsSalesWarehouse,
                                 DefaultOwnerSalesPlanId = db.WarehouseStockOwners.Where(link => link.IdWarehouse == warehouse.Id && link.IsDefault).Select(link => (long?)link.IdStockOwner).FirstOrDefault(),
                                 warehouse.IsActive
                             };
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out long parsed) ? parsed : 0;
            long[] allowed = await db.AdminUserWarehouseAccesses.AsNoTracking().Where(x => x.IdUser == userId) .Select(x => x.IdWarehouse).ToArrayAsync(cancellationToken);
            warehouseQuery = warehouseQuery.Where(x => allowed.Contains(x.Id));
        }

        var warehouses = await warehouseQuery.ToListAsync(cancellationToken);
        var plans = await db.SalesPlanReferences.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayName)
            .Select(x => new { x.Id, x.DisplayName, x.StoreSalesPlanKey }).ToListAsync(cancellationToken);
        var units = await db.WarehouseUnits.AsNoTracking().Where(x => x.IsActive && x.IdParent != null).OrderBy(x => x.NameFa) .Select(x => new { x.Id, name = x.NameFa, x.NameFa, x.NameEn }).ToListAsync(cancellationToken);
        var products = await (from product in db.Products.AsNoTracking()
                              join baseUnit in db.WarehouseProductUnits.AsNoTracking().Where(x => x.IsBaseUnit && x.IsActive)
                                  on product.Id equals baseUnit.IdProduct
                              where product.IsWarehouseEnabled
                              orderby product.Name
                              select new { product.Id, product.Name, StoreProductKey = product.ProductKey, IdBaseUnit = baseUnit.IdUnit })
            .ToListAsync(cancellationToken);

        return Ok(new { companies, warehouses, plans, units, products });
    }

    private async Task<bool> SetSiteWarehouseAsync(long warehouseId, CancellationToken cancellationToken)
    {
        var settings = await db.StoreSiteIdentities.SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);
        if (settings is null) return false;

        settings.IdWarehouse = warehouseId;
        settings.SettingsUpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    private Task<bool> IsSiteWarehouseAsync(long warehouseId, CancellationToken cancellationToken) =>
        db.StoreSiteIdentities.AsNoTracking().AnyAsync(x => x.Id == 1 && x.IdWarehouse == warehouseId, cancellationToken);

    private async Task<bool> CanAccessWarehouseAsync(long warehouseId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("system.full-access")) return true;

        long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out long parsed) ? parsed : 0;

        return await db.AdminUserWarehouseAccesses.AsNoTracking().AnyAsync(x => x.IdUser == userId && x.IdWarehouse == warehouseId, cancellationToken);
    }

    private async Task<bool> CanAccessCompanyAsync(long companyId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("system.full-access")) return true;

        long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out long parsed) ? parsed : 0;

        return await (from access in db.AdminUserWarehouseAccesses.AsNoTracking()
                      join warehouse in db.Warehouses.AsNoTracking() on access.IdWarehouse equals warehouse.Id
                      where access.IdUser == userId && warehouse.IdCompany == companyId
                      select access.IdUser).AnyAsync(cancellationToken);
    }

    private static T? Parse<T>(JsonElement payload)
    {
        try
        {
            return payload.Deserialize<T>(JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static bool ValidCodeName(string? code, string? name, int codeMax, int nameMax) =>
        !string.IsNullOrWhiteSpace(code) && code.Trim().Length <= codeMax && !string.IsNullOrWhiteSpace(name) &&
        name.Trim().Length <= nameMax;

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record StockOwnerInput(string Code, string DisplayName, byte IdStockOwnerType, bool IsActive);

    private sealed record CompanyInput(string Code, string Name, string? Phone, string? Address, string? Description, bool IsActive);

    private sealed record WarehouseInput(
        long CompanyId,
        string Code,
        string Name,
        bool IsSalesWarehouse,
        long? OwnerSalesPlanId,
        bool IsActive,
        bool IsSiteWarehouse);

    private sealed record DocumentTypeInput(
        string Code,
        string Name,
        string Direction,
        bool AffectsStock,
        bool IsActive);

    private sealed record PartyInput(
        string Name,
        string PartyType,
        string? ExternalPartyKey,
        long? WarehouseId,
        bool IsActive);

    private sealed record UnitInput(string NameFa, string? NameEn, long? ParentId, bool IsActive);

    private sealed record UnitGroupInput(string Name, string? NameEn, int SortOrder, bool IsActive);

    private sealed record ProductUnitInput(
        long ProductId,
        long UnitId,
        decimal ConversionFactor,
        bool IsBaseUnit,
        bool IsActive);

    private sealed record VariantInput(
        long ProductId,
        string Name,
        string? Code,
        VariantAttributeInput[]? Attributes,
        bool IsDefault,
        bool IsActive);

    private sealed record VariantAttributeInput(long AttributeId, long ValueId);

    public sealed record SalesPlanMappingInput(long CompanyId, long WarehouseId, long SalesPlanId, bool IsDefault = false);
}
