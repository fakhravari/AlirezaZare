namespace Shop.Data.Application.Warehouse.Contracts;

public interface IStorePaidOrderDispatchData
{
    Task<long?> ReadSalesWarehouseIdAsync(CancellationToken cancellationToken);
    Task<PaidOrderPageData> ReadPaidOrdersAsync(string search, int page, int pageSize,
        CancellationToken cancellationToken);
    Task<StoreDispatchResult> CreateAsync(decimal[] orderIds, string actor, CancellationToken cancellationToken);
}

public sealed record PaidOrderData(decimal Id, string OrderNumber, decimal TotalAmount, int ItemCount,
    DateTime PaidAtUtc, string CustomerName);
public sealed record PaidOrderPageData(IReadOnlyList<PaidOrderData> Items, int Total, int Page, int PageSize);
public sealed record StoreDispatchResult(bool Success, string? Error, decimal VoucherId, long? VoucherNumber);
