using System.Data;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Warehouse.Contracts;
using Shop.Data.Domain.Warehouse.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Warehouse;

public sealed class WarehouseInventoryAdminService(ShopDbContext db) : IWarehouseInventoryAdminService
{
    public async Task<bool> SetMinimumStockAsync(long warehouseId, long inventoryItemId, decimal minimum,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        bool valid = await db.Warehouses.AnyAsync(warehouse => warehouse.Id == warehouseId && warehouse.IsActive,
                         cancellationToken) &&
                     await db.InventoryItems.AnyAsync(item => item.Id == inventoryItemId && item.IsActive &&
                         item.Product.IsWarehouseEnabled, cancellationToken);
        long? regularPlanId = await db.SalesPlanReferences.AsNoTracking()
            .Where(plan => plan.IsRegular && plan.IsActive).Select(plan => (long?)plan.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (!valid || !regularPlanId.HasValue) return false;
        var balance = await db.StockBalances.SingleOrDefaultAsync(row => row.IdWarehouse == warehouseId &&
            row.IdInventoryItem == inventoryItemId && row.IdSalesPlanReference == regularPlanId.Value,
            cancellationToken);
        if (balance is null)
        {
            db.StockBalances.Add(new StockBalance
            {
                IdWarehouse = warehouseId, IdInventoryItem = inventoryItemId,
                IdSalesPlanReference = regularPlanId.Value, Quantity = 0, MinimumQuantity = minimum
            });
        }
        else balance.MinimumQuantity = minimum;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<WarehouseSalesPlanOption>> ReadActiveSalesPlansAsync(
        CancellationToken cancellationToken) => await db.SalesPlanReferences.AsNoTracking()
        .Where(plan => plan.IsActive).OrderBy(plan => plan.DisplayName)
        .Select(plan => new WarehouseSalesPlanOption(plan.StoreSalesPlanKey, plan.DisplayName))
        .ToListAsync(cancellationToken);
}
