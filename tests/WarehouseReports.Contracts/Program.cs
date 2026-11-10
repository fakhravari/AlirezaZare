using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shop.Admin.Controllers.Api;
using Shop.Data.Infrastructure.Persistence;
using Shop.Data.Infrastructure.Persistence.Repositories;

string connectionString = Environment.GetEnvironmentVariable("SHOP_TEST_CONNECTION")
    ?? throw new InvalidOperationException("SHOP_TEST_CONNECTION is required.");
var connection = new SqlConnectionStringBuilder(connectionString);
bool historyOnly = args.Contains("--history-only", StringComparer.Ordinal);
bool mainRollback = args.Contains("--main-rollback", StringComparer.Ordinal);
bool concurrency = args.Contains("--concurrency", StringComparer.Ordinal);
bool unitLoop = args.Contains("--unit-loop", StringComparer.Ordinal);
bool unitController = args.Contains("--unit-controller", StringComparer.Ordinal);
bool stockOverflow = args.Contains("--stock-overflow", StringComparer.Ordinal);
bool documentRace = args.Contains("--document-race", StringComparer.Ordinal);
bool multiStockRace = args.Contains("--multi-stock-race", StringComparer.Ordinal);
bool dispatchStockRace = args.Contains("--dispatch-stock-race", StringComparer.Ordinal);
bool fixtureCheck = args.Contains("--fixture-check", StringComparer.Ordinal);
bool aggregateMixedPlan = args.Contains("--aggregate-mixed-plan", StringComparer.Ordinal);
bool aggregateMultiPlan = aggregateMixedPlan || args.Contains("--aggregate-multiplan", StringComparer.Ordinal);
bool aggregateDispatch = aggregateMultiPlan || args.Contains("--aggregate-dispatch", StringComparer.Ordinal);
bool stockRace = dispatchStockRace || multiStockRace || args.Contains("--stock-race", StringComparer.Ordinal);
if (!historyOnly && !mainRollback && !concurrency && !unitLoop && !unitController && !stockOverflow && !documentRace && !stockRace && !fixtureCheck && !aggregateDispatch && !connection.InitialCatalog.StartsWith("ShopVerify_", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Reports tests require a ShopVerify_ database.");
var options = new DbContextOptionsBuilder<ShopDbContext>().UseSqlServer(connectionString).Options;
await using var db = new ShopDbContext(options);
if (stockOverflow)
{
    await StockOverflowTest.RunAsync(options);
    return;
}
if (unitController)
{
    await UnitControllerTest.RunAsync(options);
    return;
}
if (aggregateDispatch)
{
    await AggregateDispatchTest.RunAsync(options, aggregateMultiPlan, aggregateMixedPlan);
    return;
}
if (fixtureCheck)
{
    int remainingFixtures = await db.Database.SqlQueryRaw<int>("""
        SELECT (SELECT COUNT(*) FROM Warehouse.Product WHERE ProductKey LIKE N'concurrency-stock-%')
             + (SELECT COUNT(*) FROM Account.Profile WHERE Email LIKE N'concurrency-stock-%@example.invalid')
             + (SELECT COUNT(*) FROM Warehouse.PaidOrderInbox WHERE CustomerKey LIKE N'concurrency-stock-%@example.invalid') AS [Value]
        """).SingleAsync();
    if (remainingFixtures != 0) throw new InvalidOperationException($"{remainingFixtures} concurrency fixtures remain.");
    Console.WriteLine("No concurrency product, profile or inbox fixtures remain.");
    return;
}
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
if (stockRace)
{
    long raceWarehouseId = selectedLink.WarehouseId;
    long racePlanId = selectedLink.PlanId;
    if (dispatchStockRace)
    {
        raceWarehouseId = await db.Database.SqlQueryRaw<long>("SELECT [IdWarehouse] AS [Value] FROM [Store].[SiteSetting] WHERE [Id]=1").SingleAsync();
        racePlanId = await (from owner in db.WarehouseStockOwners.AsNoTracking()
            join plan in db.SalesPlanReferences.AsNoTracking() on owner.IdStockOwner equals plan.Id
            where owner.IdWarehouse == raceWarehouseId && plan.IsActive
            select plan.Id).FirstAsync();
    }
    long unitId = await db.WarehouseUnits.AsNoTracking().Where(x => x.IsActive).Select(x => x.Id).FirstAsync();
    long receiptType = await db.WarehouseDocumentTypes.AsNoTracking()
        .Where(x => x.IsActive && x.AffectsStock && x.Direction == "R" && !x.Code.Contains("return") && !x.Name.Contains("مرجوع"))
        .Select(x => x.Id).FirstAsync();
    long issueType = await db.WarehouseDocumentTypes.AsNoTracking()
        .Where(x => x.IsActive && x.AffectsStock && x.Direction == "I" && !x.Code.Contains("return") && !x.Name.Contains("مرجوع"))
        .Select(x => x.Id).FirstAsync();
    string testKey = $"concurrency-stock-{Guid.NewGuid():N}";
    var products = new List<(long ProductId, long ItemId, string Key)>();
    await using (var setup = await db.Database.BeginTransactionAsync())
    {
        for (int index = 0; index < (multiStockRace ? 2 : 1); index++)
        {
            long productId = long.MaxValue - Random.Shared.NextInt64(1, 1000000000);
            string productKey = $"{testKey}-{index}";
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT Warehouse.Product (Id,IdBaseUnit,Name,ProductKey,Code,IsWarehouseEnabled,IsStoreEnabled,IsCatalogActive) VALUES ({productId},{unitId},{productKey},{productKey},{productKey},1,0,0)");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT Warehouse.ProductUnit (IdProduct,IdUnit,ConversionFactor,IsBaseUnit,IsActive) VALUES ({productId},{unitId},1,1,1)");
            long testItemId = (await db.Database.SqlQuery<long>($"INSERT Warehouse.InventoryItem (IdProduct,IsActive) OUTPUT INSERTED.Id AS [Value] VALUES ({productId},1)").ToListAsync()).Single();
            products.Add((productId, testItemId, productKey));
        }
        await setup.CommitAsync();
    }
    try
    {
        for (int iteration = 1; iteration <= 3; iteration++)
        {
            var receipt = validHeader with { WarehouseId = raceWarehouseId, SalesPlanId = racePlanId, DocumentTypeId = receiptType, Status = "F", Reason = testKey,
                Lines = products.Select(x => new WarehouseDocumentLineInput(x.ItemId, unitId, 10, racePlanId)).ToArray() };
            var issue = receipt with { DocumentTypeId = issueType,
                Lines = products.Select(x => new WarehouseDocumentLineInput(x.ItemId, unitId, 7, racePlanId)).ToArray() };
            var reversedIssue = issue with { Lines = issue.Lines!.Reverse().ToArray() };
            await using var receiptDb = new ShopDbContext(options);
            var createdReceipt = await NewFullAccessController(receiptDb).Create(receipt, CancellationToken.None) as OkObjectResult
                ?? throw new InvalidOperationException("Could not create test receipt.");
            long receiptId = JsonSerializer.SerializeToElement(createdReceipt.Value).GetProperty("Id").GetInt64();
            long testProfileId = 0, testOrderId = 0, testInboxId = 0;
            try
            {
                if (dispatchStockRace)
                {
                    await using var fixture = new ShopDbContext(options);
                    await using var fixtureTx = await fixture.Database.BeginTransactionAsync();
                    string email = $"{testKey}-{iteration}@example.invalid";
                    string orderNumber = $"TEST{Guid.NewGuid():N}"[..28];
                    string gateway = $"{testKey}-{iteration}";
                    testProfileId = (await fixture.Database.SqlQuery<long>($"INSERT Account.Profile (Email,FirstName,LastName) OUTPUT INSERTED.Id AS [Value] VALUES ({email},N'Test',N'Dispatch concurrency')").ToListAsync()).Single();
                    testOrderId = (await fixture.Database.SqlQuery<long>($"INSERT Store.[Order] (IdProfile,OrderNumber,Status,PaymentMethod,ItemCount,TotalAmount,AddressSnapshotJson) OUTPUT INSERTED.Id AS [Value] VALUES ({testProfileId},{orderNumber},N'payment-confirmed',N'demo',1,100,N'{{}}')").ToListAsync()).Single();
                    await fixture.Database.ExecuteSqlInterpolatedAsync($"INSERT Store.OrderItem (IdCustomerOrder,IdProduct,ProductNameSnapshot,IdSalesPlan,Quantity,UnitAmount,OptionsJson) VALUES ({testOrderId},{products[0].ProductId},{testKey},{racePlanId},7,100,N'{{}}')");
                    await fixture.Database.ExecuteSqlInterpolatedAsync($"INSERT Store.PaymentAttempt (Id,IdOrder,Amount,Status,GatewayReference,ReceiptReference,PaidAtUtc) VALUES (NEWID(),{testOrderId},100,N'verified',{gateway},{gateway},SYSUTCDATETIME())");
                    testInboxId = (await fixture.Database.SqlQuery<long>($"INSERT Warehouse.PaidOrderInbox (StoreOrderKey,CustomerKey,PaymentAtUtc) OUTPUT INSERTED.Id AS [Value] VALUES ({orderNumber},{email},SYSUTCDATETIME())").ToListAsync()).Single();
                    await fixture.Database.ExecuteSqlInterpolatedAsync($"INSERT Warehouse.PaidOrderInboxLine (IdPaidOrderInbox,IdInventoryItem,IdUnit,Quantity,ConversionFactor,BaseQuantity,IdSalesPlanReference) VALUES ({testInboxId},{products[0].ItemId},{unitId},7,1,7,{racePlanId})");
                    await fixtureTx.CommitAsync();
                }
                if (multiStockRace)
                {
                    var partialIssue = issue with { Lines = [
                        new WarehouseDocumentLineInput(products[0].ItemId, unitId, 7, racePlanId),
                        new WarehouseDocumentLineInput(products[1].ItemId, unitId, 11, racePlanId)] };
                    await using var partialDb = new ShopDbContext(options);
                    if (await NewFullAccessController(partialDb).Create(partialIssue, CancellationToken.None) is not ConflictObjectResult)
                        throw new InvalidOperationException("Issue with one insufficient item was not rejected.");
                    await using var partialVerify = new ShopDbContext(options);
                    foreach (var product in products)
                    {
                        decimal beforeRace = await partialVerify.StockBalances.AsNoTracking()
                            .Where(x => x.IdWarehouse == raceWarehouseId && x.IdInventoryItem == product.ItemId && x.IdSalesPlanReference == racePlanId)
                            .Select(x => x.Quantity).SingleAsync();
                        if (beforeRace != 10) throw new InvalidOperationException("Failed issue partially changed stock.");
                    }
                    if (await partialVerify.WarehouseDocuments.AsNoTracking().CountAsync(x => x.Reason == testKey) != 1)
                        throw new InvalidOperationException("Failed issue left a document behind.");
                }
                await using var firstDb = new ShopDbContext(options);
                await using var secondDb = new ShopDbContext(options);
                Task<IActionResult> first, second;
                if (dispatchStockRace)
                {
                    await using var blocker = new SqlConnection(connectionString);
                    await blocker.OpenAsync();
                    await using var blockerTx = (SqlTransaction)await blocker.BeginTransactionAsync();
                    await using (var lockCommand = blocker.CreateCommand())
                    {
                        lockCommand.Transaction = blockerTx;
                        lockCommand.CommandText = "SELECT [Id] FROM Warehouse.StockBalance WITH (UPDLOCK,HOLDLOCK) WHERE IdWarehouse=@warehouse AND IdInventoryItem=@item AND IdSalesPlanReference=@plan";
                        lockCommand.Parameters.AddWithValue("@warehouse", raceWarehouseId);
                        lockCommand.Parameters.AddWithValue("@item", products[0].ItemId);
                        lockCommand.Parameters.AddWithValue("@plan", racePlanId);
                        if (await lockCommand.ExecuteScalarAsync() is null) throw new InvalidOperationException("Test balance disappeared.");
                    }
                    var dispatch = new StorePaidOrdersDispatchApiController(secondDb) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "system.full-access")], "test")) } } };
                    if (iteration % 2 == 0)
                    {
                        first = NewFullAccessController(firstDb).Create(issue, CancellationToken.None);
                        await Task.Delay(1000);
                        second = dispatch.Create(new StorePaidOrdersDispatchApiController.DispatchInput([testOrderId]), CancellationToken.None);
                    }
                    else
                    {
                        second = dispatch.Create(new StorePaidOrdersDispatchApiController.DispatchInput([testOrderId]), CancellationToken.None);
                        await Task.Delay(1000);
                        first = NewFullAccessController(firstDb).Create(issue, CancellationToken.None);
                    }
                    await Task.Delay(500);
                    if (first.IsCompleted || second.IsCompleted) throw new InvalidOperationException("A competing issue did not wait for the test stock lock.");
                    await blockerTx.RollbackAsync();
                }
                else
                {
                    first = NewFullAccessController(firstDb).Create(issue, CancellationToken.None);
                    second = NewFullAccessController(secondDb).Create(reversedIssue, CancellationToken.None);
                }
                IActionResult[] issueResults = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(60));
                if (issueResults.Count(x => x is OkObjectResult) != 1 || issueResults.Count(x => x is ConflictObjectResult) != 1)
                    throw new InvalidOperationException($"Expected one issue and one stock conflict, got {string.Join(", ", issueResults.Select(x => x.GetType().Name))}.");
                await using var verify = new ShopDbContext(options);
                foreach (var product in products)
                {
                    decimal quantity = await verify.StockBalances.AsNoTracking().Where(x => x.IdWarehouse == raceWarehouseId && x.IdInventoryItem == product.ItemId && x.IdSalesPlanReference == racePlanId).Select(x => x.Quantity).SingleAsync();
                    if (quantity != 3) throw new InvalidOperationException($"Expected stock 3 for each item, got {quantity}.");
                }
                if (dispatchStockRace)
                {
                    string state = await verify.Database.SqlQuery<string>($"SELECT CONCAT(o.Status,N'/',i.Status) AS [Value] FROM Store.[Order] o JOIN Warehouse.PaidOrderInbox i ON i.StoreOrderKey=o.OrderNumber WHERE o.Id={testOrderId}").SingleAsync();
                    string expected = issueResults[1] is OkObjectResult ? "warehouse-confirmed/C" : "payment-confirmed/P";
                    if (state != expected) throw new InvalidOperationException($"Unexpected order/inbox state: {state}.");
                    int matchingLinks = await verify.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM Store.[Order] o JOIN Warehouse.PaidOrderInboxDocument d ON d.IdWarehouseDocument=o.IdWarehouseDocument WHERE o.Id={testOrderId} AND d.IdPaidOrderInbox={testInboxId}").SingleAsync();
                    if (matchingLinks != (issueResults[1] is OkObjectResult ? 1 : 0))
                        throw new InvalidOperationException("Order and inbox document link do not match the dispatch result.");
                }
                Console.WriteLine($"Concurrent final issues iteration {iteration}/3 OK; {products.Count} item(s), stock 10 -> 3; winner: {(dispatchStockRace && issueResults[1] is OkObjectResult ? "order dispatch" : "manual issue")}.");
            }
            finally
            {
                await using var cleanupDb = new ShopDbContext(options);
                long? dispatchDocumentId = null;
                if (testOrderId != 0)
                {
                    dispatchDocumentId = await cleanupDb.Database.SqlQuery<long?>($"SELECT IdWarehouseDocument AS [Value] FROM Store.[Order] WHERE Id={testOrderId}").SingleOrDefaultAsync();
                    await using var fixtureCleanup = await cleanupDb.Database.BeginTransactionAsync();
                    await cleanupDb.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.PaidOrderInboxDocument WHERE IdPaidOrderInbox={testInboxId}");
                    await cleanupDb.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.PaidOrderInboxLine WHERE IdPaidOrderInbox={testInboxId}");
                    await cleanupDb.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.PaidOrderInbox WHERE Id={testInboxId}");
                    await cleanupDb.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Store.OrderItem WHERE IdCustomerOrder={testOrderId}");
                    await cleanupDb.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Store.PaymentAttempt WHERE IdOrder={testOrderId}");
                    await cleanupDb.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Store.[Order] WHERE Id={testOrderId} AND IdProfile={testProfileId}");
                    await cleanupDb.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Account.Profile WHERE Id={testProfileId}");
                    await fixtureCleanup.CommitAsync();
                }
                var documentIds = await cleanupDb.WarehouseDocuments.AsNoTracking().Where(x => x.Reason == testKey && x.Id != receiptId).Select(x => x.Id).ToArrayAsync();
                foreach (long id in documentIds.Concat(dispatchDocumentId.HasValue ? [dispatchDocumentId.Value] : Array.Empty<long>()).Append(receiptId))
                {
                    await using var deleteDb = new ShopDbContext(options);
                    if (await NewFullAccessController(deleteDb).Delete(id, CancellationToken.None) is not OkObjectResult)
                        throw new InvalidOperationException($"Could not clean up test document {id}.");
                }
            }
        }
    }
    finally
    {
        await using var cleanup = new ShopDbContext(options);
        await using var tx = await cleanup.Database.BeginTransactionAsync();
        if (await cleanup.WarehouseDocuments.AsNoTracking().AnyAsync(x => x.Reason == testKey))
            throw new InvalidOperationException($"Test documents remain for {testKey}; product was preserved for recovery.");
        foreach (var product in products)
        {
            await cleanup.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.StockBalance WHERE IdInventoryItem={product.ItemId} AND Quantity=0");
            await cleanup.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.InventoryItem WHERE Id={product.ItemId} AND IdProduct={product.ProductId}");
            await cleanup.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.ProductUnit WHERE IdProduct={product.ProductId}");
            await cleanup.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.Product WHERE Id={product.ProductId} AND ProductKey={product.Key}");
        }
        await tx.CommitAsync();
    }
    return;
}
if (documentRace)
{
    var selectedItem = await (from item in db.InventoryItems.AsNoTracking()
        join unit in db.WarehouseProductUnits.AsNoTracking() on item.IdProduct equals unit.IdProduct
        join definition in db.WarehouseUnits.AsNoTracking() on unit.IdUnit equals definition.Id
        where item.IsActive && item.Product.IsWarehouseEnabled &&
              (item.ProductVariant == null || item.ProductVariant.IsActive) && unit.IsActive && definition.IsActive &&
              unit.ConversionFactor >= 0.001m && unit.ConversionFactor <= 1000000m
        select new { ItemId = item.Id, UnitId = unit.IdUnit }).FirstAsync();
    var testInput = validHeader with { Reason = "concurrency-test-draft", Lines =
        [new WarehouseDocumentLineInput(selectedItem.ItemId, selectedItem.UnitId, 1, selectedLink.PlanId)] };
    for (int iteration = 1; iteration <= 3; iteration++)
    {
        await using var creatorDb = new ShopDbContext(options);
        var creator = NewFullAccessController(creatorDb);
        var created = await creator.Create(testInput, CancellationToken.None) as OkObjectResult
            ?? throw new InvalidOperationException("Failed to create concurrency test draft.");
        long testId = JsonSerializer.SerializeToElement(created.Value).GetProperty("Id").GetInt64();
        try
        {
            await using var blocker = new SqlConnection(connectionString);
            await blocker.OpenAsync();
            await using var blockerTx = (SqlTransaction)await blocker.BeginTransactionAsync();
            await using (var lockCommand = blocker.CreateCommand())
            {
                lockCommand.Transaction = blockerTx;
                lockCommand.CommandText = "SELECT [Id] FROM [Warehouse].[WarehouseDocument] WITH (UPDLOCK, HOLDLOCK) WHERE [Id]=@id";
                lockCommand.Parameters.AddWithValue("@id", testId);
                if (await lockCommand.ExecuteScalarAsync() is null) throw new InvalidOperationException("Test draft disappeared.");
            }
            await using var updateDb = new ShopDbContext(options);
            await using var deleteDb = new ShopDbContext(options);
            var updateTask = NewFullAccessController(updateDb).Update(testId,
                testInput with { Reason = "concurrency-test-updated" }, CancellationToken.None);
            var deleteTask = NewFullAccessController(deleteDb).Delete(testId, CancellationToken.None);
            await Task.Delay(1500);
            if (updateTask.IsCompleted || deleteTask.IsCompleted)
                throw new InvalidOperationException("A competing document operation bypassed the existing update lock.");
            await blockerTx.RollbackAsync();
            IActionResult[] outcomes = await Task.WhenAll(updateTask, deleteTask).WaitAsync(TimeSpan.FromSeconds(60));
            if (outcomes[1] is not OkObjectResult || outcomes[0] is not (OkObjectResult or NotFoundResult))
                throw new InvalidOperationException($"Unexpected concurrent update/delete results: {outcomes[0].GetType().Name}, {outcomes[1].GetType().Name}.");
            await using var verifyDb = new ShopDbContext(options);
            if (await verifyDb.WarehouseDocuments.AnyAsync(x => x.Id == testId) ||
                await verifyDb.WarehouseDocumentLines.AnyAsync(x => x.IdDocument == testId) ||
                await verifyDb.WarehouseStockMovements.AnyAsync(x => x.IdDocument == testId))
                throw new InvalidOperationException("Deleted draft, its lines or its stock movements remain.");
            string[] auditActions = await verifyDb.WarehouseDocumentAudits.AsNoTracking()
                .Where(x => x.DocumentId == testId).OrderBy(x => x.Id).Select(x => x.Action).ToArrayAsync();
            string[] expectedActions = outcomes[0] is OkObjectResult
                ? ["Created", "Updated", "Deleted"] : ["Created", "Deleted"];
            if (!auditActions.SequenceEqual(expectedActions))
                throw new InvalidOperationException("Audit history does not match the committed operations.");
            Console.WriteLine($"Concurrent document update/delete iteration {iteration}/3 OK.");
        }
        finally
        {
            await using var cleanupDb = new ShopDbContext(options);
            if (await cleanupDb.WarehouseDocuments.AnyAsync(x => x.Id == testId))
            {
                var cleanup = await NewFullAccessController(cleanupDb).Delete(testId, CancellationToken.None);
                if (cleanup is not OkObjectResult) throw new InvalidOperationException($"Test draft {testId} could not be cleaned up.");
            }
        }
    }
    return;
}
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
if (await documentApi.Update(long.MaxValue, validHeader, CancellationToken.None) is not NotFoundResult ||
    await documentApi.Delete(long.MaxValue, CancellationToken.None) is not NotFoundResult)
    throw new InvalidOperationException("Missing document update/delete did not return 404.");
long? editableDocumentId = await db.Database.SqlQueryRaw<long?>("""
    SELECT TOP (1) d.[Id] AS [Value] FROM [Warehouse].[WarehouseDocument] d
    WHERE NOT EXISTS (SELECT 1 FROM [Warehouse].[PaidOrderInboxDocument] p WHERE p.[IdWarehouseDocument] = d.[Id])
    ORDER BY d.[Id]
    """).SingleOrDefaultAsync();
if (editableDocumentId.HasValue && await documentApi.Update(editableDocumentId.Value,
        validHeader with { Reason = "   " }, CancellationToken.None) is not ObjectResult { Value: ProblemDetails { Status: 400 } })
    throw new InvalidOperationException("Existing document update did not validate the reason.");
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

static WarehouseDocumentsApiController NewFullAccessController(ShopDbContext context) => new(context)
{
    ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
    { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "system.full-access")], "test")) } }
};
