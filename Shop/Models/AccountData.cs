using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shop.Models;

// Transport amounts are whole toman (IRT); dates are ISO calendar dates.
public sealed record AccountData(int SchemaVersion, string Currency,
    IReadOnlyList<AccountOrder> Orders, IReadOnlyList<AccountCoupon> Coupons)
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static AccountData Parse(string json)
    {
        var data = JsonSerializer.Deserialize<AccountData>(json, JsonOptions)
            ?? throw new InvalidDataException("Missing account data.");
        if (data.SchemaVersion != 1 || data.Currency != "IRT" || data.Orders is null || data.Coupons is null)
            throw new InvalidDataException("Unsupported account schema or currency.");
        var orders = new HashSet<string>(StringComparer.Ordinal);
        var numbers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var order in data.Orders)
            if (order is null || string.IsNullOrWhiteSpace(order.Id) || !orders.Add(order.Id) ||
                string.IsNullOrWhiteSpace(order.Number) || !numbers.Add(order.Number) ||
                order.Status is not ("shipping" or "processing" or "delivered" or "cancelled") ||
                order.OrderedOn == default || order.ItemCount < 1 || order.TotalAmount < 0 ||
                order.TotalAmount != decimal.Truncate(order.TotalAmount))
                throw new InvalidDataException("Invalid or duplicate order.");
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var coupon in data.Coupons)
            if (coupon is null || string.IsNullOrWhiteSpace(coupon.Id) || !ids.Add(coupon.Id) ||
                string.IsNullOrWhiteSpace(coupon.Code) || !codes.Add(coupon.Code) ||
                coupon.Code.Any(c => !(c is >= 'A' and <= 'Z' or >= '0' and <= '9')) ||
                coupon.DiscountPercent is <= 0 or >= 100 || coupon.ExpiresOn == default ||
                coupon.UsageLimit < 1 || coupon.OrderIds is null ||
                coupon.OrderIds.Distinct().Count() != coupon.OrderIds.Count ||
                coupon.OrderIds.Any(id => !orders.Contains(id)))
                throw new InvalidDataException("Invalid coupon or order reference.");
        return data;
    }
}

public sealed record AccountOrder(string Id, string Number, string Status, DateOnly OrderedOn,
    int ItemCount, decimal TotalAmount, string DeliveryDescription)
{
    [JsonIgnore] public string StatusLabel => Status switch
    {
        "shipping" => "در حال ارسال", "processing" => "در حال آماده‌سازی",
        "delivered" => "تحویل‌شده", "cancelled" => "لغوشده", _ => "نامشخص"
    };
    [JsonIgnore] public string StatusClass => $"is-{Status}";
    [JsonIgnore] public string DateLabel => AccountDisplay.Date(OrderedOn);
}

public sealed record AccountCoupon(string Id, string Code, int DiscountPercent, bool Enabled,
    DateOnly ExpiresOn, int UsageLimit, IReadOnlyList<string> OrderIds)
{
    public bool IsActive(DateOnly today) => Enabled && ExpiresOn >= today;
}

public static class AccountDisplay
{
    public static string Date(DateOnly date) => date.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("fa-IR"));
}
