using System.Text.Json;
using Shop.Data.Application.Store;
using Shop.Data.Application.Store.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Helpers;
using Shop.Data.Application.Admin.Contracts;
using static Shop.Data.Application.Admin.Contracts.AdminOperationResult;

namespace Shop.Data.Infrastructure.Store;

public sealed class StoreCatalogAdminData(ShopDbContext db,
    IMediaCdnBaseUrlProvider mediaCdn,
    IStorePriceVersionService priceVersions, System.Security.Claims.ClaimsPrincipal User) : IStoreCatalogAdminData
{
    private IQueryable<StorePriceVersion> RegularProductPrices() =>
        db.StorePriceVersions
            .AsNoTracking()
            .Join(
                db.SalesPlanReferences.AsNoTracking(),
                version => version.IdSalesPlan,
                plan => (long?)plan.Id,
                (version, plan) => new { version, plan })
            .Where(pricePlan => pricePlan.version.IsActive && pricePlan.plan.IsRegular &&
                                pricePlan.plan.IdStockOwnerType == 1 &&
                                pricePlan.version.IdVariant == null && pricePlan.version.IdAccessory == null)
            .OrderBy(pricePlan => pricePlan.plan.Id)
            .ThenByDescending(pricePlan => pricePlan.version.Id)
            .Select(pricePlan => pricePlan.version);

    private static decimal FinalPrice(StorePriceVersion? price) => price?.FinalPriceAmount ?? 0m;

    public async Task<AdminOperationResult> ProductAccessories(long id, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();
        if (!await db.Products.AnyAsync(x => x.Id == id, cancellationToken)) return Missing();
        var items = await db.StoreProductAccessories
            .AsNoTracking()
            .Join(
                db.StoreAccessories.AsNoTracking(),
                link => link.IdAccessory,
                accessory => accessory.Id,
                (link, accessory) => new { link, accessory })
            .Where(linkAccessory => linkAccessory.link.IdProduct == id)
            .OrderBy(linkAccessory => linkAccessory.link.SortOrder)
            .ThenBy(linkAccessory => linkAccessory.link.Id)
            .Select(linkAccessory => new
                {
                    linkAccessory.link.Id,
                    linkAccessory.accessory.Name,
                    linkAccessory.accessory.Description,
                    linkAccessory.accessory.IsActive,
                    linkAccessory.link.IsRequired,
                    linkAccessory.link.SortOrder,
                    linkAccessory.link.DisplayDiscountPercent,
                    PriceAmount = db.StorePriceVersions
                        .AsNoTracking()
                        .Join(
                            db.SalesPlanReferences.AsNoTracking(),
                            version => version.IdSalesPlan,
                            plan => (long?)plan.Id,
                            (version, plan) => new { version, plan })
                        .Where(pricePlan =>
                            pricePlan.version.IdProduct == id &&
                            pricePlan.version.IdAccessory == linkAccessory.link.IdAccessory &&
                            pricePlan.version.IsActive && pricePlan.plan.IsRegular &&
                            pricePlan.plan.IdStockOwnerType == 1)
                        .OrderBy(pricePlan => pricePlan.plan.Id)
                        .Select(pricePlan => (decimal?)pricePlan.version.FinalPriceAmount)
                        .FirstOrDefault()
                })
            .ToListAsync(cancellationToken);
        return Success(items);
    }

    public async Task<AdminOperationResult> CreateProductAccessory(long id, ProductAccessoryInput input,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();
        if (!ValidAccessory(input)) return Invalid(new { title = "نام، توضیح، ترتیب یا قیمت متعلقه معتبر نیست." });
        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        if (!await db.Products.AnyAsync(x => x.Id == id, cancellationToken)) return Missing();
        var accessory = new StoreAccessory
            { Name = input.Name.Trim(), Description = input.Description?.Trim(), IsActive = input.IsActive };
        db.StoreAccessories.Add(accessory);
        await db.SaveChangesAsync(cancellationToken);
        var link = new StoreProductAccessory
        {
            IdProduct = id, IdAccessory = accessory.Id, IsRequired = input.IsRequired, SortOrder = input.SortOrder,
            DisplayDiscountPercent = input.DisplayDiscountPercent
        };
        db.StoreProductAccessories.Add(link);
        var offers = await db.StoreProductSalesPlanOffers.Where(x => x.IdProduct == id).ToListAsync(cancellationToken);
        db.StoreProductSalesPlanOfferAccessories.AddRange(offers.Select(x => new StoreProductSalesPlanOfferAccessory
        {
            IdOffer = x.Id, IdAccessory = accessory.Id, IsAllowed = true,
            IsIncludedByDefault = input.IsRequired
        }));
        await db.SaveChangesAsync(cancellationToken);
        var regularPlans = await db.SalesPlanReferences.Where(x => x.IsRegular && x.IdStockOwnerType == 1)
            .Select(x => x.Id).ToListAsync(cancellationToken);
        foreach (long planId in regularPlans.Concat(offers.Select(x => x.IdSalesPlan)).Distinct())
            await priceVersions.AppendAsync(id, planId, null, accessory.Id, input.PriceAmount, 0,
                long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Success(new { link.Id, message = "متعلقه ساخته و به کالا متصل شد." });
    }

    public async Task<AdminOperationResult> UpdateProductAccessory(long id, long linkId,
        ProductAccessoryInput input, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();
        if (!ValidAccessory(input)) return Invalid(new { title = "نام، توضیح، ترتیب یا قیمت متعلقه معتبر نیست." });
        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var link = await db.StoreProductAccessories.SingleOrDefaultAsync(x => x.Id == linkId && x.IdProduct == id,
            cancellationToken);
        if (link is null) return Missing();
        var accessory = await db.StoreAccessories.SingleAsync(x => x.Id == link.IdAccessory, cancellationToken);
        if (await db.StoreProductAccessories.AnyAsync(x => x.IdAccessory == accessory.Id && x.Id != link.Id,
                cancellationToken) && (accessory.Name != input.Name.Trim() ||
                                       (accessory.Description ?? "") != (input.Description?.Trim() ?? "") ||
                                       accessory.IsActive != input.IsActive))
            return Conflict(new { title = "نام، توضیح و وضعیت متعلقهٔ مشترک از این مسیر قابل تغییر نیست." });
        accessory.Name = input.Name.Trim();
        accessory.Description = input.Description?.Trim();
        accessory.IsActive = input.IsActive;
        link.IsRequired = input.IsRequired;
        link.SortOrder = input.SortOrder;
        link.DisplayDiscountPercent = input.DisplayDiscountPercent;
        if (input.IsRequired)
        {
            var overrides = await db.StoreProductSalesPlanOfferAccessories
                .Join(
                    db.StoreProductSalesPlanOffers,
                    row => row.IdOffer,
                    offer => offer.Id,
                    (row, offer) => new { row, offer })
                .Where(rowOffer => rowOffer.offer.IdProduct == id && rowOffer.row.IdAccessory == accessory.Id)
                .Select(rowOffer => rowOffer.row)
                .ToListAsync(cancellationToken);
            foreach (var row in overrides)
            {
                row.IsAllowed = true;
                row.IsIncludedByDefault = true;
            }

            var offerIds = await db.StoreProductSalesPlanOffers.Where(x => x.IdProduct == id).Select(x => x.Id)
                .ToListAsync(cancellationToken);
            var existingIds = overrides.Select(x => x.IdOffer).ToHashSet();
            db.StoreProductSalesPlanOfferAccessories.AddRange(offerIds.Where(x => !existingIds.Contains(x)).Select(x =>
                new StoreProductSalesPlanOfferAccessory
                {
                    IdOffer = x, IdAccessory = accessory.Id, IsAllowed = true,
                    IsIncludedByDefault = true
                }));
            var offerPlans = await db.StoreProductSalesPlanOffers.Where(x => x.IdProduct == id)
                .Select(x => x.IdSalesPlan).ToListAsync(cancellationToken);
            foreach (long planId in offerPlans)
                if (!await db.StorePriceVersions.AnyAsync(
                        v => v.IdProduct == id && v.IdAccessory == accessory.Id && v.IdSalesPlan == planId &&
                             v.IsActive, cancellationToken))
                    await priceVersions.AppendAsync(id, planId, null, accessory.Id, input.PriceAmount, 0,
                        long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value),
                        cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        var regularPlans = await db.SalesPlanReferences.Where(x => x.IsRegular && x.IdStockOwnerType == 1)
            .Select(x => x.Id).ToListAsync(cancellationToken);
        foreach (long planId in regularPlans)
            if (!await db.StorePriceVersions.AnyAsync(
                    v => v.IdProduct == id && v.IdAccessory == accessory.Id && v.IdSalesPlan == planId && v.IsActive &&
                         v.PriceBeforeDiscountAmount == input.PriceAmount, cancellationToken))
                await priceVersions.AppendAsync(id, planId, null, accessory.Id, input.PriceAmount, 0,
                    long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value),
                    cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Success(new { message = "متعلقهٔ کالا ذخیره شد." });
    }

    private static bool ValidAccessory(ProductAccessoryInput input) => input is not null &&
                                                                       !string.IsNullOrWhiteSpace(input.Name) &&
                                                                       input.Name.Trim().Length <= 300 &&
                                                                       (input.Description?.Trim().Length ?? 0) <=
                                                                       1000 && input.SortOrder >= 0 &&
                                                                       input.PriceAmount >= 0 &&
                                                                       input.PriceAmount <= 9007199254740991m &&
                                                                       input.PriceAmount ==
                                                                       decimal.Truncate(input.PriceAmount) &&
                                                                       (!input.DisplayDiscountPercent.HasValue ||
                                                                        (input.DisplayDiscountPercent.Value is >= 0
                                                                             and < 100 &&
                                                                         decimal.Round(
                                                                             input.DisplayDiscountPercent.Value, 2) ==
                                                                         input.DisplayDiscountPercent.Value));

    public async Task<AdminOperationResult> ProductEditor(long id, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        var product = await db.Products.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new
                { x.Id, code = x.ProductKey, Name = x.NameFa, EnglishName = x.NameEn, x.IdBrand, x.IdCategory, x.MaxPurchaseQuantity })
            .SingleOrDefaultAsync(cancellationToken);
        if (product is null) return Missing();
        var price = await RegularProductPrices().FirstOrDefaultAsync(x => x.IdProduct == id, cancellationToken);

        var brands = await db.StoreBrands.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.NameFa)
            .Select(x => new EditorOption { Id = x.Id, Name = x.NameFa }).ToListAsync(cancellationToken);
        var categories = await GetLeafCategoryOptions(cancellationToken);

        var productUnitLinks = await db.WarehouseProductUnits.AsNoTracking().Where(x => x.IdProduct == id)
            .Select(x => new { x.IdUnit, x.ConversionFactor, x.IsBaseUnit, x.IsActive }).ToListAsync(cancellationToken);
        long? baseUnitId = productUnitLinks.Where(x => x.IsActive && x.IsBaseUnit).Select(x => (long?)x.IdUnit)
            .SingleOrDefault();
        if (!baseUnitId.HasValue) return Conflict(new { title = "واحد پایهٔ فعال برای کالا تعریف نشده است." });
        long[] historicalUnitIds = productUnitLinks.Where(x => !x.IsActive).Select(x => x.IdUnit).ToArray();
        var units = await db.WarehouseUnits.AsNoTracking()
            .Where(x => x.IdParent != null && (x.IsActive || x.Id == baseUnitId.Value)).OrderBy(x => x.NameFa)
            .Where(x => x.Id == baseUnitId.Value || !historicalUnitIds.Contains(x.Id))
            .Select(x => new { x.Id, Name = x.IsActive ? x.NameFa : x.NameFa + " (غیرفعال)", x.IsActive })
            .ToListAsync(cancellationToken);
        var unitConversions = await db.WarehouseProductUnits
            .AsNoTracking()
            .Join(
                db.WarehouseUnits.AsNoTracking(),
                link => link.IdUnit,
                unit => unit.Id,
                (link, unit) => new { link, unit })
            .Where(linkUnit => linkUnit.link.IdProduct == id && linkUnit.link.IsActive && !linkUnit.link.IsBaseUnit)
            .OrderBy(linkUnit => linkUnit.unit.NameFa)
            .Select(linkUnit => new
            {
                linkUnit.link.IdUnit,
                UnitName = linkUnit.unit.NameFa,
                linkUnit.link.ConversionFactor
            })
            .ToListAsync(cancellationToken);
        bool hasVouchers = await HasVouchers(id, cancellationToken);
        return Success(new
        {
            product = new
            {
                product.Id, product.code, product.Name, product.EnglishName, product.IdBrand, product.IdCategory,
                IdBaseUnit = baseUnitId.Value, PriceAmount = price?.PriceBeforeDiscountAmount ?? 0m,
                DiscountPercent = price?.DiscountPercent ?? 0m, FinalPriceAmount = price?.FinalPriceAmount ?? 0m,
                product.MaxPurchaseQuantity
            },
            brands, categories, units, unitConversions, unavailableUnitIds = historicalUnitIds, hasVouchers
        });
    }

    private Task<bool> HasVouchers(long productId, CancellationToken cancellationToken) =>
        db.VoucherLines
            .AsNoTracking()
            .Join(
                db.InventoryItems.AsNoTracking(),
                line => line.IdInventoryItem,
                item => item.Id,
                (line, item) => new { line, item })
            .AnyAsync(lineItem => lineItem.item.IdProduct == productId, cancellationToken);

    public async Task<AdminOperationResult> ProductDetails(long id, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking().Where(x => x.Id == id).Select(x => new
        {
            x.Id, x.ProductKey, Code = x.ProductKey, Name = x.NameFa, EnglishName = x.NameEn, x.IdBrand, x.IdCategory,
            x.MaxPurchaseQuantity,
            x.Slug, Badge = x.ProductBadge == null ? null : x.ProductBadge.Title, x.IsStoreEnabled,
            x.IsWarehouseEnabled, x.CreatedAtUtc, x.UpdatedAtUtc
        }).SingleOrDefaultAsync(cancellationToken);
        if (product is null) return Missing();
        long? baseUnitId = await db.WarehouseProductUnits.AsNoTracking()
            .Where(x => x.IdProduct == id && x.IsActive && x.IsBaseUnit).Select(x => (long?)x.IdUnit)
            .SingleOrDefaultAsync(cancellationToken);
        if (!baseUnitId.HasValue) return Conflict(new { title = "واحد پایهٔ فعال برای کالا تعریف نشده است." });
        var price = await RegularProductPrices().FirstOrDefaultAsync(x => x.IdProduct == id, cancellationToken);

        string? brandName = product.IdBrand is long brandId
            ? await db.StoreBrands.AsNoTracking().Where(x => x.Id == brandId).Select(x => x.NameFa)
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        string? unitName = await db.WarehouseUnits.AsNoTracking().Where(x => x.Id == baseUnitId.Value)
            .Select(x => x.NameFa).SingleOrDefaultAsync(cancellationToken);
        var secondaryUnits = await db.WarehouseProductUnits
            .AsNoTracking()
            .Join(
                db.WarehouseUnits.AsNoTracking(),
                link => link.IdUnit,
                unit => unit.Id,
                (link, unit) => new { link, unit })
            .Where(linkUnit => linkUnit.link.IdProduct == id && linkUnit.link.IsActive && !linkUnit.link.IsBaseUnit)
            .OrderBy(linkUnit => linkUnit.unit.NameFa)
            .Select(linkUnit => new
            {
                linkUnit.link.IdUnit,
                UnitName = linkUnit.unit.NameFa,
                linkUnit.link.ConversionFactor
            })
            .ToListAsync(cancellationToken);

        string? categoryPath = null;
        if (product.IdCategory is long categoryId)
        {
            categoryPath = await db.StoreCategories.AsNoTracking().Where(x => x.Id == categoryId)
                .Select(x => x.TreePathNames).SingleOrDefaultAsync(cancellationToken);
        }

        var variantRows = await db.ProductVariants.AsNoTracking().Where(x => x.IdProduct == id)
            .OrderByDescending(x => x.IsDefault).ThenBy(x => x.VariantName.Name)
            .Select(x => new
                { x.Id, Name = x.VariantName.Name, x.IsDefault, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc })
            .ToListAsync(cancellationToken);
        long[] variantIds = variantRows.Select(x => x.Id).ToArray();
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
                    linkValueAttribute.attribute.Key,
                    linkValueAttribute.attribute.Label,
                    linkValueAttribute.value.ValueKey,
                    linkValueAttribute.value.DisplayValue,
                    linkValueAttribute.value.Swatch
                })
            .ToListAsync(cancellationToken);
        var attributesByVariant = variantAttributes.GroupBy(x => x.IdProductVariant).ToDictionary(x => x.Key,
            x => x.Select(a => new
                    { key = a.Key, label = a.Label, value = a.ValueKey, display = a.DisplayValue, swatch = a.Swatch })
                .ToArray());
        var variants = variantRows.Select(x => new
        {
            x.Id, x.Name, x.IsDefault, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc,
            Attributes = attributesByVariant.GetValueOrDefault(x.Id) ?? []
        }).ToList();
        bool fullAccess = User.IsInRole("system.full-access");
        long[] allowedWarehouseIds = User.FindAll("admin_warehouse")
            .Select(claim => long.TryParse(claim.Value, out long warehouseId) ? warehouseId : 0)
            .Where(warehouseId => warehouseId > 0).ToArray();
        var inventory = await db.StockBalances
            .AsNoTracking()
            .Join(
                db.InventoryItems.AsNoTracking(),
                balance => balance.IdInventoryItem,
                item => item.Id,
                (balance, item) => new { balance, item })
            .Join(
                db.Warehouses.AsNoTracking(),
                balanceItem => balanceItem.balance.IdWarehouse,
                warehouse => warehouse.Id,
                (balanceItem, warehouse) => new { balanceItem.balance, balanceItem.item, warehouse })
            .Join(
                db.Companies.AsNoTracking(),
                balanceItemWarehouse => balanceItemWarehouse.warehouse.IdCompany,
                company => company.Id,
                (balanceItemWarehouse, company) => new
                {
                    balanceItemWarehouse.balance,
                    balanceItemWarehouse.item,
                    balanceItemWarehouse.warehouse,
                    company
                })
            .Join(
                db.SalesPlanReferences.AsNoTracking(),
                balanceDetails => balanceDetails.balance.IdSalesPlanReference,
                plan => plan.Id,
                (balanceDetails, plan) => new
                {
                    balanceDetails.balance,
                    balanceDetails.item,
                    balanceDetails.warehouse,
                    balanceDetails.company,
                    plan
                })
            .Where(balanceDetails => balanceDetails.item.IdProduct == id &&
                                     (fullAccess || allowedWarehouseIds.Contains(balanceDetails.balance.IdWarehouse)))
            .OrderBy(balanceDetails => balanceDetails.company.Name)
            .ThenBy(balanceDetails => balanceDetails.warehouse.Name)
            .ThenBy(balanceDetails => balanceDetails.plan.DisplayName)
            .Select(balanceDetails => new
            {
                CompanyName = balanceDetails.company.Name,
                WarehouseName = balanceDetails.warehouse.Name,
                WarehouseCode = balanceDetails.warehouse.Code,
                SalesPlanName = balanceDetails.plan.DisplayName,
                VariantName = balanceDetails.item.ProductVariant == null
                    ? null
                    : balanceDetails.item.ProductVariant.VariantName.Name,
                balanceDetails.balance.Quantity,
                balanceDetails.balance.MinimumQuantity,
                balanceDetails.balance.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);
        var benefits = await db.StoreProductBenefits.AsNoTracking().Where(x => x.IdProduct == id)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new { x.Title, x.Detail, x.Icon, x.SortOrder, x.IsActive }).ToListAsync(cancellationToken);
        var specifications = await db.StoreProductCatalogFeatureValues
            .AsNoTracking()
            .Join(
                db.StoreCatalogFeatures.AsNoTracking(),
                selected => selected.IdFeature,
                feature => feature.Id,
                (selected, feature) => new { selected, feature })
            .Join(
                db.StoreCatalogFeatureValues.AsNoTracking(),
                selectedFeature => selectedFeature.selected.IdFeatureValue,
                value => value.Id,
                (selectedFeature, value) => new { selectedFeature.selected, selectedFeature.feature, value })
            .Where(selectedFeatureValue => selectedFeatureValue.selected.IdProduct == id &&
                                          selectedFeatureValue.feature.IsActive &&
                                          selectedFeatureValue.value.IsActive)
            .OrderBy(selectedFeatureValue => selectedFeatureValue.feature.Name)
            .ThenBy(selectedFeatureValue => selectedFeatureValue.value.Name)
            .Select(selectedFeatureValue => new
            {
                Feature = selectedFeatureValue.feature.Name,
                Value = selectedFeatureValue.value.Name
            })
            .ToListAsync(cancellationToken);
        var assets = await db.StoreProductAssets.AsNoTracking().Where(x => x.IdProduct == id).OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var documents = assets.Where(x => x.IdMediaType == ProductMediaTypes.Document)
            .OrderByDescending(x => x.CreatedAtUtc).Select(x => new { x.Title, x.Path, x.CreatedAtUtc }).ToList();
        var media = assets.FirstOrDefault(x => x.IdMediaType == ProductMediaTypes.Image && x.IsDefault);
        var imageList = assets.Where(x => x.IdMediaType == ProductMediaTypes.Image && !x.IsDefault)
            .Select(x => new { src = x.Path, thumbnail = x.ThumbnailPath }).ToList();
        var videoList = assets.Where(x => x.IdMediaType == ProductMediaTypes.Video).Select(x => x.Path).ToList();

        object? accessories = null,
            unitHistory = null,
            attributeDefinitions = null,
            currentPlanPrices = null,
            priceHistory = null,
            salesOffers = null;
        if (fullAccess)
        {
            accessories = await db.StoreProductAccessories
                .AsNoTracking()
                .Join(
                    db.StoreAccessories.AsNoTracking(),
                    link => link.IdAccessory,
                    accessory => accessory.Id,
                    (link, accessory) => new { link, accessory })
                .Where(linkAccessory => linkAccessory.link.IdProduct == id)
                .OrderBy(linkAccessory => linkAccessory.link.SortOrder)
                .ThenBy(linkAccessory => linkAccessory.link.Id)
                .Select(linkAccessory => new
                {
                    linkAccessory.accessory.Name,
                    linkAccessory.accessory.Description,
                    linkAccessory.accessory.IsActive,
                    linkAccessory.link.IsRequired,
                    linkAccessory.link.DisplayDiscountPercent,
                    linkAccessory.link.SortOrder
                })
                .ToListAsync(cancellationToken);
            unitHistory = await db.WarehouseProductUnits
                .AsNoTracking()
                .Join(
                    db.WarehouseUnits.AsNoTracking(),
                    link => link.IdUnit,
                    unit => unit.Id,
                    (link, unit) => new { link, unit })
                .Where(linkUnit => linkUnit.link.IdProduct == id && !linkUnit.link.IsActive)
                .OrderBy(linkUnit => linkUnit.unit.NameFa)
                .Select(linkUnit => new
                {
                    UnitName = linkUnit.unit.NameFa,
                    linkUnit.link.ConversionFactor,
                    linkUnit.link.IsBaseUnit
                })
                .ToListAsync(cancellationToken);
            var attributeRows = await db.ProductAttributes.AsNoTracking().Where(x => x.IdProduct == id)
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Select(x => new { x.Id, x.Label, x.Key, x.IsActive })
                .ToListAsync(cancellationToken);
            var attributeValues = await db.ProductAttributeValues.AsNoTracking().Where(x => x.IdProduct == id)
                .OrderBy(x => x.Id)
                .Select(x => new { x.IdProductAttribute, x.Label, x.DisplayValue, x.ValueKey, x.IsActive })
                .ToListAsync(cancellationToken);
            attributeDefinitions = attributeRows.Select(x => new
            {
                x.Label, x.Key, x.IsActive,
                Values = attributeValues.Where(v => v.IdProductAttribute == x.Id)
                    .Select(v => new { v.Label, v.DisplayValue, v.ValueKey, v.IsActive }).ToArray()
            }).ToArray();
            salesOffers = await db.StoreProductSalesPlanOffers
                .AsNoTracking()
                .Join(
                    db.SalesPlanReferences.AsNoTracking(),
                    offer => offer.IdSalesPlan,
                    plan => plan.Id,
                    (offer, plan) => new { offer, plan })
                .Where(offerPlan => offerPlan.offer.IdProduct == id)
                .OrderBy(offerPlan => offerPlan.plan.DisplayName)
                .Select(offerPlan => new
                {
                    SalesPlanName = offerPlan.plan.DisplayName,
                    offerPlan.offer.IsEnabled,
                    offerPlan.offer.MaxPurchaseQuantity
                })
                .ToListAsync(cancellationToken);
            var prices = db.StorePriceVersions
                .AsNoTracking()
                .Join(
                    db.SalesPlanReferences.AsNoTracking(),
                    version => version.IdSalesPlan,
                    plan => (long?)plan.Id,
                    (version, plan) => new { version, plan })
                .GroupJoin(
                    db.ProductVariants.AsNoTracking(),
                    versionPlan => versionPlan.version.IdVariant,
                    variant => (long?)variant.Id,
                    (versionPlan, variants) => new { versionPlan, variants })
                .SelectMany(
                    versionPlanVariants => versionPlanVariants.variants.DefaultIfEmpty(),
                    (versionPlanVariants, variant) => new { versionPlanVariants.versionPlan, variant })
                .GroupJoin(
                    db.StoreAccessories.AsNoTracking(),
                    versionPlanVariant => versionPlanVariant.versionPlan.version.IdAccessory,
                    accessory => (long?)accessory.Id,
                    (versionPlanVariant, accessories) => new { versionPlanVariant, accessories })
                .SelectMany(
                    versionPlanAccessories => versionPlanAccessories.accessories.DefaultIfEmpty(),
                    (versionPlanAccessories, accessory) => new
                    {
                        versionPlanAccessories.versionPlanVariant.versionPlan.version,
                        versionPlanAccessories.versionPlanVariant.versionPlan.plan,
                        versionPlanAccessories.versionPlanVariant.variant,
                        accessory
                    })
                .Where(priceDetails => priceDetails.version.IdProduct == id)
                .Select(priceDetails => new
                {
                    priceDetails.version.Id,
                    SalesPlanName = priceDetails.plan.DisplayName,
                    VariantName = priceDetails.variant == null ? null : priceDetails.variant.VariantName.Name,
                    AccessoryName = priceDetails.accessory == null ? null : priceDetails.accessory.Name,
                    PriceVersionAmount = priceDetails.version.PriceBeforeDiscountAmount,
                    priceDetails.version.DiscountPercent,
                    priceDetails.version.FinalPriceAmount,
                    priceDetails.version.IsActive,
                    priceDetails.version.CreatedAtUtc,
                    priceDetails.version.IdChangedBy,
                    priceDetails.version.ChangeReason
                });
            currentPlanPrices = await prices.Where(x => x.IsActive).OrderBy(x => x.SalesPlanName)
                .ToListAsync(cancellationToken);
            priceHistory = await prices.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).Take(200)
                .ToListAsync(cancellationToken);
        }

        return Success(new
        {
            product.Id,
            product.ProductKey,
            Code = product.ProductKey,
            Name = product.Name,
            EnglishName = product.EnglishName,
            BrandName = brandName,
            CategoryName = categoryPath,
            UnitName = unitName,
            IdBaseUnit = baseUnitId.Value,
            SecondaryUnits = secondaryUnits,
            product.IdBrand,
            product.IdCategory,
            PriceAmount = FinalPrice(price),
            PriceBeforeDiscountAmount = price?.PriceBeforeDiscountAmount ?? 0m,
            DiscountPercent = price?.DiscountPercent ?? 0m,
            FinalPriceAmount = FinalPrice(price),
            product.MaxPurchaseQuantity,
            product.Slug,
            product.Badge,
            product.IsStoreEnabled,
            product.IsWarehouseEnabled,
            product.CreatedAtUtc,
            product.UpdatedAtUtc,
            Variants = variants,
            Accessories = accessories,
            UnitHistory = unitHistory,
            AttributeDefinitions = attributeDefinitions,
            SalesOffers = salesOffers,
            CurrentPlanPrices = currentPlanPrices,
            PriceHistory = priceHistory,
            StockByWarehouseAndPlan = inventory,
            Benefits = benefits,
            Specifications = specifications,
            Documents = documents,
            Media = media is null && imageList.Count == 0 && videoList.Count == 0
                ? null
                : new
                {
                    MainPath = media?.Path,
                    CardPath = media?.CardPath,
                    ThumbnailPath = media?.ThumbnailPath,
                    ImageList = JsonSerializer.Serialize(imageList),
                    VideoList = JsonSerializer.Serialize(videoList)
                }
        });
    }

    public async Task<AdminOperationResult> UpdateProduct(long id, ProductEditorInput input,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        string name = input.Name?.Trim() ?? "",
            englishName = input.EnglishName?.Trim() ?? "",
            key = input.Key?.Trim() ?? "";
        if (id < 1 || key.Length is < 1 or > 100 || name.Length is < 1 or > 300 || englishName.Length is < 1 or > 300 ||
            input.BrandId < 1 || input.CategoryId < 1 || input.UnitId < 1 ||
            !ValidUnitConversions(input.UnitConversions, input.UnitId) || input.PriceAmount < 0 ||
            input.PriceAmount > 9007199254740991m || input.PriceAmount != decimal.Truncate(input.PriceAmount) ||
            input.DiscountPercent is < 0 or >= 100 ||
            decimal.Round(input.DiscountPercent, 2) != input.DiscountPercent ||
            input.MaxPurchaseQuantity is < 1 or > 99)
            return Invalid(new { title = "نام، دسته، برند، قیمت، تخفیف یا حداکثر خرید معتبر نیست." });

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        bool validCategory = await db.StoreCategories.AnyAsync(c => c.Id == input.CategoryId && c.IsActive &&
                                                                    !db.StoreCategories.Any(child =>
                                                                        child.IsActive &&
                                                                        child.IdParentCategory == c.Id),
            cancellationToken);
        bool validBrand = await db.StoreBrands.AnyAsync(x => x.Id == input.BrandId && x.IsActive, cancellationToken);
        bool validUnits =
            await ActiveUnitsExistAsync(input.UnitConversions?.Select(x => x.UnitId) ?? [], cancellationToken);
        if (!validCategory || !validBrand || !validUnits)
            return Invalid(new { title = "برند، دسته یا واحد انتخاب‌شده معتبر نیست." });

        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (product is null) return Missing();
        if (product.IdCategory != input.CategoryId)
        {
            var nodes = await db.StoreCategories.AsNoTracking().Select(x => new { x.Id, x.IdParentCategory })
                .ToDictionaryAsync(x => x.Id, cancellationToken);
            var ancestors = new HashSet<long>();
            long? current = input.CategoryId;
            while (current is long categoryId && nodes.TryGetValue(categoryId, out var node) &&
                   ancestors.Add(categoryId)) current = node.IdParentCategory;
            if (current is not null) return Conflict(new { title = "ساختار دستهٔ انتخاب‌شده معتبر نیست." });
            var allowed = (await db.StoreCategoryCatalogFeatures.Where(x => ancestors.Contains(x.IdCategory))
                .Select(x => x.IdFeature).Distinct().ToListAsync(cancellationToken)).ToHashSet();
            var displayFeatures = await db.StoreProductCatalogFeatureValues.Where(x => x.IdProduct == id)
                .Select(x => x.IdFeature).ToListAsync(cancellationToken);
            var variantFeatures = await db.StoreVariantCatalogFeatureValues
                .AsNoTracking()
                .Join(
                    db.ProductVariants.AsNoTracking(),
                    selection => selection.IdProductVariant,
                    variant => variant.Id,
                    (selection, variant) => new { selection, variant })
                .Where(selectionVariant => selectionVariant.variant.IdProduct == id)
                .Select(selectionVariant => selectionVariant.selection.IdFeature)
                .ToListAsync(cancellationToken);
            if (displayFeatures.Concat(variantFeatures).Any(featureId => !allowed.Contains(featureId)))
                return Conflict(new
                    { title = "دستهٔ جدید باید همهٔ ویژگی‌های ثبت‌شدهٔ کالا و تنوع‌های آن را داشته باشد." });
        }

        var unitLinks = await db.WarehouseProductUnits.Where(x => x.IdProduct == id).ToListAsync(cancellationToken);
        long? currentBaseUnitId = unitLinks.Where(x => x.IsBaseUnit && x.IsActive).Select(x => (long?)x.IdUnit)
            .SingleOrDefault();
        if (!currentBaseUnitId.HasValue) return Conflict(new { title = "واحد پایهٔ فعال برای کالا تعریف نشده است." });
        bool baseUnitChanged = input.UnitId != currentBaseUnitId.Value;
        if (baseUnitChanged)
        {
            if (!await db.WarehouseUnits.AnyAsync(x => x.Id == input.UnitId && x.IdParent != null && x.IsActive,
                    cancellationToken))
                return Invalid(new { title = "واحد پایهٔ انتخاب‌شده فعال نیست." });
            if (await HasVouchers(id, cancellationToken))
                return Conflict(new { title = "برای این کالا سند انبار ثبت شده است؛ تغییر واحد پایه مجاز نیست." });
        }

        var baseLink = unitLinks.SingleOrDefault(x => x.IdUnit == input.UnitId);
        if (baseUnitChanged && baseLink is { IsActive: false })
            return Conflict(new { title = "واحد غیرفعال‌شدهٔ این کالا قابل فعال‌سازی مجدد نیست." });

        bool createBaseLink = baseLink is null;
        if (baseLink is null)
        {
            baseLink = new Shop.Data.Domain.Warehouse.Entities.WarehouseProductUnit
                { IdProduct = id, IdUnit = input.UnitId };
            unitLinks.Add(baseLink);
            db.WarehouseProductUnits.Add(baseLink);
        }

        if (baseLink.IsActive || createBaseLink)
        {
            baseLink.IsActive = true;
            baseLink.IsBaseUnit = true;
            baseLink.ConversionFactor = 1m;
        }

        HashSet<long>? requestedConversionIds = input.UnitConversions is null
            ? null
            : input.UnitConversions.Select(x => x.UnitId).ToHashSet();
        foreach (var link in unitLinks.Where(x => x.IdUnit != input.UnitId))
        {
            link.IsBaseUnit = false;
            if ((baseUnitChanged && requestedConversionIds is null) ||
                (requestedConversionIds is not null && !requestedConversionIds.Contains(link.IdUnit)))
                link.IsActive = false;
        }

        foreach (ProductUnitConversionInput conversion in input.UnitConversions ?? [])
        {
            var link = unitLinks.SingleOrDefault(x => x.IdUnit == conversion.UnitId);
            if (link is { IsActive: false })
                return Conflict(new
                    { title = "واحد غیرفعال‌شدهٔ این کالا فقط در تاریخچه قابل مشاهده است و دوباره فعال نمی‌شود." });
            if (link is null)
            {
                link = new Shop.Data.Domain.Warehouse.Entities.WarehouseProductUnit
                    { IdProduct = id, IdUnit = conversion.UnitId };
                unitLinks.Add(link);
                db.WarehouseProductUnits.Add(link);
            }

            link.IsBaseUnit = false;
            link.ConversionFactor = NormalizeConversionFactor(conversion.ConversionFactor);
            link.IsActive = true;
        }

        if (await db.Products.AnyAsync(x => x.Id != id && x.ProductKey == key, cancellationToken))
            return Conflict(new { title = "کد کالا قبلاً ثبت شده است." });

        product.ProductKey = key;
        product.NameFa = name;
        product.NameEn = englishName;
        product.IdBrand = input.BrandId;
        product.IdCategory = input.CategoryId;
        product.MaxPurchaseQuantity = input.MaxPurchaseQuantity;
        product.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var regularPlans = await db.SalesPlanReferences.Where(x => x.IsRegular && x.IdStockOwnerType == 1)
            .Select(x => x.Id).ToListAsync(cancellationToken);
        if (regularPlans.Count == 0) return Conflict(new { title = "طرح فروش عادی تعریف نشده است." });
        foreach (long planId in regularPlans)
        {
            var currentPrice = await db.StorePriceVersions.SingleOrDefaultAsync(x => x.IdProduct == id &&
                x.IdSalesPlan == planId && x.IsActive &&
                x.IdVariant == null && x.IdAccessory == null, cancellationToken);
            if (currentPrice is null || currentPrice.PriceBeforeDiscountAmount != input.PriceAmount ||
                currentPrice.DiscountPercent != input.DiscountPercent)
                await priceVersions.AppendAsync(id, planId, null, null, input.PriceAmount, input.DiscountPercent,
                    long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value),
                    cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return Success(new { message = "اطلاعات کالا ذخیره شد." });
    }

    public sealed class EditorOption
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
    }

    private static bool ValidUnitConversions(IReadOnlyCollection<ProductUnitConversionInput>? conversions,
        long baseUnitId) =>
        conversions is null || (conversions.Count <= 100 &&
                                conversions.All(x =>
                                    x is not null && x.UnitId > 0 && x.UnitId != baseUnitId && x.ConversionFactor > 0 &&
                                    x.ConversionFactor <= 999999999.999999999m &&
                                    NormalizeConversionFactor(x.ConversionFactor) > 0) &&
                                conversions.Select(x => x.UnitId).Distinct().Count() == conversions.Count);

    private static decimal NormalizeConversionFactor(decimal factor) =>
        decimal.Truncate(factor * 1_000_000_000m) / 1_000_000_000m;

    private async Task<bool> ActiveUnitsExistAsync(IEnumerable<long> unitIds, CancellationToken cancellationToken)
    {
        long[] ids = unitIds.Distinct().ToArray();
        return ids.Length == 0 ||
               await db.WarehouseUnits.CountAsync(x => ids.Contains(x.Id) && x.IdParent != null && x.IsActive,
                   cancellationToken) == ids.Length;
    }

    public async Task<AdminOperationResult> CreateProductBadge(ProductBadgeInput input,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();
        string title = input.Title?.Trim() ?? "";
        if (title.Length is < 1 or > 100) return Invalid(new { title = "عنوان نشان باید بین ۱ تا ۱۰۰ نویسه باشد." });
        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        if (await db.StoreProductBadges.AnyAsync(x => x.Title == title, cancellationToken))
            return Conflict(new { title = "این نشان قبلاً ثبت شده است." });
        var badge = new StoreProductBadge { Title = title };
        db.StoreProductBadges.Add(badge);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Success(new { badge.Id, badge.Title });
    }

    public async Task<AdminOperationResult> ProductBenefits(long id, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        if (!await db.Products.AnyAsync(x => x.Id == id, cancellationToken)) return Missing();

        var items = await db.StoreProductBenefits.AsNoTracking().Where(x => x.IdProduct == id).OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .Select(x => new ProductBenefitRow(x.Id, x.Title, x.Detail, x.Icon, x.SortOrder, x.IsActive))
            .ToListAsync(cancellationToken);

        return Success(items);
    }

    public async Task<AdminOperationResult> CreateProductBenefit(long id, ProductBenefitInput input,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        if (!TryNormalizeBenefit(input, out var value, out string error)) return Invalid(new { title = error });

        if (!await db.Products.AnyAsync(x => x.Id == id, cancellationToken)) return Missing();

        var benefit = new StoreProductBenefit
        {
            IdProduct = id,
            Title = value.Title,
            Detail = value.Detail,
            Icon = value.Icon,
            SortOrder = value.SortOrder,
            IsActive = value.IsActive
        };
        db.StoreProductBenefits.Add(benefit);
        await db.SaveChangesAsync(cancellationToken);

        return Success(new { id = benefit.Id, message = "مزیت کالا ثبت شد." });
    }

    public async Task<AdminOperationResult> UpdateProductBenefit(long id, long benefitId, ProductBenefitInput input,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        if (!TryNormalizeBenefit(input, out var value, out string error)) return Invalid(new { title = error });

        var benefit =
            await db.StoreProductBenefits.SingleOrDefaultAsync(x => x.Id == benefitId && x.IdProduct == id,
                cancellationToken);
        if (benefit is null) return Missing();

        benefit.Title = value.Title;
        benefit.Detail = value.Detail;
        benefit.Icon = value.Icon;
        benefit.SortOrder = value.SortOrder;
        benefit.IsActive = value.IsActive;
        await db.SaveChangesAsync(cancellationToken);

        return Success(new { message = "مزیت کالا ذخیره شد." });
    }

    public async Task<AdminOperationResult> DeleteProductBenefit(long id, long benefitId, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        var benefit =
            await db.StoreProductBenefits.SingleOrDefaultAsync(x => x.Id == benefitId && x.IdProduct == id,
                cancellationToken);
        if (benefit is null) return Missing();

        db.StoreProductBenefits.Remove(benefit);
        await db.SaveChangesAsync(cancellationToken);

        return Success(new { message = "مزیت کالا حذف شد." });
    }

    private static bool TryNormalizeBenefit(ProductBenefitInput input, out NormalizedProductBenefit value,
        out string error)
    {
        string title = input.Title?.Trim() ?? "";
        string detail = input.Detail?.Trim() ?? "";
        string icon = input.Icon?.Trim() ?? "";
        if (title.Length is < 1 or > 300 || title.Any(char.IsControl) || detail.Length > 1000 ||
            input.SortOrder is < 0 or > 100000 || icon.Length > 100 ||
            icon.Length > 0 && !System.Text.RegularExpressions.Regex.IsMatch(icon, "^[a-z0-9-]+$"))
        {
            value = default;
            error = "عنوان، توضیح، آیکن یا ترتیب مزیت معتبر نیست.";

            return false;
        }

        value = new(title, detail.Length == 0 ? null : detail, icon.Length == 0 ? null : icon, input.SortOrder,
            input.IsActive);
        error = "";

        return true;
    }

    public sealed record ProductBenefitRow(
        decimal Id,
        string Title,
        string? Detail,
        string? Icon,
        int SortOrder,
        bool IsActive);

    private readonly record struct NormalizedProductBenefit(
        string Title,
        string? Detail,
        string? Icon,
        int SortOrder,
        bool IsActive);

    private async Task<List<EditorOption>> GetLeafCategoryOptions(CancellationToken cancellationToken)
    {
        var nodes = await db.StoreCategories.AsNoTracking().Where(x => x.IsActive)
            .Select(x => new { x.Id, x.NameFa, x.IdParentCategory, x.TreePathNames }).ToListAsync(cancellationToken);
        var parentIds = nodes.Where(x => x.IdParentCategory.HasValue).Select(x => x.IdParentCategory!.Value)
            .ToHashSet();
        return nodes.Where(x => !parentIds.Contains(x.Id))
            .Select(node => new EditorOption { Id = node.Id, Name = node.TreePathNames })
            .OrderBy(x => x.Name, StringComparer.CurrentCulture).ToList();
    }

    public async Task<AdminOperationResult> ProductDrafts(
        string? q,
        int page = 1,
        int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        if (!AdminListQuery.TryCreate(page, pageSize, q, out AdminPageRequest paging))
            return Validation("شماره صفحه، اندازهٔ صفحه یا عبارت جست‌وجو معتبر نیست.");

        var regularPrices = RegularProductPrices();
        var drafts = db.Products
            .AsNoTracking()
            .Where(product => !product.IsStoreEnabled)
            .GroupJoin(
                db.StoreBrands.AsNoTracking(),
                product => product.IdBrand,
                brand => (long?)brand.Id,
                (product, brands) => new { product, brands })
            .SelectMany(
                productBrands => productBrands.brands.DefaultIfEmpty(),
                (productBrands, brand) => new { productBrands.product, brand })
            .GroupJoin(
                db.StoreCategories.AsNoTracking(),
                productBrand => productBrand.product.IdCategory,
                category => (long?)category.Id,
                (productBrand, categories) => new { productBrand, categories })
            .SelectMany(
                productBrandCategories => productBrandCategories.categories.DefaultIfEmpty(),
                (productBrandCategories, category) => new
                {
                    productBrandCategories.productBrand.product,
                    productBrandCategories.productBrand.brand,
                    category
                })
            .Select(productBrandCategory => new DraftProduct
            {
                Id = productBrandCategory.product.Id,
                CreatedAtUtc = productBrandCategory.product.CreatedAtUtc,
                Code = productBrandCategory.product.ProductKey,
                Name = productBrandCategory.product.NameFa,
                BrandName = productBrandCategory.brand == null ? null : productBrandCategory.brand.NameFa,
                CategoryName = productBrandCategory.category == null ? null : productBrandCategory.category.NameFa,
                PriceAmount = regularPrices.Where(price => price.IdProduct == productBrandCategory.product.Id)
                    .Select(x => (decimal?)x.FinalPriceAmount).FirstOrDefault(),
                HasImage = db.StoreProductAssets.Any(media =>
                    media.IdProduct == productBrandCategory.product.Id &&
                    media.IdMediaType == ProductMediaTypes.Image && media.IsDefault)
            });
        string query = paging.Search;
        if (query.Length > 0)
            drafts = drafts.Where(x => x.Code.Contains(query) || x.Name.Contains(query) ||
                                       (x.BrandName != null && x.BrandName.Contains(query)) ||
                                       (x.CategoryName != null && x.CategoryName.Contains(query)));
        int total = await drafts.CountAsync(cancellationToken);
        var items = await drafts.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).Skip(paging.Offset)
            .Take(paging.PageSize).ToListAsync(cancellationToken);

        return Success(AdminListQuery.CreateResult(items, total, paging));
    }

    public async Task<AdminOperationResult> ProductCreateOptions(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        var brands = await db.StoreBrands.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.NameFa)
            .Select(x => new EditorOption { Id = x.Id, Name = x.NameFa }).ToListAsync(cancellationToken);
        var categories = await GetLeafCategoryOptions(cancellationToken);
        var units = await db.WarehouseUnits.AsNoTracking().Where(x => x.IsActive && x.IdParent != null)
            .OrderBy(x => x.NameFa)
            .Select(x => new EditorOption { Id = x.Id, Name = x.NameFa }).ToListAsync(cancellationToken);

        return Success(new { brands, categories, units });
    }

    public async Task<AdminOperationResult> CreateProduct(ProductCreateInput input,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        string key = input.Key?.Trim() ?? "",
            name = input.Name?.Trim() ?? "",
            english = input.EnglishName?.Trim() ?? "";
        if (key.Length > 100 ||
            name.Length is < 1 or > 300 || english.Length is < 1 or > 300 || input.BrandId < 1 ||
            input.CategoryId < 1 || input.UnitId < 1 ||
            !ValidUnitConversions(input.UnitConversions, input.UnitId) || input.PriceAmount < 0 ||
            input.PriceAmount > 9007199254740991m || input.PriceAmount != decimal.Truncate(input.PriceAmount) ||
            input.DiscountPercent is < 0 or >= 100 ||
            decimal.Round(input.DiscountPercent, 2) != input.DiscountPercent ||
            input.MaxPurchaseQuantity is < 1 or > 99)
            return Invalid(new { title = "اطلاعات کالا معتبر نیست." });

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        long id = await db.Products.Select(x => (long?)x.Id).MaxAsync(cancellationToken) is long maxId ? maxId + 1 : 1;
        if (id > int.MaxValue) return Conflict(new { title = "ظرفیت شناسهٔ کالا تکمیل شده است." });
        if (key.Length == 0) key = id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string slug = $"product-{id}";
        while (await db.Products.AnyAsync(x => x.Slug == slug, cancellationToken))
            slug = $"product-{id}-{Guid.NewGuid():N}";

        bool validCategory = await db.StoreCategories.AnyAsync(c => c.Id == input.CategoryId && c.IsActive &&
                                                                    !db.StoreCategories.Any(child =>
                                                                        child.IsActive &&
                                                                        child.IdParentCategory == c.Id),
            cancellationToken);
        bool validBrand = await db.StoreBrands.AnyAsync(x => x.Id == input.BrandId && x.IsActive, cancellationToken);
        bool validUnits = await ActiveUnitsExistAsync(
            new[] { input.UnitId }.Concat(input.UnitConversions?.Select(x => x.UnitId) ?? []), cancellationToken);
        bool duplicate = await db.Products.AnyAsync(x => x.ProductKey == key, cancellationToken);
        if (!validCategory || !validBrand || !validUnits || duplicate)
            return Conflict(new { title = "برند، دسته یا واحد معتبر نیست یا شناسه و کد قبلاً ثبت شده‌اند." });

        db.Products.Add(new Shop.Data.Domain.Warehouse.Entities.WarehouseProduct
        {
            Id = id,
            ProductKey = key,
            NameFa = name,
            NameEn = english,
            IdBrand = input.BrandId,
            IdCategory = input.CategoryId,
            MaxPurchaseQuantity = input.MaxPurchaseQuantity,
            Slug = slug,
            IsStoreEnabled = false,
            IsWarehouseEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        db.WarehouseProductUnits.Add(new Shop.Data.Domain.Warehouse.Entities.WarehouseProductUnit
        {
            IdProduct = id,
            IdUnit = input.UnitId,
            ConversionFactor = 1m,
            IsBaseUnit = true,
            IsActive = true
        });
        foreach (ProductUnitConversionInput conversion in input.UnitConversions ?? [])
            db.WarehouseProductUnits.Add(new Shop.Data.Domain.Warehouse.Entities.WarehouseProductUnit
            {
                IdProduct = id,
                IdUnit = conversion.UnitId,
                ConversionFactor = NormalizeConversionFactor(conversion.ConversionFactor),
                IsBaseUnit = false,
                IsActive = true
            });
        await db.SaveChangesAsync(cancellationToken);

        var regularPlans = await db.SalesPlanReferences.Where(x => x.IsRegular && x.IdStockOwnerType == 1)
            .Select(x => x.Id).ToListAsync(cancellationToken);
        if (regularPlans.Count == 0) return Conflict(new { title = "طرح فروش عادی تعریف نشده است." });
        foreach (long planId in regularPlans)
            await priceVersions.AppendAsync(id, planId, null, null, input.PriceAmount, input.DiscountPercent,
                long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value), cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Success(new { id, message = "پیش‌نویس کالا ساخته شد؛ تصویر را بارگذاری و سپس کالا را منتشر کنید." });
    }

    public async Task<AdminOperationResult> PublishProduct(long id, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id && !x.IsStoreEnabled, cancellationToken);
        if (product is null) return Conflict(new { title = "پیش‌نویس پیدا نشد یا اطلاعات و تصویر آن کامل نیست." });

        bool validCategory = product.IdCategory is long categoryId && await db.StoreCategories.AnyAsync(c =>
            c.Id == categoryId && c.IsActive &&
            !db.StoreCategories.Any(child => child.IsActive && child.IdParentCategory == c.Id), cancellationToken);
        bool validBrand = product.IdBrand is long brandId &&
                          await db.StoreBrands.AnyAsync(x => x.Id == brandId && x.IsActive, cancellationToken);
        var media = await db.StoreProductAssets.AsNoTracking().SingleOrDefaultAsync(
            x => x.IdProduct == product.Id && x.IdMediaType == ProductMediaTypes.Image && x.IsDefault,
            cancellationToken);
        bool validMedia = media is not null &&
                          media.Path.StartsWith("content/products/images/main/", StringComparison.OrdinalIgnoreCase) &&
                          media.CardPath?.StartsWith("content/products/images/card/",
                              StringComparison.OrdinalIgnoreCase) == true &&
                          media.ThumbnailPath?.StartsWith("content/products/images/thumbnail/",
                              StringComparison.OrdinalIgnoreCase) == true;
        bool hasPrice = await RegularProductPrices().AnyAsync(x => x.IdProduct == id, cancellationToken);
        bool validProduct = hasPrice && product.MaxPurchaseQuantity is >= 1 and <= 99 &&
                            !string.IsNullOrWhiteSpace(product.Slug) &&
                            !string.IsNullOrWhiteSpace(product.NameEn);
        if (!validCategory || !validBrand || !validMedia || !validProduct)
            return Conflict(new { title = "پیش‌نویس پیدا نشد یا اطلاعات و تصویر آن کامل نیست." });

        product.IsStoreEnabled = true;
        product.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Success(new { message = "کالا منتشر شد." });
    }

    public async Task<AdminOperationResult> UnpublishProduct(long id, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id && x.IsStoreEnabled, cancellationToken);
        if (product is null) return Conflict(new { title = "کالای منتشرشده پیدا نشد." });

        product.IsStoreEnabled = false;
        product.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Success(new { message = "کالا از انتشار خارج شد و در پیش‌نویس‌ها قرار گرفت." });
    }

    private static bool IsJson(string? value)
    {
        if (value is null) return true;

        try
        {
            using var _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public sealed class DraftProduct
    {
        public decimal Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string? BrandName { get; set; }
        public string? CategoryName { get; set; }
        public decimal? PriceAmount { get; set; }
        public bool HasImage { get; set; }
    }

    public async Task<AdminOperationResult> BrandCountries(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        return Success(await db.GeoCountries.AsNoTracking().OrderBy(x => x.NameFa)
            .Select(x => new CountryRow { CountryCode = x.CountryCode, NameFa = x.NameFa })
            .ToListAsync(cancellationToken));
    }

    public Task<AdminOperationResult> CreateBrand(BrandInput input, CancellationToken cancellationToken) =>
        SaveBrand(null, input, cancellationToken);

    public Task<AdminOperationResult> UpdateBrand(string key, BrandInput input, CancellationToken cancellationToken) =>
        SaveBrand(key, input, cancellationToken);

    private async Task<AdminOperationResult> SaveBrand(string? key, BrandInput input, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        string brandKey = (key ?? input.Key ?? "").Trim();
        string nameFa = input.NameFa?.Trim() ?? "", nameEn = input.NameEn?.Trim() ?? "";
        string countryCode = input.CountryCode?.Trim() ?? "";
        if (brandKey.Length is < 1 or > 100 ||
            !System.Text.RegularExpressions.Regex.IsMatch(brandKey, "^[a-z0-9]+(?:-[a-z0-9]+)*$") ||
            nameFa.Length is < 1 or > 200 || nameEn.Length is < 1 or > 200 || countryCode.Length > 10)
            return Invalid(new { title = "شناسه، نام یا کشور برند معتبر نیست." });

        if (countryCode.Length > 0)
        {
            if (!await db.GeoCountries.AnyAsync(x => x.CountryCode == countryCode, cancellationToken))
                return Invalid(new { title = "کشور انتخاب‌شده معتبر نیست." });
        }

        long? countryId = countryCode.Length == 0
            ? null
            : await db.GeoCountries
                .Where(x => x.CountryCode == countryCode).Select(x => (long?)x.Id)
                .SingleOrDefaultAsync(cancellationToken);
        try
        {
            var brand = key is null
                ? null
                : await db.StoreBrands.SingleOrDefaultAsync(x => x.BrandKey == brandKey, cancellationToken);
            if (key is not null && brand is null) return Missing();

            if (brand is null)
            {
                brand = new Shop.Data.Domain.Store.Entities.StoreBrand { BrandKey = brandKey, IsActive = true };
                db.StoreBrands.Add(brand);
            }

            brand.NameFa = nameFa;
            brand.NameEn = nameEn;
            brand.IdCountry = countryId;

            if (input.ShowInStore.HasValue) brand.ShowInStore = input.ShowInStore.Value;
            await db.SaveChangesAsync(cancellationToken);

            return Success(new { message = "برند ذخیره شد." });
        }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Conflict(new { title = "این شناسهٔ برند قبلاً ثبت شده است." });
        }
    }

    public sealed class CountryRow
    {
        public string CountryCode { get; set; } = "";
        public string NameFa { get; set; } = "";
    }

    public async Task<AdminOperationResult> CategoryOptions(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        return Success(await db.StoreCategories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.NameFa)
            .Select(x => new CategoryOption(x.Id, x.CategoryKey, x.NameFa, x.NameEn, x.IdParentCategory, x.TreePathIds,
                x.TreePathNames)).ToListAsync(cancellationToken));
    }

    public Task<AdminOperationResult> CreateCategory(CategoryInput input, CancellationToken cancellationToken) =>
        SaveCategory(null, input, cancellationToken);

    public Task<AdminOperationResult> UpdateCategory(string key, CategoryInput input,
        CancellationToken cancellationToken) => SaveCategory(key, input, cancellationToken);

    private async Task<AdminOperationResult> SaveCategory(string? key, CategoryInput input,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        string categoryKey = (key ?? input.Key ?? "").Trim(),
            nameFa = input.NameFa?.Trim() ?? "",
            nameEn = input.NameEn?.Trim() ?? "";
        if (categoryKey.Length is < 1 or > 150 ||
            !System.Text.RegularExpressions.Regex.IsMatch(categoryKey, "^[a-z0-9]+(?:-[a-z0-9]+)*$") ||
            nameFa.Length is < 1 or > 200 || nameEn.Length is < 1 or > 200 ||
            !nameEn.Any(c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z') ||
            nameFa.Any(char.IsControl) || nameEn.Any(char.IsControl) || input.ParentId is <= 0)
            return Invalid(new { title = "شناسه یا نام دسته معتبر نیست." });

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var nodes = await db.StoreCategories.ToListAsync(cancellationToken);
        var current = key is null ? null : nodes.SingleOrDefault(x => x.CategoryKey == categoryKey && x.IsActive);
        if (key is not null && current is null) return Missing();

        if (current is null &&
            nodes.Any(x => string.Equals(x.CategoryKey, categoryKey, StringComparison.OrdinalIgnoreCase)))
            return Conflict(new { title = "شناسهٔ دسته قبلاً ثبت شده است." });

        long? parentId = current?.IdParentCategory ?? input.ParentId;
        if (current is not null && input.ParentId != current.IdParentCategory)
            return Invalid(new { title = "تغییر والد دسته از این فرم مجاز نیست." });

        var parent = parentId is null ? null : nodes.SingleOrDefault(x => x.Id == parentId && x.IsActive);
        if (parentId is not null && parent is null)
            return Invalid(new { title = "دستهٔ والد فعال نیست یا وجود ندارد." });

        if (nodes.Any(x =>
                x.IsActive && x.Id != current?.Id && x.IdParentCategory == parentId &&
                string.Equals(x.NameFa, nameFa, StringComparison.Ordinal)))
            return Conflict(new { title = "دسته‌ای با این نام در همین سطح وجود دارد." });

        if (current is null && parent is not null)
        {
            bool hasPublishedProducts =
                await db.Products.AnyAsync(x => x.IdCategory == parent.Id && x.IsStoreEnabled, cancellationToken);
            if (hasPublishedProducts)
                return Conflict(new
                {
                    title = "برای دستهٔ دارای کالای منتشرشده نمی‌توان زیردسته ساخت؛ ابتدا دستهٔ کالاها را تغییر دهید."
                });
        }

        try
        {
            if (current is null)
            {
                var parentPath = parent is null
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(parent.PathJson) ?? throw new JsonException();
                parentPath.Add(nameFa);
                string path = JsonSerializer.Serialize(parentPath);
                db.StoreCategories.Add(new Shop.Data.Domain.Store.Entities.StoreCategory
                {
                    CategoryKey = categoryKey,
                    NameEn = nameEn,
                    NameFa = nameFa,
                    PathJson = path,
                    IdParentCategory = parentId,
                    SortOrder = parentPath.Count,
                    IsActive = true
                });
            }
            else
            {
                var oldPath = JsonSerializer.Deserialize<List<string>>(current.PathJson) ?? throw new JsonException();
                if (oldPath.Count == 0 || oldPath[^1] != current.NameFa)
                    return Conflict(new { title = "مسیر دسته ناسازگار است؛ ابتدا دادهٔ دسته اصلاح شود." });

                var descendants = new List<Shop.Data.Domain.Store.Entities.StoreCategory> { current };
                for (int i = 0; i < descendants.Count; i++)
                {
                    foreach (var child in nodes.Where(x => x.IdParentCategory == descendants[i].Id))
                    {
                        if (descendants.Any(x => x.Id == child.Id))
                            return Conflict(new { title = "ساختار دسته‌ها دارای چرخه است." });

                        descendants.Add(child);
                    }
                }

                foreach (var node in descendants)
                {
                    var path = JsonSerializer.Deserialize<List<string>>(node.PathJson) ?? throw new JsonException();
                    if (path.Count < oldPath.Count || !path.Take(oldPath.Count).SequenceEqual(oldPath))
                        return Conflict(new { title = "مسیر یکی از زیردسته‌ها ناسازگار است." });

                    path[oldPath.Count - 1] = nameFa;
                    string newPath = JsonSerializer.Serialize(path);
                    node.PathJson = newPath;
                }

                current.NameFa = nameFa;
                current.NameEn = nameEn;
            }

            await db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (JsonException)
        {
            return Conflict(new { title = "مسیر دسته ناسازگار است." });
        }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Conflict(new { title = "شناسهٔ دسته قبلاً ثبت شده است." });
        }

        return Success(new { message = "دسته ذخیره شد." });
    }

    public sealed record CategoryOption(
        long Id,
        string CategoryKey,
        string NameFa,
        string NameEn,
        long? IdParentCategory,
        string TreePathIds,
        string TreePathNames);

    public async Task<AdminOperationResult> AttributeEditor(string key, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        string attributeKey = key.Trim();
        if (attributeKey.Length is < 1 or > 100) return Missing();

        var item = await db.StoreAttributeDefinitions.AsNoTracking().Where(x => x.AttributeKey == attributeKey)
            .Select(x => new AttributeDefinitionRow(x.AttributeKey, x.Label, x.Icon, x.SourceKey,
                x.FilterKey, x.IsFilterable, x.IsHighlight, x.IsActive)).SingleOrDefaultAsync(cancellationToken);

        return item is null ? Missing() : Success(item);
    }

    public async Task<AdminOperationResult> CreateAttribute(AttributeDefinitionInput input,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        if (!TryNormalizeAttribute(input, out var normalized, out var error)) return Invalid(new { title = error });

        if (await db.StoreAttributeDefinitions.AnyAsync(x => x.AttributeKey == normalized.Key, cancellationToken))
            return Conflict(new { title = "این کلید ویژگی قبلاً ثبت شده است." });

        db.StoreAttributeDefinitions.Add(new Shop.Data.Domain.Store.Entities.StoreAttributeDefinition
        {
            AttributeKey = normalized.Key,
            Label = normalized.Label,
            Icon = normalized.Icon,
            SourceKey = normalized.SourceKey,
            FilterKey = normalized.FilterKey,
            IsFilterable = normalized.IsFilterable,
            IsHighlight = normalized.IsHighlight,
            IsActive = true
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException dbError) when (dbError.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Conflict(new { title = "این کلید ویژگی قبلاً ثبت شده است." });
        }

        return Success(new { message = "ویژگی ایجاد شد." });
    }

    public async Task<AdminOperationResult> UpdateAttribute(string key, AttributeDefinitionInput input,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        string attributeKey = key.Trim();
        if (attributeKey.Length is < 1 or > 100) return Missing();

        if (!TryNormalizeAttribute(input, out var normalized, out var error)) return Invalid(new { title = error });

        var item = await db.StoreAttributeDefinitions.SingleOrDefaultAsync(x => x.AttributeKey == attributeKey,
            cancellationToken);
        if (item is null) return Missing();

        item.Label = normalized.Label;
        item.Icon = normalized.Icon;
        item.SourceKey = normalized.SourceKey;
        item.FilterKey = normalized.FilterKey;
        item.IsFilterable = normalized.IsFilterable;
        item.IsHighlight = normalized.IsHighlight;
        await db.SaveChangesAsync(cancellationToken);

        return Success(new { message = "ویژگی ذخیره شد." });
    }

    private static bool TryNormalizeAttribute(AttributeDefinitionInput input, out NormalizedAttribute value,
        out string error)
    {
        string key = input.Key?.Trim() ?? "";
        string label = input.Label?.Trim() ?? "";
        string icon = input.Icon?.Trim() ?? "";
        string source = input.SourceKey?.Trim() ?? "";
        string filter = input.FilterKey?.Trim() ?? "";
        if (key.Length is < 1 or > 100 ||
            !System.Text.RegularExpressions.Regex.IsMatch(key, "^[A-Za-z][A-Za-z0-9]*(?:-[A-Za-z0-9]+)*$") ||
            label.Length is < 1 or > 200 || label.Any(char.IsControl) ||
            icon.Length > 100 ||
            (icon.Length > 0 && !System.Text.RegularExpressions.Regex.IsMatch(icon, "^[a-z0-9-]+$")) ||
            source.Length > 100 || (source.Length > 0 &&
                                    !System.Text.RegularExpressions.Regex.IsMatch(source,
                                        "^[A-Za-z][A-Za-z0-9_:-]*$")) ||
            filter.Length > 100 || (filter.Length > 0 &&
                                    !System.Text.RegularExpressions.Regex.IsMatch(filter,
                                        "^[A-Za-z][A-Za-z0-9_-]*$")) ||
            input.IsFilterable && filter.Length == 0)
        {
            value = new NormalizedAttribute("", "", null, null, null, false, false);
            error = "کلید، عنوان، آیکن یا منبع ویژگی معتبر نیست؛ ویژگی قابل‌فیلتر باید کلید فیلتر داشته باشد.";

            return false;
        }

        value = new NormalizedAttribute(key, label, EmptyToNull(icon), EmptyToNull(source), EmptyToNull(filter),
            input.IsFilterable, input.IsHighlight);
        error = "";

        return true;
    }

    private static string? EmptyToNull(string? value) => string.IsNullOrEmpty(value) ? null : value;

    private sealed record NormalizedAttribute(
        string Key,
        string Label,
        string? Icon,
        string? SourceKey,
        string? FilterKey,
        bool IsFilterable,
        bool IsHighlight);

    public sealed record AttributeDefinitionRow(
        string Key,
        string Label,
        string? Icon,
        string? SourceKey,
        string? FilterKey,
        bool IsFilterable,
        bool IsHighlight,
        bool IsActive);

    public async Task<AdminOperationResult> SetAttributeStatus(string key, CatalogStatusInput input,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        var item = await db.StoreAttributeDefinitions.SingleOrDefaultAsync(x => x.AttributeKey == key,
            cancellationToken);
        if (item is null) return Missing();

        item.IsActive = input.IsActive;
        await db.SaveChangesAsync(cancellationToken);

        return Success(new { message = input.IsActive ? "ویژگی منتشر شد." : "ویژگی از کاتالوگ خارج شد." });
    }

    public async Task<AdminOperationResult> SetCategoryStatus(string key, CatalogStatusInput input,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        var category = await db.StoreCategories.SingleOrDefaultAsync(x => x.CategoryKey == key, cancellationToken);
        if (category is null) return Missing();

        if (!input.IsActive)
        {
            bool hasChildren = await db.StoreCategories.AnyAsync(x => x.IsActive && x.IdParentCategory == category.Id,
                cancellationToken);
            bool hasPublishedProducts = await db.Products.AnyAsync(x => x.IsStoreEnabled && x.IdCategory == category.Id,
                cancellationToken);
            if (hasChildren || hasPublishedProducts)
                return Conflict(new { title = "ابتدا زیردسته‌ها یا کالاهای منتشرشدهٔ این دسته را غیرفعال کنید." });
        }

        category.IsActive = input.IsActive;
        await db.SaveChangesAsync(cancellationToken);

        return Success(new { message = input.IsActive ? "دسته منتشر شد." : "دسته از کاتالوگ خارج شد." });
    }

    public async Task<AdminOperationResult> SetBrandStatus(string key, CatalogStatusInput input,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        var brand = await db.StoreBrands.SingleOrDefaultAsync(x => x.BrandKey == key, cancellationToken);
        if (brand is null) return Missing();

        brand.IsActive = input.IsActive;
        await db.SaveChangesAsync(cancellationToken);

        return Success(new { message = input.IsActive ? "برند منتشر شد." : "برند از کاتالوگ خارج شد." });
    }

    public async Task<AdminOperationResult> GetSection(
        string section,
        string? q,
        int page = 1,
        int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!AdminListQuery.TryCreate(page, pageSize, q, out AdminPageRequest paging))
            return Validation("شماره صفحه باید مثبت و اندازهٔ صفحه بین ۱ تا ۲۰۰ باشد.");

        if (string.Equals(section, "attributes", StringComparison.OrdinalIgnoreCase))
        {
            var attributes = db.StoreAttributeDefinitions.AsNoTracking();
            string attributeQuery = paging.Search;
            if (attributeQuery.Length > 0)
                attributes = attributes.Where(x =>
                    x.AttributeKey.Contains(attributeQuery) || x.Label.Contains(attributeQuery) ||
                    (x.SourceKey != null && x.SourceKey.Contains(attributeQuery)) ||
                    (x.FilterKey != null && x.FilterKey.Contains(attributeQuery)));
            int total = await attributes.CountAsync(cancellationToken);
            var items = await attributes.OrderBy(x => x.Id).Skip(paging.Offset).Take(paging.PageSize)
                .Select(x => new AttributeDefinitionRow(x.AttributeKey, x.Label, x.Icon, x.SourceKey, x.FilterKey,
                    x.IsFilterable, x.IsHighlight, x.IsActive))
                .ToListAsync(cancellationToken);

            return Success(AdminListQuery.CreateResult(items, total, paging));
        }

        if (string.Equals(section, "categories", StringComparison.OrdinalIgnoreCase))
        {
            var source = db.StoreCategories.AsNoTracking();
            string categoryQuery = paging.Search;
            if (categoryQuery.Length > 0)
                source = source.Where(x =>
                    x.CategoryKey.Contains(categoryQuery) || x.NameFa.Contains(categoryQuery) ||
                    x.NameEn.Contains(categoryQuery));
            int total = await source.CountAsync(cancellationToken);
            var all = await source.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Skip(paging.Offset).Take(paging.PageSize)
                .ToListAsync(cancellationToken);
            var parents = await db.StoreCategories.AsNoTracking()
                .ToDictionaryAsync(x => x.Id, x => x.CategoryKey, cancellationToken);
            var rows = all.Select(x => new CategoryRow(x.CategoryKey, x.NameFa, x.NameEn, x.CategoryKey,
                x.IdParentCategory is long parentId && parents.TryGetValue(parentId, out string? parentKey)
                    ? parentKey
                    : "", x.IsActive, x.TreePathIds, x.TreePathNames)).ToArray();

            return Success(AdminListQuery.CreateResult(rows, total, paging));
        }

        if (string.Equals(section, "brands", StringComparison.OrdinalIgnoreCase))
        {
            var brands = db.StoreBrands.AsNoTracking();
            string brandQuery = paging.Search;
            if (brandQuery.Length > 0)
                brands = brands.Where(x =>
                    x.BrandKey.Contains(brandQuery) || x.NameFa.Contains(brandQuery) || x.NameEn.Contains(brandQuery));
            int total = await brands.CountAsync(cancellationToken);
            var rows = await brands
                .GroupJoin(
                    db.GeoCountries.AsNoTracking(),
                    brand => brand.IdCountry,
                    country => (long?)country.Id,
                    (brand, countries) => new { brand, countries })
                .SelectMany(
                    brandCountries => brandCountries.countries.DefaultIfEmpty(),
                    (brandCountries, country) => new { brandCountries.brand, country })
                .OrderBy(brandCountry => brandCountry.brand.NameFa)
                .ThenBy(brandCountry => brandCountry.brand.BrandKey)
                .Select(brandCountry => new BrandRow(
                    brandCountry.brand.BrandKey,
                    brandCountry.brand.NameFa,
                    brandCountry.brand.NameEn,
                    brandCountry.country == null ? "" : brandCountry.country.CountryCode,
                    brandCountry.brand.LogoPath,
                    brandCountry.brand.IsActive,
                    brandCountry.brand.ShowInStore))
                .Skip(paging.Offset)
                .Take(paging.PageSize)
                .ToListAsync(cancellationToken);
            string cdnBaseUrl = await mediaCdn.GetBaseUrlAsync(cancellationToken);
            var items = rows.Select(row => row with { LogoUrl = ContentCdn.ToUrl(row.LogoPath, cdnBaseUrl) }).ToArray();

            return Success(AdminListQuery.CreateResult(items, total, paging));
        }

        string query = paging.Search;
        if (!string.Equals(section, "products", StringComparison.OrdinalIgnoreCase)) return Missing();

        var regularPrices = RegularProductPrices();
        var productSource = db.Products
            .AsNoTracking()
            .Join(
                db.StoreBrands.AsNoTracking().Where(brand => brand.IsActive),
                product => product.IdBrand,
                brand => (long?)brand.Id,
                (product, brand) => new { product, brand })
            .GroupJoin(
                db.StoreCategories.AsNoTracking().Where(category => category.IsActive),
                productBrand => productBrand.product.IdCategory,
                category => (long?)category.Id,
                (productBrand, categories) => new { productBrand, categories })
            .SelectMany(
                productBrandCategories => productBrandCategories.categories.DefaultIfEmpty(),
                (productBrandCategories, category) => new
                {
                    productBrandCategories.productBrand.product,
                    productBrandCategories.productBrand.brand,
                    category
                })
            .Where(productBrandCategory => productBrandCategory.product.IsStoreEnabled &&
                                           (productBrandCategory.product.IdCategory == null ||
                                            productBrandCategory.category != null));
        if (query.Length > 0)
            productSource = productSource.Where(x =>
                x.product.ProductKey.Contains(query) || x.product.NameFa.Contains(query) ||
                (x.product.NameEn != null && x.product.NameEn.Contains(query)) ||
                x.brand.NameFa.Contains(query) ||
                (x.category != null && x.category.NameFa.Contains(query)));
        int productTotal = await productSource.CountAsync(cancellationToken);
        var productRows = await productSource.OrderByDescending(x => x.product.CreatedAtUtc)
            .ThenByDescending(x => x.product.Id).Skip(paging.Offset).Take(paging.PageSize).Select(x => new
            {
                x.product.Id,
                Code = x.product.ProductKey,
                x.product.NameFa,
                x.product.NameEn,
                BrandName = x.brand.NameFa,
                CategoryName = x.category == null ? "" : x.category.NameFa,
                PriceVersion = regularPrices.FirstOrDefault(v => v.IdProduct == x.product.Id)
            }).ToListAsync(cancellationToken);
        decimal[] productIds = productRows.Select(x => x.Id).ToArray();
        var productStockTotals = productIds.Length == 0
            ? new Dictionary<decimal, decimal>()
            : await db.InventoryItems
                .AsNoTracking()
                .Join(
                    db.StockBalances.AsNoTracking(),
                    inventory => inventory.Id,
                    balance => balance.IdInventoryItem,
                    (inventory, balance) => new { inventory, balance })
                .Where(inventoryBalance => productIds.Contains(inventoryBalance.inventory.IdProduct))
                .GroupBy(inventoryBalance => inventoryBalance.inventory.IdProduct)
                .Select(productGroup => new
                {
                    Id = productGroup.Key,
                    Quantity = productGroup.Sum(inventoryBalance => inventoryBalance.balance.Quantity)
                })
                .ToDictionaryAsync(stock => stock.Id, stock => stock.Quantity, cancellationToken);
        var productItems = productRows.Select(x =>
        {
            decimal stock = productStockTotals.GetValueOrDefault(x.Id);
            int stockQuantity = stock >= int.MaxValue ? int.MaxValue : Math.Max(0, (int)Math.Floor(stock));

            return new ProductRow(checked((int)x.Id), x.Code, x.NameFa, x.NameEn ?? x.NameFa, x.BrandName,
                x.CategoryName, FinalPrice(x.PriceVersion) / 1_000_000m, stockQuantity,
                x.PriceVersion?.DiscountPercent ?? 0m);
        });

        return Success(AdminListQuery.CreateResult(productItems.ToArray(), productTotal, paging));
    }

    private static string Text(JsonElement item, string property, string fallback) =>
        item.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    private sealed record ProductRow(
        int Id,
        string Code,
        string Name,
        string EnglishName,
        string BrandName,
        string CategoryName,
        decimal Price,
        int StockQuantity,
        decimal DiscountPercent);

    private sealed record CategoryRow(
        string Id,
        string Name,
        string EnglishName,
        string Slug,
        string ParentId,
        bool IsActive,
        string TreePathIds,
        string TreePathNames);

    private sealed record BrandRow(
        string Id,
        string Name,
        string EnglishName,
        string CountryCode,
        string? LogoPath,
        bool IsActive,
        bool ShowInStore)
    {
        public string? LogoUrl { get; init; }
    }
}
