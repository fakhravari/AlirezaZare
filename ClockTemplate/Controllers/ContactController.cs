using ClockTemplate.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClockTemplate.Controllers;

public sealed class ContactController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("تماس با ما | کمپانی علی‌رضا زارع", "تماس با کمپانی علی‌رضا زارع؛ اطلاعات شعب، مدیریت و پشتیبانی."));
        return await PageAsync(model);
    }
}
