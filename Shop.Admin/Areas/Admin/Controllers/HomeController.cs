using Microsoft.AspNetCore.Mvc;

namespace Shop.Admin.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class HomeController : Controller
{
    public IActionResult Index() => View();
}