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
        => await GetStockCoreAsync(warehouseId, null, salesPlanKey, search, lowStockOnly, page, pageSize, cancellationToken);

    public async Task<IReadOnlyList<WarehouseStockItem>> GetStockForWarehousesAsync(
        IReadOnlyCollection<long> warehouseIds,
        string? salesPlanKey,
        string? search,
        bool lowStockOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        await GetStockCoreAsync(null, warehouseIds, salesPlanKey, search, lowStockOnly, page, pageSize, cancellationToken);

    private async Task<IReadOnlyList<WarehouseStockItem>> GetStockCoreAsync(
        long? warehouseId,
        IReadOnlyCollection<long>? warehouseIds,
        string? salesPlanKey,
        string? search,
        bool lowStockOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = from warehouse in dbContext.Warehouses.AsNoTracking()
                    from inventoryItem in dbContext.InventoryItems.AsNoTracking()
                    from plan in dbContext.SalesPlanReferences.AsNoTracking()
                    join balance in dbContext.StockBalances.AsNoTracking()
                        on new { IdWarehouse = warehouse.Id, IdInventoryItem = inventoryItem.Id, IdSalesPlanReference = plan.Id }
                        equals new { balance.IdWarehouse, balance.IdInventoryItem, balance.IdSalesPlanReference } into balances
                    from balance in balances.DefaultIfEmpty()
                    where warehouse.IsActive && warehouse.Company.IsActive && inventoryItem.IsActive && inventoryItem.Product.IsActive
                        && (inventoryItem.ProductVariant == null || inventoryItem.ProductVariant!.IsActive) && plan.IsActive
                    select new { warehouse, inventoryItem, plan, balance };

        if (warehouseId.HasValue) query = query.Where(x => x.warehouse.Id == warehouseId.Value);
        if (warehouseIds is not null) query = query.Where(x => warehouseIds.Contains(x.warehouse.Id));
        if (!string.IsNullOrWhiteSpace(salesPlanKey))
            query = query.Where(x => x.plan.StoreSalesPlanKey == salesPlanKey);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.inventoryItem.Product.Name.Contains(search)
                || x.inventoryItem.Product.StoreProductKey.Contains(search)
                || (x.inventoryItem.ProductVariant != null && x.inventoryItem.ProductVariant.Name.Contains(search)));
        if (lowStockOnly)
            query = query.Where(x => x.plan.IsRegular && x.balance != null
                && x.balance.MinimumQuantity > 0 && x.balance.Quantity <= x.balance.MinimumQuantity);

        return await query.OrderBy(x => x.warehouse.Name)
            .ThenBy(x => x.inventoryItem.Product.Name)
            .ThenBy(x => x.inventoryItem.ProductVariant == null ? string.Empty : x.inventoryItem.ProductVariant.Name)
            .ThenBy(x => x.plan.StoreSalesPlanKey)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new WarehouseStockItem(
                x.warehouse.Id,
                x.inventoryItem.Id,
                x.plan.Id,
                x.warehouse.Code,
                x.warehouse.Name,
                x.inventoryItem.Product.IdStoreProduct,
                x.inventoryItem.Product.StoreProductKey,
                x.inventoryItem.Product.Name,
                x.inventoryItem.IdProductVariant,
                x.inventoryItem.ProductVariant == null ? null : x.inventoryItem.ProductVariant.Name,
                x.plan.StoreSalesPlanKey,
                x.plan.DisplayName,
                x.plan.IsRegular,
                x.balance == null ? 0m : x.balance.Quantity,
                x.plan.IsRegular && x.balance != null ? x.balance.MinimumQuantity : 0m,
                x.plan.IsRegular && x.balance != null && x.balance.MinimumQuantity > 0 && x.balance.Quantity <= x.balance.MinimumQuantity))
            .ToListAsync(cancellationToken);
    }
}
