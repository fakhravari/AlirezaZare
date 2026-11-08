using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Shop.Admin.Controllers;

public sealed class HomeController : Controller
{
    [AllowAnonymous]
    public IActionResult Index() => User.Identity?.IsAuthenticated == true
        ? RedirectToAction("Index", "Home", new { area = "Admin" })
        : RedirectToAction("Login", "Auth");
}
