using Shop.Models;
using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Controllers;

public sealed class FaqController(IStoreRuntimeDatasetRepository content) : PageController
{
    public async Task<IActionResult> Index()
    {
        IReadOnlyList<FaqEntry> entries = (await content.ReadFaqAsync(HttpContext.RequestAborted))
            .Select(row => new FaqEntry(row.Question, row.Answer, row.LinkText, row.LinkUrl)).ToArray();
        var faqData = new Dictionary<string, object?>
        {
            ["@type"] = "FAQPage",
            ["mainEntity"] = entries.Select(item => new Dictionary<string, object?>
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
        PageModel<IReadOnlyList<FaqEntry>> model = new(entries,
            new SeoModel("پرسش‌های متداول | ندا اولیائی",
                "پاسخ پرسش‌های رایج درباره انتخاب ابزار، آماده‌سازی خرید، پیگیری سفارش، هزینه و زمان ارسال، ضمانت کالا و روش بازگرداندن آن را یک‌جا بخوانید.",
                StructuredData: faqData,
                Breadcrumbs: [new SeoBreadcrumb("خانه", "/"), new SeoBreadcrumb("پرسش‌های متداول", "/faq")]));
        return await PageAsync(model with { ContentKey = "faq" });
    }
}
