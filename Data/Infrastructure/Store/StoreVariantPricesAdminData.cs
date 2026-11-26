using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;
using Shop.Data.Helpers;
using Shop.Data.Application.Admin.Contracts;
using static Shop.Data.Application.Admin.Contracts.AdminOperationResult;

namespace Shop.Data.Infrastructure.Store;

public sealed class StoreVariantPricesAdminData(ShopDbContext db, IStorePriceVersionService priceVersions, System.Security.Claims.ClaimsPrincipal User) : IStoreVariantPricesAdminData
{
    public async Task<AdminOperationResult> Plans(string? q, DateOnly? from, DateOnly? to,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        var search = q?.Trim() ?? "";
        if (search.Length > 100) return Validation("عبارت جست‌وجو طولانی است.");
        if (!TryDateRange(from, to, out var start, out var end))
            return Validation("بازهٔ تاریخ معتبر نیست.");
        var plans = db.SalesPlanReferences.AsNoTracking().Where(x => x.IdStockOwnerType == 1);
        var startOffset = start.HasValue ? new DateTimeOffset(start.Value) : (DateTimeOffset?)null;
        var endOffset = end.HasValue ? new DateTimeOffset(end.Value) : (DateTimeOffset?)null;
        if (startOffset.HasValue)
            plans = plans.Where(x => !x.EndsAtUtc.HasValue || x.EndsAtUtc.Value > startOffset.Value);
        if (endOffset.HasValue)
            plans = plans.Where(x => !x.StartsAtUtc.HasValue || x.StartsAtUtc.Value < endOffset.Value);
        if (search.Length > 0)
        {
            var productIds = db.Products.AsNoTracking()
                .Where(x => x.NameFa.Contains(search) || x.ProductKey.Contains(search)).Select(x => x.Id);
            plans = plans.Where(plan =>
                db.StoreProductSalesPlanOffers.Any(offer =>
                    offer.IdSalesPlan == plan.Id && productIds.Contains(offer.IdProduct)) ||
                db.StoreProductVariantSalesPlanOffers.Any(offer =>
                    offer.IdSalesPlan == plan.Id && db.ProductVariants.Any(variant =>
                        variant.Id == offer.IdVariant && productIds.Contains(variant.IdProduct))) ||
                db.StorePriceVersions.Any(version =>
                    version.IdSalesPlan == plan.Id && productIds.Contains(version.IdProduct)));
        }

        return Success(await plans.OrderBy(x => x.DisplayName).Select(x => new
        {
            x.Id,
            x.DisplayName,
            x.Code,
            x.IsRegular,
            x.IsActive,
            x.PlanType,
            x.StoreSalesPlanKey,
            x.StartsAtUtc,
            x.EndsAtUtc,
            x.Label,
            x.ShowCountdown
        }).ToListAsync(cancellationToken));
    }

