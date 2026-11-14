using Shop.Data.Application.Store.Contracts;

namespace Shop.Services;

public sealed class UnconfiguredPaymentGateway : IPaymentGateway
{
    public Task<PaymentGatewayRequestResult> RequestAsync(Guid attemptId, decimal amountToman,
        CancellationToken cancellationToken)
    {
        // TODO: Implement the selected bank's server-to-server payment request and callback URL here.
        return Task.FromResult(new PaymentGatewayRequestResult(false, null, null));
    }

    public Task<PaymentGatewayVerification> VerifyAsync(string gatewayReference, decimal amountToman,
        IReadOnlyDictionary<string, IReadOnlyList<string>> callback, CancellationToken cancellationToken)
    {
        // TODO: Verify the bank response server-to-server; never trust callback parameters as proof of payment.
        return Task.FromResult(new PaymentGatewayVerification(false, 0, null, null, null));
    }
}
