namespace Shop.Data.Application.Store.Contracts;

public interface IStoreOrdersAdminData
{
    Task<IReadOnlyList<OrderStatusData>> ReadStatusesAsync(CancellationToken cancellationToken);
    Task<bool> UpdateTrackingAsync(decimal orderId, string carrier, string trackingCode, string? expectedCarrier,
        string? expectedTrackingCode, CancellationToken cancellationToken);
    Task<ShippingStatusUpdateStatus> UpdateShippingStatusAsync(decimal orderId, string expectedStatus, string status,
        CancellationToken cancellationToken);
    Task<OrderListData> ReadOrdersAsync(string search, string status, DateTime? fromUtc, DateTime? untilUtc,
        int offset, int pageSize, CancellationToken cancellationToken);
    Task<OrderDetailsData?> ReadOrderDetailsAsync(decimal orderId, CancellationToken cancellationToken);
}

public sealed record OrderStatusData(int Id, string Code, string NameFa, string NameEn);
public sealed record OrderListData(IReadOnlyList<OrderListRowData> Items, int Total);
public sealed record OrderListRowData(decimal Id, int IdStatus, string OrderNumber, string Status, string StatusName,
    string PaymentMethod, long IdBank, string? BankName, int ItemCount, decimal TotalAmount,
    DateTime CreatedAtUtc, string CustomerName);
public enum ShippingStatusUpdateStatus { Updated, Conflict, MissingStatus }
public sealed class OrderDetailsData : OrderListRowDataBase
{
    public decimal IdAddress { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal CouponDiscountAmount { get; set; }
    public decimal CalculatedItemSubtotal { get; set; }
    public bool HasItemSubtotalMismatch { get; set; }
    public decimal? IdVoucher { get; set; }
    public string? ShippingCarrier { get; set; }
    public string? ShippingTrackingCode { get; set; }
    public string AddressJson { get; set; } = "{}";
    public string ItemsJson { get; set; } = "[]";
    public string ComponentsJson { get; set; } = "[]";
}
public abstract class OrderListRowDataBase
{
    public decimal Id { get; set; }
    public int IdStatus { get; set; }
    public string OrderNumber { get; set; } = "";
    public string Status { get; set; } = "";
    public string StatusName { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public long IdBank { get; set; }
    public string? BankName { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CustomerName { get; set; } = "";
}
