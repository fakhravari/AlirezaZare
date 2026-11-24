using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Warehouse.Contracts;
using Shop.Data.Application.Warehouse.Models;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Persistence.Repositories;

public sealed class WarehouseReadRepository(ShopDbContext dbContext) : IWarehouseReadRepository
{
    public async Task<IReadOnlyList<WarehouseInfo>> GetWarehousesAsync(CancellationToken cancellationToken) =>
        await dbContext.Warehouses.AsNoTracking().OrderBy(x => x.Name)
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
        => await GetStockCoreAsync(warehouseId, null, salesPlanKey, search, lowStockOnly, page, pageSize,
            cancellationToken);

    public async Task<IReadOnlyList<WarehouseStockItem>> GetStockForWarehousesAsync(
        IReadOnlyCollection<long> warehouseIds,
        string? salesPlanKey,
        string? search,
        bool lowStockOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        await GetStockCoreAsync(null, warehouseIds, salesPlanKey, search, lowStockOnly, page, pageSize,
            cancellationToken);

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
        var query = dbContext.Warehouses
            .AsNoTracking()
            .SelectMany(
                warehouse => dbContext.InventoryItems.AsNoTracking(),
                (warehouse, inventoryItem) => new { warehouse, inventoryItem })
            .SelectMany(
                warehouseInventory => dbContext.SalesPlanReferences.AsNoTracking(),
                (warehouseInventory, plan) => new
                {
                    warehouseInventory.warehouse,
                    warehouseInventory.inventoryItem,
                    plan
                })
            .Join(
                dbContext.WarehouseProductUnits.AsNoTracking().Where(unitLink => unitLink.IsActive && unitLink.IsBaseUnit),
                warehouseInventoryPlan => warehouseInventoryPlan.inventoryItem.IdProduct,
                baseUnit => baseUnit.IdProduct,
                (warehouseInventoryPlan, baseUnit) => new { warehouseInventoryPlan.warehouse,
                    warehouseInventoryPlan.inventoryItem, warehouseInventoryPlan.plan, baseUnit })
            .Join(
                dbContext.WarehouseUnits.AsNoTracking(),
                warehouseInventoryPlanUnit => warehouseInventoryPlanUnit.baseUnit.IdUnit,
                unit => unit.Id,
                (warehouseInventoryPlanUnit, unit) => new
                {
                    warehouseInventoryPlanUnit.warehouse,
                    warehouseInventoryPlanUnit.inventoryItem,
                    warehouseInventoryPlanUnit.plan,
                    unit
                })
            .GroupJoin(
                dbContext.StockBalances.AsNoTracking(),
                warehouseInventoryPlanUnit => new
                {
                    IdWarehouse = warehouseInventoryPlanUnit.warehouse.Id,
                    IdInventoryItem = warehouseInventoryPlanUnit.inventoryItem.Id,
                    IdSalesPlanReference = warehouseInventoryPlanUnit.plan.Id
                },
                balance => new
                {
                    balance.IdWarehouse,
                    balance.IdInventoryItem,
                    balance.IdSalesPlanReference
                },
                (warehouseInventoryPlanUnit, balances) => new { warehouseInventoryPlanUnit, balances })
            .SelectMany(
                warehouseInventoryPlanBalances => warehouseInventoryPlanBalances.balances.DefaultIfEmpty(),
                (warehouseInventoryPlanBalances, balance) => new
                {
                    warehouseInventoryPlanBalances.warehouseInventoryPlanUnit.warehouse,
                    warehouseInventoryPlanBalances.warehouseInventoryPlanUnit.inventoryItem,
                    warehouseInventoryPlanBalances.warehouseInventoryPlanUnit.plan,
                    warehouseInventoryPlanBalances.warehouseInventoryPlanUnit.unit,
                    balance
                })
            .Where(stockRow =>
                stockRow.warehouse.IsActive && stockRow.warehouse.Company.IsActive && stockRow.inventoryItem.IsActive &&
                stockRow.inventoryItem.Product.IsWarehouseEnabled &&
                (stockRow.inventoryItem.ProductVariant == null || stockRow.inventoryItem.ProductVariant.IsActive) &&
                stockRow.plan.IsActive &&
                dbContext.WarehouseStockOwners.Any(owner =>
                    owner.IdWarehouse == stockRow.warehouse.Id && owner.IdStockOwner == stockRow.plan.Id));

        if (warehouseId.HasValue) query = query.Where(x => x.warehouse.Id == warehouseId.Value);
        if (warehouseIds is not null) query = query.Where(x => warehouseIds.Contains(x.warehouse.Id));
        if (!string.IsNullOrWhiteSpace(salesPlanKey))
            query = query.Where(x => x.plan.StoreSalesPlanKey == salesPlanKey);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.inventoryItem.Product.NameFa.Contains(search)
                                     || x.inventoryItem.Product.ProductKey.Contains(search)
                                     || (x.inventoryItem.ProductVariant != null &&
                                         x.inventoryItem.ProductVariant.VariantName.Name.Contains(search)));
        if (lowStockOnly)
            query = query.Where(x =>
                x.plan.IsRegular && x.balance != null && x.balance.MinimumQuantity > 0 &&
                x.balance.Quantity <= x.balance.MinimumQuantity);

        return await query.OrderBy(x => x.warehouse.Name).ThenBy(x => x.inventoryItem.Product.NameFa)
            .ThenBy(x =>
                x.inventoryItem.ProductVariant == null ? string.Empty : x.inventoryItem.ProductVariant.VariantName.Name)
            .ThenBy(x => x.plan.StoreSalesPlanKey).Skip((page - 1) * pageSize).Take(pageSize).Select(x =>
                new WarehouseStockItem(
                    x.warehouse.Id,
                    x.inventoryItem.Id,
                    x.plan.Id,
                    x.warehouse.Code,
                    x.warehouse.Name,
                    x.warehouse.Company.Name,
                    x.inventoryItem.Product.Id,
                    x.inventoryItem.Product.ProductKey,
                    x.inventoryItem.Product.NameFa,
                    x.inventoryItem.IdVariant,
                    x.inventoryItem.ProductVariant == null ? null : x.inventoryItem.ProductVariant.VariantName.Name,
                    x.plan.StoreSalesPlanKey,
                    x.plan.DisplayName,
                    x.unit.NameFa,
                    x.plan.IsRegular,
                    x.balance == null ? 0m : x.balance.Quantity,
                    x.plan.IsRegular && x.balance != null ? x.balance.MinimumQuantity : 0m,
                    x.plan.IsRegular && x.balance != null && x.balance.MinimumQuantity > 0 &&
                    x.balance.Quantity <= x.balance.MinimumQuantity)).ToListAsync(cancellationToken);
    }
}
