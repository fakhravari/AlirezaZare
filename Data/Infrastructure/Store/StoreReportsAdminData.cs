using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;
using Shop.Data.Helpers;

namespace Shop.Data.Infrastructure.Store;

public sealed class StoreReportsAdminData(ShopDbContext db) : IStoreReportsAdminData
{
    private static readonly string[] SalesStatuses =
    [
        StoreOrderStatusNames.PaymentConfirmed, StoreOrderStatusNames.WarehouseConfirmed,
        StoreOrderStatusNames.InTransit, StoreOrderStatusNames.ShippedToPost, StoreOrderStatusNames.Delivered
    ];

    public async Task<object> ReadWarehousesAsync(bool fullAccess, IReadOnlyCollection<long> allowedWarehouseIds,
        CancellationToken ct)
    {
        var warehouses = await db.Warehouses.AsNoTracking()
            .Where(x => x.IsActive && x.Company.IsActive && x.IsSalesWarehouse &&
                        (fullAccess || allowedWarehouseIds.Contains(x.Id)))
            .OrderBy(x => x.Company.Name).ThenBy(x => x.Name)
            .Select(x => new { x.Id, Label = x.Company.Name + " / " + x.Name + " (" + x.Code + ")" })
            .ToListAsync(ct);
        long? defaultWarehouseId = await ReadDefaultSalesWarehouseIdAsync(fullAccess, allowedWarehouseIds, ct);
        return new { items = warehouses, defaultWarehouseId, warehouseAccess = fullAccess || allowedWarehouseIds.Count > 0 };
    }

    public async Task<long?> ReadDefaultSalesWarehouseIdAsync(bool fullAccess,
        IReadOnlyCollection<long> allowedWarehouseIds, CancellationToken ct)
    {
        long? configured = await db.StoreSiteIdentities.AsNoTracking().Where(x => x.Id == 1)
            .Select(x => x.IdWarehouse).SingleOrDefaultAsync(ct);
        var warehouses = db.Warehouses.AsNoTracking().Where(x => x.IsActive && x.Company.IsActive &&
            x.IsSalesWarehouse && (fullAccess || allowedWarehouseIds.Contains(x.Id)));
        if (configured.HasValue && await warehouses.AnyAsync(x => x.Id == configured.Value, ct)) return configured;
        return await warehouses.OrderBy(x => x.Company.Name).ThenBy(x => x.Name).Select(x => (long?)x.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<object> ReadSalesAsync(DateTime start, DateTime end, int page, int pageSize,
        CancellationToken ct)
    {
        TimeSpan duration = end - start;
        DateTime previousStart = start - duration;
        var orders = PaidOrders().Where(x => x.CreatedAtUtc >= start && x.CreatedAtUtc < end);
        var current = await orders.GroupBy(_ => 1).Select(group => new
        {
            OrderCount = group.Count(), Revenue = group.Sum(x => x.TotalAmount),
            Discount = group.Sum(x => x.CouponDiscountAmount)
        }).SingleOrDefaultAsync(ct);
        var previous = await PaidOrders().Where(x => x.CreatedAtUtc >= previousStart && x.CreatedAtUtc < start)
            .GroupBy(_ => 1).Select(group => new { Revenue = group.Sum(x => x.TotalAmount) })
            .SingleOrDefaultAsync(ct);
        int ordersWithoutVerifiedPayment = await db.StoreCustomerOrders.CountAsync(order =>
            order.CreatedAtUtc >= start && order.CreatedAtUtc < end && SalesStatuses.Contains(order.StatusReference.NameEn) &&
            !db.StorePaymentAttempts.Any(payment => payment.IdOrder == order.Id &&
                payment.StatusReference.Code == "verified" && payment.Amount == order.TotalAmount), ct);
        var daily = await orders.GroupBy(x => x.CreatedAtUtc.Date).Select(group => new
        {
            Date = group.Key, OrderCount = group.Count(), Revenue = group.Sum(x => x.TotalAmount),
            Discount = group.Sum(x => x.CouponDiscountAmount)
        }).OrderBy(x => x.Date).ToListAsync(ct);
        var items = daily.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new
        {
            date = x.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), orderCount = x.OrderCount,
            revenue = x.Revenue, discount = x.Discount
        }).ToArray();
        decimal revenue = current?.Revenue ?? 0;
        return new
        {
            items, total = daily.Count, page, pageSize,
            hasNext = AdminListQuery.HasNext(page, pageSize, daily.Count),
            summary = new
            {
                orderCount = current?.OrderCount ?? 0, revenue, discount = current?.Discount ?? 0,
                averageOrderAmount = current?.OrderCount > 0 ? revenue / current.OrderCount : 0,
                ordersWithoutVerifiedPayment, previousRevenue = previous?.Revenue ?? 0,
                revenueChangePercent = ChangePercent(revenue, previous?.Revenue ?? 0)
            }
        };
    }

