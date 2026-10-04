using ClockTemplate.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClockTemplate.Controllers;

public sealed class HomeController : PageController
{
    public async Task<IActionResult> Index()
    {
        PageModel model = new(
            new SeoModel(
                "فروشگاه ساعت هادی نوروزی | عرضه برندهای معتبر و اورجینال",
                "فروشگاه چندبرندی ساعت هادی نوروزی؛ عرضه ساعت‌های اورجینال مردانه و زنانه از برندهای معتبر، با تضمین اصالت و خدمات پس از فروش."),
            IsHome: true);

        return await PageAsync(model);
    }
}
