using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shop.Admin.Controllers.Api;
using Shop.Database.Infrastructure.Persistence;
using Shop.Database.Infrastructure.Persistence.Repositories;

string connectionString = Environment.GetEnvironmentVariable("SHOP_TEST_CONNECTION")
    ?? throw new InvalidOperationException("SHOP_TEST_CONNECTION is required.");
var connection = new SqlConnectionStringBuilder(connectionString);
if (!connection.InitialCatalog.StartsWith("ShopVerify_", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Reports tests require a ShopVerify_ database.");
var options = new DbContextOptionsBuilder<ShopDbContext>().UseSqlServer(connectionString).Options;
await using var db = new ShopDbContext(options);
var controller = new WarehouseMovementsApiController(db)
{
    ControllerContext = new ControllerContext
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "system.full-access")], "test"))
        }
    }
};
var response = await controller.Get(null, null, new DateOnly(2020, 1, 1), null, summary: true);
var result = response as OkObjectResult ?? throw new InvalidOperationException("Summary did not return OK.");
var json = JsonSerializer.SerializeToElement(result.Value);
var items = json.GetProperty("items");
if (items.GetArrayLength() == 0) throw new InvalidOperationException("Expected test inventory movements.");
foreach (var item in items.EnumerateArray())
{
    decimal opening = item.GetProperty("Opening").GetDecimal();
    decimal incoming = item.GetProperty("Incoming").GetDecimal();
    decimal outgoing = item.GetProperty("Outgoing").GetDecimal();
    decimal closing = item.GetProperty("Closing").GetDecimal();
    if (opening + incoming - outgoing != closing) throw new InvalidOperationException("Summary balance is incorrect.");
}
var periodStart = new DateOnly(2026, 11, 9);
var periodEnd = new DateOnly(2026, 11, 9);
var periodResponse = await controller.Get(null, null, periodStart, periodEnd, summary: true) as OkObjectResult
    ?? throw new InvalidOperationException("Period summary failed.");
var periodItems = JsonSerializer.SerializeToElement(periodResponse.Value).GetProperty("items");
if (periodItems.GetArrayLength() == 0) throw new InvalidOperationException("Expected period summary data.");
var periodItem = periodItems[0];
long warehouseId = periodItem.GetProperty("WarehouseId").GetInt64();
long itemId = periodItem.GetProperty("ItemId").GetInt64();
long planId = periodItem.GetProperty("PlanId").GetInt64();
var movements = await (from movement in db.WarehouseStockMovements.AsNoTracking()
    join document in db.WarehouseDocuments.AsNoTracking() on movement.IdDocument equals document.Id
    where movement.IdWarehouse == warehouseId && movement.IdInventoryItem == itemId
        && movement.IdSalesPlanReference == planId && document.DocumentDate <= periodEnd
    select new { document.DocumentDate, movement.QuantityDelta }).ToListAsync();
decimal expectedOpening = movements.Where(x => x.DocumentDate < periodStart).Sum(x => x.QuantityDelta);
decimal expectedIncoming = movements.Where(x => x.DocumentDate >= periodStart && x.QuantityDelta > 0).Sum(x => x.QuantityDelta);
decimal expectedOutgoing = -movements.Where(x => x.DocumentDate >= periodStart && x.QuantityDelta < 0).Sum(x => x.QuantityDelta);
if (periodItem.GetProperty("Opening").GetDecimal() != expectedOpening
    || periodItem.GetProperty("Incoming").GetDecimal() != expectedIncoming
    || periodItem.GetProperty("Outgoing").GetDecimal() != expectedOutgoing
    || periodItem.GetProperty("Closing").GetDecimal() != expectedOpening + expectedIncoming - expectedOutgoing)
    throw new InvalidOperationException("Period report does not match recorded movements.");
var movementResponse = await controller.Get(null, null, null, null);
if (movementResponse is not OkObjectResult) throw new InvalidOperationException("Movement report failed.");
var forbidden = await controller.Get(0, null, null, null);
if (forbidden is not ObjectResult { Value: ProblemDetails })
    throw new InvalidOperationException($"Invalid warehouse produced {forbidden.GetType().Name}.");
var stock = await new WarehouseReadRepository(db).GetStockAsync(null, null, null, false, 1, 50, CancellationToken.None);
if (stock.Count == 0) throw new InvalidOperationException("Expected test stock.");
await using (var transaction = await db.Database.BeginTransactionAsync())
{
    await db.Database.ExecuteSqlRawAsync("""
        DECLARE @Document bigint, @Type bigint=(SELECT TOP (1) Id FROM Warehouse.DocumentType WHERE Direction='R' AND AffectsStock=1),
            @Party bigint=(SELECT TOP (1) Id FROM Warehouse.Party);
        INSERT Warehouse.WarehouseDocument (IdWarehouse,IdDocumentType,IdParty,IdSalesPlanReference,DocumentDate,Status,Reason,CreatedByUserKey)
            VALUES (1,@Type,@Party,1,'2099-12-31','F',N'REPORT-CONTRACT-TEST',N'test');
        SET @Document=SCOPE_IDENTITY();
        INSERT Warehouse.WarehouseDocumentLine (IdDocument,IdInventoryItem,IdUnit,IdSalesPlanReference,Quantity,ConversionFactor,BaseQuantity,ProductNameSnapshot,UnitNameSnapshot)
            VALUES (@Document,1,1,1,1,1,1,N'REPORT-CONTRACT-TEST',N'test'),
                   (@Document,1,1,2,1,1,1,N'REPORT-CONTRACT-TEST',N'test');
        EXEC Warehouse.RebuildDocumentStock @IdDocument=@Document;
        """);
    var testedResponse = await controller.Get(1, "REPORT-CONTRACT-TEST", null, null);
    var testedResult = testedResponse as OkObjectResult ?? throw new InvalidOperationException("Filtered movements failed.");
    var lines = JsonSerializer.SerializeToElement(testedResult.Value).EnumerateArray().ToArray();
    if (lines.Length != 2) throw new InvalidOperationException("Expected two report lines.");
    foreach (var line in lines)
    {
        string key = line.GetProperty("SalesPlanKey").GetString()!;
        var balance = await (from item in db.StockBalances.AsNoTracking()
            join plan in db.SalesPlanReferences.AsNoTracking() on item.IdSalesPlanReference equals plan.Id
            where item.IdWarehouse == 1 && item.IdInventoryItem == 1 && plan.StoreSalesPlanKey == key
            select item.Quantity).SingleAsync();
        if (line.GetProperty("BalanceAfter").GetDecimal() != balance)
            throw new InvalidOperationException("Movement used the document header plan instead of the line plan.");
    }
    if (lines.Select(line => line.GetProperty("SalesPlanKey").GetString()).Distinct().Count() != 2)
        throw new InvalidOperationException("Report did not preserve both line plans.");
    await transaction.RollbackAsync();
}
controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
    [new Claim(ClaimTypes.NameIdentifier, "9223372036854775000")], "test"));
if (await controller.Get(1, null, null, null, summary: true) is not ForbidResult)
    throw new InvalidOperationException("Restricted user could access another warehouse.");
var restrictedResponse = await controller.Get(null, null, null, null, summary: true) as OkObjectResult
    ?? throw new InvalidOperationException("Restricted report failed.");
if (JsonSerializer.SerializeToElement(restrictedResponse.Value).GetProperty("items").GetArrayLength() != 0)
    throw new InvalidOperationException("Restricted report leaked warehouse records.");
Console.WriteLine($"Warehouse reports OK: {items.GetArrayLength()} summary rows, {stock.Count} stock rows.");
