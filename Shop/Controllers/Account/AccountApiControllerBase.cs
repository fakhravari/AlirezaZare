using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Account.Contracts;

namespace Shop.Controllers.Account;

public abstract class AccountApiControllerBase(IAccountDataCommands accountData) : ControllerBase
{
    protected readonly IAccountDataCommands Data = accountData;

    protected long ProfileId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
