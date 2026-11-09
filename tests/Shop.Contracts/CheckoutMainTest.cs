using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Shop.Controllers;
using Shop.Database.Infrastructure.Persistence;
using Shop.Database.Infrastructure.Persistence.Repositories;
using Shop.Models;
using Shop.Services;

internal static class CheckoutMainTest
{
    public static async Task RunAsync(bool race)
    {
        string connection = Environment.GetEnvironmentVariable("SHOP_TEST_CONNECTION")
            ?? throw new InvalidOperationException("SHOP_TEST_CONNECTION is required.");
        var options = new DbContextOptionsBuilder<ShopDbContext>().UseSqlServer(connection).Options;
        await using var db = new ShopDbContext(options);
        var selector = new DatabaseSalesPlanSource(new StoreSalesPlanRepository(db));
        SalesPlanCatalog catalog = await selector.ReadAsync();
        ProductSalesOffer? selected = null;
        ProductPurchaseConfiguration? selectedConfiguration = null;
        foreach (var offer in catalog.Offers.Where(x => x.IsEnabled && x.StockQuantity >= 2 && x.MaxPurchaseQuantity >= 2 &&
                     (!race || catalog.Plans.Any(p => p.Id == x.SalesPlanId && p.IsDefault)) &&
                     !catalog.InventoryVariants.Any(v => v.ProductId == x.ProductId)))
        {
            try { _ = catalog.Quote(offer.ProductId, offer.SalesPlanId, 2, DateTimeOffset.UtcNow); }
            catch (SalesPlanUnavailableException) { continue; }
            var config = await selector.ReadAsync(offer.ProductId);
            if (config is not null && config.MaxPurchaseQuantity >= 2 && config.Options.Count > 0 && (!race || config.Accessories.Any(x => x.IsRequired)) &&
                config.Options.All(x => x.Values.Count > 0) && config.Accessories.Where(x => x.IsRequired)
                    .All(x => offer.AccessoryPriceAmounts.ContainsKey(x.Key) && (offer.AccessoryIds is null || offer.AccessoryIds.Contains(x.Key))))
            { selected = offer; selectedConfiguration = config; break; }
        }
        if (selected is null || selectedConfiguration is null) throw new InvalidOperationException("No in-stock simple product with valid price options was found.");
        decimal expectedAmount = catalog.Quote(selected.ProductId, selected.SalesPlanId, 2, DateTimeOffset.UtcNow).UnitAmount
            + selectedConfiguration.Options.Sum(x => x.Values[0].PriceDelta)
            + selectedConfiguration.Accessories.Where(x => x.IsRequired).Sum(x => selected.AccessoryPriceAmounts[x.Key]);
        long profile = 0;
        string email = $"checkout-test-{Guid.NewGuid():N}@example.invalid";
        try
        {
            profile = (await db.Database.SqlQuery<long>($"INSERT Account.Profile (Email,FirstName,LastName) OUTPUT INSERTED.Id AS [Value] VALUES ({email},N'Test',N'Checkout')").ToListAsync()).Single();
            long address = (await db.Database.SqlQuery<long>($"""
                INSERT Account.Address (IdProfile,Title,Recipient,Phone,IdCountry,IdProvince,IdCity,Street,PostalCode)
                OUTPUT INSERTED.Id AS [Value]
                SELECT TOP (1) {profile},N'Test',N'Test',N'0000000000',IdCountry,IdProvince,Id,N'Test',N'0000000000' FROM Geo.City ORDER BY Id
                """).ToListAsync()).Single();
            for (int iteration = 1; iteration <= 3; iteration++)
            {
                await using var requestDb = new ShopDbContext(options);
                var source = new DatabaseSalesPlanSource(new StoreSalesPlanRepository(requestDb));
                var controller = new AccountController(requestDb, source, source, new TestEnvironment())
                {
                    ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User =
                        new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, profile.ToString())], "test")) } }
                };
                var invalidOptions = JsonDocument.Parse("{\"priceOptions\":[{\"key\":\"unknown\",\"productValue\":\"unknown\"}]}").RootElement.Clone();
                foreach (string kind in new[] { "favorites", "compare" })
                {
                    if (await controller.SaveList(kind, new([selected.ProductId]), CancellationToken.None) is not OkObjectResult ||
                        await controller.SaveList(kind, new([selected.ProductId, int.MaxValue]), CancellationToken.None) is not ConflictObjectResult)
                        throw new InvalidOperationException("Unavailable product list replacement was not rejected.");
                    int retainedList = kind == "favorites"
                        ? await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM Account.Favorite WHERE IdProfile={profile} AND IdProduct={selected.ProductId}").SingleAsync()
                        : await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM Account.CompareItem WHERE IdProfile={profile} AND IdProduct={selected.ProductId}").SingleAsync();
                    if (retainedList != 1) throw new InvalidOperationException("Invalid replacement changed the previous product list.");
                }
                var alertOptions = JsonDocument.Parse("{\"color\":\"red\"}").RootElement.Clone();
                var alert = new AccountController.PriceAlertRequest(selected.ProductId.ToString(), "variant", alertOptions, 100, "in-stock", DateTimeOffset.UtcNow);
                if (await controller.SavePriceAlerts(new([alert]), CancellationToken.None) is not OkObjectResult ||
                    await controller.SavePriceAlerts(new([alert with { ProductId = int.MaxValue.ToString() }]), CancellationToken.None) is not ConflictObjectResult)
                    throw new InvalidOperationException("Unavailable product alert replacement was not rejected.");
                string? storedAlert = await db.Database.SqlQuery<string>($"SELECT VariantsJson AS [Value] FROM Account.PriceAlert WHERE IdProfile={profile} AND IdProduct={selected.ProductId}").SingleOrDefaultAsync();
                if (storedAlert is null || JsonDocument.Parse(storedAlert).RootElement.GetProperty("color").GetString() != "red")
                    throw new InvalidOperationException("Alert options were lost or invalid replacement changed the previous alert.");
                if (await controller.SaveCart(new([new(selected.ProductId, 1, selected.SalesPlanId, null)]), CancellationToken.None) is not OkObjectResult)
                    throw new InvalidOperationException("Valid cart was not saved.");
                if (await controller.SaveCart(new([new(int.MaxValue, 1, selected.SalesPlanId, null)]), CancellationToken.None) is not ConflictObjectResult)
                    throw new InvalidOperationException("An unavailable product did not return a cart conflict.");
                int retained = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM Account.CartItem i JOIN Account.Cart c ON c.Id=i.IdCart WHERE c.IdProfile={profile} AND i.IdProduct={selected.ProductId} AND i.Quantity=1").SingleAsync();
                if (retained != 1) throw new InvalidOperationException("An invalid cart replacement changed the previous cart.");
                var invalid = new AccountController.CheckoutRequest(address, "zarinpal", [new(selected.ProductId, 2, selected.SalesPlanId, invalidOptions, ExpectedUnitAmount: expectedAmount)]);
                if (await controller.Checkout(invalid, CancellationToken.None) is not ConflictObjectResult)
                    throw new InvalidOperationException("Checkout accepted an option not defined for the product.");
                int countBefore = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM Store.[Order] WHERE IdProfile={profile}").SingleAsync();
                if (countBefore != iteration - 1) throw new InvalidOperationException("Rejected checkout created an order.");

                // A fresh request source ensures that prices are read inside checkout's own transaction.
                await using var validDb = new ShopDbContext(options);
                var validSource = new DatabaseSalesPlanSource(new StoreSalesPlanRepository(validDb));
                var gate = race ? new GatedSource(validSource) : null;
                var validController = new AccountController(validDb, validSource, gate ?? (IProductPurchaseConfigurationSource)validSource, new TestEnvironment()) { ControllerContext = controller.ControllerContext };
                var spoofed = JsonSerializer.SerializeToElement(new
                {
                    priceOptions = selectedConfiguration.Options.Select(x => new { key = x.Key, productValue = x.Values[0].Key, priceDelta = -999999 }),
                    accessories = selectedConfiguration.Accessories.Where(x => x.IsRequired).Select(x => new { id = x.Key, price = 1 }),
                    price = 1, unitAmount = 1
                });
                await using (var staleDb = new ShopDbContext(options))
                {
                    var staleSource = new DatabaseSalesPlanSource(new StoreSalesPlanRepository(staleDb));
                    var staleController = new AccountController(staleDb, staleSource, staleSource, new TestEnvironment()) { ControllerContext = controller.ControllerContext };
                    if (await staleController.Checkout(new(address, "zarinpal", [new(selected.ProductId, 2, selected.SalesPlanId, spoofed, ExpectedUnitAmount: expectedAmount + 1)]), CancellationToken.None) is not ConflictObjectResult)
                        throw new InvalidOperationException("Checkout accepted a stale expected price.");
                }
                int afterStale = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM Store.[Order] WHERE IdProfile={profile}").SingleAsync();
                if (afterStale != countBefore) throw new InvalidOperationException("Stale price created an order.");
                var checkoutTask = validController.Checkout(new(address, "zarinpal", [new(selected.ProductId, 2, selected.SalesPlanId, spoofed, ExpectedUnitAmount: expectedAmount)]), CancellationToken.None);
                if (gate is not null)
                {
                    try
                    {
                        await gate.ReadFinished.Task.WaitAsync(TimeSpan.FromSeconds(30));
                        if (checkoutTask.IsCompleted) throw new InvalidOperationException("Checkout did not pause inside its transaction.");
                        await CheckRivalUpdatesAsync(connection, selected.ProductId, selected.SalesPlanId, expectBlocked: true);
                    }
                    catch
                    {
                        gate.Release.TrySetResult();
                        try { await checkoutTask.WaitAsync(TimeSpan.FromSeconds(30)); } catch { }
                        throw;
                    }
                    finally { gate.Release.TrySetResult(); }
                }
                var result = await checkoutTask.WaitAsync(TimeSpan.FromSeconds(30)) as OkObjectResult
                    ?? throw new InvalidOperationException("Valid checkout failed.");
                if (gate is not null) await CheckRivalUpdatesAsync(connection, selected.ProductId, selected.SalesPlanId, expectBlocked: false);
                var resultJson = JsonSerializer.SerializeToElement(result.Value);
                long order = resultJson.GetProperty("id").GetInt64();
                var afterCatalog = await validSource.ReadAsync();
                var currentOffer = afterCatalog.Offers.Single(x => x.ProductId == selected.ProductId && x.SalesPlanId == selected.SalesPlanId);
                var currentConfiguration = await validSource.ReadAsync(selected.ProductId) ?? throw new InvalidOperationException("Product configuration disappeared.");
                decimal expectedUnit = afterCatalog.Quote(selected.ProductId, selected.SalesPlanId, 2, DateTimeOffset.UtcNow).UnitAmount
                    + currentConfiguration.Options.Sum(x => x.Values.Single(v => v.Key == selectedConfiguration.Options.Single(o => o.Key == x.Key).Values[0].Key).PriceDelta)
                    + currentConfiguration.Accessories.Where(x => x.IsRequired).Sum(x => currentOffer.AccessoryPriceAmounts[x.Key]);
                var saved = (await db.Database.SqlQuery<OrderData>($"SELECT o.TotalAmount, o.ItemCount, o.Status, i.UnitAmount, i.Quantity FROM Store.[Order] o JOIN Store.OrderItem i ON i.IdCustomerOrder=o.Id WHERE o.Id={order} AND o.IdProfile={profile}").ToListAsync()).Single();
                if (saved.Status != "pending-payment" || saved.Quantity != 2 || saved.ItemCount != 2 || saved.UnitAmount != expectedUnit || saved.TotalAmount != expectedUnit * 2 || resultJson.GetProperty("totalAmount").GetDecimal() != saved.TotalAmount)
                    throw new InvalidOperationException("Checkout price, quantity or status differs from the authoritative quote.");
                Console.WriteLine($"Checkout iteration {iteration}/3 OK: invalid option rejected, submitted price ignored, stored order matches server quote; options={selectedConfiguration.Options.Count}, required accessories={selectedConfiguration.Accessories.Count(x => x.IsRequired)}.");
            }
        }
        finally
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE i FROM Store.OrderItem i JOIN Store.[Order] o ON o.Id=i.IdCustomerOrder WHERE o.IdProfile={profile}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Store.[Order] WHERE IdProfile={profile}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Account.CheckoutPreference WHERE IdProfile={profile}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE i FROM Account.CartItem i JOIN Account.Cart c ON c.Id=i.IdCart WHERE c.IdProfile={profile}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Account.Cart WHERE IdProfile={profile}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Account.Favorite WHERE IdProfile={profile}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Account.CompareItem WHERE IdProfile={profile}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Account.PriceAlert WHERE IdProfile={profile}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Account.Address WHERE IdProfile={profile}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Account.Profile WHERE Id={profile} AND Email={email}");
            await tx.CommitAsync();
        }
        int remaining = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM Account.Profile WHERE Email={email}").SingleAsync();
        if (remaining != 0) throw new InvalidOperationException("Temporary checkout profile remains.");
    }

    public sealed class OrderData
    {
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public string Status { get; set; } = "";
        public decimal UnitAmount { get; set; }
        public int Quantity { get; set; }
    }

    private static async Task CheckRivalUpdatesAsync(string connection, int productId, string planKey, bool expectBlocked)
    {
        string[] updates =
        [
            "UPDATE Warehouse.Product SET PriceAmount=PriceAmount WHERE Id=@product",
            "UPDATE b SET Quantity=b.Quantity FROM Warehouse.StockBalance b JOIN Store.SiteSetting s ON s.IdWarehouse=b.IdWarehouse JOIN Warehouse.InventoryItem i ON i.Id=b.IdInventoryItem JOIN Warehouse.SalesPlanReference p ON p.Id=b.IdSalesPlanReference WHERE i.IdProduct=@product AND i.IdProductVariant IS NULL AND p.SalesPlanKey=@plan",
            "UPDATE v SET PriceDelta=v.PriceDelta FROM Store.ProductPurchaseOptionValue v JOIN Store.ProductPurchaseOption o ON o.Id=v.IdProductPurchaseOption WHERE o.IdProduct=@product",
            "UPDATE Store.ProductAccessory SET PriceAmount=PriceAmount WHERE IdProduct=@product AND IsRequired=1"
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
                command.Parameters.AddWithValue("@product", productId);
                command.Parameters.AddWithValue("@plan", planKey);
                int rows = await command.ExecuteNonQueryAsync();
                if (expectBlocked) throw new InvalidOperationException("A competing price or stock update succeeded while checkout held its snapshot.");
                if (rows < 1) throw new InvalidOperationException("The competing update did not target any row.");
            }
            catch (SqlException exception) when (expectBlocked && exception.Number == 1222) { }
            finally { await transaction.RollbackAsync(); }
        }
    }

    internal sealed class GatedSource(DatabaseSalesPlanSource source) : IProductPurchaseConfigurationSource
    {
        public TaskCompletionSource ReadFinished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<ProductPurchaseConfiguration?> ReadAsync(int productId, CancellationToken cancellationToken = default)
        {
            var result = await source.ReadAsync(productId, cancellationToken);
            ReadFinished.TrySetResult();
            await Release.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            return result;
        }
    }

    internal sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "CheckoutTest";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public string EnvironmentName { get; set; } = "Production";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
