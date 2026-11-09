using Microsoft.AspNetCore.Mvc;

namespace Shop.Admin.Areas.Store.Controllers;

[Area("Store")]
public sealed class DashboardController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "نمای کلی فروشگاه";
        ViewData["AreaTitle"] = "مدیریت فروشگاه";
        ViewData["FeatureId"] = "overview";
        return View();
    }
}