using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Shop.Database.Application.Store.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/catalog")]
public sealed class StoreCatalogAdminApiController(IStoreRuntimeDatasetRepository datasets, ShopDbContext db) : ControllerBase
{
    [HttpGet("products/manage/{id:long}")]
    public async Task<IActionResult> ProductEditor(long id, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        var product = await db.Products.AsNoTracking().Where(x => x.Id == id && x.IsStoreEnabled)
            .Select(x => new { x.Id, x.Code, x.Name, x.EnglishName, x.IdBrand, x.IdCategory, x.PriceAmount, x.DiscountPercent, x.MaxPurchaseQuantity })
            .SingleOrDefaultAsync(cancellationToken);
        if (product is null) return NotFound();
        var brands = await db.Database.SqlQueryRaw<EditorOption>("SELECT [Id], [NameFa] AS [Name] FROM [Store].[Brand] ORDER BY [NameFa]").ToListAsync(cancellationToken);
        var categories = await db.Database.SqlQueryRaw<EditorOption>("""
            SELECT c.[Id], c.[NameFa] AS [Name] FROM [Store].[Category] c
            WHERE c.[IsActive] = 1 AND NOT EXISTS (SELECT 1 FROM [Store].[Category] child WHERE child.[IdParentCategory] = c.[Id] AND child.[IsActive] = 1)
            ORDER BY c.[NameFa]
            """).ToListAsync(cancellationToken);
        return Ok(new { product, brands, categories });
    }

