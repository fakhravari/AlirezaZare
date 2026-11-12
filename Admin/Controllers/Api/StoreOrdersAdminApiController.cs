using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Infrastructure.Persistence;
using System.Security.Claims;

namespace Shop.Admin.Controllers.Api;

[Area("Store")]
[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/orders")]
public sealed class StoreOrdersAdminApiController(ShopDbContext db) : ControllerBase
{
    [HttpPut("{id:long}/tracking")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTracking(long id, [FromBody] TrackingInput input, CancellationToken cancellationToken)
    {
        string carrier = input.Carrier?.Trim() ?? "", code = input.TrackingCode?.Trim() ?? "";
        if (id <= 0 || carrier.Length is < 1 or > 100 || code.Length is < 1 or > 100 ||
            carrier.Any(char.IsControl) || code.Any(char.IsControl) ||
            (input.ExpectedCarrier?.Length ?? 0) > 100 || (input.ExpectedTrackingCode?.Length ?? 0) > 100)
            return BadRequest(new { title = "شرکت حمل یا کد رهگیری معتبر نیست." });
        string actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "system";
        if (actor.Length > 256) return BadRequest(new { title = "شناسهٔ مدیر نامعتبر است." });
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var order = await db.StoreCustomerOrders.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (order is null || order.Status is not ("warehouse-confirmed" or "in-transit" or "shipped-to-post" or "delivered") ||
            order.IdWarehouseDocument is null || (order.PaymentMethod == "demo" && order.ExternalOrderKey?.StartsWith("demo-order-") == true) ||
            !await db.StorePaymentAttempts.AnyAsync(p => p.IdOrder == id && p.Status == "verified" && p.Amount == order.TotalAmount, cancellationToken) ||
            order.ShippingCarrier != input.ExpectedCarrier || order.ShippingTrackingCode != input.ExpectedTrackingCode)
            return Conflict(new { title = "اطلاعات سفارش تغییر کرده یا سفارش شرایط ثبت رهگیری را ندارد." });
        var previousCarrier = order.ShippingCarrier;
        var previousTrackingCode = order.ShippingTrackingCode;
        order.ShippingCarrier = carrier; order.ShippingTrackingCode = code;
        db.StoreOrderShipmentTrackingHistories.Add(new StoreOrderShipmentTrackingHistory
        {
            IdOrder = id, PreviousCarrier = previousCarrier, PreviousTrackingCode = previousTrackingCode,
            Carrier = carrier, TrackingCode = code, ActorUserKey = actor, ChangedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "اطلاعات رهگیری ثبت شد." });
    }

    [HttpPut("{id:long}/shipping-status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateShippingStatus(long id, [FromBody] ShippingStatusInput input, CancellationToken cancellationToken)
    {
        if (id <= 0 || input.ExpectedStatus is null || input.Status is null ||
            !((input.ExpectedStatus == "warehouse-confirmed" && input.Status is "in-transit" or "shipped-to-post") ||
              (input.ExpectedStatus is "in-transit" or "shipped-to-post" && input.Status == "delivered")))
            return BadRequest(new { title = "تغییر وضعیت ارسال مجاز نیست." });
        string actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "system";
        if (actor.Length > 256) return BadRequest(new { title = "شناسهٔ مدیر نامعتبر است." });
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var order = await db.StoreCustomerOrders.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (order is null || order.Status != input.ExpectedStatus || order.IdWarehouseDocument is null ||
            (order.PaymentMethod == "demo" && order.ExternalOrderKey?.StartsWith("demo-order-") == true) ||
            !await db.StorePaymentAttempts.AnyAsync(p => p.IdOrder == id && p.Status == "verified" && p.Amount == order.TotalAmount, cancellationToken))
            return Conflict(new { title = "وضعیت سفارش تغییر کرده یا پرداخت و حوالهٔ معتبر ندارد." });
        order.Status = input.Status;
        db.StoreOrderShippingHistories.Add(new StoreOrderShippingHistory
        {
            IdOrder = id, PreviousStatus = input.ExpectedStatus, NewStatus = input.Status,
            ActorUserKey = actor, ChangedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "وضعیت ارسال ثبت شد." });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? q, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        q = q?.Trim() ?? "";
        if (q.Length > 100 || page is < 1 or > 100000 || pageSize is < 1 or > 100)
            return BadRequest(new { title = "جست‌وجو یا صفحه‌بندی نامعتبر است." });
        int offset = (page - 1) * pageSize;
        int take = pageSize + 1;
        var rows = await (from o in db.StoreCustomerOrders.AsNoTracking()
                          join p in db.AccountProfiles.AsNoTracking() on o.IdProfile equals p.Id
                          where q == "" || o.OrderNumber.Contains(q) || (p.FirstName + " " + p.LastName).Contains(q)
                          orderby o.Id descending
                          select new OrderRow
                          {
                              Id = o.Id, OrderNumber = o.OrderNumber, Status = o.Status, PaymentMethod = o.PaymentMethod,
                              IdBank = o.IdBank, BankName = o.Bank == null ? null : o.Bank.NameFa,
                              ItemCount = o.ItemCount, TotalAmount = o.TotalAmount, OrderedAtUtc = o.OrderedAtUtc,
                              IsSample = o.PaymentMethod == "demo" && o.ExternalOrderKey != null && o.ExternalOrderKey.StartsWith("demo-order-"),
                              CustomerName = (p.FirstName + " " + p.LastName).Trim()
                          }).Skip(offset).Take(take).ToListAsync(cancellationToken);
        return Ok(new { items = rows.Take(pageSize), page, hasNext = rows.Count > pageSize });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();
        var order = await (from o in db.StoreCustomerOrders.AsNoTracking().Where(x => x.Id == id)
                           join p in db.AccountProfiles.AsNoTracking() on o.IdProfile equals p.Id
                           select new OrderDetailsRow
                           {
                               Id = o.Id, OrderNumber = o.OrderNumber, Status = o.Status, PaymentMethod = o.PaymentMethod,
                               IdBank = o.IdBank, BankName = o.Bank == null ? null : o.Bank.NameFa,
                               ItemCount = o.ItemCount, TotalAmount = o.TotalAmount, OrderedAtUtc = o.OrderedAtUtc,
                               IsSample = o.PaymentMethod == "demo" && o.ExternalOrderKey != null && o.ExternalOrderKey.StartsWith("demo-order-"),
                               CustomerName = (p.FirstName + " " + p.LastName).Trim(), IdWarehouseDocument = o.IdWarehouseDocument,
                               ShippingCarrier = o.ShippingCarrier, ShippingTrackingCode = o.ShippingTrackingCode,
                               AddressSnapshotJson = o.AddressSnapshotJson
                           }).SingleOrDefaultAsync(cancellationToken);
        if (order is not null)
        {
            var itemRows = await (from i in db.StoreCustomerOrderItems.AsNoTracking().Where(x => x.IdCustomerOrder == id)
                                  join s in db.SalesPlanReferences.AsNoTracking() on i.IdSalesPlan equals s.Id
                                  orderby i.Id
                                  select new { i.Id, i.ProductNameSnapshot, i.Quantity, i.UnitAmount, s.StoreSalesPlanKey, i.OptionsJson })
                .ToListAsync(cancellationToken);
            var references = await (from r in db.StoreOrderItemPriceReferences.AsNoTracking()
                                    join v in db.StorePriceVersions.AsNoTracking() on r.IdPriceVersion equals v.Id
                                    where itemRows.Select(i => i.Id).Contains(r.IdOrderItem)
                                    orderby r.IdOrderItem, r.IdPriceVersion
                                    select new { r.IdOrderItem, r.IdPriceVersion, v.ScopeKey, v.PriceKind, v.PriceAmount, v.DiscountPercent, v.IsActive, v.CreatedAtUtc, v.ChangeReason })
                .ToListAsync(cancellationToken);
            var items = itemRows.Select(i => new
            {
                name = i.ProductNameSnapshot, quantity = i.Quantity, unitAmount = i.UnitAmount,
                salesPlan = i.StoreSalesPlanKey, options = JsonDocument.Parse(i.OptionsJson).RootElement.Clone(),
                priceReferences = references.Where(r => r.IdOrderItem == i.Id).Select(r => new { priceVersionId = r.IdPriceVersion, component = r.PriceKind, calculatedUnitAmount = r.PriceKind is "option" or "accessory" or "order-snapshot" ? r.PriceAmount : decimal.Round(r.PriceAmount * (100m - r.DiscountPercent) / 100m, 0, MidpointRounding.AwayFromZero), scope = r.ScopeKey, priceKind = r.PriceKind, recordedPriceAmount = r.PriceAmount, discountPercent = r.DiscountPercent, isCurrent = r.IsActive, recordedAtUtc = r.CreatedAtUtc, reason = r.ChangeReason }).ToArray()
            }).ToList();
            var history = await db.StoreOrderShippingHistories.AsNoTracking().Where(x => x.IdOrder == id)
                .OrderByDescending(x => x.Id).Select(x => new { x.PreviousStatus, x.NewStatus, x.ActorUserKey, x.ChangedAtUtc })
                .ToListAsync(cancellationToken);
            order.ItemsJson = System.Text.Json.JsonSerializer.Serialize(items);
            order.ShippingHistoryJson = System.Text.Json.JsonSerializer.Serialize(history);
        }
        return order is null ? NotFound() : Ok(order);
    }

    public class OrderRow
    {
        public long Id { get; set; }
        public string OrderNumber { get; set; } = "";
        public string Status { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public long IdBank { get; set; }
        public string? BankName { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime OrderedAtUtc { get; set; }
        public string CustomerName { get; set; } = "";
        public bool IsSample { get; set; }
    }

    public sealed class OrderDetailsRow : OrderRow
    {
        public long? IdWarehouseDocument { get; set; }
        public string? ShippingCarrier { get; set; }
        public string? ShippingTrackingCode { get; set; }
        public string AddressSnapshotJson { get; set; } = "{}";
        public string ItemsJson { get; set; } = "[]";
        public string ShippingHistoryJson { get; set; } = "[]";
    }
    public sealed record ShippingStatusInput(string? ExpectedStatus, string? Status);
    public sealed record TrackingInput(string? Carrier, string? TrackingCode, string? ExpectedCarrier, string? ExpectedTrackingCode);
}
