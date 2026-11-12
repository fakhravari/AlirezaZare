using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/variant-prices")]
public sealed class StoreVariantPricesAdminApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet("plans")]
    public async Task<IActionResult> Plans(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        return Ok(await db.SalesPlanReferences.AsNoTracking().Where(x => x.IdStockOwnerType == 1)
            .OrderBy(x => x.DisplayName).Select(x => new { x.Id, x.DisplayName, x.Code, x.IsRegular,
                x.IsActive, x.PlanType, x.StoreSalesPlanKey, x.StartsAtUtc, x.EndsAtUtc, x.Label, x.ShowCountdown }).ToListAsync(cancellationToken));
    }

    [HttpGet("price-history")]
    public async Task<IActionResult> PriceHistory([FromQuery] long? productId, [FromQuery] long? salesPlanId, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        if (!CanManage()) return Forbid();
        take = Math.Clamp(take, 1, 500);
        var query = db.StorePriceVersions.AsNoTracking().Where(x => x.PriceKind != StorePriceKinds.OrderSnapshot);
        if (productId.HasValue) query = query.Where(x => x.IdProduct == productId.Value);
        if (salesPlanId.HasValue) query = query.Where(x => x.IdSalesPlan == salesPlanId.Value);
        return Ok(await query.OrderByDescending(x => x.Id).Take(take)
            .Select(x => new { x.Id, x.ScopeKey, x.IdProduct, x.IdSalesPlan, x.PriceKind, x.TargetId, x.PriceAmount, x.DiscountPercent, x.IsActive, x.CreatedAtUtc, x.ChangedBy, x.ChangeReason, x.PreviousVersionId })
            .ToListAsync(cancellationToken));
    }

    [HttpPost("plans")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePlan([FromBody] PlanInput input, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        var error = ValidatePlan(input, true);
        if (error is not null) return ValidationProblem(detail: error, statusCode: 400);
        var key = input.Key!.Trim().ToLowerInvariant();
        var code = input.Code!.Trim();
        var name = input.DisplayName!.Trim();
        var type = input.PlanType!.Trim().ToLowerInvariant();
        if (await db.SalesPlanReferences.AnyAsync(x => x.StoreSalesPlanKey == key || x.Code == code, cancellationToken))
            return Conflict(new { message = "کلید یا کد این طرح قبلاً ثبت شده است." });
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var plan = new Shop.Data.Domain.Warehouse.Entities.SalesPlanReference
        {
            StoreSalesPlanKey = key, DisplayName = name, IsRegular = false, IsActive = false,
            IdStockOwnerType = 1, Code = code, PlanType = type,
            StartsAtUtc = input.StartsAtUtc?.ToUniversalTime(), EndsAtUtc = input.EndsAtUtc?.ToUniversalTime(),
            Label = NormalizeLabel(input.Label), ShowCountdown = input.ShowCountdown
        };
        db.SalesPlanReferences.Add(plan);
        await db.SaveChangesAsync(cancellationToken);
        var salesWarehouseIds = await db.Warehouses.Where(x => x.IsSalesWarehouse).Select(x => x.Id).ToListAsync(cancellationToken);
        db.WarehouseStockOwners.AddRange(salesWarehouseIds.Select(warehouseId =>
            new Shop.Data.Domain.Warehouse.Entities.WarehouseStockOwner { IdWarehouse = warehouseId, IdStockOwner = plan.Id }));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var created = await db.SalesPlanReferences.AsNoTracking().Where(x => x.IdStockOwnerType == 1 && x.StoreSalesPlanKey == key)
            .Select(x => new { x.Id, x.StoreSalesPlanKey }).SingleAsync(cancellationToken);
        return Ok(new { created.Id, created.StoreSalesPlanKey, message = "طرح فروش به‌صورت پیش‌نویس ساخته شد." });
    }

    [HttpPut("plans/{id:long}/details")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePlan(long id, [FromBody] PlanInput input, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        var error = ValidatePlan(input, false);
        if (error is not null) return ValidationProblem(detail: error, statusCode: 400);
        var plan = await db.SalesPlanReferences.SingleOrDefaultAsync(x => x.Id == id && x.IdStockOwnerType == 1, cancellationToken);
        if (plan is null) return NotFound();
        if (plan.IsRegular) return ValidationProblem(detail: "مشخصات طرح عادی قابل تغییر نیست.", statusCode: 400);
        var code = input.Code!.Trim();
        if (await db.SalesPlanReferences.AnyAsync(x => x.Id != id && x.IdStockOwnerType == 1 && x.Code == code, cancellationToken))
            return Conflict(new { message = "این کد قبلاً برای طرح دیگری ثبت شده است." });
        plan.DisplayName = input.DisplayName!.Trim();
        plan.Code = code;
        plan.PlanType = input.PlanType!.Trim().ToLowerInvariant();
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "مشخصات طرح فروش ذخیره شد." });
    }

    [HttpPut("plans/{id:long}/publication")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPlanPublication(long id, [FromBody] PlanPublicationInput input, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        var plan = await db.SalesPlanReferences.SingleOrDefaultAsync(x => x.Id == id && x.IdStockOwnerType == 1, cancellationToken);
        if (plan is null) return NotFound();
        if (plan.IsRegular && !input.IsActive) return ValidationProblem(detail: "طرح عادی همیشه فعال می‌ماند.", statusCode: 400);
        plan.IsActive = input.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = input.IsActive ? "طرح منتشر و فعال شد." : "طرح از فروش خارج شد و سوابق آن حفظ شد." });
    }

    [HttpDelete("plans/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePlan(long id, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        var plan = await db.SalesPlanReferences.SingleOrDefaultAsync(x => x.Id == id && x.IdStockOwnerType == 1, cancellationToken);
        if (plan is null) return NotFound();
        if (plan.IsRegular) return Conflict(new { message = "طرح عادی حذف‌شدنی نیست." });
        bool referenced = await db.AccountCartItems.AnyAsync(x => x.IdSalesPlan == id, cancellationToken) ||
                          await db.StoreCustomerOrderItems.AnyAsync(x => x.IdSalesPlan == id, cancellationToken) ||
                          await db.StoreProductSalesPlanOffers.AnyAsync(x => x.IdSalesPlan == id, cancellationToken) ||
                          await db.StoreProductVariantSalesPlanOffers.AnyAsync(x => x.IdSalesPlan == id, cancellationToken) ||
                          await db.StockBalances.AnyAsync(x => x.IdSalesPlanReference == id, cancellationToken) ||
                          await db.WarehouseDocuments.AnyAsync(x => x.IdSalesPlanReference == id, cancellationToken) ||
                          await db.Warehouses.AnyAsync(x => x.IdOwnerSalesPlanReference == id, cancellationToken);
        if (referenced) return Conflict(new { message = "این طرح سابقه یا اتصال دارد؛ برای حفظ سابقه آن را غیرفعال کنید." });
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var warehouseIds = await db.Warehouses.Where(x => x.IsSalesWarehouse).Select(x => x.Id).ToListAsync(cancellationToken);
        var links = await db.WarehouseStockOwners.Where(x => x.IdStockOwner == id && warehouseIds.Contains(x.IdWarehouse)).ToListAsync(cancellationToken);
        db.WarehouseStockOwners.RemoveRange(links); db.SalesPlanReferences.Remove(plan);
        await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "طرح فروش حذف شد." });
    }

    private static string? ValidatePlan(PlanInput input, bool creating)
    {
        if (creating && (string.IsNullOrWhiteSpace(input.Key) || input.Key.Length > 64 || !System.Text.RegularExpressions.Regex.IsMatch(input.Key, "^[a-z0-9][a-z0-9-]*$"))) return "کلید طرح باید حداکثر ۶۴ نویسه و شامل حروف کوچک انگلیسی، عدد و خط تیره باشد.";
        if (string.IsNullOrWhiteSpace(input.Code) || input.Code.Length > 64 || string.IsNullOrWhiteSpace(input.DisplayName) || input.DisplayName.Length > 200 || string.IsNullOrWhiteSpace(input.PlanType) || input.PlanType.Length > 30) return "کد، نام و نوع طرح الزامی و باید در محدودهٔ مجاز باشند.";
        if (input.StartsAtUtc.HasValue && input.EndsAtUtc.HasValue && input.StartsAtUtc >= input.EndsAtUtc || input.Label?.Length > 100 || input.ShowCountdown && !input.EndsAtUtc.HasValue) return "بازه یا برچسب طرح نامعتبر است؛ شمارش معکوس به تاریخ پایان نیاز دارد.";
        return null;
    }
    private static string? NormalizeLabel(string? label) => string.IsNullOrWhiteSpace(label) ? null : label.Trim();

    [HttpPut("plans/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePlan(long id, [FromBody] PlanSettingsInput input, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        if (id < 1 || input.Label?.Length > 100 ||
            (input.StartsAtUtc.HasValue && input.EndsAtUtc.HasValue && input.StartsAtUtc >= input.EndsAtUtc) ||
            (input.ShowCountdown && !input.EndsAtUtc.HasValue))
            return ValidationProblem(detail: "بازهٔ طرح یا برچسب نامعتبر است؛ شمارش معکوس به تاریخ پایان نیاز دارد.", statusCode: 400);
        var plan = await db.SalesPlanReferences.SingleOrDefaultAsync(x => x.Id == id && x.IdStockOwnerType == 1, cancellationToken);
        if (plan is null) return NotFound();
        if (plan.IsRegular && (input.StartsAtUtc.HasValue || input.EndsAtUtc.HasValue || input.ShowCountdown))
            return ValidationProblem(detail: "برای طرح عادی بازه یا شمارش معکوس تعیین نمی‌شود.", statusCode: 400);
        plan.StartsAtUtc = input.StartsAtUtc?.ToUniversalTime();
        plan.EndsAtUtc = input.EndsAtUtc?.ToUniversalTime();
        plan.Label = string.IsNullOrWhiteSpace(input.Label) ? null : input.Label.Trim();
        plan.ShowCountdown = input.ShowCountdown;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "تنظیمات طرح فروش ذخیره شد." });
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (!CanManage()) return Forbid();
        var rows = await (from variant in db.ProductVariants.AsNoTracking()
                          join product in db.Products.AsNoTracking() on variant.IdProduct equals product.Id
                          from plan in db.SalesPlanReferences.AsNoTracking().Where(x => x.IdStockOwnerType == 1)
                          join offer in db.StoreProductVariantSalesPlanOffers.AsNoTracking() on new { VariantId = variant.Id, PlanId = plan.Id } equals new { VariantId = offer.IdProductVariant, PlanId = offer.IdSalesPlan } into offers
                          from offer in offers.DefaultIfEmpty()
                          where variant.IsActive && product.IsWarehouseEnabled && product.IsCatalogActive && product.IsStoreEnabled
                          orderby product.Name, variant.Name, plan.Code
                          select new VariantPlanPriceRow { VariantId = variant.Id, ProductName = product.Name, VariantName = variant.Name, Code = variant.Code, AttributesJson = variant.AttributesJson, SalesPlanId = plan.StoreSalesPlanKey, SalesPlanName = plan.DisplayName, PriceAmount = offer == null ? null : offer.PriceAmount, DiscountPercent = offer == null ? null : offer.DiscountPercent, IsEnabled = offer == null ? null : offer.IsEnabled }).ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpPut("{variantId:long}/{salesPlanId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(long variantId, string salesPlanId, [FromBody] VariantPlanPriceInput input, CancellationToken cancellationToken)
    {
        if (!CanManage()) return Forbid();
        if (variantId < 1 || string.IsNullOrWhiteSpace(salesPlanId) || salesPlanId.Length > 64 || input.PriceAmount < 0 || input.PriceAmount != decimal.Truncate(input.PriceAmount) || input.DiscountPercent is < 0 or >= 100) return ValidationProblem(detail: "قیمت باید تومان صحیح و تخفیف بین صفر تا کمتر از صد درصد باشد.", statusCode: 400);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var plan = await db.SalesPlanReferences.SingleOrDefaultAsync(x => x.StoreSalesPlanKey == salesPlanId && x.IsActive && x.IdStockOwnerType == 1, cancellationToken);
        if (plan is null) return NotFound(new { code = "variant-or-plan-not-found" });
        bool valid = await db.ProductVariants.AnyAsync(v => v.Id == variantId && v.IsActive && db.Products.Any(p => p.Id == v.IdProduct && p.IsWarehouseEnabled && p.IsCatalogActive && p.IsStoreEnabled), cancellationToken);
        if (!valid) return NotFound(new { code = "variant-or-plan-not-found" });
        var offer = await db.StoreProductVariantSalesPlanOffers.SingleOrDefaultAsync(x => x.IdProductVariant == variantId && x.IdSalesPlan == plan.Id, cancellationToken);
        if (offer is null) db.StoreProductVariantSalesPlanOffers.Add(new Shop.Data.Domain.Store.Entities.StoreProductVariantSalesPlanOffer { IdProductVariant = variantId, IdSalesPlan = plan.Id, PriceAmount = input.PriceAmount, DiscountPercent = input.DiscountPercent, IsEnabled = input.IsEnabled, CreatedAtUtc = DateTime.UtcNow });
        else { offer.PriceAmount = input.PriceAmount; offer.DiscountPercent = input.DiscountPercent; offer.IsEnabled = input.IsEnabled; offer.UpdatedAtUtc = DateTime.UtcNow; }
        await db.SaveChangesAsync(cancellationToken);
        long productId = await db.ProductVariants.Where(x => x.Id == variantId).Select(x => x.IdProduct).SingleAsync(cancellationToken);
        long priceVersionId = await AppendPriceVersionAsync(StorePriceScopes.Variant(variantId, plan.Id), productId, plan.Id, StorePriceKinds.Variant, variantId, input.PriceAmount, input.DiscountPercent, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { saved = true, priceVersionId, message = "قیمت تنوع و طرح فروش ذخیره شد." });
    }

    [HttpDelete("{variantId:long}/{salesPlanId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear(long variantId, string salesPlanId, CancellationToken cancellationToken)
    {
        if (!CanManage()) return Forbid();
        if (variantId < 1 || string.IsNullOrWhiteSpace(salesPlanId) || salesPlanId.Length > 64) return ValidationProblem(detail: "تنوع یا طرح فروش معتبر نیست.", statusCode: 400);
        var plan = await db.SalesPlanReferences.SingleOrDefaultAsync(x => x.StoreSalesPlanKey == salesPlanId, cancellationToken);
        if (plan is not null) { var offer = await db.StoreProductVariantSalesPlanOffers.SingleOrDefaultAsync(x => x.IdProductVariant == variantId && x.IdSalesPlan == plan.Id, cancellationToken); if (offer is not null) { db.StoreProductVariantSalesPlanOffers.Remove(offer); var prior = await db.StorePriceVersions.SingleOrDefaultAsync(x => x.ScopeKey == StorePriceScopes.Variant(variantId, plan.Id) && x.IsActive, cancellationToken); if (prior is not null) prior.IsActive = false; await db.SaveChangesAsync(cancellationToken); } }
        return Ok(new { saved = true, message = "قیمت اختصاصی حذف شد و قیمت پایهٔ کالا/طرح اعمال می‌شود." });
    }

    private bool CanManage() => User.IsInRole("system.full-access") ||
                                User.HasClaim("admin_permission", "Store:Features:Index:promotions");

    private async Task<long> AppendPriceVersionAsync(string scope, long productId, long planId, string kind, long? targetId, decimal amount, decimal discount, CancellationToken ct)
    {
        var prior = await db.StorePriceVersions.SingleOrDefaultAsync(x => x.ScopeKey == scope && x.IsActive, ct);
        if (prior is not null) prior.IsActive = false;
        var version = new StorePriceVersion { ScopeKey = scope, IdProduct = productId, IdSalesPlan = planId, PriceKind = kind, TargetId = targetId, PriceAmount = amount, DiscountPercent = discount, IsActive = true, CreatedAtUtc = DateTime.UtcNow, ChangedBy = User.Identity?.Name, PreviousVersionId = prior?.Id };
        db.StorePriceVersions.Add(version);
        await db.SaveChangesAsync(ct);
        return version.Id;
    }

    public sealed class VariantPlanPriceRow
    {
        public long VariantId { get; set; }
        public string ProductName { get; set; } = "";
        public string VariantName { get; set; } = "";
        public string? Code { get; set; }
        public string? AttributesJson { get; set; }
        public string SalesPlanId { get; set; } = "";
        public string SalesPlanName { get; set; } = "";
        public decimal? PriceAmount { get; set; }
        public decimal? DiscountPercent { get; set; }
        public bool? IsEnabled { get; set; }
    }

    public sealed record VariantPlanPriceInput(decimal PriceAmount, decimal DiscountPercent, bool IsEnabled);
    public sealed record PlanSettingsInput(DateTimeOffset? StartsAtUtc, DateTimeOffset? EndsAtUtc, string? Label, bool ShowCountdown);
    public sealed record PlanInput(string? Key, string? Code, string? DisplayName, string? PlanType, DateTimeOffset? StartsAtUtc, DateTimeOffset? EndsAtUtc, string? Label, bool ShowCountdown);
    public sealed record PlanPublicationInput(bool IsActive);

    [HttpGet("product-offers")]
    public async Task<IActionResult> ProductOffers(CancellationToken cancellationToken)
    {
        if (!CanManage()) return Forbid();
        var rows = await (from product in db.Products.AsNoTracking()
                          from plan in db.SalesPlanReferences.AsNoTracking().Where(x => x.IdStockOwnerType == 1)
                          join offer in db.StoreProductSalesPlanOffers.AsNoTracking() on new { ProductId = product.Id, PlanId = plan.Id } equals new { ProductId = offer.IdProduct, PlanId = offer.IdSalesPlan } into offers
                          from offer in offers.DefaultIfEmpty()
                          where product.IsStoreEnabled && product.IsCatalogActive && !plan.IsRegular
                          orderby product.Name, plan.DisplayName
                          select new ProductOfferRow { ProductId = product.Id, ProductCode = product.Code, ProductName = product.Name, SalesPlanId = plan.StoreSalesPlanKey, SalesPlanName = plan.DisplayName, PriceAmount = offer == null ? null : offer.PriceAmount, DiscountPercent = offer == null ? null : offer.DiscountPercent, MaxPurchaseQuantity = offer == null ? null : offer.MaxPurchaseQuantity, IsEnabled = offer == null ? null : offer.IsEnabled }).ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpPut("product-offers/{productId:long}/{salesPlanId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProductOffer(long productId, string salesPlanId, [FromBody] ProductOfferInput input, CancellationToken cancellationToken)
    {
        if (!CanManage()) return Forbid();
        if (productId < 1 || string.IsNullOrWhiteSpace(salesPlanId) || salesPlanId.Length > 128 || input.PriceAmount < 0 || input.PriceAmount > 9007199254740991m || input.PriceAmount != decimal.Truncate(input.PriceAmount) || input.DiscountPercent is < 0 or >= 100 || decimal.Round(input.DiscountPercent, 2) != input.DiscountPercent || input.MaxPurchaseQuantity is < 1 or > 99) return ValidationProblem(detail: "قیمت، تخفیف یا حداکثر خرید معتبر نیست.", statusCode: 400);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var plan = await db.SalesPlanReferences.SingleOrDefaultAsync(x => x.StoreSalesPlanKey == salesPlanId && x.IdStockOwnerType == 1 && !x.IsRegular, cancellationToken);
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == productId && x.IsStoreEnabled && x.IsCatalogActive, cancellationToken);
        if (plan is null || product is null) return NotFound(new { code = "product-or-plan-not-found" });
        var offer = await db.StoreProductSalesPlanOffers.SingleOrDefaultAsync(x => x.IdProduct == productId && x.IdSalesPlan == plan.Id, cancellationToken);
        if (offer is null) {
            offer = new Shop.Data.Domain.Store.Entities.StoreProductSalesPlanOffer { IdProduct = productId, IdSalesPlan = plan.Id, PriceAmount = input.PriceAmount, DiscountPercent = input.DiscountPercent, MaxPurchaseQuantity = input.MaxPurchaseQuantity, IsEnabled = input.IsEnabled, CreatedAtUtc = DateTime.UtcNow };
            db.StoreProductSalesPlanOffers.Add(offer); await db.SaveChangesAsync(cancellationToken);
            var accessorySeed = await (from pa in db.StoreProductAccessories join accessory in db.StoreAccessories on pa.IdAccessory equals accessory.Id where pa.IdProduct == productId && accessory.IsActive select new { pa.IdAccessory, pa.PriceAmount, pa.IsRequired }).ToListAsync(cancellationToken);
            db.StoreProductSalesPlanOfferAccessories.AddRange(accessorySeed.Select(x => new Shop.Data.Domain.Store.Entities.StoreProductSalesPlanOfferAccessory { IdProductSalesPlanOffer = offer.Id, IdAccessory = x.IdAccessory, PriceAmount = x.PriceAmount, IsIncludedByDefault = x.IsRequired, IsAllowed = true }));
        } else { offer.PriceAmount = input.PriceAmount; offer.DiscountPercent = input.DiscountPercent; offer.MaxPurchaseQuantity = input.MaxPurchaseQuantity; offer.IsEnabled = input.IsEnabled; offer.UpdatedAtUtc = DateTime.UtcNow; }
        await db.SaveChangesAsync(cancellationToken);
        long priceVersionId = await AppendPriceVersionAsync(StorePriceScopes.Product(productId, plan.Id), productId, plan.Id, StorePriceKinds.Product, null, input.PriceAmount, input.DiscountPercent, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { priceVersionId, message = "پیشنهاد کالا در طرح ذخیره شد." });
    }

    public sealed class ProductOfferRow
    {
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string SalesPlanId { get; set; } = "";
        public string SalesPlanName { get; set; } = "";
        public decimal? PriceAmount { get; set; }
        public decimal? DiscountPercent { get; set; }
        public int? MaxPurchaseQuantity { get; set; }
        public bool? IsEnabled { get; set; }
    }
    public sealed record ProductOfferInput(decimal PriceAmount, decimal DiscountPercent, int MaxPurchaseQuantity, bool IsEnabled);

    [HttpGet("product-offers/{productId:long}/{salesPlanId}/accessories")]
    public async Task<IActionResult> OfferAccessories(long productId, string salesPlanId, CancellationToken cancellationToken)
    {
        if (!CanManage()) return Forbid();
        if (productId < 1 || string.IsNullOrWhiteSpace(salesPlanId) || salesPlanId.Length > 128) return BadRequest();
        var offer = await FindOffer(productId, salesPlanId, cancellationToken);
        if (offer is null) return NotFound(new { title = "ابتدا پیشنهاد این کالا و طرح را ذخیره کنید." });
        var rows = await (from pa in db.StoreProductAccessories.AsNoTracking()
                          join accessory in db.StoreAccessories.AsNoTracking() on pa.IdAccessory equals accessory.Id
                          join oa in db.StoreProductSalesPlanOfferAccessories.AsNoTracking().Where(x => x.IdProductSalesPlanOffer == offer.OfferId) on accessory.Id equals oa.IdAccessory into overrides
                          from oa in overrides.DefaultIfEmpty()
                          where pa.IdProduct == productId && accessory.IsActive
                          orderby pa.SortOrder, accessory.Name
                          select new OfferAccessoryRow { AccessoryId = accessory.Id, Name = accessory.Name, IsRequired = pa.IsRequired, BasePriceAmount = pa.PriceAmount, PriceAmount = oa == null ? null : oa.PriceAmount, IsAllowed = oa != null && oa.IsAllowed }).ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpPut("product-offers/{productId:long}/{salesPlanId}/accessories/{accessoryId:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveOfferAccessory(long productId, string salesPlanId, long accessoryId, [FromBody] OfferAccessoryInput input, CancellationToken cancellationToken)
    {
        if (!CanManage()) return Forbid();
        if (productId < 1 || accessoryId < 1 || string.IsNullOrWhiteSpace(salesPlanId) || salesPlanId.Length > 128 || input.PriceAmount < 0 || input.PriceAmount > 9007199254740991m || input.PriceAmount != decimal.Truncate(input.PriceAmount)) return ValidationProblem(detail: "قیمت متعلقه باید مبلغ صحیح و غیرمنفی باشد.", statusCode: 400);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var offer = await FindOffer(productId, salesPlanId, cancellationToken);
        var productAccessory = await (from pa in db.StoreProductAccessories where pa.IdProduct == productId && pa.IdAccessory == accessoryId join accessory in db.StoreAccessories on pa.IdAccessory equals accessory.Id where accessory.IsActive select pa).SingleOrDefaultAsync(cancellationToken);
        if (offer is null || productAccessory is null) return NotFound(new { title = "پیشنهاد یا متعلقهٔ فعال کالا یافت نشد." });
        if (productAccessory.IsRequired && !input.IsAllowed) return BadRequest(new { title = "متعلقهٔ اجباری باید در پیشنهاد مجاز باشد." });
        var overrideRow = await db.StoreProductSalesPlanOfferAccessories.SingleOrDefaultAsync(x => x.IdProductSalesPlanOffer == offer.OfferId && x.IdAccessory == accessoryId, cancellationToken);
        if (overrideRow is null) db.StoreProductSalesPlanOfferAccessories.Add(new Shop.Data.Domain.Store.Entities.StoreProductSalesPlanOfferAccessory { IdProductSalesPlanOffer = offer.OfferId, IdAccessory = accessoryId, PriceAmount = input.PriceAmount, IsIncludedByDefault = productAccessory.IsRequired, IsAllowed = input.IsAllowed });
        else { overrideRow.PriceAmount = input.PriceAmount; overrideRow.IsAllowed = input.IsAllowed; }
        await db.SaveChangesAsync(cancellationToken);
        long planId = await db.SalesPlanReferences.Where(x => x.StoreSalesPlanKey == salesPlanId).Select(x => x.Id).SingleAsync(cancellationToken);
        long priceVersionId = await AppendPriceVersionAsync(StorePriceScopes.Accessory(productId, accessoryId, planId), productId, planId, StorePriceKinds.Accessory, accessoryId, input.PriceAmount, 0, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { priceVersionId, message = "متعلقهٔ پیشنهاد ذخیره شد." });
    }

    public sealed class OfferAccessoryRow { public long AccessoryId { get; set; } public string Name { get; set; } = ""; public bool IsRequired { get; set; } public decimal BasePriceAmount { get; set; } public decimal? PriceAmount { get; set; } public bool IsAllowed { get; set; } }
    public sealed class OfferAccessoryMatch { public long OfferId { get; set; } public bool IsRequired { get; set; } }
    public sealed record OfferAccessoryInput(decimal PriceAmount, bool IsAllowed);

    [HttpGet("product-offers/{productId:long}/{salesPlanId}/specifications")]
    public async Task<IActionResult> OfferSpecifications(long productId, string salesPlanId, CancellationToken cancellationToken)
    {
        if (!CanManage()) return Forbid();
        if (productId < 1 || string.IsNullOrWhiteSpace(salesPlanId) || salesPlanId.Length > 128) return BadRequest();
        var offer = await FindOffer(productId, salesPlanId, cancellationToken);
        if (offer is null) return NotFound(new { title = "ابتدا پیشنهاد این کالا و طرح را ذخیره کنید." });
        return Ok(await db.StoreProductSalesPlanOfferSpecificationOverrides.AsNoTracking().Where(x => x.IdProductSalesPlanOffer == offer.OfferId).OrderBy(x => x.Id).Select(x => new OfferSpecification { Key = x.SpecificationKey, Value = x.Value }).ToListAsync(cancellationToken));
    }

    [HttpPut("product-offers/{productId:long}/{salesPlanId}/specifications")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveOfferSpecifications(long productId, string salesPlanId, [FromBody] List<OfferSpecification> input, CancellationToken cancellationToken)
    {
        if (!CanManage()) return Forbid();
        if (productId < 1 || string.IsNullOrWhiteSpace(salesPlanId) || salesPlanId.Length > 128 || input is null || input.Count > 50 || input.Any(x => x is null)) return BadRequest(new { title = "مشخصات معتبر نیستند." });
        var specs = input.Select(x => new OfferSpecification { Key = x.Key?.Trim() ?? "", Value = x.Value?.Trim() ?? "" }).ToList();
        if (specs.Any(x => x.Key.Length is < 1 or > 80 || x.Value.Length is < 1 or > 240 || x.Key.Contains('|') || x.Key.Any(char.IsControl) || x.Value.Any(char.IsControl)) || specs.Select(x => x.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() != specs.Count) return BadRequest(new { title = "عنوان‌ها باید یکتا و مقدارها در محدودهٔ مجاز باشند." });
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var offer = await FindOffer(productId, salesPlanId, cancellationToken);
        if (offer is null) return NotFound(new { title = "پیشنهاد کالا و طرح یافت نشد." });
        var oldRows = await db.StoreProductSalesPlanOfferSpecificationOverrides.Where(x => x.IdProductSalesPlanOffer == offer.OfferId).ToListAsync(cancellationToken);
        db.StoreProductSalesPlanOfferSpecificationOverrides.RemoveRange(oldRows);
        db.StoreProductSalesPlanOfferSpecificationOverrides.AddRange(specs.Select(x => new Shop.Data.Domain.Store.Entities.StoreProductSalesPlanOfferSpecificationOverride { IdProductSalesPlanOffer = offer.OfferId, SpecificationKey = x.Key, Value = x.Value }));
        try { await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); }
        catch (DbUpdateException) { return Conflict(new { title = "عنوان مشخصات تکراری است." }); }
        return Ok(new { message = "مشخصات پیشنهاد ذخیره شد." });
    }

    private async Task<OfferAccessoryMatch?> FindOffer(long productId, string salesPlanId, CancellationToken cancellationToken)
    {
        var row = await (from offer in db.StoreProductSalesPlanOffers.AsNoTracking()
                         join product in db.Products.AsNoTracking() on offer.IdProduct equals product.Id
                         join plan in db.SalesPlanReferences.AsNoTracking() on offer.IdSalesPlan equals plan.Id
                         where offer.IdProduct == productId && plan.StoreSalesPlanKey == salesPlanId && plan.IdStockOwnerType == 1 && !plan.IsRegular && product.IsStoreEnabled && product.IsCatalogActive
                         select new OfferAccessoryMatch { OfferId = offer.Id, IsRequired = false }).SingleOrDefaultAsync(cancellationToken);
        return row;
    }

    public sealed class OfferSpecification { public string Key { get; set; } = ""; public string Value { get; set; } = ""; }
}
