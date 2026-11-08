using Shop.Database.Application.Warehouse.Models;

namespace Shop.Database.Application.Warehouse.Contracts;

public interface IWarehouseReadService
{
    Task<IReadOnlyList<WarehouseInfo>> GetWarehousesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<WarehouseStockItem>> GetStockAsync(
        long? warehouseId,
        string? salesPlanKey,
        string? search,
        bool lowStockOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
