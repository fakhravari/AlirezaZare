using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Domain.Store.Entities;
using Shop.Database.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/magazine")]
public sealed class StoreMagazineAdminApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await db.MagazineArticles.AsNoTracking()
        .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc).Select(x => new { x.Id, x.ArticleKey, x.Title, x.Category, x.IsPublished, x.Sections.Count }).ToListAsync(ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        var article = await db.MagazineArticles.AsNoTracking().Include(x => x.Sections).SingleOrDefaultAsync(x => x.Id == id, ct);
        return article is null ? NotFound() : Ok(new ArticleInput(article.ArticleKey, article.Category, article.Title, article.Summary, article.Author,
            article.Kicker, article.Branch, article.Topic, article.Area, article.HeroImagePath, article.ContentImagePath,
            article.ReadTimeMinutes, article.ReadTimeText, article.Lead, article.Conclusion, article.IsPublished,
            article.Sections.OrderBy(s => s.SortOrder).Select(s => new SectionInput(s.Heading, s.Body)).ToList()));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create([FromBody] ArticleInput input, CancellationToken ct) => Save(null, input, ct);

    [HttpPut("{id:long}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Update(long id, [FromBody] ArticleInput input, CancellationToken ct) => Save(id, input, ct);

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
            (input.Sections ?? []).Any(s => string.IsNullOrWhiteSpace(s.Heading) || s.Heading.Trim().Length > 300 || string.IsNullOrWhiteSpace(s.Body)))
            return BadRequest(new { title = "اطلاعات مقاله یا بخش‌های آن معتبر نیست." });
        if (await db.MagazineArticles.AnyAsync(x => x.ArticleKey == key && x.Id != (id ?? 0), ct))
            return Conflict(new { title = "این نشانی مقاله قبلاً استفاده شده است." });
        var article = id is null ? new MagazineArticle { CreatedAtUtc = DateTime.UtcNow } :
            await db.MagazineArticles.Include(x => x.Sections).SingleOrDefaultAsync(x => x.Id == id, ct) ?? null!;
        if (article is null) return NotFound();
        article.ArticleKey = key; article.Category = input.Category.Trim(); article.Title = input.Title.Trim();
        article.Summary = input.Summary.Trim(); article.Author = Clean(input.Author); article.Kicker = Clean(input.Kicker);
        article.Branch = Clean(input.Branch); article.Topic = Clean(input.Topic); article.Area = Clean(input.Area);
        article.HeroImagePath = Clean(input.HeroImagePath); article.ContentImagePath = Clean(input.ContentImagePath);
        article.ReadTimeMinutes = input.ReadTimeMinutes; article.ReadTimeText = Clean(input.ReadTimeText);
        article.Lead = Clean(input.Lead); article.Conclusion = Clean(input.Conclusion); article.IsPublished = input.IsPublished;
        article.UpdatedAtUtc = DateTime.UtcNow;
        article.Sections.Clear();
        int order = 0;
        foreach (var section in input.Sections ?? []) article.Sections.Add(new MagazineArticleSection { Heading = section.Heading.Trim(), Body = section.Body.Trim(), SortOrder = order++ });
        if (id is null) db.MagazineArticles.Add(article);
        await db.SaveChangesAsync(ct);
        return Ok(new { message = "مقاله ذخیره شد.", id = article.Id });
    }

    [HttpDelete("{id:long}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var article = await db.MagazineArticles.Include(x => x.Sections).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (article is null) return NotFound();
        if (await db.AccountArticleInteractions.AnyAsync(x => x.IdMagazineArticle == id, ct))
            return Conflict(new { title = "برای مقاله تعامل کاربر ثبت شده است؛ ابتدا انتشار آن را غیرفعال کنید." });
        db.MagazineArticles.Remove(article); await db.SaveChangesAsync(ct); return Ok(new { message = "مقاله حذف شد." });
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public sealed record SectionInput(string Heading, string Body);
    public sealed record ArticleInput(string? ArticleKey, string? Category, string? Title, string? Summary, string? Author, string? Kicker,
        string? Branch, string? Topic, string? Area, string? HeroImagePath, string? ContentImagePath, int? ReadTimeMinutes,
        string? ReadTimeText, string? Lead, string? Conclusion, bool IsPublished, List<SectionInput>? Sections);
}
