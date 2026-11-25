using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Models.Account;
using Shop.Services;

namespace Shop.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "ShopCookie")]
[Route("api/account")]
public sealed class AccountCheckoutController(AccountCheckoutService checkoutService) : ControllerBase
{
    [HttpPost("coupon/quote")]
    public async Task<IActionResult> QuoteCoupon([FromBody] CouponRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await checkoutService.QuoteCouponAsync(ProfileId, request, cancellationToken));

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await checkoutService.CheckoutAsync(ProfileId, request, cancellationToken));

    private long ProfileId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    private IActionResult ToActionResult(AccountUseCaseResult result) => result.Status switch
    {
        AccountUseCaseStatus.Ok => Ok(result.Body),
        AccountUseCaseStatus.BadRequest => BadRequest(result.Body),
        AccountUseCaseStatus.NotFound => NotFound(result.Body),
        AccountUseCaseStatus.Conflict => Conflict(result.Body),
        _ => StatusCode(StatusCodes.Status500InternalServerError)
    };
}