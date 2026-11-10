using Shop.Data.Application.Warehouse.Models;

namespace Shop.Data.Application.Warehouse.Contracts;

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

    Task<IReadOnlyList<WarehouseStockItem>> GetStockForWarehousesAsync(
        IReadOnlyCollection<long> warehouseIds,
        string? salesPlanKey,
        string? search,
        bool lowStockOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}