using Microsoft.AspNetCore.Mvc;
using Shop.Admin.Areas.Warehouse.Features;

namespace Shop.Admin.Areas.Warehouse.Controllers;

[Area("Warehouse")]
public sealed class WarehousesController : Controller
{
    public IActionResult Index()
    {
        if (!WarehouseAdminFeatureCatalog.TryGet("warehouses", out var page)) return NotFound();
        ViewData["Title"] = page.Title;
        ViewData["AreaTitle"] = page.AreaTitle;
        ViewData["FeatureId"] = page.FeatureId;
        ViewData["ApiUrl"] = "/api/admin/warehouse/definitions";
        return View("~/Areas/Warehouse/Views/Features/Definitions.cshtml", page);
    }
}
