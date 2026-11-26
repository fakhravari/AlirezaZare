using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Warehouse.Contracts;
using Shop.Data.Domain.Warehouse.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Warehouse;

public sealed class StorePaidOrderDispatchData(ShopDbContext db, IWarehouseStockLedger stockLedger,
    IVoucherNumberGenerator voucherNumberGenerator) : IStorePaidOrderDispatchData
{
    public Task<long?> ReadSalesWarehouseIdAsync(CancellationToken ct) => db.StoreSiteIdentities.AsNoTracking()
        .Where(x => x.Id == 1).Select(x => x.IdWarehouse).SingleOrDefaultAsync(ct);

    public async Task<PaidOrderPageData> ReadPaidOrdersAsync(string search, int page, int pageSize,
        CancellationToken ct)
    {
        var eligible = db.StoreCustomerOrders.AsNoTracking()
            .Join(db.AccountProfiles.AsNoTracking(), order => order.IdProfile, profile => profile.Id,
                (order, profile) => new { order, profile })
            .Where(x => x.order.StatusReference.Code == "payment-confirmed" && x.order.IdVoucher == null &&
                db.StorePaymentAttempts.Any(payment => payment.IdOrder == x.order.Id &&
                    payment.StatusReference.Code == "verified" && payment.Amount == x.order.TotalAmount &&
                    payment.PaidAtUtc != null) && db.StoreCustomerOrderItems.Any(item => item.IdOrder == x.order.Id));
        if (search.Length > 0) eligible = eligible.Where(x => x.order.OrderNumber.Contains(search) ||
            (x.profile.FirstName + " " + x.profile.LastName).Contains(search));
        int total = await eligible.CountAsync(ct);
        var rows = await eligible.Select(x => new
            {
                x.order, x.profile,
                PaidAtUtc = db.StorePaymentAttempts.AsNoTracking().Where(payment => payment.IdOrder == x.order.Id &&
                    payment.StatusReference.Code == "verified" && payment.Amount == x.order.TotalAmount &&
                    payment.PaidAtUtc != null).OrderBy(payment => payment.PaidAtUtc)
                    .Select(payment => payment.PaidAtUtc).FirstOrDefault() ?? x.order.CreatedAtUtc
            }).OrderBy(x => x.PaidAtUtc).ThenBy(x => x.order.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new PaidOrderData(x.order.Id, x.order.OrderNumber, x.order.TotalAmount, x.order.ItemCount,
                x.PaidAtUtc, (x.profile.FirstName + " " + x.profile.LastName).Trim())).ToListAsync(ct);
        return new PaidOrderPageData(rows, total, page, pageSize);
    }

    public async Task<StoreDispatchResult> CreateAsync(decimal[] ids, string actor, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var orders = await db.StoreCustomerOrders.Include(x => x.StatusReference).Where(x => ids.Contains(x.Id))
                .ToListAsync(ct);
            if (orders.Count != ids.Length) return Fail("orders-changed");
            var orderIds = orders.Select(x => x.Id).ToArray();
            var payments = await db.StorePaymentAttempts.Where(x => orderIds.Contains(x.IdOrder) &&
                x.StatusReference.Code == "verified" && x.PaidAtUtc != null).ToListAsync(ct);
            var orderItems = await db.StoreCustomerOrderItems.AsNoTracking().Where(x => orderIds.Contains(x.IdOrder))
                .ToListAsync(ct);
            if (orders.Any(order => order.StatusReference.Code != "payment-confirmed" || order.IdVoucher is not null ||
                !payments.Any(payment => payment.IdOrder == order.Id && payment.Amount == order.TotalAmount) ||
                !orderItems.Any(item => item.IdOrder == order.Id))) return Fail("orders-changed");

            long[] planIds = orderItems.Select(x => x.IdSalesPlan).Distinct().ToArray();
            var settings = await db.StoreSiteIdentities.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct);
            var warehouse = settings?.IdWarehouse is long wid
                ? await db.Warehouses.SingleOrDefaultAsync(x => x.Id == wid && x.IsActive, ct) : null;
            var type = await db.VoucherTypes.SingleOrDefaultAsync(x => x.IsSystemSalesDispatch && x.IsActive &&
                x.Direction == "I", ct);
            var party = await db.WarehouseParties.SingleOrDefaultAsync(x => x.ExternalPartyKey == "STORE-SALES" &&
                x.IsActive, ct);
            if (warehouse is null || type is null || party is null) return Fail("dispatch-config");
            long[] assigned = await db.WarehouseStockOwners.AsNoTracking().Where(x => x.IdWarehouse == warehouse.Id &&
                planIds.Contains(x.IdStockOwner)).Select(x => x.IdStockOwner).ToArrayAsync(ct);
            if (assigned.Length != planIds.Length) return Fail("warehouse-plan");

            decimal[] productIds = orderItems.Select(x => x.IdProduct).Distinct().ToArray();
            var productNames = await db.Products.AsNoTracking().Where(x => productIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.NameFa, ct);
            long[] variantIds = orderItems.Where(x => x.IdVariant.HasValue).Select(x => x.IdVariant!.Value)
                .Distinct().ToArray();
            var units = await db.WarehouseProductUnits.AsNoTracking().Where(x => productIds.Contains(x.IdProduct) &&
                x.IsBaseUnit && x.IsActive).Select(x => new { x.IdProduct, x.IdUnit }).ToListAsync(ct);
            var baseUnits = units.GroupBy(x => x.IdProduct).Where(x => x.Count() == 1)
                .ToDictionary(x => x.Key, x => x.Single().IdUnit);
            var inventory = await db.InventoryItems.AsNoTracking().Where(x => productIds.Contains(x.IdProduct))
                .Select(x => new { x.Id, x.IdProduct, x.IdVariant }).ToListAsync(ct);
            long[] unitIds = baseUnits.Values.Distinct().ToArray();
            var unitNames = await db.WarehouseUnits.AsNoTracking().Where(x => unitIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.NameFa, ct);
            var variantNames = await db.ProductVariants.AsNoTracking().Where(x => variantIds.Contains(x.Id))
                .Select(x => new { x.Id, Name = x.VariantName.Name }).ToDictionaryAsync(x => x.Id, x => x.Name, ct);
            var inventoryLookup = inventory.GroupBy(x => (x.IdProduct, x.IdVariant)).ToDictionary(x => x.Key, x => x.ToArray());
            var lines = new List<DispatchLine>(orderItems.Count);
            foreach (var line in orderItems)
            {
                if (!productNames.TryGetValue(line.IdProduct, out var productName) ||
                    !baseUnits.TryGetValue(line.IdProduct, out long unitId) ||
                    !unitNames.TryGetValue(unitId, out var unitName) ||
                    !inventoryLookup.TryGetValue((line.IdProduct, line.IdVariant), out var matches) || matches.Length != 1)
                    return Fail("inventory-mapping");
                lines.Add(new DispatchLine(line.IdOrder, matches[0].Id, unitId, line.IdSalesPlan, line.Quantity,
                    productName, line.IdVariant is long variantId ? variantNames.GetValueOrDefault(variantId) : null,
                    unitName));
            }

            var document = new Voucher
            {
                VoucherNumber = await voucherNumberGenerator.NextAsync(ct), IdWarehouse = warehouse.Id,
                IdVoucherType = type.Id, IdParty = party.Id, IdSalesPlanReference = lines.Min(x => x.PlanId),
                VoucherDate = DateOnly.FromDateTime(DateTime.Today),
                IdVoucherStatus = await db.VoucherStatuses.Where(x => x.Code == "F").Select(x => x.Id).SingleAsync(ct),
                Reason = "حوالهٔ تجمیعی سفارش‌های فروشگاه", CreatedByUserKey = actor, CreatedAtUtc = DateTime.UtcNow
            };
            db.Vouchers.Add(document);
            await db.SaveChangesAsync(ct);
            foreach (var group in lines.GroupBy(x => new { x.InventoryId, x.UnitId, x.PlanId, x.ProductName, x.VariantName, x.UnitName }))
                db.VoucherLines.Add(new VoucherLine { IdVoucher = document.Id, IdInventoryItem = group.Key.InventoryId,
                    IdUnit = group.Key.UnitId, IdSalesPlanReference = group.Key.PlanId, Quantity = group.Sum(x => x.Quantity),
                    ConversionFactor = 1, BaseQuantity = group.Sum(x => x.Quantity), ProductNameSnapshot = group.Key.ProductName,
                    VariantNameSnapshot = group.Key.VariantName, UnitNameSnapshot = group.Key.UnitName });
            await db.SaveChangesAsync(ct);
            await stockLedger.RebuildVoucherAsync(document.Id, ct);
            int confirmed = await db.OrderStatuses.Where(x => x.Code == "warehouse-confirmed").Select(x => x.Id).SingleAsync(ct);
            foreach (var order in orders) { order.IdVoucher = document.Id; order.IdStatus = confirmed; }
            db.VoucherAudits.Add(new VoucherAudit { VoucherId = checked((long)document.Id), VoucherNumber = document.VoucherNumber,
                WarehouseId = warehouse.Id, Action = "Created", ActorUserKey = actor, OccurredAtUtc = DateTime.UtcNow,
                Reason = document.Reason, AfterJson = JsonSerializer.Serialize(new { orderIds = ids }) });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return new StoreDispatchResult(true, null, document.Id, document.VoucherNumber);
        }
        catch (InsufficientWarehouseStockException) { return Fail("insufficient-stock"); }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 or 547 })
        { return Fail("orders-changed"); }
    }

    private static StoreDispatchResult Fail(string reason) => new(false, reason, 0, null);
    private sealed record DispatchLine(decimal OrderId, decimal InventoryId, long UnitId, long PlanId,
        decimal Quantity, string ProductName, string? VariantName, string UnitName);
}
