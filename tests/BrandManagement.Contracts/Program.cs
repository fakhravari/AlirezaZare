using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shop.Admin.Controllers.Api;
using Shop.Database.Infrastructure.Persistence;
using Shop.Database.Infrastructure.Persistence.Repositories;

string connectionString = Environment.GetEnvironmentVariable("SHOP_TEST_CONNECTION") ?? throw new InvalidOperationException("SHOP_TEST_CONNECTION is required.");
if (!new SqlConnectionStringBuilder(connectionString).InitialCatalog.StartsWith("ShopVerify_", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("A ShopVerify_ test database is required.");
var options = new DbContextOptionsBuilder<ShopDbContext>().UseSqlServer(connectionString).Options;
await using var db = new ShopDbContext(options);
await using var transaction = await db.Database.BeginTransactionAsync();
var controller = new StoreCatalogAdminApiController(new StoreRuntimeDatasetRepository(db), db)
{
    ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "system.full-access")], "test")) } }
};
string key = "test-brand-" + Guid.NewGuid().ToString("N");
var input = new StoreCatalogAdminApiController.BrandInput(key, "برند آزمایشی", "Test Brand", "");
if (await controller.CreateBrand(input, default) is not OkObjectResult) throw new Exception("Brand creation failed.");
if (await controller.UpdateBrand(key, input with { NameEn = "Updated" }, default) is not OkObjectResult) throw new Exception("Brand update failed.");
var name = await db.Database.SqlQuery<string>($"SELECT [NameEn] AS [Value] FROM [Store].[Brand] WHERE [BrandKey] = {key}").SingleAsync();
if (name != "Updated") throw new Exception("Update was not persisted in transaction.");
if (await controller.CreateBrand(input with { Key = "bad key" }, default) is not BadRequestObjectResult) throw new Exception("Invalid key accepted.");
if (await controller.CreateBrand(input with { CountryCode = "invalid" }, default) is not BadRequestObjectResult) throw new Exception("Invalid country accepted.");
controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([], "test"));
if (await controller.UpdateBrand(key, input, default) is not ForbidResult) throw new Exception("Unauthorized update accepted.");
await transaction.RollbackAsync();
await transaction.DisposeAsync();
controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "system.full-access")], "test"));
var sample = await db.Products.AsNoTracking().Where(p => p.IsStoreEnabled && p.IsCatalogActive && p.IdBrand != null && p.IdCategory != null && p.PriceAmount != null)
    .Select(p => new { p.IdBaseUnit, Brand = p.IdBrand!.Value, Category = p.IdCategory!.Value }).FirstAsync();
