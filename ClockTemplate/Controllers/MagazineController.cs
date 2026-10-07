using ClockTemplate.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClockTemplate.Controllers;

[Route("Magazine")]
public sealed class MagazineController : PageController
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        PageModel model = new(new SeoModel("مجله تخصصی ساعت | کمپانی علی‌رضا زارع", "مجله تخصصی ساعت؛ راهنمای خرید، نگهداری و تشخیص اصالت."));
        return await PageAsync(model);
    }

    [HttpGet("Details/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        if (JournalArticles.Find(slug) is { } relatedArticle)
        {
            return await PageAsync(new PageModel<MagazineDetailsModel>(relatedArticle, new SeoModel(relatedArticle.Title + " | مجله علی‌رضا زارع", relatedArticle.Summary)));
        }

        if (!string.Equals(slug, "choosing-watch-for-wrist", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        MagazineDetailsModel article = new(
            slug,
            "راهنمای خرید",
            "راهنمای جامع خرید",
            "چطور ساعت مناسب استایل و اندازه مچ خود انتخاب کنیم؟",
            "یک راهنمای مرحله‌به‌مرحله برای انتخاب قطر قاب، جنس بند، رنگ صفحه و نوع موتور؛ بدون سردرگمی میان اصطلاحات فنی.",
            "تحریریه علی‌رضا زارع",
            "۱۸ مرداد ۱۴۰۵",
            "۸ دقیقه مطالعه",
            "ClockTemplate/images/heroes/hero-electrical-editorial.png",
            "ClockTemplate/images/products/tool-hammer-drill.png",
            "انتخاب ساعت فقط به پسندیدن ظاهر آن محدود نمی‌شود. تناسب ابعاد ساعت با مچ، هماهنگی رنگ و جنس بند با سبک پوشش و انتخاب موتور مناسب، تفاوت میان یک خرید معمولی و انتخابی ماندگار را رقم می‌زند.",
            [
                new("مچ", "ابتدا اندازه مچ خود را بدانید", "با یک متر پارچه‌ای، محیط مچ را دقیقاً در محلی که ساعت قرار می‌گیرد اندازه بگیرید. متر نباید بیش از حد سفت یا آزاد باشد."),
                new("case", "قطر و ضخامت قاب را با هم ببینید", "قطر قاب تنها عدد مهم نیست. ساعت‌هایی با قاب باریک کوچک‌تر از اندازه واقعی دیده می‌شوند، در حالی که قاب ضخیم حضور بصری بیشتری دارد."),
                new("strap", "بند مناسب سبک زندگی شما", "بند چرمی برای پوشش رسمی و کلاسیک انتخابی مطمئن است. بند استیل دوام بالا و بند سیلیکونی برای فعالیت روزمره مناسب است."),
                new("movement", "کوارتز، اتوماتیک یا مکانیکی؟", "اگر دقت بالا و نگهداری آسان اولویت شماست، موتور کوارتز انتخاب منطقی‌تری است. مدل اتوماتیک تجربه مکانیکی جذاب‌تری ارائه می‌دهد."),
                new("checklist", "چک‌لیست نهایی پیش از خرید", "قطر، ضخامت، مقاومت آب، وزن، اصالت، شماره سریال، شرایط ضمانت و خدمات پس از فروش را پیش از خرید بررسی کنید.")
            ],
            "ساعت مناسب باید روی مچ شما متعادل باشد، با سبک زندگی‌تان هماهنگ بماند و از فروشنده‌ای معتبر تهیه شود.");

        PageModel<MagazineDetailsModel> model = new(article, new SeoModel(article.Title + " | مجله علی‌رضا زارع", article.Summary));

        return await PageAsync(model);
    }
}
