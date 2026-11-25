using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Warehouse.Contracts;
using Shop.Data.Domain.Warehouse.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Warehouse;

public sealed class WarehouseStockLedger(ShopDbContext db) : IWarehouseStockLedger
{
    public async Task RebuildVoucherAsync(decimal voucherId, CancellationToken ct)
    {
        var document = await db.Vouchers.SingleOrDefaultAsync(x => x.Id == voucherId, ct) ?? throw new InvalidOperationException("Warehouse voucher was not found.");
        var type = await db.VoucherTypes.AsNoTracking().SingleAsync(x => x.Id == document.IdVoucherType, ct);
        var previous = await db.WarehouseStockMovements.Where(x => x.IdVoucher == voucherId).ToListAsync(ct);
        var lines = await db.VoucherLines.Where(x => x.IdVoucher == voucherId).ToListAsync(ct);

        var deltas = new Dictionary<StockKey, decimal>();
        foreach (var movement in previous)
        {
            var key = new StockKey(movement.IdWarehouse, movement.IdInventoryItem, movement.IdSalesPlanReference);
            deltas[key] = deltas.GetValueOrDefault(key) - movement.QuantityDelta;
        }

        var replacement = new List<WarehouseStockMovement>();
        if (type.AffectsStock && await db.VoucherStatuses.AnyAsync(x => x.Id == document.IdVoucherStatus && x.Code == "F", ct))
        {
            decimal direction = type.Direction switch { "R" => 1m, "I" => -1m, _ => 0m };
            foreach (var line in lines)
            {
                decimal delta = line.BaseQuantity * direction;
                if (delta == 0) continue;
                long planId = line.IdSalesPlanReference ?? document.IdSalesPlanReference;
                var key = new StockKey(document.IdWarehouse, line.IdInventoryItem, planId);
                deltas[key] = deltas.GetValueOrDefault(key) + delta;
                replacement.Add(new WarehouseStockMovement
                {
                    IdWarehouse = document.IdWarehouse,
                    IdInventoryItem = line.IdInventoryItem,
                    IdSalesPlanReference = planId,
                    IdVoucher = document.Id,
                    IdVoucherLine = line.Id,
                    QuantityDelta = delta,
                    OccurredAtUtc = DateTime.UtcNow
                });
            }
        }

        await ApplyDeltasAsync(deltas, ct);
        db.WarehouseStockMovements.RemoveRange(previous);
        db.WarehouseStockMovements.AddRange(replacement);
        await db.SaveChangesAsync(ct);
    }

    public async Task ReverseMovementsAsync(decimal voucherId, CancellationToken ct)
    {
        var movements = await db.WarehouseStockMovements.Where(x => x.IdVoucher == voucherId).ToListAsync(ct);
        var deltas = new Dictionary<StockKey, decimal>();
        foreach (var movement in movements)
        {
            var key = new StockKey(movement.IdWarehouse, movement.IdInventoryItem, movement.IdSalesPlanReference);
            deltas[key] = deltas.GetValueOrDefault(key) - movement.QuantityDelta;
        }

        await ApplyDeltasAsync(deltas, ct);
        db.WarehouseStockMovements.RemoveRange(movements);
        await db.SaveChangesAsync(ct);
    }

    private async Task ApplyDeltasAsync(Dictionary<StockKey, decimal> deltas, CancellationToken ct)
    {
        DateTime now = DateTime.UtcNow;
        foreach (var key in deltas.Keys.OrderBy(x => x.WarehouseId).ThenBy(x => x.InventoryItemId).ThenBy(x => x.PlanId))
        {
            int locked = await db.StockBalances.Where(x =>
                    x.IdWarehouse == key.WarehouseId && x.IdInventoryItem == key.InventoryItemId &&
                    x.IdSalesPlanReference == key.PlanId)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.UpdatedAtUtc, now), ct);
            if (locked == 0)
            {
                db.StockBalances.Add(new StockBalance
                {
                    IdWarehouse = key.WarehouseId,
                    IdInventoryItem = key.InventoryItemId,
                    IdSalesPlanReference = key.PlanId,
                    Quantity = 0,
                    MinimumQuantity = 0,
                    UpdatedAtUtc = now
                });
                await db.SaveChangesAsync(ct);
            }
        }

        foreach (var (key, delta) in deltas.OrderBy(x => x.Key.WarehouseId).ThenBy(x => x.Key.InventoryItemId)
                     .ThenBy(x => x.Key.PlanId))
        {
            if (delta == 0) continue;
            int updated = await db.StockBalances.Where(x => x.IdWarehouse == key.WarehouseId &&
                                                            x.IdInventoryItem == key.InventoryItemId &&
                                                            x.IdSalesPlanReference == key.PlanId &&
                                                            x.Quantity + delta >= 0)
                .ExecuteUpdateAsync(
                    update => update.SetProperty(x => x.Quantity, x => x.Quantity + delta)
                        .SetProperty(x => x.UpdatedAtUtc, now), ct);
            if (updated != 1) throw new InsufficientWarehouseStockException();
        }
    }

    private sealed record StockKey(long WarehouseId, decimal InventoryItemId, long PlanId);
}
