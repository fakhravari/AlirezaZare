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

    [HttpGet("product-offers")]
    public async Task<IActionResult> ProductOffers(CancellationToken cancellationToken)
    {
        if (!CanManage()) return Forbid();
        return Ok(await db.Database.SqlQueryRaw<ProductOfferRow>("""
            SELECT p.[Id] AS [ProductId], p.[Code] AS [ProductCode], p.[Name] AS [ProductName],
                   s.[SalesPlanKey] AS [SalesPlanId], s.[Name] AS [SalesPlanName],
                   o.[PriceAmount], o.[DiscountPercent], o.[MaxPurchaseQuantity], o.[IsEnabled]
            FROM [Warehouse].[Product] p CROSS JOIN [Warehouse].[SalesPlanReference] s
            LEFT JOIN [Store].[ProductSalesPlanOffer] o ON o.[IdProduct] = p.[Id] AND o.[IdSalesPlan] = s.[Id]
            WHERE p.[IsStoreEnabled] = 1 AND p.[IsCatalogActive] = 1 AND s.[IdStockOwnerType] = 1 AND s.[IsDefault] = 0
            ORDER BY p.[Name], s.[Name]
            """).ToListAsync(cancellationToken));
    }

    [HttpPut("product-offers/{productId:long}/{salesPlanId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProductOffer(long productId, string salesPlanId, [FromBody] ProductOfferInput input, CancellationToken cancellationToken)
    {
        if (!CanManage()) return Forbid();
        if (productId < 1 || string.IsNullOrWhiteSpace(salesPlanId) || salesPlanId.Length > 128 ||
            input.PriceAmount < 0 || input.PriceAmount > 9007199254740991m || input.PriceAmount != decimal.Truncate(input.PriceAmount) ||
            input.DiscountPercent is < 0 or >= 100 || decimal.Round(input.DiscountPercent, 2) != input.DiscountPercent || input.MaxPurchaseQuantity is < 1 or > 99)
            return ValidationProblem(detail: "قیمت، تخفیف یا حداکثر خرید معتبر نیست.", statusCode: 400);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        int valid = await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS [Value] FROM [Warehouse].[Product] p CROSS JOIN [Warehouse].[SalesPlanReference] s
            WHERE p.[Id] = {productId} AND p.[IsStoreEnabled] = 1 AND p.[IsCatalogActive] = 1
              AND s.[SalesPlanKey] = {salesPlanId} AND s.[IdStockOwnerType] = 1 AND s.[IsDefault] = 0
            """).SingleAsync(cancellationToken);
        if (valid != 1) return NotFound(new { code = "product-or-plan-not-found" });
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            DECLARE @planId BIGINT = (SELECT [Id] FROM [Warehouse].[SalesPlanReference] WHERE [SalesPlanKey] = {salesPlanId});
            DECLARE @offerId BIGINT = (SELECT [Id] FROM [Store].[ProductSalesPlanOffer] WITH (UPDLOCK, HOLDLOCK) WHERE [IdProduct] = {productId} AND [IdSalesPlan] = @planId);
            IF @offerId IS NULL
            BEGIN
                INSERT [Store].[ProductSalesPlanOffer] ([IdProduct], [IdSalesPlan], [PriceAmount], [DiscountPercent], [MaxPurchaseQuantity], [IsEnabled])
                VALUES ({productId}, @planId, {input.PriceAmount}, {input.DiscountPercent}, {input.MaxPurchaseQuantity}, {input.IsEnabled});
                SET @offerId = SCOPE_IDENTITY();
                INSERT [Store].[ProductSalesPlanOfferAccessory] ([IdProductSalesPlanOffer], [IdAccessory], [PriceAmount], [IsIncludedByDefault], [IsAllowed])
                SELECT @offerId, pa.[IdAccessory], pa.[PriceAmount], pa.[IsRequired], 1 FROM [Store].[ProductAccessory] pa
                JOIN [Store].[Accessory] a ON a.[Id] = pa.[IdAccessory] AND a.[IsActive] = 1 WHERE pa.[IdProduct] = {productId};
            END
            ELSE
                UPDATE [Store].[ProductSalesPlanOffer] SET [PriceAmount] = {input.PriceAmount}, [DiscountPercent] = {input.DiscountPercent},
                    [MaxPurchaseQuantity] = {input.MaxPurchaseQuantity}, [IsEnabled] = {input.IsEnabled}, [UpdatedAtUtc] = SYSUTCDATETIME() WHERE [Id] = @offerId;
            """, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "پیشنهاد کالا در طرح ذخیره شد." });
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
        int exists = await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS [Value] FROM [Store].[ProductSalesPlanOffer] o
            JOIN [Warehouse].[SalesPlanReference] s ON s.[Id] = o.[IdSalesPlan]
            JOIN [Warehouse].[Product] p ON p.[Id] = o.[IdProduct]
            WHERE o.[IdProduct] = {productId} AND s.[SalesPlanKey] = {salesPlanId} AND s.[IsDefault] = 0 AND s.[IdStockOwnerType] = 1 AND p.[IsStoreEnabled] = 1 AND p.[IsCatalogActive] = 1
            """).SingleAsync(cancellationToken);
        if (exists != 1) return NotFound(new { title = "ابتدا پیشنهاد این کالا و طرح را ذخیره کنید." });
        return Ok(await db.Database.SqlQuery<OfferAccessoryRow>($"""
            SELECT a.[Id] AS [AccessoryId], a.[Name], pa.[IsRequired], pa.[PriceAmount] AS [BasePriceAmount],
                   oa.[PriceAmount], COALESCE(oa.[IsAllowed], CONVERT(bit, 0)) AS [IsAllowed]
            FROM [Store].[ProductAccessory] pa JOIN [Store].[Accessory] a ON a.[Id] = pa.[IdAccessory] AND a.[IsActive] = 1
            JOIN [Store].[ProductSalesPlanOffer] o ON o.[IdProduct] = pa.[IdProduct]
            JOIN [Warehouse].[SalesPlanReference] s ON s.[Id] = o.[IdSalesPlan] AND s.[SalesPlanKey] = {salesPlanId} AND s.[IsDefault] = 0 AND s.[IdStockOwnerType] = 1
            LEFT JOIN [Store].[ProductSalesPlanOfferAccessory] oa ON oa.[IdProductSalesPlanOffer] = o.[Id] AND oa.[IdAccessory] = a.[Id]
            WHERE pa.[IdProduct] = {productId} ORDER BY pa.[SortOrder], a.[Name]
            """).ToListAsync(cancellationToken));
    }

    [HttpPut("product-offers/{productId:long}/{salesPlanId}/accessories/{accessoryId:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveOfferAccessory(long productId, string salesPlanId, long accessoryId, [FromBody] OfferAccessoryInput input, CancellationToken cancellationToken)
    {
        if (!CanManage()) return Forbid();
        if (productId < 1 || accessoryId < 1 || string.IsNullOrWhiteSpace(salesPlanId) || salesPlanId.Length > 128 ||
            input.PriceAmount < 0 || input.PriceAmount > 9007199254740991m || input.PriceAmount != decimal.Truncate(input.PriceAmount))
            return ValidationProblem(detail: "قیمت متعلقه باید مبلغ صحیح و غیرمنفی باشد.", statusCode: 400);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var match = await db.Database.SqlQuery<OfferAccessoryMatch>($"""
            SELECT o.[Id] AS [OfferId], pa.[IsRequired] FROM [Store].[ProductSalesPlanOffer] o
            JOIN [Warehouse].[Product] p ON p.[Id] = o.[IdProduct] AND p.[IsStoreEnabled] = 1 AND p.[IsCatalogActive] = 1
            JOIN [Warehouse].[SalesPlanReference] s ON s.[Id] = o.[IdSalesPlan] AND s.[SalesPlanKey] = {salesPlanId} AND s.[IsDefault] = 0 AND s.[IdStockOwnerType] = 1
            JOIN [Store].[ProductAccessory] pa ON pa.[IdProduct] = o.[IdProduct] AND pa.[IdAccessory] = {accessoryId}
            JOIN [Store].[Accessory] a ON a.[Id] = pa.[IdAccessory] AND a.[IsActive] = 1 WHERE o.[IdProduct] = {productId}
            """).SingleOrDefaultAsync(cancellationToken);
        if (match is null) return NotFound(new { title = "پیشنهاد یا متعلقهٔ فعال کالا یافت نشد." });
        if (match.IsRequired && !input.IsAllowed) return BadRequest(new { title = "متعلقهٔ اجباری کالا باید در پیشنهاد مجاز باشد." });
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            IF EXISTS (SELECT 1 FROM [Store].[ProductSalesPlanOfferAccessory] WITH (UPDLOCK, HOLDLOCK) WHERE [IdProductSalesPlanOffer] = {match.OfferId} AND [IdAccessory] = {accessoryId})
                UPDATE [Store].[ProductSalesPlanOfferAccessory] SET [PriceAmount] = {input.PriceAmount}, [IsAllowed] = {input.IsAllowed}
                WHERE [IdProductSalesPlanOffer] = {match.OfferId} AND [IdAccessory] = {accessoryId};
            ELSE INSERT [Store].[ProductSalesPlanOfferAccessory] ([IdProductSalesPlanOffer], [IdAccessory], [PriceAmount], [IsIncludedByDefault], [IsAllowed])
                VALUES ({match.OfferId}, {accessoryId}, {input.PriceAmount}, {match.IsRequired}, {input.IsAllowed});
            """, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "متعلقهٔ پیشنهاد ذخیره شد." });
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
        return Ok(await db.Database.SqlQuery<OfferSpecification>($"SELECT [SpecificationKey] AS [Key], [Value] FROM [Store].[ProductSalesPlanOfferSpecificationOverride] WHERE [IdProductSalesPlanOffer] = {offer.OfferId} ORDER BY [Id]").ToListAsync(cancellationToken));
    }

    [HttpPut("product-offers/{productId:long}/{salesPlanId}/specifications")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveOfferSpecifications(long productId, string salesPlanId, [FromBody] List<OfferSpecification> input, CancellationToken cancellationToken)
    {
        if (!CanManage()) return Forbid();
        if (productId < 1 || string.IsNullOrWhiteSpace(salesPlanId) || salesPlanId.Length > 128 || input is null || input.Count > 50 || input.Any(x => x is null)) return BadRequest(new { title = "مشخصات معتبر نیستند." });
        var specs = input.Select(x => new OfferSpecification { Key = x.Key?.Trim() ?? "", Value = x.Value?.Trim() ?? "" }).ToList();
        if (specs.Any(x => x.Key.Length is < 1 or > 80 || x.Value.Length is < 1 or > 240 || x.Key.Contains('|') || x.Key.Any(char.IsControl) || x.Value.Any(char.IsControl)) ||
            specs.Select(x => x.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() != specs.Count)
            return BadRequest(new { title = "عنوان‌ها باید یکتا و حداکثر ۸۰ نویسه و مقادیر حداکثر ۲۴۰ نویسه باشند." });
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var offer = await FindOffer(productId, salesPlanId, cancellationToken);
        if (offer is null) return NotFound(new { title = "پیشنهاد کالا و طرح یافت نشد." });
        string json = System.Text.Json.JsonSerializer.Serialize(specs);
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM [Store].[ProductSalesPlanOfferSpecificationOverride] WHERE [IdProductSalesPlanOffer] = {offer.OfferId};
                INSERT [Store].[ProductSalesPlanOfferSpecificationOverride] ([IdProductSalesPlanOffer], [SpecificationKey], [Value])
                SELECT {offer.OfferId}, [Key], [Value] FROM OPENJSON({json}) WITH ([Key] nvarchar(80) '$.Key', [Value] nvarchar(240) '$.Value');
                """, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Microsoft.Data.SqlClient.SqlException error) when (error.Number is 2601 or 2627)
        { return Conflict(new { title = "عنوان مشخصات تکراری است." }); }
        return Ok(new { message = "مشخصات پیشنهاد ذخیره شد." });
    }

    private Task<OfferAccessoryMatch?> FindOffer(long productId, string salesPlanId, CancellationToken cancellationToken) =>
        db.Database.SqlQuery<OfferAccessoryMatch>($"""
            SELECT o.[Id] AS [OfferId], CONVERT(bit, 0) AS [IsRequired] FROM [Store].[ProductSalesPlanOffer] o
            JOIN [Warehouse].[Product] p ON p.[Id] = o.[IdProduct] AND p.[IsStoreEnabled] = 1 AND p.[IsCatalogActive] = 1
            JOIN [Warehouse].[SalesPlanReference] s ON s.[Id] = o.[IdSalesPlan] AND s.[SalesPlanKey] = {salesPlanId} AND s.[IsDefault] = 0 AND s.[IdStockOwnerType] = 1
            WHERE o.[IdProduct] = {productId}
            """).SingleOrDefaultAsync(cancellationToken);

    public sealed class OfferSpecification { public string Key { get; set; } = ""; public string Value { get; set; } = ""; }
}
