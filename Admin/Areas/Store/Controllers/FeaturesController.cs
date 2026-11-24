using Microsoft.AspNetCore.Mvc;
using Shop.Admin.Areas.Store.Features;

namespace Shop.Admin.Areas.Store.Controllers;

[Area("Store")]
public sealed class FeaturesController : Controller
{
    public IActionResult Index(string feature, string? section = null)
    {
        if (!StoreAdminFeatureCatalog.TryGet(feature ?? "", out var page)) return NotFound();

        ViewData["Title"] = page.Title;
        ViewData["AreaTitle"] = page.AreaTitle;
        ViewData["FeatureId"] = page.FeatureId;

        if (page.FeatureId == "geography")
        {
            if (!User.IsInRole("system.full-access")) return Forbid();

            return View("~/Areas/Store/Views/Features/Geography.cshtml", page);
        }

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

        if (page.FeatureId == "customers")
        {
            if (!User.IsInRole("system.full-access")) return Forbid();

            return View("~/Areas/Store/Views/Features/Customers.cshtml", page);
        }

        if (page.FeatureId == "newsletter-subscribers")
            return View("~/Areas/Store/Views/Features/NewsletterSubscribers.cshtml", page);

        if (page.FeatureId == "reports")
            return View("~/Areas/Store/Views/Features/Reports.cshtml", page);

        if (page.FeatureId == "contact-messages")
            return View("~/Areas/Store/Views/Features/ContactMessages.cshtml", page);

        if (page.FeatureId == "account-messages")
            return View("~/Areas/Store/Views/Features/AccountMessages.cshtml", page);

        if (page.FeatureId == "content")
        {
            if (!User.IsInRole("system.full-access")) return Forbid();

            string[] sections = ["faq", "magazine", "pages"];
            if (!sections.Contains(section ?? "", StringComparer.OrdinalIgnoreCase))
                return RedirectToAction(nameof(Index), new { feature = page.FeatureId, section = "faq" });

            section = section!.ToLowerInvariant();
            ViewData["ContentSection"] = section;
            ViewData["Title"] = section switch
            {
                "magazine" => "مقاله‌های مجله", "pages" => "صفحه‌های خدماتی", _ => "پرسش‌های متداول"
            };

            return View("~/Areas/Store/Views/Features/Faq.cshtml", page);
        }

        if (page.FeatureId == "promotions")
        {
            string[] sections = ["plans", "offers", "variant-prices", "history"];
            string defaultSection = User.IsInRole("system.full-access") ? "plans" : "offers";
            if (!sections.Contains(section ?? "", StringComparer.OrdinalIgnoreCase))
                return RedirectToAction(nameof(Index), new { feature = page.FeatureId, section = defaultSection });

            section = section!.ToLowerInvariant();
            if (section == "plans" && !User.IsInRole("system.full-access")) return Forbid();

            ViewData["PromotionSection"] = section;
            ViewData["ApiUrl"] = "/api/admin/store/variant-prices";

            return View("~/Areas/Store/Views/Features/VariantPrices.cshtml", page);
        }

        if (page.FeatureId is "reviews" or "questions")
            return View("~/Areas/Store/Views/Features/Feedback.cshtml", page);

        if (page.FeatureId == "testimonials")
            return View("~/Areas/Store/Views/Features/Testimonials.cshtml", page);

        return View("~/Areas/Store/Views/Features/Index.cshtml", page);
    }
}