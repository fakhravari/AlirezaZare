namespace Shop.Data.Application.Account.Contracts;

public interface IAccountCheckoutData
{
    Task<CheckoutResult> QuoteCouponAsync(long profileId, string? couponCode, CancellationToken ct);
    Task<CheckoutResult> CheckoutAsync(long profileId, long addressId, string paymentMethod, string? couponCode, CancellationToken ct);
}

public enum CheckoutStatus
{
    Ok,
    BadRequest,
    NotFound,
    Conflict
}

public sealed record CheckoutResult(CheckoutStatus Status, object? Body)
{
    public static CheckoutResult Ok(object? body) => new(CheckoutStatus.Ok, body);
    public static CheckoutResult BadRequest(object? body) => new(CheckoutStatus.BadRequest, body);
    public static CheckoutResult NotFound(object? body) => new(CheckoutStatus.NotFound, body);
    public static CheckoutResult Conflict(object? body) => new(CheckoutStatus.Conflict, body);
}
