using Microsoft.AspNetCore.Mvc;

namespace Shop.Admin.Areas.Warehouse.Controllers;

[Area("Warehouse")]
public sealed class WarehousesController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "انبارها";
        ViewData["AreaTitle"] = "مدیریت انبار";
        ViewData["FeatureId"] = "warehouses";
        ViewData["DataSourceLabel"] = "خواندن از دیتابیس با API";
        return View();
    }
}
