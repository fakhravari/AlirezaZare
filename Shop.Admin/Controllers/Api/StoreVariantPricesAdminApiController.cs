using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Infrastructure.Persistence;

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
                x.IsActive, x.StartsAtUtc, x.EndsAtUtc, x.Label, x.ShowCountdown }).ToListAsync(cancellationToken));
    }

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
        const string sql = """
                           SELECT variant.[Id] AS [VariantId], product.[Name] AS [ProductName], variant.[Name] AS [VariantName],
                                  variant.[Code] AS [Code], variant.[AttributesJson] AS [AttributesJson], [plan].[SalesPlanKey] AS [SalesPlanId],
                                  [plan].[Name] AS [SalesPlanName], offer.[PriceAmount] AS [PriceAmount], offer.[DiscountPercent] AS [DiscountPercent],
                                  offer.[IsEnabled] AS [IsEnabled]
                           FROM [Warehouse].[ProductVariant] variant
                           INNER JOIN [Warehouse].[Product] product ON product.[Id] = variant.[IdProduct] AND product.[IsWarehouseEnabled] = 1 AND product.[IsCatalogActive] = 1 AND product.[IsStoreEnabled] = 1

                           CROSS JOIN [Warehouse].[SalesPlanReference] [plan]
                           LEFT JOIN [Store].[ProductVariantSalesPlanOffer] offer ON offer.[IdProductVariant] = variant.[Id] AND offer.[IdSalesPlan] = [plan].[Id]
                           WHERE variant.[IsActive] = 1 AND [plan].[IdStockOwnerType] = 1
                           ORDER BY product.[Name], variant.[Name], [plan].[Code];
                           """;
        return Ok(await db.Database.SqlQueryRaw<VariantPlanPriceRow>(sql).ToListAsync(cancellationToken));
    }

    [HttpPut("{variantId:long}/{salesPlanId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(long variantId, string salesPlanId,
        [FromBody] VariantPlanPriceInput input, CancellationToken cancellationToken)
    {
        if (!CanManage()) return Forbid();
        if (variantId < 1 || string.IsNullOrWhiteSpace(salesPlanId) || salesPlanId.Length > 64 ||
            input.PriceAmount < 0 || input.PriceAmount != decimal.Truncate(input.PriceAmount) ||
            input.DiscountPercent is < 0 or >= 100)
            return ValidationProblem(detail: "قیمت باید تومان صحیح و تخفیف بین صفر تا کمتر از صد درصد باشد.", statusCode: 400);

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        int valid = await db.Database.SqlQueryRaw<int>("""
                                                       SELECT COUNT(*) AS [Value]
                                                       FROM [Warehouse].[ProductVariant] variant
                                                       INNER JOIN [Warehouse].[Product] product ON product.[Id] = variant.[IdProduct] AND product.[IsWarehouseEnabled] = 1 AND product.[IsCatalogActive] = 1 AND product.[IsStoreEnabled] = 1

                                                       INNER JOIN [Warehouse].[SalesPlanReference] [plan] ON [plan].[SalesPlanKey] = {0} AND [plan].[IsEnabled] = 1 AND [plan].[IdStockOwnerType] = 1
                                                       WHERE variant.[Id] = {1} AND variant.[IsActive] = 1;
                                                       """, salesPlanId, variantId).SingleAsync(cancellationToken);
        if (valid == 0) return NotFound(new { code = "variant-or-plan-not-found" });

        await db.Database.ExecuteSqlInterpolatedAsync($"""
                                                       IF EXISTS (SELECT 1 FROM [Store].[ProductVariantSalesPlanOffer] WITH (UPDLOCK, HOLDLOCK) WHERE [IdProductVariant] = {variantId} AND [IdSalesPlan] = (SELECT [Id] FROM [Warehouse].[SalesPlanReference] WHERE [SalesPlanKey] = {salesPlanId}))
                                                           UPDATE [Store].[ProductVariantSalesPlanOffer]
                                                           SET [PriceAmount] = {input.PriceAmount}, [DiscountPercent] = {input.DiscountPercent}, [IsEnabled] = {input.IsEnabled}, [UpdatedAtUtc] = SYSUTCDATETIME()
                                                           WHERE [IdProductVariant] = {variantId} AND [IdSalesPlan] = (SELECT [Id] FROM [Warehouse].[SalesPlanReference] WHERE [SalesPlanKey] = {salesPlanId});
                                                       ELSE
                                                           INSERT INTO [Store].[ProductVariantSalesPlanOffer] ([IdProductVariant], [IdSalesPlan], [PriceAmount], [DiscountPercent], [IsEnabled])
                                                           SELECT {variantId}, [Id], {input.PriceAmount}, {input.DiscountPercent}, {input.IsEnabled}
                                                           FROM [Warehouse].[SalesPlanReference] WHERE [SalesPlanKey] = {salesPlanId};
                                                       """, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { saved = true, message = "قیمت تنوع و طرح فروش ذخیره شد." });
    }

    [HttpDelete("{variantId:long}/{salesPlanId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear(long variantId, string salesPlanId, CancellationToken cancellationToken)
    {
        if (!CanManage()) return Forbid();
        if (variantId < 1 || string.IsNullOrWhiteSpace(salesPlanId) || salesPlanId.Length > 64)
            return ValidationProblem(detail: "تنوع یا طرح فروش معتبر نیست.", statusCode: 400);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
                                                       DELETE offer FROM [Store].[ProductVariantSalesPlanOffer] offer
                                                       INNER JOIN [Warehouse].[SalesPlanReference] [plan] ON [plan].[Id] = offer.[IdSalesPlan]
                                                       WHERE offer.[IdProductVariant] = {variantId} AND [plan].[SalesPlanKey] = {salesPlanId};
                                                       """, cancellationToken);
        return Ok(new { saved = true, message = "قیمت اختصاصی حذف شد و قیمت پایهٔ کالا/طرح اعمال می‌شود." });
    }

    private bool CanManage() => User.IsInRole("system.full-access") ||
                                User.HasClaim("admin_permission", "Store:Features:Index:promotions");

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
}