    public async Task<AdminOperationResult> PricePlans(CancellationToken cancellationToken)
    {
        if (!CanManage()) return Denied();

        return Success(await db.SalesPlanReferences.AsNoTracking()
            .Where(x => x.IdStockOwnerType == 1 && x.IsActive)
            .OrderBy(x => x.DisplayName).ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.DisplayName, x.Code })
            .ToListAsync(cancellationToken));
    }

    public async Task<AdminOperationResult> Products(string? q, int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (!CanManage()) return Denied();
        var search = q?.Trim() ?? "";
        if (search.Length > 100 || page < 1 || page > 10000)
            return Validation("عبارت جست‌وجو یا صفحه معتبر نیست.");
        const int size = 30;
        var products = db.Products.AsNoTracking()
            .Where(x => search == "" || x.NameFa.Contains(search) || x.ProductKey.Contains(search));
        var items = await products.OrderBy(x => x.NameFa).ThenBy(x => x.Id).Skip((page - 1) * size).Take(size + 1)
            .Select(x => new { x.Id, Name = x.NameFa, Code = x.ProductKey }).ToListAsync(cancellationToken);
        return Success(new { items = items.Take(size), more = items.Count > size });
    }

    public async Task<AdminOperationResult> PriceHistory(decimal? productId, decimal[]? productIds,
        long? salesPlanId, string? q, DateOnly? from, DateOnly? to,
        int take = 100, CancellationToken cancellationToken = default)
    {
        if (!CanManage()) return Denied();

        take = Math.Clamp(take, 1, 500);
        var query = db.StorePriceVersions.AsNoTracking();
        var search = q?.Trim() ?? "";
        if (search.Length > 100) return Validation("عبارت جست‌وجو طولانی است.");
        if (!TryDateRange(from, to, out var start, out var end))
            return Validation("بازهٔ تاریخ معتبر نیست.");
        if (start.HasValue) query = query.Where(x => x.CreatedAtUtc >= start.Value);
        if (end.HasValue) query = query.Where(x => x.CreatedAtUtc < end.Value);
        if (search.Length > 0)
            query = query.Where(x => db.Products.Any(product =>
                product.Id == x.IdProduct && (product.NameFa.Contains(search) || product.ProductKey.Contains(search))));
        if (productId.HasValue) query = query.Where(x => x.IdProduct == productId.Value);
        if (productIds is { Length: > 0 }) query = query.Where(x => productIds.Contains(x.IdProduct));
        if (salesPlanId.HasValue) query = query.Where(x => x.IdSalesPlan == salesPlanId.Value);

        return Success(await query.OrderByDescending(x => x.Id).Take(take).Select(x => new
        {
            x.Id, x.IdProduct, x.IdSalesPlan,
            productName = db.Products.Where(product => product.Id == x.IdProduct).Select(product => product.NameFa)
                .FirstOrDefault(),
            salesPlanName = db.SalesPlanReferences.Where(plan => plan.Id == x.IdSalesPlan)
                .Select(plan => plan.DisplayName).FirstOrDefault(),
            targetType = x.IdAccessory.HasValue ? "accessory" : x.IdVariant.HasValue ? "variant" : "product",
            targetId = x.IdVariant ?? x.IdAccessory,
            IdProductVariant = x.IdVariant, x.IdAccessory,
            PriceAmount = x.PriceBeforeDiscountAmount, x.DiscountPercent, x.FinalPriceAmount, x.IsActive,
            x.CreatedAtUtc, x.IdChangedBy, x.ChangeReason, x.IdPreviousVersion
        }).ToListAsync(cancellationToken));
    }

    public async Task<AdminOperationResult> CreatePlan(PlanInput input, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        var error = ValidatePlan(input, true);
        if (error is not null) return Validation(error);

        var key = input.Key!.Trim().ToLowerInvariant();
        var code = input.Code!.Trim();
        var name = input.DisplayName!.Trim();
        var type = input.PlanType!.Trim().ToLowerInvariant();
        if (await db.SalesPlanReferences.AnyAsync(x => x.StoreSalesPlanKey == key || x.Code == code, cancellationToken))
            return Conflict(new { message = "کلید یا کد این طرح قبلاً ثبت شده است." });

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var plan = new Shop.Data.Domain.Warehouse.Entities.SalesPlanReference
        {
            StoreSalesPlanKey = key,
            DisplayName = name,
            IsRegular = false,
            IsActive = false,
            IdStockOwnerType = 1,
            Code = code,
            PlanType = type,
            StartsAtUtc = input.StartsAtUtc?.ToUniversalTime(),
            EndsAtUtc = input.EndsAtUtc?.ToUniversalTime(),
            Label = NormalizeLabel(input.Label),
            ShowCountdown = input.ShowCountdown
        };
        db.SalesPlanReferences.Add(plan);
        await db.SaveChangesAsync(cancellationToken);

        var salesWarehouseIds = await db.Warehouses.Where(x => x.IsSalesWarehouse).Select(x => x.Id)
            .ToListAsync(cancellationToken);
        db.WarehouseStockOwners.AddRange(salesWarehouseIds.Select(warehouseId =>
            new Shop.Data.Domain.Warehouse.Entities.WarehouseStockOwner
                { IdWarehouse = warehouseId, IdStockOwner = plan.Id }));
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        var created = await db.SalesPlanReferences.AsNoTracking()
            .Where(x => x.IdStockOwnerType == 1 && x.StoreSalesPlanKey == key)
            .Select(x => new { x.Id, x.StoreSalesPlanKey }).SingleAsync(cancellationToken);

        return Success(new { created.Id, created.StoreSalesPlanKey, message = "طرح فروش به‌صورت پیش‌نویس ساخته شد." });
    }

    public async Task<AdminOperationResult> UpdatePlan(long id, PlanInput input,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        var error = ValidatePlan(input, false);
        if (error is not null) return Validation(error);

        var plan = await db.SalesPlanReferences.SingleOrDefaultAsync(x => x.Id == id && x.IdStockOwnerType == 1,
            cancellationToken);
        if (plan is null) return Missing();

        if (plan.IsRegular) return Validation("مشخصات طرح عادی قابل تغییر نیست.");

        var code = input.Code!.Trim();
        if (await db.SalesPlanReferences.AnyAsync(x => x.Id != id && x.IdStockOwnerType == 1 && x.Code == code,
                cancellationToken))
            return Conflict(new { message = "این کد قبلاً برای طرح دیگری ثبت شده است." });

        plan.DisplayName = input.DisplayName!.Trim();
        plan.Code = code;
        plan.PlanType = input.PlanType!.Trim().ToLowerInvariant();
        await db.SaveChangesAsync(cancellationToken);

        return Success(new { message = "مشخصات طرح فروش ذخیره شد." });
    }

    public async Task<AdminOperationResult> SetPlanPublication(long id, PlanPublicationInput input,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        var plan = await db.SalesPlanReferences.SingleOrDefaultAsync(x => x.Id == id && x.IdStockOwnerType == 1,
            cancellationToken);
        if (plan is null) return Missing();

        if (plan.IsRegular && !input.IsActive)
            return Validation("طرح عادی همیشه فعال می‌ماند.");

        plan.IsActive = input.IsActive;
        await db.SaveChangesAsync(cancellationToken);

        return Success(new { message = input.IsActive ? "طرح منتشر و فعال شد." : "طرح از فروش خارج شد و سوابق آن حفظ شد." });
    }

    public async Task<AdminOperationResult> DeletePlan(long id, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        var plan = await db.SalesPlanReferences.SingleOrDefaultAsync(x => x.Id == id && x.IdStockOwnerType == 1,
            cancellationToken);
        if (plan is null) return Missing();

        if (plan.IsRegular) return Conflict(new { message = "طرح عادی حذف‌شدنی نیست." });

        bool referenced = await db.AccountCartItems.AnyAsync(x => x.IdSalesPlan == id, cancellationToken) ||
                          await db.StoreCustomerOrderItems.AnyAsync(x => x.IdSalesPlan == id, cancellationToken) ||
                          await db.StoreProductSalesPlanOffers.AnyAsync(x => x.IdSalesPlan == id, cancellationToken) ||
                          await db.StoreProductVariantSalesPlanOffers.AnyAsync(x => x.IdSalesPlan == id,
                              cancellationToken) ||
                          await db.StockBalances.AnyAsync(x => x.IdSalesPlanReference == id, cancellationToken) ||
                          await db.Vouchers.AnyAsync(x => x.IdSalesPlanReference == id, cancellationToken) ||
                          await db.WarehouseStockOwners.AnyAsync(x => x.IdStockOwner == id && x.IsDefault,
                              cancellationToken);
        if (referenced)
            return Conflict(new { message = "این طرح سابقه یا اتصال دارد؛ برای حفظ سابقه آن را غیرفعال کنید." });

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var warehouseIds = await db.Warehouses.Where(x => x.IsSalesWarehouse).Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var links = await db.WarehouseStockOwners
            .Where(x => x.IdStockOwner == id && warehouseIds.Contains(x.IdWarehouse)).ToListAsync(cancellationToken);
        db.WarehouseStockOwners.RemoveRange(links);
        db.SalesPlanReferences.Remove(plan);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Success(new { message = "طرح فروش حذف شد." });
    }

    private static string? ValidatePlan(PlanInput input, bool creating)
    {
        if (creating && (string.IsNullOrWhiteSpace(input.Key) || input.Key.Length > 64 ||
                         !System.Text.RegularExpressions.Regex.IsMatch(input.Key, "^[a-z0-9][a-z0-9-]*$")))
            return "کلید طرح باید حداکثر ۶۴ نویسه و شامل حروف کوچک انگلیسی، عدد و خط تیره باشد.";

        if (string.IsNullOrWhiteSpace(input.Code) || input.Code.Length > 64 ||
            string.IsNullOrWhiteSpace(input.DisplayName) || input.DisplayName.Length > 200 ||
            string.IsNullOrWhiteSpace(input.PlanType) ||
            input.PlanType.Length > 30) return "کد، نام و نوع طرح الزامی و باید در محدودهٔ مجاز باشند.";

        if (input.StartsAtUtc.HasValue && input.EndsAtUtc.HasValue && input.StartsAtUtc >= input.EndsAtUtc ||
            input.Label?.Length > 100 ||
            input.ShowCountdown && !input.EndsAtUtc.HasValue)
            return "بازه یا برچسب طرح نامعتبر است؛ شمارش معکوس به تاریخ پایان نیاز دارد.";

        return null;
    }

    private static string? NormalizeLabel(string? label) => string.IsNullOrWhiteSpace(label) ? null : label.Trim();

    public async Task<AdminOperationResult> SavePlan(long id, PlanSettingsInput input,
        CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Denied();

        if (id < 1 || input.Label?.Length > 100 ||
            (input.StartsAtUtc.HasValue && input.EndsAtUtc.HasValue && input.StartsAtUtc >= input.EndsAtUtc) ||
            (input.ShowCountdown && !input.EndsAtUtc.HasValue))
            return Validation("بازهٔ طرح یا برچسب نامعتبر است؛ شمارش معکوس به تاریخ پایان نیاز دارد.");

        var plan = await db.SalesPlanReferences.SingleOrDefaultAsync(x => x.Id == id && x.IdStockOwnerType == 1,
            cancellationToken);
        if (plan is null) return Missing();

        if (plan.IsRegular && (input.StartsAtUtc.HasValue || input.EndsAtUtc.HasValue || input.ShowCountdown))
            return Validation("برای طرح عادی بازه یا شمارش معکوس تعیین نمی‌شود.");

        plan.StartsAtUtc = input.StartsAtUtc?.ToUniversalTime();
        plan.EndsAtUtc = input.EndsAtUtc?.ToUniversalTime();
        plan.Label = string.IsNullOrWhiteSpace(input.Label) ? null : input.Label.Trim();
        plan.ShowCountdown = input.ShowCountdown;
        await db.SaveChangesAsync(cancellationToken);

        return Success(new { message = "تنظیمات طرح فروش ذخیره شد." });
    }

    public async Task<AdminOperationResult> List(
        string? q,
        decimal[]? productIds,
        long? salesPlanId,
        DateOnly? from,
        DateOnly? to,
        bool all = false,
        int page = 1,
        int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!CanManage()) return Denied();

        if (!AdminListQuery.TryCreate(page, pageSize, q, out AdminPageRequest paging))
            return Validation("عبارت جست‌وجو یا صفحه‌بندی معتبر نیست.");
        if (!TryDateRange(from, to, out var start, out var end))
            return Validation("بازهٔ تاریخ معتبر نیست.");

        string query = paging.Search;
        var rows = db.ProductVariants
            .AsNoTracking()
            .Join(
                db.Products.AsNoTracking(),
                variant => variant.IdProduct,
                product => product.Id,
                (variant, product) => new { variant, product })
            .SelectMany(
                variantProduct => db.SalesPlanReferences.AsNoTracking()
                    .Where(plan => plan.IdStockOwnerType == 1 && plan.IsActive),
                (variantProduct, plan) => new { variantProduct.variant, variantProduct.product, plan })
            .GroupJoin(
                db.StoreProductVariantSalesPlanOffers.AsNoTracking(),
                variantProductPlan => new { VariantId = variantProductPlan.variant.Id,
                    PlanId = variantProductPlan.plan.Id },
                offer => new { VariantId = offer.IdVariant, PlanId = offer.IdSalesPlan },
                (variantProductPlan, offers) => new { variantProductPlan, offers })
            .SelectMany(
                variantProductOffers => variantProductOffers.offers.DefaultIfEmpty(),
                (variantProductOffers, offer) => new
                {
                    variantProductOffers.variantProductPlan.variant,
                    variantProductOffers.variantProductPlan.product,
                    variantProductOffers.variantProductPlan.plan,
                    offer
                })
            .Where(variantProductPlanOffer =>
                variantProductPlanOffer.variant.IsActive && variantProductPlanOffer.product.IsStoreEnabled &&
                (query == "" || variantProductPlanOffer.product.NameFa.Contains(query) ||
                 variantProductPlanOffer.product.ProductKey.Contains(query) ||
                 variantProductPlanOffer.variant.VariantName.Name.Contains(query) ||
                 (variantProductPlanOffer.plan.DisplayName ?? "").Contains(query)));
        if (productIds is { Length: > 0 }) rows = rows.Where(x => productIds.Contains(x.product.Id));
        if (salesPlanId.HasValue) rows = rows.Where(x => x.plan.Id == salesPlanId.Value);
        if (start.HasValue) rows = rows.Where(x => x.offer != null && x.offer.CreatedAtUtc >= start.Value);
        if (end.HasValue) rows = rows.Where(x => x.offer != null && x.offer.CreatedAtUtc < end.Value);
        int total = await rows.CountAsync(cancellationToken);
        var orderedRows = rows.OrderBy(x => x.product.NameFa).ThenBy(x => x.variant.VariantName.Name)
            .ThenBy(x => x.plan.Code)
            .ThenBy(x => x.variant.Id).ThenBy(x => x.plan.Id);
        var selectedRows = all ? orderedRows : orderedRows.Skip(paging.Offset).Take(paging.PageSize);
        var items = await selectedRows.Select(x => new VariantPlanPriceRow
        {
            VariantId = x.variant.Id,
            ProductName = x.product.NameFa,
            VariantName = x.variant.VariantName.Name,
            SalesPlanId = x.plan.StoreSalesPlanKey,
            SalesPlanName = x.plan.DisplayName,
            PriceAmount = db.StorePriceVersions
                .Where(v => v.IdProduct == x.product.Id && v.IdVariant == x.variant.Id &&
                            v.IdSalesPlan == x.plan.Id && v.IsActive).Select(v => (decimal?)v.PriceBeforeDiscountAmount)
                .FirstOrDefault(),
            DiscountPercent = db.StorePriceVersions
                .Where(v => v.IdProduct == x.product.Id && v.IdVariant == x.variant.Id &&
                            v.IdSalesPlan == x.plan.Id && v.IsActive).Select(v => (decimal?)v.DiscountPercent)
                .FirstOrDefault(),
            FinalPriceAmount = db.StorePriceVersions
                .Where(v => v.IdProduct == x.product.Id && v.IdVariant == x.variant.Id &&
                            v.IdSalesPlan == x.plan.Id && v.IsActive).Select(v => (decimal?)v.FinalPriceAmount)
                .FirstOrDefault(),
            IsEnabled = x.offer == null ? null : x.offer.IsEnabled
        }).ToListAsync(cancellationToken);
        long[] variantIds = items.Select(x => x.VariantId).Distinct().ToArray();
        var attributes = await db.ProductVariantAttributeValues
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
                    AttributeLabel = linkValueAttribute.attribute.Label,
                    linkValueAttribute.value.ValueKey,
                    linkValueAttribute.value.DisplayValue,
                    linkValueAttribute.value.Swatch
                })
            .ToListAsync(cancellationToken);
        var attributesByVariant = attributes.GroupBy(x => x.IdProductVariant).ToDictionary(x => x.Key,
            x => (JsonElement?)JsonSerializer.SerializeToElement(x.Select(a => new
            {
                key = a.Key, label = a.AttributeLabel, value = a.ValueKey, display = a.DisplayValue, swatch = a.Swatch
            }).ToArray()));
        foreach (VariantPlanPriceRow item in items)
            item.Attributes = attributesByVariant.GetValueOrDefault(item.VariantId);

        return Success(AdminListQuery.CreateResult(items, total, paging));
    }

    public async Task<AdminOperationResult> Save(long variantId, string salesPlanId, VariantPlanPriceInput input,
        CancellationToken cancellationToken)
    {
        if (!CanManage()) return Denied();

        if (variantId < 1 || string.IsNullOrWhiteSpace(salesPlanId) || salesPlanId.Length > 64 ||
            input.PriceAmount < 0 || input.PriceAmount > 9007199254740991m ||
            input.PriceAmount != decimal.Truncate(input.PriceAmount) || input.DiscountPercent is < 0 or >= 100 ||
            decimal.Round(input.DiscountPercent, 2) != input.DiscountPercent)
            return Validation("قیمت باید تومان صحیح و تخفیف بین صفر تا کمتر از صد درصد با حداکثر دو رقم اعشار باشد.");

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var plan = await db.SalesPlanReferences.SingleOrDefaultAsync(
            x => x.StoreSalesPlanKey == salesPlanId && x.IsActive && x.IdStockOwnerType == 1, cancellationToken);
        if (plan is null) return Missing(new { code = "variant-or-plan-not-found" });

        var variant = await db.ProductVariants.SingleOrDefaultAsync(
            v => v.Id == variantId && v.IsActive && db.Products.Any(p => p.Id == v.IdProduct && p.IsStoreEnabled),
            cancellationToken);
        if (variant is null) return Missing(new { code = "variant-or-plan-not-found" });

        var offer = await db.StoreProductVariantSalesPlanOffers.SingleOrDefaultAsync(
            x => x.IdVariant == variantId && x.IdSalesPlan == plan.Id, cancellationToken);
        if (offer is null)
            db.StoreProductVariantSalesPlanOffers.Add(
                new Shop.Data.Domain.Store.Entities.StoreProductVariantSalesPlanOffer
                {
                    IdVariant = variantId, IdSalesPlan = plan.Id, IsEnabled = input.IsEnabled,
                    CreatedAtUtc = DateTime.UtcNow
                });
        else
        {
            offer.IsEnabled = input.IsEnabled;
            offer.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);

        decimal priceVersionId = await priceVersions.AppendAsync(variant.IdProduct, plan.Id, variantId, null,
            input.PriceAmount, input.DiscountPercent,
            long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value), cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Success(new { saved = true, priceVersionId, message = "قیمت تنوع و طرح فروش ذخیره شد." });
    }

    private static bool TryDateRange(DateOnly? from, DateOnly? to, out DateTime? start, out DateTime? end)
    {
        start = end = null;
        if ((from.HasValue && to.HasValue && from > to) || from is { Year: < 1900 or > 9998 } ||
            to is { Year: < 1900 or > 9998 }) return false;
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");
        if (from.HasValue) start = TimeZoneInfo.ConvertTimeToUtc(from.Value.ToDateTime(TimeOnly.MinValue), timeZone);
        if (to.HasValue)
            end = TimeZoneInfo.ConvertTimeToUtc(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), timeZone);
        return true;
    }

    private bool CanManage() => User.IsInRole("system.full-access") ||
                                User.HasClaim("admin_permission", "Store:Features:Index:promotions");

    public sealed class VariantPlanPriceRow
    {
        public long VariantId { get; set; }
        public string ProductName { get; set; } = "";
        public string VariantName { get; set; } = "";
        public string? Code { get; set; }
        public JsonElement? Attributes { get; set; }
        public string SalesPlanId { get; set; } = "";
        public string SalesPlanName { get; set; } = "";
        public decimal? PriceAmount { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? FinalPriceAmount { get; set; }
        public bool? IsEnabled { get; set; }
    }

    public async Task<AdminOperationResult> ProductOffers(
        string? q,
        decimal[]? productIds,
        DateOnly? from,
        DateOnly? to,
        int page = 1,
        int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!CanManage()) return Denied();

        if (!AdminListQuery.TryCreate(page, pageSize, q, out AdminPageRequest paging))
            return Validation("عبارت جست‌وجو یا صفحه‌بندی معتبر نیست.");
        if (!TryDateRange(from, to, out var start, out var end))
            return Validation("بازهٔ تاریخ معتبر نیست.");

        string query = paging.Search;
        var rows = db.Products
            .AsNoTracking()
            .SelectMany(
                product => db.SalesPlanReferences.AsNoTracking().Where(plan => plan.IdStockOwnerType == 1),
                (product, plan) => new { product, plan })
            .GroupJoin(
                db.StoreProductSalesPlanOffers.AsNoTracking(),
                productPlan => new { ProductId = productPlan.product.Id, PlanId = productPlan.plan.Id },
                offer => new { ProductId = offer.IdProduct, PlanId = offer.IdSalesPlan },
                (productPlan, offers) => new { productPlan, offers })
            .SelectMany(
                productPlanOffers => productPlanOffers.offers.DefaultIfEmpty(),
                (productPlanOffers, offer) => new
                {
                    productPlanOffers.productPlan.product,
                    productPlanOffers.productPlan.plan,
                    offer
                })
            .Where(productPlanOffer =>
                productPlanOffer.product.IsStoreEnabled && !productPlanOffer.plan.IsRegular &&
                (query == "" || productPlanOffer.product.NameFa.Contains(query) ||
                 productPlanOffer.product.ProductKey.Contains(query) || productPlanOffer.plan.DisplayName.Contains(query)));
        if (productIds is { Length: > 0 }) rows = rows.Where(x => productIds.Contains(x.product.Id));
        if (start.HasValue) rows = rows.Where(x => x.offer != null && x.offer.CreatedAtUtc >= start.Value);
        if (end.HasValue) rows = rows.Where(x => x.offer != null && x.offer.CreatedAtUtc < end.Value);
        int total = await rows.CountAsync(cancellationToken);
        var items = await rows.OrderBy(x => x.product.NameFa).ThenBy(x => x.plan.DisplayName).ThenBy(x => x.product.Id)
            .ThenBy(x => x.plan.Id).Skip(paging.Offset).Take(paging.PageSize).Select(x => new ProductOfferRow
            {
                ProductId = x.product.Id,
                ProductCode = x.product.ProductKey,
                ProductName = x.product.NameFa,
                SalesPlanId = x.plan.StoreSalesPlanKey,
                SalesPlanName = x.plan.DisplayName,
                PriceAmount = db.StorePriceVersions
                    .Where(v => v.IdProduct == x.product.Id && v.IdSalesPlan == x.plan.Id && v.IsActive &&
                                v.IdVariant == null && v.IdAccessory == null)
                    .Select(v => (decimal?)v.PriceBeforeDiscountAmount).FirstOrDefault(),
                DiscountPercent = db.StorePriceVersions
                    .Where(v => v.IdProduct == x.product.Id && v.IdSalesPlan == x.plan.Id && v.IsActive &&
                                v.IdVariant == null && v.IdAccessory == null)
                    .Select(v => (decimal?)v.DiscountPercent).FirstOrDefault(),
                FinalPriceAmount = db.StorePriceVersions
                    .Where(v => v.IdProduct == x.product.Id && v.IdSalesPlan == x.plan.Id && v.IsActive &&
                                v.IdVariant == null && v.IdAccessory == null)
                    .Select(v => (decimal?)v.FinalPriceAmount).FirstOrDefault(),
                MaxPurchaseQuantity = x.offer == null ? null : x.offer.MaxPurchaseQuantity,
                IsEnabled = x.offer == null ? null : x.offer.IsEnabled
            }).ToListAsync(cancellationToken);

        return Success(AdminListQuery.CreateResult(items, total, paging));
    }

    public async Task<AdminOperationResult> SaveProductOffer(long productId, string salesPlanId,
        ProductOfferInput input, CancellationToken cancellationToken)
    {
        if (!CanManage()) return Denied();

        if (productId < 1 || string.IsNullOrWhiteSpace(salesPlanId) || salesPlanId.Length > 128 ||
            input.PriceAmount < 0 || input.PriceAmount > 9007199254740991m ||
            input.PriceAmount != decimal.Truncate(input.PriceAmount) || input.DiscountPercent is < 0 or >= 100 ||
            decimal.Round(input.DiscountPercent, 2) != input.DiscountPercent ||
            input.MaxPurchaseQuantity is < 1 or > 99)
            return Validation("قیمت، تخفیف یا حداکثر خرید معتبر نیست.");

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var plan = await db.SalesPlanReferences.SingleOrDefaultAsync(
            x => x.StoreSalesPlanKey == salesPlanId && x.IdStockOwnerType == 1 && !x.IsRegular, cancellationToken);
        var product =
            await db.Products.SingleOrDefaultAsync(x => x.Id == productId && x.IsStoreEnabled, cancellationToken);
        if (plan is null || product is null) return Missing(new { code = "product-or-plan-not-found" });

        var offer = await db.StoreProductSalesPlanOffers.SingleOrDefaultAsync(
            x => x.IdProduct == productId && x.IdSalesPlan == plan.Id, cancellationToken);
        List<(decimal AccessoryId, decimal PriceAmount)> newAccessoryPrices = [];
        if (offer is null)
        {
            offer = new Shop.Data.Domain.Store.Entities.StoreProductSalesPlanOffer
            {
                IdProduct = productId, IdSalesPlan = plan.Id, MaxPurchaseQuantity = input.MaxPurchaseQuantity,
                IsEnabled = input.IsEnabled, CreatedAtUtc = DateTime.UtcNow
            };
            db.StoreProductSalesPlanOffers.Add(offer);
            await db.SaveChangesAsync(cancellationToken);

            var accessorySeed = await db.StoreProductAccessories
                .AsNoTracking()
                .Join(
                    db.StoreAccessories.AsNoTracking(),
                    productAccessory => productAccessory.IdAccessory,
                    accessory => accessory.Id,
                    (productAccessory, accessory) => new { productAccessory, accessory })
                .Where(productAccessory => productAccessory.productAccessory.IdProduct == productId &&
                                           productAccessory.accessory.IsActive)
                .Select(productAccessory => new
                {
                    productAccessory.productAccessory.IdAccessory,
                    productAccessory.productAccessory.IsRequired,
                    PriceAmount = db.StorePriceVersions
                        .AsNoTracking()
                        .Join(
                            db.SalesPlanReferences.AsNoTracking(),
                            version => version.IdSalesPlan,
                            regular => (long?)regular.Id,
                            (version, regular) => new { version, regular })
                        .Where(versionPlan =>
                            versionPlan.version.IdProduct == productId &&
                            versionPlan.version.IdAccessory == productAccessory.productAccessory.IdAccessory &&
                            versionPlan.version.IsActive && versionPlan.regular.IsRegular &&
                            versionPlan.regular.IdStockOwnerType == 1)
                        .OrderBy(versionPlan => versionPlan.regular.Id)
                        .Select(versionPlan => (decimal?)versionPlan.version.FinalPriceAmount)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);
            if (accessorySeed.Any(x => !x.PriceAmount.HasValue))
                return Conflict(new { title = "قیمت عادی متعلقه ثبت نشده است." });
            db.StoreProductSalesPlanOfferAccessories.AddRange(accessorySeed.Select(x =>
                new Shop.Data.Domain.Store.Entities.StoreProductSalesPlanOfferAccessory
                {
                    IdOffer = offer.Id, IdAccessory = x.IdAccessory, IsIncludedByDefault = x.IsRequired,
                    IsAllowed = true
                }));
            newAccessoryPrices = accessorySeed.Select(x => (x.IdAccessory, x.PriceAmount!.Value)).ToList();
        }
        else
        {
            offer.MaxPurchaseQuantity = input.MaxPurchaseQuantity;
            offer.IsEnabled = input.IsEnabled;
            offer.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);

        decimal priceVersionId = await priceVersions.AppendAsync(productId, plan.Id, null, null, input.PriceAmount,
            input.DiscountPercent, long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value),
            cancellationToken);
        foreach (var accessory in newAccessoryPrices)
            await priceVersions.AppendAsync(productId, plan.Id, null, accessory.AccessoryId, accessory.PriceAmount, 0,
                long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value), cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Success(new { priceVersionId, message = "پیشنهاد کالا در طرح ذخیره شد." });
    }

    public sealed class ProductOfferRow
    {
        public decimal ProductId { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string SalesPlanId { get; set; } = "";
        public string SalesPlanName { get; set; } = "";
        public decimal? PriceAmount { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? FinalPriceAmount { get; set; }
        public int? MaxPurchaseQuantity { get; set; }
        public bool? IsEnabled { get; set; }
    }

    public async Task<AdminOperationResult> OfferAccessories(decimal productId, string salesPlanId,
        CancellationToken cancellationToken)
    {
        if (!CanManage()) return Denied();

        if (productId < 1 || string.IsNullOrWhiteSpace(salesPlanId) || salesPlanId.Length > 128) return Invalid();

        var offer = await FindOffer(productId, salesPlanId, cancellationToken);
        if (offer is null) return Missing(new { title = "ابتدا پیشنهاد این کالا و طرح را ذخیره کنید." });

        var rows = await db.StoreProductAccessories
            .AsNoTracking()
            .Join(
                db.StoreAccessories.AsNoTracking(),
                productAccessory => productAccessory.IdAccessory,
                accessory => accessory.Id,
                (productAccessory, accessory) => new { productAccessory, accessory })
            .GroupJoin(
                db.StoreProductSalesPlanOfferAccessories.AsNoTracking()
                    .Where(overrideRow => overrideRow.IdOffer == offer.OfferId),
                productAccessory => productAccessory.accessory.Id,
                overrideRow => overrideRow.IdAccessory,
                (productAccessory, overrides) => new { productAccessory, overrides })
            .SelectMany(
                productAccessoryOverrides => productAccessoryOverrides.overrides.DefaultIfEmpty(),
                (productAccessoryOverrides, overrideRow) => new
                {
                    productAccessoryOverrides.productAccessory.productAccessory,
                    productAccessoryOverrides.productAccessory.accessory,
                    overrideRow
                })
            .Where(productAccessory => productAccessory.productAccessory.IdProduct == productId &&
                                       productAccessory.accessory.IsActive)
            .OrderBy(productAccessory => productAccessory.productAccessory.SortOrder)
            .ThenBy(productAccessory => productAccessory.accessory.Name)
            .Select(productAccessory => new OfferAccessoryRow
            {
                AccessoryId = productAccessory.accessory.Id,
                Name = productAccessory.accessory.Name,
                IsRequired = productAccessory.productAccessory.IsRequired,
                BasePriceAmount = db.StorePriceVersions
                    .AsNoTracking()
                    .Join(
                        db.SalesPlanReferences.AsNoTracking(),
                        version => version.IdSalesPlan,
                        regular => (long?)regular.Id,
                        (version, regular) => new { version, regular })
                    .Where(versionPlan =>
                        versionPlan.version.IdProduct == productId &&
                        versionPlan.version.IdAccessory == productAccessory.accessory.Id &&
                        versionPlan.version.IsActive && versionPlan.regular.IsRegular &&
                        versionPlan.regular.IdStockOwnerType == 1)
                    .OrderBy(versionPlan => versionPlan.regular.Id)
                    .Select(versionPlan => (decimal?)versionPlan.version.FinalPriceAmount)
                    .FirstOrDefault() ?? 0m,
                PriceAmount = db.StorePriceVersions
                    .AsNoTracking()
                    .Join(
                        db.SalesPlanReferences.AsNoTracking(),
                        version => version.IdSalesPlan,
                        plan => (long?)plan.Id,
                        (version, plan) => new { version, plan })
                    .Where(versionPlan =>
                        versionPlan.version.IdProduct == productId &&
                        versionPlan.version.IdAccessory == productAccessory.accessory.Id &&
                        versionPlan.version.IsActive && versionPlan.plan.StoreSalesPlanKey == salesPlanId)
                    .Select(versionPlan => (decimal?)versionPlan.version.FinalPriceAmount)
                    .FirstOrDefault(),
                IsAllowed = productAccessory.overrideRow != null && productAccessory.overrideRow.IsAllowed
            })
            .ToListAsync(cancellationToken);

        return Success(rows);
    }

    public async Task<AdminOperationResult> SaveOfferAccessory(decimal productId, string salesPlanId, decimal accessoryId,
        OfferAccessoryInput input, CancellationToken cancellationToken)
    {
        if (!CanManage()) return Denied();

        if (productId < 1 || accessoryId < 1 || string.IsNullOrWhiteSpace(salesPlanId) || salesPlanId.Length > 128 ||
            input.PriceAmount < 0 || input.PriceAmount > 9007199254740991m ||
            input.PriceAmount != decimal.Truncate(input.PriceAmount))
            return Validation("قیمت متعلقه باید مبلغ صحیح و غیرمنفی باشد.");

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var offer = await FindOffer(productId, salesPlanId, cancellationToken);
        var productAccessory = await db.StoreProductAccessories
            .Where(link => link.IdProduct == productId && link.IdAccessory == accessoryId)
            .Join(
                db.StoreAccessories.AsNoTracking(),
                link => link.IdAccessory,
                accessory => accessory.Id,
                (link, accessory) => new { link, accessory })
            .Where(linkAccessory => linkAccessory.accessory.IsActive)
            .Select(linkAccessory => linkAccessory.link)
            .SingleOrDefaultAsync(cancellationToken);
        if (offer is null || productAccessory is null)
            return Missing(new { title = "پیشنهاد یا متعلقهٔ فعال کالا یافت نشد." });

        if (productAccessory.IsRequired && !input.IsAllowed)
            return Invalid(new { title = "متعلقهٔ اجباری باید در پیشنهاد مجاز باشد." });

        var overrideRow = await db.StoreProductSalesPlanOfferAccessories.SingleOrDefaultAsync(
            x => x.IdOffer == offer.OfferId && x.IdAccessory == accessoryId, cancellationToken);
        if (overrideRow is null)
            db.StoreProductSalesPlanOfferAccessories.Add(
                new Shop.Data.Domain.Store.Entities.StoreProductSalesPlanOfferAccessory
                {
                    IdOffer = offer.OfferId, IdAccessory = accessoryId,
                    IsIncludedByDefault = productAccessory.IsRequired, IsAllowed = input.IsAllowed
                });
        else
        {
            overrideRow.IsAllowed = input.IsAllowed;
        }

        await db.SaveChangesAsync(cancellationToken);

        long planId = await db.SalesPlanReferences.Where(x => x.StoreSalesPlanKey == salesPlanId).Select(x => x.Id)
            .SingleAsync(cancellationToken);
        decimal priceVersionId = await priceVersions.AppendAsync(productId, planId, null, accessoryId, input.PriceAmount,
            0, long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value), cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Success(new { priceVersionId, message = "متعلقهٔ پیشنهاد ذخیره شد." });
    }

    public sealed class OfferAccessoryRow
    {
        public decimal AccessoryId { get; set; }
        public string Name { get; set; } = "";
        public bool IsRequired { get; set; }
        public decimal BasePriceAmount { get; set; }
        public decimal? PriceAmount { get; set; }
        public bool IsAllowed { get; set; }
    }

    public sealed class OfferAccessoryMatch
    {
        public decimal OfferId { get; set; }
        public bool IsRequired { get; set; }
    }

    public async Task<AdminOperationResult> OfferSpecifications(decimal productId, string salesPlanId,
        CancellationToken cancellationToken)
    {
        if (!CanManage()) return Denied();

        if (productId < 1 || string.IsNullOrWhiteSpace(salesPlanId) || salesPlanId.Length > 128) return Invalid();

        var offer = await FindOffer(productId, salesPlanId, cancellationToken);
        if (offer is null) return Missing(new { title = "ابتدا پیشنهاد این کالا و طرح را ذخیره کنید." });

        return Success(await db.StoreProductSalesPlanOfferSpecificationOverrides.AsNoTracking()
            .Where(x => x.IdOffer == offer.OfferId).OrderBy(x => x.Id)
            .Select(x => new OfferSpecification { Key = x.SpecificationKey, Value = x.Value })
            .ToListAsync(cancellationToken));
    }

    public async Task<AdminOperationResult> SaveOfferSpecifications(decimal productId, string salesPlanId,
        List<OfferSpecification> input, CancellationToken cancellationToken)
    {
        if (!CanManage()) return Denied();

        if (productId < 1 || string.IsNullOrWhiteSpace(salesPlanId) || salesPlanId.Length > 128 || input is null ||
            input.Count > 50 || input.Any(x => x is null)) return Invalid(new { title = "مشخصات معتبر نیستند." });

        var specs = input.Select(x => new OfferSpecification
            { Key = x.Key?.Trim() ?? "", Value = x.Value?.Trim() ?? "" }).ToList();
        if (specs.Any(x =>
                x.Key.Length is < 1 or > 80 || x.Value.Length is < 1 or > 240 || x.Key.Contains('|') ||
                x.Key.Any(char.IsControl) || x.Value.Any(char.IsControl)) ||
            specs.Select(x => x.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() != specs.Count)
            return Invalid(new { title = "عنوان‌ها باید یکتا و مقدارها در محدودهٔ مجاز باشند." });

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var offer = await FindOffer(productId, salesPlanId, cancellationToken);
        if (offer is null) return Missing(new { title = "پیشنهاد کالا و طرح یافت نشد." });

        var oldRows = await db.StoreProductSalesPlanOfferSpecificationOverrides
            .Where(x => x.IdOffer == offer.OfferId).ToListAsync(cancellationToken);
        db.StoreProductSalesPlanOfferSpecificationOverrides.RemoveRange(oldRows);
        db.StoreProductSalesPlanOfferSpecificationOverrides.AddRange(specs.Select(x =>
            new Shop.Data.Domain.Store.Entities.StoreProductSalesPlanOfferSpecificationOverride
                { IdOffer = offer.OfferId, SpecificationKey = x.Key, Value = x.Value }));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { title = "عنوان مشخصات تکراری است." });
        }

        return Success(new { message = "مشخصات پیشنهاد ذخیره شد." });
    }

    private async Task<OfferAccessoryMatch?> FindOffer(decimal productId, string salesPlanId,
        CancellationToken cancellationToken)
    {
        var row = await db.StoreProductSalesPlanOffers
            .AsNoTracking()
            .Join(
                db.Products.AsNoTracking(),
                offer => offer.IdProduct,
                product => product.Id,
                (offer, product) => new { offer, product })
            .Join(
                db.SalesPlanReferences.AsNoTracking(),
                offerProduct => offerProduct.offer.IdSalesPlan,
                plan => plan.Id,
                (offerProduct, plan) => new { offerProduct.offer, offerProduct.product, plan })
            .Where(offerProductPlan =>
                offerProductPlan.offer.IdProduct == productId &&
                offerProductPlan.plan.StoreSalesPlanKey == salesPlanId &&
                offerProductPlan.plan.IdStockOwnerType == 1 && !offerProductPlan.plan.IsRegular &&
                offerProductPlan.product.IsStoreEnabled)
            .Select(offerProductPlan => new OfferAccessoryMatch
            {
                OfferId = offerProductPlan.offer.Id,
                IsRequired = false
            })
            .SingleOrDefaultAsync(cancellationToken);

        return row;
    }


}
