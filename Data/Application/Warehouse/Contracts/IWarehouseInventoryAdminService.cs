namespace Shop.Data.Application.Warehouse.Contracts;

public interface IWarehouseInventoryAdminService
{
    Task<bool> SetMinimumStockAsync(long warehouseId, long inventoryItemId, decimal minimum,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<WarehouseSalesPlanOption>> ReadActiveSalesPlansAsync(CancellationToken cancellationToken);
}

public sealed record WarehouseSalesPlanOption(string? Id, string Name);
