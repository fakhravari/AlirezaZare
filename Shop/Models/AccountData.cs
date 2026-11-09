using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shop.Models;

// Transport amounts are whole toman (IRT); dates are ISO calendar dates.
public sealed record AccountData(
    int SchemaVersion,
    string Currency,
    IReadOnlyList<AccountOrder> Orders,
    IReadOnlyList<AccountCoupon> Coupons)
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static AccountData Parse(string json)
    {
        var data = JsonSerializer.Deserialize<AccountData>(json, JsonOptions)
                   ?? throw new InvalidDataException("Missing account data.");
        if (data.SchemaVersion != 1 || data.Currency != "IRT" || data.Orders is null || data.Coupons is null)
            throw new InvalidDataException("Unsupported account schema or currency.");
        var orders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var numbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var order in data.Orders)
            if (order is null || string.IsNullOrWhiteSpace(order.Id) || !orders.Add(order.Id) ||
                order.Id != order.Id.Trim() ||
                string.IsNullOrWhiteSpace(order.Number) || !numbers.Add(order.Number) ||
                order.Number != order.Number.Trim() ||
                !AccountOrderStatus.IsKnown(order.Status) ||
                order.OrderedOn == default || order.ItemCount < 1 || order.TotalAmount < 0 ||
                order.TotalAmount != decimal.Truncate(order.TotalAmount))
                throw new InvalidDataException("Invalid or duplicate order.");
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var coupon in data.Coupons)
            if (coupon is null || string.IsNullOrWhiteSpace(coupon.Id) || !ids.Add(coupon.Id) ||
                string.IsNullOrWhiteSpace(coupon.Code) || !codes.Add(coupon.Code) ||
                coupon.Code.Any(c => !(c is >= 'A' and <= 'Z' or >= '0' and <= '9')) ||
                coupon.DiscountPercent is <= 0 or >= 100 || coupon.ExpiresOn == default ||
                coupon.UsageLimit < 1 || coupon.OrderIds is null ||
                coupon.OrderIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != coupon.OrderIds.Count ||
                coupon.OrderIds.Any(id => !orders.Contains(id)))
                throw new InvalidDataException("Invalid coupon or order reference.");
        return data;
    }
}

public sealed record AccountOrder(
    string Id,
    string Number,
    string Status,
    DateOnly OrderedOn,
    int ItemCount,
    decimal TotalAmount,
    string DeliveryDescription)
{
    [JsonIgnore] public string StatusLabel => AccountOrderStatus.Label(Status);

    [JsonIgnore] public string StatusIcon => AccountOrderStatus.Icon(Status);

    [JsonIgnore] public string StatusClass => $"is-{Status}";
    [JsonIgnore] public string DateLabel => AccountDisplay.Date(OrderedOn);
}

public static class AccountOrderStatus
{
    public const string PendingPayment = "pending-payment";
    public const string PaymentConfirmed = "payment-confirmed";
    public const string WarehouseConfirmed = "warehouse-confirmed";
    public const string InTransit = "in-transit";
    public const string ShippedToPost = "shipped-to-post";
    public const string Delivered = "delivered";
    public const string Cancelled = "cancelled";

    public static bool IsKnown(string status) => status is
        PendingPayment or PaymentConfirmed or WarehouseConfirmed or InTransit or
        ShippedToPost or Delivered or Cancelled;

    public static string Label(string status) => status switch
    {
        PendingPayment => "در انتظار پرداخت",
        PaymentConfirmed => "تایید پرداخت",
        WarehouseConfirmed => "تایید انبار",
        InTransit => "در حال ارسال",
        ShippedToPost => "ارسال شد به پست",
        Delivered => "تحویل‌شده",
        Cancelled => "لغوشده",
        _ => "نامشخص"
    };

    public static string Icon(string status) => status switch
    {
        PendingPayment => "bi-hourglass-split",
        PaymentConfirmed => "bi-credit-card",
        WarehouseConfirmed => "bi-box-seam",
        InTransit or ShippedToPost => "bi-truck",
        Delivered => "bi-check-circle-fill",
        Cancelled => "bi-x-circle-fill",
        _ => "bi-question-circle"
    };
}

public sealed record AccountCoupon(
    string Id,
    string Code,
    int DiscountPercent,
    bool Enabled,
    DateOnly ExpiresOn,
    int UsageLimit,
    IReadOnlyList<string> OrderIds)
{
    public bool IsActive(DateOnly today) => Enabled && ExpiresOn >= today;
}

public static class AccountDisplay
{
    public static string Date(DateOnly date) => date.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("fa-IR"));
}