using Microsoft.AspNetCore.Mvc;

namespace Shop.Admin.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class UsersController : Controller
{
    public IActionResult Index(string? section)
    {
        section = section?.ToLowerInvariant();
        if (section is not (null or "users" or "permissions" or "roles")) return NotFound();

        ViewData["Title"] = section switch
        {
            "permissions" => "درخت مجوز نقش‌ها",
            "roles" => "نقش‌های کاربران پنل",
            _ => "کاربران پنل"
        };
        ViewData["AreaTitle"] = "مدیریت سامانه";
        ViewData["FeatureId"] = section switch
        {
            "permissions" => "user-permissions",
            "roles" => "user-roles",
            _ => "users"
        };
        return section switch
        {
            "permissions" => View("Permissions"),
            "roles" => View("Roles"),
            _ => View("Index")
        };
    }
}
