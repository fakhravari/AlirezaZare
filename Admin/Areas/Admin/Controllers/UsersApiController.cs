using Microsoft.AspNetCore.Mvc;
using Shop.Admin.Infrastructure;
using Shop.Data.Application.Admin.Contracts;

namespace Shop.Admin.Areas.Admin.Controllers;

[Area("Admin")]
[ApiController]
[Route("api/admin/users")]
public sealed class UsersApiController(IAdminUsersData data) : AdminOperationController
{
    [HttpGet]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken) =>
        Respond(await data.GetUsers(Request.Query["section"].ToString(), cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveUser([FromBody] SaveSystemUserInput input, CancellationToken cancellationToken) =>
        Respond(await data.SaveUser(input, cancellationToken));

    [HttpPut("roles/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRole(long id, [FromBody] SaveRoleInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.SaveRole(Request.Query["section"].ToString(), id, input, cancellationToken));

    [HttpPost("roles")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleInput input, CancellationToken cancellationToken) =>
        Respond(await data.CreateRole(input, cancellationToken));

    [HttpPost("{id:long}/reset-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(long id, [FromBody] ResetPasswordInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.ResetPassword(id, input, cancellationToken));
}
