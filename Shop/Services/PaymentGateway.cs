namespace Shop.Services;

public interface IPaymentGateway
{
    Task<PaymentGatewayRequestResult> RequestAsync(Guid attemptId, decimal amountToman, CancellationToken cancellationToken);
    Task<PaymentGatewayVerification> VerifyAsync(string gatewayReference, decimal amountToman,
        IQueryCollection callback, CancellationToken cancellationToken);
}

public sealed record PaymentGatewayRequestResult(bool Available, string? GatewayReference, Uri? RedirectUri);
public sealed record PaymentGatewayVerification(bool Verified, decimal AmountToman, string? GatewayReference, string? ReceiptReference, DateTimeOffset? PaidAtUtc, bool DefinitivelyFailed = false);

public sealed class UnconfiguredPaymentGateway : IPaymentGateway
{
    public Task<PaymentGatewayRequestResult> RequestAsync(Guid attemptId, decimal amountToman, CancellationToken cancellationToken)
    {
        // TODO: Implement the selected bank's server-to-server payment request and callback URL here.
        return Task.FromResult(new PaymentGatewayRequestResult(false, null, null));
    }

    public Task<PaymentGatewayVerification> VerifyAsync(string gatewayReference, decimal amountToman,
        IQueryCollection callback, CancellationToken cancellationToken)
    {
        // TODO: Verify the bank response server-to-server; never trust callback parameters as proof of payment.
        return Task.FromResult(new PaymentGatewayVerification(false, 0, null, null, null));
    }
}
