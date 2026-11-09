using Microsoft.AspNetCore.Mvc;

namespace Shop.Admin.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class UsersController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "کاربران سیستم و دسترسی‌ها";
        ViewData["AreaTitle"] = "مدیریت سامانه";
        ViewData["FeatureId"] = "users";
        return View();
    }
}