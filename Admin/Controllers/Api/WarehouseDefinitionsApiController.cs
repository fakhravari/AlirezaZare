using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Domain.Warehouse.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

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
            .Select(x => new { x.Id, x.Code, x.Name, x.Direction, x.AffectsStock, x.IsActive })
            .ToListAsync(cancellationToken)),
        "references" => await GetReferencesAsync(cancellationToken),
        "units" => Ok(await db.WarehouseUnits.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Code, x.Name, x.IsActive }).ToListAsync(cancellationToken)),
        "product-units" => Ok(await (from link in db.WarehouseProductUnits.AsNoTracking()
                join product in db.Products.AsNoTracking() on link.IdProduct equals product.Id
                join unit in db.WarehouseUnits.AsNoTracking() on link.IdUnit equals unit.Id
                orderby product.Name, link.IsBaseUnit descending, unit.Name
                select new
                {
                    link.Id, link.IdProduct, ProductName = product.Name, link.IdUnit, UnitName = unit.Name,
                    link.ConversionFactor, link.IsBaseUnit, link.IsActive
                })
            .ToListAsync(cancellationToken)),
        "variants" => Ok(await (from variant in db.ProductVariants.AsNoTracking()
                join product in db.Products.AsNoTracking() on variant.IdProduct equals product.Id
                orderby product.Name, variant.Name
                select new
                {
                    variant.Id, variant.IdProduct, ProductName = product.Name,
                    variant.Code, variant.Name, variant.AttributesJson, variant.IsActive
                })
            .ToListAsync(cancellationToken)),
        "lookups" => await GetLookupsAsync(cancellationToken),
        "stock-owners" => Ok(await db.SalesPlanReferences.AsNoTracking().OrderBy(x => x.DisplayName)
            .Select(x => new { x.Id, x.StoreSalesPlanKey, x.DisplayName, x.IdStockOwnerType, x.Code, x.PlanType, x.IsActive }).ToListAsync(cancellationToken)),
        "sales-plan-mapping" => await GetSalesPlanMappingAsync(cancellationToken),
        _ => NotFound()
    };

    [HttpPost("{section}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string section, [FromBody] JsonElement payload,
        CancellationToken cancellationToken) =>
        await SaveCoreAsync(section, null, payload, cancellationToken);

    [HttpPut("{section}/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(string section, long id, [FromBody] JsonElement payload,
        CancellationToken cancellationToken) =>
        await SaveCoreAsync(section, id, payload, cancellationToken);

    [HttpPut("sales-plan-mapping")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMapping([FromBody] SalesPlanMappingInput input,
        CancellationToken cancellationToken)
    {
        if (input.CompanyId < 1 || input.WarehouseId < 1 || input.SalesPlanId < 1)
            return ValidationProblem(detail: "شرکت، انبار فروش و طرح سایت را انتخاب کنید.", statusCode: 400);
        if (!User.IsInRole("system.full-access")) return Forbid();
        Warehouse? warehouse = await db.Warehouses.SingleOrDefaultAsync(x => x.Id == input.WarehouseId
                                                                             && x.IdCompany == input.CompanyId &&
                                                                             x.Company.IsActive &&
                                                                             x.IsActive, cancellationToken);
        if (warehouse is null) return ValidationProblem(detail: "انبار انتخاب‌شده، انبار فروش فعال شرکت نیست.", statusCode: 400);
        if (!await db.SalesPlanReferences.AnyAsync(x => x.Id == input.SalesPlanId && x.IsActive, cancellationToken))
            return ValidationProblem(detail: "طرح فروش انتخاب‌شده فعال نیست.", statusCode: 400);
        if (!await db.WarehouseStockOwners.AnyAsync(x => x.IdWarehouse == input.WarehouseId && x.IdStockOwner == input.SalesPlanId, cancellationToken))
        {
            db.WarehouseStockOwners.Add(new WarehouseStockOwner { IdWarehouse = input.WarehouseId, IdStockOwner = input.SalesPlanId });
            await db.SaveChangesAsync(cancellationToken);
        }
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
            || await db.Warehouses.AnyAsync(x => x.Id == warehouseId && x.IdOwnerSalesPlanReference == ownerId, cancellationToken)
            || await db.WarehouseDocuments.AnyAsync(x => x.IdWarehouse == warehouseId && x.IdSalesPlanReference == ownerId, cancellationToken)
            || await db.StockBalances.AnyAsync(x => x.IdWarehouse == warehouseId && x.IdSalesPlanReference == ownerId, cancellationToken))
            return Conflict(new { detail = "اتصال آخر، مالک پیش‌فرض یا مالک دارای سند و مانده قابل حذف نیست." });
        db.WarehouseStockOwners.Remove(link);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "اتصال مالک حذف شد." });
    }

    [HttpPost("{section}/{id:long}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(string section, long id, CancellationToken cancellationToken)
    {
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
                Warehouse? warehouse = await db.Warehouses.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (warehouse is null) return NotFound();
                if (!await CanAccessWarehouseAsync(id, cancellationToken)) return Forbid();
                if (await IsSiteWarehouseAsync(id, cancellationToken))
                    return Conflict(new { detail = "ابتدا انبار پیش‌فرض سایت را به انبار دیگری منتقل کنید." });
                if (warehouse.IsSalesWarehouse &&
                    await db.Companies.AnyAsync(x => x.Id == warehouse.IdCompany && x.IsActive, cancellationToken))
                    return Conflict(new
                    {
                        detail =
                            "شرکت فعال باید یک انبار فروش داشته باشد؛ ابتدا انبار فروش دیگری برای همین شرکت تعیین کنید."
                    });
                warehouse.IsActive = false;
                break;
            case "document-types":
                WarehouseDocumentType? type =
                    await db.WarehouseDocumentTypes.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (type is null) return NotFound();
                type.IsActive = false;
                type.UpdatedAtUtc = DateTime.UtcNow;
                break;
            case "references":
                WarehouseParty? party =
                    await db.WarehouseParties.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
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
                    return Conflict(new
                    {
                        detail =
                            "این واحد هنوز در تعریف واحدهای کالا استفاده می‌شود؛ ابتدا آن اتصال‌ها را غیرفعال کنید."
                    });
                unit.IsActive = false;
                break;
            case "product-units":
                WarehouseProductUnit? productUnit =
                    await db.WarehouseProductUnits.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (productUnit is null) return NotFound();
                if (productUnit.IsBaseUnit)
                    return Conflict(new
                        { detail = "واحد اصلی کالا را غیرفعال نکنید؛ ابتدا واحد اصلی دیگری انتخاب کنید." });
                productUnit.IsActive = false;
                break;
            case "variants":
                ProductVariant? variant =
                    await db.ProductVariants.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (variant is null) return NotFound();
                variant.IsActive = false;
                variant.UpdatedAtUtc = DateTime.UtcNow;
                var inventoryItem =
                    await db.InventoryItems.SingleOrDefaultAsync(x => x.IdProductVariant == id, cancellationToken);
                if (inventoryItem is not null) inventoryItem.IsActive = false;
                break;
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
                if (!id.HasValue && !System.Text.RegularExpressions.Regex.IsMatch(input.Code.Trim(), "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
                    return ValidationProblem(detail: "کد باید با حروف کوچک انگلیسی، عدد و خط تیره باشد.", statusCode: 400);
                if (!id.HasValue && await db.SalesPlanReferences.AnyAsync(x => x.StoreSalesPlanKey == input.Code.Trim() || x.Code == input.Code.Trim(), cancellationToken))
                    return Conflict(new { detail = "این کد قبلاً ثبت شده است." });
                SalesPlanReference? entity = id.HasValue ? await db.SalesPlanReferences.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) : new();
                if (entity is null) return NotFound();
                if (id.HasValue && entity.IdStockOwnerType != input.IdStockOwnerType)
                    return Conflict(new { detail = "نوع مالک موجود قابل تغییر نیست." });
                if (id.HasValue && entity.Code != input.Code.Trim())
                    return Conflict(new { detail = "کد مالک موجود قابل تغییر نیست؛ مالک جدید تعریف کنید." });
                if (id.HasValue && !input.IsActive && (entity.IsRegular ||
                    await db.WarehouseStockOwners.AnyAsync(x => x.IdStockOwner == id.Value, cancellationToken)))
                    return Conflict(new { detail = "طرح عادی یا مالک متصل به انبار قابل غیرفعال‌سازی نیست." });
                if (!id.HasValue)
                {
                    entity.StoreSalesPlanKey = input.Code.Trim();
                    entity.Code = input.Code.Trim();
                    entity.IdStockOwnerType = input.IdStockOwnerType;
                    entity.PlanType = input.IdStockOwnerType == 1 ? input.Code.Trim() : "owner";
                }
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
                if (!id.HasValue && !User.IsInRole("system.full-access")) return Forbid();
                Company entity = id.HasValue
                    ? await db.Companies.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? null!
                    : new Company();
                if (id.HasValue && entity is null) return NotFound();
                if (id.HasValue && !User.IsInRole("system.full-access") &&
                    !await CanAccessCompanyAsync(id.Value, cancellationToken)) return Forbid();
                entity.Code = input.Code.Trim();
                entity.Name = input.Name.Trim();
                entity.IsActive = input.IsActive;
                if (!id.HasValue) db.Companies.Add(entity);
                await db.SaveChangesAsync(cancellationToken);
                return Ok(new { entity.Id, message = "شرکت ذخیره شد." });
            }
            case "warehouses":
            {
                WarehouseInput? input = Parse<WarehouseInput>(payload);
                if (input is null || input.CompanyId < 1 || !ValidCodeName(input.Code, input.Name, 50, 200))
                    return ValidationProblem(detail: "شرکت، کد و نام انبار را درست وارد کنید.", statusCode: 400);
                if (id.HasValue && !await CanAccessWarehouseAsync(id.Value, cancellationToken)) return Forbid();
                if (!await db.Companies.AnyAsync(x => x.Id == input.CompanyId && x.IsActive, cancellationToken))
                    return ValidationProblem(detail: "شرکت فعال پیدا نشد.", statusCode: 400);
                if (!User.IsInRole("system.full-access"))
                {
                    if (!await CanAccessCompanyAsync(input.CompanyId, cancellationToken)) return Forbid();
                    if (id.HasValue)
                    {
                        long existingCompanyId = await db.Warehouses.Where(x => x.Id == id.Value)
                            .Select(x => x.IdCompany).SingleAsync(cancellationToken);
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
                    return Conflict(new
                        { detail = "انبار متصل به سایت باید تا زمان جابه‌جایی اتصال، انبار فروش بماند." });
                if (input.IsSiteWarehouse && !User.IsInRole("system.full-access")) return Forbid();
                Warehouse entity = id.HasValue
                    ? await db.Warehouses.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? null!
                    : new Warehouse();
                if (id.HasValue && entity is null) return NotFound();
                if (id.HasValue && entity.IsSalesWarehouse && !input.IsSalesWarehouse)
                    return Conflict(new
                    {
                        detail =
                            "شرکت فعال باید یک انبار فروش داشته باشد؛ برای انتقال نقش، انبار فروش جایگزین را فعال کنید."
                    });
                Warehouse? previousSalesWarehouse = input.IsSalesWarehouse
                    ? await db.Warehouses.SingleOrDefaultAsync(
                        x => x.IdCompany == input.CompanyId && x.IsSalesWarehouse && (!id.HasValue || x.Id != id.Value),
                        cancellationToken)
                    : null;
                if (previousSalesWarehouse is not null &&
                    await IsSiteWarehouseAsync(previousSalesWarehouse.Id, cancellationToken))
                    return Conflict(new
                    {
                        detail = "انبار فروش فعلی به سایت متصل است؛ ابتدا اتصال سایت را به انبار فروش دیگری منتقل کنید."
                    });
                await using var transaction =
                    await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
                if (previousSalesWarehouse is not null)
                {
                    previousSalesWarehouse.IsSalesWarehouse = false;
                }

                entity.IdCompany = input.CompanyId;
                entity.Code = input.Code.Trim();
                entity.Name = input.Name.Trim();
                entity.IsSalesWarehouse = input.IsSalesWarehouse;
                entity.IdOwnerSalesPlanReference = input.OwnerSalesPlanId;
                entity.IsActive = input.IsActive;
                if (!id.HasValue) db.Warehouses.Add(entity);
                await db.SaveChangesAsync(cancellationToken);
                if (!await db.WarehouseStockOwners.AnyAsync(x => x.IdWarehouse == entity.Id && x.IdStockOwner == input.OwnerSalesPlanId, cancellationToken))
                {
                    db.WarehouseStockOwners.Add(new WarehouseStockOwner { IdWarehouse = entity.Id, IdStockOwner = input.OwnerSalesPlanId!.Value });
                    await db.SaveChangesAsync(cancellationToken);
                }
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
                    return Conflict(new
                    {
                        detail =
                            "جهت و اثر موجودی نوع سندی که سابقه دارد قابل تغییر نیست؛ آن را غیرفعال و نوع تازه تعریف کنید."
                    });
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
                WarehouseParty entity = id.HasValue
                    ? await db.WarehouseParties.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? null!
                    : new WarehouseParty();
                if (id.HasValue && entity is null) return NotFound();
                if (entity.IdWarehouse.HasValue &&
                    !await CanAccessWarehouseAsync(entity.IdWarehouse.Value, cancellationToken)) return Forbid();
                entity.Name = input.Name.Trim();
                entity.PartyType = input.PartyType;
                entity.ExternalPartyKey = string.IsNullOrWhiteSpace(input.ExternalPartyKey)
                    ? null
                    : input.ExternalPartyKey.Trim();
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
                if (input is null || !ValidCodeName(input.Code, input.Name, 50, 100))
                    return ValidationProblem(detail: "کد و نام واحد را درست وارد کنید.", statusCode: 400);
                WarehouseUnit entity = id.HasValue
                    ? await db.WarehouseUnits.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? null!
                    : new WarehouseUnit();
                if (id.HasValue && entity is null) return NotFound();
                entity.Code = input.Code.Trim();
                entity.Name = input.Name.Trim();
                entity.IsActive = input.IsActive;
                if (!id.HasValue) db.WarehouseUnits.Add(entity);
                await db.SaveChangesAsync(cancellationToken);
                return Ok(new { entity.Id, message = "واحد ذخیره شد." });
            }
            case "product-units":
            {
                ProductUnitInput? input = Parse<ProductUnitInput>(payload);
                if (input is null || input.ProductId < 1 || input.UnitId < 1 || input.ConversionFactor <= 0 ||
                    input.ConversionFactor > 999999999.999999999m)
                    return ValidationProblem(detail: "کالا، واحد و ضریب تبدیل در محدودهٔ مجاز الزامی است.", statusCode: 400);
                WarehouseProduct? product =
                    await db.Products.SingleOrDefaultAsync(x => x.Id == input.ProductId && x.IsWarehouseEnabled,
                        cancellationToken);
                if (product is null ||
                    !await db.WarehouseUnits.AnyAsync(x => x.Id == input.UnitId && x.IsActive, cancellationToken))
                    return ValidationProblem(detail: "کالا یا واحد فعال پیدا نشد.", statusCode: 400);
                if (input.IsBaseUnit && product.IdBaseUnit != input.UnitId)
                    return Conflict(new { detail = "تغییر واحد اصلی کالا بدون تبدیل مانده‌ها و سوابق مجاز نیست." });
                WarehouseProductUnit? entity = id.HasValue
                    ? await db.WarehouseProductUnits.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
                    : await db.WarehouseProductUnits.SingleOrDefaultAsync(
                        x => x.IdProduct == input.ProductId && x.IdUnit == input.UnitId, cancellationToken);
                if (id.HasValue && entity is null) return NotFound();
                if (entity is not null && entity.IdProduct != input.ProductId && entity.IsBaseUnit)
                    return Conflict(new { detail = "واحد اصلی کالا را نمی‌توان به کالای دیگری منتقل کرد." });
                if (entity?.IsBaseUnit == true && (!input.IsBaseUnit || !input.IsActive))
                    return Conflict(new
                        { detail = "واحد اصلی کالا باید فعال بماند؛ ابتدا واحد اصلی جایگزین را تعریف کنید." });
                if (!input.IsBaseUnit && !await db.WarehouseProductUnits.AnyAsync(
                        x => x.IdProduct == input.ProductId && x.IsBaseUnit && x.IsActive, cancellationToken))
                    return ValidationProblem(detail: "ابتدا واحد اصلی فعال کالا را تعریف کنید.", statusCode: 400);
                await using var transaction =
                    await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
                entity ??= new WarehouseProductUnit();
                if (input.IsBaseUnit)
                {
                    await db.WarehouseProductUnits.Where(x =>
                            x.IdProduct == input.ProductId && x.IsBaseUnit && (!id.HasValue || x.Id != id))
                        .ExecuteUpdateAsync(update => update.SetProperty(x => x.IsBaseUnit, false), cancellationToken);
                    product.IdBaseUnit = input.UnitId;
                    entity.ConversionFactor = 1m;
                }
                else
                {
                    if (entity.IsBaseUnit && entity.IdProduct == input.ProductId)
                        return Conflict(new
                        {
                            detail = "واحد اصلی کالا قابل تبدیل به واحد فرعی نیست؛ ابتدا واحد اصلی جدید را تعریف کنید."
                        });
                    entity.ConversionFactor =
                        decimal.Truncate(input.ConversionFactor * 1_000_000_000m) / 1_000_000_000m;
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
                if (input is null || input.ProductId < 1 || !ValidCodeName(input.Name, input.Name, 300, 300))
                    return ValidationProblem(detail: "محصول و نام تنوع را وارد کنید.", statusCode: 400);
                if (!string.IsNullOrWhiteSpace(input.Code) && input.Code.Length > 100)
                    return ValidationProblem(detail: "کد تنوع حداکثر ۱۰۰ نویسه است.", statusCode: 400);
                if (!string.IsNullOrWhiteSpace(input.AttributesJson))
                {
                    if (!TryReadInventoryAttributeKeys(input.AttributesJson, out HashSet<string> attributeKeys))
                        return ValidationProblem(detail: "ویژگی‌های موجودی باید آرایه‌ای از key، label، value و display معتبر باشند.", statusCode: 400);
                }

                WarehouseProduct? product =
                    await db.Products.SingleOrDefaultAsync(x => x.Id == input.ProductId && x.IsWarehouseEnabled,
                        cancellationToken);
                if (product is null) return ValidationProblem(detail: "محصول فعال پیدا نشد.", statusCode: 400);
                ProductVariant entity = id.HasValue
                    ? await db.ProductVariants.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? null!
                    : new ProductVariant();
                if (id.HasValue && entity is null) return NotFound();
                if (id.HasValue && entity.IdProduct != input.ProductId &&
                    await db.InventoryItems.AnyAsync(x => x.IdProductVariant == id))
                    return Conflict(new { detail = "تنوع دارای موجودی یا گردش است و به محصول دیگری منتقل نمی‌شود." });
                entity.IdProduct = product.Id;
                entity.Code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim();
                entity.Name = input.Name.Trim();
                entity.AttributesJson = string.IsNullOrWhiteSpace(input.AttributesJson) ? null : input.AttributesJson;
                entity.IsActive = input.IsActive;
                entity.UpdatedAtUtc = DateTime.UtcNow;
                await using var transaction =
                    await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
                if (!id.HasValue) db.ProductVariants.Add(entity);
                await db.SaveChangesAsync(cancellationToken);
                if (!id.HasValue &&
                    !await db.InventoryItems.AnyAsync(x => x.IdProduct == product.Id && x.IdProductVariant == entity.Id,
                        cancellationToken))
                {
                    db.InventoryItems.Add(new InventoryItem
                        { IdProduct = product.Id, IdProductVariant = entity.Id, IsActive = true });
                    await db.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                return Ok(new { entity.Id, message = "تنوع و قلم موجودی آن ذخیره شد." });
            }
            default:
                return NotFound();
        }
    }

    private async Task<IActionResult> GetWarehousesAsync(CancellationToken cancellationToken)
    {
        long? siteWarehouseId = await db.StoreSiteIdentities.AsNoTracking().Where(x => x.Id == 1)
            .Select(x => x.IdWarehouse).SingleOrDefaultAsync(cancellationToken);
        var query = from warehouse in db.Warehouses.AsNoTracking()
            join company in db.Companies.AsNoTracking() on warehouse.IdCompany equals company.Id
            join plan in db.SalesPlanReferences.AsNoTracking() on warehouse.IdOwnerSalesPlanReference equals plan.Id
                into plans
            from plan in plans.DefaultIfEmpty()
            orderby company.Name, warehouse.Name
            select new
            {
                warehouse.Id, warehouse.IdCompany, CompanyName = company.Name, warehouse.Code, warehouse.Name,
                warehouse.IsSalesWarehouse, warehouse.IdOwnerSalesPlanReference,
                OwnerSalesPlan = plan == null ? null : plan.DisplayName, warehouse.IsActive,
                IsSiteWarehouse = warehouse.Id == siteWarehouseId
            };
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out long parsed)
                ? parsed
                : 0;
            long[] allowed = await db.AdminUserWarehouseAccesses.AsNoTracking().Where(x => x.IdUser == userId)
                .Select(x => x.IdWarehouse).ToArrayAsync(cancellationToken);
            query = query.Where(x => allowed.Contains(x.Id));
        }

        return Ok(await query.ToListAsync(cancellationToken));
    }

    private async Task<IActionResult> GetCompaniesAsync(CancellationToken cancellationToken)
    {
        IQueryable<Company> query = db.Companies.AsNoTracking();
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out long parsed)
                ? parsed
                : 0;
            long[] companyIds = await (from access in db.AdminUserWarehouseAccesses.AsNoTracking()
                join warehouse in db.Warehouses.AsNoTracking() on access.IdWarehouse equals warehouse.Id
                where access.IdUser == userId
                select warehouse.IdCompany).Distinct().ToArrayAsync(cancellationToken);
            query = query.Where(x => companyIds.Contains(x.Id));
        }

        return Ok(await query.OrderBy(x => x.Name).Select(x => new { x.Id, x.Code, x.Name, x.IsActive })
            .ToListAsync(cancellationToken));
    }

    private async Task<IActionResult> GetReferencesAsync(CancellationToken cancellationToken)
    {
        IQueryable<WarehouseParty> query = db.WarehouseParties.AsNoTracking();
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out long parsed)
                ? parsed
                : 0;
            long[] allowed = await db.AdminUserWarehouseAccesses.AsNoTracking().Where(x => x.IdUser == userId)
                .Select(x => x.IdWarehouse).ToArrayAsync(cancellationToken);
            query = query.Where(x => x.IdWarehouse == null || allowed.Contains(x.IdWarehouse.Value));
        }

        return Ok(await query.OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.PartyType, x.ExternalPartyKey, x.IdWarehouse, x.IsActive })
            .ToListAsync(cancellationToken));
    }

    private async Task<IActionResult> GetSalesPlanMappingAsync(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        long? siteId = await db.StoreSiteIdentities.AsNoTracking().Where(x => x.Id == 1)
            .Select(x => x.IdWarehouse).SingleOrDefaultAsync(cancellationToken);
        var companies = await db.Companies.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken);
        var warehouses = await db.Warehouses.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Name).Select(x => new { x.Id, x.IdCompany, x.Name, x.Code, x.IdOwnerSalesPlanReference })
            .ToListAsync(cancellationToken);
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
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out long parsed)
                ? parsed
                : 0;
            long[] allowedCompanyIds = await (from access in db.AdminUserWarehouseAccesses.AsNoTracking()
                join warehouse in db.Warehouses.AsNoTracking() on access.IdWarehouse equals warehouse.Id
                where access.IdUser == userId
                select warehouse.IdCompany).Distinct().ToArrayAsync(cancellationToken);
            companyQuery = companyQuery.Where(x => allowedCompanyIds.Contains(x.Id));
        }

        var companies = await companyQuery.OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Code }).ToListAsync(cancellationToken);
        var warehouseQuery = from warehouse in db.Warehouses.AsNoTracking()
            join company in db.Companies.AsNoTracking() on warehouse.IdCompany equals company.Id
            orderby company.Name, warehouse.Name
            select new
            {
                warehouse.Id, warehouse.IdCompany, CompanyName = company.Name, warehouse.Code,
                warehouse.Name, warehouse.IsSalesWarehouse, warehouse.IdOwnerSalesPlanReference, warehouse.IsActive
            };
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out long parsed)
                ? parsed
                : 0;
            long[] allowed = await db.AdminUserWarehouseAccesses.AsNoTracking().Where(x => x.IdUser == userId)
                .Select(x => x.IdWarehouse).ToArrayAsync(cancellationToken);
            warehouseQuery = warehouseQuery.Where(x => allowed.Contains(x.Id));
        }

        var warehouses = await warehouseQuery.ToListAsync(cancellationToken);
        var plans = await db.SalesPlanReferences.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayName)
            .Select(x => new { x.Id, x.DisplayName, x.StoreSalesPlanKey }).ToListAsync(cancellationToken);
        var units = await db.WarehouseUnits.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Code, x.Name }).ToListAsync(cancellationToken);
        var products = await db.Products.AsNoTracking().Where(x => x.IsWarehouseEnabled).OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, StoreProductKey = x.ProductKey, x.IdBaseUnit })
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
        long userId =
            long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out long parsed)
                ? parsed
                : 0;
        return await db.AdminUserWarehouseAccesses.AsNoTracking()
            .AnyAsync(x => x.IdUser == userId && x.IdWarehouse == warehouseId, cancellationToken);
    }

    private async Task<bool> CanAccessCompanyAsync(long companyId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("system.full-access")) return true;
        long userId =
            long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out long parsed)
                ? parsed
                : 0;
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

    private static bool TryReadInventoryAttributeKeys(string json, out HashSet<string> keys)
    {
        keys = new(StringComparer.Ordinal);
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array ||
                document.RootElement.GetArrayLength() is < 1 or > 12) return false;
            foreach (JsonElement attribute in document.RootElement.EnumerateArray())
            {
                if (attribute.ValueKind != JsonValueKind.Object ||
                    !attribute.TryGetProperty("key", out JsonElement keyElement) ||
                    keyElement.ValueKind != JsonValueKind.String ||
                    !attribute.TryGetProperty("label", out JsonElement labelElement) ||
                    labelElement.ValueKind != JsonValueKind.String ||
                    !attribute.TryGetProperty("value", out JsonElement valueElement) ||
                    valueElement.ValueKind != JsonValueKind.String ||
                    !attribute.TryGetProperty("display", out JsonElement displayElement) ||
                    displayElement.ValueKind != JsonValueKind.String) return false;
                string key = keyElement.GetString()!.Trim();
                if (!System.Text.RegularExpressions.Regex.IsMatch(key, "^[a-z][a-z0-9-]{0,63}$") || !keys.Add(key) ||
                    string.IsNullOrWhiteSpace(labelElement.GetString()) || labelElement.GetString()!.Length > 80 ||
                    string.IsNullOrWhiteSpace(valueElement.GetString()) || valueElement.GetString()!.Length > 100 ||
                    string.IsNullOrWhiteSpace(displayElement.GetString()) ||
                    displayElement.GetString()!.Length > 100) return false;
                if (attribute.TryGetProperty("swatch", out JsonElement swatch) &&
                    swatch.ValueKind != JsonValueKind.Null &&
                    (swatch.ValueKind != JsonValueKind.String ||
                     !System.Text.RegularExpressions.Regex.IsMatch(swatch.GetString() ?? "", "^#[0-9a-fA-F]{6}$")))
                    return false;
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed record StockOwnerInput(string Code, string DisplayName, byte IdStockOwnerType, bool IsActive);

    private sealed record CompanyInput(string Code, string Name, bool IsActive);

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

    private sealed record UnitInput(string Code, string Name, bool IsActive);

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
        string? AttributesJson,
        bool IsActive);

    public sealed record SalesPlanMappingInput(long CompanyId, long WarehouseId, long SalesPlanId);
}
