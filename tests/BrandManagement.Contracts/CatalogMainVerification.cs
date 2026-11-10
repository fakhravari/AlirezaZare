using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shop.Admin.Controllers.Api;
using Shop.Data.Application.Store;
using Shop.Data.Infrastructure.Persistence;
using Shop.Data.Infrastructure.Persistence.Repositories;

internal static class CatalogMainVerification
{
    public static async Task Run(string connectionString)
    {
        var connection = new SqlConnectionStringBuilder(connectionString);
        if (!string.Equals(connection.InitialCatalog, "bsshopir_fakhravari", StringComparison.OrdinalIgnoreCase) ||
            !connection.DataSource.Contains("31.25.91.7\\sqlserver2022", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Main catalog verification is restricted to the configured primary SQL database.");

        var options = new DbContextOptionsBuilder<ShopDbContext>().UseSqlServer(connectionString).Options;
        await using var db = new ShopDbContext(options);
        if (!await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'049-catalog-publication'").AnyAsync())
            throw new InvalidOperationException("Catalog publication migration 049 is not applied.");

        var mediaCdn = new StoreMediaCdnBaseUrlProvider(db,
            Options.Create(new MediaCdnOptions { BaseUrl = "https://cdn.bs-shop.ir/" }));
        var controller = new StoreCatalogAdminApiController(new StoreRuntimeDatasetRepository(db, mediaCdn), db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = FullAccessUser()
                }
            }
        };

        string token = Guid.NewGuid().ToString("N");
        string brandKey = "verify-brand-" + token;
        string categoryKey = "verify-category-" + token;
        string childKey = "verify-child-" + token;
        string attributeKey = "VerifyAttribute-" + token;
        string productKey = "verify-product-" + token;
        long categoryId = 0, childId = 0, productId = 0;

        try
        {
            await VerifyBrand(controller, db, brandKey);
            (categoryId, childId) = await VerifyCategories(controller, db, categoryKey, childKey);
            await VerifyAttribute(controller, db, attributeKey);
            productId = await VerifyProduct(controller, db, productKey);
            Console.WriteLine("Primary catalog CRUD, publication, validation and authorization checks passed.");
        }
        finally
        {
            db.ChangeTracker.Clear();
            var cleanupErrors = new List<Exception>();
            await Cleanup(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Store].[ProductMedia] WHERE [IdProduct] = {productId}"), cleanupErrors, productId != 0);
            await Cleanup(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Warehouse].[Product] WHERE [Id] = {productId} AND [ProductKey] = {productKey}"), cleanupErrors, productId != 0);
            await Cleanup(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Store].[AttributeDefinition] WHERE [AttributeKey] = {attributeKey}"), cleanupErrors, true);
            await Cleanup(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Store].[Category] WHERE [Id] = {childId} AND [CategoryKey] = {childKey}"), cleanupErrors, childId != 0);
            await Cleanup(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Store].[Category] WHERE [Id] = {categoryId} AND [CategoryKey] = {categoryKey}"), cleanupErrors, categoryId != 0);
            await Cleanup(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Store].[Brand] WHERE [BrandKey] = {brandKey}"), cleanupErrors, true);
            if (cleanupErrors.Count > 0) throw new AggregateException("Primary catalog test data cleanup failed.", cleanupErrors);
            Console.WriteLine("Temporary primary catalog records removed.");
        }
    }

    private static async Task VerifyBrand(StoreCatalogAdminApiController controller, ShopDbContext db, string key)
    {
        var input = new StoreCatalogAdminApiController.BrandInput(key, "برند کنترل کاتالوگ", "Catalog verification", "");
        EnsureOk(await controller.CreateBrand(input, default), "Create brand");
        EnsureOk(await controller.UpdateBrand(key, input with { NameEn = "Catalog verification edited" }, default), "Edit brand");
        EnsureOk(await controller.SetBrandStatus(key, new(false), default), "Unpublish brand");
        bool active = await db.Database.SqlQuery<bool>($"SELECT [IsActive] AS [Value] FROM [Store].[Brand] WHERE [BrandKey] = {key}").SingleAsync();
        if (active) throw new InvalidOperationException("Brand unpublish was not persisted.");
        EnsureOk(await controller.SetBrandStatus(key, new(true), default), "Republish brand");
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([], "catalog-main-verification"));
        if (await controller.UpdateBrand(key, input, default) is not ForbidResult) throw new InvalidOperationException("Unauthorized brand edit was accepted.");
        controller.ControllerContext.HttpContext.User = FullAccessUser();
    }

    private static async Task<(long Parent, long Child)> VerifyCategories(
        StoreCatalogAdminApiController controller, ShopDbContext db, string key, string childKey)
    {
        EnsureOk(await controller.CreateCategory(new(key, "دستهٔ کنترل کاتالوگ", "Catalog verification", null), default), "Create category");
        long parentId = await db.Database.SqlQuery<long>($"SELECT [Id] AS [Value] FROM [Store].[Category] WHERE [CategoryKey] = {key}").SingleAsync();
        EnsureOk(await controller.CreateCategory(new(childKey, "زیردستهٔ کنترل کاتالوگ", "Catalog child", parentId), default), "Create child category");
        long childId = await db.Database.SqlQuery<long>($"SELECT [Id] AS [Value] FROM [Store].[Category] WHERE [CategoryKey] = {childKey}").SingleAsync();
        EnsureOk(await controller.UpdateCategory(key, new(key, "دستهٔ ویرایش‌شده", "Catalog edited", null), default), "Edit category");
        if (await controller.SetCategoryStatus(key, new(false), default) is not ConflictObjectResult)
            throw new InvalidOperationException("Category with an active child was unpublished.");
        EnsureOk(await controller.SetCategoryStatus(childKey, new(false), default), "Unpublish child category");
        EnsureOk(await controller.SetCategoryStatus(key, new(false), default), "Unpublish category");
        EnsureOk(await controller.SetCategoryStatus(key, new(true), default), "Republish category");
        EnsureOk(await controller.SetCategoryStatus(childKey, new(true), default), "Republish child category");
        return (parentId, childId);
    }