    [HttpPut("products/manage/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProduct(long id, [FromBody] ProductEditorInput input, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        string name = input.Name?.Trim() ?? "", englishName = input.EnglishName?.Trim() ?? "";
        if (id < 1 || name.Length is < 1 or > 300 || englishName.Length is < 1 or > 300 || input.BrandId < 1 || input.CategoryId < 1 ||
            input.PriceAmount < 0 || input.PriceAmount > 9007199254740991m || input.PriceAmount != decimal.Truncate(input.PriceAmount) ||
            input.DiscountPercent is < 0 or >= 100 || decimal.Round(input.DiscountPercent, 2) != input.DiscountPercent || input.MaxPurchaseQuantity is < 1 or > 99)
            return BadRequest(new { title = "نام، دسته، برند، قیمت، تخفیف یا حداکثر خرید معتبر نیست." });
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        int valid = await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS [Value] FROM [Store].[Category] c
            WHERE c.[Id] = {input.CategoryId} AND c.[IsActive] = 1
              AND NOT EXISTS (SELECT 1 FROM [Store].[Category] child WHERE child.[IdParentCategory] = c.[Id] AND child.[IsActive] = 1)
              AND EXISTS (SELECT 1 FROM [Store].[Brand] b WHERE b.[Id] = {input.BrandId})
            """).SingleAsync(cancellationToken);
        if (valid != 1) return BadRequest(new { title = "برند یا دستهٔ برگ معتبر نیست." });
        int updated = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [Warehouse].[Product] SET [Name] = {name}, [EnglishName] = {englishName}, [IdBrand] = {input.BrandId},
                [IdCategory] = {input.CategoryId}, [PriceAmount] = {input.PriceAmount}, [DiscountPercent] = {input.DiscountPercent},
                [MaxPurchaseQuantity] = {input.MaxPurchaseQuantity}, [UpdatedAtUtc] = SYSUTCDATETIME()
            WHERE [Id] = {id} AND [IsStoreEnabled] = 1
            """, cancellationToken);
        if (updated == 0) return NotFound();
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "اطلاعات کالا ذخیره شد." });
    }

    public sealed class EditorOption { public long Id { get; set; } public string Name { get; set; } = ""; }
    public sealed record ProductEditorInput(string? Name, string? EnglishName, long BrandId, long CategoryId, decimal PriceAmount, decimal DiscountPercent, int MaxPurchaseQuantity);

    [HttpGet("products/drafts")]
    public async Task<IActionResult> ProductDrafts(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        return Ok(await db.Database.SqlQueryRaw<DraftProduct>("""
            SELECT p.[Id], p.[Code], p.[Name], b.[NameFa] AS [BrandName], c.[NameFa] AS [CategoryName],
                   p.[PriceAmount], CONVERT(bit, CASE WHEN m.[Id] IS NULL THEN 0 ELSE 1 END) AS [HasImage]
            FROM [Warehouse].[Product] p LEFT JOIN [Store].[Brand] b ON b.[Id] = p.[IdBrand]
            LEFT JOIN [Store].[Category] c ON c.[Id] = p.[IdCategory]
            LEFT JOIN [Store].[ProductMedia] m ON m.[IdProduct] = p.[Id] AND m.[MediaKey] = CONCAT(N'product-', p.[ProductKey])
            WHERE p.[IsStoreEnabled] = 1 AND p.[IsCatalogActive] = 0 ORDER BY p.[Id] DESC
            """).ToListAsync(cancellationToken));
    }

    [HttpGet("products/create-options")]
    public async Task<IActionResult> ProductCreateOptions(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        var brands = await db.Database.SqlQueryRaw<EditorOption>("SELECT [Id], [NameFa] AS [Name] FROM [Store].[Brand] ORDER BY [NameFa]").ToListAsync(cancellationToken);
        var categories = await db.Database.SqlQueryRaw<EditorOption>("SELECT c.[Id], c.[NameFa] AS [Name] FROM [Store].[Category] c WHERE c.[IsActive] = 1 AND NOT EXISTS (SELECT 1 FROM [Store].[Category] child WHERE child.[IdParentCategory] = c.[Id] AND child.[IsActive] = 1) ORDER BY c.[NameFa]").ToListAsync(cancellationToken);
        var units = await db.Database.SqlQueryRaw<EditorOption>("SELECT [Id], [Name] FROM [Warehouse].[Unit] WHERE [IsActive] = 1 ORDER BY [Name]").ToListAsync(cancellationToken);
        return Ok(new { brands, categories, units });
    }

    [HttpPost("products/manage")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct([FromBody] ProductCreateInput input, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        string key = input.Key?.Trim() ?? "", code = input.Code?.Trim() ?? "", name = input.Name?.Trim() ?? "", english = input.EnglishName?.Trim() ?? "";
        if (key.Length is < 1 or > 100 || !System.Text.RegularExpressions.Regex.IsMatch(key, "^[a-z0-9]+(?:-[a-z0-9]+)*$") || code.Length is < 1 or > 100 ||
            name.Length is < 1 or > 300 || english.Length is < 1 or > 300 || input.BrandId < 1 || input.CategoryId < 1 || input.UnitId < 1 ||
            input.PriceAmount < 0 || input.PriceAmount > 9007199254740991m || input.PriceAmount != decimal.Truncate(input.PriceAmount) ||
            input.DiscountPercent is < 0 or >= 100 || decimal.Round(input.DiscountPercent, 2) != input.DiscountPercent || input.MaxPurchaseQuantity is < 1 or > 99)
            return BadRequest(new { title = "اطلاعات کالا معتبر نیست." });
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        long id = await db.Database.SqlQueryRaw<long>("SELECT COALESCE(MAX([Id]), 0) + 1 AS [Value] FROM [Warehouse].[Product] WITH (UPDLOCK, HOLDLOCK)").SingleAsync(cancellationToken);
        if (id > int.MaxValue) return Conflict(new { title = "ظرفیت شناسهٔ کالا تکمیل شده است." });
        int valid = await db.Database.SqlQuery<int>($"""
            SELECT COUNT(*) AS [Value] FROM [Store].[Category] c WHERE c.[Id] = {input.CategoryId} AND c.[IsActive] = 1
            AND NOT EXISTS (SELECT 1 FROM [Store].[Category] child WHERE child.[IdParentCategory] = c.[Id] AND child.[IsActive] = 1)
            AND EXISTS (SELECT 1 FROM [Store].[Brand] WHERE [Id] = {input.BrandId})
            AND EXISTS (SELECT 1 FROM [Warehouse].[Unit] WHERE [Id] = {input.UnitId} AND [IsActive] = 1)
            AND NOT EXISTS (SELECT 1 FROM [Warehouse].[Product] WHERE [Code] = {code} OR [ProductKey] = {key} OR [Slug] = {key})
            """).SingleAsync(cancellationToken);
        if (valid != 1) return Conflict(new { title = "برند، دسته یا واحد معتبر نیست یا شناسه و کد قبلاً ثبت شده‌اند." });
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT [Warehouse].[Product] ([Id], [IdBaseUnit], [ProductKey], [Code], [Name], [EnglishName], [IdBrand], [IdCategory], [PriceAmount], [DiscountPercent], [MaxPurchaseQuantity], [Slug], [IsStoreEnabled], [IsCatalogActive], [IsWarehouseEnabled])
            VALUES ({id}, {input.UnitId}, {key}, {code}, {name}, {english}, {input.BrandId}, {input.CategoryId}, {input.PriceAmount}, {input.DiscountPercent}, {input.MaxPurchaseQuantity}, {key}, 1, 0, 1)
            """, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { id, message = "پیش‌نویس کالا ساخته شد؛ تصویر را بارگذاری و سپس کالا را منتشر کنید." });
    }

    [HttpPost("products/manage/{id:long}/publish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishProduct(long id, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        int changed = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE p SET [IsCatalogActive] = 1, [UpdatedAtUtc] = SYSUTCDATETIME()
            FROM [Warehouse].[Product] p JOIN [Store].[Brand] b ON b.[Id] = p.[IdBrand]
            JOIN [Store].[Category] c ON c.[Id] = p.[IdCategory] AND c.[IsActive] = 1
            JOIN [Store].[ProductMedia] m ON m.[IdProduct] = p.[Id] AND m.[MediaKey] = CONCAT(N'product-', p.[ProductKey])
            WHERE p.[Id] = {id} AND p.[IsStoreEnabled] = 1 AND p.[IsCatalogActive] = 0
            AND NOT EXISTS (SELECT 1 FROM [Store].[Category] child WHERE child.[IdParentCategory] = c.[Id] AND child.[IsActive] = 1)
            AND p.[PriceAmount] >= 0 AND p.[MaxPurchaseQuantity] BETWEEN 1 AND 99
            AND p.[DiscountPercent] >= 0 AND p.[DiscountPercent] < 100
            AND LEN(LTRIM(RTRIM(p.[Slug]))) > 0 AND LEN(LTRIM(RTRIM(p.[EnglishName]))) > 0
            AND m.[MainPath] LIKE N'content/products/images/main/%' AND m.[CardPath] LIKE N'content/products/images/card/%'
            AND m.[ThumbnailPath] LIKE N'content/products/images/thumbnail/%' AND ISJSON(m.[ImagesJson]) = 1 AND ISJSON(m.[VideosJson]) = 1
            """, cancellationToken);
        return changed == 1 ? Ok(new { message = "کالا منتشر شد." }) : Conflict(new { title = "پیش‌نویس پیدا نشد یا اطلاعات و تصویر آن کامل نیست." });
    }

    public sealed record ProductCreateInput(string? Key, string? Code, string? Name, string? EnglishName, long BrandId, long CategoryId, long UnitId, decimal PriceAmount, decimal DiscountPercent, int MaxPurchaseQuantity);
    public sealed class DraftProduct { public long Id { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string? BrandName { get; set; } public string? CategoryName { get; set; } public decimal? PriceAmount { get; set; } public bool HasImage { get; set; } }

    [HttpGet("brands/countries")]
    public async Task<IActionResult> BrandCountries(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        return Ok(await db.Database.SqlQueryRaw<CountryRow>("SELECT [CountryCode], [NameFa] FROM [Geo].[Country] ORDER BY [NameFa]").ToListAsync(cancellationToken));
    }

    [HttpPost("brands/manage")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateBrand([FromBody] BrandInput input, CancellationToken cancellationToken) => SaveBrand(null, input, cancellationToken);

    [HttpPut("brands/manage/{key}")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> UpdateBrand(string key, [FromBody] BrandInput input, CancellationToken cancellationToken) => SaveBrand(key, input, cancellationToken);

    private async Task<IActionResult> SaveBrand(string? key, BrandInput input, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        string brandKey = (key ?? input.Key ?? "").Trim();
        string nameFa = input.NameFa?.Trim() ?? "", nameEn = input.NameEn?.Trim() ?? "";
        string countryCode = input.CountryCode?.Trim() ?? "";
        if (brandKey.Length is < 1 or > 100 || !System.Text.RegularExpressions.Regex.IsMatch(brandKey, "^[a-z0-9]+(?:-[a-z0-9]+)*$") ||
            nameFa.Length is < 1 or > 200 || nameEn.Length is < 1 or > 200 || countryCode.Length > 10)
            return BadRequest(new { title = "شناسه، نام یا کشور برند معتبر نیست." });
        if (countryCode.Length > 0)
        {
            int countryCount = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [Geo].[Country] WHERE [CountryCode] = {countryCode}").SingleAsync(cancellationToken);
            if (countryCount == 0) return BadRequest(new { title = "کشور انتخاب‌شده معتبر نیست." });
        }
        try
        {
            int changed = key is null
                ? await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT [Store].[Brand] ([BrandKey], [NameFa], [NameEn], [IdCountry])
                    VALUES ({brandKey}, {nameFa}, {nameEn}, (SELECT [Id] FROM [Geo].[Country] WHERE [CountryCode] = {countryCode}))
                    """, cancellationToken)
                : await db.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE [Store].[Brand] SET [NameFa] = {nameFa}, [NameEn] = {nameEn},
                        [IdCountry] = (SELECT [Id] FROM [Geo].[Country] WHERE [CountryCode] = {countryCode})
                    WHERE [BrandKey] = {brandKey}
                    """, cancellationToken);
            return changed == 0 ? NotFound() : Ok(new { message = "برند ذخیره شد." });
        }
        catch (SqlException error) when (error.Number is 2601 or 2627)
        {
            return Conflict(new { title = "این شناسهٔ برند قبلاً ثبت شده است." });
        }
    }

    public sealed record BrandInput(string? Key, string? NameFa, string? NameEn, string? CountryCode);
    public sealed class CountryRow { public string CountryCode { get; set; } = ""; public string NameFa { get; set; } = ""; }

    [HttpGet("{section}")]
    public async Task<IActionResult> GetSection(
        string section,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 200)
            return ValidationProblem(detail: "شماره صفحه باید مثبت و اندازهٔ صفحه بین ۱ تا ۲۰۰ باشد.", statusCode: 400);

        string? json = await datasets.ReadCatalogJsonAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "دادهٔ کاتالوگ در دیتابیس در دسترس نیست.");

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        string query = q?.Trim() ?? "";

        return section.ToLowerInvariant() switch
        {
            "products" => Ok(Page(Products(root).Where(item => Matches(query,
                item.Code, item.Name, item.EnglishName, item.BrandName, item.CategoryName)), page, pageSize)),
            "categories" => Ok(Page(Categories(root).Where(item => Matches(query,
                item.Name, item.EnglishName, item.Slug)), page, pageSize)),
            "brands" => Ok(Page(Brands(root).Where(item => Matches(query,
                item.Name, item.EnglishName)), page, pageSize)),
            "attributes" => Ok(Page(Attributes(root).Where(item => Matches(query,
                item.Key, item.Name, item.Source)), page, pageSize)),
            _ => NotFound()
        };
    }

    private static IReadOnlyList<ProductRow> Products(JsonElement root)
    {
        Dictionary<string, string> brands = root.GetProperty("brands").EnumerateArray()
            .ToDictionary(item => Text(item, "id", ""), item => Text(item, "fa", Text(item, "name", "")));
        Dictionary<string, string> categories = root.GetProperty("categories").EnumerateArray()
            .ToDictionary(item => Text(item, "id", ""), item => Text(item, "fa", ""));

        return root.GetProperty("products").EnumerateArray().Select(item =>
        {
            string brandId = Text(item, "brandId", "");
            string categoryId = Text(item, "categoryId", "");
            return new ProductRow(
                item.GetProperty("id").GetInt32(),
                Text(item, "code", ""),
                Text(item, "name", ""),
                Text(item, "englishName", ""),
                brands.GetValueOrDefault(brandId, ""),
                categories.GetValueOrDefault(categoryId, ""),
                item.TryGetProperty("price", out JsonElement price) ? price.GetDecimal() : 0,
                item.TryGetProperty("stockQuantity", out JsonElement stock) ? stock.GetInt32() : 0,
                item.TryGetProperty("discountPercent", out JsonElement discount) ? discount.GetDecimal() : 0);
        }).ToArray();
    }

    private static IReadOnlyList<CategoryRow> Categories(JsonElement root) =>
        root.GetProperty("categories").EnumerateArray().Select(item => new CategoryRow(
            Text(item, "id", ""),
            Text(item, "fa", ""),
            Text(item, "en", ""),
            Text(item, "slug", ""),
            Text(item, "parentId", ""))).ToArray();

    private static IReadOnlyList<BrandRow> Brands(JsonElement root) =>
        root.GetProperty("brands").EnumerateArray().Select(item => new BrandRow(
            Text(item, "id", ""), Text(item, "fa", Text(item, "name", "")),
            Text(item, "name", ""), Text(item, "countryCode", ""))).ToArray();

    private static IReadOnlyList<AttributeRow> Attributes(JsonElement root) =>
        root.GetProperty("attributeDefinitions").EnumerateArray().Select(item => new AttributeRow(
            Text(item, "id", ""),
            Text(item, "label", ""),
            Text(item, "source", ""),
            item.TryGetProperty("is_filterable", out JsonElement filterable) && filterable.GetBoolean(),
            item.TryGetProperty("is_highlight", out JsonElement highlight) && highlight.GetBoolean())).ToArray();

    private static object Page<T>(IEnumerable<T> rows, int page, int pageSize)
    {
        T[] items = rows.ToArray();
        return new
        {
            total = items.Length,
            page,
            pageSize,
            items = items.Skip((int)Math.Min((long)(page - 1) * pageSize, items.Length)).Take(pageSize)
        };
    }

    private static bool Matches(string query, params string?[] values) =>
        query.Length == 0 || values.Any(value => value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);

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

    private sealed record CategoryRow(string Id, string Name, string EnglishName, string Slug, string ParentId);

    private sealed record BrandRow(string Id, string Name, string EnglishName, string CountryCode);

    private sealed record AttributeRow(string Key, string Name, string Source, bool IsFilterable, bool IsHighlight);
}
