using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shop.Database.Infrastructure.Persistence;
using Shop.Services;

internal static class PaymentFlowTest
{
    public static async Task RunAsync()
    {
        string connection = Environment.GetEnvironmentVariable("SHOP_TEST_CONNECTION")
            ?? throw new InvalidOperationException("SHOP_TEST_CONNECTION is required.");
        if (!new SqlConnectionStringBuilder(connection).InitialCatalog.StartsWith("ShopVerify_", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A ShopVerify_ test database is required.");
        var options = new DbContextOptionsBuilder<ShopDbContext>().UseSqlServer(connection).Options;
        await using var db = new ShopDbContext(options);
        string key = $"PAY-{Guid.NewGuid():N}"[..28];
        long profile = 0, order = 0;
        try
        {
            profile = (await db.Database.SqlQuery<long>($"INSERT Account.Profile (Email,FirstName,LastName) OUTPUT INSERTED.Id AS [Value] VALUES ({key + "@example.invalid"},N'Test',N'Payment')").ToListAsync()).Single();
            order = (await db.Database.SqlQuery<long>($"INSERT Store.[Order] (IdProfile,OrderNumber,Status,PaymentMethod,ItemCount,TotalAmount,AddressSnapshotJson) OUTPUT INSERTED.Id AS [Value] VALUES ({profile},{key},N'pending-payment',N'zarinpal',1,250000,N'{{}}')").ToListAsync()).Single();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT Store.OrderItem (IdCustomerOrder,IdProduct,ProductNameSnapshot,Quantity,UnitAmount,OptionsJson,IdSalesPlan)
                SELECT TOP (1) {order},p.Id,N'Test',1,250000,CONCAT(NCHAR(123),NCHAR(125)),s.Id
                FROM Warehouse.Product p CROSS JOIN Warehouse.SalesPlanReference s ORDER BY p.Id,s.Id
                """);

            var service = new PaymentService(db, new UnconfiguredPaymentGateway(), NullLogger<PaymentService>.Instance);
            if ((await service.StartAsync(order, profile, default)).Status != "unavailable") throw new Exception("Unconfigured gateway must remain unavailable.");
            var gateway = new TestGateway();
            service = new PaymentService(db, gateway, NullLogger<PaymentService>.Instance);
            if ((await service.StartAsync(order, profile + 1000000000, default)).Status != "not-found") throw new Exception("Another customer could start payment.");
            if ((await service.StartAsync(order, profile, default)).Status != "redirect") throw new Exception("Payment start failed.");
            if ((await service.StartAsync(order, profile, default)).Status != "already-started") throw new Exception("Duplicate payment start accepted.");
            Guid attempt = gateway.AttemptId;
            gateway.Verification = new(true, 249999, gateway.Reference, "test-receipt-" + key, DateTimeOffset.UtcNow);
            if (await service.VerifyAsync(attempt, new QueryCollection(), default) != "unverified") throw new Exception("Incorrect amount accepted.");
            gateway.Verification = gateway.Verification with { AmountToman = 250000 };
            if (await service.VerifyAsync(attempt, new QueryCollection(), default) != "verified" ||
                await service.VerifyAsync(attempt, new QueryCollection(), default) != "verified") throw new Exception("Verification or repeated callback failed.");
            int saved = await db.Database.SqlQuery<int>($"""
                SELECT COUNT(*) AS [Value] FROM Store.[Order] o
                JOIN Store.PaymentAttempt p ON p.IdOrder=o.Id AND p.Status=N'verified'
                JOIN Store.PaidOrderOutbox q ON q.IdOrder=o.Id AND q.IdPaymentAttempt=p.Id
                WHERE o.Id={order} AND o.Status=N'payment-confirmed'
                """).SingleAsync();
            if (saved != 1) throw new Exception("Verified payment and outbox were not persisted exactly once.");
            Console.WriteLine("Payment flow OK: unavailable gateway, ownership, duplicate start, mismatch, verification, repeated callback and outbox.");
        }
        finally
        {
            if (order != 0)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Store.PaidOrderOutbox WHERE IdOrder={order}");
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Store.PaymentAttempt WHERE IdOrder={order}");
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Store.OrderItem WHERE IdCustomerOrder={order}");
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Store.[Order] WHERE Id={order} AND IdProfile={profile}");
            }
            if (profile != 0) await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Account.Profile WHERE Id={profile}");
        }
    }

    private sealed class TestGateway : IPaymentGateway
    {
        public Guid AttemptId { get; private set; }
        public string Reference { get; } = "test-gateway-" + Guid.NewGuid().ToString("N");
        public PaymentGatewayVerification Verification { get; set; } = new(false, 0, null, null, null);
        public Task<PaymentGatewayRequestResult> RequestAsync(Guid attemptId, decimal amountToman, CancellationToken cancellationToken)
        {
            AttemptId = attemptId;
            return Task.FromResult(new PaymentGatewayRequestResult(true, Reference, new Uri("https://example.invalid/payment")));
        }
        public Task<PaymentGatewayVerification> VerifyAsync(string gatewayReference, decimal amountToman, IQueryCollection callback, CancellationToken cancellationToken) => Task.FromResult(Verification);
    }
}
