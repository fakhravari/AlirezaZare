using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/magazine")]
public sealed class StoreMagazineAdminApiController(IStoreMagazineAdminData data) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok((await data.ReadArticlesAsync(ct)).Select(article => new
        {
            article.Id, article.ArticleKey, article.Title, article.Category, article.IsPublished,
            Count = article.SectionCount
        }));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        var article = await data.ReadArticleAsync(id, ct);

        return article is null
            ? NotFound()
            : Ok(new ArticleInput(article.ArticleKey, article.Category, article.Title, article.Summary,
                article.Author,
                article.Kicker, article.Branch, article.Topic, article.Area,
                article.HeroImagePath, article.ContentImagePath,
                article.ReadTimeMinutes, article.ReadTimeText, article.Lead, article.Conclusion, article.IsPublished,
                article.Sections.Select(s => new SectionInput(s.Heading, s.Body)).ToList()));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create([FromBody] ArticleInput input, CancellationToken ct) => Save(null, input, ct);

    [HttpPut("{id:long}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Update(long id, [FromBody] ArticleInput input, CancellationToken ct) =>
        Save(id, input, ct);

    private async Task<IActionResult> Save(long? id, ArticleInput input, CancellationToken ct)
    {
        string key = input.ArticleKey?.Trim().ToLowerInvariant() ?? "";
        if (!Regex.IsMatch(key, "^[a-z0-9]+(?:-[a-z0-9]+)*$") || key.Length > 150 ||
            string.IsNullOrWhiteSpace(input.Title) || input.Title.Trim().Length > 300 ||
            string.IsNullOrWhiteSpace(input.Category) || input.Category.Trim().Length > 150 ||
            string.IsNullOrWhiteSpace(input.Summary) || input.Summary.Trim().Length > 1500 ||
            (input.Author?.Length ?? 0) > 200 || (input.Kicker?.Length ?? 0) > 300 ||
            (input.HeroImagePath?.Length ?? 0) > 1000 || (input.ContentImagePath?.Length ?? 0) > 1000 ||
            input.ReadTimeMinutes is < 1 or > 180 || (input.Sections?.Count ?? 0) > 40 ||
            (input.Sections ?? []).Any(s =>
                string.IsNullOrWhiteSpace(s.Heading) || s.Heading.Trim().Length > 300 ||
                string.IsNullOrWhiteSpace(s.Body)))
            return BadRequest(new { title = "اطلاعات مقاله یا بخش‌های آن معتبر نیست." });

        if (string.IsNullOrWhiteSpace(input.Branch) || input.Branch.Trim().Length > 160 ||
            string.IsNullOrWhiteSpace(input.Topic) || input.Topic.Trim().Length > 240 ||
            string.IsNullOrWhiteSpace(input.Area) || input.Area.Trim().Length > 240)
            return BadRequest(new { title = "دسته، شاخه، موضوع و حوزهٔ مقاله را کامل و معتبر وارد کنید." });

        var update = new MagazineArticleUpdate(key, input.Category!.Trim(), input.Title.Trim(), input.Summary.Trim(),
            Clean(input.Author), Clean(input.Kicker), input.Branch.Trim(), input.Topic.Trim(), input.Area.Trim(),
            Clean(input.HeroImagePath), Clean(input.ContentImagePath), input.ReadTimeMinutes, Clean(input.ReadTimeText),
            Clean(input.Lead), Clean(input.Conclusion), input.IsPublished,
            (input.Sections ?? []).Select(section => new MagazineArticleSectionData(section.Heading.Trim(),
                section.Body.Trim())).ToArray());
        var result = await data.SaveArticleAsync(id, update, ct);
        if (result.Kind == MagazineArticleSaveResultKind.DuplicateKey)
            return Conflict(new { title = "این نشانی مقاله قبلاً استفاده شده است." });
        if (result.Kind == MagazineArticleSaveResultKind.NotFound) return NotFound();
        return Ok(new { message = "مقاله ذخیره شد.", id = result.Id });
    }

    [HttpDelete("{id:long}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await data.DeleteArticleAsync(id, ct);
        if (result == MagazineArticleDeleteResult.NotFound) return NotFound();
        if (result == MagazineArticleDeleteResult.HasInteractions)
            return Conflict(new { title = "برای مقاله تعامل کاربر ثبت شده است؛ ابتدا انتشار آن را غیرفعال کنید." });
        return Ok(new { message = "مقاله حذف شد." });
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public sealed record SectionInput(string Heading, string Body);

    public sealed record ArticleInput(
        string? ArticleKey,
        string? Category,
        string? Title,
        string? Summary,
        string? Author,
        string? Kicker,
        string? Branch,
        string? Topic,
        string? Area,
        string? HeroImagePath,
        string? ContentImagePath,
        int? ReadTimeMinutes,
        string? ReadTimeText,
        string? Lead,
        string? Conclusion,
        bool IsPublished,
        List<SectionInput>? Sections);
}
