using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/variant-prices")]
public sealed class StoreVariantPricesAdminApiController(ShopDbContext db) : ControllerBase
{
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
            return ValidationProblem("قیمت باید تومان صحیح و تخفیف بین صفر تا کمتر از صد درصد باشد.");

        int valid = await db.Database.SqlQueryRaw<int>("""
                                                       SELECT COUNT(*) AS [Value]
                                                       FROM [Warehouse].[ProductVariant] variant
                                                       INNER JOIN [Warehouse].[Product] product ON product.[Id] = variant.[IdProduct] AND product.[IsWarehouseEnabled] = 1 AND product.[IsCatalogActive] = 1 AND product.[IsStoreEnabled] = 1

                                                       INNER JOIN [Warehouse].[SalesPlanReference] [plan] ON [plan].[SalesPlanKey] = {0} AND [plan].[IsEnabled] = 1 AND [plan].[IdStockOwnerType] = 1
                                                       WHERE variant.[Id] = {1} AND variant.[IsActive] = 1;
                                                       """, salesPlanId, variantId).SingleAsync(cancellationToken);
        if (valid == 0) return NotFound(new { code = "variant-or-plan-not-found" });

        await db.Database.ExecuteSqlInterpolatedAsync($"""
                                                       IF EXISTS (SELECT 1 FROM [Store].[ProductVariantSalesPlanOffer] WHERE [IdProductVariant] = {variantId} AND [IdSalesPlan] = (SELECT [Id] FROM [Warehouse].[SalesPlanReference] WHERE [SalesPlanKey] = {salesPlanId}))
                                                           UPDATE [Store].[ProductVariantSalesPlanOffer]
                                                           SET [PriceAmount] = {input.PriceAmount}, [DiscountPercent] = {input.DiscountPercent}, [IsEnabled] = {input.IsEnabled}, [UpdatedAtUtc] = SYSUTCDATETIME()
                                                           WHERE [IdProductVariant] = {variantId} AND [IdSalesPlan] = (SELECT [Id] FROM [Warehouse].[SalesPlanReference] WHERE [SalesPlanKey] = {salesPlanId});
                                                       ELSE
                                                           INSERT INTO [Store].[ProductVariantSalesPlanOffer] ([IdProductVariant], [IdSalesPlan], [PriceAmount], [DiscountPercent], [IsEnabled])
                                                           SELECT {variantId}, [Id], {input.PriceAmount}, {input.DiscountPercent}, {input.IsEnabled}
                                                           FROM [Warehouse].[SalesPlanReference] WHERE [SalesPlanKey] = {salesPlanId};
                                                       """, cancellationToken);
        return Ok(new { saved = true, message = "قیمت تنوع و طرح فروش ذخیره شد." });
    }

    [HttpDelete("{variantId:long}/{salesPlanId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear(long variantId, string salesPlanId, CancellationToken cancellationToken)
    {
        if (!CanManage()) return Forbid();
        if (variantId < 1 || string.IsNullOrWhiteSpace(salesPlanId) || salesPlanId.Length > 64)
            return ValidationProblem("تنوع یا طرح فروش معتبر نیست.");
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
}
