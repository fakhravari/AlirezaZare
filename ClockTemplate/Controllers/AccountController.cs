using Microsoft.AspNetCore.Mvc;

namespace ClockTemplate.Controllers;

public sealed class AccountController : PageController
{
    public IActionResult Index() => RedirectToAction("Index", "Profile");
}
