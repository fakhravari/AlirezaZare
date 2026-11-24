using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Helpers.Extensions;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Controllers.Api;

[ApiController]
[AllowAnonymous]
[Route("api/store/orders/tracking")]
public sealed class OrderTrackingApiController(ShopDbContext db) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("order-tracking")]
    public async Task<IActionResult> Lookup([FromBody] TrackingRequest request, CancellationToken ct)
    {
        string orderNumber = (request.OrderNumber ?? string.Empty).Trim().ToUpperInvariant();
        string phone = NormalizePhone(request.Phone);
        if (orderNumber.Length is < 8 or > 32 ||
            orderNumber.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-') ||
            phone.Length != 11 || !phone.StartsWith("09", StringComparison.Ordinal))
            return BadRequest(new { code = "invalid-tracking-request", message = "کد سفارش یا شمارهٔ همراه معتبر نیست." });

        var candidates = await db.StoreCustomerOrders.AsNoTracking().Where(x => x.OrderNumber == orderNumber ||
            db.StoreOrderPreviousNumbers.Any(previous => previous.IdOrder == x.Id && previous.PreviousNumber == orderNumber)).Select(x => new
            {
                x.Id,
                x.OrderNumber,
                Status = x.StatusReference.Code,
                StatusName = x.StatusReference.NameFa,
                x.CreatedAtUtc,
                x.ShippingCarrier,
                x.ShippingTrackingCode,
                x.AddressSnapshotJson
            }).ToListAsync(ct);
        var order = candidates.FirstOrDefault(x => SnapshotPhoneMatches(x.AddressSnapshotJson, phone));
        if (order is null)
            return NotFound(new { code = "order-not-found", message = "سفارشی با این کد و شمارهٔ همراه پیدا نشد." });

        var trackingRows = await db.StoreOrderShipmentTrackingHistories.AsNoTracking().Where(x => x.IdOrder == order.Id).OrderBy(x => x.ChangedAtUtc).Select(x => new { x.Carrier, x.TrackingCode, x.ChangedAtUtc }).ToListAsync(ct);
        var trackingHistory = trackingRows.Select(x => new { x.Carrier, x.TrackingCode, changedAtUtc = DateTime.SpecifyKind(x.ChangedAtUtc, DateTimeKind.Utc) }).ToArray();

        return Ok(new
        {
            orderNumber = order.OrderNumber,
            status = order.Status,
            statusName = order.StatusName,
            createdAtUtc = DateTime.SpecifyKind(order.CreatedAtUtc, DateTimeKind.Utc),
            shippingCarrier = order.ShippingCarrier,
            shippingTrackingCode = order.ShippingTrackingCode,
            trackingHistory
        });
    }

    private static bool SnapshotPhoneMatches(string addressSnapshot, string phone)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(addressSnapshot);
            if (!document.RootElement.TryGetProperty("phone", out var phoneValue) &&
                !document.RootElement.TryGetProperty("Phone", out phoneValue)) return false;

            return phoneValue.ValueKind == JsonValueKind.String && NormalizePhone(phoneValue.GetString()) == phone;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string NormalizePhone(string? value)
    {
        string normalized = new string((value ?? string.Empty).Fa2En().Where(char.IsAsciiDigit).ToArray());
        if (normalized.StartsWith("0098", StringComparison.Ordinal) && normalized.Length == 14)
            return "0" + normalized[4..];

        if (normalized.StartsWith("98", StringComparison.Ordinal) && normalized.Length == 12)
            return "0" + normalized[2..];

        return normalized;
    }

    public sealed record TrackingRequest(string? OrderNumber, string? Phone);
}
