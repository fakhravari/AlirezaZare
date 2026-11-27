using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Domain.Warehouse.Entities;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;
using WarehouseEntity = Shop.Data.Domain.Warehouse.Entities.Warehouse;
using Shop.Data.Application.Warehouse.Contracts;
using Shop.Data.Application.Admin.Contracts;
using static Shop.Data.Application.Admin.Contracts.AdminOperationResult;

namespace Shop.Data.Infrastructure.Warehouse;

public sealed class WarehouseDefinitionsAdminData(ShopDbContext db, System.Security.Claims.ClaimsPrincipal User) : IWarehouseDefinitionsAdminData
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AdminOperationResult> Get(string section, CancellationToken cancellationToken) => section switch
    {
        "companies" => await GetCompaniesAsync(cancellationToken),
        "warehouses" => await GetWarehousesAsync(cancellationToken),
        "voucher-types" => Success(await db.VoucherTypes.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new
                { x.Id, x.Name, x.Direction, x.AffectsStock, x.IsReturn, x.IsSystemSalesDispatch, x.IsActive })
            .ToListAsync(cancellationToken)),
        "references" => await GetReferencesAsync(cancellationToken),
        "unit-groups" => Success(await db.WarehouseUnits.AsNoTracking().Where(x => x.IdParent == null)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.NameFa)
            .Select(x => new
            {
                x.Id, Name = x.NameFa, x.NameEn, x.IdParent, x.TreePathIds, x.TreePathNames, x.IsActive, x.SortOrder,
                CanDelete = !db.WarehouseUnits.Any(child => child.IdParent == x.Id)
            }).ToListAsync(cancellationToken)),
        "units" => Success(await db.WarehouseUnits.AsNoTracking().Where(x => x.IdParent != null).OrderBy(x => x.NameFa)
            .Select(x => new
            {
                x.Id, x.NameFa, x.NameEn, x.IdParent, x.TreePathIds, x.TreePathNames,
                ParentName = db.WarehouseUnits.Where(parent => parent.Id == x.IdParent).Select(parent => parent.NameFa)
                    .FirstOrDefault(),
                x.IsActive,
                CanDelete = !db.WarehouseUnits.Any(child => child.IdParent == x.Id) &&
                            !db.WarehouseProductUnits.Any(link => link.IdUnit == x.Id) &&
                            !db.VoucherLines.Any(line => line.IdUnit == x.Id)
            })
            .ToListAsync(cancellationToken)),
        "product-units" => Success(await db.WarehouseProductUnits
            .AsNoTracking()
            .Join(
                db.Products.AsNoTracking(),
                link => link.IdProduct,
                product => product.Id,
                (link, product) => new { link, product })
            .Join(
                db.WarehouseUnits.AsNoTracking(),
                linkProduct => linkProduct.link.IdUnit,
                unit => unit.Id,
                (linkProduct, unit) => new { linkProduct.link, linkProduct.product, unit })
            .Where(linkDetails => !linkDetails.link.IsActive)
            .OrderBy(linkDetails => linkDetails.product.NameFa)
            .ThenByDescending(linkDetails => linkDetails.link.IsBaseUnit)
            .ThenBy(linkDetails => linkDetails.unit.NameFa)
            .Select(linkDetails => new
            {
                linkDetails.link.Id,
                linkDetails.link.IdProduct,
                ProductName = linkDetails.product.NameFa,
                linkDetails.link.IdUnit,
                UnitName = linkDetails.unit.NameFa,
                linkDetails.link.ConversionFactor,
                linkDetails.link.IsBaseUnit,
                linkDetails.link.IsActive
            })
            .ToListAsync(cancellationToken)),
        "variants" => await GetVariantsAsync(cancellationToken),
        "lookups" => await GetLookupsAsync(cancellationToken),
        "stock-owners" => await GetStockOwnersAsync(cancellationToken),
        "sales-plan-mapping" => await GetSalesPlanMappingAsync(cancellationToken),
        _ => Missing()
    };

    private async Task<AdminOperationResult> GetVariantsAsync(CancellationToken cancellationToken)
    {
        if (!CanAccessVariants("variants")) return Denied();
        var variants = await db.ProductVariants
            .AsNoTracking()
            .Join(
                db.Products.AsNoTracking(),
                variant => variant.IdProduct,
                product => product.Id,
                (variant, product) => new { variant, product })
            .OrderByDescending(variantProduct => variantProduct.variant.CreatedAtUtc)
            .ThenByDescending(variantProduct => variantProduct.variant.Id)
            .Select(variantProduct => new
                {
                    variantProduct.variant.Id,
                    variantProduct.variant.IdProduct,
                    ProductName = variantProduct.product.NameFa,
                    ProductCode = variantProduct.product.ProductKey,
                    variantProduct.variant.IdVariantName,
                    Name = variantProduct.variant.VariantName.Name,
                    variantProduct.variant.IsDefault,
                    variantProduct.variant.IsActive,
                    variantProduct.variant.CreatedAtUtc,
                    HasNonZeroStock = db.InventoryItems.Any(item => item.IdVariant == variantProduct.variant.Id &&
                                                                    db.StockBalances.Any(balance =>
                                                                        balance.IdInventoryItem == item.Id &&
                                                                        balance.Quantity != 0)),
                    HasWarehouseHistory = db.InventoryItems.Any(item => item.IdVariant == variantProduct.variant.Id &&
                                                                        (db.VoucherLines.Any(line =>
                                                                             line.IdInventoryItem == item.Id) ||
                                                                         db.WarehouseStockMovements.Any(movement =>
                                                                             movement.IdInventoryItem == item.Id))),
                    HasStoreHistory = db.StorePriceVersions.Any(price => price.IdVariant == variantProduct.variant.Id) ||
                                      db.StoreProductVariantSalesPlanOffers.Any(offer =>
                                          offer.IdVariant == variantProduct.variant.Id) ||
                                      db.AccountCartItems.Any(cart =>
                                          cart.IdVariant == variantProduct.variant.Id) ||
                                      db.StoreCustomerOrderItems.Any(order => order.IdVariant == variantProduct.variant.Id),
                    HasOtherActiveVariant = db.ProductVariants.Any(other =>
                        other.IdProduct == variantProduct.variant.IdProduct && other.Id != variantProduct.variant.Id && other.IsActive)
                })
            .ToListAsync(cancellationToken);
        long[] ids = variants.Select(x => x.Id).ToArray();
        var attributes = await db.StoreVariantCatalogFeatureValues
            .AsNoTracking()
            .Join(
                db.StoreCatalogFeatures.AsNoTracking(),
                link => link.IdFeatureValue,
                value => value.Id,
                (link, value) => new { link, value })
            .Join(
                db.StoreCatalogFeatures.AsNoTracking(),
                linkValue => linkValue.link.IdFeature,
                attribute => attribute.Id,
                (linkValue, attribute) => new { linkValue.link, linkValue.value, attribute })
            .Where(linkValueAttribute => ids.Contains(linkValueAttribute.link.IdProductVariant))
            .OrderBy(linkValueAttribute => linkValueAttribute.attribute.Id)
            .Select(linkValueAttribute => new
                {
                    linkValueAttribute.link.IdProductVariant,
                    linkValueAttribute.attribute.Id,
                    AttributeLabel = linkValueAttribute.attribute.Name,
                    ValueId = linkValueAttribute.value.Id,
                    DisplayValue = linkValueAttribute.value.Name
                })
            .ToListAsync(cancellationToken);
        var byVariant = attributes.GroupBy(x => x.IdProductVariant).ToDictionary(x => x.Key,
            x => x.Select(a => new
                    { attributeId = a.Id, valueId = a.ValueId, label = a.AttributeLabel, display = a.DisplayValue })
                .ToArray());
        return Success(variants.Select(x => new
        {
            x.Id, x.IdProduct, x.ProductName, x.ProductCode, x.IdVariantName, x.Name, x.IsDefault, x.IsActive,
            x.CreatedAtUtc,
            CanDelete = !x.HasNonZeroStock && !x.HasWarehouseHistory && !x.HasStoreHistory && !x.IsDefault &&
                        (!x.IsActive || x.HasOtherActiveVariant),
            CanDeactivate = x.IsActive && !x.IsDefault && x.HasOtherActiveVariant && !x.HasNonZeroStock,
            DeactivateBlockReason = !x.IsActive ? null
                : x.IsDefault ? "تنوع پیش‌فرض است؛ ابتدا یک تنوع فعال دیگر را پیش‌فرض کنید."
                : !x.HasOtherActiveVariant ? "آخرین تنوع فعال این کالا است؛ دست‌کم یک تنوع باید فعال بماند."
                : x.HasNonZeroStock ? "این تنوع موجودی غیرصفر دارد؛ ابتدا موجودی آن را از گردش انبار صفر کنید."
                : null,
            DeleteBlockReason = x.IsDefault ? "تنوع پیش‌فرض را حذف نکنید؛ ابتدا تنوع دیگری را پیش‌فرض کنید."
                : x.IsActive && !x.HasOtherActiveVariant ? "آخرین تنوع فعال کالا قابل حذف نیست."
                : x.HasNonZeroStock ? "تنوع موجودی غیرصفر دارد؛ ابتدا موجودی را از گردش انبار صفر کنید."
                : x.HasWarehouseHistory ? "تنوع سابقهٔ سند یا گردش انبار دارد و قابل حذف نیست."
                : x.HasStoreHistory ? "تنوع در قیمت، پیشنهاد فروش، سبد یا سفارش استفاده شده و قابل حذف نیست."
                : null,
            Attributes = byVariant.GetValueOrDefault(x.Id) ?? []
        }));
    }

    public async Task<AdminOperationResult> GetVariantNames(CancellationToken cancellationToken)
    {
        if (!CanAccessVariantNames() && !CanAccessVariants("variants")) return Denied();
        var nodes = await db.VariantNames.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.IdParent, x.Name, x.TreePathIds, x.TreePathNames, x.IsActive })
            .ToListAsync(cancellationToken);
        long[] referencedNames = (await db.ProductVariants
            .AsNoTracking()
            .Select(variant => variant.IdVariantName)
            .Distinct()
            .ToListAsync(cancellationToken))
            .ToArray();
        long[] activeVariantNames = (await db.ProductVariants
            .AsNoTracking()
            .Where(variant => variant.IsActive)
            .Select(variant => variant.IdVariantName)
            .Distinct()
            .ToListAsync(cancellationToken))
            .ToArray();
        return Success(nodes.Select(node => new
        {
            node.Id, node.IdParent, node.Name, node.TreePathIds, node.TreePathNames, node.IsActive,
            CanDelete = !nodes.Any(child => child.IdParent == node.Id) &&
                        !referencedNames.Contains(node.Id),
            CanDeactivate = node.IsActive && !nodes.Any(child => child.IdParent == node.Id && child.IsActive) &&
                            !activeVariantNames.Contains(node.Id),
            DeleteBlockReason = nodes.Any(child => child.IdParent == node.Id)
                ? "این نام گروه والد است و ابتدا باید فرزندهایش حذف شوند."
                : referencedNames.Contains(node.Id)
                    ? "این نام به تنوع کالا متصل است و قابل حذف نیست."
                    : null,
            DeactivateBlockReason = nodes.Any(child => child.IdParent == node.Id && child.IsActive)
                ? "ابتدا فرزندهای فعال این گروه را غیرفعال کنید."
                : activeVariantNames.Contains(node.Id)
                    ? "ابتدا تنوع‌های فعال متصل به این نام را غیرفعال کنید."
                    : null
        }).ToArray());
    }

    public Task<AdminOperationResult> CreateVariantName(VariantNameInput input, CancellationToken cancellationToken) =>
        SaveVariantName(null, input, cancellationToken);

    public Task<AdminOperationResult> UpdateVariantName(long id, VariantNameInput input,
        CancellationToken cancellationToken) =>
        SaveVariantName(id, input, cancellationToken);

    public async Task<AdminOperationResult> ActivateVariantName(long id, CancellationToken cancellationToken)
    {
        if (!CanAccessVariantNames()) return Denied();
        if (!User.IsInRole("system.full-access") &&
            !User.HasClaim("admin_permission", "Warehouse:DefinitionsApi:Save")) return Denied();
        var node = await db.VariantNames.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (node is null) return Missing();
        if (node.IdParent.HasValue &&
            !await db.VariantNames.AnyAsync(x => x.Id == node.IdParent && x.IsActive && x.IdParent == null,
                cancellationToken))
            return Conflict(new { detail = "برای فعال‌سازی فرزند، ابتدا گروه ریشهٔ آن را فعال کنید." });
        node.IsActive = true;
        await db.SaveChangesAsync(cancellationToken);
        return Success(new { message = "نام تنوع فعال شد." });
    }

    public async Task<AdminOperationResult> DeleteUnusedVariantName(long id, CancellationToken cancellationToken)
    {
        if (!CanAccessVariantNames()) return Denied();
        if (!User.IsInRole("system.full-access")) return Denied();
        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var node = await db.VariantNames.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (node is null) return Missing();
        if (await db.VariantNames.AnyAsync(x => x.IdParent == id, cancellationToken) ||
            await db.ProductVariants.AnyAsync(x => x.IdVariantName == id, cancellationToken))
            return Conflict(new
                { detail = "نامی که فرزند یا اتصال تنوع کالا دارد قابل حذف نیست؛ در صورت نیاز آن را غیرفعال کنید." });
        db.VariantNames.Remove(node);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Success(new { message = "نام تنوع استفاده‌نشده حذف شد." });
    }

    private async Task<AdminOperationResult> SaveVariantName(long? id, VariantNameInput input,
        CancellationToken cancellationToken)
    {
        if (!CanAccessVariantNames()) return Denied();
        string name = input.Name?.Trim() ?? "";
        if (name.Length is < 1 or > 300 || input.IdParent == id && id.HasValue)
            return Validation("نام و والد مرجع تنوع معتبر نیست.");
        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        VariantName? node = id.HasValue
            ? await db.VariantNames.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            : new VariantName { IsActive = true };
        if (node is null) return Missing();
        var parents = await db.VariantNames.AsNoTracking().Select(x => new { x.Id, x.IdParent, x.IsActive })
            .ToListAsync(cancellationToken);
        long? parentId = input.IdParent;
        var visited = new HashSet<long>();
        while (parentId.HasValue)
        {
            if (parentId == id || !visited.Add(parentId.Value))
                return Conflict(new { detail = "والد انتخاب‌شده باعث چرخه در درخت می‌شود." });
            var parent = parents.Find(x => x.Id == parentId.Value);
            if (parent is null || !parent.IsActive)
                return Validation("والد انتخاب‌شده فعال نیست.");
            if (parent.IdParent.HasValue)
                return Validation("والد باید یک گروه ریشه باشد؛ درخت تنوع محصول فقط دو سطح دارد.");
            parentId = parent.IdParent;
        }

        if (input.IdParent.HasValue && id.HasValue &&
            await db.VariantNames.AnyAsync(x => x.IdParent == id, cancellationToken))
            return Validation("گروهی که فرزند دارد نمی‌تواند خودش فرزند گروه دیگری شود.");
        if (await db.VariantNames.AnyAsync(x => x.Id != (id ?? 0) && x.IdParent == input.IdParent && x.Name == name,
                cancellationToken))
            return Conflict(new { detail = "این نام در شاخهٔ انتخاب‌شده وجود دارد." });
        node.Name = name;
        node.IdParent = input.IdParent;
        if (!id.HasValue) db.VariantNames.Add(node);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Success(new { node.Id, message = "مرجع تنوع ذخیره شد." });
    }

    public async Task<AdminOperationResult> GetVariantOptions(long productId, long? variantId,
        CancellationToken cancellationToken)
    {
        if (!CanAccessVariants("variants")) return Denied();
        if (!await db.Products.AnyAsync(x => x.Id == productId && x.IsWarehouseEnabled, cancellationToken))
            return Missing();
        if (variantId.HasValue &&
            !await db.ProductVariants.AnyAsync(x => x.Id == variantId && x.IdProduct == productId, cancellationToken))
            return Missing();
        var categoryId = await db.Products.Where(x => x.Id == productId).Select(x => x.IdCategory)
            .SingleAsync(cancellationToken);
        var categoryIds = new List<long>();
        var categories = await db.StoreCategories.AsNoTracking().Select(x => new { x.Id, x.IdParentCategory })
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        while (categoryId is long current && categories.TryGetValue(current, out var category) &&
               !categoryIds.Contains(current))
        {
            categoryIds.Add(current);
            categoryId = category.IdParentCategory;
        }

        var rows = await db.StoreCatalogFeatures
            .AsNoTracking()
            .Where(x => x.IdParent == null)
            .Join(
                db.StoreCatalogFeatures.AsNoTracking().Where(x => x.IdParent != null),
                attribute => (long?)attribute.Id,
                value => value.IdParent,
                (attribute, value) => new { attribute, value })
            .Where(attributeValue =>
                attributeValue.attribute.Kind == StoreCatalogFeatureKinds.PriceVariant &&
                attributeValue.attribute.IsActive && attributeValue.value.IsActive &&
                db.StoreCategoryCatalogFeatures.Any(link =>
                    categoryIds.Contains(link.IdCategory) && link.IdFeature == attributeValue.attribute.Id))
            .OrderBy(attributeValue => attributeValue.attribute.Name)
            .ThenBy(attributeValue => attributeValue.value.Name)
            .Select(attributeValue => new
            {
                attributeValue.attribute.Id,
                Label = attributeValue.attribute.Name,
                attributeValue.attribute.TreePathIds,
                attributeValue.attribute.TreePathNames,
                ValueId = attributeValue.value.Id,
                ValueLabel = attributeValue.value.Name,
                ValueTreePathIds = attributeValue.value.TreePathIds,
                ValueTreePathNames = attributeValue.value.TreePathNames
            })
            .ToListAsync(cancellationToken);
        return Success(rows.GroupBy(x => new { x.Id, x.Label }).Select(group => new
        {
            id = group.Key.Id, label = group.Key.Label,
            treePathIds = group.First().TreePathIds,
            treePathNames = group.First().TreePathNames,
            values = group.Select(x => new
            {
                id = x.ValueId, label = x.ValueLabel,
                treePathIds = x.ValueTreePathIds, treePathNames = x.ValueTreePathNames
            }).ToArray()
        }).ToArray());
    }

    public async Task<AdminOperationResult> Create(string section, JsonElement payload,
        CancellationToken cancellationToken) =>
        !CanAccessVariants(section) ? Denied() : await SaveCoreAsync(section, null, payload, cancellationToken);

    public async Task<AdminOperationResult> Save(string section, long id, JsonElement payload,
        CancellationToken cancellationToken) =>
        !CanAccessVariants(section) ? Denied() : await SaveCoreAsync(section, id, payload, cancellationToken);

    public async Task<AdminOperationResult> DeleteUnusedUnitDefinition(string section, long id,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();
        if (section is not ("units" or "unit-groups")) return Missing();

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        WarehouseUnit? unit = await db.WarehouseUnits.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (unit is null || (section == "units") != unit.IdParent.HasValue) return Missing();

        if (section == "units")
        {
            bool inUse = await db.WarehouseUnits.AnyAsync(x => x.IdParent == id, cancellationToken) ||
                         await db.WarehouseProductUnits.AnyAsync(x => x.IdUnit == id, cancellationToken) ||
                         await db.VoucherLines.AnyAsync(x => x.IdUnit == id, cancellationToken);
            if (inUse)
                return Conflict(new
                {
                    detail =
                        "واحدی که زیرواحد، اتصال کالا یا سابقهٔ سند انبار دارد قابل حذف نیست؛ در صورت نیاز آن را غیرفعال کنید."
                });
        }
        else if (await db.WarehouseUnits.AnyAsync(x => x.IdParent == id, cancellationToken))
            return Conflict(new { detail = "گروه دارای واحد یا زیرگروه است و قابل حذف نیست." });

        db.WarehouseUnits.Remove(unit);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Success(new { message = section == "units" ? "واحد حذف شد." : "گروه واحد حذف شد." });
    }

    public async Task<AdminOperationResult> DeleteUnusedVariant(long id, CancellationToken cancellationToken)
    {
        if (!CanAccessVariants("variants")) return Denied();
        if (!User.IsInRole("system.full-access")) return Denied();

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var variant = await db.ProductVariants.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (variant is null) return Missing();

        bool hasOtherActiveVariant =
            await db.ProductVariants.AnyAsync(x => x.IdProduct == variant.IdProduct && x.Id != id && x.IsActive,
                cancellationToken);
        bool hasNonZeroStock = await db.InventoryItems
            .Where(item => item.IdVariant == id)
            .AnyAsync(item => db.StockBalances.Any(balance =>
                balance.IdInventoryItem == item.Id && balance.Quantity != 0), cancellationToken);
        bool hasWarehouseHistory = await db.InventoryItems
            .Where(item => item.IdVariant == id)
            .AnyAsync(item =>
                db.VoucherLines.Any(line => line.IdInventoryItem == item.Id) ||
                db.WarehouseStockMovements.Any(movement => movement.IdInventoryItem == item.Id), cancellationToken);
        bool hasStoreHistory = await db.StorePriceVersions.AnyAsync(x => x.IdVariant == id, cancellationToken) ||
                               await db.StoreProductVariantSalesPlanOffers.AnyAsync(x => x.IdVariant == id,
                                   cancellationToken) ||
                               await db.AccountCartItems.AnyAsync(x => x.IdVariant == id, cancellationToken) ||
                               await db.StoreCustomerOrderItems.AnyAsync(x => x.IdVariant == id,
                                   cancellationToken);

        if (variant.IsDefault || (variant.IsActive && !hasOtherActiveVariant) || hasNonZeroStock ||
            hasWarehouseHistory || hasStoreHistory)
            return Conflict(new
            {
                detail = variant.IsDefault ? "تنوع پیش‌فرض را حذف نکنید؛ ابتدا تنوع دیگری را پیش‌فرض کنید."
                    : variant.IsActive && !hasOtherActiveVariant ? "آخرین تنوع فعال کالا قابل حذف نیست."
                    : hasNonZeroStock ? "تنوع موجودی غیرصفر دارد؛ ابتدا موجودی را از گردش انبار صفر کنید."
                    : hasWarehouseHistory ? "تنوع سابقهٔ سند یا گردش انبار دارد و قابل حذف نیست."
                    : "تنوع در قیمت، پیشنهاد فروش، سبد یا سفارش استفاده شده و قابل حذف نیست."
            });

        var inventoryItems =
            await db.InventoryItems.Where(x => x.IdVariant == id).ToListAsync(cancellationToken);
        if (inventoryItems.Count > 0)
        {
            var inventoryItemIds = inventoryItems.Select(x => x.Id).ToArray();
            var zeroBalances = await db.StockBalances
                .Where(x => inventoryItemIds.Contains(x.IdInventoryItem) && x.Quantity == 0)
                .ToListAsync(cancellationToken);
            db.StockBalances.RemoveRange(zeroBalances);
            db.InventoryItems.RemoveRange(inventoryItems);
        }

        var catalogValues = await db.StoreVariantCatalogFeatureValues.Where(x => x.IdProductVariant == id)
            .ToListAsync(cancellationToken);
        db.StoreVariantCatalogFeatureValues.RemoveRange(catalogValues);
        db.ProductVariants.Remove(variant);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Success(new { message = "تنوع استفاده‌نشده حذف شد." });
    }

    public async Task<AdminOperationResult> SaveMapping(SalesPlanMappingInput input,
        CancellationToken cancellationToken)
    {
        if (input.CompanyId < 1 || input.WarehouseId < 1 || input.SalesPlanId < 1)
            return Validation("شرکت، انبار فروش و طرح سایت را انتخاب کنید.");

        if (!User.IsInRole("system.full-access")) return Denied();

        WarehouseEntity? warehouse = await db.Warehouses.SingleOrDefaultAsync(x => x.Id == input.WarehouseId
            && x.IdCompany == input.CompanyId &&
            x.Company.IsActive &&
            x.IsActive, cancellationToken);
        if (warehouse is null)
            return Validation("انبار انتخاب‌شده، انبار فروش فعال شرکت نیست.");

        if (!await db.SalesPlanReferences.AnyAsync(x => x.Id == input.SalesPlanId && x.IsActive, cancellationToken))
            return Validation("طرح فروش انتخاب‌شده فعال نیست.");

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var links = await db.WarehouseStockOwners.Where(x => x.IdWarehouse == input.WarehouseId)
            .ToListAsync(cancellationToken);
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

        return Success(new { message = "مالک به انبار اختصاص یافت." });
    }

    public async Task<AdminOperationResult> RemoveMapping(long warehouseId, long ownerId, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var link = await db.WarehouseStockOwners.SingleOrDefaultAsync(
            x => x.IdWarehouse == warehouseId && x.IdStockOwner == ownerId, cancellationToken);
        if (link is null) return Missing();

        if (await db.WarehouseStockOwners.CountAsync(x => x.IdWarehouse == warehouseId, cancellationToken) <= 1
            || link.IsDefault
            || await db.Vouchers.AnyAsync(x => x.IdWarehouse == warehouseId && x.IdSalesPlanReference == ownerId,
                cancellationToken)
            || await db.VoucherLines.AnyAsync(x => x.IdSalesPlanReference == ownerId
                                                   && db.Vouchers.Any(document =>
                                                       document.Id == x.IdVoucher &&
                                                       document.IdWarehouse == warehouseId), cancellationToken)
            || await db.WarehouseStockMovements.AnyAsync(
                x => x.IdWarehouse == warehouseId && x.IdSalesPlanReference == ownerId, cancellationToken))
            return Conflict(new
                { detail = "آخرین اتصال، مالک پیش‌فرض یا مالکی که سند یا گردش انبار دارد قابل حذف نیست." });

        var balances = await db.StockBalances
            .Where(x => x.IdWarehouse == warehouseId && x.IdSalesPlanReference == ownerId)
            .ToListAsync(cancellationToken);
        if (balances.Any(x => x.Quantity != 0))
            return Conflict(new { detail = "مالک دارای ماندهٔ غیرصفر است و اتصال آن قابل حذف نیست." });

        db.StockBalances.RemoveRange(balances);

        db.WarehouseStockOwners.Remove(link);
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Success(new { message = "اتصال مالک حذف شد." });
    }

    public async Task<AdminOperationResult> SetDefaultMapping(long warehouseId, long ownerId,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();
        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var target =
            await db.WarehouseStockOwners.SingleOrDefaultAsync(
                x => x.IdWarehouse == warehouseId && x.IdStockOwner == ownerId, cancellationToken);
        if (target is null) return Missing();
        await db.WarehouseStockOwners.Where(x => x.IdWarehouse == warehouseId && x.IsDefault)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.IsDefault, false), cancellationToken);
        await db.Entry(target).ReloadAsync(cancellationToken);
        target.IsDefault = true;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Success(new { message = "مالک پیش‌فرض انبار تغییر کرد." });
    }

    public async Task<AdminOperationResult> Deactivate(string section, long id, CancellationToken cancellationToken)
    {
        if (!CanAccessVariants(section)) return Denied();
        switch (section)
        {
            case "companies":
                Company? company = await db.Companies.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (company is null) return Missing();
                if (!User.IsInRole("system.full-access")) return Denied();
                if (await db.Warehouses.AnyAsync(x => x.IdCompany == id && x.IsActive, cancellationToken))
                    return Conflict(new { detail = "ابتدا انبارهای فعال شرکت را غیرفعال کنید." });
                company.IsActive = false;
                break;
            case "warehouses":
                WarehouseEntity? warehouse =
                    await db.Warehouses.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (warehouse is null) return Missing();
                if (!await CanAccessWarehouseAsync(id, cancellationToken)) return Denied();
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
            case "voucher-types":
                VoucherType? type = await db.VoucherTypes.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (type is null) return Missing();
                type.IsActive = false;
                type.UpdatedAtUtc = DateTime.UtcNow;
                break;
            case "references":
                WarehouseParty? party =
                    await db.WarehouseParties.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (party is null) return Missing();
                if (party.IdWarehouse.HasValue &&
                    !await CanAccessWarehouseAsync(party.IdWarehouse.Value, cancellationToken)) return Denied();
                party.IsActive = false;
                party.UpdatedAtUtc = DateTime.UtcNow;
                break;
            case "units":
                WarehouseUnit? unit = await db.WarehouseUnits.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (unit is null) return Missing();
                if (await db.WarehouseProductUnits.AnyAsync(x => x.IdUnit == id && x.IsActive, cancellationToken))
                    return Conflict(new
                    {
                        detail =
                            "این واحد هنوز در تعریف واحدهای کالا استفاده می‌شود؛ ابتدا آن اتصال‌ها را غیرفعال کنید."
                    });
                unit.IsActive = false;
                break;
            case "unit-groups":
                WarehouseUnit? group =
                    await db.WarehouseUnits.SingleOrDefaultAsync(x => x.Id == id && x.IdParent == null,
                        cancellationToken);
                if (group is null) return Missing();
                if (await db.WarehouseUnits.AnyAsync(x => x.IdParent == id && x.IsActive, cancellationToken))
                    return Conflict(new
                    {
                        detail = "برای غیرفعال‌کردن گروه، ابتدا واحدهای فعال آن را به گروه دیگری منتقل یا غیرفعال کنید."
                    });
                group.IsActive = false;
                break;
            case "product-units":
                WarehouseProductUnit? productUnit =
                    await db.WarehouseProductUnits.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                if (productUnit is null) return Missing();
                if (productUnit.IsBaseUnit)
                    return Conflict(new
                        { detail = "واحد اصلی کالا را غیرفعال نکنید؛ ابتدا واحد اصلی دیگری انتخاب کنید." });
                productUnit.IsActive = false;
                break;
            case "variant-names":
                if (!CanAccessVariantNames()) return Denied();
                if (!User.IsInRole("system.full-access") &&
                    !User.HasClaim("admin_permission", "Warehouse:DefinitionsApi:Deactivate")) return Denied();
                await using (var transaction =
                             await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,
                                 cancellationToken))
                {
                    var node = await db.VariantNames.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                    if (node is null) return Missing();
                    if (await db.VariantNames.AnyAsync(x => x.IdParent == id && x.IsActive, cancellationToken))
                        return Conflict(new { detail = "ابتدا فرزندهای فعال این گروه را غیرفعال کنید." });
                    if (await db.ProductVariants.AnyAsync(x => x.IdVariantName == id && x.IsActive, cancellationToken))
                        return Conflict(new { detail = "ابتدا تنوع‌های فعال متصل به این نام را غیرفعال کنید." });
                    node.IsActive = false;
                    await db.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return Success(new { message = "نام تنوع غیرفعال شد؛ سابقه حفظ شده است." });
                }
            case "variants":
                await using (var transaction =
                             await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,
                                 cancellationToken))
                {
                    if (!User.IsInRole("system.full-access") &&
                        !User.HasClaim("admin_permission", "Warehouse:DefinitionsApi:Deactivate")) return Denied();
                    ProductVariant? variant =
                        await db.ProductVariants.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
                    if (variant is null) return Missing();
                    if (variant.IsActive && !await db.ProductVariants.AnyAsync(
                            x => x.IdProduct == variant.IdProduct && x.Id != variant.Id && x.IsActive,
                            cancellationToken))
                        return Conflict(new
                        {
                            detail =
                                "کالای دارای تنوع باید حداقل یک تنوع فعال داشته باشد؛ آخرین تنوع فعال را نمی‌توان غیرفعال کرد."
                        });
                    if (variant.IsDefault)
                        return Conflict(new
                            { detail = "تنوع پیش‌فرض قابل غیرفعال‌سازی نیست؛ ابتدا تنوع فعال دیگری را پیش‌فرض کنید." });
                    bool hasStock = await db.InventoryItems
                        .Where(item => item.IdVariant == id)
                        .AnyAsync(item => db.StockBalances.Any(balance =>
                            balance.IdInventoryItem == item.Id && balance.Quantity != 0), cancellationToken);
                    if (hasStock)
                        return Conflict(new
                        {
                            detail =
                                "این تنوع موجودی دارد؛ ابتدا از بخش گردش انبار موجودی آن را صفر کنید، سپس دوباره برای غیرفعال‌سازی اقدام کنید."
                        });
                    variant.IsActive = false;
                    variant.UpdatedAtUtc = DateTime.UtcNow;
                    var inventoryItem =
                        await db.InventoryItems.SingleOrDefaultAsync(x => x.IdVariant == id, cancellationToken);
                    if (inventoryItem is not null) inventoryItem.IsActive = false;
                    await db.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return Success(new { message = "رکورد غیرفعال شد و سابقه حفظ شد." });
                }
            default:
                return Missing();
        }

        await db.SaveChangesAsync(cancellationToken);

        return Success(new { message = "رکورد غیرفعال شد و سابقه حفظ شد." });
    }

    public async Task<AdminOperationResult> ActivateVariant(long id, CancellationToken cancellationToken)
    {
        if (!CanAccessVariants("variants")) return Denied();
        if (!User.IsInRole("system.full-access") &&
            !User.HasClaim("admin_permission", "Warehouse:DefinitionsApi:Save")) return Denied();
        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        ProductVariant? variant = await db.ProductVariants.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (variant is null) return Missing();
        if (variant.IsActive) return Success(new { message = "تنوع از قبل فعال است." });
        if (!await db.VariantNames.AnyAsync(x => x.Id == variant.IdVariantName && x.IsActive, cancellationToken))
            return Conflict(new { detail = "نام مرجع این تنوع غیرفعال است؛ ابتدا نام مرجع را فعال کنید." });
        if (!await db.Products.AnyAsync(x => x.Id == variant.IdProduct && x.IsWarehouseEnabled, cancellationToken))
            return Conflict(new { detail = "کالای فعال پیدا نشد." });
        bool hasOtherActive =
            await db.ProductVariants.AnyAsync(x => x.IdProduct == variant.IdProduct && x.Id != id && x.IsActive,
                cancellationToken);
        if (!hasOtherActive)
        {
            bool hasUnvariedVouchers = await db.VoucherLines
                .AnyAsync(line => db.InventoryItems.Any(item =>
                    item.Id == line.IdInventoryItem && item.IdProduct == variant.IdProduct &&
                    item.IdVariant == null), cancellationToken);
            bool hasUnvariedMovements = await db.WarehouseStockMovements
                .AnyAsync(movement => db.InventoryItems.Any(item =>
                    item.Id == movement.IdInventoryItem && item.IdProduct == variant.IdProduct &&
                    item.IdVariant == null), cancellationToken);
            bool hasUnvariedBalance = await db.StockBalances
                .AnyAsync(balance => db.InventoryItems.Any(item =>
                    item.Id == balance.IdInventoryItem && item.IdProduct == variant.IdProduct &&
                    item.IdVariant == null) && balance.Quantity != 0, cancellationToken);
            if (hasUnvariedVouchers || hasUnvariedMovements || hasUnvariedBalance)
                return Conflict(new
                {
                    detail =
                        "برای این کالا سند انبار یا موجودی بدون تنوع ثبت شده است؛ ابتدا اتصال موجودی را بررسی کنید."
                });
        }

        variant.IsActive = true;
        variant.UpdatedAtUtc = DateTime.UtcNow;
        var inventoryItem =
            await db.InventoryItems.SingleOrDefaultAsync(x => x.IdVariant == id, cancellationToken);
        if (inventoryItem is not null) inventoryItem.IsActive = true;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Success(new { message = "تنوع فعال شد." });
    }

    private async Task<AdminOperationResult> SaveCoreAsync(string section, long? id, JsonElement payload,
        CancellationToken cancellationToken)
    {
        switch (section)
        {
            case "stock-owners":
            {
                if (!User.IsInRole("system.full-access")) return Denied();

                StockOwnerInput? input = Parse<StockOwnerInput>(payload);
                if (input is null || !ValidCodeName(input.Code, input.DisplayName, 64, 200) ||
                    input.IdStockOwnerType is not (1 or 2))
                    return Validation("کد، نام و نوع مالک معتبر الزامی است.");

                if (!System.Text.RegularExpressions.Regex.IsMatch(input.Code.Trim(), "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
                    return Validation("کد باید با حروف کوچک انگلیسی، عدد و خط تیره باشد.");

                if (await db.SalesPlanReferences.AnyAsync(x => x.Id != id &&
                                                               (x.StoreSalesPlanKey == input.Code.Trim() ||
                                                                x.Code == input.Code.Trim()), cancellationToken))
                    return Conflict(new { detail = "این کد قبلاً ثبت شده است." });

                SalesPlanReference? entity = id.HasValue
                    ? await db.SalesPlanReferences.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
                    : new();
                if (entity is null) return Missing();

                if (id.HasValue && entity.IdStockOwnerType != input.IdStockOwnerType)
                    return Conflict(new { detail = "نوع مالک موجود قابل تغییر نیست." });

                if (id.HasValue && !input.IsActive && (entity.IsRegular ||
                                                       await db.WarehouseStockOwners.AnyAsync(
                                                           x => x.IdStockOwner == id.Value, cancellationToken)))
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

                return Success(new { entity.Id, message = "مالک ذخیره شد." });
            }
            case "companies":
            {
                CompanyInput? input = Parse<CompanyInput>(payload);
                if (input is null || !ValidCodeName(input.Code, input.Name, 50, 200))
                    return Validation("کد و نام شرکت را درست وارد کنید.");

                if (input.Phone?.Trim().Length > 50 || input.Address?.Trim().Length > 500 ||
                    input.Description?.Trim().Length > 2000)
                    return Validation("طول تلفن، آدرس یا توضیحات بیش از حد مجاز است.");

                if (!id.HasValue && !User.IsInRole("system.full-access")) return Denied();

                Company entity = id.HasValue
                    ? await db.Companies.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? null!
                    : new Company();
                if (id.HasValue && entity is null) return Missing();

                if (id.HasValue && !User.IsInRole("system.full-access") &&
                    !await CanAccessCompanyAsync(id.Value, cancellationToken)) return Denied();

                entity.Code = input.Code.Trim();
                entity.Name = input.Name.Trim();
                entity.Phone = NormalizeOptional(input.Phone);
                entity.Address = NormalizeOptional(input.Address);
                entity.Description = NormalizeOptional(input.Description);
                entity.IsActive = input.IsActive;

                if (!id.HasValue) db.Companies.Add(entity);
                await db.SaveChangesAsync(cancellationToken);

                return Success(new { entity.Id, message = "شرکت ذخیره شد." });
            }
            case "warehouses":
            {
                WarehouseInput? input = Parse<WarehouseInput>(payload);
                if (input is null || input.CompanyId < 1 || string.IsNullOrWhiteSpace(input.Name) ||
                    input.Name.Trim().Length > 200 || string.IsNullOrWhiteSpace(input.Code) ||
                    input.Code.Trim().Length > 50)
                    return Validation("شرکت، کد و نام انبار را درست وارد کنید.");

                if (id.HasValue && !await CanAccessWarehouseAsync(id.Value, cancellationToken)) return Denied();

                if (!await db.Companies.AnyAsync(x => x.Id == input.CompanyId && x.IsActive, cancellationToken))
                    return Validation("شرکت فعال پیدا نشد.");

                if (!User.IsInRole("system.full-access"))
                {
                    if (!await CanAccessCompanyAsync(input.CompanyId, cancellationToken)) return Denied();

                    if (id.HasValue)
                    {
                        long existingCompanyId = await db.Warehouses.Where(x => x.Id == id.Value)
                            .Select(x => x.IdCompany).SingleAsync(cancellationToken);
                        if (existingCompanyId != input.CompanyId) return Denied();
                    }
                }

                if (input.OwnerSalesPlanId is null ||
                    !await db.SalesPlanReferences.AnyAsync(
                        x => x.Id == input.OwnerSalesPlanId && x.IsActive,
                        cancellationToken))
                    return Validation("برای هر انبار، مالک فعال الزامی است.");

                if (input.IsSiteWarehouse && !input.IsSalesWarehouse)
                    return Validation("انبار پیش‌فرض سایت باید انبار فروش باشد.");

                if (!input.IsActive &&
                    (input.IsSiteWarehouse || await IsSiteWarehouseAsync(id ?? 0, cancellationToken)))
                    return Validation("انبار متصل به سایت باید فعال باشد.");

                if (await IsSiteWarehouseAsync(id ?? 0, cancellationToken) && !input.IsSalesWarehouse)
                    return Conflict(new
                        { detail = "انبار متصل به سایت باید تا زمان جابه‌جایی اتصال، انبار فروش بماند." });

                if (input.IsSiteWarehouse && !User.IsInRole("system.full-access")) return Denied();

                WarehouseEntity entity = id.HasValue
                    ? await db.Warehouses.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? null!
                    : new WarehouseEntity();
                if (id.HasValue && entity is null) return Missing();

                if (id.HasValue && entity.IsSalesWarehouse && !input.IsSalesWarehouse)
                    return Conflict(new
                    {
                        detail =
                            "شرکت فعال باید یک انبار فروش داشته باشد؛ برای انتقال نقش، انبار فروش جایگزین را فعال کنید."
                    });

                WarehouseEntity? previousSalesWarehouse = input.IsSalesWarehouse
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
                if (await db.Warehouses.AnyAsync(
                        x => x.IdCompany == input.CompanyId && x.Name == input.Name.Trim() &&
                             (!id.HasValue || x.Id != id.Value), cancellationToken))
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

                var ownerLinks = await db.WarehouseStockOwners.Where(x => x.IdWarehouse == entity.Id)
                    .ToListAsync(cancellationToken);
                foreach (var ownerLink in ownerLinks) ownerLink.IsDefault = false;
                var selectedOwnerLink = ownerLinks.SingleOrDefault(x => x.IdStockOwner == input.OwnerSalesPlanId);
                if (selectedOwnerLink is null)
                {
                    selectedOwnerLink = new WarehouseStockOwner
                        { IdWarehouse = entity.Id, IdStockOwner = input.OwnerSalesPlanId!.Value };
                    db.WarehouseStockOwners.Add(selectedOwnerLink);
                }

                selectedOwnerLink.IsDefault = true;
                await db.SaveChangesAsync(cancellationToken);
                if (input.IsSiteWarehouse && !await SetSiteWarehouseAsync(entity.Id, cancellationToken))
                    return Conflict(new { detail = "تنظیمات سایت پیدا نشد." });

                await transaction.CommitAsync(cancellationToken);

                return Success(new { entity.Id, message = "انبار ذخیره شد." });
            }
            case "voucher-types":
            {
                VoucherTypeInput? input = Parse<VoucherTypeInput>(payload);
                if (input is null || string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 150 ||
                    input.Direction is not ("R" or "I"))
                    return Validation("نام و جهت سند را درست وارد کنید.");

                VoucherType entity = id.HasValue
                    ? await db.VoucherTypes.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? null!
                    : new VoucherType();
                if (id.HasValue && entity is null) return Missing();

                if (id.HasValue && await db.Vouchers.AnyAsync(x => x.IdVoucherType == id)
                                && (entity.Direction != input.Direction || entity.AffectsStock != input.AffectsStock ||
                                    entity.IsReturn != input.IsReturn))
                    return Conflict(new
                    {
                        detail =
                            "جهت، اثر موجودی و نوع مرجوعی سندی که سابقه دارد قابل تغییر نیست؛ آن را غیرفعال و نوع تازه تعریف کنید."
                    });

                if (entity.IsSystemSalesDispatch && (input.Direction != "I" || !input.AffectsStock || input.IsReturn))
                    return Conflict(new
                        { detail = "حوالهٔ سیستمی فروش باید خروجی و مؤثر بر موجودی باشد و نمی‌تواند مرجوعی باشد." });

                entity.Name = input.Name.Trim();
                entity.Direction = input.Direction;
                entity.AffectsStock = input.AffectsStock;
                entity.IsReturn = input.IsReturn;
                entity.IsActive = input.IsActive;
                entity.UpdatedAtUtc = DateTime.UtcNow;

                if (!id.HasValue) db.VoucherTypes.Add(entity);
                await db.SaveChangesAsync(cancellationToken);

                return Success(new { entity.Id, message = "نوع سند ذخیره شد." });
            }
            case "references":
            {
                PartyInput? input = Parse<PartyInput>(payload);
                if (input is null || !ValidCodeName(input.Name, input.Name, 200, 200)
                                  || input.PartyType is not ("Person" or "Organization" or "Warehouse" or "Other")
                                  || (input.ExternalPartyKey?.Trim().Length ?? 0) > 128)
                    return Validation("نوع و نام مرجع معتبر نیست.");

                long? linkedWarehouse = input.PartyType == "Warehouse" ? input.WarehouseId : null;
                if (input.PartyType == "Warehouse" && (linkedWarehouse is null ||
                                                       !await db.Warehouses.AnyAsync(
                                                           x => x.Id == linkedWarehouse && x.IsActive,
                                                           cancellationToken)))
                    return Validation("برای مرجع انبار، انبار فعال را انتخاب کنید.");

                string? externalPartyKey = string.IsNullOrWhiteSpace(input.ExternalPartyKey)
                    ? null
                    : input.ExternalPartyKey.Trim();
                if (linkedWarehouse.HasValue && await db.WarehouseParties.AnyAsync(
                        party => party.IdWarehouse == linkedWarehouse.Value && (!id.HasValue || party.Id != id.Value),
                        cancellationToken))
                    return Conflict(new { detail = "برای این انبار قبلاً مرجع ثبت شده است؛ همان مرجع را ویرایش کنید." });

                if (externalPartyKey is not null && await db.WarehouseParties.AnyAsync(
                        party => party.ExternalPartyKey == externalPartyKey && (!id.HasValue || party.Id != id.Value),
                        cancellationToken))
                    return Conflict(new { detail = "این شناسهٔ بیرونی قبلاً برای مرجع دیگری ثبت شده است." });

                if (linkedWarehouse.HasValue &&
                    !await CanAccessWarehouseAsync(linkedWarehouse.Value, cancellationToken)) return Denied();

                WarehouseParty entity = id.HasValue
                    ? await db.WarehouseParties.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? null!
                    : new WarehouseParty();
                if (id.HasValue && entity is null) return Missing();

                if (entity.IdWarehouse.HasValue &&
                    !await CanAccessWarehouseAsync(entity.IdWarehouse.Value, cancellationToken)) return Denied();

                entity.Name = input.Name.Trim();
                entity.PartyType = input.PartyType;
                entity.ExternalPartyKey = externalPartyKey;
                entity.IdWarehouse = linkedWarehouse;
                entity.IsActive = input.IsActive;
                entity.UpdatedAtUtc = DateTime.UtcNow;

                if (!id.HasValue) db.WarehouseParties.Add(entity);
                await db.SaveChangesAsync(cancellationToken);

                return Success(new { entity.Id, message = "مرجع ذخیره شد." });
            }
            case "units":
            {
                UnitInput? input = Parse<UnitInput>(payload);
                if (input is null || string.IsNullOrWhiteSpace(input.NameFa) || input.NameFa.Trim().Length > 100 ||
                    (input.NameEn?.Trim().Length > 100) || !input.ParentId.HasValue)
                    return Validation("نام فارسی واحد و گروه والد فعال الزامی است؛ نام انگلیسی اختیاری است.");

                if (!await db.WarehouseUnits.AnyAsync(
                        x => x.Id == input.ParentId!.Value && x.IdParent == null && x.IsActive, cancellationToken))
                    return Validation("گروه والد انتخاب‌شده فعال نیست.");

                WarehouseUnit entity = id.HasValue
                    ? await db.WarehouseUnits.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? null!
                    : new WarehouseUnit();
                if (id.HasValue && entity is null) return Missing();

                if (id.HasValue && entity.IsActive && !input.IsActive && !User.IsInRole("system.full-access") &&
                    !User.HasClaim("admin_permission", "Warehouse:DefinitionsApi:Deactivate")) return Denied();

                if (id.HasValue && !input.IsActive &&
                    await db.WarehouseProductUnits.AnyAsync(x => x.IdUnit == id.Value && x.IsActive, cancellationToken))
                    return Conflict(new
                    {
                        detail =
                            "این واحد برای کالا استفاده می‌شود؛ ابتدا واحدهای وابسته را تغییر دهید یا غیرفعال کنید."
                    });

                entity.NameFa = input.NameFa.Trim();
                entity.NameEn = string.IsNullOrWhiteSpace(input.NameEn) ? null : input.NameEn.Trim();
                entity.IdParent = input.ParentId;
                entity.IsActive = input.IsActive;

                if (!id.HasValue) db.WarehouseUnits.Add(entity);
                await db.SaveChangesAsync(cancellationToken);

                return Success(new { entity.Id, message = "واحد ذخیره شد." });
            }
            case "unit-groups":
            {
                UnitGroupInput? input = Parse<UnitGroupInput>(payload);
                string name = input?.Name?.Trim() ?? "";
                if (name.Length is < 1 or > 100 || input!.SortOrder < 0 || input.NameEn?.Trim().Length > 100)
                    return Validation("نام گروه و ترتیب نمایش معتبر الزامی است.");
                if (await db.WarehouseUnits.AnyAsync(
                        x => x.IdParent == null && x.NameFa == name && (!id.HasValue || x.Id != id.Value),
                        cancellationToken))
                    return Conflict(new { detail = "گروهی با این نام از قبل ثبت شده است." });

                WarehouseUnit? entity = id.HasValue
                    ? await db.WarehouseUnits.SingleOrDefaultAsync(x => x.Id == id && x.IdParent == null,
                        cancellationToken)
                    : new WarehouseUnit();
                if (entity is null) return Missing();
                if (id.HasValue && entity.IsActive && !input.IsActive &&
                    await db.WarehouseUnits.AnyAsync(x => x.IdParent == id.Value && x.IsActive, cancellationToken))
                    return Conflict(new
                        { detail = "گروه دارای عضو فعال است؛ ابتدا اعضای آن را جابه‌جا یا غیرفعال کنید." });

                entity.NameFa = name;
                entity.NameEn = string.IsNullOrWhiteSpace(input.NameEn) ? null : input.NameEn.Trim();
                entity.IdParent = null;
                entity.SortOrder = input.SortOrder;
                entity.IsActive = input.IsActive;
                if (!id.HasValue) db.WarehouseUnits.Add(entity);
                await db.SaveChangesAsync(cancellationToken);
                return Success(new { entity.Id, message = "گروه واحد ذخیره شد." });
            }
            case "product-units":
            {
                ProductUnitInput? input = Parse<ProductUnitInput>(payload);
                if (input is null || input.ProductId < 1 || input.UnitId < 1 || input.ConversionFactor <= 0 ||
                    input.ConversionFactor > 999999999.999999999m)
                    return Validation("کالا، واحد و ضریب تبدیل در محدودهٔ مجاز الزامی است.");

                WarehouseProduct? product =
                    await db.Products.SingleOrDefaultAsync(x => x.Id == input.ProductId && x.IsWarehouseEnabled,
                        cancellationToken);
                if (product is null ||
                    !await db.WarehouseUnits.AnyAsync(x => x.Id == input.UnitId && x.IdParent != null && x.IsActive,
                        cancellationToken))
                    return Validation("کالا یا واحد فعال پیدا نشد.");

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
                if (id.HasValue && entity is null) return Missing();
                if (entity is { IsActive: false })
                    return Conflict(new
                        { detail = "واحد غیرفعال‌شده فقط در تاریخچه قابل مشاهده است و دوباره فعال نمی‌شود." });

                if (entity is not null && entity.IdProduct != input.ProductId && entity.IsBaseUnit)
                    return Conflict(new { detail = "واحد اصلی کالا را نمی‌توان به کالای دیگری منتقل کرد." });

                if (entity?.IsBaseUnit == true && (!input.IsBaseUnit || !input.IsActive))
                    return Conflict(new
                        { detail = "واحد اصلی کالا باید فعال بماند؛ ابتدا واحد اصلی جایگزین را تعریف کنید." });

                if (!input.IsBaseUnit && !await db.WarehouseProductUnits.AnyAsync(
                        x => x.IdProduct == input.ProductId && x.IsBaseUnit && x.IsActive, cancellationToken))
                    return Validation("ابتدا واحد اصلی فعال کالا را تعریف کنید.");

                await using var transaction =
                    await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
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

                return Success(new { entity.Id, message = "واحد کالا و ضریب تبدیل ذخیره شد." });
            }
            case "variants":
            {
                VariantInput? input = Parse<VariantInput>(payload);
                if (input is null || (!id.HasValue && input.ProductId < 1) || input.VariantNameId < 1)
                    return Validation("نام تنوع را از مرجع انتخاب کنید و برای تنوع جدید، کالا را تعیین کنید.");

                await using var transaction =
                    await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
                ProductVariant entity = id.HasValue
                    ? await db.ProductVariants.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? null!
                    : new ProductVariant();
                if (id.HasValue && entity is null) return Missing();

                if (id.HasValue && entity.IdProduct != input.ProductId)
                    return Conflict(new { detail = "کالای تنوع پس از ایجاد قابل تغییر نیست." });
                decimal productId = id.HasValue ? entity.IdProduct : input.ProductId;
                WarehouseProduct? product =
                    await db.Products.SingleOrDefaultAsync(x => x.Id == productId && x.IsWarehouseEnabled,
                        cancellationToken);
                if (product is null) return Validation("محصول فعال پیدا نشد.");
                // Activating the first variant would hide the unvaried inventory from the store.
                if (input.IsActive && !entity.IsActive &&
                    !await db.ProductVariants.AnyAsync(x => x.IdProduct == productId && x.Id != (id ?? 0) && x.IsActive,
                        cancellationToken))
                {
                    bool hasUnvariedVouchers = await db.VoucherLines
                        .AnyAsync(line => db.InventoryItems.Any(item =>
                            item.Id == line.IdInventoryItem && item.IdProduct == productId &&
                            item.IdVariant == null), cancellationToken);
                    bool hasUnvariedMovements = await db.WarehouseStockMovements
                        .AnyAsync(movement => db.InventoryItems.Any(item =>
                            item.Id == movement.IdInventoryItem && item.IdProduct == productId &&
                            item.IdVariant == null), cancellationToken);
                    bool hasUnvariedBalance = await db.StockBalances
                        .AnyAsync(balance => db.InventoryItems.Any(item =>
                            item.Id == balance.IdInventoryItem && item.IdProduct == productId &&
                            item.IdVariant == null) && balance.Quantity != 0, cancellationToken);
                    if (hasUnvariedVouchers || hasUnvariedMovements || hasUnvariedBalance)
                        return Conflict(new
                        {
                            detail =
                                "برای این کالا سند انبار یا موجودی بدون تنوع ثبت شده است؛ امکان فعال‌سازی نخستین تنوع وجود ندارد."
                        });
                }

                if (!await db.VariantNames.AnyAsync(x => x.Id == input.VariantNameId && x.IsActive, cancellationToken))
                    return Validation("نام تنوع انتخاب‌شده در مرجع فعال نیست.");
                if (!await ValidVariantAttributesAsync(productId, id, input.Attributes, cancellationToken))
                    return Validation("ویژگی‌ها و مقدارها باید از درخت تنوع همین کالا انتخاب شوند و ویژگی تکراری نباشد.");

                if (id.HasValue && entity.IsActive && !input.IsActive &&
                    !await db.ProductVariants.AnyAsync(x => x.IdProduct == productId && x.Id != entity.Id && x.IsActive,
                        cancellationToken))
                    return Conflict(new
                    {
                        detail =
                            "کالای دارای تنوع باید حداقل یک تنوع فعال داشته باشد؛ آخرین تنوع فعال را نمی‌توان غیرفعال کرد."
                    });

                if (entity.IsDefault && !input.IsActive)
                    return Conflict(new
                        { detail = "تنوع پیش‌فرض را نمی‌توان غیرفعال کرد؛ ابتدا تنوع فعال دیگری را پیش‌فرض کنید." });

                bool hasOtherDefault = await db.ProductVariants.AnyAsync(
                    x => x.IdProduct == productId && x.Id != (id ?? 0) && x.IsDefault && x.IsActive, cancellationToken);
                if (!id.HasValue && !hasOtherDefault && !input.IsActive)
                    return Validation("نخستین تنوع کالا باید فعال باشد تا به‌عنوان پیش‌فرض ثبت شود.");
                if (!input.IsActive && input.IsDefault)
                    return Validation("تنوع پیش‌فرض باید فعال باشد.");
                bool makeDefault = input.IsActive && (input.IsDefault || !hasOtherDefault);
                if (!makeDefault && entity.IsDefault)
                    return Validation("برای هر کالا باید یک تنوع پیش‌فرض فعال باقی بماند.");

                if (makeDefault)
                    await db.ProductVariants.Where(x => x.IdProduct == productId && x.Id != (id ?? 0) && x.IsDefault)
                        .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsDefault, false), cancellationToken);
                entity.IdProduct = productId;
                entity.IdVariantName = input.VariantNameId;
                entity.IsDefault = makeDefault;
                entity.IsActive = input.IsActive;
                entity.UpdatedAtUtc = DateTime.UtcNow;

                if (!id.HasValue)
                {
                    entity.CreatedAtUtc = DateTime.UtcNow;
                    db.ProductVariants.Add(entity);
                }

                await db.SaveChangesAsync(cancellationToken);

                if (input.Attributes is not null)
                {
                    await SaveVariantAttributesAsync(entity, input.Attributes, cancellationToken);
                    await db.SaveChangesAsync(cancellationToken);
                }

                if (!id.HasValue &&
                    !await db.InventoryItems.AnyAsync(x => x.IdProduct == productId && x.IdVariant == entity.Id,
                        cancellationToken))
                {
                    db.InventoryItems.Add(new InventoryItem
                        { IdProduct = productId, IdVariant = entity.Id, IsActive = true });
                    await db.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                return Success(new { entity.Id, message = "تنوع و قلم موجودی آن ذخیره شد." });
            }
            default:
                return Missing();
        }
    }

    private async Task<bool> ValidVariantAttributesAsync(decimal productId, long? variantId,
        IReadOnlyCollection<VariantAttributeInput>? attributes, CancellationToken cancellationToken)
    {
        if (attributes is null) return true;
        if (attributes.Count > 12) return false;
        if (attributes.Any(x => x is null || x.AttributeId < 1 || x.ValueId < 1) ||
            attributes.Select(x => x.AttributeId).Distinct().Count() != attributes.Count) return false;
        var categoryId = await db.Products.Where(x => x.Id == productId).Select(x => x.IdCategory)
            .SingleAsync(cancellationToken);
        var categoryIds = new List<long>();
        var categories = await db.StoreCategories.AsNoTracking().Select(x => new { x.Id, x.IdParentCategory })
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        while (categoryId is long current && categories.TryGetValue(current, out var category) &&
               !categoryIds.Contains(current))
        {
            categoryIds.Add(current);
            categoryId = category.IdParentCategory;
        }

        foreach (VariantAttributeInput item in attributes)
            if (!await db.StoreCatalogFeatures
                    .AsNoTracking().Where(x => x.IdParent != null)
                    .Join(
                        db.StoreCatalogFeatures.AsNoTracking(),
                        value => value.IdParent,
                        feature => (long?)feature.Id,
                        (value, feature) => new { value, feature })
                    .AnyAsync(valueFeature =>
                            valueFeature.value.Id == item.ValueId && valueFeature.feature.Id == item.AttributeId &&
                            valueFeature.feature.Kind == StoreCatalogFeatureKinds.PriceVariant &&
                            valueFeature.feature.IsActive && valueFeature.value.IsActive &&
                            db.StoreCategoryCatalogFeatures.Any(link =>
                                categoryIds.Contains(link.IdCategory) && link.IdFeature == valueFeature.feature.Id),
                        cancellationToken))
                return false;
        return true;
    }

    private bool CanAccessVariants(string section) =>
        !string.Equals(section, "variants", StringComparison.OrdinalIgnoreCase) ||
        User.IsInRole("system.full-access") || User.HasClaim("admin_permission", "Warehouse:Features:Index:variants");

    private bool CanAccessVariantNames() =>
        User.IsInRole("system.full-access") ||
        User.HasClaim("admin_permission", "Warehouse:Features:Index:variant-names");

    private async Task SaveVariantAttributesAsync(ProductVariant variant,
        IReadOnlyCollection<VariantAttributeInput> input,
        CancellationToken cancellationToken)
    {
        var currentLinks = await db.ProductVariantAttributeValues.Where(x => x.IdProductVariant == variant.Id)
            .ToListAsync(cancellationToken);
        var oldValues = await db.ProductVariantAttributeValues
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
            .Where(linkValueAttribute => linkValueAttribute.link.IdProductVariant == variant.Id)
            .Select(linkValueAttribute => new
            {
                linkValueAttribute.attribute.Label,
                linkValueAttribute.value.DisplayValue,
                ValueId = linkValueAttribute.value.Id
            })
            .ToListAsync(cancellationToken);
        db.ProductVariantAttributeValues.RemoveRange(currentLinks);
        var catalogLinks = await db.StoreVariantCatalogFeatureValues.Where(x => x.IdProductVariant == variant.Id)
            .ToListAsync(cancellationToken);
        db.StoreVariantCatalogFeatureValues.RemoveRange(catalogLinks);
        if (currentLinks.Count != 0 || catalogLinks.Count != 0) await db.SaveChangesAsync(cancellationToken);
        if (input.Count == 0) return;

        foreach (VariantAttributeInput item in input)
        {
            var feature = await db.StoreCatalogFeatures.SingleAsync(x => x.Id == item.AttributeId, cancellationToken);
            var selected =
                await db.StoreCatalogFeatures.SingleAsync(x => x.Id == item.ValueId && x.IdParent == feature.Id,
                    cancellationToken);
            var oldValue = oldValues.FirstOrDefault(x => x.Label == feature.Name && x.DisplayValue == selected.Name);
            if (oldValue is not null)
            {
                var preserved =
                    await db.ProductAttributeValues.SingleAsync(x => x.Id == oldValue.ValueId, cancellationToken);
                db.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
                    { IdProduct = variant.IdProduct, ProductVariant = variant, AttributeValue = preserved });
                db.StoreVariantCatalogFeatureValues.Add(new StoreVariantCatalogFeatureValue
                    { IdProductVariant = variant.Id, IdFeature = feature.Id, IdFeatureValue = selected.Id });
                continue;
            }

            string attributeKey = $"catalog-{feature.Id}";
            string valueKey = $"catalog-{selected.Id}";
            var attribute =
                await db.ProductAttributes.SingleOrDefaultAsync(
                    x => x.IdProduct == variant.IdProduct && x.Key == attributeKey, cancellationToken);
            if (attribute is null)
            {
                attribute = new ProductAttribute
                    { IdProduct = variant.IdProduct, Key = attributeKey, Label = feature.Name, IsActive = true };
                db.ProductAttributes.Add(attribute);
                await db.SaveChangesAsync(cancellationToken);
            }

            var value = await db.ProductAttributeValues.SingleOrDefaultAsync(
                x => x.IdProductAttribute == attribute.Id && x.ValueKey == valueKey, cancellationToken);
            if (value is null)
            {
                value = new ProductAttributeValue
                {
                    IdProduct = variant.IdProduct, IdProductAttribute = attribute.Id, ValueKey = valueKey,
                    Label = selected.Name, DisplayValue = selected.Name, IsActive = true
                };
                db.ProductAttributeValues.Add(value);
                await db.SaveChangesAsync(cancellationToken);
            }

            db.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
                { IdProduct = variant.IdProduct, ProductVariant = variant, AttributeValue = value });
            db.StoreVariantCatalogFeatureValues.Add(new StoreVariantCatalogFeatureValue
                { IdProductVariant = variant.Id, IdFeature = feature.Id, IdFeatureValue = selected.Id });
        }
    }

    private async Task<AdminOperationResult> GetWarehousesAsync(CancellationToken cancellationToken)
    {
        long? siteWarehouseId = await db.StoreSiteIdentities.AsNoTracking().Where(x => x.Id == 1)
            .Select(x => x.IdWarehouse).SingleOrDefaultAsync(cancellationToken);
        var query = db.Warehouses
            .AsNoTracking()
            .Join(
                db.Companies.AsNoTracking(),
                warehouse => warehouse.IdCompany,
                company => company.Id,
                (warehouse, company) => new { warehouse, company })
            .GroupJoin(
                db.WarehouseStockOwners.AsNoTracking().Where(owner => owner.IsDefault),
                warehouseCompany => warehouseCompany.warehouse.Id,
                ownerLink => ownerLink.IdWarehouse,
                (warehouseCompany, ownerLinks) => new { warehouseCompany, ownerLinks })
            .SelectMany(
                warehouseOwnerLinks => warehouseOwnerLinks.ownerLinks.DefaultIfEmpty(),
                (warehouseOwnerLinks, ownerLink) => new { warehouseOwnerLinks.warehouseCompany, ownerLink })
            .GroupJoin(
                db.SalesPlanReferences.AsNoTracking(),
                warehouseOwner => warehouseOwner.ownerLink == null
                    ? (long?)null
                    : warehouseOwner.ownerLink.IdStockOwner,
                plan => (long?)plan.Id,
                (warehouseOwner, plans) => new { warehouseOwner, plans })
            .SelectMany(
                warehouseOwnerPlans => warehouseOwnerPlans.plans.DefaultIfEmpty(),
                (warehouseOwnerPlans, plan) => new
                {
                    warehouse = warehouseOwnerPlans.warehouseOwner.warehouseCompany.warehouse,
                    company = warehouseOwnerPlans.warehouseOwner.warehouseCompany.company,
                    warehouseOwnerPlans.warehouseOwner.ownerLink,
                    plan
                })
            .OrderBy(warehouseDetails => warehouseDetails.company.Name)
            .ThenBy(warehouseDetails => warehouseDetails.warehouse.Name)
            .Select(warehouseDetails => new
            {
                warehouseDetails.warehouse.Id,
                warehouseDetails.warehouse.IdCompany,
                CompanyName = warehouseDetails.company.Name,
                warehouseDetails.warehouse.Code,
                warehouseDetails.warehouse.Name,
                warehouseDetails.warehouse.IsSalesWarehouse,
                OwnerSalesPlanId = warehouseDetails.ownerLink == null
                    ? (long?)null
                    : warehouseDetails.ownerLink.IdStockOwner,
                OwnerSalesPlan = warehouseDetails.plan == null ? null : warehouseDetails.plan.DisplayName,
                warehouseDetails.warehouse.IsActive,
                IsSiteWarehouse = warehouseDetails.warehouse.Id == siteWarehouseId
            });
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out long parsed)
                ? parsed
                : 0;
            long[] allowed = (await db.AdminSystemUserWarehouses
                .AsNoTracking()
                .Where(access => access.IdUser == userId)
                .Select(access => access.IdWarehouse)
                .ToListAsync(cancellationToken))
                .ToArray();
            query = query.Where(x => allowed.Contains(x.Id));
        }

        return Success(await query.ToListAsync(cancellationToken));
    }

    private async Task<AdminOperationResult> GetCompaniesAsync(CancellationToken cancellationToken)
    {
        IQueryable<Company> query = db.Companies.AsNoTracking();
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out long parsed)
                ? parsed
                : 0;
            long[] companyIds = (await db.AdminSystemUserWarehouses
                .AsNoTracking()
                .Join(
                    db.Warehouses.AsNoTracking(),
                    access => access.IdWarehouse,
                    warehouse => warehouse.Id,
                    (access, warehouse) => new { access, warehouse })
                .Where(accessWarehouse => accessWarehouse.access.IdUser == userId)
                .Select(accessWarehouse => accessWarehouse.warehouse.IdCompany)
                .Distinct()
                .ToListAsync(cancellationToken))
                .ToArray();
            query = query.Where(x => companyIds.Contains(x.Id));
        }

        return Success(await query.OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Code, x.Name, x.Phone, x.Address, x.Description, x.IsActive })
            .ToListAsync(cancellationToken));
    }

    private async Task<AdminOperationResult> GetReferencesAsync(CancellationToken cancellationToken)
    {
        IQueryable<WarehouseParty> query = db.WarehouseParties.AsNoTracking();
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out long parsed)
                ? parsed
                : 0;
            long[] allowed = (await db.AdminSystemUserWarehouses
                .AsNoTracking()
                .Where(access => access.IdUser == userId)
                .Select(access => access.IdWarehouse)
                .ToListAsync(cancellationToken))
                .ToArray();
            query = query.Where(x => x.IdWarehouse == null || allowed.Contains(x.IdWarehouse.Value));
        }

        return Success(await query.OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.PartyType, x.ExternalPartyKey, x.IdWarehouse, x.IsActive })
            .ToListAsync(cancellationToken));
    }

    private async Task<AdminOperationResult> GetSalesPlanMappingAsync(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        long? siteId = await db.StoreSiteIdentities.AsNoTracking().Where(x => x.Id == 1).Select(x => x.IdWarehouse)
            .SingleOrDefaultAsync(cancellationToken);
        var companies = await db.Companies.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken);
        var warehouses = await db.Warehouses.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Name).Select(x => new
            {
                x.Id, x.IdCompany, x.Name, x.Code,
                DefaultOwnerSalesPlanId = db.WarehouseStockOwners
                    .Where(link => link.IdWarehouse == x.Id && link.IsDefault).Select(link => (long?)link.IdStockOwner)
                    .FirstOrDefault()
            }).ToListAsync(cancellationToken);
        var plans = await db.SalesPlanReferences.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayName)
            .Select(x => new { x.Id, x.DisplayName, x.StoreSalesPlanKey }).ToListAsync(cancellationToken);
        var links = await db.WarehouseStockOwners.AsNoTracking().ToListAsync(cancellationToken);

        return Success(new { siteWarehouseId = siteId, companies, warehouses, plans, links });
    }

    private async Task<AdminOperationResult> GetStockOwnersAsync(CancellationToken cancellationToken)
    {
        var owners = await db.SalesPlanReferences.AsNoTracking().OrderBy(x => x.DisplayName)
            .Select(x => new
                { x.Id, x.StoreSalesPlanKey, x.DisplayName, x.IdStockOwnerType, x.Code, x.PlanType, x.IsActive })
            .ToListAsync(cancellationToken);
        var warehouseLinks = await db.WarehouseStockOwners.AsNoTracking()
            .Join(db.Warehouses.AsNoTracking(), link => link.IdWarehouse, warehouse => warehouse.Id,
                (link, warehouse) => new { link.IdStockOwner, warehouse.IdCompany, warehouse.Name })
            .Join(db.Companies.AsNoTracking(), link => link.IdCompany, company => company.Id,
                (link, company) => new { link.IdStockOwner, link.Name, CompanyName = company.Name })
            .OrderBy(link => link.CompanyName).ThenBy(link => link.Name)
            .ToListAsync(cancellationToken);
        var labelsByOwner = warehouseLinks.GroupBy(link => link.IdStockOwner)
            .ToDictionary(group => group.Key,
                group => string.Join("، ", group.Select(link => $"{link.CompanyName} / {link.Name}")));

        return Success(owners.Select(owner => new
        {
            owner.Id,
            owner.StoreSalesPlanKey,
            owner.DisplayName,
            WarehouseLabel = labelsByOwner.GetValueOrDefault(owner.Id),
            owner.IdStockOwnerType,
            owner.Code,
            owner.PlanType,
            owner.IsActive
        }).ToList());
    }

    private async Task<AdminOperationResult> GetLookupsAsync(CancellationToken cancellationToken)
    {
        IQueryable<Company> companyQuery = db.Companies.AsNoTracking().Where(x => x.IsActive);
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out long parsed)
                ? parsed
                : 0;
            long[] allowedCompanyIds = (await db.AdminSystemUserWarehouses
                .AsNoTracking()
                .Join(
                    db.Warehouses.AsNoTracking(),
                    access => access.IdWarehouse,
                    warehouse => warehouse.Id,
                    (access, warehouse) => new { access, warehouse })
                .Where(accessWarehouse => accessWarehouse.access.IdUser == userId)
                .Select(accessWarehouse => accessWarehouse.warehouse.IdCompany)
                .Distinct()
                .ToListAsync(cancellationToken))
                .ToArray();
            companyQuery = companyQuery.Where(x => allowedCompanyIds.Contains(x.Id));
        }

        var companies = await companyQuery.OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Code })
            .ToListAsync(cancellationToken);
        var warehouseQuery = db.Warehouses
            .AsNoTracking()
            .Join(
                db.Companies.AsNoTracking(),
                warehouse => warehouse.IdCompany,
                company => company.Id,
                (warehouse, company) => new { warehouse, company })
            .OrderBy(warehouseCompany => warehouseCompany.company.Name)
            .ThenBy(warehouseCompany => warehouseCompany.warehouse.Name)
            .Select(warehouseCompany => new
            {
                warehouseCompany.warehouse.Id,
                warehouseCompany.warehouse.IdCompany,
                CompanyName = warehouseCompany.company.Name,
                warehouseCompany.warehouse.Code,
                warehouseCompany.warehouse.Name,
                warehouseCompany.warehouse.IsSalesWarehouse,
                DefaultOwnerSalesPlanId = db.WarehouseStockOwners
                    .Where(link => link.IdWarehouse == warehouseCompany.warehouse.Id && link.IsDefault)
                    .Select(link => (long?)link.IdStockOwner)
                    .FirstOrDefault(),
                warehouseCompany.warehouse.IsActive
            });
        if (!User.IsInRole("system.full-access"))
        {
            long userId = long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out long parsed)
                ? parsed
                : 0;
            long[] allowed = (await db.AdminSystemUserWarehouses
                .AsNoTracking()
                .Where(access => access.IdUser == userId)
                .Select(access => access.IdWarehouse)
                .ToListAsync(cancellationToken))
                .ToArray();
            warehouseQuery = warehouseQuery.Where(x => allowed.Contains(x.Id));
        }

        var warehouses = await warehouseQuery.ToListAsync(cancellationToken);
        var plans = await db.SalesPlanReferences.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayName)
            .Select(x => new { x.Id, x.DisplayName, x.StoreSalesPlanKey }).ToListAsync(cancellationToken);
        var units = await db.WarehouseUnits.AsNoTracking().Where(x => x.IsActive && x.IdParent != null)
            .OrderBy(x => x.NameFa).Select(x => new { x.Id, Name = x.NameFa, x.NameEn })
            .ToListAsync(cancellationToken);
        var products = await db.Products
            .AsNoTracking()
            .Join(
                db.WarehouseProductUnits.AsNoTracking().Where(unitLink => unitLink.IsBaseUnit && unitLink.IsActive),
                product => product.Id,
                baseUnit => baseUnit.IdProduct,
                (product, baseUnit) => new { product, baseUnit })
            .Where(productUnit => productUnit.product.IsWarehouseEnabled)
            .OrderBy(productUnit => productUnit.product.NameFa)
            .Select(productUnit => new
                {
                    productUnit.product.Id,
                    Name = productUnit.product.NameFa,
                    StoreProductKey = productUnit.product.ProductKey,
                    IdBaseUnit = productUnit.baseUnit.IdUnit
                })
            .ToListAsync(cancellationToken);

        return Success(new { companies, warehouses, plans, units, products });
    }

    private async Task<bool> SetSiteWarehouseAsync(long warehouseId, CancellationToken cancellationToken)
    {
        var settings = await db.StoreSiteIdentities.SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);
        if (settings is null) return false;

        settings.IdWarehouse = warehouseId;
        settings.IdentityUpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    private Task<bool> IsSiteWarehouseAsync(long warehouseId, CancellationToken cancellationToken) =>
        db.StoreSiteIdentities.AsNoTracking()
            .AnyAsync(x => x.Id == 1 && x.IdWarehouse == warehouseId, cancellationToken);

    private async Task<bool> CanAccessWarehouseAsync(long warehouseId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("system.full-access")) return true;

        long userId =
            long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out long parsed)
                ? parsed
                : 0;

        return await db.AdminSystemUserWarehouses.AsNoTracking()
            .AnyAsync(x => x.IdUser == userId && x.IdWarehouse == warehouseId, cancellationToken);
    }

    private async Task<bool> CanAccessCompanyAsync(long companyId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("system.full-access")) return true;

        long userId =
            long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out long parsed)
                ? parsed
                : 0;

        return await db.AdminSystemUserWarehouses
            .AsNoTracking()
            .Join(
                db.Warehouses.AsNoTracking(),
                access => access.IdWarehouse,
                warehouse => warehouse.Id,
                (access, warehouse) => new { access, warehouse })
            .AnyAsync(accessWarehouse =>
                    accessWarehouse.access.IdUser == userId && accessWarehouse.warehouse.IdCompany == companyId,
                cancellationToken);
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

    private sealed record CompanyInput(
        string Code,
        string Name,
        string? Phone,
        string? Address,
        string? Description,
        bool IsActive);

    private sealed record WarehouseInput(
        long CompanyId,
        string Code,
        string Name,
        bool IsSalesWarehouse,
        long? OwnerSalesPlanId,
        bool IsActive,
        bool IsSiteWarehouse);

    private sealed record VoucherTypeInput(
        string Name,
        string Direction,
        bool AffectsStock,
        bool IsReturn,
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
        long VariantNameId,
        VariantAttributeInput[]? Attributes,
        bool IsDefault,
        bool IsActive);

    private sealed record VariantAttributeInput(long AttributeId, long ValueId);

}
