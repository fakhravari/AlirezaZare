using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Payments;

public sealed class PaymentService(ShopDbContext db, IPaymentGateway gateway, ILogger<PaymentService> logger)
{
    public async Task<PaymentStartResult> StartAsync(long orderId, long profileId, CancellationToken ct)
    {
        if (orderId < 1 || profileId < 1) return new("not-found");

        decimal attemptId;
        decimal amount;
        StorePaymentAttempt paymentAttempt;
        await using (var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct))
        {
            var order = await db.StoreCustomerOrders.Include(x => x.Bank).Include(x => x.StatusReference).SingleOrDefaultAsync(x => x.Id == orderId && x.IdProfile == profileId, ct);
            if (order is null) return new("not-found");

            if (order.StatusReference.Code != "pending-payment" || order.TotalAmount <= 0 || order.PaymentMethod != "zarinpal" ||
                order.Bank is not { Code: "zarinpal", IsActive: true }) return new("invalid-order");

            bool hasItems = await db.StoreCustomerOrderItems.AnyAsync(x => x.IdOrder == orderId, ct);
            if (!hasItems) return new("invalid-order");

            decimal itemSubtotal = await (from item in db.StoreCustomerOrderItems
                                          join price in db.StoreOrderItemAmounts on item.Id equals price.IdOrderItem
                                          where item.IdOrder == orderId
                                          select price.UnitAmount * item.Quantity).SumAsync(ct);
            if (itemSubtotal != order.SubtotalAmount) return new("amount-mismatch");

            bool active = await db.StorePaymentAttempts.AnyAsync(x => x.IdOrder == orderId && (x.StatusReference.Code == "initiated" || x.StatusReference.Code == "awaiting"), ct);
            if (active) return new("already-started");

            amount = order.TotalAmount;
            DateTime now = DateTime.UtcNow;
            paymentAttempt = new StorePaymentAttempt
            {
                IdOrder = orderId,
                Amount = amount,
                IdStatus = await db.PaymentAttemptStatuses.Where(x => x.Code == "initiated").Select(x => x.Id).SingleAsync(ct),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            db.StorePaymentAttempts.Add(paymentAttempt);
            await db.SaveChangesAsync(ct);
            attemptId = paymentAttempt.Id;

            await transaction.CommitAsync(ct);
        }

        PaymentGatewayRequestResult requested;
        try { requested = await gateway.RequestAsync(attemptId, amount, ct); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Payment request outcome is unknown for attempt {AttemptId}", attemptId); return new("request-unknown"); }
        if (!requested.Available)
        {
            int unavailableId = await db.PaymentAttemptStatuses.Where(x => x.Code == "unavailable").Select(x => x.Id).SingleAsync(ct);
            await db.StorePaymentAttempts.Where(x => x.Id == attemptId && x.StatusReference.Code == "initiated")
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.IdStatus, unavailableId).SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), ct);

            return new("unavailable");
        }
        if (string.IsNullOrWhiteSpace(requested.GatewayReference) || requested.GatewayReference.Length > 200 ||
            requested.RedirectUri is not { IsAbsoluteUri: true, Scheme: "https" } redirect || !string.IsNullOrEmpty(redirect.UserInfo))
            return new("request-unknown");

        int awaitingId = await db.PaymentAttemptStatuses.Where(x => x.Code == "awaiting").Select(x => x.Id).SingleAsync(ct);
        int updated = await db.StorePaymentAttempts.Where(x => x.Id == attemptId && x.StatusReference.Code == "initiated")
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.IdStatus, awaitingId).SetProperty(x => x.GatewayReference, requested.GatewayReference).SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), ct);

        return updated == 1 ? new("redirect", redirect) : new("request-unknown");
    }

    public async Task<string> VerifyAsync(decimal attemptId,
        IReadOnlyDictionary<string, IReadOnlyList<string>> callback, CancellationToken ct)
    {
        var attempt = await db.StorePaymentAttempts.AsNoTracking().Where(x => x.Id == attemptId).Select(x => new PaymentAttemptRow
            { Id = x.Id, IdOrder = x.IdOrder, Amount = x.Amount, Status = x.StatusReference.Code, GatewayReference = x.GatewayReference }).SingleOrDefaultAsync(ct);
        if (attempt is null) return "not-found";

        if (attempt.Status == "verified") return "verified";

        if (attempt.Status != "awaiting" || string.IsNullOrEmpty(attempt.GatewayReference)) return "not-ready";

        PaymentGatewayVerification verification;
        try { verification = await gateway.VerifyAsync(attempt.GatewayReference, attempt.Amount, callback, ct); }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { logger.LogError(exception, "Payment verification failed for attempt {AttemptId}", attemptId); return "unverified"; }
        if (PaymentVerificationRules.IsDefinitiveFailure(attempt.GatewayReference, verification))
        {
            int failedId = await db.PaymentAttemptStatuses.Where(x => x.Code == "failed").Select(x => x.Id).SingleAsync(ct);
            int failed = await db.StorePaymentAttempts.Where(x => x.Id == attemptId && x.StatusReference.Code == "awaiting")
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.IdStatus, failedId).SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), ct);

            return failed == 1 ? "failed" : "not-ready";
        }
        if (!PaymentVerificationRules.Matches(attempt.Amount, attempt.GatewayReference, verification)) return "unverified";

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var locked = await db.StorePaymentAttempts.Include(x => x.StatusReference).SingleAsync(x => x.Id == attemptId, ct);
        if (locked.StatusReference.Code == "verified") return "verified";

        if (locked.StatusReference.Code != "awaiting" || !PaymentVerificationRules.Matches(locked.Amount, locked.GatewayReference, verification)) return "not-ready";

        var order = await db.StoreCustomerOrders.Include(x => x.StatusReference).SingleOrDefaultAsync(x => x.Id == locked.IdOrder, ct);
        if (order is null || order.StatusReference.Code != "pending-payment" || order.TotalAmount != locked.Amount) return "order-conflict";

        var items = await db.StoreCustomerOrderItems.Where(x => x.IdOrder == order.Id).ToListAsync(ct);
        if (items.Count == 0) return "order-conflict";

        DateTime now = DateTime.UtcNow;
        DateTime paidAtUtc = verification.PaidAtUtc!.Value.UtcDateTime;
        order.IdStatus = await db.OrderStatuses.Where(x => x.Code == "payment-confirmed").Select(x => x.Id).SingleAsync(ct);
        locked.IdStatus = await db.PaymentAttemptStatuses.Where(x => x.Code == "verified").Select(x => x.Id).SingleAsync(ct);
        locked.ReceiptReference = verification.ReceiptReference;
        locked.PaidAtUtc = paidAtUtc;
        locked.UpdatedAtUtc = now;
        await db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return "verified";
    }

    public sealed class PaymentAttemptRow { public decimal Id { get; set; } public long IdOrder { get; set; } public decimal Amount { get; set; } public string Status { get; set; } = ""; public string? GatewayReference { get; set; } }
}

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
