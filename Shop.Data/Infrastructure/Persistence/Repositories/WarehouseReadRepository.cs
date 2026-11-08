using Microsoft.EntityFrameworkCore;
using Shop.Database.Application.Warehouse.Contracts;
using Shop.Database.Application.Warehouse.Models;
using Shop.Database.Infrastructure.Persistence;

namespace Shop.Database.Infrastructure.Persistence.Repositories;

public sealed class WarehouseReadRepository(ShopDbContext dbContext) : IWarehouseReadRepository
{
    public async Task<IReadOnlyList<WarehouseInfo>> GetWarehousesAsync(CancellationToken cancellationToken) =>
        await dbContext.Warehouses.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new WarehouseInfo(x.Id, x.Code, x.Name, x.Company.Name, x.IsSalesWarehouse, x.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WarehouseStockItem>> GetStockAsync(
        long? warehouseId,
        string? salesPlanKey,
        string? search,
        bool lowStockOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.StockBalances.AsNoTracking()
            .Where(x => x.Warehouse.IsActive && x.InventoryItem.Product.IsActive);

        if (warehouseId.HasValue) query = query.Where(x => x.IdWarehouse == warehouseId.Value);
        if (!string.IsNullOrWhiteSpace(salesPlanKey))
            query = query.Where(x => x.SalesPlanReference.StoreSalesPlanKey == salesPlanKey);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.InventoryItem.Product.Name.Contains(search)
                || x.InventoryItem.Product.StoreProductKey.Contains(search));
        if (lowStockOnly) query = query.Where(x => x.Quantity <= x.MinimumQuantity);

        return await query.OrderBy(x => x.Warehouse.Name)
            .ThenBy(x => x.InventoryItem.Product.Name)
            .ThenBy(x => x.SalesPlanReference.StoreSalesPlanKey)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new WarehouseStockItem(
                x.IdWarehouse,
                x.Warehouse.Code,
                x.Warehouse.Name,
                x.InventoryItem.Product.IdStoreProduct,
                x.InventoryItem.Product.StoreProductKey,
                x.InventoryItem.Product.Name,
                x.InventoryItem.IdProductVariant,
                x.InventoryItem.ProductVariant == null ? null : x.InventoryItem.ProductVariant.Name,
                x.SalesPlanReference.StoreSalesPlanKey,
                x.SalesPlanReference.DisplayName,
                x.Quantity,
                x.MinimumQuantity,
                x.Quantity <= x.MinimumQuantity))
            .ToListAsync(cancellationToken);
    }
}
