namespace Shop.Data.Application.Store.Contracts;

public interface IPaymentGateway
{
    Task<PaymentGatewayRequestResult> RequestAsync(Guid attemptId, decimal amountToman,
        CancellationToken cancellationToken);

    Task<PaymentGatewayVerification> VerifyAsync(string gatewayReference, decimal amountToman,
        IReadOnlyDictionary<string, IReadOnlyList<string>> callback, CancellationToken cancellationToken);
}

public sealed record PaymentGatewayRequestResult(bool Available, string? GatewayReference, Uri? RedirectUri);

public sealed record PaymentGatewayVerification(bool Verified, decimal AmountToman, string? GatewayReference,
    string? ReceiptReference, DateTimeOffset? PaidAtUtc, bool DefinitivelyFailed = false);

public sealed record PaymentStartResult(string Status, Uri? RedirectUri = null);
