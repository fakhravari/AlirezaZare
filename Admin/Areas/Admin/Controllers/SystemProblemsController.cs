using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Admin.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "system.full-access")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SystemProblemsController(ShopDbContext db) : Controller
{
    private const int PageSize = 50;

    [HttpGet]
    public async Task<IActionResult> Index(string? check, int page = 1, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var plans = db.SalesPlanReferences.AsNoTracking().Where(p => p.IdStockOwnerType == 1 && p.IsActive &&
            (p.StartsAtUtc == null || p.StartsAtUtc <= now) && (p.EndsAtUtc == null || p.EndsAtUtc > now));
        var products = db.Products.AsNoTracking().Where(p => p.IsStoreEnabled);
        var productPlans = from product in products
                           from plan in plans
                           where (plan.IsRegular || db.StoreProductSalesPlanOffers.Any(o => o.IdProduct == product.Id && o.IdSalesPlan == plan.Id && o.IsEnabled))
                               && !db.StoreProductSalesPlanOffers.Any(o => o.IdProduct == product.Id && o.IdSalesPlan == plan.Id && !o.IsEnabled)
                           select new { product, plan };

        var variantPrices = from pair in productPlans
                            join variant in db.ProductVariants.AsNoTracking() on pair.product.Id equals variant.IdProduct
                            join offer in db.StoreProductVariantSalesPlanOffers.AsNoTracking()
                                on new { Variant = variant.Id, Plan = pair.plan.Id } equals new { Variant = offer.IdProductVariant, Plan = offer.IdSalesPlan } into offers
                            from offer in offers.DefaultIfEmpty()
                            where variant.IsActive && db.InventoryItems.Any(i => i.IdProductVariant == variant.Id && i.IsActive)
                                && (offer == null || (offer.IsEnabled && !db.StorePriceVersions.Any(p => p.IdProductVariant == variant.Id && p.IdSalesPlan == pair.plan.Id && p.IsActive)))
                            select new SystemProblemRow { Id = variant.Id, Subject = pair.product.Name, Context = variant.VariantName.Name, Plan = pair.plan.DisplayName,
                                Detail = offer == null ? "اتصال تنوع به طرح ثبت نشده" : "قیمت فعال تنوع ثبت نشده" };

        var defaultSale = from pair in productPlans
                          join variant in db.ProductVariants.AsNoTracking() on pair.product.Id equals variant.IdProduct
                          where variant.IsActive && variant.IsDefault
                              && (!db.InventoryItems.Any(i => i.IdProductVariant == variant.Id && i.IsActive)
                                  || !db.StoreProductVariantSalesPlanOffers.Any(o => o.IdProductVariant == variant.Id && o.IdSalesPlan == pair.plan.Id && o.IsEnabled)
                                  || !db.StorePriceVersions.Any(p => p.IdProductVariant == variant.Id && p.IdSalesPlan == pair.plan.Id && p.IsActive))
                          select new SystemProblemRow { Id = variant.Id, Subject = pair.product.Name, Context = variant.VariantName.Name, Plan = pair.plan.DisplayName,
                              Detail = "تنوع پیش‌فرض در این طرح قلم موجودی، اتصال فعال یا قیمت فعال ندارد؛ وضعیت آن را بررسی کنید" };

        var productPrices = from pair in productPlans
                            where !db.ProductVariants.Any(v => v.IdProduct == pair.product.Id && v.IsActive)
                                && !db.StorePriceVersions.Any(p => p.IdProduct == pair.product.Id && p.IdSalesPlan == pair.plan.Id && p.IdProductVariant == null && p.IdAccessory == null && p.IsActive)
                            select new SystemProblemRow { Id = pair.product.Id, Subject = pair.product.Name, Context = "کالای بدون تنوع فعال", Plan = pair.plan.DisplayName, Detail = "قیمت فعال کالا ثبت نشده" };

        var defaultVariants = from product in products
                              where db.ProductVariants.Any(v => v.IdProduct == product.Id)
                                  && !db.ProductVariants.Any(v => v.IdProduct == product.Id && v.IsActive && v.IsDefault)
                              select new SystemProblemRow { Id = product.Id, Subject = product.Name, Plan = "", Detail = "تنوع پیش‌فرض فعال ندارد" };

        var variantInventory = from variant in db.ProductVariants.AsNoTracking()
                               where variant.IsActive && variant.Product.IsStoreEnabled
                                   && !db.InventoryItems.Any(i => i.IdProductVariant == variant.Id && i.IdProduct == variant.IdProduct && i.IsActive)
                               select new SystemProblemRow { Id = variant.Id, Subject = variant.Product.Name, Context = variant.VariantName.Name, Plan = "", Detail = "قلم موجودی فعال برای تنوع ثبت نشده" };

        var productInventory = from product in products
                               where !db.ProductVariants.Any(v => v.IdProduct == product.Id && v.IsActive)
                                   && !db.InventoryItems.Any(i => i.IdProduct == product.Id && i.IdProductVariant == null && i.IsActive)
                               select new SystemProblemRow { Id = product.Id, Subject = product.Name, Plan = "", Detail = "قلم موجودی فعال برای کالای بدون تنوع ثبت نشده" };

        var baseUnits = from product in db.Products.AsNoTracking()
                        where (product.IsStoreEnabled || product.IsWarehouseEnabled)
                            && (db.WarehouseProductUnits.Count(u => u.IdProduct == product.Id && u.IsActive && u.IsBaseUnit) != 1
                                || db.WarehouseProductUnits.Any(u => u.IdProduct == product.Id && u.IsActive && u.IsBaseUnit &&
                                    (u.ConversionFactor != 1 || !db.WarehouseUnits.Any(unit => unit.Id == u.IdUnit && unit.IsActive))))
                        select new SystemProblemRow { Id = product.Id, Subject = product.Name, Plan = "", Detail = "واحد پایهٔ فعال، ضریب یک یا مرجع واحد فعال معتبر نیست" };

        var requiredAccessories = from pair in productPlans
                                  join relation in db.StoreProductAccessories.AsNoTracking() on pair.product.Id equals relation.IdProduct
                                  join accessory in db.StoreAccessories.AsNoTracking() on relation.IdAccessory equals accessory.Id
                                  where relation.IsRequired && accessory.IsActive &&
                                    (!db.StorePriceVersions.Any(p => p.IdProduct == pair.product.Id && p.IdSalesPlan == pair.plan.Id && p.IdAccessory == accessory.Id && p.IsActive)
                                     || db.StoreProductSalesPlanOffers.Any(o => o.IdProduct == pair.product.Id && o.IdSalesPlan == pair.plan.Id && o.IsEnabled &&
                                         !db.StoreProductSalesPlanOfferAccessories.Any(a => a.IdProductSalesPlanOffer == o.Id && a.IdAccessory == accessory.Id && a.IsAllowed)))
                                  select new SystemProblemRow { Id = relation.Id, Subject = pair.product.Name, Context = accessory.Name, Plan = pair.plan.DisplayName,
                                    Detail = "قیمت فعال یا مجوز متعلقهٔ اجباری در طرح موجود نیست" };

        var negativeStock = from stock in db.StockBalances.AsNoTracking()
                            where stock.Quantity < 0
                            select new SystemProblemRow { Id = stock.Id, Subject = stock.InventoryItem.Product.Name,
                                Context = stock.InventoryItem.ProductVariant != null ? stock.InventoryItem.ProductVariant.VariantName.Name : "بدون تنوع",
                                Plan = stock.Warehouse.Name, Detail = "ماندهٔ موجودی منفی است؛ گردش انبار بررسی شود" };

        var orderAmounts = from order in db.StoreCustomerOrders.AsNoTracking()
                           where db.StoreCustomerOrderItems.Any(i => i.IdOrder == order.Id)
                             && (from item in db.StoreCustomerOrderItems
                                 join amount in db.StoreOrderItemAmounts on item.Id equals amount.IdOrderItem
                                 where item.IdOrder == order.Id
                                 select (decimal?)(amount.UnitAmount * item.Quantity)).Sum() != order.SubtotalAmount
                           select new SystemProblemRow { Id = order.Id, Subject = order.OrderNumber, Plan = "", Detail = "جمع اقلام با مبلغ اقلام سفارش یکسان نیست" };

        var configuration = new List<SystemProblemRow>();
        var identity = await db.StoreSiteIdentities.AsNoTracking().Where(i => i.Id == 1).Select(i => new { i.IdWarehouse }).SingleOrDefaultAsync(cancellationToken);
        if (identity is null || !identity.IdWarehouse.HasValue || !await db.Warehouses.AsNoTracking().AnyAsync(w => w.Id == identity.IdWarehouse && w.IsActive && w.IsSalesWarehouse, cancellationToken))
            configuration.Add(new() { Subject = "انبار سایت", Detail = "انبار فروش فعال برای سایت تنظیم نشده است" });
        int regularPlanCount = await db.SalesPlanReferences.AsNoTracking().CountAsync(p => p.IdStockOwnerType == 1 && p.IsRegular && p.IsActive &&
            (p.StartsAtUtc == null || p.StartsAtUtc <= now) && (p.EndsAtUtc == null || p.EndsAtUtc > now), cancellationToken);
        if (regularPlanCount != 1)
            configuration.Add(new() { Subject = "طرح عادی فروش", Detail = "تعداد طرح‌های عادی فعال و جاری فروشگاه برابر یک نیست" });

        var checks = new List<SystemProblemCheck>
        {
            new("configuration", "تنظیمات اصلی فروش", "انبار فروش سایت و طرح عادی فعال و جاری", null, configuration),
            new("variant-prices", "قیمت و اتصال تنوع", "تنوع‌های فعال در طرح‌های جاری؛ اتصال عمداً غیرفعال گزارش نمی‌شود", variantPrices),
            new("default-sale", "تنوع پیش‌فرض قابل عرضه", "تنوع پیش‌فرض باید در طرح جاری قلم موجودی، اتصال و قیمت فعال داشته باشد؛ اتصال غیرفعال نیز گزارش می‌شود", defaultSale),
            new("product-prices", "قیمت کالای بدون تنوع", "قیمت فعال کالا در طرح‌های جاری", productPrices),
            new("default-variant", "تنوع پیش‌فرض", "کالای فروشگاهی دارای سابقهٔ تنوع باید پیش‌فرض فعال داشته باشد", defaultVariants),
            new("variant-inventory", "قلم موجودی تنوع", "تنوع فعال فروشگاهی باید قلم موجودی فعال داشته باشد", variantInventory),
            new("product-inventory", "قلم موجودی کالا", "کالای فروشگاهی بدون تنوع فعال باید قلم موجودی فعال داشته باشد", productInventory),
            new("base-unit", "واحد پایهٔ کالا", "کالای فعال باید دقیقاً یک واحد پایهٔ فعال با ضریب یک و مرجع واحد فعال داشته باشد", baseUnits),
            new("required-accessory", "متعلقهٔ اجباری", "قیمت فعال و مجاز بودن متعلقه در طرح جاری", requiredAccessories),
            new("negative-stock", "موجودی منفی", "مانده‌های منفی انبار در تمام طرح‌ها", negativeStock),
            new("order-subtotal", "جمع اقلام سفارش", "مقایسهٔ جمع اقلام قیمت‌گذاری‌شده با مبلغ اقلام ثبت‌شده", orderAmounts)
        };
        foreach (var item in checks)
            item.Count = item.Query is null ? item.LocalRows!.Count : await item.Query.CountAsync(cancellationToken);
        var selected = checks.FirstOrDefault(c => c.Key == check) ?? checks.FirstOrDefault(c => c.Count > 0) ?? checks[0];
        int lastPage = Math.Max(1, (int)(((long)selected.Count + PageSize - 1) / PageSize));
        page = Math.Clamp(page, 1, lastPage);
        var rows = selected.Query is null ? selected.LocalRows! : await selected.Query
            .OrderBy(r => r.Subject).ThenBy(r => r.Id).ThenBy(r => r.Plan)
            .Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(cancellationToken);
        return View(new SystemProblemsModel(checks, selected, rows, page, lastPage, DateTimeOffset.UtcNow));
    }
}

public sealed class SystemProblemRow
{
    public long Id { get; set; }
    public string Subject { get; set; } = "";
    public string Context { get; set; } = "";
    public string Plan { get; set; } = "";
    public string Detail { get; set; } = "";
}
public sealed class SystemProblemCheck(string key, string title, string description, IQueryable<SystemProblemRow>? query, List<SystemProblemRow>? localRows = null)
{
    public string Key { get; } = key;
    public string Title { get; } = title;
    public string Description { get; } = description;
    public IQueryable<SystemProblemRow>? Query { get; } = query;
    public List<SystemProblemRow>? LocalRows { get; } = localRows;
    public int Count { get; set; }
}
public sealed record SystemProblemsModel(List<SystemProblemCheck> Checks, SystemProblemCheck Selected, List<SystemProblemRow> Rows, int Page, int LastPage, DateTimeOffset CheckedAtUtc);
