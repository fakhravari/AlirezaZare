using Microsoft.AspNetCore.Mvc;
using Shop.Admin.Areas.Warehouse.Features;

namespace Shop.Admin.Areas.Warehouse.Controllers;

[Area("Warehouse")]
public sealed class FeaturesController : Controller
{
    public IActionResult Index(string feature)
    {
        if (!WarehouseAdminFeatureCatalog.TryGet(feature ?? "", out var page)) return NotFound();
        ViewData["Title"] = page.Title;
        ViewData["AreaTitle"] = page.AreaTitle;
        ViewData["FeatureId"] = page.FeatureId;
        return View("~/Areas/Warehouse/Views/Features/Index.cshtml", page);
    }
}
