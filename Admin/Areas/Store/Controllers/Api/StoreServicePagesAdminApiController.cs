using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/content/pages")]
public sealed class StoreServicePagesAdminApiController(IStoreContentAdminData data) : ControllerBase
{
    private static readonly string[] EditableKeys =
        ["about", "contact", "shipping", "warranty", "tracking", "terms", "returns", "privacy", "faq"];

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await data.ReadManagedPagesAsync(ct));

    [HttpGet("{pageKey}")]
    public async Task<IActionResult> Get(string pageKey, CancellationToken ct)
    {
        if (!EditableKeys.Contains(pageKey)) return NotFound();

        var page = await data.ReadManagedPageAsync(pageKey, ct);

        return page is null
            ? NotFound()
            : Ok(new PageInput(page.Title, page.Description, page.Published,
                page.Sections
                    .Select(x => new SectionInput(x.SectionKey, x.Heading ?? "", x.Body, x.Published))
                    .ToList()));
    }

    [HttpPut("{pageKey}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string pageKey, [FromBody] PageInput input, CancellationToken ct)
    {
        if (!EditableKeys.Contains(pageKey)) return NotFound();

        string title = input.Title?.Trim() ?? "", description = input.Description?.Trim() ?? "";
        var sections = input.Sections ?? [];
        if (title.Length is < 1 or > 300 || description.Length > 1000 || sections.Count is < 1 or > 50 ||
            sections.Any(x => !System.Text.RegularExpressions.Regex.IsMatch(x.SectionKey ?? "", "^[a-z0-9-]{1,100}$") ||
                              (x.Heading?.Length ?? 0) > 300 || string.IsNullOrWhiteSpace(x.Body) ||
                              x.Body.Length > 20000) ||
            sections.Select(x => x.SectionKey).Distinct(StringComparer.Ordinal).Count() != sections.Count)
            return BadRequest(new { title = "عنوان، شرح یا بخش‌های صفحه معتبر نیستند." });

        var sectionData = sections.Select(section => new ManagedPageSectionEditData(section.SectionKey!.Trim(),
            string.IsNullOrWhiteSpace(section.Heading) ? null : section.Heading!.Trim(), section.Body!.Trim(), true))
            .ToArray();
        if (!await data.SaveManagedPageAsync(pageKey, title, description, input.IsPublished, sectionData, ct))
            return NotFound();

        return Ok(new { message = "صفحهٔ خدماتی ذخیره شد." });
    }

    public sealed record SectionInput(string? SectionKey, string? Heading, string? Body, bool IsPublished = true);

    public sealed record PageInput(string? Title, string? Description, bool IsPublished, List<SectionInput>? Sections);
}
