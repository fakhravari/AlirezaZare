namespace Shop.Data.Domain.Store.Entities;

public sealed class OrderStatusReference
{
    public int Id { get; set; }
    public string NameFa { get; set; } = "";
    public string NameEn { get; set; } = "";
}

public static class StoreOrderStatusNames
{
    public const string PendingPayment = "Pending payment";
    public const string PaymentConfirmed = "Payment confirmed";
    public const string WarehouseConfirmed = "Warehouse confirmed";
    public const string InTransit = "In transit";
    public const string ShippedToPost = "Shipped to post";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";

    public static string CodeFromNameEn(string nameEn) => nameEn switch
    {
        PendingPayment => "pending-payment",
        PaymentConfirmed => "payment-confirmed",
        WarehouseConfirmed => "warehouse-confirmed",
        InTransit => "in-transit",
        ShippedToPost => "shipped-to-post",
        Delivered => "delivered",
        Cancelled => "cancelled",
        _ => "unknown"
    };

    public static string? NameEnFromCode(string code) => code switch
    {
        "pending-payment" => PendingPayment,
        "payment-confirmed" => PaymentConfirmed,
        "warehouse-confirmed" => WarehouseConfirmed,
        "in-transit" => InTransit,
        "shipped-to-post" => ShippedToPost,
        "delivered" => Delivered,
        "cancelled" => Cancelled,
        _ => null
    };
}

public sealed class PaymentAttemptStatusReference
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string NameFa { get; set; } = "";
    public string NameEn { get; set; } = "";
}

public sealed class ContactMessageStatusReference
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string NameFa { get; set; } = "";
    public string NameEn { get; set; } = "";
}

public sealed class ArticleFeedbackStatusReference
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string NameFa { get; set; } = "";
    public string NameEn { get; set; } = "";
}

public sealed class VoucherStatus
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string NameFa { get; set; } = "";
    public string NameEn { get; set; } = "";
}