long productId = await db.Database.SqlQueryRaw<long>("SELECT ISNULL(MAX([Id]), 0) + 1000 AS [Value] FROM [Warehouse].[Product]").SingleAsync();
string productKey = "test-product-" + Guid.NewGuid().ToString("N");
try
{
    await db.Database.ExecuteSqlInterpolatedAsync($"""
        INSERT [Warehouse].[Product] ([Id], [IdBaseUnit], [ProductKey], [Code], [Name], [EnglishName], [IdBrand], [IdCategory], [PriceAmount], [DiscountPercent], [MaxPurchaseQuantity], [Slug], [IsStoreEnabled], [IsCatalogActive], [IsWarehouseEnabled])
        VALUES ({productId}, {sample.IdBaseUnit}, {productKey}, {productKey}, N'Test product', N'Test product', {sample.Brand}, {sample.Category}, 1000, 0, 3, {productKey}, 1, 1, 1)
        """);
    var productInput = new StoreCatalogAdminApiController.ProductEditorInput("نام آزمایشی", "Test product", sample.Brand, sample.Category, 21000, 10, 4);
    if (await controller.UpdateProduct(productId, productInput, default) is not OkObjectResult) throw new Exception("Product update failed.");
    var amount = await db.Database.SqlQuery<decimal>($"SELECT [PriceAmount] AS [Value] FROM [Warehouse].[Product] WHERE [Id] = {productId}").SingleAsync();
    if (amount != 21000) throw new Exception("Product price not saved.");
    if (await controller.UpdateProduct(productId, productInput with { DiscountPercent = 100 }, default) is not BadRequestObjectResult) throw new Exception("Invalid discount accepted.");
    string specialPlan = await db.Database.SqlQueryRaw<string>("SELECT TOP (1) [SalesPlanKey] AS [Value] FROM [Warehouse].[SalesPlanReference] WHERE [IsDefault] = 0 AND [IdStockOwnerType] = 1 ORDER BY [Id]").FirstAsync();
    long accessoryId = await db.Database.SqlQueryRaw<long>("SELECT TOP (1) a.[Id] AS [Value] FROM [Store].[Accessory] a WHERE a.[IsActive] = 1 ORDER BY a.[Id]").FirstAsync();
    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT [Store].[ProductAccessory] ([IdProduct], [IdAccessory], [IsRequired], [SortOrder], [PriceAmount]) VALUES ({productId}, {accessoryId}, 1, 1, 100)");
    var promotions = new StoreVariantPricesAdminApiController(db) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "system.full-access")], "test")) } } };
    var offer = new StoreVariantPricesAdminApiController.ProductOfferInput(10000, 20, 4, true);
    if (await promotions.SaveProductOffer(productId, specialPlan, offer, default) is not OkObjectResult) throw new Exception("Product offer creation failed.");
    int required = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [Store].[ProductSalesPlanOffer] o JOIN [Store].[ProductSalesPlanOfferAccessory] a ON a.[IdProductSalesPlanOffer] = o.[Id] WHERE o.[IdProduct] = {productId} AND a.[IdAccessory] = {accessoryId} AND a.[IsAllowed] = 1 AND a.[PriceAmount] = 100").SingleAsync();
    if (required != 1) throw new Exception("Required accessory was not copied into offer.");
    if (await promotions.OfferAccessories(productId, specialPlan, default) is not OkObjectResult) throw new Exception("Offer accessories lookup failed.");
    if (await promotions.SaveOfferAccessory(productId, specialPlan, accessoryId, new(200, false), default) is not BadRequestObjectResult) throw new Exception("Required accessory was disabled.");
    if (await promotions.SaveOfferAccessory(productId, specialPlan, accessoryId, new(200, true), default) is not OkObjectResult) throw new Exception("Offer accessory price update failed.");
    long optionalId = await db.Database.SqlQuery<long>($"SELECT TOP (1) [Id] AS [Value] FROM [Store].[Accessory] WHERE [IsActive] = 1 AND [Id] <> {accessoryId} ORDER BY [Id]").FirstAsync();
    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT [Store].[ProductAccessory] ([IdProduct], [IdAccessory], [IsRequired], [SortOrder], [PriceAmount]) VALUES ({productId}, {optionalId}, 0, 2, 500)");
    if (await promotions.SaveOfferAccessory(productId, specialPlan, optionalId, new(300, false), default) is not OkObjectResult) throw new Exception("Optional accessory configuration failed.");
    int accessorySaved = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [Store].[ProductSalesPlanOffer] o JOIN [Store].[ProductSalesPlanOfferAccessory] a ON a.[IdProductSalesPlanOffer] = o.[Id] WHERE o.[IdProduct] = {productId} AND ((a.[IdAccessory] = {accessoryId} AND a.[IsAllowed] = 1 AND a.[PriceAmount] = 200) OR (a.[IdAccessory] = {optionalId} AND a.[IsAllowed] = 0 AND a.[PriceAmount] = 300))").SingleAsync();
    if (accessorySaved != 2) throw new Exception("Offer accessory values were not persisted.");
    var spec = new StoreVariantPricesAdminApiController.OfferSpecification { Key = "توان", Value = "۲۰ وات" };
    if (await promotions.SaveOfferSpecifications(productId, specialPlan, [spec], default) is not OkObjectResult) throw new Exception("Offer specification save failed.");
    if (await promotions.OfferSpecifications(productId, specialPlan, default) is not OkObjectResult) throw new Exception("Offer specification read failed.");
    if (await promotions.SaveOfferSpecifications(productId, specialPlan, [spec, spec], default) is not BadRequestObjectResult) throw new Exception("Duplicate specification accepted.");
    int specCount = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [Store].[ProductSalesPlanOfferSpecificationOverride] x JOIN [Store].[ProductSalesPlanOffer] o ON o.[Id] = x.[IdProductSalesPlanOffer] WHERE o.[IdProduct] = {productId} AND x.[SpecificationKey] = N'توان' AND x.[Value] = N'۲۰ وات'").SingleAsync();
    if (specCount != 1) throw new Exception("Offer specification not preserved after invalid save.");
    if (await promotions.SaveProductOffer(productId, specialPlan, offer with { PriceAmount = 15000, IsEnabled = false }, default) is not OkObjectResult) throw new Exception("Product offer update failed.");
    int saved = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [Store].[ProductSalesPlanOffer] WHERE [IdProduct] = {productId} AND [PriceAmount] = 15000 AND [IsEnabled] = 0").SingleAsync();
    if (saved != 1) throw new Exception("Product offer update was not persisted.");
    if (await promotions.SaveProductOffer(productId, specialPlan, offer with { DiscountPercent = 100 }, default) is not ObjectResult invalid || invalid.StatusCode != 400) throw new Exception("Invalid offer discount accepted.");
    promotions.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([], "test"));
    if (await promotions.SaveOfferAccessory(productId, specialPlan, accessoryId, new(200, true), default) is not ForbidResult) throw new Exception("Unauthorized accessory update accepted.");
    if (await promotions.SaveProductOffer(productId, specialPlan, offer, default) is not ForbidResult) throw new Exception("Unauthorized offer accepted.");
    controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([], "test"));
    if (await controller.UpdateProduct(productId, productInput, default) is not ForbidResult) throw new Exception("Unauthorized product update accepted.");
}
finally
{
    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE a FROM [Store].[ProductSalesPlanOfferAccessory] a JOIN [Store].[ProductSalesPlanOffer] o ON o.[Id] = a.[IdProductSalesPlanOffer] WHERE o.[IdProduct] = {productId}");
    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE a FROM [Store].[ProductSalesPlanOfferSpecificationOverride] a JOIN [Store].[ProductSalesPlanOffer] o ON o.[Id] = a.[IdProductSalesPlanOffer] WHERE o.[IdProduct] = {productId}");
    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Store].[ProductSalesPlanOffer] WHERE [IdProduct] = {productId}");
    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Store].[ProductAccessory] WHERE [IdProduct] = {productId}");
    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Warehouse].[Product] WHERE [Id] = {productId} AND [ProductKey] = {productKey}");
}
Console.WriteLine("Brand create, update, validation, authorization and rollback OK.");
Console.WriteLine("Product update, validation and authorization OK; test product removed.");
Console.WriteLine("Product offer create, update, required accessory, validation and authorization OK; test data removed.");
controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "system.full-access")], "test"));
string categoryKey = "test-category-" + Guid.NewGuid().ToString("N"), childKey = "test-child-" + Guid.NewGuid().ToString("N");
long categoryId = 0, childId = 0;
try
{
    var categoryInput = new StoreCatalogAdminApiController.CategoryInput(categoryKey, "گروه آزمایشی", "Test category", null);
    if (await controller.CreateCategory(categoryInput, default) is not OkObjectResult) throw new Exception("Category creation failed.");
    categoryId = await db.Database.SqlQuery<long>($"SELECT [Id] AS [Value] FROM [Store].[Category] WHERE [CategoryKey] = {categoryKey}").SingleAsync();
    if (await controller.CreateCategory(categoryInput, default) is not ConflictObjectResult) throw new Exception("Duplicate category key accepted.");
    var childInput = new StoreCatalogAdminApiController.CategoryInput(childKey, "زیرگروه آزمایشی", "Test child", categoryId);
    if (await controller.CreateCategory(childInput, default) is not OkObjectResult) throw new Exception("Child category creation failed.");
    childId = await db.Database.SqlQuery<long>($"SELECT [Id] AS [Value] FROM [Store].[Category] WHERE [CategoryKey] = {childKey}").SingleAsync();
    if (await controller.UpdateCategory(categoryKey, categoryInput with { NameFa = "گروه اصلاح‌شده" }, default) is not OkObjectResult) throw new Exception("Category rename failed.");
    string childPath = await db.Database.SqlQuery<string>($"SELECT [PathJson] AS [Value] FROM [Store].[Category] WHERE [Id] = {childId}").SingleAsync();
    var categoryPath = System.Text.Json.JsonSerializer.Deserialize<string[]>(childPath);
    if (categoryPath is null || categoryPath.Length != 2 || categoryPath[0] != "گروه اصلاح‌شده" || categoryPath[1] != "زیرگروه آزمایشی") throw new Exception("Category descendant path was not updated.");
    if (await controller.UpdateCategory(categoryKey, categoryInput with { ParentId = childId }, default) is not BadRequestObjectResult) throw new Exception("Category reparenting accepted.");
    if (await controller.CreateCategory(childInput with { Key = "another-" + childKey, ParentId = sample.Category }, default) is not ConflictObjectResult) throw new Exception("Child under published product category accepted.");
    controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([], "test"));
    if (await controller.CreateCategory(categoryInput, default) is not ForbidResult) throw new Exception("Unauthorized category creation accepted.");
}
finally
{
    if (childId != 0) await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Store].[Category] WHERE [Id] = {childId} AND [CategoryKey] = {childKey}");
    if (categoryId != 0) await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Store].[Category] WHERE [Id] = {categoryId} AND [CategoryKey] = {categoryKey}");
}
controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "system.full-access")], "test"));
Console.WriteLine("Category creation, descendant rename, validation and authorization OK; test categories removed.");
string draftKey = "test-draft-" + Guid.NewGuid().ToString("N");
var draft = new StoreCatalogAdminApiController.ProductCreateInput(draftKey, draftKey, "کالای آزمایشی", "Test draft", sample.Brand, sample.Category, sample.IdBaseUnit, 1000, 0, 2);
long draftId = 0;
try
{
    if (await controller.CreateProduct(draft, default) is not OkObjectResult) throw new Exception("Draft creation failed.");
    draftId = await db.Database.SqlQuery<long>($"SELECT [Id] AS [Value] FROM [Warehouse].[Product] WHERE [ProductKey] = {draftKey}").SingleAsync();
    if (await controller.CreateProduct(draft, default) is not ConflictObjectResult) throw new Exception("Duplicate draft accepted.");
    if (await controller.PublishProduct(draftId, default) is not ConflictObjectResult) throw new Exception("Draft published without image.");
    string mediaKey = "product-" + draftKey;
    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT [Store].[ProductMedia] ([IdProduct], [MediaKey], [MainPath], [CardPath], [ThumbnailPath], [ImagesJson], [VideosJson]) VALUES ({draftId}, {mediaKey}, N'content/products/images/main/test.webp', N'content/products/images/card/test.webp', N'content/products/images/thumbnail/test.webp', N'[]', N'[]')");
    if (await controller.PublishProduct(draftId, default) is not OkObjectResult) throw new Exception("Draft publication failed.");
    int publishedDraft = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [Warehouse].[Product] WHERE [Id] = {draftId} AND [IsCatalogActive] = 1").SingleAsync();
    if (publishedDraft != 1) throw new Exception("Draft publication not persisted.");
    controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
    if (await controller.UnpublishProduct(draftId, default) is not ForbidResult) throw new Exception("Unauthorized unpublish accepted.");
    controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "system.full-access")], "test"));
    if (await controller.UnpublishProduct(draftId, default) is not OkObjectResult) throw new Exception("Product unpublish failed.");
    if (await controller.UnpublishProduct(draftId, default) is not ConflictObjectResult) throw new Exception("Unpublished product accepted again.");
    if (await controller.PublishProduct(draftId, default) is not OkObjectResult) throw new Exception("Product republication failed.");
}
finally
{
    if (draftId != 0) {
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Store].[ProductMedia] WHERE [IdProduct] = {draftId}");
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Warehouse].[Product] WHERE [Id] = {draftId} AND [ProductKey] = {draftKey}");
    }
}
Console.WriteLine("Product draft creation, image requirement and publication OK; test data removed.");
await using var faqTransaction = await db.Database.BeginTransactionAsync();
var faq = new StoreFaqAdminApiController(db);
string question = "test-faq-" + Guid.NewGuid().ToString("N");
var faqInput = new StoreFaqAdminApiController.FaqInput(question, "answer", "", "", 123, false);
if (await faq.Create(faqInput, default) is not OkObjectResult) throw new Exception("FAQ creation failed.");
long faqId = await db.Database.SqlQuery<long>($"SELECT [Id] AS [Value] FROM [Store].[FaqItem] WHERE [Question] = {question}").SingleAsync();
int published = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [Store].[FaqItem] WHERE [Question] = {question} AND [IsPublished] = 1").SingleAsync();
if (published != 0) throw new Exception("Unpublished FAQ is visible.");
if (await faq.Update(faqId, faqInput with { IsPublished = true }, default) is not OkObjectResult) throw new Exception("FAQ update failed.");
published = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [Store].[FaqItem] WHERE [Question] = {question} AND [IsPublished] = 1").SingleAsync();
if (published != 1) throw new Exception("Published FAQ is absent.");
if (await faq.Update(faqId, faqInput with { LinkText = "missing URL" }, default) is not BadRequestObjectResult) throw new Exception("Invalid FAQ link accepted.");
await faqTransaction.RollbackAsync();
Console.WriteLine("FAQ create, publication and validation OK; changes rolled back.");
await faqTransaction.DisposeAsync();
long profileId = await db.Database.SqlQueryRaw<long>("SELECT TOP (1) [IdProfile] AS [Value] FROM [Account].[Address] ORDER BY [Id]").SingleAsync();
var customers = new StoreCustomersAdminApiController(db);
var customerResult = await customers.Details(profileId, default) as OkObjectResult;
if (customerResult?.Value is not StoreCustomersAdminApiController.CustomerDetails customer ||
    System.Text.Json.JsonDocument.Parse(customer.AddressesJson).RootElement.GetArrayLength() == 0)
    throw new Exception("Customer details did not return the existing address.");
