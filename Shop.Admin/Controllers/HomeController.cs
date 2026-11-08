using Microsoft.AspNetCore.Mvc;

namespace Shop.Admin.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Home", new { area = "Admin" });
}
