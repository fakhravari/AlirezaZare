using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Account.Contracts;

namespace Shop.Controllers.Account;

[ApiController]
[Authorize(AuthenticationSchemes = "ShopCookie")]
[Route("api/account")]
public sealed class AccountStateController(IAccountStateQueryService accountState) : ControllerBase
{
    [HttpGet("state")]
    public async Task<IActionResult> State(CancellationToken cancellationToken)
    {
        long profileId = long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        string? json = await accountState.ReadJsonAsync(profileId, cancellationToken);
        return json is null ? NotFound() : Content(json, "application/json; charset=utf-8");
    }
}