    private static async Task VerifyAttribute(StoreCatalogAdminApiController controller, ShopDbContext db, string key)
    {
        var input = new StoreCatalogAdminApiController.AttributeDefinitionInput(key, "ویژگی کنترل کاتالوگ", "check-circle", "catalog_test", "catalog-test", true, false);
        EnsureOk(await controller.CreateAttribute(input, default), "Create attribute");
        EnsureOk(await controller.UpdateAttribute(key, input with { Key = "Changed-Key", Label = "ویژگی ویرایش‌شده", IsHighlight = true }, default), "Edit attribute");
        var item = await db.StoreAttributeDefinitions.AsNoTracking().SingleAsync(x => x.AttributeKey == key);
        if (item.Label != "ویژگی ویرایش‌شده" || !item.IsHighlight || item.AttributeKey != key)
            throw new InvalidOperationException("Attribute edit changed its stable key or did not persist.");
        EnsureOk(await controller.SetAttributeStatus(key, new(false), default), "Unpublish attribute");
        if (await db.StoreAttributeDefinitions.AsNoTracking().Where(x => x.AttributeKey == key).Select(x => x.IsActive).SingleAsync())
            throw new InvalidOperationException("Attribute unpublish was not persisted.");
        EnsureOk(await controller.SetAttributeStatus(key, new(true), default), "Republish attribute");
        if (await controller.CreateAttribute(input with { Key = "invalid key" }, default) is not BadRequestObjectResult)
            throw new InvalidOperationException("Invalid attribute key was accepted.");
        if (await controller.CreateAttribute(input with { Key = "FilterWithoutKey", FilterKey = " " }, default) is not BadRequestObjectResult)
            throw new InvalidOperationException("Filterable attribute without a filter key was accepted.");
    }

    private static async Task<long> VerifyProduct(StoreCatalogAdminApiController controller, ShopDbContext db, string key)
    {
        var sample = await db.Products.AsNoTracking()
            .Where(x => x.IsStoreEnabled && x.IsCatalogActive && x.IdBrand != null && x.IdCategory != null && x.PriceAmount != null &&
                        db.StoreCategories.Any(c => c.Id == x.IdCategory && c.IsActive &&
                            !db.StoreCategories.Any(child => child.IsActive && child.IdParentCategory == c.Id)))
            .Select(x => new { x.IdBaseUnit, Brand = x.IdBrand!.Value, Category = x.IdCategory!.Value })
            .FirstAsync();
        var media = await (from item in db.StoreProductMedia.AsNoTracking()
                           join product in db.Products.AsNoTracking() on item.IdProduct equals product.Id
                           where product.IsCatalogActive && item.MainPath != null && item.CardPath != null && item.ThumbnailPath != null
                           select new { item.MainPath, item.CardPath, item.ThumbnailPath, item.ImagesJson, item.VideosJson }).FirstAsync();

        var input = new StoreCatalogAdminApiController.ProductCreateInput(key, key, "کالای کنترل کاتالوگ", "Catalog verification product",
            sample.Brand, sample.Category, sample.IdBaseUnit, 1000, 0, 2);
        EnsureOk(await controller.CreateProduct(input, default), "Create product draft");
        long id = await db.Database.SqlQuery<long>($"SELECT [Id] AS [Value] FROM [Warehouse].[Product] WHERE [ProductKey] = {key}").SingleAsync();
        EnsureOk(await controller.UpdateProduct(id, new("کالای ویرایش‌شده", "Catalog verification product", sample.Brand, sample.Category, 2000, 5, 3), default), "Edit product");
        if (await controller.PublishProduct(id, default) is not ConflictObjectResult)
            throw new InvalidOperationException("Product without registered media was published.");

        string mediaKey = "product-" + key;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT [Store].[ProductMedia] ([IdProduct], [MediaKey], [MainPath], [CardPath], [ThumbnailPath], [ImagesJson], [VideosJson])
            VALUES ({id}, {mediaKey}, {media.MainPath}, {media.CardPath}, {media.ThumbnailPath}, {media.ImagesJson}, {media.VideosJson})
            """);
        EnsureOk(await controller.PublishProduct(id, default), "Publish product");
        EnsureOk(await controller.UnpublishProduct(id, default), "Unpublish product");
        EnsureOk(await controller.PublishProduct(id, default), "Republish product");
        bool published = await db.Products.AsNoTracking().Where(x => x.Id == id).Select(x => x.IsCatalogActive).SingleAsync();
        if (!published) throw new InvalidOperationException("Product republication was not persisted.");
        return id;
    }

    private static ClaimsPrincipal FullAccessUser() =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Role, "system.full-access")], "catalog-main-verification"));

    private static void EnsureOk(IActionResult result, string action)
    {
        if (result is not OkObjectResult) throw new InvalidOperationException($"{action} failed ({result.GetType().Name}).");
    }

    private static async Task Cleanup(Func<Task<int>> cleanup, List<Exception> errors, bool needed)
    {
        if (!needed) return;
        try { await cleanup(); }
        catch (Exception error) { errors.Add(error); }
    }
}
