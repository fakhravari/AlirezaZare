using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Store.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;

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
        var brands = await db.StoreBrands.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.NameFa)
            .Select(x => new EditorOption { Id = x.Id, Name = x.NameFa }).ToListAsync(cancellationToken);
        var categories = await GetLeafCategoryOptions(cancellationToken);
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
        bool validCategory = await db.StoreCategories.AnyAsync(c => c.Id == input.CategoryId && c.IsActive &&
            !db.StoreCategories.Any(child => child.IsActive && child.IdParentCategory == c.Id), cancellationToken);
        bool validBrand = await db.StoreBrands.AnyAsync(x => x.Id == input.BrandId && x.IsActive, cancellationToken);
        if (!validCategory || !validBrand) return BadRequest(new { title = "برند یا دستهٔ برگ معتبر نیست." });
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id && x.IsStoreEnabled, cancellationToken);
        if (product is null) return NotFound();
        product.Name = name;
        product.EnglishName = englishName;
        product.IdBrand = input.BrandId;
        product.IdCategory = input.CategoryId;
        product.PriceAmount = input.PriceAmount;
        product.DiscountPercent = input.DiscountPercent;
        product.MaxPurchaseQuantity = input.MaxPurchaseQuantity;
        product.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "اطلاعات کالا ذخیره شد." });
    }

    public sealed class EditorOption { public long Id { get; set; } public string Name { get; set; } = ""; }
    public sealed record ProductEditorInput(string? Name, string? EnglishName, long BrandId, long CategoryId, decimal PriceAmount, decimal DiscountPercent, int MaxPurchaseQuantity);

    private async Task<List<EditorOption>> GetLeafCategoryOptions(CancellationToken cancellationToken)
    {
        var nodes = await db.StoreCategories.AsNoTracking().Where(x => x.IsActive)
            .Select(x => new { x.Id, x.NameFa, x.IdParentCategory }).ToListAsync(cancellationToken);
        var byId = nodes.ToDictionary(x => x.Id);
        var parentIds = nodes.Where(x => x.IdParentCategory.HasValue).Select(x => x.IdParentCategory!.Value).ToHashSet();

        return nodes.Where(x => !parentIds.Contains(x.Id)).Select(node =>
        {
            var segments = new List<string>();
            var visited = new HashSet<long>();
            var currentId = node.Id;
            while (visited.Add(currentId) && byId.TryGetValue(currentId, out var current))
            {
                segments.Add(current.NameFa);
                if (current.IdParentCategory is not long parentId) break;
                currentId = parentId;
            }
            segments.Reverse();
            return new EditorOption { Id = node.Id, Name = string.Join(" / ", segments) };
        }).OrderBy(x => x.Name, StringComparer.CurrentCulture).ToList();
    }

    [HttpGet("products/drafts")]
    public async Task<IActionResult> ProductDrafts(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        var drafts = from product in db.Products.AsNoTracking()
                     where product.IsStoreEnabled && !product.IsCatalogActive
                     join brand in db.StoreBrands.AsNoTracking() on product.IdBrand equals brand.Id into brands
                     from brand in brands.DefaultIfEmpty()
                     join category in db.StoreCategories.AsNoTracking() on product.IdCategory equals category.Id into categories
                     from category in categories.DefaultIfEmpty()
                     select new DraftProduct
                     {
                         Id = product.Id,
                         Code = product.Code,
                         Name = product.Name,
                         BrandName = brand == null ? null : brand.NameFa,
                         CategoryName = category == null ? null : category.NameFa,
                         PriceAmount = product.PriceAmount,
                         HasImage = db.StoreProductMedia.Any(media => media.IdProduct == product.Id && media.MediaKey == "product-" + product.ProductKey)
                     };
        return Ok(await drafts.OrderByDescending(x => x.Id).ToListAsync(cancellationToken));
    }

    [HttpGet("products/create-options")]
    public async Task<IActionResult> ProductCreateOptions(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        var brands = await db.StoreBrands.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.NameFa)
            .Select(x => new EditorOption { Id = x.Id, Name = x.NameFa }).ToListAsync(cancellationToken);
        var categories = await GetLeafCategoryOptions(cancellationToken);
        var units = await db.WarehouseUnits.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new EditorOption { Id = x.Id, Name = x.Name }).ToListAsync(cancellationToken);
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
        long id = await db.Products.Select(x => (long?)x.Id).MaxAsync(cancellationToken) is long maxId ? maxId + 1 : 1;
        if (id > int.MaxValue) return Conflict(new { title = "ظرفیت شناسهٔ کالا تکمیل شده است." });
        bool validCategory = await db.StoreCategories.AnyAsync(c => c.Id == input.CategoryId && c.IsActive &&
            !db.StoreCategories.Any(child => child.IsActive && child.IdParentCategory == c.Id), cancellationToken);
        bool validBrand = await db.StoreBrands.AnyAsync(x => x.Id == input.BrandId && x.IsActive, cancellationToken);
        bool validUnit = await db.WarehouseUnits.AnyAsync(x => x.Id == input.UnitId && x.IsActive, cancellationToken);
        bool duplicate = await db.Products.AnyAsync(x => x.Code == code || x.ProductKey == key || x.Slug == key, cancellationToken);
        if (!validCategory || !validBrand || !validUnit || duplicate)
            return Conflict(new { title = "برند، دسته یا واحد معتبر نیست یا شناسه و کد قبلاً ثبت شده‌اند." });
        db.Products.Add(new Shop.Data.Domain.Warehouse.Entities.WarehouseProduct
        {
            Id = id,
            IdBaseUnit = input.UnitId,
            ProductKey = key,
            Code = code,
            Name = name,
            EnglishName = english,
            IdBrand = input.BrandId,
            IdCategory = input.CategoryId,
            PriceAmount = input.PriceAmount,
            DiscountPercent = input.DiscountPercent,
            MaxPurchaseQuantity = input.MaxPurchaseQuantity,
            Slug = key,
            IsStoreEnabled = true,
            IsCatalogActive = false,
            IsWarehouseEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { id, message = "پیش‌نویس کالا ساخته شد؛ تصویر را بارگذاری و سپس کالا را منتشر کنید." });
    }

    [HttpPost("products/manage/{id:long}/publish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishProduct(long id, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id && x.IsStoreEnabled && !x.IsCatalogActive, cancellationToken);
        if (product is null) return Conflict(new { title = "پیش‌نویس پیدا نشد یا اطلاعات و تصویر آن کامل نیست." });
        bool validCategory = product.IdCategory is long categoryId && await db.StoreCategories.AnyAsync(c => c.Id == categoryId && c.IsActive &&
            !db.StoreCategories.Any(child => child.IsActive && child.IdParentCategory == c.Id), cancellationToken);
        bool validBrand = product.IdBrand is long brandId && await db.StoreBrands.AnyAsync(x => x.Id == brandId && x.IsActive, cancellationToken);
        var media = await db.StoreProductMedia.AsNoTracking().SingleOrDefaultAsync(x => x.IdProduct == product.Id && x.MediaKey == "product-" + product.ProductKey, cancellationToken);
        bool validMedia = media is not null &&
            media.MainPath?.StartsWith("content/products/images/main/", StringComparison.OrdinalIgnoreCase) == true &&
            media.CardPath?.StartsWith("content/products/images/card/", StringComparison.OrdinalIgnoreCase) == true &&
            media.ThumbnailPath?.StartsWith("content/products/images/thumbnail/", StringComparison.OrdinalIgnoreCase) == true &&
            IsJson(media.ImagesJson) && IsJson(media.VideosJson);
        bool validProduct = product.PriceAmount >= 0 && product.MaxPurchaseQuantity is >= 1 and <= 99 &&
            product.DiscountPercent is >= 0 and < 100 && !string.IsNullOrWhiteSpace(product.Slug) &&
            !string.IsNullOrWhiteSpace(product.EnglishName);
        if (!validCategory || !validBrand || !validMedia || !validProduct)
            return Conflict(new { title = "پیش‌نویس پیدا نشد یا اطلاعات و تصویر آن کامل نیست." });
        product.IsCatalogActive = true;
        product.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "کالا منتشر شد." });
    }

    [HttpPost("products/manage/{id:long}/unpublish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnpublishProduct(long id, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id && x.IsStoreEnabled && x.IsCatalogActive, cancellationToken);
        if (product is null) return Conflict(new { title = "کالای منتشرشده پیدا نشد." });
        product.IsCatalogActive = false;
        product.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "کالا از انتشار خارج شد و در پیش‌نویس‌ها قرار گرفت." });
    }

    private static bool IsJson(string? value)
    {
        if (value is null) return true;
        try { using var _ = JsonDocument.Parse(value); return true; }
        catch (JsonException) { return false; }
    }

    public sealed record ProductCreateInput(string? Key, string? Code, string? Name, string? EnglishName, long BrandId, long CategoryId, long UnitId, decimal PriceAmount, decimal DiscountPercent, int MaxPurchaseQuantity);
    public sealed class DraftProduct { public long Id { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string? BrandName { get; set; } public string? CategoryName { get; set; } public decimal? PriceAmount { get; set; } public bool HasImage { get; set; } }

    [HttpGet("brands/countries")]
    public async Task<IActionResult> BrandCountries(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        return Ok(await db.GeoCountries.AsNoTracking().OrderBy(x => x.NameFa)
            .Select(x => new CountryRow { CountryCode = x.CountryCode, NameFa = x.NameFa }).ToListAsync(cancellationToken));
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
            if (!await db.GeoCountries.AnyAsync(x => x.CountryCode == countryCode, cancellationToken))
                return BadRequest(new { title = "کشور انتخاب‌شده معتبر نیست." });
        }
        long? countryId = countryCode.Length == 0 ? null : await db.GeoCountries
            .Where(x => x.CountryCode == countryCode).Select(x => (long?)x.Id).SingleOrDefaultAsync(cancellationToken);
        try
        {
            var brand = key is null ? null : await db.StoreBrands.SingleOrDefaultAsync(x => x.BrandKey == brandKey, cancellationToken);
            if (key is not null && brand is null) return NotFound();
            if (brand is null)
            {
                brand = new Shop.Data.Domain.Store.Entities.StoreBrand { BrandKey = brandKey, IsActive = true };
                db.StoreBrands.Add(brand);
            }
            brand.NameFa = nameFa;
            brand.NameEn = nameEn;
            brand.IdCountry = countryId;
            await db.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "برند ذخیره شد." });
        }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Conflict(new { title = "این شناسهٔ برند قبلاً ثبت شده است." });
        }
    }

    public sealed record BrandInput(string? Key, string? NameFa, string? NameEn, string? CountryCode);
    public sealed class CountryRow { public string CountryCode { get; set; } = ""; public string NameFa { get; set; } = ""; }

    [HttpGet("categories/manage/options")]
    public async Task<IActionResult> CategoryOptions(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        return Ok(await db.StoreCategories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.NameFa)
            .Select(x => new CategoryOption(x.Id, x.CategoryKey, x.NameFa, x.NameEn, x.IdParentCategory)).ToListAsync(cancellationToken));
    }

    [HttpPost("categories/manage")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateCategory([FromBody] CategoryInput input, CancellationToken cancellationToken) => SaveCategory(null, input, cancellationToken);

    [HttpPut("categories/manage/{key}")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> UpdateCategory(string key, [FromBody] CategoryInput input, CancellationToken cancellationToken) => SaveCategory(key, input, cancellationToken);

    private async Task<IActionResult> SaveCategory(string? key, CategoryInput input, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        string categoryKey = (key ?? input.Key ?? "").Trim(), nameFa = input.NameFa?.Trim() ?? "", nameEn = input.NameEn?.Trim() ?? "";
        if (categoryKey.Length is < 1 or > 150 || !System.Text.RegularExpressions.Regex.IsMatch(categoryKey, "^[a-z0-9]+(?:-[a-z0-9]+)*$") ||
            nameFa.Length is < 1 or > 200 || nameEn.Length is < 1 or > 200 || !nameEn.Any(c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z') ||
            nameFa.Any(char.IsControl) || nameEn.Any(char.IsControl) || input.ParentId is <= 0)
            return BadRequest(new { title = "شناسه یا نام دسته معتبر نیست." });
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var nodes = await db.StoreCategories.ToListAsync(cancellationToken);
        var current = key is null ? null : nodes.SingleOrDefault(x => x.CategoryKey == categoryKey && x.IsActive);
        if (key is not null && current is null) return NotFound();
        if (current is null && nodes.Any(x => string.Equals(x.CategoryKey, categoryKey, StringComparison.OrdinalIgnoreCase))) return Conflict(new { title = "شناسهٔ دسته قبلاً ثبت شده است." });
        long? parentId = current?.IdParentCategory ?? input.ParentId;
        if (current is not null && input.ParentId != current.IdParentCategory) return BadRequest(new { title = "تغییر والد دسته از این فرم مجاز نیست." });
        var parent = parentId is null ? null : nodes.SingleOrDefault(x => x.Id == parentId && x.IsActive);
        if (parentId is not null && parent is null) return BadRequest(new { title = "دستهٔ والد فعال نیست یا وجود ندارد." });
        if (nodes.Any(x => x.IsActive && x.Id != current?.Id && x.IdParentCategory == parentId && string.Equals(x.NameFa, nameFa, StringComparison.Ordinal)))
            return Conflict(new { title = "دسته‌ای با این نام در همین سطح وجود دارد." });
        if (current is null && parent is not null)
        {
            bool hasPublishedProducts = await db.Products.AnyAsync(x => x.IdCategory == parent.Id && x.IsStoreEnabled && x.IsCatalogActive, cancellationToken);
            if (hasPublishedProducts) return Conflict(new { title = "برای دستهٔ دارای کالای منتشرشده نمی‌توان زیردسته ساخت؛ ابتدا دستهٔ کالاها را تغییر دهید." });
        }
        try
        {
            if (current is null)
            {
                var parentPath = parent is null ? new List<string>() : JsonSerializer.Deserialize<List<string>>(parent.PathJson) ?? throw new JsonException();
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
                if (oldPath.Count == 0 || oldPath[^1] != current.NameFa) return Conflict(new { title = "مسیر دسته ناسازگار است؛ ابتدا دادهٔ دسته اصلاح شود." });
                var descendants = new List<Shop.Data.Domain.Store.Entities.StoreCategory> { current };
                for (int i = 0; i < descendants.Count; i++)
                {
                    foreach (var child in nodes.Where(x => x.IdParentCategory == descendants[i].Id))
                    {
                        if (descendants.Any(x => x.Id == child.Id)) return Conflict(new { title = "ساختار دسته‌ها دارای چرخه است." });
                        descendants.Add(child);
                    }
                }
                foreach (var node in descendants)
                {
                    var path = JsonSerializer.Deserialize<List<string>>(node.PathJson) ?? throw new JsonException();
                    if (path.Count < oldPath.Count || !path.Take(oldPath.Count).SequenceEqual(oldPath)) return Conflict(new { title = "مسیر یکی از زیردسته‌ها ناسازگار است." });
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
        catch (JsonException) { return Conflict(new { title = "مسیر دسته ناسازگار است." }); }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 }) { return Conflict(new { title = "شناسهٔ دسته قبلاً ثبت شده است." }); }
        return Ok(new { message = "دسته ذخیره شد." });
    }

    public sealed record CategoryInput(string? Key, string? NameFa, string? NameEn, long? ParentId);
    public sealed record CategoryOption(long Id, string CategoryKey, string NameFa, string NameEn, long? IdParentCategory);

    [HttpGet("attributes/manage/{key}")]
    public async Task<IActionResult> AttributeEditor(string key, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        string attributeKey = key.Trim();
        if (attributeKey.Length is < 1 or > 100) return NotFound();
        var item = await db.StoreAttributeDefinitions.AsNoTracking()
            .Where(x => x.AttributeKey == attributeKey)
            .Select(x => new AttributeDefinitionRow(x.AttributeKey, x.Label, x.Icon, x.SourceKey,
                x.FilterKey, x.IsFilterable, x.IsHighlight, x.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("attributes/manage")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAttribute([FromBody] AttributeDefinitionInput input, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        if (!TryNormalizeAttribute(input, out var normalized, out var error)) return BadRequest(new { title = error });
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
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException dbError) when (dbError.InnerException is SqlException { Number: 2601 or 2627 })
        { return Conflict(new { title = "این کلید ویژگی قبلاً ثبت شده است." }); }
        return Ok(new { message = "ویژگی ایجاد شد." });
    }

    [HttpPut("attributes/manage/{key}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAttribute(string key, [FromBody] AttributeDefinitionInput input, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        string attributeKey = key.Trim();
        if (attributeKey.Length is < 1 or > 100) return NotFound();
        if (!TryNormalizeAttribute(input, out var normalized, out var error)) return BadRequest(new { title = error });

        var item = await db.StoreAttributeDefinitions.SingleOrDefaultAsync(x => x.AttributeKey == attributeKey, cancellationToken);
        if (item is null) return NotFound();
        item.Label = normalized.Label;
        item.Icon = normalized.Icon;
        item.SourceKey = normalized.SourceKey;
        item.FilterKey = normalized.FilterKey;
        item.IsFilterable = normalized.IsFilterable;
        item.IsHighlight = normalized.IsHighlight;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "ویژگی ذخیره شد." });
    }

    private static bool TryNormalizeAttribute(AttributeDefinitionInput input, out NormalizedAttribute value, out string error)
    {
        string key = input.Key?.Trim() ?? "";
        string label = input.Label?.Trim() ?? "";
        string icon = input.Icon?.Trim() ?? "";
        string source = input.SourceKey?.Trim() ?? "";
        string filter = input.FilterKey?.Trim() ?? "";
        if (key.Length is < 1 or > 100 || !System.Text.RegularExpressions.Regex.IsMatch(key, "^[A-Za-z][A-Za-z0-9]*(?:-[A-Za-z0-9]+)*$") ||
            label.Length is < 1 or > 200 || label.Any(char.IsControl) ||
            icon.Length > 100 || (icon.Length > 0 && !System.Text.RegularExpressions.Regex.IsMatch(icon, "^[a-z0-9-]+$")) ||
            source.Length > 100 || (source.Length > 0 && !System.Text.RegularExpressions.Regex.IsMatch(source, "^[A-Za-z][A-Za-z0-9_:-]*$")) ||
            filter.Length > 100 || (filter.Length > 0 && !System.Text.RegularExpressions.Regex.IsMatch(filter, "^[A-Za-z][A-Za-z0-9_-]*$")) ||
            input.IsFilterable && filter.Length == 0)
        {
            value = new NormalizedAttribute("", "", null, null, null, false, false);
            error = "کلید، عنوان، آیکن یا منبع ویژگی معتبر نیست؛ ویژگی قابل‌فیلتر باید کلید فیلتر داشته باشد.";
            return false;
        }

        value = new NormalizedAttribute(key, label, EmptyToNull(icon), EmptyToNull(source), EmptyToNull(filter), input.IsFilterable, input.IsHighlight);
        error = "";
        return true;
    }

    private static string? EmptyToNull(string? value) => string.IsNullOrEmpty(value) ? null : value;
    private sealed record NormalizedAttribute(string Key, string Label, string? Icon, string? SourceKey, string? FilterKey, bool IsFilterable, bool IsHighlight);
    public sealed record AttributeDefinitionInput(string? Key, string? Label, string? Icon, string? SourceKey, string? FilterKey, bool IsFilterable, bool IsHighlight);
    public sealed record AttributeDefinitionRow(string Key, string Label, string? Icon, string? SourceKey, string? FilterKey, bool IsFilterable, bool IsHighlight, bool IsActive);

    [HttpPost("attributes/manage/{key}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAttributeStatus(string key, [FromBody] CatalogStatusInput input, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        var item = await db.StoreAttributeDefinitions.SingleOrDefaultAsync(x => x.AttributeKey == key, cancellationToken);
        if (item is null) return NotFound();
        item.IsActive = input.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = input.IsActive ? "ویژگی منتشر شد." : "ویژگی از کاتالوگ خارج شد." });
    }

    [HttpPost("categories/manage/{key}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCategoryStatus(string key, [FromBody] CatalogStatusInput input, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        var category = await db.StoreCategories.SingleOrDefaultAsync(x => x.CategoryKey == key, cancellationToken);
        if (category is null) return NotFound();
        if (!input.IsActive)
        {
            bool hasChildren = await db.StoreCategories.AnyAsync(x => x.IsActive && x.IdParentCategory == category.Id, cancellationToken);
            bool hasPublishedProducts = await db.Products.AnyAsync(x => x.IsStoreEnabled && x.IsCatalogActive && x.IdCategory == category.Id, cancellationToken);
            if (hasChildren || hasPublishedProducts)
                return Conflict(new { title = "ابتدا زیردسته‌ها یا کالاهای منتشرشدهٔ این دسته را غیرفعال کنید." });
        }
        category.IsActive = input.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = input.IsActive ? "دسته منتشر شد." : "دسته از کاتالوگ خارج شد." });
    }

    [HttpPost("brands/manage/{key}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetBrandStatus(string key, [FromBody] CatalogStatusInput input, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("system.full-access")) return Forbid();
        var brand = await db.StoreBrands.SingleOrDefaultAsync(x => x.BrandKey == key, cancellationToken);
        if (brand is null) return NotFound();
        if (!input.IsActive && await db.Products.AnyAsync(x => x.IsStoreEnabled && x.IsCatalogActive && x.IdBrand == brand.Id, cancellationToken))
            return Conflict(new { title = "ابتدا کالاهای منتشرشدهٔ این برند را از انتشار خارج کنید." });
        brand.IsActive = input.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = input.IsActive ? "برند منتشر شد." : "برند از کاتالوگ خارج شد." });
    }

    public sealed record CatalogStatusInput(bool IsActive);

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

        if (string.Equals(section, "attributes", StringComparison.OrdinalIgnoreCase))
        {
            var attributes = db.StoreAttributeDefinitions.AsNoTracking();
            string attributeQuery = q?.Trim() ?? "";
            if (attributeQuery.Length > 0)
                attributes = attributes.Where(x => x.AttributeKey.Contains(attributeQuery) || x.Label.Contains(attributeQuery) ||
                    (x.SourceKey != null && x.SourceKey.Contains(attributeQuery)) || (x.FilterKey != null && x.FilterKey.Contains(attributeQuery)));
            int total = await attributes.CountAsync(cancellationToken);
            var items = await attributes.OrderBy(x => x.Id)
                .Skip((int)Math.Min((long)(page - 1) * pageSize, int.MaxValue)).Take(pageSize)
                .Select(x => new AttributeDefinitionRow(x.AttributeKey, x.Label, x.Icon, x.SourceKey, x.FilterKey, x.IsFilterable, x.IsHighlight, x.IsActive))
                .ToListAsync(cancellationToken);
            return Ok(new { total, page, pageSize, items });
        }

        if (string.Equals(section, "categories", StringComparison.OrdinalIgnoreCase))
        {
            var source = db.StoreCategories.AsNoTracking();
            string categoryQuery = q?.Trim() ?? "";
            if (categoryQuery.Length > 0)
                source = source.Where(x => x.CategoryKey.Contains(categoryQuery) || x.NameFa.Contains(categoryQuery) || x.NameEn.Contains(categoryQuery));
            var all = await source.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(cancellationToken);
            var parents = await db.StoreCategories.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.CategoryKey, cancellationToken);
            var rows = all.Select(x => new CategoryRow(x.CategoryKey, x.NameFa, x.NameEn, x.CategoryKey,
                x.IdParentCategory is long parentId && parents.TryGetValue(parentId, out string? parentKey) ? parentKey : "", x.IsActive));
            return Ok(Page(rows, page, pageSize, all.Count));
        }

        if (string.Equals(section, "brands", StringComparison.OrdinalIgnoreCase))
        {
            var brands = db.StoreBrands.AsNoTracking();
            string brandQuery = q?.Trim() ?? "";
            if (brandQuery.Length > 0)
                brands = brands.Where(x => x.BrandKey.Contains(brandQuery) || x.NameFa.Contains(brandQuery) || x.NameEn.Contains(brandQuery));
            int total = await brands.CountAsync(cancellationToken);
            var rows = await (from brand in brands
                              join country in db.GeoCountries.AsNoTracking() on brand.IdCountry equals (long?)country.Id into countries
                              from country in countries.DefaultIfEmpty()
                              orderby brand.NameFa, brand.BrandKey
                              select new BrandRow(brand.BrandKey, brand.NameFa, brand.NameEn,
                                  country == null ? "" : country.CountryCode, brand.IsActive))
                .Skip((int)Math.Min((long)(page - 1) * pageSize, int.MaxValue))
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            return Ok(new { total, page, pageSize, items = rows });
        }

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

    private static object Page<T>(IEnumerable<T> rows, int page, int pageSize, int? knownTotal = null)
    {
        T[] items = rows.ToArray();
        return new
        {
            total = knownTotal ?? items.Length,
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

    private sealed record CategoryRow(string Id, string Name, string EnglishName, string Slug, string ParentId, bool IsActive);

    private sealed record BrandRow(string Id, string Name, string EnglishName, string CountryCode, bool IsActive);
}
