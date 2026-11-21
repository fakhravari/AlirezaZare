using Microsoft.AspNetCore.Mvc;
using Shop.Admin.Areas.Warehouse.Features;

namespace Shop.Admin.Areas.Warehouse.Controllers;

[Area("Warehouse")]
public sealed class FeaturesController : Controller
{
    public IActionResult Index(string feature, string? section = null)
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
                "companies", "warehouses", "references", "units", "product-units", "variant-names", "variants", "document-types",
                "sales-plan-mapping", "stock-owners"
            }.Contains(page.FeatureId, StringComparer.OrdinalIgnoreCase))
        {
            page = page with { AreaTitle = "مدیریت سامانه" };
            ViewData["AreaTitle"] = page.AreaTitle;
            ViewData["ApiUrl"] = "/api/admin/warehouse/definitions";

            return View("~/Areas/Warehouse/Views/Features/Definitions.cshtml", page);
        }

        if (page.FeatureId == "order-confirmation")
        {
            ViewData["WarehouseOrderQueue"] = true;

            return View("~/Areas/Store/Views/Features/PaidOrdersDispatch.cshtml", page);
        }

        if (page.FeatureId == "movements")
        {
            string mode = string.IsNullOrWhiteSpace(section) ? "stock" : section.ToLowerInvariant();
            if (mode is not ("stock" or "summary" or "audit")) return NotFound();
            string title = mode switch
            {
                "summary" => "خلاصهٔ گردش کالا",
                "audit" => "تاریخچهٔ تغییر سند",
                _ => "گردش موجودی"
            };
            page = page with { Title = title };
            ViewData["Title"] = title;
            ViewData["MovementSection"] = mode;
            ViewData["ApiUrl"] = "/api/admin/warehouse/movements";

            return View("~/Areas/Warehouse/Views/Features/Movements.cshtml", page);
        }

        return View("~/Areas/Warehouse/Views/Features/Index.cshtml", page);
    }
}
