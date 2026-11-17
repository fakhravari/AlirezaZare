using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Admin.Infrastructure;
using Shop.Data.Application.Warehouse.Contracts;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Domain.Warehouse.Entities;
using Shop.Data.Infrastructure.Persistence;

using Shop.Data.Helpers;
namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Authorize]
[Route("api/admin/store/paid-orders-dispatch")]
public sealed class StorePaidOrdersDispatchApiController(ShopDbContext db, IWarehouseStockLedger stockLedger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool warehouseQueue = false,
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!User.IsInRole("system.full-access"))
        {
            if (!warehouseQueue) return Forbid();

            var warehouseId = await db.StoreSiteIdentities.AsNoTracking().Where(x => x.Id == 1) .Select(x => x.IdWarehouse).SingleOrDefaultAsync(cancellationToken);
            if (warehouseId is null || !User.HasClaim("admin_warehouse", warehouseId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)))
                return Forbid();
        }
        if (!AdminListQuery.TryCreate(page, pageSize, q, out AdminPageRequest paging))
            return BadRequest(new { title = "عبارت جست‌وجو یا صفحه‌بندی معتبر نیست." });

        var eligible = from order in db.StoreCustomerOrders.AsNoTracking()
                       join profile in db.AccountProfiles.AsNoTracking() on order.IdProfile equals profile.Id
                       where order.Status == "payment-confirmed" && order.IdWarehouseDocument == null &&
                             ((warehouseQueue && order.PaymentMethod == "demo" && order.ExternalOrderKey != null && order.ExternalOrderKey.StartsWith("demo-order-")) ||
                              (!(order.PaymentMethod == "demo" && order.ExternalOrderKey != null && order.ExternalOrderKey.StartsWith("demo-order-")) &&
                               db.StorePaymentAttempts.Any(payment => payment.IdOrder == order.Id && payment.Status == "verified" &&
                                   payment.Amount == order.TotalAmount && payment.PaidAtUtc != null))) &&
                             db.StoreCustomerOrderItems.Any(item => item.IdCustomerOrder == order.Id)
                       select new { order, profile };
        if (paging.Search.Length > 0)
            eligible = eligible.Where(x => x.order.OrderNumber.Contains(paging.Search) || (x.profile.FirstName + " " + x.profile.LastName).Contains(paging.Search));
        int total = await eligible.CountAsync(cancellationToken);
        var rows = await eligible.Select(x => new
            {
                x.order,
                x.profile,
                IsSample = x.order.PaymentMethod == "demo" && x.order.ExternalOrderKey != null && x.order.ExternalOrderKey.StartsWith("demo-order-"),
                PaidAtUtc = db.StorePaymentAttempts.AsNoTracking().Where(payment => payment.IdOrder == x.order.Id && payment.Status == "verified" &&
                        payment.Amount == x.order.TotalAmount && payment.PaidAtUtc != null)
                    .OrderBy(payment => payment.PaidAtUtc).Select(payment => payment.PaidAtUtc).FirstOrDefault() ?? x.order.OrderedAtUtc
            }).OrderBy(x => x.PaidAtUtc).ThenBy(x => x.order.Id).Skip(paging.Offset).Take(paging.PageSize).Select(x => new PaidOrderRow
            {
                Id = x.order.Id,
                OrderNumber = x.order.OrderNumber,
                TotalAmount = x.order.TotalAmount,
                ItemCount = x.order.ItemCount,
                PaidAtUtc = x.PaidAtUtc,
                CustomerName = (x.profile.FirstName + " " + x.profile.LastName).Trim(),
                IsSample = x.IsSample
            }).ToListAsync(cancellationToken);

        return Ok(AdminListQuery.CreateResult(rows, total, paging));
    }

    [HttpPost]
    [Authorize(Roles = "system.full-access")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] DispatchInput input, CancellationToken cancellationToken)
    {
        if (input.OrderIds is null || input.OrderIds.Length is < 1 or > 200 || input.OrderIds.Any(id => id <= 0) ||
            input.OrderIds.Distinct().Count() != input.OrderIds.Length)
            return BadRequest(new { detail = "سفارش‌های انتخاب‌شده معتبر نیستند." });

        string actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "system";
        if (actor.Length > 128) return BadRequest(new { detail = "شناسهٔ مدیر نامعتبر است." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            long[] ids = input.OrderIds;
            var orders = await db.StoreCustomerOrders.Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
            if (orders.Count != ids.Length) return Conflict(new { detail = "وضعیت یکی از سفارش‌ها تغییر کرده است." });

            var orderIds = orders.Select(x => x.Id).ToArray();
            var payments = await db.StorePaymentAttempts.Where(x => orderIds.Contains(x.IdOrder) && x.Status == "verified" && x.PaidAtUtc != null) .ToListAsync(cancellationToken);
            var orderItems = await db.StoreCustomerOrderItems.AsNoTracking().Where(x => orderIds.Contains(x.IdCustomerOrder)).ToListAsync(cancellationToken);
            if (orders.Any(order => order.Status != "payment-confirmed" ||
                    order.IdWarehouseDocument is not null ||
                    (!(order.PaymentMethod == "demo" && order.ExternalOrderKey?.StartsWith("demo-order-") == true) &&
                     !payments.Any(payment => payment.IdOrder == order.Id && payment.Amount == order.TotalAmount)) ||
                    !orderItems.Any(item => item.IdCustomerOrder == order.Id)))
                return Conflict(new { detail = "وضعیت یکی از سفارش‌ها تغییر کرده است؛ فهرست را دوباره بارگذاری کنید." });

            long[] planIds = orderItems.Select(x => x.IdSalesPlan).Distinct().ToArray();
            var settings = await db.StoreSiteIdentities.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);
            var warehouse = settings?.IdWarehouse is long warehouseId ? await db.Warehouses.SingleOrDefaultAsync(x => x.Id == warehouseId && x.IsActive, cancellationToken) : null;
            var type = await db.WarehouseDocumentTypes.SingleOrDefaultAsync(x => x.Code == "SALES-DISPATCH" && x.IsActive && x.Direction == "I", cancellationToken);
            var party = await db.WarehouseParties.SingleOrDefaultAsync(x => x.ExternalPartyKey == "STORE-SALES" && x.IsActive, cancellationToken);
            if (warehouse is null || type is null || party is null)
                return Conflict(new { detail = "انبار فروش، نوع حواله یا مرجع آن تنظیم نشده است." });

            var assignedPlanIds = await db.WarehouseStockOwners.AsNoTracking().Where(x => x.IdWarehouse == warehouse.Id && planIds.Contains(x.IdStockOwner)).Select(x => x.IdStockOwner).ToArrayAsync(cancellationToken);
            if (assignedPlanIds.Length != planIds.Length)
                return Conflict(new { detail = "طرح یکی از اقلام به انبار فروش متصل نیست." });

            long[] productIds = orderItems.Select(x => x.IdProduct).Distinct().ToArray();
            long[] variantIds = orderItems.Where(x => x.IdProductVariant.HasValue).Select(x => x.IdProductVariant!.Value).Distinct().ToArray();
            var products = await db.Products.AsNoTracking().Where(x => productIds.Contains(x.Id))
                .Select(x => new { x.Id, x.IdBaseUnit }).ToDictionaryAsync(x => x.Id, cancellationToken);
            var inventoryItems = await db.InventoryItems.AsNoTracking().Where(x => productIds.Contains(x.IdProduct))
                .Select(x => new { x.Id, x.IdProduct, x.IdProductVariant }).ToListAsync(cancellationToken);
            long[] unitIds = products.Values.Select(product => product.IdBaseUnit).Distinct().ToArray();
            var unitNames = await db.WarehouseUnits.AsNoTracking().Where(x => unitIds.Contains(x.Id)).Select(x => new { x.Id, x.Name }).ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
            var variantNames = await db.ProductVariants.AsNoTracking().Where(x => variantIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name }).ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
            var inventoryByProductVariant = inventoryItems.GroupBy(x => (x.IdProduct, x.IdProductVariant)).ToDictionary(x => x.Key, x => x.ToArray());
            var lineDetails = new List<DispatchLineDetail>(orderItems.Count);
            foreach (var line in orderItems)
            {
                var key = (line.IdProduct, line.IdProductVariant);
                if (!products.TryGetValue(line.IdProduct, out var product) ||
                    !unitNames.TryGetValue(product.IdBaseUnit, out string? unitName) ||
                    !inventoryByProductVariant.TryGetValue(key, out var matches) || matches.Length != 1)
                    return Conflict(new { detail = "برای یکی از اقلام سفارش، کالا یا واحد انبار متناظر یکتا پیدا نشد." });

                lineDetails.Add(new DispatchLineDetail
                {
                    IdCustomerOrder = line.IdCustomerOrder,
                    IdInventoryItem = matches[0].Id,
                    IdUnit = product.IdBaseUnit,
                    IdSalesPlanReference = line.IdSalesPlan,
                    Quantity = line.Quantity,
                    ConversionFactor = 1,
                    BaseQuantity = line.Quantity,
                    ProductName = line.ProductNameSnapshot,
                    VariantName = line.IdProductVariant is long variantId ? variantNames.GetValueOrDefault(variantId) : null,
                    UnitName = unitName
                });
            }

            var headerPlanId = lineDetails.Min(x => x.IdSalesPlanReference);
            var document = new WarehouseDocumentRecord
            {
                IdWarehouse = warehouse.Id,
                IdDocumentType = type.Id,
                IdParty = party.Id,
                IdSalesPlanReference = headerPlanId,
                DocumentDate = DateOnly.FromDateTime(DateTime.Today),
                Status = "F",
                Reason = "حوالهٔ تجمیعی سفارش‌های فروشگاه",
                CreatedByUserKey = actor,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.WarehouseDocuments.Add(document);
            await db.SaveChangesAsync(cancellationToken);

            foreach (var group in lineDetails.GroupBy(x => new
            { x.IdInventoryItem, x.IdUnit, x.IdSalesPlanReference, x.ConversionFactor, x.ProductName, x.VariantName, x.UnitName }))
            {
                db.WarehouseDocumentLines.Add(new WarehouseDocumentLineRecord
                {
                    IdDocument = document.Id,
                    IdInventoryItem = group.Key.IdInventoryItem,
                    IdUnit = group.Key.IdUnit,
                    IdSalesPlanReference = group.Key.IdSalesPlanReference,
                    Quantity = group.Sum(x => x.Quantity),
                    ConversionFactor = group.Key.ConversionFactor,
                    BaseQuantity = group.Sum(x => x.BaseQuantity),
                    ProductNameSnapshot = group.Key.ProductName,
                    VariantNameSnapshot = group.Key.VariantName,
                    UnitNameSnapshot = group.Key.UnitName
                });
            }
            await db.SaveChangesAsync(cancellationToken);

            await stockLedger.RebuildDocumentAsync(document.Id, cancellationToken);

            foreach (var order in orders)
            {
                order.IdWarehouseDocument = document.Id;
                order.Status = "warehouse-confirmed";
            }
            db.WarehouseDocumentAudits.Add(new WarehouseDocumentAudit
            {
                DocumentId = document.Id,
                DocumentNumber = document.DocumentNumber,
                WarehouseId = warehouse.Id,
                Action = "Created",
                ActorUserKey = actor,
                OccurredAtUtc = DateTime.UtcNow,
                Reason = document.Reason,
                AfterJson = JsonSerializer.Serialize(new { orderIds = ids })
            });
            await db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return Ok(new { document.Id, document.DocumentNumber });
        }
        catch (InsufficientWarehouseStockException)
        {
            return Conflict(new { detail = "موجودی برای صدور حواله کافی نیست." });
        }
        catch (DbUpdateException exception) when (exception.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 or 547 })
        {
            return Conflict(new { detail = "وضعیت یکی از سفارش‌ها یا وابستگی‌های انبار تغییر کرده است." });
        }
    }

    public sealed class PaidOrderRow
    {
        public long Id { get; set; }
        public string OrderNumber { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PaidAtUtc { get; set; }
        public bool IsSample { get; set; }
    }
    public sealed record DispatchInput(long[]? OrderIds);

    private sealed class DispatchLineDetail
    {
        public long IdCustomerOrder { get; init; }
        public long IdInventoryItem { get; init; }
        public long IdUnit { get; init; }
        public long IdSalesPlanReference { get; init; }
        public decimal Quantity { get; init; }
        public decimal ConversionFactor { get; init; }
        public decimal BaseQuantity { get; init; }
        public string ProductName { get; init; } = "";
        public string? VariantName { get; init; }
        public string UnitName { get; init; } = "";
    }
}
