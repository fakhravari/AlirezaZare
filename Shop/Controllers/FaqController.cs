using Shop.Models;
using Microsoft.AspNetCore.Mvc;

namespace Shop.Controllers;

public sealed class FaqController : PageController
{
    public async Task<IActionResult> Index()
    {
        var faqData = new Dictionary<string, object?>
        {
            ["@type"] = "FAQPage",
            ["mainEntity"] = FaqCatalog.Items.Select(item => new Dictionary<string, object?>
            {
                ["@type"] = "Question",
                ["name"] = item.Question,
                ["acceptedAnswer"] = new Dictionary<string, object?>
                {
                    ["@type"] = "Answer",
                    ["text"] = item.StructuredAnswer
                }
            }).ToArray()
        };
        PageModel<IReadOnlyList<FaqEntry>> model = new(FaqCatalog.Items,
            new SeoModel("پرسش‌های متداول | ندا اولیائی", "پاسخ پرسش‌های رایج درباره انتخاب ابزار، آماده‌سازی خرید، پیگیری سفارش، هزینه و زمان ارسال، ضمانت کالا و روش بازگرداندن آن را یک‌جا بخوانید.", StructuredData: faqData, Breadcrumbs: [new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("پرسش‌های متداول", "/faq")]));
        return await PageAsync(model);
    }
}
