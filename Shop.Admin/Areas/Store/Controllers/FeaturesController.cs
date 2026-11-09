using Microsoft.AspNetCore.Mvc;
using Shop.Admin.Areas.Store.Features;

namespace Shop.Admin.Areas.Store.Controllers;

[Area("Store")]
public sealed class FeaturesController : Controller
{
    public IActionResult Index(string feature)
    {
        if (!StoreAdminFeatureCatalog.TryGet(feature ?? "", out var page)) return NotFound();
        ViewData["Title"] = page.Title;
        ViewData["AreaTitle"] = page.AreaTitle;
        ViewData["FeatureId"] = page.FeatureId;
        if (page.FeatureId == "paid-orders-dispatch")
        {
            if (!User.IsInRole("system.full-access")) return Forbid();
            return View("~/Areas/Store/Views/Features/PaidOrdersDispatch.cshtml", page);
        }
        if (page.FeatureId == "orders")
        {
            if (!User.IsInRole("system.full-access")) return Forbid();
            return View("~/Areas/Store/Views/Features/Orders.cshtml", page);
        }
        if (page.FeatureId == "promotions")
        {
            ViewData["ApiUrl"] = "/api/admin/store/variant-prices";
            return View("~/Areas/Store/Views/Features/VariantPrices.cshtml", page);
        }

        if (page.FeatureId is "reviews" or "questions")
            return View("~/Areas/Store/Views/Features/Feedback.cshtml", page);
        return View("~/Areas/Store/Views/Features/Index.cshtml", page);
    }
}
