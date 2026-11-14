using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Infrastructure.Payments;
using Shop.Services;

namespace Shop.Controllers;

[ApiController]
[Route("api/payment")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PaymentController(PaymentService payments, IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("token")]
    [Authorize(AuthenticationSchemes = "ShopCookie")]
    public IActionResult Token() => Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    [HttpPost("orders/{orderId:long}/start")]
    [Authorize(AuthenticationSchemes = "ShopCookie")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(long orderId, CancellationToken ct)
    {
        long profileId = long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        PaymentStartResult result = await payments.StartAsync(orderId, profileId, ct);
        return result.Status switch
        {
            "redirect" => Ok(new { redirectUrl = result.RedirectUri!.AbsoluteUri }),
            "not-found" => NotFound(),
            "invalid-order" or "already-started" => Conflict(new { code = result.Status }),
            _ => StatusCode(StatusCodes.Status503ServiceUnavailable, new { code = result.Status, message = "درگاه پرداخت در دسترس نیست؛ سفارش در انتظار پرداخت باقی مانده است." })
        };
    }

    [AllowAnonymous]
    [HttpGet("callback/{attemptId:guid}")]
    public async Task<IActionResult> Callback(Guid attemptId, CancellationToken ct)
    {
        IReadOnlyDictionary<string, IReadOnlyList<string>> callback = Request.Query.ToDictionary(
            item => item.Key,
            item => (IReadOnlyList<string>)item.Value.Select(value => value ?? string.Empty).ToArray(),
            StringComparer.OrdinalIgnoreCase);
        string result = await payments.VerifyAsync(attemptId, callback, ct);
        return result switch
        {
            "verified" => Ok(new { status = result }),
            "not-found" => NotFound(),
            "not-ready" or "order-conflict" or "failed" => Conflict(new { status = result }),
            _ => StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = result })
        };
    }
}
