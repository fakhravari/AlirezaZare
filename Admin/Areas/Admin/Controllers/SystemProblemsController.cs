using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Admin.Contracts;

namespace Shop.Admin.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "system.full-access")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SystemProblemsController(ISystemProblemsData data) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? check, int page = 1, CancellationToken cancellationToken = default) =>
        View(await data.ReadAsync(check, page, cancellationToken));
}
