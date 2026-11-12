using Microsoft.AspNetCore.Mvc;

namespace Shop.Admin.Areas.Store.Controllers;

[Area("Store")]
public sealed class CatalogController : Controller
{
    public IActionResult Products() => CatalogPage("products", "محصولات", "کالاها و وضعیت پایهٔ کاتالوگ فروشگاه.");

    public IActionResult Categories() =>
        CatalogPage("categories", "دسته‌بندی‌ها", "ساختار درختی دسته‌بندی‌های کاتالوگ.");

    public IActionResult Brands() => CatalogPage("brands", "برندها", "برندهای متصل به کالاهای فروشگاه.");
    public IActionResult Attributes() => CatalogPage("attributes", "ویژگی‌ها", "تعریف ویژگی‌های فیلتر و مشخصات کالا.");
    public IActionResult PaymentInstitutions()
    {
        ViewData["Title"] = "درگاه‌های پرداخت";
        ViewData["AreaTitle"] = "مدیریت فروشگاه";
        ViewData["FeatureId"] = "payment-institutions";
        ViewData["DataSourceLabel"] = "خواندن از دیتابیس با API";
        ViewData["Description"] = "نام و لوگوی درگاه‌های نمایش‌داده‌شده در فروشگاه.";
        ViewData["Section"] = "payment-institutions";
        ViewData["ApiUrl"] = "/api/admin/store/payment-institutions";
        return View("Index");
    }

    private IActionResult CatalogPage(string section, string title, string description)
    {
        ViewData["Title"] = title;
        ViewData["AreaTitle"] = "مدیریت فروشگاه";
        ViewData["FeatureId"] = section;
        ViewData["DataSourceLabel"] = "خواندن از دیتابیس با API";
        ViewData["Description"] = description;
        ViewData["Section"] = section;
        ViewData["ApiUrl"] = $"/api/admin/store/catalog/{section}";
        return View("Index");
    }
}
