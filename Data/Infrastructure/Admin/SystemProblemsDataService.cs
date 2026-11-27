using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Admin.Contracts;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Admin;

public sealed class SystemProblemsDataService(ShopDbContext db) : ISystemProblemsData
{
    private const int PageSize = 50;
    public async Task<SystemProblemsData> ReadAsync(string? check, int page, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var plans = db.SalesPlanReferences.AsNoTracking().Where(p => p.IdStockOwnerType == 1 && p.IsActive &&
                                                                     (p.StartsAtUtc == null || p.StartsAtUtc <= now) &&
                                                                     (p.EndsAtUtc == null || p.EndsAtUtc > now));
        var products = db.Products.AsNoTracking().Where(p => p.IsStoreEnabled);

        var productPlans = products
            .SelectMany(product => plans, (product, plan) => new { product, plan }).Where(productPlan =>
                (productPlan.plan.IsRegular || db.StoreProductSalesPlanOffers.Any(offer =>
                    offer.IdProduct == productPlan.product.Id && offer.IdSalesPlan == productPlan.plan.Id &&
                    offer.IsEnabled)) &&
                !db.StoreProductSalesPlanOffers.Any(offer =>
                    offer.IdProduct == productPlan.product.Id && offer.IdSalesPlan == productPlan.plan.Id &&
                    !offer.IsEnabled));

        var variantPrices = productPlans
            .Join(db.ProductVariants.AsNoTracking(),
                productPlan => productPlan.product.Id,
                variant => variant.IdProduct,
                (productPlan, variant) => new { productPlan.product, productPlan.plan, variant })
            .GroupJoin(db.StoreProductVariantSalesPlanOffers.AsNoTracking(),
                productPlanVariant => new
                {
                    Variant = productPlanVariant.variant.Id,
                    Plan = productPlanVariant.plan.Id
                },
                offer => new { Variant = offer.IdVariant, Plan = offer.IdSalesPlan },
                (productPlanVariant, offers) => new { productPlanVariant, offers })
            .SelectMany(productPlanOffers => productPlanOffers.offers.DefaultIfEmpty(),
                (productPlanOffers, offer) => new { productPlanOffers.productPlanVariant, offer })
            .Where(productPlanVariantOffer =>
                productPlanVariantOffer.productPlanVariant.variant.IsActive &&
                db.InventoryItems.Any(item => item.IdVariant == productPlanVariantOffer.productPlanVariant.variant.Id && item.IsActive) &&
                (productPlanVariantOffer.offer == null ||
                 (productPlanVariantOffer.offer.IsEnabled && !db.StorePriceVersions.Any(price =>
                     price.IdVariant == productPlanVariantOffer.productPlanVariant.variant.Id &&
                     price.IdSalesPlan == productPlanVariantOffer.productPlanVariant.plan.Id && price.IsActive))))
            .Select(productPlanVariantOffer => new SystemProblemRowData
            {
                Id = productPlanVariantOffer.productPlanVariant.variant.Id,
                Subject = productPlanVariantOffer.productPlanVariant.product.NameFa,
                Context = productPlanVariantOffer.productPlanVariant.variant.VariantName.Name,
                Plan = productPlanVariantOffer.productPlanVariant.plan.DisplayName,
                Detail = productPlanVariantOffer.offer == null
                    ? "اتصال تنوع به طرح ثبت نشده"
                    : "قیمت فعال تنوع ثبت نشده"
            });

        var defaultSale = productPlans
            .Join(db.ProductVariants.AsNoTracking(),
                productPlan => productPlan.product.Id,
                variant => variant.IdProduct,
                (productPlan, variant) => new { productPlan.product, productPlan.plan, variant })
            .Where(productPlanVariant =>
                productPlanVariant.variant.IsActive && productPlanVariant.variant.IsDefault &&
                !db.StoreProductVariantSalesPlanOffers.Any(offer =>
                    offer.IdVariant == productPlanVariant.variant.Id &&
                    offer.IdSalesPlan == productPlanVariant.plan.Id && !offer.IsEnabled) &&
                (!db.InventoryItems.Any(item =>
                     item.IdVariant == productPlanVariant.variant.Id && item.IsActive) ||
                 !db.StoreProductVariantSalesPlanOffers.Any(offer =>
                     offer.IdVariant == productPlanVariant.variant.Id &&
                     offer.IdSalesPlan == productPlanVariant.plan.Id && offer.IsEnabled) ||
                 !db.StorePriceVersions.Any(price =>
                     price.IdVariant == productPlanVariant.variant.Id &&
                     price.IdSalesPlan == productPlanVariant.plan.Id && price.IsActive)))
            .Select(productPlanVariant => new SystemProblemRowData
            {
                Id = productPlanVariant.variant.Id,
                Subject = productPlanVariant.product.NameFa,
                Context = productPlanVariant.variant.VariantName.Name,
                Plan = productPlanVariant.plan.DisplayName,
                Detail = "تنوع پیش‌فرض در این طرح قلم موجودی، اتصال فعال یا قیمت فعال ندارد؛ وضعیت آن را بررسی کنید"
            });

        var productPrices = productPlans
            .Where(productPlan =>
                !db.ProductVariants.Any(variant => variant.IdProduct == productPlan.product.Id && variant.IsActive) &&
                !db.StorePriceVersions.Any(price =>
                    price.IdProduct == productPlan.product.Id && price.IdSalesPlan == productPlan.plan.Id &&
                    price.IdVariant == null && price.IdAccessory == null && price.IsActive))
            .Select(productPlan => new SystemProblemRowData
            {
                Id = productPlan.product.Id,
                Subject = productPlan.product.NameFa,
                Context = "کالای بدون تنوع فعال",
                Plan = productPlan.plan.DisplayName,
                Detail = "قیمت فعال کالا ثبت نشده"
            });

        var defaultVariants = products
            .Where(product =>
                db.ProductVariants.Any(variant => variant.IdProduct == product.Id && variant.IsActive) &&
                !db.ProductVariants.Any(variant =>
                    variant.IdProduct == product.Id && variant.IsActive && variant.IsDefault))
            .Select(product => new SystemProblemRowData
            {
                Id = product.Id,
                Subject = product.NameFa,
                Plan = "",
                Detail = "تنوع پیش‌فرض فعال ندارد"
            });

        var variantInventory = db.ProductVariants.AsNoTracking().Where(variant => variant.IsActive && variant.Product.IsStoreEnabled &&
                              !db.InventoryItems.Any(item =>
                                  item.IdVariant == variant.Id && item.IdProduct == variant.IdProduct &&
                                  item.IsActive))
            .Select(variant => new SystemProblemRowData
            {
                Id = variant.Id,
                Subject = variant.Product.NameFa,
                Context = variant.VariantName.Name,
                Plan = "",
                Detail = "قلم موجودی فعال برای تنوع ثبت نشده"
            });

        var productInventory = products
            .Where(product =>
                !db.ProductVariants.Any(variant => variant.IdProduct == product.Id && variant.IsActive) &&
                !db.InventoryItems.Any(item =>
                    item.IdProduct == product.Id && item.IdVariant == null && item.IsActive))
            .Select(product => new SystemProblemRowData
            {
                Id = product.Id,
                Subject = product.NameFa,
                Plan = "",
                Detail = "قلم موجودی فعال برای کالای بدون تنوع ثبت نشده"
            });

        var baseUnits = db.Products
            .AsNoTracking()
            .Where(product =>
                (product.IsStoreEnabled || product.IsWarehouseEnabled) &&
                (db.WarehouseProductUnits.Count(unitLink =>
                     unitLink.IdProduct == product.Id && unitLink.IsActive && unitLink.IsBaseUnit) != 1 ||
                 db.WarehouseProductUnits.Any(unitLink =>
                     unitLink.IdProduct == product.Id && unitLink.IsActive && unitLink.IsBaseUnit &&
                     (unitLink.ConversionFactor != 1 || !db.WarehouseUnits.Any(unit =>
                         unit.Id == unitLink.IdUnit && unit.IsActive)))))
            .Select(product => new SystemProblemRowData
            {
                Id = product.Id,
                Subject = product.NameFa,
                Plan = "",
                Detail = "واحد پایهٔ فعال، ضریب یک یا مرجع واحد فعال معتبر نیست"
            });

        var requiredAccessories = productPlans
            .Join(
                db.StoreProductAccessories.AsNoTracking(),
                productPlan => productPlan.product.Id,
                relation => relation.IdProduct,
                (productPlan, relation) => new { productPlan.product, productPlan.plan, relation })
            .Join(
                db.StoreAccessories.AsNoTracking(),
                productPlanRelation => productPlanRelation.relation.IdAccessory,
                accessory => accessory.Id,
                (productPlanRelation, accessory) => new
                {
                    productPlanRelation.product,
                    productPlanRelation.plan,
                    productPlanRelation.relation,
                    accessory
                })
            .Where(productPlanAccessory =>
                productPlanAccessory.relation.IsRequired && productPlanAccessory.accessory.IsActive &&
                (!db.StorePriceVersions.Any(price =>
                     price.IdProduct == productPlanAccessory.product.Id &&
                     price.IdSalesPlan == productPlanAccessory.plan.Id &&
                     price.IdAccessory == productPlanAccessory.accessory.Id && price.IsActive) ||
                 db.StoreProductSalesPlanOffers.Any(offer =>
                     offer.IdProduct == productPlanAccessory.product.Id &&
                     offer.IdSalesPlan == productPlanAccessory.plan.Id && offer.IsEnabled &&
                     !db.StoreProductSalesPlanOfferAccessories.Any(overrideRow =>
                         overrideRow.IdOffer == offer.Id &&
                         overrideRow.IdAccessory == productPlanAccessory.accessory.Id && overrideRow.IsAllowed))))
            .Select(productPlanAccessory => new SystemProblemRowData
            {
                Id = productPlanAccessory.relation.Id,
                Subject = productPlanAccessory.product.NameFa,
                Context = productPlanAccessory.accessory.Name,
                Plan = productPlanAccessory.plan.DisplayName,
                Detail = "قیمت فعال یا مجوز متعلقهٔ اجباری در طرح موجود نیست"
            });

        var negativeStock = db.StockBalances
            .AsNoTracking()
            .Where(stock =>
                stock.Quantity < 0 && stock.Warehouse.IsActive && stock.InventoryItem.IsActive &&
                stock.InventoryItem.Product.IsWarehouseEnabled && stock.SalesPlanReference.IsActive &&
                (!stock.InventoryItem.IdVariant.HasValue || stock.InventoryItem.ProductVariant!.IsActive))
            .Select(stock => new SystemProblemRowData
            {
                Id = stock.Id,
                Subject = stock.InventoryItem.Product.NameFa,
                Context = stock.InventoryItem.ProductVariant != null
                    ? stock.InventoryItem.ProductVariant.VariantName.Name
                    : "بدون تنوع",
                Plan = stock.Warehouse.Name,
                Detail = "ماندهٔ موجودی منفی است؛ گردش انبار بررسی شود"
            });

        var orderAmounts = db.StoreCustomerOrders
            .AsNoTracking()
            .Where(order =>
                db.StoreCustomerOrderItems.Any(item => item.IdOrder == order.Id) &&
                db.StoreCustomerOrderItems
                    .Join(
                        db.StorePriceVersions.AsNoTracking(),
                        item => item.IdPriceVersion,
                        price => price.Id,
                        (item, price) => new
                        {
                            item,
                            UnitAmount = price.FinalPriceAmount +
                                (db.StoreOrderAccessories
                                    .Where(accessory => accessory.IdOrder == item.IdOrder &&
                                                        accessory.IdProduct == item.IdProduct &&
                                                        accessory.IdSalesPlan == item.IdSalesPlan)
                                    .Select(accessory => (decimal?)accessory.PriceVersion.FinalPriceAmount)
                                    .Sum() ?? 0m)
                        })
                    .Where(itemAmount => itemAmount.item.IdOrder == order.Id)
                    .Select(itemAmount => (decimal?)(itemAmount.UnitAmount * itemAmount.item.Quantity))
                    .Sum() != order.SubtotalAmount)
            .Select(order => new SystemProblemRowData
            {
                Id = order.Id,
                Subject = order.Id.ToString(),
                Plan = "",
                Detail = "جمع اقلام با مبلغ اقلام سفارش یکسان نیست"
            });

        var configuration = new List<SystemProblemRowData>();
        var identity = await db.StoreSiteIdentities.AsNoTracking().Where(i => i.Id == 1).Select(i => new { i.IdWarehouse }).SingleOrDefaultAsync(cancellationToken);

        if (identity is null || !identity.IdWarehouse.HasValue || !await db.Warehouses.AsNoTracking().AnyAsync(w => w.Id == identity.IdWarehouse && w.IsActive && w.IsSalesWarehouse, cancellationToken))
            configuration.Add(new() { Subject = "انبار سایت", Detail = "انبار فروش فعال برای سایت تنظیم نشده است" });

        int regularPlanCount = await db.SalesPlanReferences.AsNoTracking().CountAsync(p => p.IdStockOwnerType == 1 &&
                p.IsRegular && p.IsActive &&
                (p.StartsAtUtc == null || p.StartsAtUtc <= now) && (p.EndsAtUtc == null || p.EndsAtUtc > now),
            cancellationToken);
        if (regularPlanCount != 1)
            configuration.Add(new()
            { Subject = "طرح عادی فروش", Detail = "تعداد طرح‌های عادی فعال و جاری فروشگاه برابر یک نیست" });

        var checks = new List<SystemProblemCheck>
        {
            new("configuration", "تنظیمات اصلی فروش", "انبار فروش سایت و طرح عادی فعال و جاری", null, configuration),
            new("variant-prices", "قیمت و اتصال تنوع",
                "تنوع‌های فعال در طرح‌های جاری؛ اتصال عمداً غیرفعال گزارش نمی‌شود", variantPrices),
            new("default-sale", "تنوع پیش‌فرض قابل عرضه",
                "تنوع پیش‌فرض فعال در طرح جاری باید قلم موجودی، اتصال و قیمت فعال داشته باشد؛ اتصال غیرفعال بررسی نمی‌شود",
                defaultSale),
            new("product-prices", "قیمت کالای بدون تنوع", "قیمت فعال کالا در طرح‌های جاری", productPrices),
            new("default-variant", "تنوع پیش‌فرض", "کالای فروشگاهی دارای تنوع فعال باید پیش‌فرض فعال داشته باشد",
                defaultVariants),
            new("variant-inventory", "قلم موجودی تنوع", "تنوع فعال فروشگاهی باید قلم موجودی فعال داشته باشد",
                variantInventory),
            new("product-inventory", "قلم موجودی کالا", "کالای فروشگاهی بدون تنوع فعال باید قلم موجودی فعال داشته باشد",
                productInventory),
            new("base-unit", "واحد پایهٔ کالا",
                "کالای فعال باید دقیقاً یک واحد پایهٔ فعال با ضریب یک و مرجع واحد فعال داشته باشد", baseUnits),
            new("required-accessory", "متعلقهٔ اجباری", "قیمت فعال و مجاز بودن متعلقه در طرح جاری",
                requiredAccessories),
            new("negative-stock", "موجودی منفی", "مانده‌های منفی اقلام، انبارها و طرح‌های فعال", negativeStock),
            new("order-subtotal", "جمع اقلام سفارش", "مقایسهٔ جمع اقلام قیمت‌گذاری‌شده با مبلغ اقلام ثبت‌شده",
                orderAmounts)
        };

        foreach (var item in checks)
            item.Count = item.Query is null ? item.LocalRows!.Count : await item.Query.CountAsync(cancellationToken);

        var selected = checks.FirstOrDefault(c => c.Key == check) ?? checks.FirstOrDefault(c => c.Count > 0) ?? checks[0];
        int lastPage = Math.Max(1, (int)(((long)selected.Count + PageSize - 1) / PageSize));
        page = Math.Clamp(page, 1, lastPage);
        var rows = selected.Query is null
            ? selected.LocalRows!
            : await selected.Query
                .OrderBy(r => r.Subject).ThenBy(r => r.Id).ThenBy(r => r.Plan)
                .Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(cancellationToken);
        return new SystemProblemsData(checks.Select(x => new SystemProblemCheckData(x.Key, x.Title, x.Description, x.Count)).ToArray(), new SystemProblemCheckData(selected.Key, selected.Title, selected.Description, selected.Count), rows, page, lastPage, DateTimeOffset.UtcNow);
    }

    private sealed class SystemProblemCheck(string key, string title, string description, IQueryable<SystemProblemRowData>? query, List<SystemProblemRowData>? localRows = null)
    {
        public string Key { get; } = key;
        public string Title { get; } = title;
        public string Description { get; } = description;
        public IQueryable<SystemProblemRowData>? Query { get; } = query;
        public List<SystemProblemRowData>? LocalRows { get; } = localRows;
        public int Count { get; set; }
    }
}
