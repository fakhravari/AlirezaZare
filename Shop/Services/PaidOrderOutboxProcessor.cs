using System.Data;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Infrastructure.Persistence;

namespace Shop.Services;

public sealed class PaidOrderOutboxProcessor(ShopDbContext db)
{
    public async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var pending = await db.Database.SqlQueryRaw<PendingOrder>("""
            SELECT TOP (1) outbox.[IdOrder] FROM [Store].[PaidOrderOutbox] outbox WITH (UPDLOCK, ROWLOCK)
            JOIN [Store].[PaymentAttempt] payment ON payment.[Id]=outbox.[IdPaymentAttempt]
            WHERE outbox.[ProcessedAtUtc] IS NULL ORDER BY payment.[PaidAtUtc], outbox.[IdOrder]
            """).ToListAsync(ct);
        if (pending.Count == 0) return false;
        long orderId = pending[0].IdOrder;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            DECLARE @OrderId bigint = {orderId};
            DECLARE @OrderKey nvarchar(128), @CustomerKey nvarchar(128), @PaymentAtUtc datetime2(3), @InboxId bigint;
            SELECT @OrderKey=o.[OrderNumber], @CustomerKey=CONVERT(nvarchar(128), o.[IdProfile]), @PaymentAtUtc=p.[PaidAtUtc]
            FROM [Store].[PaidOrderOutbox] outbox
            JOIN [Store].[Order] o ON o.[Id]=outbox.[IdOrder]
            JOIN [Store].[PaymentAttempt] p ON p.[Id]=outbox.[IdPaymentAttempt] AND p.[Status]=N'verified'
            WHERE outbox.[IdOrder]=@OrderId AND outbox.[ProcessedAtUtc] IS NULL;
            IF @OrderKey IS NULL THROW 51045, 'Verified payment for outbox order is missing.', 1;
            IF NOT EXISTS (SELECT 1 FROM [Store].[OrderItem] WHERE [IdCustomerOrder]=@OrderId)
                THROW 51045, 'Paid order has no items.', 1;
            IF EXISTS (
                SELECT 1 FROM [Store].[OrderItem] i
                LEFT JOIN [Warehouse].[InventoryItem] item ON item.[IdProduct]=i.[IdProduct]
                    AND (item.[IdProductVariant]=i.[IdProductVariant] OR (item.[IdProductVariant] IS NULL AND i.[IdProductVariant] IS NULL))
                LEFT JOIN [Warehouse].[Product] product ON product.[Id]=i.[IdProduct]
                WHERE i.[IdCustomerOrder]=@OrderId AND (item.[Id] IS NULL OR product.[IdBaseUnit] IS NULL))
                THROW 51045, 'Paid order inventory mapping is missing.', 1;
            IF EXISTS (SELECT 1 FROM [Warehouse].[PaidOrderInbox] WHERE [StoreOrderKey]=@OrderKey)
                THROW 51045, 'Paid order key already exists in warehouse inbox.', 1;
            INSERT [Warehouse].[PaidOrderInbox] ([StoreOrderKey], [CustomerKey], [PaymentAtUtc])
                VALUES (@OrderKey, @CustomerKey, @PaymentAtUtc);
            SET @InboxId=SCOPE_IDENTITY();
            INSERT [Warehouse].[PaidOrderInboxLine] ([IdPaidOrderInbox], [IdInventoryItem], [IdUnit], [Quantity], [ConversionFactor], [BaseQuantity], [IdSalesPlanReference])
                SELECT @InboxId, item.[Id], product.[IdBaseUnit], i.[Quantity], 1, i.[Quantity], i.[IdSalesPlan]
                FROM [Store].[OrderItem] i
                JOIN [Warehouse].[InventoryItem] item ON item.[IdProduct]=i.[IdProduct]
                    AND (item.[IdProductVariant]=i.[IdProductVariant] OR (item.[IdProductVariant] IS NULL AND i.[IdProductVariant] IS NULL))
                JOIN [Warehouse].[Product] product ON product.[Id]=i.[IdProduct]
                WHERE i.[IdCustomerOrder]=@OrderId;
            UPDATE [Store].[PaidOrderOutbox] SET [ProcessedAtUtc]=SYSUTCDATETIME() WHERE [IdOrder]=@OrderId;
            """, ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    public sealed class PendingOrder { public long IdOrder { get; set; } }
}

public sealed class PaidOrderOutboxWorker(IServiceScopeFactory scopes, ILogger<PaidOrderOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                if (await scope.ServiceProvider.GetRequiredService<PaidOrderOutboxProcessor>().ProcessNextAsync(stoppingToken))
                    continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Could not process the paid order outbox"); }
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
