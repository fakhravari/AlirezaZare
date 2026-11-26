using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Account.Contracts;

namespace Shop.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "ShopCookie")]
[Route("api/account")]
public sealed class AccountCheckoutController(IAccountCheckoutData checkoutService) : ControllerBase
{
    [HttpPost("coupon/quote")]
    public async Task<IActionResult> QuoteCoupon([FromBody] CouponRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await checkoutService.QuoteCouponAsync(ProfileId, request.Code, cancellationToken));

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await checkoutService.CheckoutAsync(ProfileId, request.AddressId, request.PaymentMethod, request.CouponCode, cancellationToken));

    private long ProfileId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    private IActionResult ToActionResult(CheckoutResult result) => result.Status switch
    {
        CheckoutStatus.Ok => Ok(result.Body),
        CheckoutStatus.BadRequest => BadRequest(result.Body),
        CheckoutStatus.NotFound => NotFound(result.Body),
        CheckoutStatus.Conflict => Conflict(result.Body),
        _ => StatusCode(StatusCodes.Status500InternalServerError)
    };
}
