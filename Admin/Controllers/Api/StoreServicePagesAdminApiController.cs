using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/content/pages")]
public sealed class StoreServicePagesAdminApiController(ShopDbContext db) : ControllerBase
{
    private static readonly string[] EditableKeys = ["about", "contact", "shipping", "warranty", "tracking", "terms", "returns", "privacy", "faq"];

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await db.StoreContentPages.AsNoTracking()
        .Where(x => EditableKeys.Contains(x.PageKey)).OrderBy(x => x.Title)
        .Select(x => new { x.PageKey, x.RoutePath, x.Title, x.IsPublished, SectionCount = x.Sections.Count }).ToListAsync(ct));

    [HttpGet("{pageKey}")]
    public async Task<IActionResult> Get(string pageKey, CancellationToken ct)
    {
        if (!EditableKeys.Contains(pageKey)) return NotFound();
        var page = await db.StoreContentPages.AsNoTracking().Include(x => x.Sections).SingleOrDefaultAsync(x => x.PageKey == pageKey, ct);
        return page is null ? NotFound() : Ok(new PageInput(page.Title, page.Description ?? "", page.IsPublished,
            page.Sections.OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
                .Select(x => new SectionInput(x.SectionKey, x.Heading ?? "", x.Body ?? "", x.IsPublished)).ToList()));
    }

    [HttpPut("{pageKey}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string pageKey, [FromBody] PageInput input, CancellationToken ct)
    {
        if (!EditableKeys.Contains(pageKey)) return NotFound();
        string title = input.Title?.Trim() ?? "", description = input.Description?.Trim() ?? "";
        var sections = input.Sections ?? [];
        if (title.Length is < 1 or > 300 || description.Length > 1000 || sections.Count is < 1 or > 50 ||
            sections.Any(x => !System.Text.RegularExpressions.Regex.IsMatch(x.SectionKey ?? "", "^[a-z0-9-]{1,100}$") ||
                (x.Heading?.Length ?? 0) > 300 || string.IsNullOrWhiteSpace(x.Body) || x.Body.Length > 20000) ||
            sections.Select(x => x.SectionKey).Distinct(StringComparer.Ordinal).Count() != sections.Count)
            return BadRequest(new { title = "عنوان، شرح یا بخش‌های صفحه معتبر نیستند." });
        var page = await db.StoreContentPages.Include(x => x.Sections).SingleOrDefaultAsync(x => x.PageKey == pageKey, ct);
        if (page is null) return NotFound();
        page.Title = title; page.Description = description; page.ContentSource = "Database"; page.IsPublished = input.IsPublished; page.UpdatedAtUtc = DateTime.UtcNow;
        page.Sections.Clear();
        for (int i = 0; i < sections.Count; i++)
        {
            SectionInput section = sections[i];
            page.Sections.Add(new StoreContentPageSection
            {
                SectionKey = section.SectionKey!.Trim(),
                Heading = string.IsNullOrWhiteSpace(section.Heading) ? null : section.Heading!.Trim(),
                Body = section.Body!.Trim(),
                SortOrder = i,
                IsPublished = true
            });
        }
        await db.SaveChangesAsync(ct);
        return Ok(new { message = "صفحهٔ خدماتی ذخیره شد." });
    }

    public sealed record SectionInput(string? SectionKey, string? Heading, string? Body, bool IsPublished = true);
    public sealed record PageInput(string? Title, string? Description, bool IsPublished, List<SectionInput>? Sections);
}
