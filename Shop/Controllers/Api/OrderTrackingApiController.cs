using System.Globalization;
using Shop.Data.Domain.Store.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shop.Data.Helpers.Extensions;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Controllers.Api;

[ApiController]
[AllowAnonymous]
[Route("api/store/orders/tracking")]
public sealed class OrderTrackingApiController(IStorePublicPageData pageData) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("order-tracking")]
    public async Task<IActionResult> Lookup([FromBody] TrackingRequest request, CancellationToken ct)
    {
        string orderNumber = (request.OrderNumber ?? string.Empty).Trim().Fa2En();
        string phone = NormalizePhone(request.Phone);
        if (orderNumber.Length is < 1 or > 29 ||
            orderNumber.Any(character => !char.IsAsciiDigit(character)) ||
            !decimal.TryParse(orderNumber, NumberStyles.None, CultureInfo.InvariantCulture, out decimal orderId) ||
            phone.Length != 11 || !phone.StartsWith("09", StringComparison.Ordinal))
            return BadRequest(new
                { code = "invalid-tracking-request", message = "شمارهٔ سفارش یا شمارهٔ همراه معتبر نیست." });

        var candidates = await pageData.FindOrderTrackingCandidatesAsync(orderId, ct);
        var order = candidates.FirstOrDefault(x => NormalizePhone(x.Phone) == phone);
        if (order is null)
            return NotFound(new { code = "order-not-found", message = "سفارشی با این شماره و شمارهٔ همراه پیدا نشد." });

        return Ok(new
        {
            orderNumber = order.Id.ToString(CultureInfo.InvariantCulture),
            status = StoreOrderStatusNames.CodeFromNameEn(order.StatusNameEn),
            statusName = order.StatusName,
            createdAtUtc = DateTime.SpecifyKind(order.CreatedAtUtc, DateTimeKind.Utc)
        });
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
