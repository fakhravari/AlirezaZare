using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class AccountController : PageController
{
    public IActionResult Index() => RedirectToAction("Index", "Profile");
}
