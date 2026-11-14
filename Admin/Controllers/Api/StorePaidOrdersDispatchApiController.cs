using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Warehouse.Contracts;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Domain.Warehouse.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[Area("Store")]
[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/paid-orders-dispatch")]
public sealed class StorePaidOrdersDispatchApiController(ShopDbContext db, IWarehouseStockLedger stockLedger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var rows = await (from order in db.StoreCustomerOrders.AsNoTracking()
                          join profile in db.AccountProfiles.AsNoTracking() on order.IdProfile equals profile.Id
                          join payment in db.StorePaymentAttempts.AsNoTracking() on order.Id equals payment.IdOrder
                          join inbox in db.WarehousePaidOrderInboxes.AsNoTracking() on order.OrderNumber equals inbox.StoreOrderKey
                          where payment.Status == "verified" && payment.Amount == order.TotalAmount && inbox.Status == "P" &&
                                order.Status == "payment-confirmed" && order.IdWarehouseDocument == null &&
                                db.WarehousePaidOrderInboxLines.Any(line => line.IdPaidOrderInbox == inbox.Id)
                          orderby payment.PaidAtUtc, order.Id
                          select new PaidOrderRow
                          {
                              Id = order.Id, OrderNumber = order.OrderNumber, TotalAmount = order.TotalAmount,
                              ItemCount = order.ItemCount, PaidAtUtc = payment.PaidAtUtc!.Value,
                              CustomerName = (profile.FirstName + " " + profile.LastName).Trim()
                          }).Take(200).ToListAsync(cancellationToken);
        return Ok(new { items = rows });
    }

    [HttpPost]
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
            var payments = await db.StorePaymentAttempts.Where(x => orderIds.Contains(x.IdOrder) && x.Status == "verified")
                .ToListAsync(cancellationToken);
            var orderNumbers = orders.Select(x => x.OrderNumber).ToArray();
            var inboxes = await db.WarehousePaidOrderInboxes.Include(x => x.Lines)
                .Where(x => orderNumbers.Contains(x.StoreOrderKey) && x.Status == "P").ToListAsync(cancellationToken);
            var orderByNumber = orders.ToDictionary(x => x.OrderNumber);
            if (inboxes.Count != ids.Length || orders.Any(order => order.Status != "payment-confirmed" ||
                    order.IdWarehouseDocument is not null || (order.PaymentMethod == "demo" && order.ExternalOrderKey?.StartsWith("demo-order-") == true) ||
                    !payments.Any(payment => payment.IdOrder == order.Id && payment.Amount == order.TotalAmount) ||
                    !inboxes.Any(inbox => inbox.StoreOrderKey == order.OrderNumber && inbox.Lines.Count > 0)))
                return Conflict(new { detail = "وضعیت یکی از سفارش‌ها تغییر کرده است؛ فهرست را دوباره بارگذاری کنید." });

            var allLines = inboxes.SelectMany(inbox => inbox.Lines.Select(line => new { Inbox = inbox, Line = line })).ToList();
            long[] planIds = allLines.Select(x => x.Line.IdSalesPlanReference).Distinct().ToArray();
            var settings = await db.StoreSiteIdentities.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);
            var warehouse = settings?.IdWarehouse is long warehouseId
                ? await db.Warehouses.SingleOrDefaultAsync(x => x.Id == warehouseId && x.IsActive, cancellationToken)
                : null;
            var type = await db.WarehouseDocumentTypes.SingleOrDefaultAsync(x => x.Code == "SALES-DISPATCH" && x.IsActive && x.Direction == "I", cancellationToken);
            var party = await db.WarehouseParties.SingleOrDefaultAsync(x => x.ExternalPartyKey == "STORE-SALES" && x.IsActive, cancellationToken);
            if (warehouse is null || type is null || party is null)
                return Conflict(new { detail = "انبار فروش، نوع حواله یا مرجع آن تنظیم نشده است." });

            var assignedPlanIds = await db.WarehouseStockOwners.AsNoTracking()
                .Where(x => x.IdWarehouse == warehouse.Id && planIds.Contains(x.IdStockOwner))
                .Select(x => x.IdStockOwner).ToArrayAsync(cancellationToken);
            if (assignedPlanIds.Length != planIds.Length)
                return Conflict(new { detail = "طرح یکی از اقلام به انبار فروش متصل نیست." });

            var inboxIds = inboxes.Select(x => x.Id).ToArray();
            var lineDetails = await (from line in db.WarehousePaidOrderInboxLines.AsNoTracking().Where(x => inboxIds.Contains(x.IdPaidOrderInbox))
                                     join item in db.InventoryItems.AsNoTracking() on line.IdInventoryItem equals item.Id
                                     join product in db.Products.AsNoTracking() on item.IdProduct equals product.Id
                                     join unit in db.WarehouseUnits.AsNoTracking() on line.IdUnit equals unit.Id
                                     join variant0 in db.ProductVariants.AsNoTracking() on item.IdProductVariant equals variant0.Id into variants
                                     from variant in variants.DefaultIfEmpty()
                                     select new
                                     {
                                         line.IdInventoryItem, line.IdUnit, line.IdSalesPlanReference,
                                         line.Quantity, line.ConversionFactor, line.BaseQuantity,
                                         ProductName = product.Name, VariantName = variant == null ? null : variant.Name,
                                         UnitName = unit.Name
                                     }).ToListAsync(cancellationToken);

            var headerPlanId = lineDetails.Min(x => x.IdSalesPlanReference);
            var document = new WarehouseDocumentRecord
            {
                IdWarehouse = warehouse.Id, IdDocumentType = type.Id, IdParty = party.Id,
                IdSalesPlanReference = headerPlanId, DocumentDate = DateOnly.FromDateTime(DateTime.Today),
                Status = "F", Reason = "حوالهٔ تجمیعی سفارش‌های فروشگاه", CreatedByUserKey = actor,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.WarehouseDocuments.Add(document);
            await db.SaveChangesAsync(cancellationToken);

            foreach (var group in lineDetails.GroupBy(x => new
                         { x.IdInventoryItem, x.IdUnit, x.IdSalesPlanReference, x.ConversionFactor, x.ProductName, x.VariantName, x.UnitName }))
            {
                db.WarehouseDocumentLines.Add(new WarehouseDocumentLineRecord
                {
                    IdDocument = document.Id, IdInventoryItem = group.Key.IdInventoryItem,
                    IdUnit = group.Key.IdUnit, IdSalesPlanReference = group.Key.IdSalesPlanReference,
                    Quantity = group.Sum(x => x.Quantity), ConversionFactor = group.Key.ConversionFactor,
                    BaseQuantity = group.Sum(x => x.BaseQuantity), ProductNameSnapshot = group.Key.ProductName,
                    VariantNameSnapshot = group.Key.VariantName, UnitNameSnapshot = group.Key.UnitName
                });
            }
            await db.SaveChangesAsync(cancellationToken);
            await stockLedger.RebuildDocumentAsync(document.Id, cancellationToken);

            foreach (var inbox in inboxes)
            {
                inbox.Status = "C";
                var order = orderByNumber[inbox.StoreOrderKey];
                order.IdWarehouseDocument = document.Id;
                order.Status = "warehouse-confirmed";
                db.WarehousePaidOrderInboxDocuments.Add(new WarehousePaidOrderInboxDocument
                {
                    IdPaidOrderInbox = inbox.Id, IdWarehouseDocument = document.Id
                });
            }
            db.WarehouseDocumentAudits.Add(new WarehouseDocumentAudit
            {
                DocumentId = document.Id, DocumentNumber = document.DocumentNumber, WarehouseId = warehouse.Id,
                Action = "Created", ActorUserKey = actor, OccurredAtUtc = DateTime.UtcNow,
                Reason = document.Reason, AfterJson = JsonSerializer.Serialize(new { orderIds = ids })
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
    }
    public sealed record DispatchInput(long[]? OrderIds);
}
