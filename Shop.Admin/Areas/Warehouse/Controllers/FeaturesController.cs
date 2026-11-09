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
        if (new[] { "receipts", "dispatches", "transfers", "returns", "stocktake", "adjustments" }.Contains(
                page.FeatureId, StringComparer.OrdinalIgnoreCase))
        {
            ViewData["ApiUrl"] = "/api/admin/warehouse/documents";
            return View("~/Areas/Warehouse/Views/Features/Documents.cshtml", page);
        }

        if (new[]
            {
                "companies", "warehouses", "references", "units", "product-units", "variants", "document-types",
                "sales-plan-mapping"
            }.Contains(page.FeatureId, StringComparer.OrdinalIgnoreCase))
        {
            ViewData["ApiUrl"] = "/api/admin/warehouse/definitions";
            return View("~/Areas/Warehouse/Views/Features/Definitions.cshtml", page);
        }

        if (page.FeatureId == "movements")
        {
            ViewData["ApiUrl"] = "/api/admin/warehouse/movements";
            return View("~/Areas/Warehouse/Views/Features/Movements.cshtml", page);
        }

        return View("~/Areas/Warehouse/Views/Features/Index.cshtml", page);
    }
}