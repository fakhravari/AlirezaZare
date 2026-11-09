using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shop.Controllers;
using Shop.Database.Infrastructure.Persistence;
using Shop.Database.Infrastructure.Persistence.Repositories;
using Shop.Services;

internal static class CheckoutVariantTest
{
    public static async Task RunAsync(bool race, bool multiPlan)
    {
        string connection = Environment.GetEnvironmentVariable("SHOP_TEST_CONNECTION") ?? throw new InvalidOperationException("SHOP_TEST_CONNECTION is required.");
        var options = new DbContextOptionsBuilder<ShopDbContext>().UseSqlServer(connection).Options;
        await using var db = new ShopDbContext(options);
        long warehouse = await db.Database.SqlQueryRaw<long>("SELECT IdWarehouse AS [Value] FROM Store.SiteSetting WHERE Id=1").SingleAsync();
        var plan = (await db.Database.SqlQueryRaw<Plan>("SELECT Id, SalesPlanKey FROM Warehouse.SalesPlanReference WHERE IsDefault=1 AND IsEnabled=1 AND IsActive=1").ToListAsync()).Single();
        Plan? secondPlan = null;
        if (multiPlan)
        {
            secondPlan = (await db.Database.SqlQuery<Plan>($"""
                SELECT TOP (1) p.Id, p.SalesPlanKey FROM Warehouse.SalesPlanReference p
                JOIN Warehouse.WarehouseStockOwner owner ON owner.IdStockOwner=p.Id AND owner.IdWarehouse={warehouse}
                WHERE p.IsDefault=0 AND p.IsEnabled=1 AND p.IsActive=1 AND p.IdStockOwnerType=1
                  AND (p.StartsAtUtc IS NULL OR p.StartsAtUtc<=SYSUTCDATETIME()) AND (p.EndsAtUtc IS NULL OR p.EndsAtUtc>SYSUTCDATETIME())
                ORDER BY p.Id
                """).ToListAsync()).FirstOrDefault() ?? throw new InvalidOperationException("An active second sales plan connected to the sales warehouse is required.");
        }
        long unit = await db.WarehouseUnits.AsNoTracking().Where(x => x.IsActive).Select(x => x.Id).FirstAsync();
        long profile = 0, product = 2_000_000_000 - Random.Shared.Next(1, 100_000_000), variantA = 0, variantB = 0, itemA = 0, itemB = 0;
        bool setupCommitted = false;
        string key = $"checkout-variant-test-{Guid.NewGuid():N}", email = $"{key}@example.invalid";
        try
        {
            await using (var tx = await db.Database.BeginTransactionAsync())
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT Warehouse.Product (Id,IdBaseUnit,Name,ProductKey,Code,PriceAmount,DiscountPercent,MaxPurchaseQuantity,IsWarehouseEnabled,IsStoreEnabled,IsCatalogActive) VALUES ({product},{unit},{key},{key},{key},4000,0,99,1,1,1)");
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT Warehouse.ProductUnit (IdProduct,IdUnit,ConversionFactor,IsBaseUnit,IsActive) VALUES ({product},{unit},1,1,1)");
                variantA = (await db.Database.SqlQuery<long>($"INSERT Warehouse.ProductVariant (IdProduct,Name,Code,IsActive) OUTPUT INSERTED.Id AS [Value] VALUES ({product},N'Test A',N'A',1)").ToListAsync()).Single();
                variantB = (await db.Database.SqlQuery<long>($"INSERT Warehouse.ProductVariant (IdProduct,Name,Code,IsActive) OUTPUT INSERTED.Id AS [Value] VALUES ({product},N'Test B',N'B',1)").ToListAsync()).Single();
                itemA = (await db.Database.SqlQuery<long>($"INSERT Warehouse.InventoryItem (IdProduct,IdProductVariant,IsActive) OUTPUT INSERTED.Id AS [Value] VALUES ({product},{variantA},1)").ToListAsync()).Single();
                itemB = (await db.Database.SqlQuery<long>($"INSERT Warehouse.InventoryItem (IdProduct,IdProductVariant,IsActive) OUTPUT INSERTED.Id AS [Value] VALUES ({product},{variantB},1)").ToListAsync()).Single();
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT Warehouse.StockBalance (IdWarehouse,IdInventoryItem,IdSalesPlanReference,Quantity) VALUES ({warehouse},{itemA},{plan.Id},5),({warehouse},{itemB},{plan.Id},1)");
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT Store.ProductVariantSalesPlanOffer (IdProductVariant,IdSalesPlan,PriceAmount,DiscountPercent,IsEnabled) VALUES ({variantA},{plan.Id},2000,25,1)");
                if (secondPlan is not null)
                {
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT Warehouse.StockBalance (IdWarehouse,IdInventoryItem,IdSalesPlanReference,Quantity) VALUES ({warehouse},{itemA},{secondPlan.Id},1),({warehouse},{itemB},{secondPlan.Id},4)");
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT Store.ProductSalesPlanOffer (IdProduct,IdSalesPlan,PriceAmount,DiscountPercent,MaxPurchaseQuantity,IsEnabled) VALUES ({product},{secondPlan.Id},6000,10,99,1)");
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT Store.ProductVariantSalesPlanOffer (IdProductVariant,IdSalesPlan,PriceAmount,DiscountPercent,IsEnabled) VALUES ({variantA},{secondPlan.Id},3000,20,1)");
                }
                profile = (await db.Database.SqlQuery<long>($"INSERT Account.Profile (Email,FirstName,LastName) OUTPUT INSERTED.Id AS [Value] VALUES ({email},N'Test',N'Variant')").ToListAsync()).Single();
                await tx.CommitAsync();
                setupCommitted = true;
            }
            long address = (await db.Database.SqlQuery<long>($"""
                INSERT Account.Address (IdProfile,Title,Recipient,Phone,IdCountry,IdProvince,IdCity,Street,PostalCode)
                OUTPUT INSERTED.Id AS [Value]
                SELECT TOP (1) {profile},N'Test',N'Test',N'0000000000',IdCountry,IdProvince,Id,N'Test',N'0000000000' FROM Geo.City ORDER BY Id
                """).ToListAsync()).Single();
            for (int iteration = 1; iteration <= 3; iteration++)
            {
                var badVariant = new AccountController.CheckoutRequest(address, "zarinpal", [new(product, 2, plan.SalesPlanKey!, null, variantB, 4000)]);
                await using (var context = new ShopDbContext(options))
                    if (await Controller(context, profile).Checkout(badVariant, CancellationToken.None) is not ConflictObjectResult)
                        throw new InvalidOperationException("An undersupplied variant was accepted based on another variant's stock.");
                await using (var context = new ShopDbContext(options))
                    if (await Controller(context, profile).Checkout(badVariant with { Lines = [new(product, 2, plan.SalesPlanKey!, null, ExpectedUnitAmount: 4000)] }, CancellationToken.None) is not ConflictObjectResult)
                        throw new InvalidOperationException("A product with variants was accepted without selecting a variant.");
                int before = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM Store.[Order] WHERE IdProfile={profile}").SingleAsync();
                if (before != (iteration - 1) * (secondPlan is null ? 2 : 3)) throw new InvalidOperationException("Rejected variant checkout created an order.");
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Store.ProductVariantSalesPlanOffer SET PriceAmount=8000000000000000000 WHERE IdProductVariant={variantA} AND IdSalesPlan={plan.Id}");
                try
                {
                    await using var overflowDb = new ShopDbContext(options);
                    var overflowResult = await Controller(overflowDb, profile).Checkout(badVariant with { Lines = [new(product, 2, plan.SalesPlanKey!, null, variantA, 6000000000000000000m)] }, CancellationToken.None) as ConflictObjectResult;
                    if (overflowResult is null || JsonSerializer.SerializeToElement(overflowResult.Value).GetProperty("code").GetString() != "order-amount-out-of-range")
                        throw new InvalidOperationException("An order total exceeding decimal(19,0) was not rejected safely.");
                    if (before != await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM Store.[Order] WHERE IdProfile={profile}").SingleAsync())
                        throw new InvalidOperationException("An oversized total created an order.");
                    await using var largeDb = new ShopDbContext(options);
                    var largeResult = await Controller(largeDb, profile).Checkout(badVariant with { Lines = [new(product, 1, plan.SalesPlanKey!, null, variantA, 6000000000000000000m)] }, CancellationToken.None) as OkObjectResult
                        ?? throw new InvalidOperationException("A valid decimal(19,0) amount was rejected.");
                    long largeOrder = JsonSerializer.SerializeToElement(largeResult.Value).GetProperty("id").GetInt64();
                    var largeSaved = (await db.Database.SqlQuery<OrderLine>($"SELECT o.TotalAmount, i.UnitAmount, i.Quantity, i.IdProductVariant, i.IdSalesPlan FROM Store.[Order] o JOIN Store.OrderItem i ON i.IdCustomerOrder=o.Id WHERE o.Id={largeOrder} AND o.IdProfile={profile}").ToListAsync()).Single();
                    if (largeSaved.UnitAmount != 6000000000000000000m || largeSaved.TotalAmount != 6000000000000000000m)
                        throw new InvalidOperationException("A large valid order amount was stored incorrectly.");
                    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Store.OrderItem WHERE IdCustomerOrder={largeOrder}");
                    await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Store.[Order] WHERE Id={largeOrder} AND IdProfile={profile}");
                }
                finally
                {
                    await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Store.ProductVariantSalesPlanOffer SET PriceAmount=2000 WHERE IdProductVariant={variantA} AND IdSalesPlan={plan.Id}");
                }
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Store.ProductVariantSalesPlanOffer SET PriceAmount=2400 WHERE IdProductVariant={variantA} AND IdSalesPlan={plan.Id}");
                try
                {
                    await using var staleDb = new ShopDbContext(options);
                    if (await Controller(staleDb, profile).Checkout(badVariant with { Lines = [new(product, 2, plan.SalesPlanKey!, null, variantA, 1500)] }, CancellationToken.None) is not ConflictObjectResult)
                        throw new InvalidOperationException("A checkout with an outdated variant price was accepted.");
                    if (before != await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM Store.[Order] WHERE IdProfile={profile}").SingleAsync())
                        throw new InvalidOperationException("An outdated price created an order.");
                }
                finally
                {
                    await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Store.ProductVariantSalesPlanOffer SET PriceAmount=2000 WHERE IdProductVariant={variantA} AND IdSalesPlan={plan.Id}");
                }
                await using var validDb = new ShopDbContext(options);
                var validSource = new DatabaseSalesPlanSource(new StoreSalesPlanRepository(validDb));
                var gate = race ? new CheckoutMainTest.GatedSource(validSource) : null;
                var checkout = Controller(validDb, profile, validSource, gate ?? (IProductPurchaseConfigurationSource)validSource)
                    .Checkout(badVariant with { Lines = [new(product, 2, plan.SalesPlanKey!, null, variantA, 1500)] }, CancellationToken.None);
                if (gate is not null)
                {
                    try
                    {
                        await gate.ReadFinished.Task.WaitAsync(TimeSpan.FromSeconds(30));
                        if (checkout.IsCompleted) throw new InvalidOperationException("Variant checkout did not pause inside its transaction.");
                        await CheckRivalUpdatesAsync(connection, itemA, variantA, plan.Id, expectBlocked: true);
                    }
                    catch
                    {
                        gate.Release.TrySetResult();
                        try { await checkout.WaitAsync(TimeSpan.FromSeconds(30)); } catch { }
                        throw;
                    }
                    finally { gate.Release.TrySetResult(); }
                }
                var result = await checkout.WaitAsync(TimeSpan.FromSeconds(30)) as OkObjectResult
                    ?? throw new InvalidOperationException("Valid variant checkout failed.");
                if (gate is not null) await CheckRivalUpdatesAsync(connection, itemA, variantA, plan.Id, expectBlocked: false);
                long order = JsonSerializer.SerializeToElement(result.Value).GetProperty("id").GetInt64();
                var saved = (await db.Database.SqlQuery<OrderLine>($"SELECT o.TotalAmount, i.UnitAmount, i.Quantity, i.IdProductVariant, i.IdSalesPlan FROM Store.[Order] o JOIN Store.OrderItem i ON i.IdCustomerOrder=o.Id WHERE o.Id={order} AND o.IdProfile={profile}").ToListAsync()).Single();
                if (saved.IdProductVariant != variantA || saved.IdSalesPlan != plan.Id || saved.UnitAmount != 1500 || saved.TotalAmount != 3000 || saved.Quantity != 2)
                    throw new InvalidOperationException("Variant, plan, discounted unit price or total is incorrect.");
                await using var fallbackDb = new ShopDbContext(options);
                var fallbackSource = new DatabaseSalesPlanSource(new StoreSalesPlanRepository(fallbackDb));
                var fallbackGate = race ? new CheckoutMainTest.GatedSource(fallbackSource) : null;
                var fallbackTask = Controller(fallbackDb, profile, fallbackSource, fallbackGate ?? (IProductPurchaseConfigurationSource)fallbackSource)
                    .Checkout(badVariant with { Lines = [new(product, 1, plan.SalesPlanKey!, null, variantB, 4000)] }, CancellationToken.None);
                if (fallbackGate is not null)
                {
                    try
                    {
                        await fallbackGate.ReadFinished.Task.WaitAsync(TimeSpan.FromSeconds(30));
                        if (fallbackTask.IsCompleted) throw new InvalidOperationException("Fallback checkout did not pause inside its transaction.");
                        await CheckRivalInsertAsync(connection, variantB, plan.Id, expectBlocked: true);
                    }
                    catch
                    {
                        fallbackGate.Release.TrySetResult();
                        try { await fallbackTask.WaitAsync(TimeSpan.FromSeconds(30)); } catch { }
                        throw;
                    }
                    finally { fallbackGate.Release.TrySetResult(); }
                }
                var fallback = await fallbackTask.WaitAsync(TimeSpan.FromSeconds(30)) as OkObjectResult
                    ?? throw new InvalidOperationException("Variant checkout using the product price failed.");
                if (fallbackGate is not null) await CheckRivalInsertAsync(connection, variantB, plan.Id, expectBlocked: false);
                long fallbackOrder = JsonSerializer.SerializeToElement(fallback.Value).GetProperty("id").GetInt64();
                var fallbackSaved = (await db.Database.SqlQuery<OrderLine>($"SELECT o.TotalAmount, i.UnitAmount, i.Quantity, i.IdProductVariant, i.IdSalesPlan FROM Store.[Order] o JOIN Store.OrderItem i ON i.IdCustomerOrder=o.Id WHERE o.Id={fallbackOrder} AND o.IdProfile={profile}").ToListAsync()).Single();
                if (fallbackSaved.IdProductVariant != variantB || fallbackSaved.IdSalesPlan != plan.Id || fallbackSaved.UnitAmount != 4000 || fallbackSaved.TotalAmount != 4000 || fallbackSaved.Quantity != 1)
                    throw new InvalidOperationException("Variant fallback price or plan is incorrect.");
                if (secondPlan is not null)
                {
                    await using (var shortDb = new ShopDbContext(options))
                        if (await Controller(shortDb, profile).Checkout(new(address, "zarinpal", [new(product, 2, secondPlan.SalesPlanKey, null, variantA, 2400)]), CancellationToken.None) is not ConflictObjectResult)
                            throw new InvalidOperationException("Stock in the first plan masked a shortage in the second plan.");
                    await using var mixedDb = new ShopDbContext(options);
                    var mixed = await Controller(mixedDb, profile).Checkout(new(address, "zarinpal",
                        [new(product, 2, plan.SalesPlanKey, null, variantA, 1500), new(product, 1, secondPlan.SalesPlanKey, null, variantA, 2400), new(product, 2, secondPlan.SalesPlanKey, null, variantB, 5400)]), CancellationToken.None) as OkObjectResult
                        ?? throw new InvalidOperationException("Mixed-plan variant checkout failed.");
                    long mixedOrder = JsonSerializer.SerializeToElement(mixed.Value).GetProperty("id").GetInt64();
                    var mixedLines = await db.Database.SqlQuery<OrderLine>($"SELECT o.TotalAmount, i.UnitAmount, i.Quantity, i.IdProductVariant, i.IdSalesPlan FROM Store.[Order] o JOIN Store.OrderItem i ON i.IdCustomerOrder=o.Id WHERE o.Id={mixedOrder} AND o.IdProfile={profile}").ToArrayAsync();
                    if (mixedLines.Length != 3 || mixedLines.Any(x => x.TotalAmount != 16200) ||
                        !mixedLines.Any(x => x.IdProductVariant == variantA && x.IdSalesPlan == plan.Id && x.UnitAmount == 1500 && x.Quantity == 2) ||
                        !mixedLines.Any(x => x.IdProductVariant == variantA && x.IdSalesPlan == secondPlan.Id && x.UnitAmount == 2400 && x.Quantity == 1) ||
                        !mixedLines.Any(x => x.IdProductVariant == variantB && x.IdSalesPlan == secondPlan.Id && x.UnitAmount == 5400 && x.Quantity == 2))
                        throw new InvalidOperationException("Mixed-plan variant identities or prices are incorrect.");
                }
                var balances = await db.StockBalances.AsNoTracking().Where(x => x.IdInventoryItem == itemA || x.IdInventoryItem == itemB).Select(x => new { x.IdInventoryItem, x.IdSalesPlanReference, x.Quantity }).ToArrayAsync();
                if (balances.Single(x => x.IdInventoryItem == itemA && x.IdSalesPlanReference == plan.Id).Quantity != 5 || balances.Single(x => x.IdInventoryItem == itemB && x.IdSalesPlanReference == plan.Id).Quantity != 1 ||
                    secondPlan is not null && (balances.Single(x => x.IdInventoryItem == itemA && x.IdSalesPlanReference == secondPlan.Id).Quantity != 1 || balances.Single(x => x.IdInventoryItem == itemB && x.IdSalesPlanReference == secondPlan.Id).Quantity != 4))
                    throw new InvalidOperationException("Checkout changed variant stock.");
                Console.WriteLine($"Variant checkout iteration {iteration}/3 OK: isolated stock, required selection, variant and fallback prices, multi-plan={multiPlan}.");
            }
        }
        finally
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE i FROM Store.OrderItem i JOIN Store.[Order] o ON o.Id=i.IdCustomerOrder WHERE o.IdProfile={profile}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Store.[Order] WHERE IdProfile={profile}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Account.CheckoutPreference WHERE IdProfile={profile}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Account.Address WHERE IdProfile={profile}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Account.Profile WHERE Id={profile} AND Email={email}");
            if (setupCommitted)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Store.ProductVariantSalesPlanOffer WHERE IdProductVariant IN ({variantA},{variantB})");
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Store.ProductSalesPlanOffer WHERE IdProduct={product} AND EXISTS (SELECT 1 FROM Warehouse.Product WHERE Id={product} AND ProductKey={key})");
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.StockBalance WHERE IdInventoryItem IN ({itemA},{itemB})");
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.InventoryItem WHERE IdProduct={product} AND EXISTS (SELECT 1 FROM Warehouse.Product WHERE Id={product} AND ProductKey={key})");
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.ProductVariant WHERE IdProduct={product} AND EXISTS (SELECT 1 FROM Warehouse.Product WHERE Id={product} AND ProductKey={key})");
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.ProductUnit WHERE IdProduct={product} AND EXISTS (SELECT 1 FROM Warehouse.Product WHERE Id={product} AND ProductKey={key})");
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Warehouse.Product WHERE Id={product} AND ProductKey={key}");
            }
            await tx.CommitAsync();
        }
        int remaining = await db.Database.SqlQuery<int>($"SELECT (SELECT COUNT(*) FROM Warehouse.Product WHERE ProductKey={key}) + (SELECT COUNT(*) FROM Account.Profile WHERE Email={email}) AS [Value]").SingleAsync();
        if (remaining != 0) throw new InvalidOperationException("Variant checkout fixtures remain.");
    }

    private static AccountController Controller(ShopDbContext db, long profile, DatabaseSalesPlanSource? source = null,
        IProductPurchaseConfigurationSource? configurations = null)
    {
        source ??= new DatabaseSalesPlanSource(new StoreSalesPlanRepository(db));
        return new(db, source, configurations ?? source, new CheckoutMainTest.TestEnvironment()) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
        { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, profile.ToString())], "test")) } } };
    }

    private static async Task CheckRivalUpdatesAsync(string connection, long item, long variant, long plan, bool expectBlocked)
    {
        string[] updates =
        [
            "UPDATE Warehouse.StockBalance SET Quantity=Quantity WHERE IdInventoryItem=@item AND IdSalesPlanReference=@plan",
            "UPDATE Store.ProductVariantSalesPlanOffer SET PriceAmount=PriceAmount WHERE IdProductVariant=@variant AND IdSalesPlan=@plan",
            "UPDATE Warehouse.ProductVariant SET IsActive=IsActive WHERE Id=@variant"
        ];
        foreach (string sql in updates)
        {
            await using var rival = new SqlConnection(connection);
            await rival.OpenAsync();
            await using var transaction = await rival.BeginTransactionAsync();
            try
            {
                await using var command = rival.CreateCommand();
                command.Transaction = (SqlTransaction)transaction;
                command.CommandText = "SET LOCK_TIMEOUT 1200; " + sql;
                command.Parameters.AddWithValue("@item", item);
                command.Parameters.AddWithValue("@variant", variant);
                command.Parameters.AddWithValue("@plan", plan);
                int affected = await command.ExecuteNonQueryAsync();
                if (expectBlocked) throw new InvalidOperationException("A competing variant update was not blocked during checkout.");
                if (affected != 1) throw new InvalidOperationException("The competing variant update did not target exactly one row.");
            }
            catch (SqlException exception) when (expectBlocked && exception.Number == 1222) { }
            finally { await transaction.RollbackAsync(); }
        }
    }

    private static async Task CheckRivalInsertAsync(string connection, long variant, long plan, bool expectBlocked)
    {
        await using var rival = new SqlConnection(connection);
        await rival.OpenAsync();
        await using var transaction = await rival.BeginTransactionAsync();
        try
        {
            await using var command = rival.CreateCommand();
            command.Transaction = (SqlTransaction)transaction;
            command.CommandText = "SET LOCK_TIMEOUT 1200; INSERT Store.ProductVariantSalesPlanOffer (IdProductVariant,IdSalesPlan,PriceAmount,DiscountPercent,IsEnabled) VALUES (@variant,@plan,1234,0,1)";
            command.Parameters.AddWithValue("@variant", variant);
            command.Parameters.AddWithValue("@plan", plan);
            int affected = await command.ExecuteNonQueryAsync();
            if (expectBlocked) throw new InvalidOperationException("A new variant price was inserted during fallback checkout.");
            if (affected != 1) throw new InvalidOperationException("The competing variant price was not inserted.");
        }
        catch (SqlException exception) when (expectBlocked && exception.Number == 1222) { }
        finally { await transaction.RollbackAsync(); }
    }

    public sealed class OrderLine
    {
        public decimal TotalAmount { get; set; }
        public decimal UnitAmount { get; set; }
        public int Quantity { get; set; }
        public long IdProductVariant { get; set; }
        public long IdSalesPlan { get; set; }
    }
    public sealed class Plan
    {
        public long Id { get; set; }
        public string SalesPlanKey { get; set; } = "";
    }
}
