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
bool historyOnly = args.Contains("--history-only", StringComparer.Ordinal);
bool mainRollback = args.Contains("--main-rollback", StringComparer.Ordinal);
bool concurrency = args.Contains("--concurrency", StringComparer.Ordinal);
bool unitLoop = args.Contains("--unit-loop", StringComparer.Ordinal);
if (!historyOnly && !mainRollback && !concurrency && !unitLoop && !connection.InitialCatalog.StartsWith("ShopVerify_", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Reports tests require a ShopVerify_ database.");
var options = new DbContextOptionsBuilder<ShopDbContext>().UseSqlServer(connectionString).Options;
await using var db = new ShopDbContext(options);
if (unitLoop)
{
    string sql = await File.ReadAllTextAsync(Path.Combine("tests", "warehouse-unit-stock.sql"));
    await using var sqlConnection = new SqlConnection(connectionString);
    await sqlConnection.OpenAsync();
    for (int iteration = 1; iteration <= 3; iteration++)
    {
        await using var command = sqlConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 120;
        await using (var unitResult = await command.ExecuteReaderAsync())
        {
            if (!await unitResult.ReadAsync()) throw new InvalidOperationException("Unit stock test returned no result.");
            while (await unitResult.NextResultAsync()) { }
        }
        await using var checkTransaction = sqlConnection.CreateCommand();
        checkTransaction.CommandText = "SELECT @@TRANCOUNT";
        if ((int)(await checkTransaction.ExecuteScalarAsync())! != 0)
            throw new InvalidOperationException("Unit stock test left a transaction open.");
        Console.WriteLine($"Unit conversion and stock rollback iteration {iteration}/3 OK.");
    }
    return;
}
if (concurrency)
{
    int productId = await db.Database.SqlQueryRaw<int>("SELECT TOP (1) CONVERT(int, [Id]) AS [Value] FROM [Warehouse].[Product] WHERE [IsCatalogActive]=1 AND [IsStoreEnabled]=1 ORDER BY [Id]").SingleAsync();
    long stockId = await db.Database.SqlQueryRaw<long>("SELECT TOP (1) b.[Id] AS [Value] FROM [Warehouse].[StockBalance] b JOIN [Store].[SiteSetting] s ON s.[IdWarehouse]=b.[IdWarehouse] JOIN [Warehouse].[InventoryItem] i ON i.[Id]=b.[IdInventoryItem] WHERE i.[IsActive]=1 ORDER BY b.[Id]").SingleAsync();
    await using var rival = new SqlConnection(connectionString);
    await rival.OpenAsync();
    for (int iteration = 1; iteration <= 5; iteration++)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var repository = new StoreSalesPlanRepository(db);
            await repository.ReadSalesPlanSnapshotAsync(CancellationToken.None);
            if (await repository.ReadProductPurchaseConfigurationAsync(productId, CancellationToken.None) is null)
                throw new InvalidOperationException("Active product purchase configuration was not found.");
            foreach (var (table, id) in new[] { ("Product", (long)productId), ("StockBalance", stockId) })
            {
                await using var command = rival.CreateCommand();
                command.CommandText = $"SET LOCK_TIMEOUT 2000; SELECT [Id] FROM [Warehouse].[{table}] WITH (XLOCK, ROWLOCK) WHERE [Id]=@id";
                command.Parameters.AddWithValue("@id", id);
                try
                {
                    await command.ExecuteScalarAsync();
                    throw new InvalidOperationException($"Concurrent exclusive {table} lock succeeded before checkout transaction ended.");
                }
                catch (SqlException ex) when (ex.Number == 1222) { }
            }
        }
        finally { await tx.RollbackAsync(); }
        await using var released = rival.CreateCommand();
        released.CommandText = "SET LOCK_TIMEOUT 5000; SELECT [Id] FROM [Warehouse].[Product] WITH (XLOCK, ROWLOCK) WHERE [Id]=@id";
        released.Parameters.AddWithValue("@id", productId);
        if (await released.ExecuteScalarAsync() is null)
            throw new InvalidOperationException("Product disappeared after the transaction ended.");
        Console.WriteLine($"Checkout snapshot lock iteration {iteration}/5 OK.");
    }
    return;
}
if (historyOnly)
{
    var candidate = await (from document in db.WarehouseDocuments.AsNoTracking()
        join warehouse in db.Warehouses.AsNoTracking() on document.IdWarehouse equals warehouse.Id
        where warehouse.IsActive
        orderby document.DocumentDate descending, document.DocumentNumber descending
        select new { document.Id, document.IdWarehouse }).FirstOrDefaultAsync()
        ?? throw new InvalidOperationException("No active warehouse with documents was found.");
    await using var tx = await db.Database.BeginTransactionAsync();
    try
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Warehouse.Warehouse SET IsActive=0 WHERE Id={candidate.IdWarehouse}");
        var documents = new WarehouseDocumentsApiController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
            { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "system.full-access")], "test")) } }
        };
        var historyResponse = await documents.List(candidate.IdWarehouse, null, 1, 200);
        var ok = historyResponse as OkObjectResult ?? throw new InvalidOperationException("Document listing failed.");
        var historyItems = JsonSerializer.SerializeToElement(ok.Value).GetProperty("items");
        if (!historyItems.EnumerateArray().Any(item => item.GetProperty("Id").GetInt64() == candidate.Id))
            throw new InvalidOperationException("Inactive warehouse history disappeared from document list.");
        Console.WriteLine("Inactive warehouse document history OK; transaction rolled back.");
    }
    finally { await tx.RollbackAsync(); }
    return;
}
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
var selectedLink = await (from warehouse in db.Warehouses.AsNoTracking()
    join link in db.WarehouseStockOwners.AsNoTracking() on warehouse.Id equals link.IdWarehouse
    join plan in db.SalesPlanReferences.AsNoTracking() on link.IdStockOwner equals plan.Id
    where warehouse.IsActive && plan.IsActive
    select new { WarehouseId = warehouse.Id, PlanId = plan.Id }).FirstAsync();
