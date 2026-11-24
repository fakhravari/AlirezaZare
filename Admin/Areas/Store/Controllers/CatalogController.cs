using Microsoft.AspNetCore.Mvc;

namespace Shop.Admin.Areas.Store.Controllers;

[Area("Store")]
public sealed class CatalogController : Controller
{
    public IActionResult Products() => CatalogPage("products", "کالاها و پیش‌نویس‌ها",
        "کالاهای منتشرشده و پیش‌نویس‌های کاتالوگ فروشگاه.");

    public IActionResult ProductDetails(long id)
    {
        if (id < 1) return NotFound();
        ViewData["Title"] = "جزئیات کامل کالا";
        ViewData["AreaTitle"] = "مدیریت فروشگاه";
        ViewData["FeatureId"] = "products";
        return View(id);
    }

    public IActionResult Categories() =>
        CatalogPage("categories", "دسته‌بندی‌ها", "ساختار درختی دسته‌بندی‌های کاتالوگ.");

    public IActionResult Brands() => CatalogPage("brands", "برندها", "برندهای متصل به کالاهای فروشگاه.");

    public IActionResult Badges()
    {
        ViewData["Title"] = "نشان‌های کالا";
        ViewData["AreaTitle"] = "مدیریت فروشگاه";
        ViewData["FeatureId"] = "badges";
        return View();
    }

    public IActionResult Attributes() =>
        CatalogPage("attributes", "ویژگی‌های کالا", "تعریف ویژگی‌های فیلتر و مشخصات کالا.");

    public IActionResult PaymentInstitutions()
    {
        ViewData["Title"] = "مؤسسه‌های پرداخت";
        ViewData["AreaTitle"] = "مدیریت فروشگاه";
        ViewData["FeatureId"] = "payment-institutions";
        ViewData["Description"] =
            "نام و نشان مؤسسه‌های پرداخت معرفی‌شده در فروشگاه؛ این فهرست به‌معنی اتصال عملیاتی درگاه نیست.";
        ViewData["Section"] = "payment-institutions";
        ViewData["ApiUrl"] = "/api/admin/store/payment-institutions";

        return View("Index");
    }

    private IActionResult CatalogPage(string section, string title, string description)
    {
        ViewData["Title"] = title;
        ViewData["AreaTitle"] = "مدیریت فروشگاه";
        ViewData["FeatureId"] = section;
        ViewData["Description"] = description;
        ViewData["Section"] = section;
        ViewData["ApiUrl"] = $"/api/admin/store/catalog/{section}";

        return View("Index");
    }
}