if (await customers.Details(0, default) is not NotFoundResult) throw new Exception("Invalid customer id accepted.");
await using var customerTransaction = await db.Database.BeginTransactionAsync();
var update = new StoreCustomersAdminApiController.CustomerUpdate("نام آزمایشی", "نام خانوادگی", "123", customer.FirstName, customer.LastName, customer.Phone);
if (await customers.Update(profileId, update, default) is not OkObjectResult) throw new Exception("Customer update failed.");
if (await customers.Update(profileId, update, default) is not ConflictObjectResult) throw new Exception("Stale customer update accepted.");
var updated = (await customers.Details(profileId, default) as OkObjectResult)?.Value as StoreCustomersAdminApiController.CustomerDetails;
if (updated?.FirstName != "نام آزمایشی" || updated.Phone != "123" || updated.Email != customer.Email) throw new Exception("Customer update was not persisted correctly.");
if (await customers.Update(profileId, update with { FirstName = " " }, default) is not BadRequestObjectResult) throw new Exception("Invalid customer name accepted.");
await customerTransaction.RollbackAsync();
Console.WriteLine("Customer details and normalized address lookup OK.");
Console.WriteLine("Customer update, validation and stale update protection OK; changes rolled back.");
await customerTransaction.DisposeAsync();
long documentId = await db.Database.SqlQueryRaw<long>("SELECT TOP (1) [Id] AS [Value] FROM [Warehouse].[WarehouseDocument] ORDER BY [Id]").SingleAsync();
string orderNumber = "TEST-TRACK-" + Guid.NewGuid().ToString("N")[..20];
long orderId = (await db.Database.SqlQuery<long>($"INSERT [Store].[Order] ([IdProfile], [IdWarehouseDocument], [OrderNumber], [Status], [PaymentMethod], [ItemCount], [TotalAmount], [AddressSnapshotJson]) OUTPUT INSERTED.[Id] AS [Value] VALUES ({profileId}, {documentId}, {orderNumber}, N'warehouse-confirmed', N'test', 1, 1000, N'{{}}')").ToListAsync()).Single();
try
{
    var orders = new StoreOrdersAdminApiController(db) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "tracking-test")], "test")) } } };
    var tracking = new StoreOrdersAdminApiController.TrackingInput("پست", "12345", null, null);
    if (await orders.UpdateTracking(orderId, tracking, default) is not ConflictObjectResult) throw new Exception("Tracking without verified payment accepted.");
    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT [Store].[PaymentAttempt] ([Id], [IdOrder], [Amount], [Status], [GatewayReference], [ReceiptReference], [PaidAtUtc]) VALUES (NEWID(), {orderId}, 1000, N'verified', N'test', N'test', SYSUTCDATETIME())");
    if (await orders.UpdateTracking(orderId, tracking, default) is not OkObjectResult) throw new Exception("Tracking update failed.");
    if (await orders.UpdateTracking(orderId, tracking, default) is not ConflictObjectResult) throw new Exception("Stale tracking update accepted.");
    if (await orders.UpdateTracking(orderId, tracking with { Carrier = " " }, default) is not BadRequestObjectResult) throw new Exception("Invalid carrier accepted.");
    if (await orders.UpdateTracking(orderId, new("پست", "67890", "پست", "12345"), default) is not OkObjectResult) throw new Exception("Tracking correction failed.");
    var detail = (await orders.Details(orderId, default) as OkObjectResult)?.Value as StoreOrdersAdminApiController.OrderDetailsRow;
    if (detail?.ShippingCarrier != "پست" || detail.ShippingTrackingCode != "67890") throw new Exception("Tracking details were not persisted.");
    int history = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [Store].[OrderShipmentTrackingHistory] WHERE [IdOrder] = {orderId} AND [ActorUserKey] = N'tracking-test'").SingleAsync();
    if (history != 2) throw new Exception("Tracking history is incomplete.");
    if (await orders.UpdateShippingStatus(orderId, new("warehouse-confirmed", "in-transit"), default) is not OkObjectResult) throw new Exception("Shipping status update failed.");
    if (await orders.UpdateShippingStatus(orderId, new("warehouse-confirmed", "in-transit"), default) is not ConflictObjectResult) throw new Exception("Stale shipping status accepted.");
    await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Store].[Order] SET [PaymentMethod] = N'demo', [ExternalOrderKey] = N'demo-order-test' WHERE [Id] = {orderId}");
    if (await orders.UpdateTracking(orderId, new("پست", "999", "پست", "67890"), default) is not ConflictObjectResult) throw new Exception("Tracking for sample order accepted.");
}
finally
{
    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Store].[OrderShipmentTrackingHistory] WHERE [IdOrder] = {orderId}");
    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Store].[OrderShippingHistory] WHERE [IdOrder] = {orderId}");
    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Store].[PaymentAttempt] WHERE [IdOrder] = {orderId}");
    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Store].[Order] WHERE [Id] = {orderId} AND [OrderNumber] = {orderNumber}");
}
Console.WriteLine("Shipping tracking, history, stale updates and status transition OK; test data removed.");
