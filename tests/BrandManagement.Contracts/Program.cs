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
    controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([], "test"));
    if (await controller.UpdateProduct(productId, productInput, default) is not ForbidResult) throw new Exception("Unauthorized product update accepted.");
}
finally
{
    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Warehouse].[Product] WHERE [Id] = {productId} AND [ProductKey] = {productKey}");
}
Console.WriteLine("Brand create, update, validation, authorization and rollback OK.");
Console.WriteLine("Product update, validation and authorization OK; test product removed.");
controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "system.full-access")], "test"));
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
    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT [Store].[PaymentAttempt] ([Id], [IdOrder], [Amount], [Status], [GatewayReference], [ReceiptReference], [PaidAtUtc]) VALUES (NEWID(), {orderId}, 1000, N'verified', N'test', N'test', SYSUTCDATETIME())");
    var orders = new StoreOrdersAdminApiController(db) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "tracking-test")], "test")) } } };
    var tracking = new StoreOrdersAdminApiController.TrackingInput("پست", "12345", null, null);
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
}
finally
{
    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Store].[OrderShipmentTrackingHistory] WHERE [IdOrder] = {orderId}");
    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Store].[OrderShippingHistory] WHERE [IdOrder] = {orderId}");
    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Store].[PaymentAttempt] WHERE [IdOrder] = {orderId}");
    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Store].[Order] WHERE [Id] = {orderId} AND [OrderNumber] = {orderNumber}");
}
Console.WriteLine("Shipping tracking, history, stale updates and status transition OK; test data removed.");
