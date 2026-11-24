using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Admin.Infrastructure;
using System.Text.Json;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;
using System.Security.Claims;

using Shop.Data.Helpers;
namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/orders")]
public sealed class StoreOrdersAdminApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet("statuses")]
    public async Task<IActionResult> Statuses(CancellationToken cancellationToken) =>
        Ok(await db.OrderStatuses.AsNoTracking().OrderBy(x => x.Code).Select(x => new { x.Id, x.Code, x.NameFa, x.NameEn }).ToListAsync(cancellationToken));

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
        var order = await db.StoreCustomerOrders.Include(x => x.StatusReference).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (order is null || order.StatusReference.Code is not ("warehouse-confirmed" or "in-transit" or "shipped-to-post" or "delivered") ||
            order.IdVoucher is null ||
            !await db.StorePaymentAttempts.AnyAsync(p => p.IdOrder == id && p.StatusReference.Code == "verified" && p.Amount == order.TotalAmount, cancellationToken) ||
            order.ShippingCarrier != input.ExpectedCarrier || order.ShippingTrackingCode != input.ExpectedTrackingCode)
            return Conflict(new { title = "اطلاعات سفارش تغییر کرده یا سفارش شرایط ثبت رهگیری را ندارد." });

        var previousCarrier = order.ShippingCarrier;
        var previousTrackingCode = order.ShippingTrackingCode;
        order.ShippingCarrier = carrier; order.ShippingTrackingCode = code;
        db.StoreOrderShipmentTrackingHistories.Add(new StoreOrderShipmentTrackingHistory
        {
            IdOrder = id,
            PreviousCarrier = previousCarrier,
            PreviousTrackingCode = previousTrackingCode,
            Carrier = carrier,
            TrackingCode = code,
            ActorUserKey = actor,
            ChangedAtUtc = DateTime.UtcNow
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

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var order = await db.StoreCustomerOrders.Include(x => x.StatusReference).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (order is null || order.StatusReference.Code != input.ExpectedStatus || order.IdVoucher is null ||
            !await db.StorePaymentAttempts.AnyAsync(p => p.IdOrder == id && p.StatusReference.Code == "verified" && p.Amount == order.TotalAmount, cancellationToken))
            return Conflict(new { title = "وضعیت سفارش تغییر کرده یا پرداخت و حوالهٔ معتبر ندارد." });

        var nextStatus = await db.OrderStatuses.SingleOrDefaultAsync(x => x.Code == input.Status, cancellationToken);
        if (nextStatus is null) return BadRequest(new { title = "وضعیت سفارش در جدول مرجع پیدا نشد." });
        order.IdStatus = nextStatus.Id;
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Ok(new { message = "وضعیت ارسال ثبت شد." });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? q, [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize, [FromQuery] string? status = null,
        [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        if (!AdminListQuery.TryCreate(page, pageSize, q, out AdminPageRequest paging))
            return ValidationProblem(detail: "عبارت جست‌وجو یا صفحه‌بندی معتبر نیست.", statusCode: 400);

        string normalizedStatus = status?.Trim() ?? "";
        if (normalizedStatus.Length > 40 || normalizedStatus.Any(char.IsControl) ||
            normalizedStatus != "" && normalizedStatus is not ("pending-payment" or
                "payment-confirmed" or "warehouse-confirmed" or "in-transit" or "shipped-to-post" or "delivered" or "cancelled"))
            return ValidationProblem(detail: "وضعیت سفارش معتبر نیست.", statusCode: 400);

        if (from.HasValue && to.HasValue && from.Value > to.Value)
            return ValidationProblem(detail: "تاریخ شروع باید قبل از تاریخ پایان باشد.", statusCode: 400);

        if (to == DateOnly.MaxValue)
            return ValidationProblem(detail: "تاریخ پایان معتبر نیست.", statusCode: 400);

        if (from.HasValue && to.HasValue && to.Value.DayNumber - from.Value.DayNumber > 365)
            return ValidationProblem(detail: "بازهٔ تاریخ نمی‌تواند بیشتر از ۳۶۶ روز باشد.", statusCode: 400);

        q = paging.Search;
        DateTime? fromUtc = from.HasValue ? DateTime.SpecifyKind(from.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc) : null;
        DateTime? untilUtc = to.HasValue ? DateTime.SpecifyKind(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc) : null;
        var query = from o in db.StoreCustomerOrders.AsNoTracking()
                          join p in db.AccountProfiles.AsNoTracking() on o.IdProfile equals p.Id
                          where (q == "" || o.OrderNumber.Contains(q) || (p.FirstName + " " + p.LastName).Contains(q))
                              && (normalizedStatus == "" || o.StatusReference.Code == normalizedStatus)
                              && (!fromUtc.HasValue || o.CreatedAtUtc >= fromUtc.Value)
                              && (!untilUtc.HasValue || o.CreatedAtUtc < untilUtc.Value)
                          orderby o.Id descending
                          select new OrderRow
                          {
                              Id = o.Id,
                              IdStatus = o.IdStatus,
                              OrderNumber = o.OrderNumber,
                              Status = o.StatusReference.Code,
                              StatusName = o.StatusReference.NameFa,
                              PaymentMethod = o.PaymentMethod,
                              IdBank = o.IdBank,
                              BankName = o.Bank == null ? null : o.Bank.NameFa,
                              ItemCount = o.ItemCount,
                              TotalAmount = o.TotalAmount,
                              CreatedAtUtc = o.CreatedAtUtc,
                              CustomerName = (p.FirstName + " " + p.LastName).Trim()
                          };
        int total = await query.CountAsync(cancellationToken);
        var rows = await query.Skip(paging.Offset).Take(paging.PageSize).ToListAsync(cancellationToken);

        return Ok(AdminListQuery.CreateResult(rows, total, paging));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();

        var order = await (from o in db.StoreCustomerOrders.AsNoTracking().Where(x => x.Id == id)
                           join p in db.AccountProfiles.AsNoTracking() on o.IdProfile equals p.Id
                           select new OrderDetailsRow
                           {
                               Id = o.Id,
                               IdStatus = o.IdStatus,
                               OrderNumber = o.OrderNumber,
                               Status = o.StatusReference.Code,
                               StatusName = o.StatusReference.NameFa,
                               PaymentMethod = o.PaymentMethod,
                               IdBank = o.IdBank,
                               BankName = o.Bank == null ? null : o.Bank.NameFa,
                               ItemCount = o.ItemCount,
                               TotalAmount = o.TotalAmount,
                               SubtotalAmount = o.SubtotalAmount,
                               CouponDiscountAmount = o.CouponDiscountAmount,
                               CreatedAtUtc = o.CreatedAtUtc,
                               CustomerName = (p.FirstName + " " + p.LastName).Trim(),
                               IdVoucher = o.IdVoucher,
                               ShippingCarrier = o.ShippingCarrier,
                               ShippingTrackingCode = o.ShippingTrackingCode,
                               AddressSnapshotJson = o.AddressSnapshotJson
                           }).SingleOrDefaultAsync(cancellationToken);
        if (order is not null)
        {
            var itemRows = await (from i in db.StoreCustomerOrderItems.AsNoTracking().Where(x => x.IdOrder == id)
                                  join s in db.SalesPlanReferences.AsNoTracking() on i.IdSalesPlan equals s.Id
                                  join product in db.Products.AsNoTracking() on i.IdProduct equals product.Id
                                  orderby i.Id
                                  select new { i.Id, i.IdOrder, i.IdProduct, i.IdSalesPlan, ProductName = product.Name, VariantName = db.ProductVariants.Where(v => v.Id == i.IdProductVariant).Select(v => v.VariantName.TreePathNames).FirstOrDefault(), i.Quantity, i.IdProductVariant, i.IdPriceVersion, PriceAmount = i.PriceVersion.PriceBeforeDiscountAmount, i.PriceVersion.DiscountPercent, i.PriceVersion.FinalPriceAmount, i.PriceVersion.IsActive, i.PriceVersion.CreatedAtUtc, i.PriceVersion.ChangeReason, SalesPlanName = s.DisplayName })
                .ToListAsync(cancellationToken);
            var references = await (from r in db.StoreOrderAccessories.AsNoTracking()
                                    join v in db.StorePriceVersions.AsNoTracking() on r.IdPriceVersion equals v.Id
                                    where r.IdOrder == id
                                    orderby r.IdProduct, r.IdSalesPlan, r.IdAccessory
                                    select new { r.IdOrder, r.IdProduct, r.IdSalesPlan, r.IdPriceVersion, v.IdProductVariant, r.IdAccessory, PriceAmount = r.UnitAmountSnapshot, BaseAmount = v.PriceBeforeDiscountAmount, v.DiscountPercent, v.FinalPriceAmount, v.IsActive, v.CreatedAtUtc, v.ChangeReason, AccessoryName = r.AccessoryNameSnapshot })
                .ToListAsync(cancellationToken);
            var accessoryProductIds = references.Select(reference => reference.IdProduct).Distinct().ToArray();
            var accessoryIds = references.Select(reference => reference.IdAccessory).Distinct().ToArray();
            var accessoryRequirementRows = accessoryIds.Length == 0 ? [] : await db.StoreProductAccessories.AsNoTracking()
                .Where(link => accessoryProductIds.Contains(link.IdProduct) && accessoryIds.Contains(link.IdAccessory))
                .Select(link => new { link.IdProduct, link.IdAccessory, link.IsRequired }).ToListAsync(cancellationToken);
            var accessoryRequirements = accessoryRequirementRows.ToDictionary(link => (link.IdProduct, link.IdAccessory), link => link.IsRequired);
            var components = new List<object>();
            foreach (var item in itemRows)
            {
                components.Add(new
                {
                    productName = item.ProductName,
                    componentType = item.IdProductVariant.HasValue ? "تنوع" : "کالا",
                    accessoryRequirement = (string?)null,
                    componentId = item.IdProductVariant ?? item.IdProduct,
                    priceVersionId = item.IdPriceVersion,
                    treePathNames = item.IdProductVariant.HasValue ? item.VariantName : null,
                    accessoryName = (string?)null,
                    salesPlan = item.SalesPlanName,
                    quantity = item.Quantity,
                    baseAmount = item.PriceAmount,
                    discountPercent = item.DiscountPercent,
                    discountAmount = item.PriceAmount - item.FinalPriceAmount,
                    finalUnitAmount = item.FinalPriceAmount,
                    lineTotal = item.FinalPriceAmount * item.Quantity
                });
            }
            foreach (var accessory in references.GroupBy(reference => new { reference.IdProduct, reference.IdSalesPlan, reference.IdAccessory }))
            {
                var matchingItems = itemRows.Where(item => item.IdProduct == accessory.Key.IdProduct && item.IdSalesPlan == accessory.Key.IdSalesPlan).ToArray();
                var accessoryPrice = accessory.First();
                var hasRequirement = accessoryRequirements.TryGetValue((accessory.Key.IdProduct, accessory.Key.IdAccessory), out bool isRequired);
                components.Add(new
                {
                    productName = matchingItems.FirstOrDefault()?.ProductName,
                    componentType = "متعلقه",
                    accessoryRequirement = hasRequirement ? isRequired ? "الزامی (تعریف فعلی)" : "اختیاری (تعریف فعلی)" : null,
                    componentId = accessory.Key.IdAccessory,
                    priceVersionId = accessoryPrice.IdPriceVersion,
                    treePathNames = (string?)null,
                    accessoryName = accessoryPrice.AccessoryName,
                    salesPlan = matchingItems.FirstOrDefault()?.SalesPlanName,
                    quantity = matchingItems.Sum(item => item.Quantity),
                    baseAmount = accessoryPrice.BaseAmount,
                    discountPercent = accessoryPrice.DiscountPercent,
                    discountAmount = accessoryPrice.BaseAmount - accessoryPrice.PriceAmount,
                    finalUnitAmount = accessoryPrice.PriceAmount,
                    lineTotal = accessoryPrice.PriceAmount * matchingItems.Sum(item => item.Quantity)
                });
            }
            var items = itemRows.Select(i => new
            {
                name = i.ProductName,
                variantName = i.VariantName,
                quantity = i.Quantity,
                unitAmount = i.FinalPriceAmount + references.Where(r => r.IdOrder == i.IdOrder && r.IdProduct == i.IdProduct && r.IdSalesPlan == i.IdSalesPlan).Sum(r => r.PriceAmount),
                salesPlan = i.SalesPlanName,
                options = new { accessories = references.Where(r => r.IdOrder == i.IdOrder && r.IdProduct == i.IdProduct && r.IdSalesPlan == i.IdSalesPlan).Select(r => new { id = r.IdAccessory.ToString(), selected = true }).ToArray() },
                priceReferences = references.Where(r => r.IdOrder == i.IdOrder && r.IdProduct == i.IdProduct && r.IdSalesPlan == i.IdSalesPlan).Select(r => new
                {
                    priceVersionId = r.IdPriceVersion,
                    component = StorePriceVersionReference.GetComponent(r.IdProductVariant, r.IdAccessory),
                    name = r.AccessoryName,
                    calculatedUnitAmount = r.PriceAmount,
                    recordedPriceAmount = r.PriceAmount,
                    discountPercent = r.DiscountPercent,
                    isCurrent = r.IsActive,
                    recordedAtUtc = r.CreatedAtUtc,
                    reason = r.ChangeReason
                }).Prepend(new { priceVersionId = i.IdPriceVersion, component = i.IdProductVariant.HasValue ? "variant" : "product", name = i.VariantName ?? i.ProductName, calculatedUnitAmount = i.FinalPriceAmount, recordedPriceAmount = i.PriceAmount, discountPercent = i.DiscountPercent, isCurrent = i.IsActive, recordedAtUtc = i.CreatedAtUtc, reason = i.ChangeReason }).ToArray()
            }).ToList();
            order.CalculatedItemSubtotal = items.Sum(item => item.unitAmount * item.quantity);
            order.HasItemSubtotalMismatch = items.Count > 0 && order.CalculatedItemSubtotal != order.SubtotalAmount;
            order.ItemsJson = System.Text.Json.JsonSerializer.Serialize(items);
            order.ComponentsJson = System.Text.Json.JsonSerializer.Serialize(components);
        }

        return order is null ? NotFound() : Ok(order);
    }

    public class OrderRow
    {
        public long Id { get; set; }
        public int IdStatus { get; set; }
        public string OrderNumber { get; set; } = "";
        public string Status { get; set; } = "";
        public string StatusName { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public long IdBank { get; set; }
        public string? BankName { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string CustomerName { get; set; } = "";
    }

    public sealed class OrderDetailsRow : OrderRow
    {
        public decimal SubtotalAmount { get; set; }
        public decimal CouponDiscountAmount { get; set; }
        public decimal CalculatedItemSubtotal { get; set; }
        public bool HasItemSubtotalMismatch { get; set; }
        public long? IdVoucher { get; set; }
        public string? ShippingCarrier { get; set; }
        public string? ShippingTrackingCode { get; set; }
        public string AddressSnapshotJson { get; set; } = "{}";
        public string ItemsJson { get; set; } = "[]";
        public string ComponentsJson { get; set; } = "[]";
    }
    public sealed record ShippingStatusInput(string? ExpectedStatus, string? Status);
    public sealed record TrackingInput(string? Carrier, string? TrackingCode, string? ExpectedCarrier, string? ExpectedTrackingCode);
}
