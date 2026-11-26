namespace Shop.Data.Application.Store.Contracts;

public interface IStoreCouponAdminData
{
    Task<CouponIndexData> ReadAsync(string query, string status, int page, DateOnly today,
        CancellationToken cancellationToken);
    Task<CouponReportData?> ReadReportAsync(decimal id, int page, CancellationToken cancellationToken);
    Task<CouponMutationResult> DeleteAsync(decimal id, CancellationToken cancellationToken);
    Task<CouponMutationResult> SaveAsync(long? id, string code, int discountPercent, int usageLimit,
        DateOnly expiresOn, bool enabled, string audience, string? customerEmail,
        CancellationToken cancellationToken);
}

public sealed record CouponRowData(decimal Id, string Code, int DiscountPercent, int UsageLimit, bool Enabled,
    DateOnly ExpiresOn, int Uses, decimal Discount, bool HasOrders, decimal? IdProfile, string? CustomerEmail);
public sealed record CouponIndexData(List<CouponRowData> Coupons, string Query, string Status, int Page, int Total,
    int Uses, decimal Discount, DateOnly Today);
public sealed record CouponOrderRowData(decimal Id, string OrderNumber, string Customer, string Status,
    DateTime CreatedAtUtc, decimal Subtotal, decimal Discount, decimal Total);
public sealed record CouponReportData(decimal Id, string Code, int UsageLimit, int Uses, decimal Subtotal,
    decimal Discount, decimal Total, int Page, List<CouponOrderRowData> Orders);
public enum CouponMutationResult { Saved, Deleted, NotFound, HasHistory, CustomerNotFound, DuplicateCode, UsedCode, UsageLimitTooLow }
