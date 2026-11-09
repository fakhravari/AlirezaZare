using System.Data;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Infrastructure.Persistence;

namespace Shop.Services;

public sealed class PaymentService(ShopDbContext db, IPaymentGateway gateway, ILogger<PaymentService> logger)
{
    public async Task<PaymentStartResult> StartAsync(long orderId, long profileId, CancellationToken ct)
    {
        if (orderId < 1 || profileId < 1) return new("not-found");
        Guid attemptId = Guid.NewGuid();
        decimal amount;
        await using (var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct))
        {
            var order = await db.Database.SqlQueryRaw<PaymentOrder>(
                "SELECT [Id], [TotalAmount], [Status], [PaymentMethod] FROM [Store].[Order] WITH (UPDLOCK, HOLDLOCK) WHERE [Id]={0} AND [IdProfile]={1}", orderId, profileId).SingleOrDefaultAsync(ct);
            if (order is null) return new("not-found");
            if (order.Status != "pending-payment" || order.TotalAmount <= 0 || order.PaymentMethod != "zarinpal") return new("invalid-order");
            int items = await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM [Store].[OrderItem] WHERE [IdCustomerOrder]={0}", orderId).SingleAsync(ct);
            if (items == 0) return new("invalid-order");
            int active = await db.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*) AS [Value] FROM [Store].[PaymentAttempt] WHERE [IdOrder]={0} AND [Status] IN (N'initiated', N'awaiting')", orderId).SingleAsync(ct);
            if (active != 0) return new("already-started");
            amount = order.TotalAmount;
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT [Store].[PaymentAttempt] ([Id], [IdOrder], [Amount], [Status]) VALUES ({attemptId}, {orderId}, {amount}, N'initiated')", ct);
            await transaction.CommitAsync(ct);
        }

        PaymentGatewayRequestResult requested;
        try { requested = await gateway.RequestAsync(attemptId, amount, ct); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Payment request outcome is unknown for attempt {AttemptId}", attemptId); return new("request-unknown"); }
        if (!requested.Available)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Store].[PaymentAttempt] SET [Status]=N'unavailable', [UpdatedAtUtc]=SYSUTCDATETIME() WHERE [Id]={attemptId} AND [Status]=N'initiated'", ct);
            return new("unavailable");
        }
        if (string.IsNullOrWhiteSpace(requested.GatewayReference) || requested.GatewayReference.Length > 200 ||
            requested.RedirectUri is not { IsAbsoluteUri: true, Scheme: "https" } redirect || !string.IsNullOrEmpty(redirect.UserInfo))
            return new("request-unknown");
        int updated = await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Store].[PaymentAttempt] SET [Status]=N'awaiting', [GatewayReference]={requested.GatewayReference}, [UpdatedAtUtc]=SYSUTCDATETIME() WHERE [Id]={attemptId} AND [Status]=N'initiated'", ct);
        return updated == 1 ? new("redirect", redirect) : new("request-unknown");
    }

    public async Task<string> VerifyAsync(Guid attemptId, IQueryCollection callback, CancellationToken ct)
    {
        var attempt = await db.Database.SqlQueryRaw<PaymentAttemptRow>(
            "SELECT [Id], [IdOrder], [Amount], [Status], [GatewayReference] FROM [Store].[PaymentAttempt] WHERE [Id]={0}", attemptId).SingleOrDefaultAsync(ct);
        if (attempt is null) return "not-found";
        if (attempt.Status == "verified") return "verified";
        if (attempt.Status != "awaiting" || string.IsNullOrEmpty(attempt.GatewayReference)) return "not-ready";
        PaymentGatewayVerification verification;
        try { verification = await gateway.VerifyAsync(attempt.GatewayReference, attempt.Amount, callback, ct); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Payment verification failed for attempt {AttemptId}", attemptId); return "unverified"; }
        if (PaymentVerificationRules.IsDefinitiveFailure(attempt.GatewayReference, verification))
        {
            int failed = await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Store].[PaymentAttempt] SET [Status]=N'failed', [UpdatedAtUtc]=SYSUTCDATETIME() WHERE [Id]={attemptId} AND [Status]=N'awaiting'", ct);
            return failed == 1 ? "failed" : "not-ready";
        }
        if (!PaymentVerificationRules.Matches(attempt.Amount, attempt.GatewayReference, verification)) return "unverified";

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var locked = await db.Database.SqlQueryRaw<PaymentAttemptRow>(
            "SELECT [Id], [IdOrder], [Amount], [Status], [GatewayReference] FROM [Store].[PaymentAttempt] WITH (UPDLOCK, HOLDLOCK) WHERE [Id]={0}", attemptId).SingleAsync(ct);
        if (locked.Status == "verified") return "verified";
        if (locked.Status != "awaiting" || !PaymentVerificationRules.Matches(locked.Amount, locked.GatewayReference, verification)) return "not-ready";
        int changed = await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Store].[Order] SET [Status]=N'payment-confirmed' WHERE [Id]={locked.IdOrder} AND [Status]=N'pending-payment' AND [TotalAmount]={locked.Amount}", ct);
        if (changed != 1) return "order-conflict";
        DateTime paidAtUtc = verification.PaidAtUtc!.Value.UtcDateTime;
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Store].[PaymentAttempt] SET [Status]=N'verified', [ReceiptReference]={verification.ReceiptReference}, [PaidAtUtc]={paidAtUtc}, [UpdatedAtUtc]=SYSUTCDATETIME() WHERE [Id]={attemptId}", ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT [Store].[PaidOrderOutbox] ([IdOrder], [IdPaymentAttempt]) VALUES ({locked.IdOrder}, {attemptId})", ct);
        await transaction.CommitAsync(ct);
        return "verified";
    }

    public sealed class PaymentOrder { public long Id { get; set; } public decimal TotalAmount { get; set; } public string Status { get; set; } = ""; public string PaymentMethod { get; set; } = ""; }
    public sealed class PaymentAttemptRow { public Guid Id { get; set; } public long IdOrder { get; set; } public decimal Amount { get; set; } public string Status { get; set; } = ""; public string? GatewayReference { get; set; } }
}

public sealed record PaymentStartResult(string Status, Uri? RedirectUri = null);

public static class PaymentVerificationRules
{
    public static bool IsDefinitiveFailure(string? reference, PaymentGatewayVerification verification) =>
        !verification.Verified && verification.DefinitivelyFailed && !string.IsNullOrWhiteSpace(reference) &&
        string.Equals(reference, verification.GatewayReference, StringComparison.Ordinal);

    public static bool Matches(decimal amount, string? reference, PaymentGatewayVerification verification) =>
        verification.Verified && !verification.DefinitivelyFailed && amount > 0 && verification.AmountToman == amount &&
        !string.IsNullOrWhiteSpace(reference) && string.Equals(reference, verification.GatewayReference, StringComparison.Ordinal) &&
        !string.IsNullOrWhiteSpace(verification.ReceiptReference) && verification.ReceiptReference.Length <= 200 &&
        verification.PaidAtUtc is { } paidAt && paidAt <= DateTimeOffset.UtcNow.AddMinutes(5) && paidAt > DateTimeOffset.UnixEpoch;
}
