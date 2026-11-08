using Microsoft.AspNetCore.Mvc;

namespace Shop.Admin.Areas.Warehouse.Controllers;

[Area("Warehouse")]
public sealed class StockController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "موجودی کالا";
        ViewData["AreaTitle"] = "مدیریت انبار";
        ViewData["FeatureId"] = "stock";
        ViewData["DataSourceLabel"] = "خواندن از دیتابیس با API";
        return View();
    }
}