    public async Task<object> ReadProductsAsync(DateTime start, DateTime end, long? warehouseId, int page, int pageSize, bool fullAccess, long[] allowedWarehouseIds, CancellationToken cancellationToken)
    {
        var sold = db.StoreCustomerOrderItems
            .AsNoTracking()
            .Join(
                PaidOrders(),
                item => item.IdOrder,
                order => order.Id,
                (item, order) => new { item, order })
            .Join(
                db.Products.AsNoTracking(),
                itemOrder => itemOrder.item.IdProduct,
                product => product.Id,
                (itemOrder, product) => new { itemOrder.item, itemOrder.order, product })
            .Join(
                db.StorePriceVersions.AsNoTracking(),
                itemOrderProduct => itemOrderProduct.item.IdPriceVersion,
                price => price.Id,
                (itemOrderProduct, price) => new
                {
                    itemOrderProduct.item,
                    itemOrderProduct.order,
                    itemOrderProduct.product,
                    UnitAmount = price.FinalPriceAmount +
                        (db.StoreOrderAccessories
                            .Where(accessory => accessory.IdOrder == itemOrderProduct.item.IdOrder &&
                                                accessory.IdProduct == itemOrderProduct.item.IdProduct &&
                                                accessory.IdSalesPlan == itemOrderProduct.item.IdSalesPlan)
                            .Select(accessory => (decimal?)accessory.PriceVersion.FinalPriceAmount)
                            .Sum() ?? 0m)
                })
            .Where(itemDetails => itemDetails.order.CreatedAtUtc >= start && itemDetails.order.CreatedAtUtc < end)
            .GroupBy(itemDetails => new { itemDetails.item.IdProduct, ProductName = itemDetails.product.NameFa })
            .Select(productGroup => new
            {
                ProductId = productGroup.Key.IdProduct,
                ProductName = productGroup.Key.ProductName,
                Quantity = productGroup.Sum(itemDetails => itemDetails.item.Quantity),
                Revenue = productGroup.Sum(itemDetails => itemDetails.UnitAmount * itemDetails.item.Quantity),
                OrderCount = productGroup.Select(itemDetails => itemDetails.item.IdOrder).Distinct().Count()
            });
        int total = await sold.Select(x => x.ProductId).Distinct().CountAsync(cancellationToken);
        var previous = await db.StoreCustomerOrderItems
            .AsNoTracking()
            .Join(
                PaidOrders(),
                item => item.IdOrder,
                order => order.Id,
                (item, order) => new { item, order })
            .Join(
                db.StorePriceVersions.AsNoTracking(),
                itemOrder => itemOrder.item.IdPriceVersion,
                price => price.Id,
                (itemOrder, price) => new
                {
                    itemOrder.item,
                    itemOrder.order,
                    UnitAmount = price.FinalPriceAmount +
                        (db.StoreOrderAccessories
                            .Where(accessory => accessory.IdOrder == itemOrder.item.IdOrder &&
                                                accessory.IdProduct == itemOrder.item.IdProduct &&
                                                accessory.IdSalesPlan == itemOrder.item.IdSalesPlan)
                            .Select(accessory => (decimal?)accessory.PriceVersion.FinalPriceAmount)
                            .Sum() ?? 0m)
                })
            .Where(itemDetails =>
                itemDetails.order.CreatedAtUtc >= start.AddDays(-((end - start).Days)) &&
                itemDetails.order.CreatedAtUtc < start)
            .GroupBy(_ => 1)
            .Select(itemGroup => itemGroup.Sum(itemDetails =>
                itemDetails.UnitAmount * itemDetails.item.Quantity))
            .SingleOrDefaultAsync(cancellationToken);
        var values = await sold
            .OrderByDescending(product => product.Revenue)
            .ThenBy(product => product.ProductName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var stockQuery = db.StockBalances.AsNoTracking().Where(x => x.Warehouse.IsActive && x.Warehouse.Company.IsActive
            && x.Warehouse.IsSalesWarehouse && (fullAccess || allowedWarehouseIds.Contains(x.IdWarehouse)));
        if (warehouseId.HasValue)
            stockQuery = stockQuery.Where(x => x.IdWarehouse == warehouseId.Value);
        var stock = await stockQuery.GroupBy(x => x.InventoryItem.IdProduct)
            .Select(group => new { ProductId = group.Key, Quantity = group.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Quantity, cancellationToken);

        decimal revenue = values.Sum(x => x.Revenue);
        decimal totalRevenue = await sold.Select(x => x.Revenue).SumAsync(cancellationToken);
        int totalQuantity = await db.StoreCustomerOrderItems
            .AsNoTracking()
            .Join(
                PaidOrders(),
                item => item.IdOrder,
                order => order.Id,
                (item, order) => new { item, order })
            .Where(itemOrder => itemOrder.order.CreatedAtUtc >= start && itemOrder.order.CreatedAtUtc < end)
            .Select(itemOrder => itemOrder.item.Quantity)
            .SumAsync(cancellationToken);

        return new
        {
            items = values.Select(x => new
            {
                productId = x.ProductId,
                productName = x.ProductName,
                quantity = x.Quantity,
                revenue = x.Revenue,
                orderCount = x.OrderCount,
                stock = stock.GetValueOrDefault(x.ProductId)
            }),
            total,
            page,
            pageSize,
            hasNext = AdminListQuery.HasNext(page, pageSize, total),
            summary = new
            {
                quantity = totalQuantity, revenue = totalRevenue, productCount = total, previousRevenue = previous,
                revenueChangePercent = ChangePercent(totalRevenue, previous)
            }
        };
    }

    public async Task<object> ReadCustomersAsync(DateTime start, DateTime end, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        var currentOrders = PaidOrders().Where(x => x.CreatedAtUtc >= start && x.CreatedAtUtc < end);
        var grouped = currentOrders
            .Join(
                db.AccountProfiles.AsNoTracking(),
                order => order.IdProfile,
                profile => profile.Id,
                (order, profile) => new { order, profile })
            .GroupBy(orderProfile => new
            {
                orderProfile.profile.Id,
                orderProfile.profile.FirstName,
                orderProfile.profile.LastName,
                orderProfile.profile.Email
            })
            .Select(customerGroup => new
            {
                CustomerId = customerGroup.Key.Id,
                CustomerName = (customerGroup.Key.FirstName + " " + customerGroup.Key.LastName).Trim(),
                customerGroup.Key.Email,
                OrderCount = customerGroup.Count(),
                Revenue = customerGroup.Sum(orderProfile => orderProfile.order.TotalAmount),
                FirstPurchase = customerGroup.Min(orderProfile => orderProfile.order.CreatedAtUtc)
            });
        int total = await grouped.CountAsync(cancellationToken);
        var firstPurchase = PaidOrders().GroupBy(x => x.IdProfile).Select(group =>
            new { CustomerId = group.Key, FirstPurchase = group.Min(x => x.CreatedAtUtc) });
        int newBuyerCount =
            await firstPurchase.CountAsync(x => x.FirstPurchase >= start && x.FirstPurchase < end, cancellationToken);
        int repeatBuyerCount = await grouped.CountAsync(x => x.OrderCount > 1, cancellationToken);
        decimal revenue = await currentOrders.SumAsync(x => (decimal?)x.TotalAmount, cancellationToken) ?? 0;
        int previousBuyers = await PaidOrders()
            .Where(x => x.CreatedAtUtc >= start.AddDays(-((end - start).Days)) && x.CreatedAtUtc < start)
            .Select(x => x.IdProfile).Distinct().CountAsync(cancellationToken);
        var values = await grouped.OrderByDescending(x => x.Revenue).ThenBy(x => x.CustomerName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        decimal[] customerIds = values.Select(x => x.CustomerId).ToArray();
        Dictionary<decimal, DateTime> firstPurchases = await PaidOrders().Where(x => customerIds.Contains(x.IdProfile))
            .GroupBy(x => x.IdProfile)
            .Select(g => new { CustomerId = g.Key, FirstPurchase = g.Min(x => x.CreatedAtUtc) })
            .ToDictionaryAsync(x => x.CustomerId, x => x.FirstPurchase, cancellationToken);

        return new
        {
            items = values.Select(x => new
            {
                customerId = x.CustomerId,
                customerName = x.CustomerName,
                email = x.Email,
                orderCount = x.OrderCount,
                revenue = x.Revenue,
                firstPurchase = firstPurchases.GetValueOrDefault(x.CustomerId, x.FirstPurchase)
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            }),
            total,
            page,
            pageSize,
            hasNext = AdminListQuery.HasNext(page, pageSize, total),
            summary = new
                { buyerCount = total, newBuyerCount, repeatBuyerCount, revenue, previousBuyerCount = previousBuyers }
        };
    }

    public async Task<object> ReadInventoryAsync(DateTime start, DateTime end, long? warehouseId, int page, int pageSize, bool fullAccess, long[] allowedWarehouseIds, CancellationToken cancellationToken)
    {
        var balances = db.StockBalances.AsNoTracking().Where(x =>
            x.Warehouse.IsActive && x.Warehouse.Company.IsActive && x.Warehouse.IsSalesWarehouse
            && (fullAccess || allowedWarehouseIds.Contains(x.IdWarehouse)));
        if (warehouseId.HasValue) balances = balances.Where(x => x.IdWarehouse == warehouseId.Value);
        int total = await balances.CountAsync(cancellationToken);
        int lowStockCount = await balances.CountAsync(x => x.MinimumQuantity > 0 && x.Quantity <= x.MinimumQuantity,
            cancellationToken);
        int outOfStockCount = await balances.CountAsync(x => x.Quantity <= 0, cancellationToken);
        var values = await balances.OrderBy(x => x.Warehouse.Company.Name).ThenBy(x => x.Warehouse.Name)
            .ThenBy(x => x.InventoryItem.Product.NameFa).ThenBy(x => x.SalesPlanReference.DisplayName)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(x => new
            {
                warehouseId = x.IdWarehouse,
                inventoryItemId = x.IdInventoryItem,
                salesPlanReferenceId = x.IdSalesPlanReference,
                warehouseLabel = x.Warehouse.Company.Name + " / " + x.Warehouse.Name + " (" + x.Warehouse.Code + ")",
                productCode = x.InventoryItem.Product.ProductKey,
                productName = x.InventoryItem.Product.NameFa,
                variantName = x.InventoryItem.ProductVariant == null
                    ? "—"
                    : x.InventoryItem.ProductVariant.VariantName.Name,
                planName = x.SalesPlanReference.DisplayName,
                quantity = x.Quantity,
                minimumQuantity = x.MinimumQuantity,
                lastMovement = db.WarehouseStockMovements.Where(m =>
                        m.IdWarehouse == x.IdWarehouse && m.IdInventoryItem == x.IdInventoryItem &&
                        m.IdSalesPlanReference == x.IdSalesPlanReference)
                    .Select(m => (DateTime?)m.OccurredAtUtc).Max()
            }).ToListAsync(cancellationToken);
        var movements = await db.WarehouseStockMovements.AsNoTracking()
            .Where(x => x.OccurredAtUtc >= start && x.OccurredAtUtc < end &&
                        (fullAccess || allowedWarehouseIds.Contains(x.IdWarehouse)) &&
                        (!warehouseId.HasValue || x.IdWarehouse == warehouseId.Value))
            .GroupBy(_ => 1).Select(group => new
            {
                MovementIn = group.Where(x => x.QuantityDelta > 0).Sum(x => (decimal?)x.QuantityDelta) ?? 0,
                MovementOut = group.Where(x => x.QuantityDelta < 0).Sum(x => (decimal?)-x.QuantityDelta) ?? 0
            }).SingleOrDefaultAsync(cancellationToken);
        decimal quantityTotal = await balances.SumAsync(x => (decimal?)x.Quantity, cancellationToken) ?? 0;

        return new
        {
            items = values,
            total,
            page,
            pageSize,
            hasNext = AdminListQuery.HasNext(page, pageSize, total),
            warehouseAccess = fullAccess || allowedWarehouseIds.Length > 0,
            summary = new
            {
                rowCount = total,
                lowStockCount,
                outOfStockCount,
                quantityTotal,
                movementIn = movements?.MovementIn ?? 0,
                movementOut = movements?.MovementOut ?? 0
            }
        };
    }

    public async Task<object?> ReadDetailsAsync(string detail, long id, DateTime start, DateTime end, long? warehouseId, long? salesPlanReferenceId, int page, int pageSize, bool fullAccess, long[] allowedWarehouseIds, CancellationToken cancellationToken)
    {
        if (id <= 0) return null;

        object? result = detail switch
        {
            "product-orders" => await GetProductOrders(id, start, end, page, pageSize, cancellationToken),
            "customer-orders" => await GetCustomerOrders(id, start, end, page, pageSize, cancellationToken),
            "inventory-movements" => await GetInventoryMovements(id, start, end, warehouseId, salesPlanReferenceId, page, pageSize, fullAccess, allowedWarehouseIds, cancellationToken),
            _ => null
        };

        return result;
    }

    private async Task<object> GetProductOrders(long productId, DateTime start, DateTime end, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        var query = PaidOrders()
            .Where(order => order.CreatedAtUtc >= start && order.CreatedAtUtc < end)
            .Join(
                db.StoreCustomerOrderItems.AsNoTracking().Where(item => item.IdProduct == productId),
                order => order.Id,
                item => item.IdOrder,
                (order, item) => new { order, item })
            .Join(
                db.AccountProfiles.AsNoTracking(),
                orderItem => orderItem.order.IdProfile,
                profile => profile.Id,
                (orderItem, profile) => new { orderItem.order, orderItem.item, profile })
            .Join(
                db.StorePriceVersions.AsNoTracking(),
                orderItemProfile => orderItemProfile.item.IdPriceVersion,
                price => price.Id,
                (orderItemProfile, price) => new
                {
                    orderItemProfile.order,
                    orderItemProfile.item,
                    orderItemProfile.profile,
                    UnitAmount = price.FinalPriceAmount +
                        (db.StoreOrderAccessories
                            .Where(accessory => accessory.IdOrder == orderItemProfile.item.IdOrder &&
                                                accessory.IdProduct == orderItemProfile.item.IdProduct &&
                                                accessory.IdSalesPlan == orderItemProfile.item.IdSalesPlan)
                            .Select(accessory => (decimal?)accessory.PriceVersion.FinalPriceAmount)
                            .Sum() ?? 0m)
                })
            .OrderByDescending(orderItemDetails => orderItemDetails.order.CreatedAtUtc)
            .Select(orderItemDetails => new
            {
                orderId = orderItemDetails.order.Id,
                orderItemDetails.order.CreatedAtUtc,
                Customer = (orderItemDetails.profile.FirstName + " " + orderItemDetails.profile.LastName).Trim(),
                orderItemDetails.item.Quantity,
                Amount = orderItemDetails.UnitAmount * orderItemDetails.item.Quantity
            });
        int total = await query.CountAsync(cancellationToken);
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new
        {
            title = "سفارش‌های کالا",
            columns = DetailColumns(("orderNumber", "شماره سفارش"), ("date", "تاریخ"), ("customer", "مشتری"),
                ("quantity", "تعداد"), ("amount", "مبلغ (تومان)")),
            items = rows.Select(x => new
            {
                orderNumber = x.orderId.ToString(CultureInfo.InvariantCulture),
                date = x.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), customer = x.Customer,
                quantity = x.Quantity, amount = x.Amount
            }),
            total,
            page,
            pageSize,
            hasNext = AdminListQuery.HasNext(page, pageSize, total)
        };
    }

    private async Task<object> GetCustomerOrders(long customerId, DateTime start, DateTime end, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        var query = PaidOrders()
            .Where(x => x.IdProfile == customerId && x.CreatedAtUtc >= start && x.CreatedAtUtc < end)
            .OrderByDescending(x => x.CreatedAtUtc).Select(x => new
                { x.Id, x.CreatedAtUtc, Status = x.StatusReference.NameFa, x.TotalAmount });
        int total = await query.CountAsync(cancellationToken);
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new
        {
            title = "خریدهای مشتری",
            columns = DetailColumns(("orderNumber", "شماره سفارش"), ("date", "تاریخ"), ("status", "وضعیت"),
                ("amount", "مبلغ (تومان)")),
            items = rows.Select(x => new
            {
                orderNumber = x.Id.ToString(CultureInfo.InvariantCulture),
                date = x.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), status = x.Status,
                amount = x.TotalAmount
            }),
            total,
            page,
            pageSize,
            hasNext = AdminListQuery.HasNext(page, pageSize, total)
        };
    }

    private async Task<object> GetInventoryMovements(long inventoryItemId, DateTime start, DateTime end, long? warehouseId, long? salesPlanReferenceId, int page, int pageSize, bool fullAccess, long[] allowedWarehouseIds, CancellationToken cancellationToken)
    {
        var query = db.WarehouseStockMovements
            .AsNoTracking()
            .Join(
                db.Warehouses.AsNoTracking(),
                movement => movement.IdWarehouse,
                warehouse => warehouse.Id,
                (movement, warehouse) => new { movement, warehouse })
            .Join(
                db.Companies.AsNoTracking(),
                movementWarehouse => movementWarehouse.warehouse.IdCompany,
                company => company.Id,
                (movementWarehouse, company) => new { movementWarehouse.movement, movementWarehouse.warehouse,
                    company })
            .Where(movementDetails =>
                movementDetails.movement.IdInventoryItem == inventoryItemId &&
                movementDetails.movement.OccurredAtUtc >= start && movementDetails.movement.OccurredAtUtc < end &&
                (fullAccess || allowedWarehouseIds.Contains(movementDetails.movement.IdWarehouse)) &&
                (!warehouseId.HasValue || movementDetails.movement.IdWarehouse == warehouseId.Value) &&
                (!salesPlanReferenceId.HasValue ||
                 movementDetails.movement.IdSalesPlanReference == salesPlanReferenceId.Value))
            .OrderByDescending(movementDetails => movementDetails.movement.OccurredAtUtc)
            .Select(movementDetails => new
            {
                movementDetails.movement.OccurredAtUtc,
                movementDetails.movement.QuantityDelta,
                movementDetails.warehouse.Name,
                movementDetails.warehouse.Code,
                Company = movementDetails.company.Name,
                movementDetails.movement.IdVoucher
            });
        int total = await query.CountAsync(cancellationToken);
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new
        {
            title = "گردش موجودی کالا",
            columns = DetailColumns(("date", "تاریخ"), ("warehouse", "شرکت / انبار"), ("change", "تغییر موجودی"),
                ("voucherId", "شناسهٔ حواله")),
            items = rows.Select(x => new
            {
                date = x.OccurredAtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                warehouse = x.Company + " / " + x.Name + " (" + x.Code + ")", change = x.QuantityDelta,
                voucherId = x.IdVoucher
            }),
            total,
            page,
            pageSize,
            hasNext = AdminListQuery.HasNext(page, pageSize, total)
        };
    }


    private static object[] DetailColumns(params (string Key, string Label)[] columns) => columns.Select(column => (object)new { key = column.Key, label = column.Label }).ToArray();

    private IQueryable<StoreCustomerOrderReference> PaidOrders() => db.StoreCustomerOrders.AsNoTracking().Where(order =>
        SalesStatuses.Contains(order.StatusReference.NameEn) && db.StorePaymentAttempts.Any(payment =>
            payment.IdOrder == order.Id && payment.StatusReference.Code == "verified" &&
            payment.Amount == order.TotalAmount));

    private static decimal ChangePercent(decimal current, decimal previous) =>
        previous == 0 ? current == 0 ? 0 : 100 : (current - previous) / previous * 100;
}