var selectedType = await db.WarehouseDocumentTypes.AsNoTracking()
    .Where(x => x.IsActive && x.Direction == "R" && !x.Code.Contains("return") && !x.Name.Contains("مرجوع"))
    .Select(x => x.Id).FirstAsync();
var selectedParty = await db.WarehouseParties.AsNoTracking().Where(x => x.IsActive).Select(x => x.Id).FirstAsync();
var documentApi = new WarehouseDocumentsApiController(db)
{
    ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
    { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "system.full-access")], "test")) } }
};
var validHeader = new SaveWarehouseDocumentInput(selectedLink.WarehouseId, selectedType, selectedParty,
    selectedLink.PlanId, DateOnly.FromDateTime(DateTime.Today), "D", "validation-only", null,
    [new WarehouseDocumentLineInput(1, 1, 1, selectedLink.PlanId)]);
foreach (var invalid in new[]
{
    validHeader with { Reason = "   " },
    validHeader with { Status = "invalid" },
    validHeader with { DocumentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)) },
    validHeader with { Lines = [] },
    validHeader with { Lines = [new WarehouseDocumentLineInput(1, 1, 0, selectedLink.PlanId)] }
})
{
    var invalidResult = await documentApi.Create(invalid, CancellationToken.None);
    if (invalidResult is not ObjectResult { Value: ProblemDetails { Status: 400 } })
        throw new InvalidOperationException($"Invalid warehouse document returned {invalidResult.GetType().Name}: {JsonSerializer.Serialize((invalidResult as ObjectResult)?.Value)}, status={(invalidResult as ObjectResult)?.StatusCode}.");
}
documentApi.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
    [new Claim(ClaimTypes.NameIdentifier, "9223372036854775000")], "test"));
if (await documentApi.Create(validHeader, CancellationToken.None) is not ForbidResult)
    throw new InvalidOperationException("Unauthorized user could create a warehouse document.");
Console.WriteLine("Warehouse document validation and access OK.");
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
