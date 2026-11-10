using Microsoft.AspNetCore.Mvc;

namespace Shop.Admin.Areas.Warehouse.Controllers;

[Area("Warehouse")]
public sealed class DashboardController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "نمای کلی انبار";
        ViewData["AreaTitle"] = "مدیریت انبار";
        ViewData["FeatureId"] = "overview";
        return View();
    }
}