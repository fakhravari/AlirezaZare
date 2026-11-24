using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Admin.Infrastructure;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Domain.Warehouse.Entities;
using Shop.Data.Infrastructure.Persistence;
using Shop.Data.Helpers;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/reports")]
public sealed class StoreReportsAdminApiController(ShopDbContext db) : ControllerBase
{
    private static readonly string[] SalesStatuses =
    [
        "payment-confirmed", "warehouse-confirmed", "in-transit", "shipped-to-post", "delivered"
    ];

    [HttpGet]
    public async Task<IActionResult> GetReport(
        [FromQuery] string type = "sales",
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] long? warehouseId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        [FromQuery] string? detail = null,
        [FromQuery] long? id = null,
        [FromQuery] long? salesPlanReferenceId = null,
        CancellationToken cancellationToken = default)
    {
        if (type == "warehouses") return await GetWarehouses(cancellationToken);

        if (!AdminListQuery.TryCreate(page, pageSize, null, out _))
            return BadRequest(new { title = "شماره یا اندازهٔ صفحه معتبر نیست." });

        if (!TryGetRange(from, to, out DateTime start, out DateTime end, out string? error))
            return BadRequest(new { title = error });

        if (type is "products" or "inventory" || detail == "inventory-movements")
            warehouseId = await GetDefaultSalesWarehouseIdAsync(cancellationToken) ?? 0;
        if (!string.IsNullOrWhiteSpace(detail))
            return await GetDetails(detail, id, start, end, warehouseId, salesPlanReferenceId, page, pageSize,
                cancellationToken);

        object? result = type switch
        {
            "sales" => await GetSales(start, end, page, pageSize, cancellationToken),
            "products" => await GetProducts(start, end, warehouseId, page, pageSize, cancellationToken),
            "inventory" => await GetInventory(start, end, warehouseId, page, pageSize, cancellationToken),
            "customers" => await GetCustomers(start, end, page, pageSize, cancellationToken),
            _ => null
        };

        return result is null ? BadRequest(new { title = "نوع گزارش معتبر نیست." }) : Ok(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string type = "sales",
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] long? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetRange(from, to, out DateTime start, out DateTime end, out string? error))
            return BadRequest(new { title = error });

        if (type is "products" or "inventory")
            warehouseId = await GetDefaultSalesWarehouseIdAsync(cancellationToken) ?? 0;
        string[] keys = type switch
        {
            "sales" => ["date", "orderCount", "revenue", "discount"],
            "products" => ["productName", "quantity", "revenue", "orderCount", "stock"],
            "inventory" =>
            [
                "warehouseLabel", "productCode", "productName", "variantName", "planName", "quantity",
                "minimumQuantity", "lastMovement"
            ],
            "customers" => ["customerName", "email", "orderCount", "revenue", "firstPurchase"],
            _ => []
        };
        if (keys.Length == 0) return BadRequest(new { title = "نوع گزارش معتبر نیست." });

        string[] labels = keys.Select(ExportLabel).ToArray();

        return new AdminExcelFileResult($"store-{type}-report.xlsx", "گزارش", async (worksheet, ct) =>
        {
            await worksheet.WriteRowAsync(ct, labels);
            const int exportPageSize = AdminListQuery.ExportBatchSize;
            int page = 1;
            int total;
            do
            {
                ct.ThrowIfCancellationRequested();
                object report = type switch
                {
                    "sales" => await GetSales(start, end, page, exportPageSize, ct),
                    "products" => await GetProducts(start, end, warehouseId, page, exportPageSize, ct),
                    "inventory" => await GetInventory(start, end, warehouseId, page, exportPageSize, ct),
                    _ => await GetCustomers(start, end, page, exportPageSize, ct)
                };
                JsonElement payload = JsonSerializer.SerializeToElement(report);
                total = payload.GetProperty("total").GetInt32();
                foreach (JsonElement item in payload.GetProperty("items").EnumerateArray())
                    await worksheet.WriteRowAsync(ct, keys.Select(key => ExportValue(item, key)).ToArray());
                page++;
            } while ((long)(page - 1) * exportPageSize < total);
        });
    }

    private async Task<IActionResult> GetWarehouses(CancellationToken cancellationToken)
    {
        bool fullAccess = User.IsInRole("system.full-access");
        HashSet<long> allowed = AllowedWarehouseIds();
        var warehouses = await db.Warehouses.AsNoTracking()
            .Where(x => x.IsActive && x.Company.IsActive && x.IsSalesWarehouse &&
                        (fullAccess || allowed.Contains(x.Id)))
            .OrderBy(x => x.Company.Name).ThenBy(x => x.Name)
            .Select(x => new { x.Id, Label = x.Company.Name + " / " + x.Name + " (" + x.Code + ")" })
            .ToListAsync(cancellationToken);
        long? defaultWarehouseId = await GetDefaultSalesWarehouseIdAsync(cancellationToken);

        return Ok(new { items = warehouses, defaultWarehouseId, warehouseAccess = fullAccess || allowed.Count > 0 });
    }

    private async Task<long?> GetDefaultSalesWarehouseIdAsync(CancellationToken cancellationToken)
    {
        bool fullAccess = User.IsInRole("system.full-access");
        HashSet<long> allowed = AllowedWarehouseIds();
        long? configuredWarehouseId = await db.StoreSiteIdentities.AsNoTracking().Where(x => x.Id == 1)
            .Select(x => x.IdWarehouse).SingleOrDefaultAsync(cancellationToken);
        var warehouses = db.Warehouses.AsNoTracking().Where(x =>
            x.IsActive && x.Company.IsActive && x.IsSalesWarehouse && (fullAccess || allowed.Contains(x.Id)));
        if (configuredWarehouseId.HasValue &&
            await warehouses.AnyAsync(x => x.Id == configuredWarehouseId.Value, cancellationToken))
            return configuredWarehouseId;

        return await warehouses.OrderBy(x => x.Company.Name).ThenBy(x => x.Name).Select(x => (long?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<object> GetSales(DateTime start, DateTime end, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        TimeSpan duration = end - start;
        DateTime previousStart = start - duration;
        var orders = PaidOrders().Where(x => x.CreatedAtUtc >= start && x.CreatedAtUtc < end);
        var current = await orders.GroupBy(_ => 1).Select(group => new
        {
            OrderCount = group.Count(),
            Revenue = group.Sum(x => x.TotalAmount),
            Discount = group.Sum(x => x.CouponDiscountAmount)
        }).SingleOrDefaultAsync(cancellationToken);
        var previous = await PaidOrders().Where(x => x.CreatedAtUtc >= previousStart && x.CreatedAtUtc < start)
            .GroupBy(_ => 1).Select(group => new { Revenue = group.Sum(x => x.TotalAmount) })
            .SingleOrDefaultAsync(cancellationToken);
        int ordersWithoutVerifiedPayment = await db.StoreCustomerOrders.CountAsync(order =>
            order.CreatedAtUtc >= start && order.CreatedAtUtc < end &&
            SalesStatuses.Contains(order.StatusReference.Code)
            && !db.StorePaymentAttempts.Any(payment => payment.IdOrder == order.Id
                                                       && payment.StatusReference.Code == "verified" &&
                                                       payment.Amount == order.TotalAmount), cancellationToken);

        var daily = await orders.GroupBy(x => x.CreatedAtUtc.Date).Select(group => new
        {
            Date = group.Key,
            OrderCount = group.Count(),
            Revenue = group.Sum(x => x.TotalAmount),
            Discount = group.Sum(x => x.CouponDiscountAmount)
        }).OrderBy(x => x.Date).ToListAsync(cancellationToken);
        var items = daily.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new
        {
            date = x.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            orderCount = x.OrderCount,
            revenue = x.Revenue,
            discount = x.Discount
        }).ToArray();
        decimal revenue = current?.Revenue ?? 0;

        return new
        {
            items,
            total = daily.Count,
            page,
            pageSize,
            hasNext = AdminListQuery.HasNext(page, pageSize, daily.Count),
            summary = new
            {
                orderCount = current?.OrderCount ?? 0,
                revenue,
                discount = current?.Discount ?? 0,
                averageOrderAmount = current?.OrderCount > 0 ? revenue / current.OrderCount : 0,
                ordersWithoutVerifiedPayment,
                previousRevenue = previous?.Revenue ?? 0,
                revenueChangePercent = ChangePercent(revenue, previous?.Revenue ?? 0)
            }
        };
    }

    private async Task<object> GetProducts(DateTime start, DateTime end, long? warehouseId, int page, int pageSize,
        CancellationToken cancellationToken)
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
                db.StoreOrderItemAmounts.AsNoTracking(),
                itemOrderProduct => itemOrderProduct.item.Id,
                amount => amount.IdOrderItem,
                (itemOrderProduct, amount) => new { itemOrderProduct.item, itemOrderProduct.order,
                    itemOrderProduct.product, amount })
            .Where(itemDetails => itemDetails.order.CreatedAtUtc >= start && itemDetails.order.CreatedAtUtc < end)
            .GroupBy(itemDetails => new { itemDetails.item.IdProduct, ProductName = itemDetails.product.NameFa })
            .Select(productGroup => new
            {
                ProductId = productGroup.Key.IdProduct,
                ProductName = productGroup.Key.ProductName,
                Quantity = productGroup.Sum(itemDetails => itemDetails.item.Quantity),
                Revenue = productGroup.Sum(itemDetails =>
                    itemDetails.amount.UnitAmount * itemDetails.item.Quantity),
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
                db.StoreOrderItemAmounts.AsNoTracking(),
                itemOrder => itemOrder.item.Id,
                amount => amount.IdOrderItem,
                (itemOrder, amount) => new { itemOrder.item, itemOrder.order, amount })
            .Where(itemDetails =>
                itemDetails.order.CreatedAtUtc >= start.AddDays(-((end - start).Days)) &&
                itemDetails.order.CreatedAtUtc < start)
            .GroupBy(_ => 1)
            .Select(itemGroup => itemGroup.Sum(itemDetails =>
                itemDetails.amount.UnitAmount * itemDetails.item.Quantity))
            .SingleOrDefaultAsync(cancellationToken);
        var values = await sold
            .OrderByDescending(product => product.Revenue)
            .ThenBy(product => product.ProductName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        HashSet<long> allowed = AllowedWarehouseIds();
        bool fullAccess = User.IsInRole("system.full-access");
        var stockQuery = db.StockBalances.AsNoTracking().Where(x => x.Warehouse.IsActive && x.Warehouse.Company.IsActive
            && x.Warehouse.IsSalesWarehouse && (fullAccess || allowed.Contains(x.IdWarehouse)));
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

    private async Task<object> GetCustomers(DateTime start, DateTime end, int page, int pageSize,
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

    private async Task<object> GetInventory(DateTime start, DateTime end, long? warehouseId, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        bool fullAccess = User.IsInRole("system.full-access");
        HashSet<long> allowed = AllowedWarehouseIds();
        var balances = db.StockBalances.AsNoTracking().Where(x =>
            x.Warehouse.IsActive && x.Warehouse.Company.IsActive && x.Warehouse.IsSalesWarehouse
            && (fullAccess || allowed.Contains(x.IdWarehouse)));
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
                        (fullAccess || allowed.Contains(x.IdWarehouse)) &&
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
            warehouseAccess = fullAccess || allowed.Count > 0,
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

    private async Task<IActionResult> GetDetails(string detail, long? id, DateTime start, DateTime end,
        long? warehouseId, long? salesPlanReferenceId, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (id is null or <= 0) return BadRequest(new { title = "شناسهٔ ردیف گزارش معتبر نیست." });

        object? result = detail switch
        {
            "product-orders" => await GetProductOrders(id.Value, start, end, page, pageSize, cancellationToken),
            "customer-orders" => await GetCustomerOrders(id.Value, start, end, page, pageSize, cancellationToken),
            "inventory-movements" => await GetInventoryMovements(id.Value, start, end, warehouseId,
                salesPlanReferenceId, page, pageSize, cancellationToken),
            _ => null
        };

        return result is null ? BadRequest(new { title = "نوع جزئیات معتبر نیست." }) : Ok(result);
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
                db.StoreOrderItemAmounts.AsNoTracking(),
                orderItemProfile => orderItemProfile.item.Id,
                amount => amount.IdOrderItem,
                (orderItemProfile, amount) => new { orderItemProfile.order, orderItemProfile.item,
                    orderItemProfile.profile, amount })
            .OrderByDescending(orderItemDetails => orderItemDetails.order.CreatedAtUtc)
            .Select(orderItemDetails => new
            {
                orderItemDetails.order.OrderNumber,
                orderItemDetails.order.CreatedAtUtc,
                Customer = (orderItemDetails.profile.FirstName + " " + orderItemDetails.profile.LastName).Trim(),
                orderItemDetails.item.Quantity,
                Amount = orderItemDetails.amount.UnitAmount * orderItemDetails.item.Quantity
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
                orderNumber = x.OrderNumber,
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
                { x.OrderNumber, x.CreatedAtUtc, Status = x.StatusReference.NameFa, x.TotalAmount });
        int total = await query.CountAsync(cancellationToken);
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new
        {
            title = "خریدهای مشتری",
            columns = DetailColumns(("orderNumber", "شماره سفارش"), ("date", "تاریخ"), ("status", "وضعیت"),
                ("amount", "مبلغ (تومان)")),
            items = rows.Select(x => new
            {
                orderNumber = x.OrderNumber,
                date = x.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), status = x.Status,
                amount = x.TotalAmount
            }),
            total,
            page,
            pageSize,
            hasNext = AdminListQuery.HasNext(page, pageSize, total)
        };
    }

    private async Task<object> GetInventoryMovements(long inventoryItemId, DateTime start, DateTime end,
        long? warehouseId, long? salesPlanReferenceId, int page, int pageSize, CancellationToken cancellationToken)
    {
        bool fullAccess = User.IsInRole("system.full-access");
        HashSet<long> allowed = AllowedWarehouseIds();
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
                (fullAccess || allowed.Contains(movementDetails.movement.IdWarehouse)) &&
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

    private IQueryable<StoreCustomerOrderReference> PaidOrders() => db.StoreCustomerOrders.AsNoTracking().Where(order =>
        SalesStatuses.Contains(order.StatusReference.Code)
        && db.StorePaymentAttempts.Any(payment =>
            payment.IdOrder == order.Id && payment.StatusReference.Code == "verified" &&
            payment.Amount == order.TotalAmount));

    private HashSet<long> AllowedWarehouseIds() => User.FindAll("admin_warehouse")
        .Select(claim => long.TryParse(claim.Value, out long id) ? id : 0).Where(id => id > 0).ToHashSet();

    private static bool TryGetRange(DateOnly? from, DateOnly? to, out DateTime start, out DateTime end,
        out string? error)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly endDate = to ?? today;
        DateOnly startDate = from ?? endDate.AddDays(-29);
        start = DateTime.SpecifyKind(startDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        end = DateTime.SpecifyKind(endDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        if (startDate > endDate)
        {
            error = "تاریخ شروع باید قبل از تاریخ پایان باشد.";
            return false;
        }

        if ((endDate.DayNumber - startDate.DayNumber) > 365)
        {
            error = "بازهٔ گزارش نمی‌تواند بیشتر از ۳۶۶ روز باشد.";
            return false;
        }

        error = null;

        return true;
    }

    private static decimal ChangePercent(decimal current, decimal previous) =>
        previous == 0 ? current == 0 ? 0 : 100 : decimal.Round((current - previous) * 100 / previous, 1);

    private static object[] DetailColumns(params (string Key, string Label)[] columns) =>
        columns.Select(x => (object)new { key = x.Key, label = x.Label }).ToArray();

    private static string ExportLabel(string key) => key switch
    {
        "date" => "تاریخ",
        "orderCount" => "تعداد سفارش",
        "revenue" => "فروش / خرید (تومان)",
        "discount" => "تخفیف (تومان)",
        "productName" => "نام کالا",
        "quantity" => "تعداد",
        "stock" => "موجودی مجاز",
        "customerName" => "مشتری",
        "email" => "ایمیل",
        "firstPurchase" => "اولین خرید",
        "warehouseLabel" => "شرکت / انبار",
        "productCode" => "کد کالا",
        "variantName" => "تنوع",
        "planName" => "طرح",
        "minimumQuantity" => "حداقل موجودی",
        "lastMovement" => "آخرین گردش",
        _ => key
    };

    private static object? ExportValue(JsonElement item, string key)
    {
        if (!item.TryGetProperty(key, out JsonElement value) ||
            value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? "",
            JsonValueKind.Number when value.TryGetDecimal(out decimal number) => number,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => value.ToString()
        };
    }
}
