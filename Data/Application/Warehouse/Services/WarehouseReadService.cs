using Shop.Data.Application.Warehouse.Contracts;
using Shop.Data.Application.Warehouse.Models;

namespace Shop.Data.Application.Warehouse.Services;

public sealed class WarehouseReadService(IWarehouseReadRepository repository) : IWarehouseReadService
{
    public Task<IReadOnlyList<WarehouseInfo>> GetWarehousesAsync(CancellationToken cancellationToken) =>
        repository.GetWarehousesAsync(cancellationToken);

    public Task<IReadOnlyList<WarehouseStockItem>> GetStockAsync(
        long? warehouseId,
        string? salesPlanKey,
        string? search,
        bool lowStockOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page));
        if (pageSize is < 1 or > 200) throw new ArgumentOutOfRangeException(nameof(pageSize));
        return repository.GetStockAsync(warehouseId, salesPlanKey, search?.Trim(), lowStockOnly, page, pageSize,
            cancellationToken);
    }

    public Task<IReadOnlyList<WarehouseStockItem>> GetStockForWarehousesAsync(
        IReadOnlyCollection<long> warehouseIds,
        string? salesPlanKey,
        string? search,
        bool lowStockOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page));
        if (pageSize is < 1 or > 200) throw new ArgumentOutOfRangeException(nameof(pageSize));
        if (warehouseIds.Count == 0) return Task.FromResult<IReadOnlyList<WarehouseStockItem>>([]);
        return repository.GetStockForWarehousesAsync(warehouseIds, salesPlanKey, search?.Trim(), lowStockOnly, page,
            pageSize, cancellationToken);
    }
}