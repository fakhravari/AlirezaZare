namespace Shop.Data.Application.Store.Contracts;

public interface IStoreReportsAdminData
{
    Task<object> ReadWarehousesAsync(bool fullAccess, IReadOnlyCollection<long> allowedWarehouseIds,
        CancellationToken cancellationToken);
    Task<long?> ReadDefaultSalesWarehouseIdAsync(bool fullAccess, IReadOnlyCollection<long> allowedWarehouseIds,
        CancellationToken cancellationToken);
    Task<object> ReadSalesAsync(DateTime start, DateTime end, int page, int pageSize,
        CancellationToken cancellationToken);
    Task<object> ReadProductsAsync(DateTime start, DateTime end, long? warehouseId, int page, int pageSize,
        bool fullAccess, long[] allowedWarehouseIds, CancellationToken cancellationToken);
    Task<object> ReadCustomersAsync(DateTime start, DateTime end, int page, int pageSize,
        CancellationToken cancellationToken);
    Task<object> ReadInventoryAsync(DateTime start, DateTime end, long? warehouseId, int page, int pageSize,
        bool fullAccess, long[] allowedWarehouseIds, CancellationToken cancellationToken);
    Task<object?> ReadDetailsAsync(string detail, long id, DateTime start, DateTime end, long? warehouseId,
        long? salesPlanReferenceId, int page, int pageSize, bool fullAccess, long[] allowedWarehouseIds,
        CancellationToken cancellationToken);
}
