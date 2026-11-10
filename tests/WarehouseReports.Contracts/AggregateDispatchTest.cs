using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Admin.Controllers.Api;
using Shop.Data.Infrastructure.Persistence;

internal static class AggregateDispatchTest
{
    public static async Task RunAsync(DbContextOptions<ShopDbContext> options, bool multiPlan, bool mixedPlan)
    {
        for (int iteration = 1; iteration <= 3; iteration++)
            await RunIterationAsync(options, iteration, multiPlan, mixedPlan);
    }

    private static async Task RunIterationAsync(DbContextOptions<ShopDbContext> options, int iteration, bool multiPlan, bool mixedPlan)
    {
        await using var db = new ShopDbContext(options);
        long warehouse = await db.Database.SqlQueryRaw<long>("SELECT IdWarehouse AS [Value] FROM Store.SiteSetting WHERE Id=1").SingleAsync();
        long[] plans = await (from owner in db.WarehouseStockOwners.AsNoTracking()
            join p in db.SalesPlanReferences.AsNoTracking() on owner.IdStockOwner equals p.Id
            where owner.IdWarehouse == warehouse && p.IsActive && p.IdStockOwnerType == 1
            orderby p.Id select p.Id).Take(multiPlan ? 2 : 1).ToArrayAsync();
        if (plans.Length != (multiPlan ? 2 : 1)) throw new InvalidOperationException("Not enough active sales plans connected to the sales warehouse.");
        long plan = plans[0];
        long unit = await db.WarehouseUnits.AsNoTracking().Where(x => x.IsActive).Select(x => x.Id).FirstAsync();
        long receiptType = await db.WarehouseDocumentTypes.AsNoTracking()
            .Where(x => x.IsActive && x.AffectsStock && x.Direction == "R" && !x.Code.Contains("return") && !x.Name.Contains("مرجوع"))
            .Select(x => x.Id).FirstAsync();
        long party = await db.WarehouseParties.AsNoTracking().Where(x => x.IsActive).Select(x => x.Id).FirstAsync();
        string key = $"concurrency-stock-{Guid.NewGuid():N}";
        string email = $"{key}@example.invalid";
        var products = new List<(long ProductId, long ItemId, string Key)>();
        var orders = new List<(long Id, long InboxId)>();
        long profile = 0, receiptId = 0;
        try
        {
            await using (var setup = await db.Database.BeginTransactionAsync())
            {
                for (int i = 0; i < 2; i++)
                {
                    long product = long.MaxValue - Random.Shared.NextInt64(1, 1000000000);
                    string productKey = $"{key}-{i}";
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT Warehouse.Product (Id,IdBaseUnit,Name,ProductKey,Code,IsWarehouseEnabled,IsStoreEnabled,IsCatalogActive) VALUES ({product},{unit},{productKey},{productKey},{productKey},1,0,0)");
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT Warehouse.ProductUnit (IdProduct,IdUnit,ConversionFactor,IsBaseUnit,IsActive) VALUES ({product},{unit},1,1,1)");
                    long item = (await db.Database.SqlQuery<long>($"INSERT Warehouse.InventoryItem (IdProduct,IsActive) OUTPUT INSERTED.Id AS [Value] VALUES ({product},1)").ToListAsync()).Single();
                    products.Add((product, item, productKey));
                }
                profile = (await db.Database.SqlQuery<long>($"INSERT Account.Profile (Email,FirstName,LastName) OUTPUT INSERTED.Id AS [Value] VALUES ({email},N'Test',N'Aggregate dispatch')").ToListAsync()).Single();
                for (int i = 0; i < 2; i++)
                {
                    int qty = i + 3;
                    long orderPlan = multiPlan ? plans[i] : plan;
                    int amount = qty * (mixedPlan ? 400 : 200);
                    string number = $"TEST{Guid.NewGuid():N}"[..28];
                    string reference = $"{key}-{i}";
                    long order = (await db.Database.SqlQuery<long>($"INSERT Store.[Order] (IdProfile,OrderNumber,Status,PaymentMethod,ItemCount,TotalAmount,AddressSnapshotJson) OUTPUT INSERTED.Id AS [Value] VALUES ({profile},{number},N'payment-confirmed',N'demo',{qty * (mixedPlan ? 4 : 2)},{amount},N'{{}}')").ToListAsync()).Single();
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT Store.PaymentAttempt (Id,IdOrder,Amount,Status,GatewayReference,ReceiptReference,PaidAtUtc) VALUES (NEWID(),{order},{amount},N'verified',{reference},{reference},SYSUTCDATETIME())");
                    long inbox = (await db.Database.SqlQuery<long>($"INSERT Warehouse.PaidOrderInbox (StoreOrderKey,CustomerKey,PaymentAtUtc) OUTPUT INSERTED.Id AS [Value] VALUES ({number},{email},SYSUTCDATETIME())").ToListAsync()).Single();
                    orders.Add((order, inbox));
                    foreach (long linePlan in mixedPlan ? plans : [orderPlan])
                    foreach (var product in products)
                    {
                        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT Store.OrderItem (IdCustomerOrder,IdProduct,ProductNameSnapshot,IdSalesPlan,Quantity,UnitAmount,OptionsJson) VALUES ({order},{product.ProductId},{key},{linePlan},{qty},100,N'{{}}')");
                        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT Warehouse.PaidOrderInboxLine (IdPaidOrderInbox,IdInventoryItem,IdUnit,Quantity,ConversionFactor,BaseQuantity,IdSalesPlanReference) VALUES ({inbox},{product.ItemId},{unit},{qty},1,{qty},{linePlan})");
                    }
                }
                await setup.CommitAsync();
            }
            var receiptLines = new List<WarehouseDocumentLineInput>();
            foreach (long itemPlan in plans)
                for (int i = 0; i < products.Count; i++)
                    receiptLines.Add(new WarehouseDocumentLineInput(products[i].ItemId, unit,
                        multiPlan ? (itemPlan == plans[1] && i == 1 ? (mixedPlan ? 6 : 3) : 10) : (i == 1 ? 6 : 10), itemPlan));
            var receipt = new SaveWarehouseDocumentInput(warehouse, receiptType, party, plan,
                DateOnly.FromDateTime(DateTime.Today), "F", key, null, receiptLines);
            await using (var creatorDb = new ShopDbContext(options))
            {
                var created = await DocumentController(creatorDb).Create(receipt, CancellationToken.None) as OkObjectResult
                    ?? throw new InvalidOperationException("Failed to create aggregate test receipt.");
                receiptId = JsonSerializer.SerializeToElement(created.Value).GetProperty("Id").GetInt64();
            }
            long[] orderIds = orders.Select(x => x.Id).ToArray();
            await using (var insufficientDb = new ShopDbContext(options))
            {
                if (await DispatchController(insufficientDb).Create(new(orderIds), CancellationToken.None) is not ConflictObjectResult)
                    throw new InvalidOperationException("Aggregate dispatch with one short item was not rejected.");
            }
            await AssertBalancesAsync(options, warehouse, plan, products, multiPlan ? [10m, 10m] : [10m, 6m]);
            if (multiPlan) await AssertBalancesAsync(options, warehouse, plans[1], products, mixedPlan ? [10m, 6m] : [10m, 3m]);
            int pending = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM Store.[Order] o JOIN Warehouse.PaidOrderInbox i ON i.StoreOrderKey=o.OrderNumber JOIN Store.PaymentAttempt p ON p.IdOrder=o.Id AND p.Status=N'verified' WHERE o.IdProfile={profile} AND o.Status=N'payment-confirmed' AND o.IdWarehouseDocument IS NULL AND i.Status='P' AND NOT EXISTS (SELECT 1 FROM Warehouse.PaidOrderInboxDocument d WHERE d.IdPaidOrderInbox=i.Id)").SingleAsync();
            if (pending != 2) throw new InvalidOperationException("Insufficient stock changed an order, payment, queue or document link.");
            await using (var updateDb = new ShopDbContext(options))
            {
                var replenished = receipt with { Lines = plans.SelectMany(itemPlan => products.Select(product =>
                    new WarehouseDocumentLineInput(product.ItemId, unit, 10, itemPlan))).ToArray() };
                if (await DocumentController(updateDb).Update(receiptId, replenished, CancellationToken.None) is not OkObjectResult)
                    throw new InvalidOperationException("Could not replenish aggregate test stock.");
            }
            await using var firstDb = new ShopDbContext(options);
            await using var secondDb = new ShopDbContext(options);
            await using var blocker = await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM Store.[Order] WITH (UPDLOCK,HOLDLOCK) WHERE Id={orderIds[0]}");
            var first = DispatchController(firstDb).Create(new(orderIds), CancellationToken.None);
            var second = DispatchController(secondDb).Create(new(orderIds.Reverse().ToArray()), CancellationToken.None);
            await Task.Delay(1500);
            if (first.IsCompleted || second.IsCompleted)
                throw new InvalidOperationException("A competing dispatch did not wait for the order lock.");
            await blocker.RollbackAsync();
            IActionResult[] results = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(60));
            if (results.Count(x => x is OkObjectResult) != 1 || results.Count(x => x is ConflictObjectResult) != 1)
                throw new InvalidOperationException($"Expected one dispatch and one conflict: {string.Join(", ", results.Select(x => x.GetType().Name))}.");
            long document = JsonSerializer.SerializeToElement(((OkObjectResult)results.Single(x => x is OkObjectResult)).Value).GetProperty("Id").GetInt64();
            await AssertBalancesAsync(options, warehouse, plan, products, multiPlan && !mixedPlan ? [7m, 7m] : [3m, 3m]);
            if (multiPlan) await AssertBalancesAsync(options, warehouse, plans[1], products, mixedPlan ? [3m, 3m] : [6m, 6m]);
            await using var verify = new ShopDbContext(options);
            var lines = await verify.WarehouseDocumentLines.AsNoTracking().Where(x => x.IdDocument == document).ToArrayAsync();
            if (lines.Length != (multiPlan ? 4 : 2) || products.Any(p =>
                    lines.Count(l => l.IdInventoryItem == p.ItemId && l.BaseQuantity == (multiPlan && !mixedPlan ? 3 : 7) && l.IdSalesPlanReference == plan) != 1 ||
                    (multiPlan && lines.Count(l => l.IdInventoryItem == p.ItemId && l.BaseQuantity == (mixedPlan ? 7 : 4) && l.IdSalesPlanReference == plans[1]) != 1)))
                throw new InvalidOperationException("Aggregated document lines do not match both orders.");
            int linked = await verify.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM Store.[Order] o JOIN Warehouse.PaidOrderInbox i ON i.StoreOrderKey=o.OrderNumber JOIN Warehouse.PaidOrderInboxDocument d ON d.IdPaidOrderInbox=i.Id AND d.IdWarehouseDocument=o.IdWarehouseDocument WHERE o.IdProfile={profile} AND o.Status=N'warehouse-confirmed' AND i.Status='C' AND o.IdWarehouseDocument={document}").SingleAsync();
            if (linked != 2) throw new InvalidOperationException("Both orders were not linked to the same document.");
            Console.WriteLine($"Aggregate dispatch iteration {iteration}/3 OK: shortage rolled back; two orders, two products, {plans.Length} plan(s), mixed={mixedPlan}, one dispatch.");
        }
        finally
        {
            await CleanupAsync(options, key, profile, receiptId, products, orders);
        }
    }

    private static async Task AssertBalancesAsync(DbContextOptions<ShopDbContext> options, long warehouse, long plan,
        List<(long ProductId, long ItemId, string Key)> products, decimal[] expected)
    {
        await using var db = new ShopDbContext(options);
        for (int i = 0; i < products.Count; i++)
        {
            long item = products[i].ItemId;
            decimal quantity = await db.StockBalances.AsNoTracking().Where(x => x.IdWarehouse == warehouse && x.IdInventoryItem == item && x.IdSalesPlanReference == plan).Select(x => x.Quantity).SingleAsync();
            if (quantity != expected[i]) throw new InvalidOperationException($"Expected {expected[i]} stock for test item, got {quantity}.");
        }
    }

    private static async Task CleanupAsync(DbContextOptions<ShopDbContext> options, string key, long profile, long receiptId,
        List<(long ProductId, long ItemId, string Key)> products, List<(long Id, long InboxId)> orders)
    {
        await using var db = new ShopDbContext(options);
        var dispatchIds = new List<long>();
        foreach (var order in orders)
        {
            long? id = await db.Database.SqlQuery<long?>($"SELECT IdWarehouseDocument AS [Value] FROM Store.[Order] WHERE Id={order.Id}").SingleOrDefaultAsync();
            if (id.HasValue && !dispatchIds.Contains(id.Value)) dispatchIds.Add(id.Value);
        }
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            foreach (var order in orders)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.PaidOrderInboxDocument WHERE IdPaidOrderInbox={order.InboxId}");
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.PaidOrderInboxLine WHERE IdPaidOrderInbox={order.InboxId}");
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.PaidOrderInbox WHERE Id={order.InboxId}");
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Store.OrderItem WHERE IdCustomerOrder={order.Id}");
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Store.PaymentAttempt WHERE IdOrder={order.Id}");
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Store.[Order] WHERE Id={order.Id} AND IdProfile={profile}");
            }
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Account.Profile WHERE Id={profile} AND Email={key + "@example.invalid"}");
            await tx.CommitAsync();
        }
        if (receiptId != 0) dispatchIds.Add(receiptId);
        foreach (long id in dispatchIds)
        {
            await using var deleteDb = new ShopDbContext(options);
            if (await DocumentController(deleteDb).Delete(id, CancellationToken.None) is not OkObjectResult)
                throw new InvalidOperationException($"Could not clean up test document {id}.");
        }
        await using var clean = await db.Database.BeginTransactionAsync();
        foreach (var product in products)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.StockBalance WHERE IdInventoryItem={product.ItemId} AND Quantity=0");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.InventoryItem WHERE Id={product.ItemId} AND IdProduct={product.ProductId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.ProductUnit WHERE IdProduct={product.ProductId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.Product WHERE Id={product.ProductId} AND ProductKey={product.Key}");
        }
        await clean.CommitAsync();
    }

    private static ControllerContext Context() => new() { HttpContext = new DefaultHttpContext
    { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "system.full-access")], "test")) } };
    private static WarehouseDocumentsApiController DocumentController(ShopDbContext db) => new(db) { ControllerContext = Context() };
    private static StorePaidOrdersDispatchApiController DispatchController(ShopDbContext db) => new(db) { ControllerContext = Context() };
}
