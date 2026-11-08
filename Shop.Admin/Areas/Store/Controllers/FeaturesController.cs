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
        return View("~/Areas/Store/Views/Features/Index.cshtml", page);
    }
}
